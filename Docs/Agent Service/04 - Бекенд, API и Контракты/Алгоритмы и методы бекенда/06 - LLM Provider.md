# LLM Provider

# Введение

Алгоритм вызова OpenAI-compatible Chat Completions API через typed HttpClient. Абстракция `IAgentLlmProvider` изолирует HTTP-детали от orchestrator.

**SR:** SR-AGENT-LLM-01.

# 1. Список алгоритмов

| Алгоритм | Класс | SR |
| :--- | :--- | :--- |
| Chat completion | `OpenAiCompatibleAgentLlmProvider.CompleteAsync` | SR-AGENT-LLM-01 |

---

# Алгоритм Chat completion

## Контекст и область применения

### Почему был создан

Centralized LLM access для explain_word и general_answer без дублирования HTTP-кода в orchestrator.

### Бизнес-требование

SR-AGENT-LLM-01

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | `AgentOrchestrator` tools: ExplainWord, GeneralAnswer. |
| 2 | Non-streaming single-turn prompts. |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Требует `Ai:Enabled` и non-empty `Ai:ApiKey`. |
| 2 | Не используется для grammar/example tools (Vocabulary AIService). |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `prompt` | string | Full user prompt (built by orchestrator) | Да |
| `Ai:BaseUrl` | string | Provider base | Да (config) |
| `Ai:Model` | string | Model id | Да (config) |
| `Ai:ApiKey` | string | Bearer token | Да when enabled |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `content` | string | Trimmed assistant text from `choices[0].message.content` |

## Логика работы (Псевдокод)

```csharp
// 1. if (!Ai.Enabled || string.IsNullOrWhiteSpace(Ai.ApiKey))
//      throw InvalidOperationException("AI completion is not configured")
// 2. POST {BaseUrl}/chat/completions
//      body = { model, messages: [{ role: "user", content: prompt }], stream: false }
//      Authorization: Bearer {ApiKey}
// 3. if (!response.IsSuccessStatusCode) log + throw InvalidOperationException
// 4. parse JSON choices[0].message.content
// 5. return content?.Trim() ?? ""
```

## Связанные артефакты

* Интеграция: [[../Интеграции со сторонними сервисами/02 - LLM Provider (OpenAI-compatible HTTP)]]
* Learning tools: [[04 - Learning Tools]]
* gRPC: `#grpc-ExecuteRun`
