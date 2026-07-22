import json
import logging
import os
import threading
from concurrent import futures
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

DEFAULT_CONFIG = {"server_port": 50052, "default_language": "en", "max_pages": 40}

# EasyOCR language codes we ship for Polyraspad MVP.
SUPPORTED_LANGUAGES = {"en", "ru", "ko"}


def load_config():
    path = os.path.join(os.path.dirname(__file__), "config.json")
    if os.path.exists(path):
        try:
            with open(path, "r", encoding="utf-8") as f:
                return {**DEFAULT_CONFIG, **json.load(f)}
        except Exception:
            logging.exception("Failed to load config.json, using defaults")
    return DEFAULT_CONFIG


def map_language(language: str, default_language: str) -> str:
    lang = (language or default_language).strip().lower() or default_language
    primary = lang.split(",")[0].strip().split("-")[0]
    if primary in SUPPORTED_LANGUAGES:
        return primary
    logging.warning("Unsupported OCR language '%s', falling back to '%s'", language, default_language)
    return default_language if default_language in SUPPORTED_LANGUAGES else "en"


class OcrServiceServicer(proto.ocr_pb2_grpc.OcrServiceServicer):
    def __init__(self, default_language="en", max_pages=40):
        self.logger = logging.getLogger(__name__)
        self._default_language = default_language
        self._max_pages = max(1, int(max_pages))
        self._readers: Dict[str, object] = {}
        self._lock = threading.Lock()

    def _get_reader(self, language: str):
        primary = map_language(language, self._default_language)
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

        total_pages = len(images)
        images_to_ocr = images[: self._max_pages]
        truncated = total_pages > self._max_pages

        response = proto.ocr_pb2.RecognizeDocumentResponse()
        response.page_count = total_pages
        if truncated:
            response.warning = "OCR_PAGE_LIMIT"
            self.logger.warning(
                "OCR page limit: processing %d of %d pages",
                self._max_pages,
                total_pages,
            )

        full_parts = []

        for idx, image in enumerate(images_to_ocr, start=1):
            try:
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
            "OCR completed: %d/%d pages OCR'd, %d characters extracted, warning=%s",
            len(images_to_ocr),
            total_pages,
            len(response.text),
            response.warning or "",
        )
        return response


def serve():
    config = load_config()
    port = int(config.get("server_port", DEFAULT_CONFIG["server_port"]))
    default_language = config.get("default_language", DEFAULT_CONFIG["default_language"])
    max_pages = int(config.get("max_pages", DEFAULT_CONFIG["max_pages"]))

    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    proto.ocr_pb2_grpc.add_OcrServiceServicer_to_server(
        OcrServiceServicer(default_language=default_language, max_pages=max_pages), server
    )
    server.add_insecure_port(f"[::]:{port}")
    server.start()
    logging.info("OCR service started on port %d (max_pages=%d)", port, max_pages)
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
