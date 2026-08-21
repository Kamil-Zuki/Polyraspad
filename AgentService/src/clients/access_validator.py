"""Project access validator via VocabularyService."""

import uuid
from typing import Sequence
import grpc
from src.config import _normalize_grpc_address, settings
from src.proto import vocabulary_pb2, vocabulary_pb2_grpc


class KeyNotFoundException(Exception):
    pass


class VocabularyProjectAccessValidator:
    def __init__(self, address: str | None = None):
        self._address = _normalize_grpc_address(address or settings.VOCABULARY_GRPC_ADDRESS, "vocabulary-service:5117")
        self._channel = grpc.aio.insecure_channel(self._address)
        self._content_stub = vocabulary_pb2_grpc.ContentServiceStub(self._channel)

    async def ensure_project_access(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> vocabulary_pb2.ProjectResponse:
        req = vocabulary_pb2.GetProjectDetailsRequest(
            user_id=str(user_id),
            project_id=str(project_id),
        )
        metadata = (
            ("user_id", str(user_id)),
            ("roles", ",".join(roles)),
        )
        try:
            return await self._content_stub.GetProjectDetails(req, metadata=metadata)
        except grpc.aio.AioRpcError as ex:
            if ex.code() in (grpc.StatusCode.NOT_FOUND, grpc.StatusCode.PERMISSION_DENIED):
                raise KeyNotFoundException(f"Project {project_id} not found or access denied")
            raise

    async def close(self) -> None:
        await self._channel.close()
