"""LangGraph StateGraph definition for Polyraspad agent."""

import json
import logging
from typing import Annotated, Any, Sequence, TypedDict
from langchain_core.messages import (
    AIMessage,
    BaseMessage,
    HumanMessage,
    SystemMessage,
    ToolMessage,
)
from langchain_core.tools import BaseTool
from langchain_openai import ChatOpenAI
from langgraph.graph import END, START, StateGraph
from langgraph.graph.message import add_messages
from src.config import settings

logger = logging.getLogger(__name__)


def _merge_lists(a: list, b: list) -> list:
    return (a or []) + (b or [])


class AgentState(TypedDict):
    messages: Annotated[list[BaseMessage], add_messages]
    tools: list[BaseTool]
    system_prompt: str
    executed_tools: Annotated[list[dict[str, Any]], _merge_lists]
    actions: Annotated[list[dict[str, Any]], _merge_lists]


def create_llm(model_name: str | None = None) -> ChatOpenAI:
    """Create ChatOpenAI instance configured with environment settings."""
    api_key = settings.AI_API_KEY or "not-needed"
    base_url = settings.AI_BASE_URL or "https://api.openai.com/v1"
    model = model_name or settings.AI_MODEL

    default_headers: dict[str, str] = {}
    if "openrouter.ai" in base_url.lower():
        default_headers["HTTP-Referer"] = "https://polyraspad.online"
        default_headers["X-Title"] = "Polyraspad"

    return ChatOpenAI(
        model=model,
        api_key=api_key,
        base_url=base_url,
        default_headers=default_headers if default_headers else None,
        timeout=settings.AI_TIMEOUT_SECONDS,
        temperature=0.7,
    )


async def call_model(state: AgentState) -> dict[str, Any]:
    """Invoke the language model with bound tools and system prompt."""
    messages = list(state["messages"])
    system_prompt = state.get("system_prompt")

    # Ensure system prompt is prepended if not already first message
    if system_prompt and (not messages or not isinstance(messages[0], SystemMessage)):
        messages = [SystemMessage(content=system_prompt)] + messages

    llm = create_llm()
    tools = state.get("tools", [])

    if tools:
        llm_with_tools = llm.bind_tools(tools)
        response = await llm_with_tools.ainvoke(messages)
    else:
        response = await llm.ainvoke(messages)

    return {"messages": [response]}


async def execute_tools(state: AgentState) -> dict[str, Any]:
    """Execute tools invoked by the language model and extract action cards."""
    messages = state["messages"]
    last_message = messages[-1] if messages else None

    if not isinstance(last_message, AIMessage) or not last_message.tool_calls:
        return {"messages": [], "executed_tools": [], "actions": []}

    tools_by_name = {t.name: t for t in state.get("tools", [])}
    tool_messages = []
    executed_records = []
    actions = []

    for tc in last_message.tool_calls:
        tool_name = tc.get("name", "")
        tool_args = tc.get("args", {})
        tool_id = tc.get("id", "")

        target_tool = tools_by_name.get(tool_name)
        if not target_tool:
            err_msg = json.dumps({"error": f"Tool '{tool_name}' not found."})
            tool_messages.append(ToolMessage(content=err_msg, tool_call_id=tool_id))
            executed_records.append({
                "tool_name": tool_name,
                "input_json": json.dumps(tool_args),
                "output_json": err_msg,
                "status": "failed",
            })
            continue

        try:
            result = await target_tool.ainvoke(tool_args)
            result_str = str(result) if not isinstance(result, str) else result
            tool_messages.append(ToolMessage(content=result_str, tool_call_id=tool_id))
            executed_records.append({
                "tool_name": tool_name,
                "input_json": json.dumps(tool_args),
                "output_json": result_str,
                "status": "completed",
            })

            # Check for UI action cards in output
            try:
                parsed = json.loads(result_str)
                if isinstance(parsed, dict) and "actionType" in parsed:
                    action_type = parsed.get("actionType")
                    if action_type == "navigate":
                        actions.append({
                            "id": tool_id or tool_name,
                            "title": parsed.get("label", "Navigate"),
                            "kind": "navigate",
                            "href": parsed.get("destination", "/"),
                            "label": "Open",
                            "description": parsed.get("description"),
                        })
                    elif action_type == "open_editor_draft":
                        payload = parsed.get("payload", {})
                        draft = {}
                        if "word" in payload:
                            draft["word"] = payload["word"] or ""
                        if "expression" in payload:
                            draft["expression"] = payload["expression"] or ""
                        if "translation" in payload:
                            draft["translation"] = payload["translation"] or ""

                        actions.append({
                            "id": tool_id or tool_name,
                            "title": parsed.get("label", "Draft Card"),
                            "kind": "open_editor_draft",
                            "href": parsed.get("destination", "/editor"),
                            "label": "Open Editor",
                            "description": parsed.get("description"),
                            "editor_draft": draft,
                        })
            except Exception:
                pass

        except Exception as ex:
            logger.error("Error executing tool %s: %s", tool_name, ex)
            err_msg = json.dumps({"error": str(ex)})
            tool_messages.append(ToolMessage(content=err_msg, tool_call_id=tool_id))
            executed_records.append({
                "tool_name": tool_name,
                "input_json": json.dumps(tool_args),
                "output_json": err_msg,
                "status": "failed",
            })

    return {
        "messages": tool_messages,
        "executed_tools": executed_records,
        "actions": actions,
    }


def should_continue(state: AgentState) -> str:
    """Determine whether the model wants to call tools or finish."""
    messages = state["messages"]
    last_message = messages[-1] if messages else None

    if isinstance(last_message, AIMessage) and last_message.tool_calls:
        return "tools"
    return "end"


def create_agent_graph():
    """Build and compile the LangGraph agent state graph."""
    workflow = StateGraph(AgentState)

    workflow.add_node("model", call_model)
    workflow.add_node("tools", execute_tools)

    workflow.add_edge(START, "model")

    workflow.add_conditional_edges(
        "model",
        should_continue,
        {
            "tools": "tools",
            "end": END,
        },
    )
    workflow.add_edge("tools", "model")

    return workflow.compile()

