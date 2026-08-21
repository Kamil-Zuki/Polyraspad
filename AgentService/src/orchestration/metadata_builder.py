"""Metadata builder and helper functions for response formatting."""

import json
import re
from dataclasses import asdict, dataclass
from typing import Any
from src.orchestration.domain_policy import AgentDomainDecision
from src.orchestration.intent_router import RoutedAgentIntent


@dataclass
class AgentActionCard:
    id: str
    title: str
    kind: str
    href: str
    label: str
    description: str | None = None
    editor_draft: dict[str, str] | None = None

    def to_dict(self) -> dict[str, Any]:
        d: dict[str, Any] = {
            "id": self.id,
            "title": self.title,
            "kind": self.kind,
            "href": self.href,
            "label": self.label,
        }
        if self.description is not None:
            d["description"] = self.description
        if self.editor_draft is not None:
            d["editorDraft"] = self.editor_draft
        return d


@dataclass
class AgentToolCallRecord:
    tool_name: str
    input_json: str
    output_json: str
    status: str = "completed"


@dataclass
class AgentExecutionResult:
    assistant_content: str
    domain_decision: AgentDomainDecision
    tool_calls: list[AgentToolCallRecord]
    is_error: bool = False
    intent_category: str | None = None
    refusal: bool = False
    suggested_prompts: list[str] | None = None
    actions: list[AgentActionCard] | None = None


class AgentMessageMetadataBuilder:
    @staticmethod
    def build(result: AgentExecutionResult) -> str | None:
        metadata: dict[str, Any] = {}

        if result.actions and len(result.actions) > 0:
            metadata["actions"] = [a.to_dict() for a in result.actions]

        if result.is_error:
            metadata["isError"] = True

        if result.intent_category:
            metadata["intentCategory"] = result.intent_category

        if result.refusal:
            metadata["refusal"] = True

        if result.suggested_prompts and len(result.suggested_prompts) > 0:
            metadata["suggestedPrompts"] = result.suggested_prompts

        if result.tool_calls and len(result.tool_calls) > 0:
            metadata["toolCalls"] = [
                {
                    "name": tc.tool_name,
                    "status": tc.status,
                    "input": tc.input_json,
                    "result": tc.output_json,
                }
                for tc in result.tool_calls
            ]

        return json.dumps(metadata) if metadata else None

    @staticmethod
    def build_tool_call_record(
        intent: RoutedAgentIntent,
        user_text: str,
        result: AgentExecutionResult,
    ) -> AgentToolCallRecord:
        input_data = {
            "userText": user_text,
            "word": intent.word,
            "sentence": intent.sentence,
            "destination": intent.destination.value if intent.destination else None,
        }
        output_data = {
            "content": result.assistant_content,
            "actions": [a.to_dict() for a in result.actions] if result.actions else None,
            "isError": result.is_error,
            "intentCategory": result.intent_category or (result.domain_decision.category_name if result.domain_decision else None),
            "refusal": result.refusal,
            "suggestedPrompts": result.suggested_prompts,
        }
        return AgentToolCallRecord(
            tool_name=intent.tool_name,
            input_json=json.dumps(input_data),
            output_json=json.dumps(output_data),
            status="error" if result.is_error else "completed",
        )


class AgentThreadTitleHelper:
    DEFAULT_TITLE = "New conversation"
    MAX_TITLE_LENGTH = 60
    MAX_METADATA_BYTES = 32 * 1024
    _WHITESPACE_REGEX = re.compile(r"\s+")

    @classmethod
    def derive_title(cls, user_message_content: str | None) -> str:
        if not user_message_content or not user_message_content.strip():
            return cls.DEFAULT_TITLE

        normalized = cls._WHITESPACE_REGEX.sub(" ", user_message_content.strip())
        if len(normalized) <= cls.MAX_TITLE_LENGTH:
            return normalized

        return normalized[: cls.MAX_TITLE_LENGTH - 3] + "..."

    @classmethod
    def normalize_metadata_json(cls, metadata_json: str | dict | None) -> Any:
        if metadata_json is None:
            return None

        if isinstance(metadata_json, dict):
            return metadata_json

        if not isinstance(metadata_json, str) or not metadata_json.strip():
            return None

        raw = metadata_json.strip()
        if len(raw.encode("utf-8")) > cls.MAX_METADATA_BYTES:
            raw = raw[: cls.MAX_METADATA_BYTES]

        try:
            return json.loads(raw)
        except Exception:
            return raw

    @classmethod
    def build_user_text_preview(cls, content: str | None) -> str | None:
        if not content or not content.strip():
            return None

        normalized = cls._WHITESPACE_REGEX.sub(" ", content.strip())
        return normalized if len(normalized) <= 200 else normalized[:200]
