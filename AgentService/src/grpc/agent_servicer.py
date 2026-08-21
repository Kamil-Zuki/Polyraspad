"""gRPC Servicer implementing pvs.agent.grpc.AgentService."""

import json
import logging
import uuid
from typing import Any
from google.protobuf import empty_pb2, timestamp_pb2, wrappers_pb2
import grpc
from src.clients.access_validator import KeyNotFoundException
from src.db.models import AgentArtifact, AgentMessage, AgentRun, AgentThread
from src.grpc.context_helper import get_roles, get_user_id
from src.proto import agent_pb2, agent_pb2_grpc
from src.services.orchestrator import AgentOrchestrator
from src.services.thread_service import AgentThreadService

logger = logging.getLogger(__name__)


def _to_timestamp(dt) -> timestamp_pb2.Timestamp:
    ts = timestamp_pb2.Timestamp()
    if dt is not None:
        ts.FromDatetime(dt)
    return ts


def _to_string_value(val: str | None) -> wrappers_pb2.StringValue | None:
    if val is None or not str(val).strip():
        return None
    return wrappers_pb2.StringValue(value=str(val))


def _map_thread(thread: AgentThread) -> agent_pb2.AgentThreadResponse:
    resp = agent_pb2.AgentThreadResponse(
        id=str(thread.id),
        project_id=str(thread.project_id),
        title=thread.title or "New conversation",
        created_at=_to_timestamp(thread.created_at),
        updated_at=_to_timestamp(thread.updated_at),
    )
    if thread.archived_at is not None:
        resp.archived_at.CopyFrom(_to_timestamp(thread.archived_at))
    if thread.agent_id:
        resp.agent_id.CopyFrom(wrappers_pb2.StringValue(value=thread.agent_id))
    return resp


def _map_thread_list_item(thread: AgentThread) -> agent_pb2.AgentThreadListItem:
    item = agent_pb2.AgentThreadListItem(
        id=str(thread.id),
        project_id=str(thread.project_id),
        title=thread.title or "New conversation",
        created_at=_to_timestamp(thread.created_at),
        updated_at=_to_timestamp(thread.updated_at),
    )
    if thread.agent_id:
        item.agent_id.CopyFrom(wrappers_pb2.StringValue(value=thread.agent_id))
    return item


def _map_message_item(message: AgentMessage) -> agent_pb2.AgentMessageItem:
    item = agent_pb2.AgentMessageItem(
        id=str(message.id),
        role=message.role,
        content=message.content,
        created_at=_to_timestamp(message.created_at),
    )
    if message.metadata_json is not None:
        meta_str = json.dumps(message.metadata_json) if isinstance(message.metadata_json, dict) else str(message.metadata_json)
        item.metadata_json.CopyFrom(wrappers_pb2.StringValue(value=meta_str))
    return item


def _map_run_item(run: AgentRun) -> agent_pb2.AgentRunItem:
    item = agent_pb2.AgentRunItem(
        id=str(run.id),
        thread_id=str(run.thread_id),
        status=run.status,
        started_at=_to_timestamp(run.started_at),
    )
    if run.model:
        item.model.CopyFrom(wrappers_pb2.StringValue(value=run.model))
    if run.completed_at:
        item.completed_at.CopyFrom(_to_timestamp(run.completed_at))
    return item


def _map_run_response(res: dict[str, Any]) -> agent_pb2.CreateAgentRunResponse:
    return agent_pb2.CreateAgentRunResponse(
        run=_map_run_item(res["run"]),
        user_message=_map_message_item(res["user_message"]),
        assistant_message=_map_message_item(res["assistant_message"]),
    )


def _map_artifact_item(artifact: AgentArtifact) -> agent_pb2.AgentArtifactItem:
    payload_str = json.dumps(artifact.payload_json) if isinstance(artifact.payload_json, dict) else str(artifact.payload_json)
    return agent_pb2.AgentArtifactItem(
        id=str(artifact.id),
        run_id=str(artifact.run_id),
        thread_id=str(artifact.thread_id),
        kind=artifact.kind,
        payload_json=payload_str,
        created_at=_to_timestamp(artifact.created_at),
    )


class AgentGrpcServicer(agent_pb2_grpc.AgentServiceServicer):
    def __init__(
        self,
        thread_service: AgentThreadService | None = None,
        orchestrator: AgentOrchestrator | None = None,
    ):
        self._thread_service = thread_service or AgentThreadService()
        self._orchestrator = orchestrator or AgentOrchestrator(thread_service=self._thread_service)

    async def ListThreads(
        self,
        request: agent_pb2.ListAgentThreadsRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.ListAgentThreadsResponse:
        user_id = await get_user_id(context)
        roles = get_roles(context)

        try:
            project_id = uuid.UUID(request.project_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Project ID format")

        try:
            agent_id = request.agent_id if request.agent_id else None
            threads = await self._thread_service.list_threads(
                user_id=user_id,
                project_id=project_id,
                roles=roles,
                agent_id=agent_id,
            )
            return agent_pb2.ListAgentThreadsResponse(items=[_map_thread_list_item(t) for t in threads])
        except KeyNotFoundException as ex:
            await context.abort(grpc.StatusCode.NOT_FOUND, str(ex))
        except Exception as ex:
            logger.error("Error listing agent threads: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def CreateThread(
        self,
        request: agent_pb2.CreateAgentThreadRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.AgentThreadResponse:
        user_id = await get_user_id(context)
        roles = get_roles(context)

        try:
            project_id = uuid.UUID(request.project_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Project ID format")

        agent_id = request.agent_id if request.agent_id else None
        system_prompt = (
            request.system_prompt_override.value
            if request.HasField("system_prompt_override")
            else None
        )

        try:
            thread = await self._thread_service.create_thread(
                user_id=user_id,
                project_id=project_id,
                roles=roles,
                agent_id=agent_id,
                system_prompt_override=system_prompt,
            )
            return _map_thread(thread)
        except KeyNotFoundException as ex:
            await context.abort(grpc.StatusCode.NOT_FOUND, str(ex))
        except Exception as ex:
            logger.error("Error creating agent thread: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def GetThread(
        self,
        request: agent_pb2.GetAgentThreadRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.AgentThreadResponse:
        user_id = await get_user_id(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Thread ID format")

        try:
            thread = await self._thread_service.get_thread(user_id=user_id, thread_id=thread_id)
            if not thread:
                await context.abort(grpc.StatusCode.NOT_FOUND, "Thread not found")
            return _map_thread(thread)
        except grpc.aio.AioRpcError:
            raise
        except Exception as ex:
            logger.error("Error getting agent thread: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def ListMessages(
        self,
        request: agent_pb2.ListAgentMessagesRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.ListAgentMessagesResponse:
        user_id = await get_user_id(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Thread ID format")

        before_id = None
        if request.HasField("before") and request.before.value:
            try:
                before_id = uuid.UUID(request.before.value)
            except Exception:
                await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Before ID format")

        limit = request.limit if request.limit > 0 else 100

        try:
            messages, next_before = await self._thread_service.list_messages(
                user_id=user_id,
                thread_id=thread_id,
                limit=limit,
                before_message_id=before_id,
            )
            resp = agent_pb2.ListAgentMessagesResponse(
                items=[_map_message_item(m) for m in messages]
            )
            if next_before:
                resp.next_before.CopyFrom(wrappers_pb2.StringValue(value=next_before))
            return resp
        except Exception as ex:
            logger.error("Error listing agent messages: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def CreateRun(
        self,
        request: agent_pb2.CreateAgentRunRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.CreateAgentRunResponse:
        user_id = await get_user_id(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
            project_id = uuid.UUID(request.project_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid UUID format")

        u_msg = {
            "id": request.user_message.id.value if request.user_message.HasField("id") else None,
            "role": request.user_message.role,
            "content": request.user_message.content,
            "metadata_json": request.user_message.metadata_json.value if request.user_message.HasField("metadata_json") else None,
        }
        a_msg = {
            "id": request.assistant_message.id.value if request.assistant_message.HasField("id") else None,
            "role": request.assistant_message.role,
            "content": request.assistant_message.content,
            "metadata_json": request.assistant_message.metadata_json.value if request.assistant_message.HasField("metadata_json") else None,
        }
        d_dec = {
            "allowed": request.domain_decision.allowed,
            "category": request.domain_decision.category,
            "reason": request.domain_decision.reason.value if request.domain_decision.HasField("reason") else None,
        }
        tool_calls = [
            {
                "tool_name": tc.tool_name,
                "input_json": tc.input_json,
                "output_json": tc.output_json,
                "status": tc.status,
            }
            for tc in request.tool_calls
        ]
        model = request.model.value if request.HasField("model") else None

        try:
            result = await self._thread_service.create_run(
                user_id=user_id,
                thread_id=thread_id,
                project_id=project_id,
                user_message_dict=u_msg,
                assistant_message_dict=a_msg,
                domain_decision_dict=d_dec,
                tool_calls_list=tool_calls,
                model=model,
            )
            if not result:
                await context.abort(grpc.StatusCode.NOT_FOUND, "Thread not found")
            return _map_run_response(result)
        except ValueError as ex:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(ex))
        except Exception as ex:
            logger.error("Error creating agent run: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def ExecuteRun(
        self,
        request: agent_pb2.ExecuteAgentRunRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.CreateAgentRunResponse:
        user_id = await get_user_id(context)
        roles = get_roles(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
            project_id = uuid.UUID(request.project_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid UUID format")

        source_lang = request.source_lang.value if request.HasField("source_lang") else None
        target_lang = request.target_lang.value if request.HasField("target_lang") else None
        first_deck_id = request.first_deck_id.value if request.HasField("first_deck_id") else None

        try:
            result = await self._orchestrator.execute_run(
                user_id=user_id,
                thread_id=thread_id,
                project_id=project_id,
                user_text=request.user_text,
                source_lang=source_lang,
                target_lang=target_lang,
                first_deck_id=first_deck_id,
                is_initial_greeting=request.is_initial_greeting,
                roles=roles,
            )
            if not result:
                await context.abort(grpc.StatusCode.NOT_FOUND, "Thread not found")
            return _map_run_response(result)
        except KeyNotFoundException as ex:
            await context.abort(grpc.StatusCode.NOT_FOUND, str(ex))
        except ValueError as ex:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(ex))
        except Exception as ex:
            logger.error("Error executing agent run: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def ExecuteRunStream(
        self,
        request: agent_pb2.ExecuteAgentRunRequest,
        context: grpc.ServicerContext,
    ):
        user_id = await get_user_id(context)
        roles = get_roles(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
            project_id = uuid.UUID(request.project_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid UUID format")

        source_lang = request.source_lang.value if request.HasField("source_lang") else None
        target_lang = request.target_lang.value if request.HasField("target_lang") else None
        first_deck_id = request.first_deck_id.value if request.HasField("first_deck_id") else None

        try:
            async for evt in self._orchestrator.execute_run_stream(
                user_id=user_id,
                thread_id=thread_id,
                project_id=project_id,
                user_text=request.user_text,
                source_lang=source_lang,
                target_lang=target_lang,
                first_deck_id=first_deck_id,
                is_initial_greeting=request.is_initial_greeting,
                roles=roles,
            ):
                if evt.content_chunk is not None:
                    yield agent_pb2.ExecuteAgentRunStreamResponse(content_chunk=evt.content_chunk)
                elif evt.tool_call is not None:
                    yield agent_pb2.ExecuteAgentRunStreamResponse(
                        tool_call=agent_pb2.AgentToolCallInput(
                            tool_name=evt.tool_call.get("tool_name", ""),
                            input_json=evt.tool_call.get("input_json", "{}"),
                            output_json=evt.tool_call.get("output_json", "{}"),
                            status=evt.tool_call.get("status", "completed"),
                        )
                    )
                elif evt.final_result is not None:
                    yield agent_pb2.ExecuteAgentRunStreamResponse(
                        final_result=_map_run_response(evt.final_result)
                    )
                elif evt.error is not None:
                    yield agent_pb2.ExecuteAgentRunStreamResponse(error=evt.error)

        except KeyNotFoundException as ex:
            await context.abort(grpc.StatusCode.NOT_FOUND, str(ex))
        except ValueError as ex:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, str(ex))
        except Exception as ex:
            logger.error("Error executing agent run stream: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def ArchiveThread(
        self,
        request: agent_pb2.ArchiveAgentThreadRequest,
        context: grpc.ServicerContext,
    ) -> empty_pb2.Empty:
        user_id = await get_user_id(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Thread ID format")

        try:
            archived = await self._thread_service.archive_thread(user_id=user_id, thread_id=thread_id)
            if not archived:
                await context.abort(grpc.StatusCode.NOT_FOUND, "Thread not found")
            return empty_pb2.Empty()
        except Exception as ex:
            logger.error("Error archiving agent thread: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def CreateArtifact(
        self,
        request: agent_pb2.CreateAgentArtifactRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.AgentArtifactItem:
        user_id = await get_user_id(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
            run_id = uuid.UUID(request.run_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid UUID format")

        try:
            artifact = await self._thread_service.create_artifact(
                user_id=user_id,
                thread_id=thread_id,
                run_id=run_id,
                kind=request.kind,
                payload_json=request.payload_json,
            )
            if not artifact:
                await context.abort(grpc.StatusCode.NOT_FOUND, "Thread or run not found")
            return _map_artifact_item(artifact)
        except Exception as ex:
            logger.error("Error creating agent artifact: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")

    async def ListArtifacts(
        self,
        request: agent_pb2.ListAgentArtifactsRequest,
        context: grpc.ServicerContext,
    ) -> agent_pb2.ListAgentArtifactsResponse:
        user_id = await get_user_id(context)

        try:
            thread_id = uuid.UUID(request.thread_id)
        except Exception:
            await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Thread ID format")

        run_id = None
        if request.HasField("run_id") and request.run_id.value:
            try:
                run_id = uuid.UUID(request.run_id.value)
            except Exception:
                await context.abort(grpc.StatusCode.INVALID_ARGUMENT, "Invalid Run ID format")

        try:
            artifacts = await self._thread_service.list_artifacts(
                user_id=user_id,
                thread_id=thread_id,
                run_id=run_id,
            )
            return agent_pb2.ListAgentArtifactsResponse(items=[_map_artifact_item(a) for a in artifacts])
        except Exception as ex:
            logger.error("Error listing agent artifacts: %s", ex, exc_info=True)
            await context.abort(grpc.StatusCode.INTERNAL, "Internal server error")
