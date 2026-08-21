"""Agent orchestrator executing runs using LangGraph and domain policies."""

import logging
import uuid
from dataclasses import dataclass
from typing import Any, AsyncGenerator, Sequence
from langchain_core.messages import AIMessage, HumanMessage
from src.agent.graph import create_agent_graph
from src.agent.tools import create_agent_tools
from src.clients.access_validator import VocabularyProjectAccessValidator
from src.clients.client_registry import get_vocabulary_client
from src.clients.vocabulary_client import VocabularyGrpcClient
from src.config import settings
from src.orchestration.domain_policy import AgentDomainCategory, AgentDomainDecision, AgentDomainPolicy
from src.orchestration.intent_router import AgentIntentRouter
from src.orchestration.metadata_builder import (
    AgentActionCard,
    AgentExecutionResult,
    AgentMessageMetadataBuilder,
    AgentToolCallRecord,
)
from src.orchestration.prompt_builder import AgentSystemPromptBuilder
from src.services.thread_service import AgentThreadService

logger = logging.getLogger(__name__)


@dataclass
class ExecuteRunStreamEvent:
    content_chunk: str | None = None
    tool_call: dict[str, Any] | None = None
    final_result: dict[str, Any] | None = None
    error: str | None = None


class AgentOrchestrator:
    def __init__(
        self,
        thread_service: AgentThreadService | None = None,
        project_access_validator: VocabularyProjectAccessValidator | None = None,
        vocabulary_client: VocabularyGrpcClient | None = None,
    ):
        self._thread_service = thread_service or AgentThreadService()
        self._project_access_validator = project_access_validator or VocabularyProjectAccessValidator()
        # NOTE: vocabulary_client should be injected from client_registry.get_vocabulary_client()
        # to reuse the singleton channel. Fallback creates a new channel (acceptable for tests).
        self._vocabulary_client = vocabulary_client  # type: ignore[assignment]
        self._vocabulary_client_factory = get_vocabulary_client if vocabulary_client is None else None

    async def _get_vocabulary_client(self) -> VocabularyGrpcClient:
        """Return the injected client or resolve from the singleton registry."""
        if self._vocabulary_client is not None:
            return self._vocabulary_client
        client = await self._vocabulary_client_factory()  # type: ignore[misc]
        self._vocabulary_client = client
        return client

    async def execute_run(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        user_text: str,
        source_lang: str | None = None,
        target_lang: str | None = None,
        first_deck_id: str | None = None,
        is_initial_greeting: bool = False,
        roles: Sequence[str] = (),
    ) -> dict[str, Any] | None:
        final_result = None
        async for evt in self.execute_run_stream(
            user_id=user_id,
            thread_id=thread_id,
            project_id=project_id,
            user_text=user_text,
            source_lang=source_lang,
            target_lang=target_lang,
            first_deck_id=first_deck_id,
            is_initial_greeting=is_initial_greeting,
            roles=roles,
        ):
            if evt.final_result is not None:
                final_result = evt.final_result

        return final_result

    async def execute_run_stream(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        user_text: str,
        source_lang: str | None = None,
        target_lang: str | None = None,
        first_deck_id: str | None = None,
        is_initial_greeting: bool = False,
        roles: Sequence[str] = (),
    ) -> AsyncGenerator[ExecuteRunStreamEvent, None]:
        if not user_text or not user_text.strip():
            raise ValueError("User text is required")

        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))
        p_id = uuid.UUID(str(project_id))

        project = await self._project_access_validator.ensure_project_access(u_id, p_id, roles)

        s_lang = source_lang or (project.source_lang if hasattr(project, "source_lang") else "en")
        t_lang = target_lang or (project.target_lang if hasattr(project, "target_lang") else "ru")
        p_title = project.title if hasattr(project, "title") else "Language Learning"

        thread = await self._thread_service.get_thread(u_id, t_id)
        agent_id = thread.agent_id if thread and thread.agent_id else "study-copilot"

        system_prompt = (
            thread.system_prompt_override
            if thread and thread.system_prompt_override and thread.system_prompt_override.strip()
            else AgentSystemPromptBuilder.build(agent_id, p_title, s_lang, t_lang)
        )

        intent = AgentIntentRouter.route(user_text, is_initial_greeting)

        if not intent.domain or not intent.domain.allowed:
            refusal = AgentDomainPolicy.build_out_of_scope_refusal(user_text, s_lang)
            refused_run_result = await self._thread_service.create_run(
                user_id=u_id,
                thread_id=t_id,
                project_id=p_id,
                user_message_dict={"role": "user", "content": user_text.strip()},
                assistant_message_dict={
                    "role": "assistant",
                    "content": refusal,
                    "metadata_json": AgentMessageMetadataBuilder.build(
                        AgentExecutionResult(
                            assistant_content=refusal,
                            domain_decision=intent.domain or AgentDomainDecision(False, AgentDomainCategory.OUT_OF_SCOPE),
                            tool_calls=[],
                            intent_category=intent.domain.category_name if intent.domain else "out_of_scope",
                            refusal=True,
                        )
                    ),
                },
                domain_decision_dict={
                    "allowed": intent.domain.allowed if intent.domain else False,
                    "category": intent.domain.category_name if intent.domain else "out_of_scope",
                    "reason": intent.domain.reason if intent.domain else None,
                },
                tool_calls_list=[],
                model=None,
            )

            yield ExecuteRunStreamEvent(content_chunk=refusal)
            yield ExecuteRunStreamEvent(final_result=refused_run_result)
            return

        # Prepare LangGraph state & tools
        tools = create_agent_tools(
            vocabulary_client=await self._get_vocabulary_client(),
            user_id=u_id,
            project_id=p_id,
            roles=roles,
        )

        history_msgs, _ = await self._thread_service.list_messages(u_id, t_id, limit=10)
        messages: list[Any] = []
        for m in history_msgs:
            if m.role.lower() == "user" and m.content:
                messages.append(HumanMessage(content=m.content))
            elif m.role.lower() == "assistant" and m.content:
                messages.append(AIMessage(content=m.content))

        messages.append(HumanMessage(content=user_text.strip()))

        assistant_chunks: list[str] = []
        executed_tool_records: list[AgentToolCallRecord] = []
        actions_list: list[AgentActionCard] = []
        invocation_exception: Exception | None = None

        if not settings.AI_ENABLED:
            invocation_exception = RuntimeError("AI completion is disabled in configuration.")
        else:
            try:
                graph = create_agent_graph()
                initial_state = {
                    "messages": messages,
                    "tools": tools,
                    "system_prompt": system_prompt,
                    "executed_tools": [],
                    "actions": [],
                }

                async for event in graph.astream_events(initial_state, version="v2"):
                    kind = event.get("event")

                    # Handle token chunk streaming from LLM
                    if kind == "on_chat_model_stream":
                        chunk = event.get("data", {}).get("chunk")
                        if chunk and hasattr(chunk, "content") and chunk.content:
                            content_str = str(chunk.content)
                            assistant_chunks.append(content_str)
                            yield ExecuteRunStreamEvent(content_chunk=content_str)

                    # Handle tool completion events
                    elif kind == "on_tool_end":
                        t_data = event.get("data", {})
                        t_name = event.get("name", "")
                        t_output = str(t_data.get("output", "{}"))
                        t_input = json.dumps(t_data.get("input", {}))

                        tc_record = AgentToolCallRecord(
                            tool_name=t_name,
                            input_json=t_input,
                            output_json=t_output,
                            status="completed",
                        )
                        executed_tool_records.append(tc_record)

                        yield ExecuteRunStreamEvent(
                            tool_call={
                                "tool_name": t_name,
                                "input_json": t_input,
                                "output_json": t_output,
                                "status": "completed",
                            }
                        )

                        # Extract navigation / editor actions
                        try:
                            parsed = json.loads(t_output) if isinstance(t_output, str) else t_output
                            if isinstance(parsed, dict) and "actionType" in parsed:
                                if parsed["actionType"] == "navigate":
                                    actions_list.append(
                                        AgentActionCard(
                                            id=str(uuid.uuid4()),
                                            title=parsed.get("label", "Navigate"),
                                            kind="navigate",
                                            href=parsed.get("destination", "/"),
                                            label="Open",
                                            description=parsed.get("description"),
                                        )
                                    )
                                elif parsed["actionType"] == "open_editor_draft":
                                    payload = parsed.get("payload", {})
                                    draft = {
                                        "word": payload.get("word", ""),
                                        "expression": payload.get("expression", ""),
                                        "translation": payload.get("translation", ""),
                                    }
                                    actions_list.append(
                                        AgentActionCard(
                                            id=str(uuid.uuid4()),
                                            title=parsed.get("label", "Draft Card"),
                                            kind="open_editor_draft",
                                            href=parsed.get("destination", "/editor"),
                                            label="Open Editor",
                                            description=parsed.get("description"),
                                            editor_draft=draft,
                                        )
                                    )
                        except Exception:
                            pass

            except Exception as ex:
                invocation_exception = ex
                logger.error("Error occurred during LangGraph agent execution: %s", ex, exc_info=True)

        if invocation_exception:
            error_msg = f"\n*[System: Произошла ошибка при обращении к AI-модели: {invocation_exception}]*"
            assistant_chunks.append(error_msg)
            yield ExecuteRunStreamEvent(content_chunk=error_msg, error=str(invocation_exception))

        assistant_content = "".join(assistant_chunks).strip()

        # Fallback text if empty
        if not assistant_content:
            if executed_tool_records:
                assistant_content = "Я успешно выполнил запрошенные действия."
            elif invocation_exception:
                assistant_content = f"*[System: Произошла ошибка при обращении к AI-модели: {invocation_exception}]*"
            else:
                assistant_content = "Привет! Чем я могу помочь вам в изучении языка?"

        execution_result = AgentExecutionResult(
            assistant_content=assistant_content,
            domain_decision=intent.domain or AgentDomainDecision(True, AgentDomainCategory.LANGUAGE_LEARNING),
            tool_calls=executed_tool_records,
            intent_category=intent.domain.category_name if intent.domain else "language_learning",
            actions=actions_list if actions_list else None,
        )

        final_run_result = await self._thread_service.create_run(
            user_id=u_id,
            thread_id=t_id,
            project_id=p_id,
            user_message_dict={
                "role": "user",
                "content": user_text.strip(),
            },
            assistant_message_dict={
                "role": "assistant",
                "content": execution_result.assistant_content,
                "metadata_json": AgentMessageMetadataBuilder.build(execution_result),
            },
            domain_decision_dict={
                "allowed": execution_result.domain_decision.allowed,
                "category": execution_result.domain_decision.category_name,
                "reason": execution_result.domain_decision.reason,
            },
            tool_calls_list=[
                {
                    "tool_name": tc.tool_name,
                    "input_json": tc.input_json,
                    "output_json": tc.output_json,
                    "status": tc.status,
                }
                for tc in executed_tool_records
            ],
            model=settings.AI_MODEL if settings.AI_ENABLED else None,
        )

        yield ExecuteRunStreamEvent(final_result=final_run_result)
