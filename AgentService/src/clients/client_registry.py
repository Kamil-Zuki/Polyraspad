"""Singleton gRPC client registry for downstream services.

Provides module-level singletons for VocabularyGrpcClient and InclusiveGrpcClient
to ensure a single persistent connection pool per process.
"""

from __future__ import annotations

import asyncio
import logging
from typing import TYPE_CHECKING

from src.config import settings

if TYPE_CHECKING:
    from src.clients.inclusive_client import InclusiveGrpcClient
    from src.clients.vocabulary_client import VocabularyGrpcClient

logger = logging.getLogger(__name__)

_vocabulary_client: "VocabularyGrpcClient | None" = None
_inclusive_client: "InclusiveGrpcClient | None" = None
_lock = asyncio.Lock()


async def get_vocabulary_client() -> "VocabularyGrpcClient":
    """Return the singleton VocabularyGrpcClient, creating it if necessary."""
    global _vocabulary_client
    if _vocabulary_client is None:
        async with _lock:
            if _vocabulary_client is None:
                from src.clients.vocabulary_client import VocabularyGrpcClient
                _vocabulary_client = VocabularyGrpcClient(settings.VOCABULARY_GRPC_ADDRESS)
                logger.info("VocabularyGrpcClient connected to %s", settings.VOCABULARY_GRPC_ADDRESS)
    return _vocabulary_client


async def get_inclusive_client() -> "InclusiveGrpcClient":
    """Return the singleton InclusiveGrpcClient, creating it if necessary."""
    global _inclusive_client
    if _inclusive_client is None:
        async with _lock:
            if _inclusive_client is None:
                from src.clients.inclusive_client import InclusiveGrpcClient
                _inclusive_client = InclusiveGrpcClient(settings.INCLUSIVE_GRPC_ADDRESS)
                logger.info("InclusiveGrpcClient connected to %s", settings.INCLUSIVE_GRPC_ADDRESS)
    return _inclusive_client


async def close_all_clients() -> None:
    """Gracefully close all downstream gRPC channels on shutdown."""
    global _vocabulary_client, _inclusive_client

    if _vocabulary_client is not None:
        await _vocabulary_client.close()
        _vocabulary_client = None
        logger.info("VocabularyGrpcClient channel closed.")

    if _inclusive_client is not None:
        await _inclusive_client.close()
        _inclusive_client = None
        logger.info("InclusiveGrpcClient channel closed.")
