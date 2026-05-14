# product-agent Task

Plan ID: `02-reader-mvp-mining-2026-05-14`
Agent: `product-agent`
Status: done
Can run in parallel: yes

## Objective

Зафиксировать MVP шага 2: pop-up/инспектор слова, Mine с term-first; AI даёт контекстный перевод и подсказку-лемму только как метаданные.

## Deliverables

- Поведение согласовано с планом в `plans/active/02-reader-mvp-mining-2026-05-14.md`.

## Handoff

- Реализация: внешний OpenAI-compatible LLM через Aggregator `/api/ai/*`; reader использует `/api/ai/mining-draft` для контекстного черновика.
