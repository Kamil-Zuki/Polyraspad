# Группа 14: Внешние интеграции (Integrations)

## Введение

В этом разделе описывается **HTTP outbound** из Aggregator Service к публичным API — **MyMemory** (translate) и **Free Dictionary API** (dictionary lookup). Список providers статичен на BFF; отдельного integration microservice нет.

Aggregator **не** persist translation history; каждый call — live HTTP. JWT required — integrations не public.

**Метафора:**

Представьте **справочную стойку с двумя телефонными книгами**. Пользователь (JWT) просит перевести фразу или найти определение — оператор звонит во внешний бесплатный сервис и возвращает ответ, не сохраняя запрос в архиве.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к внешним интеграциям.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-INT-01** | **Перевод и словарный lookup:** Outbound HTTP к MyMemory и Free Dictionary; lookup по exact word form без лемматизации на BFF. |

---

# Детальная спецификация требований

## SR-AGG-INT-01: Перевод и словарный lookup {#SR-AGG-INT-01}

Outbound HTTP к публичным API перевода и словаря. Aggregator нормализует ошибки provider и не кэширует результаты — каждый запрос live.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **JWT** | `IntegrationController` — `[Authorize]`. |
| **Static providers** | translators: `mymemory`; dictionaries: `freedictionary`. |
| **Provider validation** | Unknown provider id → HTTP **400**. |
| **502 on provider failure** | HttpClient non-success → Bad Gateway. |
| **404 dictionary** | Word not found at Free Dictionary API. |
| **Term-first lookup** | Dictionary query by **exact word form** user entered — no lemma merge on BFF. |
| **Lang normalize** | `en-US` → `en` for API URLs. |

### 2. Высокоуровневое описание

Представим flow как **два окна справки на стойке Reader**.

1. **Providers:** UI loads available translator/dictionary ids for settings dropdown.
2. **Translate:** user highlights text in Reader → BFF calls MyMemory REST with lang pair.
3. **Dictionary:** term inspector requests definitions for exact form — `sleep` and `slept` are separate lookups.
4. **Response mapping:** trim definitions, cap counts for UI payload size.

Aggregator acts as **outbound HTTP client** with error normalization — not caching layer.

Таким образом, **free tier external APIs** доступны без exposing API keys to browser (MyMemory/Free Dictionary are public endpoints).

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `IntegrationController`, base `/api/integrations`.
* **HttpClient:** `IHttpClientFactory` default client.

#### Сценарий А: List providers (Happy Path)

**Сценарий:** Reader settings populate provider dropdown.

1. **GET** `/api/integrations/providers` + Bearer.
2. **Ответ:** HTTP **200**, `IntegrationProvidersResponseDto` (translators + dictionaries lists).

#### Сценарий Б: Translate selection (Happy Path)

**Сценарий:** User translates highlighted phrase in Reader.

1. **POST** `/api/integrations/translate`, body `{ text, sourceLanguage, targetLanguage, provider: "mymemory" }`.
2. **Outbound:** GET `api.mymemory.translated.net/get?q=…&langpair=en|ru`.
3. **Parse (BFF):** extract `translatedText`.
4. **Ответ:** HTTP **200**, `TranslateResponseDto`.

#### Сценарий В: Dictionary lookup (Happy Path)

1. **POST** `/api/integrations/dictionary/lookup`, body `{ word: "slept", language: "en", provider: "freedictionary" }`.
2. **Outbound:** GET dictionaryapi.dev.
3. **Ответ:** HTTP **200**, phonetic + meanings (max 8, 3 defs each).

#### Сценарий Г: Unsupported provider (Negative Path)

1. **POST** translate with `provider: "google"`.
2. **Ответ:** HTTP **400** `{ "error": "Unsupported translator provider: google" }`.

#### Сценарий Д: Word not found (Negative Path)

1. **POST** dictionary lookup for nonsense word.
2. **Provider:** HTTP 404.
3. **Ответ (BFF):** HTTP **404** `{ "error": "Word not found." }`.

#### Сценарий Е: Provider timeout (Negative Path)

1. **Outbound** fails network-level.
2. **Ответ:** HTTP **502** `{ "error": "Translator provider request failed." }`.

---

*Следующая группа: [[15 - Настройки пользователя (Settings)]].*
