# Введение

**AgentIntentRouter** — deterministic regex router от user text к `AgentToolId`.

## Контекст и проблема

ML-classifier добавляет latency, cost и нестабильность для narrow product intents (open Reader, explain word).

## Принятое решение

1. Fixed priority: navigation → progress → grammar → example → card → explain → general/out_of_scope.
2. `ExtractTargetTerm` — quoted strings и pattern captures.
3. `ToolName` snake_case для persistence в `agent_tool_calls`.

## Обоснование и последствия

### Плюсы

* Unit-testable; no external dependency.
* Прозрачное поведение для QA.

### Последствия

* Новые intents требуют code change.
* *Решение:* расширять Router + DomainPolicy вместе; document in SR-AGENT-INTENT-01.
