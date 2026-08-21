# Введение

BFF-facing LLM proxy для Next.js server routes (`/api/ai/*` на frontend → Aggregator `/api/ai/*`). **JWT не используется.** Защита: `[AllowAnonymous]` + **`AiProxyApiKeyFilter`** — header **`X-Ai-Proxy-Key`** (shared secret `AI_PROXY_API_KEY` = `Ai:ProxyApiKey`).

Provider API key (`Ai:ApiKey`) хранится только на Aggregator. Downstream: `OpenAiChatCompletionClient` (OpenAI-compatible HTTP).

Код SR в `01`: **SR-AGG-AI-01** (единый код для models, generate, mining-draft).

DTO: [[06 - Медиа, AI, интеграции и настройки (Media AI Integrations)]].

# 1. Список эндпоинтов

Сверено с `AggregatorService/Controllers/AiProxyController.cs`.

| SR | Method | Route | Назначение |
| :--- | :--- | :--- | :--- |
| SR-AGG-AI-01 | GET | `/api/ai/models` | Список доступных моделей |
| SR-AGG-AI-01 | POST | `/api/ai/generate` | Plain text completion (Card Editor) |
| SR-AGG-AI-01 | POST | `/api/ai/mining-draft` | Structured JSON для Reader mining |

---

# SR-AGG-AI-01: Models: GET /api/ai/models

## Общая информация

Список моделей для UI editor/reader.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | AiModelsResponseDto (`models[]`, `provider`) |
| **Auth** | **`X-Ai-Proxy-Key`** (required) |

## Логика обработки запроса

* `AiProxyApiKeyFilter` validates key
* If `Ai:Enabled=false` or empty `Ai:ApiKey` → **503**
* Returns configured `Ai:Model` (single entry unless client override enabled)

## Успешный ответ

HTTP **200**:

```json
{
  "models": ["gpt-4o-mini"],
  "provider": "openai-compatible"
}
```

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | Missing/invalid X-Ai-Proxy-Key |
| **503** | AI disabled or API key not configured |

---

# SR-AGG-AI-01: Generate: POST /api/ai/generate

## Общая информация

Legacy plain prompt → plain text для Card Editor.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | AiProxyGenerateRequestDto (`prompt`, optional `model`, `stream`) |
| **DTO успешного ответа** | AiGenerateResponseDto (`response`, `model`, `provider`) |
| **Auth** | **`X-Ai-Proxy-Key`** |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

* Reject `stream=true` → **400**
* `OpenAiChatCompletionClient.CompleteAsync` with fixed system prompt
* Model: client override only if `Ai:AllowClientModelOverride=true`

## Успешный ответ

HTTP **200**.

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Empty prompt / stream not supported |
| **401** | Invalid proxy key |
| **502** | Provider error |
| **503** | AI disabled |

---

# SR-AGG-AI-01: Mining draft: POST /api/ai/mining-draft

## Общая информация

Structured JSON для LingQ-style Reader: перевод target-in-context + sentence translation.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | MiningDraftRequestDto (`sentence`, `target`, `sourceLanguage`, `targetLanguage`) |
| **DTO успешного ответа** | MiningDraftResponseDto |
| **Auth** | **`X-Ai-Proxy-Key`** |

## Логика обработки запроса

* Validate sentence + target non-empty
* LLM returns JSON; BFF extracts `{...}` from response
* `dictionaryLemmaHint` — **hint only**, не identity термина (term-first model)

## Успешный ответ

HTTP **200**:

```json
{
  "targetTranslationInContext": "…",
  "sentenceTranslation": "…",
  "dictionaryLemmaHint": null
}
```

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Missing sentence/target |
| **502** | Provider / JSON parse / empty translation |
| **503** | AI not available |
