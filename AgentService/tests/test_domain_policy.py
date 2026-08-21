"""Tests for AgentDomainPolicy and IntentRouter."""

import pytest
from src.orchestration.domain_policy import AgentDomainCategory, AgentDomainPolicy
from src.orchestration.intent_router import (
    AgentIntentRouter,
    AgentNavigateDestination,
    AgentToolId,
)


def test_classify_hard_out_of_scope():
    queries = [
        "Write me Python homework please",
        "Implement a binary search tree in C#",
        "напиши код на python",
        "leetcode two sum solution",
        "give me legal advice for my contract",
    ]
    for q in queries:
        decision = AgentDomainPolicy.classify(q)
        assert not decision.allowed, f"Expected {q} to be out of scope"
        assert decision.category == AgentDomainCategory.OUT_OF_SCOPE


def test_classify_learning_material_override():
    queries = [
        "Translate the comments from this code",
        "Explain vocabulary from this text",
        "What does 'override' mean in this sentence?",
    ]
    for q in queries:
        decision = AgentDomainPolicy.classify(q)
        assert decision.allowed, f"Expected {q} to be allowed"
        assert decision.category == AgentDomainCategory.LANGUAGE_LEARNING


def test_classify_language_learning_signals():
    queries = [
        "How do I say hello in Korean?",
        "What is the difference between ser and estar in Spanish?",
        "Can you explain the past tense of 'run'?",
        "Привет, как дела?",
        "Создай карточку для слова apple",
    ]
    for q in queries:
        decision = AgentDomainPolicy.classify(q)
        assert decision.allowed, f"Expected {q} to be allowed"
        assert decision.category == AgentDomainCategory.LANGUAGE_LEARNING


def test_refusal_message_generation():
    refusal_code = AgentDomainPolicy.build_out_of_scope_refusal("write python script", "English")
    assert "I can't write or implement code here" in refusal_code
    assert "English" in refusal_code

    refusal_general = AgentDomainPolicy.build_out_of_scope_refusal("stock market advice", "English")
    assert "I can only help with language learning in Polyraspad" in refusal_general


def test_intent_router_target_term_extraction():
    assert AgentIntentRouter.extract_target_term('Explain the word "serendipity"') == "serendipity"
    assert AgentIntentRouter.extract_target_term("Create a card for 'ephemeral'") == "ephemeral"
    assert AgentIntentRouter.extract_target_term("What does «добро пожаловать» mean?") == "добро пожаловать"


def test_intent_router_routing():
    assert AgentIntentRouter.route("open reader").tool_id == AgentToolId.NAVIGATE
    assert AgentIntentRouter.route("open reader").destination == AgentNavigateDestination.READER

    assert AgentIntentRouter.route("launch editor").tool_id == AgentToolId.NAVIGATE
    assert AgentIntentRouter.route("launch editor").destination == AgentNavigateDestination.EDITOR

    assert AgentIntentRouter.route("how am I doing this week?").tool_id == AgentToolId.GET_PROGRESS
    assert AgentIntentRouter.route("give me a sample sentence").tool_id == AgentToolId.GENERATE_EXAMPLE
    assert AgentIntentRouter.route("test me on my vocabulary").tool_id == AgentToolId.GENERATE_PRACTICE
    assert AgentIntentRouter.route("create a flashcard for 'dog'").tool_id == AgentToolId.BUILD_CARD_DRAFT
    assert AgentIntentRouter.route("что делаем сегодня?").tool_id == AgentToolId.GET_DAILY_PLAN
    assert AgentIntentRouter.route("__INIT__").domain.allowed is True
