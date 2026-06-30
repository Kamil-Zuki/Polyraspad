# Введение

Agent следует **term-first** модели Polyraspad: обучение по exact forms; grammar/examples — через Vocabulary AIService где возможно.

## Контекст и проблема

LLM склонен выдавать lemmas и general answers; дублирование AI mining в Agent нарушает service boundaries.

## Принятое решение

1. Prompts явно запрещают «Lemma:» labels; post-process `SanitizeLemmaLabels`.
2. `grammar_help` / `generate_example` / `build_card_draft` → Vocabulary `AIService` gRPC.
3. `explain_word` / `general_answer` → local LLM с strict system rules.
4. Card draft actions передают exact `Word` surface form в metadata.

## Обоснование и последствия

### Плюсы

* Согласованность с Reader/Vocabulary term model.
* Переиспользование Vocabulary AI mining.

### Последствия

* Agent зависит от Vocabulary AIService availability для grammar/example paths.
* *Решение:* orchestrator catch → user-friendly error message; optional example in card draft best-effort.
