"""Pytest shared fixtures and configuration."""

import uuid
import pytest_asyncio
from sqlalchemy import event
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker, create_async_engine
from src.db.session import init_db

USER_A = uuid.UUID("11111111-1111-1111-1111-111111111111")
USER_B = uuid.UUID("22222222-2222-2222-2222-222222222222")
PROJECT_ID = uuid.UUID("550e8400-e29b-41d4-a716-446655440000")


class MockProjectAccessValidator:
    async def ensure_project_access(self, user_id, project_id, roles):
        return type("Project", (), {
            "id": str(project_id),
            "user_id": str(user_id),
            "title": "English",
            "source_lang": "en",
            "target_lang": "ru",
        })()


class MockVocabularyClient:
    async def create_deck(self, user_id, project_id, title, description, roles):
        return type("Deck", (), {"id": str(uuid.uuid4()), "title": title})()

    async def create_card(self, user_id, deck_id, word, translation, expression, roles):
        return type("Card", (), {"id": str(uuid.uuid4())})()

    async def get_vocabulary_stats(self, user_id, project_id, roles):
        return type("Stats", (), {
            "total_lemmas": 150,
            "mature_count": 80,
            "learning_count": 50,
            "new_count": 20,
        })()

    async def get_leech_cards(self, user_id, project_id, roles):
        return type("Leeches", (), {
            "total_count": 1,
            "items": [
                type("CardItem", (), {
                    "id": str(uuid.uuid4()),
                    "srs_status": "LEARNING",
                    "note": type("Note", (), {
                        "field_values": {
                            "Word": type("Val", (), {"string_value": "tenacious"})(),
                            "Translation": type("Val", (), {"string_value": "упорный"})(),
                        }
                    })(),
                })()
            ],
        })()

    async def complete_lesson(self, user_id, lesson_id, roles):
        pass

    async def set_placement_level(self, user_id, cefr_level, roles):
        pass

    async def get_learning_terms(self, user_id, project_id, count, roles):
        return [type("Term", (), {"id": str(uuid.uuid4()), "text": "ubiquitous"})()]

    async def submit_knowledge_check_result(self, *args, **kwargs):
        pass

    async def get_skill_assessment_history(self, user_id, project_id, limit, roles):
        return type("History", (), {"logs": []})()

    async def get_daily_plan(self, user_id, project_id, roles):
        return type("Plan", (), {
            "tasks": [
                type("Task", (), {
                    "task_type": "fsrs",
                    "title": "Review 15 cards",
                    "description": "FSRS daily review",
                    "duration_minutes": 5,
                    "action_url": "/study",
                })()
            ]
        })()


@pytest_asyncio.fixture
async def async_db():
    engine = create_async_engine("sqlite+aiosqlite:///:memory:", echo=False)

    @event.listens_for(engine.sync_engine, "connect")
    def _sqlite_connect(dbapi_connection, connection_record):
        try:
            cursor = dbapi_connection.cursor()
            cursor.execute("ATTACH DATABASE ':memory:' AS internal")
            cursor.close()
        except Exception:
            pass

    await init_db(engine)
    session_factory = async_sessionmaker(engine, class_=AsyncSession, expire_on_commit=False)
    yield session_factory
    await engine.dispose()
