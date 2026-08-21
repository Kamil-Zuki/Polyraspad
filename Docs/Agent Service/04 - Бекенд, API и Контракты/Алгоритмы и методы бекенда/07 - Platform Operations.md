# Platform Operations

# Введение

Платформенные контракты Agent Service без отдельных gRPC RPC: health probe, EF migrations, gRPC-only Kestrel binding.

**SR:** SR-AGENT-OPS-01 … SR-AGENT-OPS-03.

# 1. Список алгоритмов

| Алгоритм | SR | Механизм |
| :--- | :--- | :--- |
| Health check | SR-AGENT-OPS-01 | `GET /healthz` |
| Startup migrations | SR-AGENT-OPS-02 | `Database.Migrate()` |
| gRPC-only Kestrel | SR-AGENT-OPS-03 | Kestrel `:5131` HTTP/2 |

---

# Алгоритм Health check

## Контекст и область применения

### Бизнес-требование

SR-AGENT-OPS-01

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Docker Compose / CI liveness probe. |
| 2 | Не проверяет Vocabulary или LLM availability. |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `status` | string | Always `"ok"` when process alive |

## Логика работы (Псевдокод)

```csharp
// MapGet("/healthz", () => Results.Json(new { status = "ok" }))
```

## Связанные артефакты

* gRPC overview: [[../Методы API/gRPC/00 - gRPC - Общая информация#5. Платформенные контракты (Operations)]]

---

# Алгоритм Startup migrations

## Контекст и область применения

### Бизнес-требование

SR-AGENT-OPS-02

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Destructive migrations требуют review перед production deploy. |

## Логика работы (Псевдокод)

```csharp
// using var scope = app.Services.CreateScope()
// var db = scope.ServiceProvider.GetRequiredService<AgentServiceContext>()
// db.Database.Migrate()
```

## Связанные артефакты

* Entities: `agent_threads`, `agent_messages`, `agent_runs`, `agent_domain_decisions`, `agent_tool_calls`, `agent_artifacts`

---

# Алгоритм gRPC-only Kestrel

## Контекст и область применения

### Бизнес-требование

SR-AGENT-OPS-03

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Internal service; public REST только на Aggregator. |
| 2 | Port `5131` (host mapping in Docker Compose). |

## Логика работы (Псевдокод)

```csharp
// builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(5131, lo => lo.Protocols = HttpProtocols.Http2))
// app.MapGrpcService<AgentGrpcService>()
// No MapControllers()
// Exception: MapGet("/healthz") for ops
```

## Связанные артефакты

* Proto: [[../Методы API/gRPC/agent.proto]]
* Caller: Aggregator Service gRPC client
