"""Tests for AgentGrpcServicer endpoints."""

import uuid
from google.protobuf import wrappers_pb2
import grpc
import pytest
from src.grpc.agent_servicer import AgentGrpcServicer
from src.proto import agent_pb2
from src.services.orchestrator import AgentOrchestrator
from src.services.thread_service import AgentThreadService
from tests.conftest import MockProjectAccessValidator, MockVocabularyClient, PROJECT_ID, USER_A


class MockServicerContext:
    def __init__(self, user_id=USER_A, roles=("user",)):
        self._metadata = [
            ("user_id", str(user_id)),
            ("roles", ",".join(roles)),
        ]
        self.aborted = False
        self.code = None
        self.details = None

    def invocation_metadata(self):
        return self._metadata

    def abort(self, code, details):
        self.aborted = True
        self.code = code
        self.details = details
        raise grpc.RpcError(f"RPC Aborted: {code} - {details}")


@pytest.mark.asyncio
async def test_grpc_create_and_list_threads(async_db):
    thread_service = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    orchestrator = AgentOrchestrator(
        thread_service=thread_service,
        project_access_validator=MockProjectAccessValidator(),
        vocabulary_client=MockVocabularyClient(),
    )
    servicer = AgentGrpcServicer(thread_service=thread_service, orchestrator=orchestrator)
    ctx = MockServicerContext()

    create_req = agent_pb2.CreateAgentThreadRequest(
        user_id=str(USER_A),
        project_id=str(PROJECT_ID),
        agent_id="study-copilot",
    )
    create_resp = await servicer.CreateThread(create_req, ctx)
    assert create_resp.id is not None
    assert create_resp.title == "New conversation"

    list_req = agent_pb2.ListAgentThreadsRequest(
        user_id=str(USER_A),
        project_id=str(PROJECT_ID),
    )
    list_resp = await servicer.ListThreads(list_req, ctx)
    assert len(list_resp.items) == 1
    assert list_resp.items[0].id == create_resp.id


@pytest.mark.asyncio
async def test_grpc_get_thread_and_messages(async_db):
    thread_service = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    orchestrator = AgentOrchestrator(
        thread_service=thread_service,
        project_access_validator=MockProjectAccessValidator(),
        vocabulary_client=MockVocabularyClient(),
    )
    servicer = AgentGrpcServicer(thread_service=thread_service, orchestrator=orchestrator)
    ctx = MockServicerContext()

    create_req = agent_pb2.CreateAgentThreadRequest(
        user_id=str(USER_A),
        project_id=str(PROJECT_ID),
    )
    created = await servicer.CreateThread(create_req, ctx)

    get_resp = await servicer.GetThread(agent_pb2.GetAgentThreadRequest(thread_id=created.id), ctx)
    assert get_resp.id == created.id

    msg_resp = await servicer.ListMessages(agent_pb2.ListAgentMessagesRequest(thread_id=created.id), ctx)
    assert len(msg_resp.items) == 0


@pytest.mark.asyncio
async def test_grpc_execute_run_stream_refusal(async_db):
    thread_service = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    orchestrator = AgentOrchestrator(
        thread_service=thread_service,
        project_access_validator=MockProjectAccessValidator(),
        vocabulary_client=MockVocabularyClient(),
    )
    servicer = AgentGrpcServicer(thread_service=thread_service, orchestrator=orchestrator)
    ctx = MockServicerContext()

    create_req = agent_pb2.CreateAgentThreadRequest(
        user_id=str(USER_A),
        project_id=str(PROJECT_ID),
    )
    created = await servicer.CreateThread(create_req, ctx)

    stream_req = agent_pb2.ExecuteAgentRunRequest(
        thread_id=created.id,
        project_id=str(PROJECT_ID),
        user_text="Write me Python homework please",
    )

    events = []
    async for evt in servicer.ExecuteRunStream(stream_req, ctx):
        events.append(evt)

    assert len(events) >= 2
    assert any(e.HasField("content_chunk") and "language learning" in e.content_chunk for e in events)
    assert any(e.HasField("final_result") for e in events)
