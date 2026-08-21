"""LangChain tool definitions for Polyraspad agent."""

import json
import uuid
from typing import Sequence
from langchain_core.tools import BaseTool, tool
from src.clients.vocabulary_client import VocabularyGrpcClient


def _build_plan_summary(plan) -> str:
    lines = []
    fsrs_task = next((t for t in plan.tasks if t.task_type == "fsrs"), None)
    lesson_task = next((t for t in plan.tasks if t.task_type == "lesson"), None)
    check_task = next((t for t in plan.tasks if t.task_type == "knowledge_check"), None)

    if fsrs_task:
        lines.append(f"Due flashcards: {fsrs_task.title} ({fsrs_task.duration_minutes} min)")
    if lesson_task:
        lines.append(f"Next lesson: {lesson_task.title} ({lesson_task.duration_minutes} min)")
    if check_task:
        lines.append(f"Skill focus: {check_task.title} — {check_task.description}")

    return "\n".join(lines) if lines else "No specific tasks for today. Encourage the learner to read or review vocabulary."


def create_agent_tools(
    vocabulary_client: VocabularyGrpcClient,
    user_id: uuid.UUID | str,
    project_id: uuid.UUID | str,
    roles: Sequence[str],
) -> list[BaseTool]:
    """Create and bind all 12 platform tools with ambient user/project context."""

    @tool
    async def create_deck(title: str, description: str = "") -> str:
        """Create a new deck for organizing vocabulary cards."""
        try:
            deck = await vocabulary_client.create_deck(
                user_id=user_id,
                project_id=project_id,
                title=title,
                description=description,
                roles=roles,
            )
            return json.dumps({"id": str(deck.id), "title": deck.title})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def create_card(
        deck_id: str,
        word: str,
        translation: str,
        expression: str = "",
    ) -> str:
        """Create a new flashcard in a deck."""
        try:
            target_deck_id = deck_id
            is_valid_guid = False
            try:
                if deck_id and uuid.UUID(deck_id) != uuid.UUID(int=0):
                    is_valid_guid = True
            except Exception:
                is_valid_guid = False

            if not is_valid_guid:
                tree = await vocabulary_client.get_deck_tree(user_id=user_id, project_id=project_id, roles=roles)
                if not tree.root_decks:
                    return json.dumps({"error": "No decks available in this project."})
                target_deck_id = tree.root_decks[0].id

            card = await vocabulary_client.create_card(
                user_id=user_id,
                deck_id=target_deck_id,
                word=word,
                translation=translation,
                expression=expression,
                roles=roles,
            )
            return json.dumps({"id": str(card.id)})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def get_user_vocabulary_stats() -> str:
        """Get the user's progress and vocabulary statistics."""
        try:
            vocab = await vocabulary_client.get_vocabulary_stats(user_id=user_id, project_id=project_id, roles=roles)
            return json.dumps({
                "totalLemmas": vocab.total_lemmas,
                "matureCount": vocab.mature_count,
                "learningCount": vocab.learning_count,
                "newCount": vocab.new_count,
            })
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def get_recent_leeches() -> str:
        """Get a list of problematic (leech) cards the user struggles with."""
        try:
            leeches = await vocabulary_client.get_leech_cards(user_id=user_id, project_id=project_id, roles=roles)
            mapped = []
            for c in leeches.items:
                word_val = c.note.field_values.get("Word").string_value if c.note and "Word" in c.note.field_values else "Unknown"
                trans_val = c.note.field_values.get("Translation").string_value if c.note and "Translation" in c.note.field_values else "Unknown"
                mapped.append({
                    "id": c.id,
                    "srsStatus": c.srs_status,
                    "word": word_val,
                    "translation": trans_val,
                })
            return json.dumps({"total": leeches.total_count, "cards": mapped})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def mark_lesson_completed(lesson_id: str) -> str:
        """Mark the current lesson as completed. ONLY call this when the user has fully finished the lesson activities according to your assessment."""
        try:
            try:
                comp_lesson_id = uuid.UUID(lesson_id)
            except Exception:
                return json.dumps({"error": "Invalid lesson_id format"})

            await vocabulary_client.complete_lesson(user_id=user_id, lesson_id=comp_lesson_id, roles=roles)
            return json.dumps({"status": "success", "message": "Lesson marked as completed successfully."})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def submit_knowledge_check(
        term_ids: list[str],
        reading_score: int = 0,
        listening_score: int = 0,
        writing_score: int = 0,
        speaking_score: int = 0,
    ) -> str:
        """Submit the results of an exam or knowledge check to update the user's skill levels. Use this tool ONLY at the end of a Knowledge Check lesson."""
        try:
            await vocabulary_client.submit_knowledge_check_result(
                user_id=user_id,
                project_id=project_id,
                term_ids=term_ids,
                reading_score=reading_score,
                listening_score=listening_score,
                writing_score=writing_score,
                speaking_score=speaking_score,
                roles=roles,
            )
            return json.dumps({"status": "success", "message": "Knowledge check results submitted successfully."})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def set_cefr_placement(cefr_level: str) -> str:
        """Set the user's CEFR level after a placement test. This unlocks curriculum lessons for them."""
        try:
            if not cefr_level or not cefr_level.strip():
                return json.dumps({"error": "cefr_level is required"})

            cleaned_level = cefr_level.strip().upper()
            await vocabulary_client.set_placement_level(user_id=user_id, cefr_level=cleaned_level, roles=roles)
            return json.dumps({"status": "success", "message": f"CEFR level set to {cleaned_level} successfully. All previous levels are unlocked."})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def get_daily_plan() -> str:
        """Get the user's personalized daily learning plan: due flashcard count, weakest skill, next curriculum lesson, and skill CEFR levels. Call this at the start of any conversation if you need context about the user's current state."""
        try:
            plan = await vocabulary_client.get_daily_plan(user_id=user_id, project_id=project_id, roles=roles)
            summary = _build_plan_summary(plan)
            tasks = [
                {
                    "taskType": t.task_type,
                    "title": t.title,
                    "description": t.description,
                    "durationMinutes": t.duration_minutes,
                    "actionUrl": t.action_url,
                }
                for t in plan.tasks
            ]
            return json.dumps({"summary": summary, "tasks": tasks})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def generate_writing_task() -> str:
        """Get a list of words the user is currently learning to generate a writing or translation task for them."""
        try:
            practice_terms = await vocabulary_client.get_learning_terms(user_id=user_id, project_id=project_id, count=7, roles=roles)
            return json.dumps({
                "instruction": "Generate a short writing task (e.g. write a 3-sentence story, or translate a specific phrase) that requires the user to use the following words. Do not give them the answer. When they reply, evaluate their use of these words and their grammar, then call submit_knowledge_check to record their writing score (0-100) for these specific term_ids.",
                "terms": [{"term_id": t.id, "text": t.text} for t in practice_terms],
            })
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    async def get_skill_assessment_history() -> str:
        """Get the history of the user's skill assessments (reading, listening, writing, speaking scores) to analyze trends and suggest focused practice."""
        try:
            history = await vocabulary_client.get_skill_assessment_history(user_id=user_id, project_id=project_id, limit=20, roles=roles)
            logs = []
            for l in history.logs:
                logs.append({
                    "skill": l.skill,
                    "score": l.score,
                    "date": l.created_at.ToJsonString() if hasattr(l.created_at, "ToJsonString") else str(l.created_at),
                })
            return json.dumps({"logs": logs})
        except Exception as e:
            return json.dumps({"error": str(e)})

    @tool
    def navigate(destination: str, label: str, description: str = "") -> str:
        """Navigate the user to a page in the app. Destination options: reader, editor, study, vocabulary, library, import."""
        return json.dumps({
            "actionType": "navigate",
            "destination": "/" + destination.strip().lstrip("/"),
            "label": label,
            "description": description or "",
        })

    @tool
    def open_editor_draft(
        word: str,
        expression: str = "",
        translation: str = "",
        label: str = "",
        description: str = "",
    ) -> str:
        """Open the card editor with a pre-filled draft."""
        return json.dumps({
            "actionType": "open_editor_draft",
            "destination": "/editor",
            "label": label if label else "Draft Card",
            "description": description if description else "Draft a new card in the editor",
            "payload": {
                "word": word,
                "expression": expression,
                "translation": translation,
            },
        })

    return [
        create_deck,
        create_card,
        get_user_vocabulary_stats,
        get_recent_leeches,
        mark_lesson_completed,
        submit_knowledge_check,
        set_cefr_placement,
        get_daily_plan,
        generate_writing_task,
        get_skill_assessment_history,
        navigate,
        open_editor_draft,
    ]
