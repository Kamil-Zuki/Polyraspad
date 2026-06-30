# Группа 12: AI-прокси (AI Proxy)

## Введение

В этом разделе описывается **BFF-facing LLM proxy** на Aggregator — models list, plain generate (Card Editor), structured mining-draft (Reader). Аутентификация **не JWT**, а shared secret **`X-Ai-Proxy-Key`** между Next.js server routes и Aggregator (`AiProxyApiKeyFilter`).

Provider API key (`Ai:ApiKey`) хранится только на Aggregator. Browser never sees OpenAI/Mistral credentials — только Next.js BFF calls `/api/ai` server-side.

**Метафора:**

Представьте **внутренний телефон editor/reader к LLM**. Сотрудник (Next.js `/api/ai/*` route) знает служебный код (`X-Ai-Proxy-Key`); посетители сайта напрямую к Aggregator `/api/ai` не ходят.

Конфигурация: `AI_PROXY_API_KEY` (frontend) = `Ai:ProxyApiKey` (Aggregator). См. [[16 - Платформенные контракты (Operations)#SR-AGG-OPS-03|SR-AGG-OPS-03]] — dev default key blocked in Production.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к AI proxy.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-AI-01** | **LLM-прокси для Next.js BFF:** Models, generate и mining-draft по shared secret X-Ai-Proxy-Key; JWT не используется. |

---

# Детальная спецификация требований

## SR-AGG-AI-01: Models, generate, mining-draft {#SR-AGG-AI-01}

Thin wrapper над `OpenAiChatCompletionClient`. Controller `[AllowAnonymous]` + `AiProxyApiKeyFilter` — defense in depth vs public internet.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **X-Ai-Proxy-Key** | Shared secret with Next.js BFF — not end-user JWT. |
| **503 when disabled** | `Ai:Enabled=false` or empty `Ai:ApiKey` → Service Unavailable. |
| **No stream** | `generate` rejects `stream=true` with 400. |
| **Mining JSON** | Structured output: `targetTranslationInContext`, `sentenceTranslation`, optional `dictionaryLemmaHint` (**hint only** — not term identity). |
| **Term-first guard** | Lemma hint must not replace exact-form term model (см. LingQ guardrails). |
| **502 on LLM failure** | Provider errors → Bad Gateway with message. |

### 2. Высокоуровневое описание

Представим AI proxy как **переводческую будку с фиксированным оператором**.

1. **Models:** editor UI asks available models — Aggregator returns configured `Ai:Model` list.
2. **Generate:** card editor sends plain prompt → plain text completion (legacy editor shape).
3. **Mining-draft:** Reader sends sentence + **exact target form** → LLM returns JSON-only for LingQ inspector fields.
4. **BFF path:** browser → Next.js `/api/ai/*` → Aggregator `/api/ai/*` with proxy key → external OpenAI-compatible API.

Aggregator strips markdown fences from LLM JSON when needed (`ExtractJsonObject`).

Таким образом, **LLM credentials and rate** centralized on Aggregator; frontend BFF adds user session context separately.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Инициатор:** Next.js server route (не browser direct).
* **Base:** `/api/ai`.
* **Header:** `X-Ai-Proxy-Key: {AI_PROXY_API_KEY}`.
* **Outbound:** `OpenAiChatCompletionClient` → `Ai:CompletionBaseUrl`.

#### Сценарий А: Editor generate (Happy Path)

**Сценарий:** Card Editor AI assist field completion.

1. **POST** `/api/ai/generate`, body `{ prompt, model?, stream: false }`.
2. **Filter:** valid proxy key.
3. **Outbound:** chat completion with system prompt (plain text only).
4. **Ответ:** HTTP **200**, `{ response, model, provider }`.

#### Сценарий Б: Reader mining-draft (Happy Path)

**Сценарий:** User clicks blue word — inspector requests AI translations.

1. **POST** `/api/ai/mining-draft` with sentence, target (exact form), sourceLanguage, targetLanguage.
2. **LLM:** JSON-only system prompt.
3. **Parse (BFF):** extract JSON object, validate non-empty `targetTranslationInContext`.
4. **Ответ:** HTTP **200**, `MiningDraftResponseDto`.

#### Сценарий В: AI disabled (Negative Path)

1. **GET** `/api/ai/models` when `Ai:Enabled=false`.
2. **Ответ:** HTTP **503** `{ "error": "AI is disabled …" }`.

#### Сценарий Г: Missing proxy key (Negative Path)

1. Request without `X-Ai-Proxy-Key`.
2. **Filter:** HTTP **401** or **403**.

#### Сценарий Д: Stream requested (Negative Path)

1. **POST** generate with `stream: true`.
2. **Ответ:** HTTP **400** `{ "error": "Stream is not supported …" }`.

---

*Следующая группа: [[13 - Автоматизация (Automation)]].*
