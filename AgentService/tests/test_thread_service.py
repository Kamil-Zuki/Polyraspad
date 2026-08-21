"""Tests for AgentThreadService."""

import uuid
import pytest
from src.orchestration.metadata_builder import AgentThreadTitleHelper
from src.services.thread_service import AgentThreadService
from tests.conftest import MockProjectAccessValidator, PROJECT_ID, USER_A, USER_B


@pytest.mark.asyncio
async def test_list_threads_returns_non_archived_ordered_by_updated_at_desc(async_db):
    sut = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)

    t1 = await sut.create_thread(USER_A, PROJECT_ID, ["user"])
    t2 = await sut.create_thread(USER_A, PROJECT_ID, ["user"])

    threads = await sut.list_threads(USER_A, PROJECT_ID, ["user"])
    assert len(threads) == 2
    assert threads[0].id == t2.id
    assert threads[1].id == t1.id


@pytest.mark.asyncio
async def test_get_thread_cross_user_returns_none(async_db):
    sut = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)

    thread = await sut.create_thread(USER_A, PROJECT_ID, ["user"])
    fetched_by_other = await sut.get_thread(USER_B, thread.id)
    assert fetched_by_other is None

    fetched_by_owner = await sut.get_thread(USER_A, thread.id)
    assert fetched_by_owner is not None
    assert fetched_by_owner.id == thread.id


@pytest.mark.asyncio
async def test_list_messages_returns_created_at_ascending_order(async_db):
    sut = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    thread = await sut.create_thread(USER_A, PROJECT_ID, ["user"])

    run_res = await sut.create_run(
        user_id=USER_A,
        thread_id=thread.id,
        project_id=PROJECT_ID,
        user_message_dict={"role": "user", "content": "first"},
        assistant_message_dict={"role": "assistant", "content": "second"},
        domain_decision_dict={"allowed": True, "category": "language_learning"},
        tool_calls_list=[],
    )
    assert run_res is not None

    messages, next_before = await sut.list_messages(USER_A, thread.id, limit=100)
    assert len(messages) == 2
    assert messages[0].content == "first"
    assert messages[1].content == "second"
    assert next_before is None


@pytest.mark.asyncio
async def test_create_run_persists_out_of_scope_domain_decision_and_title(async_db):
    sut = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    thread = await sut.create_thread(USER_A, PROJECT_ID, ["user"])

    result = await sut.create_run(
        user_id=USER_A,
        thread_id=thread.id,
        project_id=PROJECT_ID,
        user_message_dict={"role": "user", "content": "Write me Python homework please"},
        assistant_message_dict={
            "role": "assistant",
            "content": "I can only help with language learning.",
            "metadata_json": {"refusal": True, "intentCategory": "out_of_scope"},
        },
        domain_decision_dict={
            "allowed": False,
            "category": "out_of_scope",
            "reason": "Programming homework",
        },
        tool_calls_list=[],
    )

    assert result is not None
    assert result["run"].status == "completed"

    updated_thread = await sut.get_thread(USER_A, thread.id)
    assert updated_thread.title == AgentThreadTitleHelper.derive_title("Write me Python homework please")


@pytest.mark.asyncio
async def test_archive_thread_excludes_thread_from_list(async_db):
    sut = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    thread = await sut.create_thread(USER_A, PROJECT_ID, ["user"])

    archived = await sut.archive_thread(USER_A, thread.id)
    assert archived is True

    threads = await sut.list_threads(USER_A, PROJECT_ID, ["user"])
    assert len(threads) == 0


@pytest.mark.asyncio
async def test_create_run_wrong_project_returns_none(async_db):
    sut = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    thread = await sut.create_thread(USER_A, PROJECT_ID, ["user"])

    other_project_id = uuid.uuid4()
    result = await sut.create_run(
        user_id=USER_A,
        thread_id=thread.id,
        project_id=other_project_id,
        user_message_dict={"role": "user", "content": "Hello"},
        assistant_message_dict={"role": "assistant", "content": "Hi"},
        domain_decision_dict={"allowed": True, "category": "language_learning"},
        tool_calls_list=[],
    )
    assert result is None
