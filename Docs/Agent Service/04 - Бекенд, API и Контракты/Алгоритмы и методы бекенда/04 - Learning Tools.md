# Learning Tools

# Введение

Алгоритмы learning tools выполняются внутри `AgentOrchestrator.ExecuteToolAsync` после intent routing. Часть tools вызывает **Vocabulary AIService** по gRPC; часть — **OpenAI-compatible LLM** (см. [[06 - LLM Provider]]).

**SR:** SR-AGENT-TOOL-01 … SR-AGENT-TOOL-05, SR-AGENT-VOC-02.

# 1. Список алгоритмов

| Алгоритм | ToolId | SR | Внешний вызов |
| :--- | :--- | :--- | :--- |
| Explain word | `ExplainWord` | SR-AGENT-TOOL-01 | LLM |
| Grammar help | `GrammarHelp` | SR-AGENT-TOOL-02 | Vocabulary `ExplainGrammar` |
| Generate example | `GenerateExample` | SR-AGENT-TOOL-03 | Vocabulary `GenerateContext` |
| Build card draft | `BuildCardDraft` | SR-AGENT-TOOL-04 | Optional `GenerateContext` |
| General answer | `GeneralAnswer` | SR-AGENT-TOOL-05 | LLM (PolyGuide prompt) |

---

# Алгоритм Explain word

## Контекст и область применения

### Почему был создан

Объяснение **exact surface form** слова/фразы без lemma labels — core LingQ-style learning path.

### Бизнес-требование

SR-AGENT-TOOL-01

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | User text matches explain/define patterns с extracted term. |
| 2 | Domain = `language_learning`, allowed. |

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | Требуется non-empty extracted word; иначе clarification prompt. |
| 2 | LLM must be configured (`Ai:Enabled`). |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `word` | string | Exact surface form | Да |
| `sentence` | string | Optional context | Нет |
| `source_lang` | string | Project source | Да |
| `target_lang` | string | Explanation language | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `assistant_content` | string | LLM explanation (sanitized) |
| `actions` | array | Editor draft + Vocabulary navigate cards |

## Логика работы (Псевдокод)

```csharp
// 1. Validate extracted word
// 2. Build prompt: exact form, no lemma labels, brief answer in target_lang
// 3. content = SanitizeLemmaLabels(await LlmProvider.CompleteAsync(prompt))
// 4. actions = [EditorDraftAction({ Word }), Navigate(Vocabulary)]
// 5. return AgentExecutionResult(content, domain, actions)
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`
* Интеграция LLM: [[../Интеграции со сторонними сервисами/02 - LLM Provider (OpenAI-compatible HTTP)]]
* Vocabulary: [[../Интеграции со сторонними сервисами/01 - Vocabulary Service (gRPC)]]

---

# Алгоритм Grammar help

## Контекст и область применения

### Бизнес-требование

SR-AGENT-TOOL-02

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Grammar question patterns с extracted term. |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `word` | string | Target form | Да |
| `sentence` | string | Context (fallback = word) | Нет |
| `target_lang` | string | Explanation language | Да |
| `user_id`, `roles` | — | Vocabulary metadata | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `assistant_content` | string | `ExplainGrammar` response, sanitized |

## Логика работы (Псевдокод)

```csharp
// response = VocabularyClient.ExplainGrammar(userId, sentence ?? word, word, targetLang, roles)
// return AgentExecutionResult(Sanitize(response.Explanation), domain)
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`
* Vocabulary AIService: [[../Интеграции со сторонними сервисами/01 - Vocabulary Service (gRPC)]]

---

# Алгоритм Generate example

## Контекст и область применения

### Бизнес-требование

SR-AGENT-TOOL-03

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `word` | string | Exact form | Да |
| `source_lang` | string | Example language | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `assistant_content` | string | Example + translation |
| `actions` | array | Editor draft with Word, Expression, Translation |

## Логика работы (Псевдокод)

```csharp
// response = VocabularyClient.GenerateContext(userId, word, sourceLang, roles)
// suggestion = response.Suggestions.FirstOrDefault() ?? throw
// content = format example + translation
// draft = { Word, Expression, Translation }
// return AgentExecutionResult(content, domain, [EditorDraftAction(draft)])
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`

---

# Алгоритм Build card draft

## Контекст и область применения

### Бизнес-требование

SR-AGENT-TOOL-04

## Логика работы (Псевдокод)

```csharp
// draft = { Word: word, optional Expression from intent }
// try optional GenerateContext → merge Expression, Translation
// catch → log debug, continue with Word-only draft
// return AgentExecutionResult(summary lines, domain, [EditorDraftAction(draft)])
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`

---

# Алгоритм General answer

## Контекст и область применения

### Бизнес-требование

SR-AGENT-TOOL-05

### Ограничения применения

| № | Описание |
| :--- | :--- |
| 1 | PolyGuide system prompt: language learning only, no code. |
| 2 | Term-first; strip lemma labels from output. |

## Логика работы (Псевдокод)

```csharp
// prompt = PolyGuide system rules + project title + langs + user_text
// trimmed = SanitizeLemmaLabels(await LlmProvider.CompleteAsync(prompt))
// refusal = regex detect self-refusal patterns
// return AgentExecutionResult(trimmed, language_learning domain, Refusal: refusal)
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`
* LLM: [[06 - LLM Provider]]
