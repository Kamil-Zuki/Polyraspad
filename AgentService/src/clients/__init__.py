"""Clients package for downstream gRPC communication."""
from src.clients.access_validator import VocabularyProjectAccessValidator
from src.clients.client_registry import (
    close_all_clients,
    get_inclusive_client,
    get_vocabulary_client,
)
from src.clients.inclusive_client import InclusiveGrpcClient
from src.clients.vocabulary_client import LearningTermDto, VocabularyGrpcClient

__all__ = [
    "VocabularyGrpcClient",
    "InclusiveGrpcClient",
    "VocabularyProjectAccessValidator",
    "LearningTermDto",
    "get_vocabulary_client",
    "get_inclusive_client",
    "close_all_clients",
]
