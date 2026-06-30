# Navigation и Progress

# Введение

Product-facing tools PolyGuide: навигация по разделам приложения и отображение учебного прогресса. Не используют LLM; progress tool вызывает Vocabulary AnalyticsService.

**SR:** SR-AGENT-NAV-01, SR-AGENT-NAV-02, SR-AGENT-VOC-02.

# 1. Список алгоритмов

| Алгоритм | ToolId | SR | Внешний вызов |
| :--- | :--- | :--- | :--- |
| Navigate | `Navigate` | SR-AGENT-NAV-01 | none |
| Get progress | `GetProgress` | SR-AGENT-NAV-02 | Vocabulary Analytics x2 |

---

# Алгоритм Navigate

## Контекст и область применения

### Бизнес-требование

SR-AGENT-NAV-01

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | User text matches navigation keywords (reader, editor, study, vocabulary, import, library). |
| 2 | Domain = `product_navigation`. |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `destination` | enum | Reader, Editor, Study, Vocabulary, Import, Library | Да |
| `first_deck_id` | string | Study href hint | Нет |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `assistant_content` | string | Short confirmation |
| `actions` | array | Single navigate action card |

## Логика работы (Псевдокод)

```csharp
// destination = intent.Destination ?? Library
// action = BuildNavigateAction(destination, firstDeckId)
//   Reader → /reader
//   Editor → /editor
//   Study → /study/{firstDeckId} or /study
//   Vocabulary → /vocabulary
//   Import → /import
//   Library → /library (default)
// return AgentExecutionResult($"Opening {action.Title}...", product_navigation domain, [action])
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`
* Intent routing: [[03 - Domain policy и Intent routing]]

---

# Алгоритм Get progress

## Контекст и область применения

### Бизнес-требование

SR-AGENT-NAV-02

### Область применения

| № | Описание |
| :--- | :--- |
| 1 | Progress/stats keywords в user text. |
| 2 | Domain = `progress`. |

## Входные данные

| Параметр | Тип данных | Описание | Обязательность |
| :--- | :--- | :--- | :--- |
| `user_id` | uuid | Caller | Да |
| `project_id` | uuid | Current project | Да |
| `project_title` | string | Display name | Да |
| `roles` | array | Vocabulary metadata | Да |

## Выходные данные

| Параметр | Тип данных | Описание |
| :--- | :--- | :--- |
| `assistant_content` | string | Formatted streak, reviews, terms stats |
| `actions` | array | Study + Vocabulary navigate cards |

## Логика работы (Псевдокод)

```csharp
// daily = VocabularyClient.GetDailySummary(userId, roles)
// vocab = VocabularyClient.GetVocabularyStats(userId, projectId, roles)
// content = format streak, reviews today, new cards, total/mature/learning/new terms
// content = SanitizeLemmaLabels(content)
// actions = [Navigate(Study), Navigate(Vocabulary)]
// return AgentExecutionResult(content, progress domain, actions)
```

## Связанные артефакты

* gRPC: `#grpc-ExecuteRun`
* Vocabulary Analytics: [[../Интеграции со сторонними сервисами/01 - Vocabulary Service (gRPC)]]
