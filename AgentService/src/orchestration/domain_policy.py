"""Domain policy classification and out-of-scope refusals."""

import re
from dataclasses import dataclass
from enum import Enum


class AgentDomainCategory(str, Enum):
    LANGUAGE_LEARNING = "language_learning"
    PRODUCT_NAVIGATION = "product_navigation"
    PROGRESS = "progress"
    OUT_OF_SCOPE = "out_of_scope"


@dataclass
class AgentDomainDecision:
    allowed: bool
    category: AgentDomainCategory
    reason: str | None = None

    @property
    def category_name(self) -> str:
        return self.category.value


class AgentDomainPolicy:
    REFUSAL_SUGGESTED_PROMPTS = [
        "Translate this sentence",
        "Explain vocabulary from this text",
        'Create a flashcard for "memory"',
    ]

    _LEARNING_MATERIAL_OVERRIDE = re.compile(
        r"\b(translate|vocabulary|words?|terms?|cards?|explain|meaning|grammar|learn)\b.*\b(from|in)\b.*\b(this|the)\b"
        r"|\b(from|in)\b.*\b(this|the)\b.*\b(snippet|paragraph|text|code|error|message|comment|sentence)\b"
        r"|\bwhat does\b.*\bmean\b.*\b(in|from)\b",
        re.IGNORECASE,
    )

    _HARD_OUT_OF_SCOPE = re.compile(
        r"\b(write|implement|build|create|generate|make|code|program|debug|fix)\b.*\b(code|script|function|class|app|program|algorithm|api|backend|frontend)\b"
        r"|\b(leetcode|homework solution|business plan|legal advice|medical advice)\b"
        r"|\b(binary search|sort algorithm|machine learning model)\b"
        r"|\bнапиши\s+код\b|\bнапиши\s+программ|\bреализуй\b.*\b(код|алгоритм|функци)",
        re.IGNORECASE,
    )

    _LANGUAGE_LEARNING_SIGNALS = re.compile(
        r"\b(translate|translation|vocabulary|grammar|pronunciation|conjugat|tense|phrase|idiom|fluency|flashcard|sentence|word|phrase|language|english|russian|korean|german|french|spanish|japanese|chinese|learn|study|meaning|usage|difference between|how do (?:i|you) say|speak|read|write in|hi|hello|hey|greetings)\b"
        r"|\b(cefr|a1|a2|b1|b2|c1|c2)\b"
        r"|\b(слово|фраза|перевед|граммат|произнош|изуч|язык|значени|привет|здравствуй|хай|добрый день|доброе утро|добрый вечер|здравствуйте|колод[а-я]*|карточ[а-я]*)\b",
        re.IGNORECASE,
    )

    @classmethod
    def classify(cls, user_text: str) -> AgentDomainDecision:
        text = user_text.strip()
        if not text:
            return AgentDomainDecision(allowed=False, category=AgentDomainCategory.OUT_OF_SCOPE, reason="empty")

        if cls._LEARNING_MATERIAL_OVERRIDE.search(text):
            return AgentDomainDecision(allowed=True, category=AgentDomainCategory.LANGUAGE_LEARNING)

        if cls._HARD_OUT_OF_SCOPE.search(text):
            return AgentDomainDecision(
                allowed=False,
                category=AgentDomainCategory.OUT_OF_SCOPE,
                reason="general_programming_or_non_learning_task",
            )

        if cls._LANGUAGE_LEARNING_SIGNALS.search(text):
            return AgentDomainDecision(allowed=True, category=AgentDomainCategory.LANGUAGE_LEARNING)

        return AgentDomainDecision(
            allowed=False,
            category=AgentDomainCategory.OUT_OF_SCOPE,
            reason="not_language_learning",
        )

    @classmethod
    def build_out_of_scope_refusal(cls, user_text: str, source_lang_label: str = "your target language") -> str:
        mentions_code = bool(
            re.search(r"c#|csharp|python|javascript|typescript|java", user_text, re.IGNORECASE)
            or re.search(r"\bcode\b", user_text, re.IGNORECASE)
            or "код" in user_text.lower()
            or cls._HARD_OUT_OF_SCOPE.search(user_text)
        )

        if mentions_code:
            return (
                f"I can't write or implement code here. PolyGuide is for language learning in {source_lang_label}.\n\n"
                "Try one of these instead:\n"
                "• Translate comments or error messages from the snippet\n"
                '• Explain vocabulary like "class", "method", or "Console.WriteLine"\n'
                "• Create flashcards from terms in the text"
            )

        return (
            "I can only help with language learning in Polyraspad — vocabulary, grammar, reading, cards, study, and progress.\n\n"
            "Try asking me to explain a word, translate a sentence, draft a card, or open Reader / Study."
        )
