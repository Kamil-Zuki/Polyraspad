# LLM Provider (OpenAI-compatible HTTP)

## Общая информация

| Поле | Значение |
| :--- | :--- |
| **SR** | SR-AGENT-LLM-01 |
| **Протокол** | HTTPS (OpenAI-compatible Chat Completions API) |
| **Реализация** | `OpenAiCompatibleAgentLlmProvider` (`IAgentLlmProvider`) |
| **Вызывающие** | `AgentOrchestrator` — tools `ExplainWord`, `GeneralAnswer` |

**Источник требования:** [[01 - Функциональная спецификация/Возможности сервиса/10 - LLM-провайдер (LLM Provider)#SR-AGENT-LLM-01]]

---

## Endpoint

| Метод | URL | Описание |
| :--- | :--- | :--- |
| `POST` | `{Ai:BaseUrl}/chat/completions` | Non-streaming chat completion |

Default `BaseUrl`: `https://api.openai.com/v1`. Typed `HttpClient` base address = `Ai:BaseUrl`.

---

## Request

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| `model` | string | Из `Ai:Model` (default `gpt-4o-mini`). |
| `messages` | array | Single `{ role: "user", content: prompt }`. |
| `stream` | boolean | Always `false`. |

**Headers:**

| Header | Значение |
| :--- | :--- |
| `Authorization` | `Bearer {Ai:ApiKey}` |
| `Content-Type` | `application/json` |

Prompt формируется orchestrator-ом per tool (PolyGuide system rules, term-first, no markdown).

---

## Response

Парсинг OpenAI-compatible JSON:

```
choices[0].message.content → string (trimmed)
```

При пустом content возвращается `""`.

---

## Configuration (`Ai` section)

| Key | Default | Описание |
| :--- | :--- | :--- |
| `BaseUrl` | `https://api.openai.com/v1` | Provider base URL. |
| `ApiKey` | — | Bearer token; required when Enabled. |
| `Model` | `gpt-4o-mini` | Model id. |
| `TimeoutSeconds` | `120` | HttpClient timeout. |
| `Enabled` | `true` | If false or missing ApiKey → `InvalidOperationException`. |

Docker/env: те же ключи через `AI_COMPLETION_*` на Aggregator; Agent Service использует секцию `Ai` в `appsettings` / env override.

---

## Error handling

| Условие | Поведение Agent Service |
| :--- | :--- |
| `Enabled = false` или пустой ApiKey | `InvalidOperationException` → orchestrator catch → failed tool + user-facing error message. |
| HTTP non-2xx | Log warning; `InvalidOperationException("AI completion request failed")`. |
| Timeout | HttpClient exception → orchestrator error path. |

LLM failures **не** пробрасываются как gRPC `INTERNAL` напрямую — orchestrator сохраняет run с error assistant text.

---

## Связанные артефакты

* Алгоритм: [[../Алгоритмы и методы бекенда/06 - LLM Provider]]
* Learning tools using LLM: [[../Алгоритмы и методы бекенда/04 - Learning Tools]]
* gRPC entry: `#grpc-ExecuteRun` в [[../Методы API/gRPC/02 - Запуски и оркестрация (Runs)]]
