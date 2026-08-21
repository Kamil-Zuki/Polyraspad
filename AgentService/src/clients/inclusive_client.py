"""Async gRPC client for Inclusive NLP service."""

import logging
import grpc
from src.config import _normalize_grpc_address, settings
from src.proto import vocab_pb2, vocab_pb2_grpc

logger = logging.getLogger(__name__)


class InclusiveGrpcClient:
    def __init__(self, address: str | None = None):
        self._address = _normalize_grpc_address(address or settings.INCLUSIVE_GRPC_ADDRESS, "inclusive:40051")
        self._channel = grpc.aio.insecure_channel(self._address)
        self._stub = vocab_pb2_grpc.VocabServiceStub(self._channel)

    async def analyze_text(self, text: str) -> vocab_pb2.AnalyzeTextResponse | None:
        try:
            req = vocab_pb2.AnalyzeTextRequest(text=text)
            return await self._stub.AnalyzeText(req)
        except Exception as ex:
            logger.warning("Inclusive AnalyzeText failed for text length %d: %s", len(text), ex)
            return None

    async def analyze_target_word(self, sentence: str, target_word: str) -> vocab_pb2.AnalyzeTargetWordResponse | None:
        try:
            req = vocab_pb2.AnalyzeTargetWordRequest(sentence=sentence, target_word=target_word)
            return await self._stub.AnalyzeTargetWord(req)
        except Exception as ex:
            logger.warning("Inclusive AnalyzeTargetWord failed for target word %s: %s", target_word, ex)
            return None

    async def close(self) -> None:
        await self._channel.close()
