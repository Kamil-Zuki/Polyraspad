# Введение

Настоящий документ содержит полное описание **gRPC** интерфейса микросервиса **Agent Service**. Это — **основной машинный контракт** сервиса: публичные **REST**-маршруты PolyGuide на **Aggregator Service** описаны отдельно и по смыслу маппятся на перечисленные ниже RPC, а не дублируют бизнес-логику оркестрации внутри BFF.

Agent Service принимает только gRPC (HTTP/2, порт `5131`). Вызывающая сторона — **Aggregator Service**, который передаёт идентификатор пользователя и роли через inbound metadata (`user_id`, `roles`) после JWT-валидации на периметре. Сервис хранит треды диалога, историю сообщений, аудит запусков (runs), domain decisions, tool calls и артефакты в PostgreSQL.

Источник истины proto: [[agent.proto]] (копия `AgentService/Protos/agent.proto`). C# namespace: `Pvs.Agent.Grpc`, package: `pvs.agent.grpc`.

# 1. Группы методов gRPC

Ниже представлена сводная таблица логических групп RPC, реализуемых сервисом `AgentService`.

| Группа | Описание |
| :--- | :--- |
| **Треды и сообщения (Threads)** | Жизненный цикл AI-треда в project scope и cursor-пагинация истории user/assistant сообщений. |
| **Запуски и оркестрация (Runs)** | Persist готового run (`CreateRun`) и server-side pipeline (`ExecuteRun`): domain policy → intent routing → learning/navigation tools → атомарное сохранение. |
| **Артефакты (Artifacts)** | Структурированные JSON-payload, привязанные к run и thread; create и list с optional filter по `run_id`. |
| **Платформенные контракты (Operations)** | gRPC-only Kestrel, health check и EF migrations — **не** отдельные RPC; см. замечание в разделе 5. |

# 2. Треды и сообщения (Threads)

Методы управления тредами и чтения истории сообщений. Все операции проверяют ownership (`thread.user_id == caller`) и для list/create — доступ к project через VocabularyService.

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AGENT-THREAD-01 | `ListThreads` | Unary | Активные (не archived) треды user+project, сортировка `updated_at` DESC. |
| SR-AGENT-THREAD-02 | `CreateThread` | Unary | Новый пустой тред после проверки доступа к project. |
| SR-AGENT-THREAD-03 | `GetThread` | Unary | Детали треда по id с проверкой ownership. |
| SR-AGENT-MSG-01 | `ListMessages` | Unary | До 100 сообщений; cursor `before` — id более старого сообщения. |
| SR-AGENT-THREAD-04 | `ArchiveThread` | Unary | Soft archive (`archived_at`); идempotent повтор. |

Детали: [[01 - Треды и сообщения (Threads)]].

# 3. Запуски и оркестрация (Runs)

Методы сохранения и выполнения agent runs. `ExecuteRun` инкапсулирует domain classification, intent routing и вызов learning/navigation tools; результат всегда persist через общий путь `CreateRun`.

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AGENT-RUN-01 | `CreateRun` | Unary | Атомарное сохранение user+assistant messages, run, domain decision, tool calls. |
| SR-AGENT-RUN-02 | `ExecuteRun` | Unary | Domain classify → intent route → tool execute → `CreateRun` persist. |

Детали: [[02 - Запуски и оркестрация (Runs)]].

# 4. Артефакты (Artifacts)

Structured payloads для UI и downstream-потребителей (card drafts, mining results и т.п.).

| Код требования | gRPC Метод | Тип RPC | Описание |
| :------------- | :--------- | :-----: | :------- |
| SR-AGENT-ART-01 | `CreateArtifact` | Unary | JSON payload, привязанный к run+thread; ownership check. |
| SR-AGENT-ART-02 | `ListArtifacts` | Unary | Список по thread; optional filter по `run_id`. |

Детали: [[03 - Артефакты (Artifacts)]].

# 5. Платформенные контракты (Operations)

Следующие SR **не** имеют отдельных RPC в `agent.proto`:

| Код требования | Механизм | Описание |
| :------------- | :------- | :------- |
| SR-AGENT-OPS-01 | HTTP `GET /healthz` | Liveness probe в том же Kestrel-процессе (не gRPC). |
| SR-AGENT-OPS-02 | Startup hook | `Database.Migrate()` при старте контейнера. |
| SR-AGENT-OPS-03 | Kestrel config | Только gRPC endpoint на `:5131`; REST controllers отсутствуют. |

Детали: [[../Алгоритмы и методы бекенда/07 - Platform Operations]].

# Inbound metadata

| Key | Источник | Описание |
| :--- | :--- | :--- |
| `user_id` | Aggregator → `GrpcContextHelper` | Guid пользователя (обязателен; отсутствие → `UNAUTHENTICATED`). |
| `roles` | JWT claims | Comma-separated roles для Vocabulary gRPC outbound. |

Поля `user_id` в proto-сообщениях запросов **не используются** реализацией `AgentGrpcService` — идентификатор берётся из metadata.

# Общая карта ошибок

| Условие | gRPC Status |
| :--- | :--- |
| FluentValidation / невалидный Guid | `InvalidArgument` |
| Тред или project не найден | `NotFound` |
| Run на archived thread | `FailedPrecondition` |
| Отсутствует `user_id` в metadata | `Unauthenticated` |
| Необработанное исключение (prod) | `Internal` «Internal server error» |
| Необработанное исключение (dev) | `Internal` с текстом exception |
