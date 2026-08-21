# Введение

**ExecuteRun** — центральный use case: один gRPC call выполняет classification, routing, tool execution и persist.

## Контекст и проблема

Разделение «сгенерировать ответ» и «сохранить историю» усложняет клиент и ломает audit trail (domain decisions, tool calls).

## Принятое решение

1. `ExecuteAgentRunRequest` → `AgentOrchestrator.ExecuteRunAsync`.
2. Load project langs via access validator.
3. `AgentIntentRouter.Route` + domain gate for LLM tools.
4. `ExecuteToolAsync` switch by `AgentToolId`.
5. Build `CreateAgentRunDto` → `AgentThreadService.CreateRunAsync` (single DB transaction).

`CreateRun` gRPC остаётся для persist готового payload (advanced / future client orchestration).

## Обоснование и последствия

### Плюсы

* Атомарная история + audit.
* Единая точка для tool error handling.

### Последствия

* ExecuteRun latency = tool + LLM + DB; нет streaming в текущей реализации.
* *Решение:* UI loading state; timeout на HttpClient LLM.
