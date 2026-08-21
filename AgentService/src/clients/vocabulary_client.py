"""Async gRPC client for VocabularyService."""

import uuid
from dataclasses import dataclass
from typing import Sequence
import grpc
from src.config import _normalize_grpc_address, settings
from src.proto import vocabulary_pb2, vocabulary_pb2_grpc


@dataclass
class LearningTermDto:
    id: str
    text: str


class VocabularyGrpcClient:
    def __init__(self, address: str | None = None):
        self._address = _normalize_grpc_address(address or settings.VOCABULARY_GRPC_ADDRESS, "vocabulary-service:5117")
        self._channel = grpc.aio.insecure_channel(self._address)
        self._content_stub = vocabulary_pb2_grpc.ContentServiceStub(self._channel)
        self._card_stub = vocabulary_pb2_grpc.CardServiceStub(self._channel)
        self._analytics_stub = vocabulary_pb2_grpc.AnalyticsServiceStub(self._channel)
        self._ai_stub = vocabulary_pb2_grpc.AIServiceStub(self._channel)
        self._lesson_stub = vocabulary_pb2_grpc.LessonServiceStub(self._channel)
        self._term_stub = vocabulary_pb2_grpc.TermServiceStub(self._channel)

    async def close(self) -> None:
        """Gracefully close the underlying gRPC channel."""
        await self._channel.close()

    def _build_metadata(self, user_id: uuid.UUID | str, roles: Sequence[str]) -> tuple[tuple[str, str], ...]:
        return (
            ("user_id", str(user_id)),
            ("roles", ",".join(roles)),
        )

    async def get_vocabulary_stats(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> vocabulary_pb2.GetVocabularyStatsResponse:
        req = vocabulary_pb2.GetVocabularyStatsRequest(
            user_id=str(user_id),
            project_id=str(project_id),
        )
        return await self._analytics_stub.GetVocabularyStats(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def get_daily_summary(
        self,
        user_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> vocabulary_pb2.GetDailySummaryResponse:
        req = vocabulary_pb2.GetDailySummaryRequest(user_id=str(user_id))
        return await self._analytics_stub.GetDailySummary(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def get_daily_plan(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> vocabulary_pb2.GetDailyAutopilotPlanResponse:
        req = vocabulary_pb2.GetDailyAutopilotPlanRequest(
            user_id=str(user_id),
            project_id=str(project_id),
        )
        return await self._analytics_stub.GetDailyAutopilotPlan(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def get_leech_cards(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> vocabulary_pb2.GetLeechCardsResponse:
        req = vocabulary_pb2.GetLeechCardsRequest(
            user_id=str(user_id),
            project_id=str(project_id),
            page_size=20,
            page_number=1,
        )
        return await self._card_stub.GetLeechCards(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def create_deck(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        title: str,
        description: str | None,
        roles: Sequence[str],
    ) -> vocabulary_pb2.DeckResponse:
        req = vocabulary_pb2.CreateDeckRequest(
            user_id=str(user_id),
            project_id=str(project_id),
            title=title,
            description=description or "",
            is_public=False,
        )
        return await self._content_stub.CreateDeck(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def create_card(
        self,
        user_id: uuid.UUID | str,
        deck_id: uuid.UUID | str,
        word: str,
        translation: str,
        expression: str | None,
        roles: Sequence[str],
    ) -> vocabulary_pb2.CardResponse:
        field_values = {
            "Word": vocabulary_pb2.NoteFieldValuePayload(string_value=word),
            "Translation": vocabulary_pb2.NoteFieldValuePayload(string_value=translation),
        }
        if expression and expression.strip():
            field_values["Expression"] = vocabulary_pb2.NoteFieldValuePayload(string_value=expression.strip())

        req = vocabulary_pb2.CreateCardRequest(
            user_id=str(user_id),
            deck_id=str(deck_id),
            field_values=field_values,
        )
        return await self._card_stub.CreateCard(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def get_deck_tree(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> vocabulary_pb2.GetDeckTreeResponse:
        req = vocabulary_pb2.GetDeckTreeRequest(
            user_id=str(user_id),
            project_id=str(project_id),
        )
        return await self._content_stub.GetDeckTree(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def complete_lesson(
        self,
        user_id: uuid.UUID | str,
        lesson_id: uuid.UUID | str,
        roles: Sequence[str],
    ) -> None:
        req = vocabulary_pb2.CompleteLessonRequest(
            user_id=str(user_id),
            lesson_id=str(lesson_id),
        )
        await self._lesson_stub.CompleteLesson(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def set_placement_level(
        self,
        user_id: uuid.UUID | str,
        cefr_level: str,
        roles: Sequence[str],
    ) -> None:
        req = vocabulary_pb2.SetPlacementLevelRequest(
            user_id=str(user_id),
            cefr_level=cefr_level,
        )
        await self._lesson_stub.SetPlacementLevel(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def get_learning_terms(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        count: int,
        roles: Sequence[str],
    ) -> list[LearningTermDto]:
        req = vocabulary_pb2.ListProjectTermsRequest(
            user_id=str(user_id),
            project_id=str(project_id),
            status="SAVED",
            page_size=count,
        )
        resp = await self._term_stub.ListProjectTerms(
            req,
            metadata=self._build_metadata(user_id, roles),
        )
        return [LearningTermDto(id=item.term_id, text=item.text) for item in resp.items]

    async def submit_knowledge_check_result(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        term_ids: list[str],
        reading_score: int,
        listening_score: int,
        writing_score: int,
        speaking_score: int,
        roles: Sequence[str],
    ) -> None:
        req = vocabulary_pb2.SubmitKnowledgeCheckResultRequest(
            user_id=str(user_id),
            project_id=str(project_id),
            term_ids=term_ids,
            reading_score=reading_score,
            listening_score=listening_score,
            writing_score=writing_score,
            speaking_score=speaking_score,
        )
        await self._lesson_stub.SubmitKnowledgeCheckResult(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def get_skill_assessment_history(
        self,
        user_id: uuid.UUID | str,
        project_id: uuid.UUID | str,
        limit: int,
        roles: Sequence[str],
    ) -> vocabulary_pb2.GetSkillAssessmentHistoryResponse:
        req = vocabulary_pb2.GetSkillAssessmentHistoryRequest(
            user_id=str(user_id),
            project_id=str(project_id),
            limit=limit,
        )
        return await self._analytics_stub.GetSkillAssessmentHistory(
            req,
            metadata=self._build_metadata(user_id, roles),
        )

    async def close(self) -> None:
        await self._channel.close()
