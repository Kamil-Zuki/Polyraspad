import json
import logging
import os
import sys
import threading
from concurrent import futures
from io import BytesIO
from typing import Dict

import grpc
import numpy as np
import proto.ocr_pb2
import proto.ocr_pb2_grpc
from pdf2image import convert_from_bytes

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)

DEFAULT_CONFIG = {"server_port": 50052, "default_language": "en"}


def load_config():
    path = os.path.join(os.path.dirname(__file__), "config.json")
    if os.path.exists(path):
        try:
            with open(path, "r", encoding="utf-8") as f:
                return {**DEFAULT_CONFIG, **json.load(f)}
        except Exception:
            logging.exception("Failed to load config.json, using defaults")
    return DEFAULT_CONFIG


class OcrServiceServicer(proto.ocr_pb2_grpc.OcrServiceServicer):
    def __init__(self, default_language="en"):
        self.logger = logging.getLogger(__name__)
        self._default_language = default_language
        self._readers: Dict[str, object] = {}
        self._lock = threading.Lock()

    def _get_reader(self, language: str):
        lang = (language or self._default_language).strip() or self._default_language
        # EasyOCR uses language codes like "en", "ch_sim", etc.
        # Support a simple comma-separated list by taking the first code.
        primary = lang.split(",")[0].strip()
        with self._lock:
            reader = self._readers.get(primary)
            if reader is None:
                self.logger.info("Initializing EasyOCR reader for language '%s'", primary)
                import easyocr

                reader = easyocr.Reader([primary], gpu=False, verbose=False)
                self._readers[primary] = reader
            return reader

    def RecognizeDocument(self, request, context):
        self.logger.info(
            "RecognizeDocument request: file_name=%s mime_type=%s language=%s size=%d",
            request.file_name,
            request.mime_type,
            request.language,
            len(request.content),
        )

        if not request.content:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details("Document content is empty")
            return proto.ocr_pb2.RecognizeDocumentResponse()

        file_name = (request.file_name or "").lower()
        mime_type = (request.mime_type or "").lower()
        is_pdf = file_name.endswith(".pdf") or "application/pdf" in mime_type

        if not is_pdf:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details("Only PDF documents are supported by the OCR service")
            return proto.ocr_pb2.RecognizeDocumentResponse()

        try:
            images = convert_from_bytes(request.content, dpi=200, fmt="RGB")
        except Exception as e:
            self.logger.exception("Failed to convert PDF to images")
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details(f"Could not convert PDF to images: {e}")
            return proto.ocr_pb2.RecognizeDocumentResponse()

        if not images:
            context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
            context.set_details("PDF contains no pages")
            return proto.ocr_pb2.RecognizeDocumentResponse()

        try:
            reader = self._get_reader(request.language)
        except Exception as e:
            self.logger.exception("Failed to initialize OCR reader")
            context.set_code(grpc.StatusCode.INTERNAL)
            context.set_details(f"Could not initialize OCR reader: {e}")
            return proto.ocr_pb2.RecognizeDocumentResponse()

        response = proto.ocr_pb2.RecognizeDocumentResponse()
        response.page_count = len(images)
        full_parts = []

        for idx, image in enumerate(images, start=1):
            try:
                # EasyOCR expects a numpy array in BGR or RGB; RGB works with opencv under the hood.
                array = np.array(image)
                lines = reader.readtext(array, detail=0, paragraph=True)
                page_text = "\n".join(lines)
            except Exception as e:
                self.logger.exception("OCR failed for page %d", idx)
                context.set_code(grpc.StatusCode.INTERNAL)
                context.set_details(f"OCR failed on page {idx}: {e}")
                return proto.ocr_pb2.RecognizeDocumentResponse()

            page = response.pages.add()
            page.page_number = idx
            page.text = page_text
            if page_text:
                full_parts.append(page_text)

        response.text = "\n\n".join(full_parts)
        self.logger.info(
            "OCR completed: %d pages, %d characters extracted",
            response.page_count,
            len(response.text),
        )
        return response


def serve():
    config = load_config()
    port = int(config.get("server_port", DEFAULT_CONFIG["server_port"]))
    default_language = config.get("default_language", DEFAULT_CONFIG["default_language"])

    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    proto.ocr_pb2_grpc.add_OcrServiceServicer_to_server(
        OcrServiceServicer(default_language=default_language), server
    )
    server.add_insecure_port(f"[::]:{port}")
    server.start()
    logging.info("OCR service started on port %d", port)
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
