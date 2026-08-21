"""Intent router for classified requests."""

import re
from dataclasses import dataclass
from enum import Enum
from src.orchestration.domain_policy import (
    AgentDomainCategory,
    AgentDomainDecision,
    AgentDomainPolicy,
)


class AgentToolId(str, Enum):
    EXPLAIN_WORD = "explain_word"
    GRAMMAR_HELP = "grammar_help"
    GENERATE_EXAMPLE = "generate_example"
    GENERATE_PRACTICE = "generate_practice"
    BUILD_CARD_DRAFT = "build_card_draft"
    GET_PROGRESS = "get_progress"
    GET_DAILY_PLAN = "get_daily_plan"
    NAVIGATE = "navigate"
    GENERAL_ANSWER = "general_answer"
    OUT_OF_SCOPE = "out_of_scope"


class AgentNavigateDestination(str, Enum):
    READER = "reader"
    EDITOR = "editor"
    STUDY = "study"
    VOCABULARY = "vocabulary"
    IMPORT = "import"
    LIBRARY = "library"
    DECKS = "decks"


@dataclass
class RoutedAgentIntent:
    tool_id: AgentToolId
    word: str | None = None
    sentence: str | None = None
    destination: AgentNavigateDestination | None = None
    domain: AgentDomainDecision | None = None

    @property
    def tool_name(self) -> str:
        return self.tool_id.value


class AgentIntentRouter:
    _QUOTED = re.compile(r'["\'«]([^"\'»]+)["\'»]')

    @classmethod
    def extract_target_term(cls, text: str) -> str | None:
        for match in cls._QUOTED.finditer(text):
            term = match.group(1).strip()
            if term:
                return term

        for_word = re.search(r"\b(?:word|phrase|term)\s+[\"']?([A-Za-zÀ-ÿ][\w\s'-]{0,40})", text, re.IGNORECASE)
        if for_word and for_word.group(1).strip():
            return for_word.group(1).strip()

        explain_match = re.search(
            r"\b(?:explain|define|meaning of|what does|what is)\s+(?:the\s+)?(?:word|phrase|term)?\s*[\"']?([A-Za-zÀ-ÿ][\w'-]{0,40})",
            text,
            re.IGNORECASE,
        )
        if explain_match and explain_match.group(1).strip():
            return explain_match.group(1).strip()

        card_match = re.search(
            r"\b(?:card|flashcard)\s+(?:for|about)\s+[\"']?([A-Za-zÀ-ÿ][\w\s'-]{0,40})",
            text,
            re.IGNORECASE,
        )
        if card_match and card_match.group(1).strip():
            return card_match.group(1).strip()

        return None

    @classmethod
    def extract_sentence_context(cls, text: str) -> str | None:
        in_context = re.search(r"\bin context(?: of)?[:\s]+(.+)", text, re.IGNORECASE)
        if in_context and in_context.group(1).strip():
            return in_context.group(1).strip()

        sentence_label = re.search(r"\bsentence[:\s]+(.+)", text, re.IGNORECASE)
        if sentence_label and sentence_label.group(1).strip():
            return sentence_label.group(1).strip()

        quoted = list(cls._QUOTED.finditer(text))
        if quoted:
            candidates = [
                m.group(1).strip()
                for m in quoted
                if m.group(1).strip() and len(m.group(1).strip().split()) > 1
            ]
            if candidates:
                return max(candidates, key=len)

        return None

    @classmethod
    def route(cls, user_text: str, is_initial_greeting: bool = False) -> RoutedAgentIntent:
        text = user_text.strip()
        lower = text.lower()

        if is_initial_greeting or text == "__INIT__":
            return RoutedAgentIntent(
                tool_id=AgentToolId.GENERAL_ANSWER,
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                    reason="initial_greeting",
                ),
            )

        nav = cls._match_navigation(lower)
        if nav is not None:
            return RoutedAgentIntent(
                tool_id=AgentToolId.NAVIGATE,
                word=cls.extract_target_term(text),
                destination=nav,
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.PRODUCT_NAVIGATION,
                ),
            )

        if re.search(r"\bhow am i\b|\bmy progress\b|\bthis week\b|\bstreak\b|\bstats\b|\bhow am i doing\b", lower):
            return RoutedAgentIntent(
                tool_id=AgentToolId.GET_PROGRESS,
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.PROGRESS,
                ),
            )

        if re.search(r"\bgrammar\b|\bwhy (?:is|does|was|did)\b|\bwhy .* used\b", lower):
            return RoutedAgentIntent(
                tool_id=AgentToolId.GRAMMAR_HELP,
                word=cls.extract_target_term(text),
                sentence=cls.extract_sentence_context(text),
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                ),
            )

        if re.search(r"\bexample\b|\bsample sentence\b|\buse (?:it|this) in a sentence\b", lower):
            return RoutedAgentIntent(
                tool_id=AgentToolId.GENERATE_EXAMPLE,
                word=cls.extract_target_term(text),
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                ),
            )

        if re.search(
            r"\btest me\b|\bpractice\b|\bgenerate practice\b|\bmake an exercise\b|\bупражнени[ея]\b|\bпотренир\b|\bсоставь предложени\b|\bdynamic lesson\b",
            lower,
        ):
            return RoutedAgentIntent(
                tool_id=AgentToolId.GENERATE_PRACTICE,
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                ),
            )

        if re.search(r"\bcreate\b.*\bcard\b|\bbuild\b.*\bcard\b|\bflashcard\b|\bmake a card\b|\bcards from\b", lower):
            return RoutedAgentIntent(
                tool_id=AgentToolId.BUILD_CARD_DRAFT,
                word=cls.extract_target_term(text),
                sentence=cls.extract_sentence_context(text),
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                ),
            )

        if re.search(r"\bexplain\b|\bwhat does\b|\bmeaning of\b|\bdefine\b|\bwhat is\b.*\bword\b", lower):
            return RoutedAgentIntent(
                tool_id=AgentToolId.EXPLAIN_WORD,
                word=cls.extract_target_term(text),
                sentence=cls.extract_sentence_context(text),
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                ),
            )

        if re.search(r"\b(начнём|начнем)\b|\bчто делаем сегодня\b|\b(start|begin)\b|\bwhat are we doing today\b", lower):
            return RoutedAgentIntent(
                tool_id=AgentToolId.GET_DAILY_PLAN,
                domain=AgentDomainDecision(
                    allowed=True,
                    category=AgentDomainCategory.LANGUAGE_LEARNING,
                ),
            )

        domain = AgentDomainPolicy.classify(text)
        if not domain.allowed:
            return RoutedAgentIntent(tool_id=AgentToolId.OUT_OF_SCOPE, domain=domain)

        return RoutedAgentIntent(tool_id=AgentToolId.GENERAL_ANSWER, domain=domain)

    @staticmethod
    def _match_navigation(lower: str) -> AgentNavigateDestination | None:
        if re.search(r"\b(open|go to|show|launch)\b.*\breader\b|\bread books\b", lower):
            return AgentNavigateDestination.READER
        if re.search(r"\b(open|go to|launch)\b.*\beditor\b|\bcreate card\b|\bmake a card\b", lower):
            return AgentNavigateDestination.EDITOR
        if re.search(r"\b(open|go to)\b.*\b(decks|my decks)\b", lower):
            return AgentNavigateDestination.DECKS
        if re.search(r"\b(open|go to)\b.*\blibrary\b|\bbooks\b", lower):
            return AgentNavigateDestination.LIBRARY
        if re.search(r"\b(open|go to|show)\b.*\bvocab|\bmy words\b|\bsaved words\b", lower):
            return AgentNavigateDestination.VOCABULARY
        if re.search(r"\b(open|go to)\b.*\bimport\b", lower):
            return AgentNavigateDestination.IMPORT
        if re.search(r"\bstart review\b|\bstudy now\b|\breview session\b|\bstart studying\b|\bstart a review\b", lower):
            return AgentNavigateDestination.STUDY
        return None

    @staticmethod
    def sanitize_lemma_labels(text: str) -> str:
        res = re.sub(r"^\s*lemma\s*[:：]\s*.+$", "", text, flags=re.IGNORECASE | re.MULTILINE)
        res = re.sub(r"\bLemma:\s*\S+", "", res, flags=re.IGNORECASE)
        res = res.replace("\n\n\n", "\n\n")
        return res.strip()
