# Введение

Agent Service не экспонирует REST — только **gRPC over HTTP/2** на порту 5131. Все thread operations **scoped to project_id** с проверкой доступа через VocabularyService.

## Контекст и проблема

AI-чат должен знать языковой контекст проекта и не давать доступ к чужим данным. Отдельный серvice с собственной БД изолирует agent persistence от Vocabulary domain tables.

## Принятое решение

1. Kestrel слушает только HTTP/2 (gRPC).
2. Каждый thread: `user_id` + `project_id`.
3. List/Create thread и ExecuteRun вызывают `EnsureProjectAccessAsync` (ContentService GetProjectDetails + metadata roles).
4. Aggregator проксирует REST → gRPC и передаёт identity metadata.

## Обоснование и последствия

### Плюсы

* Чёткая граница: agent state vs vocabulary content.
* Единый project access path с остальной платформой.

### Последствия

* Agent Service недоступен напрямую из browser — только через BFF.
* *Решение:* Aggregator AgentController как единственный public facade.
