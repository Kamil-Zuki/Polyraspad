"""Thread and message persistence service."""

import json
import logging
import uuid
from datetime import datetime, timedelta, timezone
from typing import Any, Sequence
from sqlalchemy import desc, select
from sqlalchemy.ext.asyncio import AsyncSession
from src.clients.access_validator import VocabularyProjectAccessValidator
from src.db.models import (
    AgentArtifact,
    AgentDomainDecision,
    AgentMessage,
    AgentRun,
    AgentThread,
    AgentToolCall,
)
from src.db.session import get_db
from src.orchestration.metadata_builder import AgentThreadTitleHelper

logger = logging.getLogger(__name__)

VALID_ROLES = {"user", "assistant", "system", "tool"}
VALID_CATEGORIES = {
    "language_learning",
    "product_navigation",
    "progress",
    "out_of_scope",
    "automation",
}
VALID_TOOL_STATUSES = {"completed", "failed"}


class AgentThreadService:
    def __init__(
        self,
        project_access_validator: VocabularyProjectAccessValidator | None = None,
        db_factory=None,
    ):
        self._project_access_validator = project_access_validator or VocabularyProjectAccessValidator()
        self._db_factory = db_factory

    async def list_threads(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
        agent_id: str | None = None,
    ) -> list[AgentThread]:
        u_id = uuid.UUID(str(user_id))
        p_id = uuid.UUID(str(project_id))

        await self._project_access_validator.ensure_project_access(u_id, p_id, roles)

        async with get_db(self._db_factory) as db:
            stmt = (
                select(AgentThread)
                .where(
                    AgentThread.user_id == u_id,
                    AgentThread.project_id == p_id,
                    AgentThread.archived_at.is_(None),
                )
                .order_by(desc(AgentThread.updated_at))
            )
            if agent_id and agent_id.strip():
                stmt = stmt.where(AgentThread.agent_id == agent_id.strip())

            result = await db.execute(stmt)
            return list(result.scalars().all())

    async def create_thread(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
        agent_id: str | None = None,
        system_prompt_override: str | None = None,
    ) -> AgentThread:
        u_id = uuid.UUID(str(user_id))
        p_id = uuid.UUID(str(project_id))

        await self._project_access_validator.ensure_project_access(u_id, p_id, roles)

        now = datetime.now(timezone.utc)
        thread = AgentThread(
            id=uuid.uuid4(),
            user_id=u_id,
            project_id=p_id,
            agent_id=agent_id.strip() if agent_id and agent_id.strip() else None,
            system_prompt_override=system_prompt_override.strip() if system_prompt_override and system_prompt_override.strip() else None,
            created_at=now,
            updated_at=now,
        )

        async with get_db(self._db_factory) as db:
            db.add(thread)
            await db.commit()
            await db.refresh(thread)
            return thread

    async def get_thread(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
    ) -> AgentThread | None:
        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))

        async with get_db(self._db_factory) as db:
            stmt = select(AgentThread).where(
                AgentThread.id == t_id,
                AgentThread.user_id == u_id,
            )
            result = await db.execute(stmt)
            return result.scalar_one_or_none()

    async def list_messages(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
        limit: int = 100,
        before_message_id: uuid.UUID | str | None = None,
    ) -> tuple[list[AgentMessage], str | None]:
        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))

        limit = max(1, min(limit, 100))

        async with get_db(self._db_factory) as db:
            # Check ownership
            thread_check = await db.execute(
                select(AgentThread.id).where(AgentThread.id == t_id, AgentThread.user_id == u_id)
            )
            if not thread_check.scalar_one_or_none():
                return [], None

            before_created_at = None
            if before_message_id:
                b_id = uuid.UUID(str(before_message_id))
                msg_check = await db.execute(
                    select(AgentMessage.created_at).where(
                        AgentMessage.id == b_id,
                        AgentMessage.thread_id == t_id,
                    )
                )
                before_created_at = msg_check.scalar_one_or_none()
                if before_created_at is None:
                    return [], None

            stmt = select(AgentMessage).where(AgentMessage.thread_id == t_id)
            if before_created_at is not None:
                stmt = stmt.where(AgentMessage.created_at < before_created_at)

            stmt = stmt.order_by(desc(AgentMessage.created_at)).limit(limit + 1)
            result = await db.execute(stmt)
            messages = list(result.scalars().all())

            next_before = None
            if len(messages) > limit:
                next_before = str(messages[limit].id)
                messages = messages[:limit]

            # Return in ascending chronological order
            messages.reverse()
            return messages, next_before

    async def create_run(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        user_message_dict: dict[str, Any],
        assistant_message_dict: dict[str, Any],
        domain_decision_dict: dict[str, Any],
        tool_calls_list: list[dict[str, Any]],
        model: str | None = None,
    ) -> dict[str, Any] | None:
        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))
        p_id = uuid.UUID(str(project_id))

        user_role = user_message_dict.get("role", "user").lower()
        assistant_role = assistant_message_dict.get("role", "assistant").lower()
        user_content = user_message_dict.get("content", "").strip()
        assistant_content = assistant_message_dict.get("content", "").strip() or "OK"

        if user_role not in VALID_ROLES:
            raise ValueError(f"Invalid user message role: {user_role}")
        if assistant_role not in VALID_ROLES:
            raise ValueError(f"Invalid assistant message role: {assistant_role}")
        if not user_content:
            raise ValueError("User message content is required")

        category = domain_decision_dict.get("category", "language_learning").lower()
        if category not in VALID_CATEGORIES:
            raise ValueError(f"Invalid domain decision category: {category}")

        for tc in tool_calls_list:
            t_name = tc.get("tool_name") or tc.get("name")
            if not t_name:
                raise ValueError("Tool name is required")
            t_status = (tc.get("status") or "completed").lower()
            if t_status not in VALID_TOOL_STATUSES:
                raise ValueError(f"Invalid tool call status: {t_status}")

        async with get_db(self._db_factory) as db:
            thread_stmt = select(AgentThread).where(
                AgentThread.id == t_id,
                AgentThread.user_id == u_id,
            )
            res = await db.execute(thread_stmt)
            thread = res.scalar_one_or_none()

            if not thread or thread.project_id != p_id:
                return None

            if thread.archived_at is not None:
                raise ValueError("Cannot create run on archived thread")

            now = datetime.now(timezone.utc)
            if not thread.title:
                thread.title = AgentThreadTitleHelper.derive_title(user_content)
            thread.updated_at = now

            u_msg_id = uuid.UUID(str(user_message_dict["id"])) if user_message_dict.get("id") else uuid.uuid4()
            a_msg_id = uuid.UUID(str(assistant_message_dict["id"])) if assistant_message_dict.get("id") else uuid.uuid4()

            user_msg = AgentMessage(
                id=u_msg_id,
                thread_id=t_id,
                role=user_role,
                content=user_content,
                metadata_json=AgentThreadTitleHelper.normalize_metadata_json(user_message_dict.get("metadata_json")),
                created_at=now,
            )

            assistant_msg = AgentMessage(
                id=a_msg_id,
                thread_id=t_id,
                role=assistant_role,
                content=assistant_content,
                metadata_json=AgentThreadTitleHelper.normalize_metadata_json(assistant_message_dict.get("metadata_json")),
                created_at=now + timedelta(milliseconds=1),
            )

            run = AgentRun(
                id=uuid.uuid4(),
                thread_id=t_id,
                status="completed",
                model=model,
                started_at=now,
                completed_at=now,
            )

            domain_dec = AgentDomainDecision(
                id=uuid.uuid4(),
                run_id=run.id,
                allowed=bool(domain_decision_dict.get("allowed", True)),
                category=category,
                reason=domain_decision_dict.get("reason"),
                user_text_preview=AgentThreadTitleHelper.build_user_text_preview(user_content),
                created_at=now,
            )

            db.add_all([user_msg, assistant_msg, run, domain_dec])

            for tc in tool_calls_list:
                t_name = tc.get("tool_name") or tc.get("name")
                input_j = tc.get("input_json") or tc.get("input") or "{}"
                output_j = tc.get("output_json") or tc.get("result") or "{}"
                t_status = (tc.get("status") or "completed").lower()

                tool_call = AgentToolCall(
                    id=uuid.uuid4(),
                    run_id=run.id,
                    tool_name=t_name,
                    input_json=AgentThreadTitleHelper.normalize_metadata_json(input_j),
                    output_json=AgentThreadTitleHelper.normalize_metadata_json(output_j),
                    status=t_status,
                    created_at=now,
                )
                db.add(tool_call)

            await db.commit()
            await db.refresh(run)
            await db.refresh(user_msg)
            await db.refresh(assistant_msg)

            return {
                "run": run,
                "user_message": user_msg,
                "assistant_message": assistant_msg,
            }

    async def archive_thread(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
    ) -> bool:
        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))

        async with get_db(self._db_factory) as db:
            stmt = select(AgentThread).where(AgentThread.id == t_id, AgentThread.user_id == u_id)
            res = await db.execute(stmt)
            thread = res.scalar_one_or_none()

            if not thread:
                return False

            if thread.archived_at is not None:
                return True

            now = datetime.now(timezone.utc)
            thread.archived_at = now
            thread.updated_at = now
            await db.commit()
            return True

    async def create_artifact(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
        run_id: uuid.UUID | str,
        kind: str,
        payload_json: str | dict,
    ) -> AgentArtifact | None:
        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))
        r_id = uuid.UUID(str(run_id))

        async with get_db(self._db_factory) as db:
            thread_check = await db.execute(
                select(AgentThread.id).where(AgentThread.id == t_id, AgentThread.user_id == u_id)
            )
            if not thread_check.scalar_one_or_none():
                return None

            run_check = await db.execute(
                select(AgentRun.id).where(AgentRun.id == r_id, AgentRun.thread_id == t_id)
            )
            if not run_check.scalar_one_or_none():
                return None

            artifact = AgentArtifact(
                id=uuid.uuid4(),
                run_id=r_id,
                thread_id=t_id,
                kind=kind,
                payload_json=AgentThreadTitleHelper.normalize_metadata_json(payload_json),
                created_at=datetime.now(timezone.utc),
            )
            db.add(artifact)
            await db.commit()
            await db.refresh(artifact)
            return artifact

    async def list_artifacts(
        self,
        user_id: uuid.UUID | str,
        thread_id: uuid.UUID | str,
        run_id: uuid.UUID | str | None = None,
    ) -> list[AgentArtifact]:
        u_id = uuid.UUID(str(user_id))
        t_id = uuid.UUID(str(thread_id))

        async with get_db(self._db_factory) as db:
            thread_check = await db.execute(
                select(AgentThread.id).where(AgentThread.id == t_id, AgentThread.user_id == u_id)
            )
            if not thread_check.scalar_one_or_none():
                return []

            stmt = select(AgentArtifact).where(AgentArtifact.thread_id == t_id)
            if run_id:
                r_id = uuid.UUID(str(run_id))
                stmt = stmt.where(AgentArtifact.run_id == r_id)

            stmt = stmt.order_by(desc(AgentArtifact.created_at))
            result = await db.execute(stmt)
            return list(result.scalars().all())
