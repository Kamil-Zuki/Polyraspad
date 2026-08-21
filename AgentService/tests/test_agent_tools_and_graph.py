"""Tests for LangChain tools and LangGraph workflow."""

import json
import uuid
import pytest
from src.agent.tools import create_agent_tools
from src.orchestration.metadata_builder import AgentActionCard, AgentExecutionResult, AgentMessageMetadataBuilder
from src.services.orchestrator import AgentOrchestrator
from tests.conftest import MockProjectAccessValidator, MockVocabularyClient, PROJECT_ID, USER_A


@pytest.mark.asyncio
async def test_tool_invocations():
    mock_client = MockVocabularyClient()
    tools = create_agent_tools(mock_client, USER_A, PROJECT_ID, ["user"])
    tools_map = {t.name: t for t in tools}

    assert len(tools) == 12

    # Test navigate tool
    nav_res = json.loads(await tools_map["navigate"].ainvoke({"destination": "reader", "label": "Go to Reader"}))
    assert nav_res["actionType"] == "navigate"
    assert nav_res["destination"] == "/reader"

    # Test open_editor_draft tool
    draft_res = json.loads(await tools_map["open_editor_draft"].ainvoke({
        "word": "solitude",
        "translation": "одиночество",
        "expression": "He enjoyed the solitude.",
    }))
    assert draft_res["actionType"] == "open_editor_draft"
    assert draft_res["payload"]["word"] == "solitude"

    # Test create_deck tool
    deck_res = json.loads(await tools_map["create_deck"].ainvoke({"title": "Travel English"}))
    assert "id" in deck_res
    assert deck_res["title"] == "Travel English"

    # Test get_user_vocabulary_stats tool
    stats_res = json.loads(await tools_map["get_user_vocabulary_stats"].ainvoke({}))
    assert stats_res["totalLemmas"] == 150
    assert stats_res["matureCount"] == 80


@pytest.mark.asyncio
async def test_metadata_builder_with_action_cards():
    action = AgentActionCard(
        id=str(uuid.uuid4()),
        title="Open Reader",
        kind="navigate",
        href="/reader",
        label="Open",
    )
    result = AgentExecutionResult(
        assistant_content="Sure! Opening reader...",
        domain_decision=type("Decision", (), {"allowed": True, "category_name": "product_navigation"})(),
        tool_calls=[],
        actions=[action],
    )
    meta = AgentMessageMetadataBuilder.build(result)
    assert meta is not None
    parsed = json.loads(meta)
    assert len(parsed["actions"]) == 1
    assert parsed["actions"][0]["href"] == "/reader"


@pytest.mark.asyncio
async def test_orchestrator_out_of_scope_execution(async_db):
    from src.services.thread_service import AgentThreadService

    thread_service = AgentThreadService(project_access_validator=MockProjectAccessValidator(), db_factory=async_db)
    thread = await thread_service.create_thread(USER_A, PROJECT_ID, ["user"])

    orchestrator = AgentOrchestrator(
        thread_service=thread_service,
        project_access_validator=MockProjectAccessValidator(),
        vocabulary_client=MockVocabularyClient(),
    )

    result = await orchestrator.execute_run(
        user_id=USER_A,
        thread_id=thread.id,
        project_id=PROJECT_ID,
        user_text="Write me Python homework please",
        roles=["user"],
    )

    assert result is not None
    assert "language learning" in result["assistant_message"].content
    assert result["run"].status == "completed"
