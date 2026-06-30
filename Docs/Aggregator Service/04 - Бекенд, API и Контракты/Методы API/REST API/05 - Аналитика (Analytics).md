# Введение

Read-only dashboard analytics. JWT. Downstream: **VocabularyService.AnalyticsService**.

# 1. Список эндпоинтов

| SR | Method | Route | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-ANALYTICS-01 | GET | `/api/analytics/vocabulary` | GetVocabularyStats |
| SR-AGG-ANALYTICS-01 | GET | `/api/analytics/heatmap` | GetHeatmap |
| SR-AGG-ANALYTICS-01 | GET | `/api/analytics/daily` | GetDailySummary |

DTO: [[03 - Карточки и обучение (Cards Study)]] — `VocabularyStatsResponseDto`, `HeatmapResponseDto`, `DailySummaryResponseDto`.

---

# SR-AGG-ANALYTICS-01: Vocabulary stats: GET /api/analytics/vocabulary

## Общая информация

Snapshot статистики терминов по project (LingQ-style counters).

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `projectId` (обязателен) |
| **DTO успешного ответа** | VocabularyStatsResponseDto |

## Логика обработки запроса

* JWT → metadata
* BFF validation: `projectId` required → иначе **400**
* gRPC **`GetVocabularyStats`**

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | ProjectId is required |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-ANALYTICS-01: Daily summary (graceful fallback): GET /api/analytics/daily

## Общая информация

Дневные цели, streak, time spent. При недоступности Vocabulary BFF возвращает **default с нулями** (не 502).

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | Query: `timezoneOffset` (optional, minutes from UTC) |
| **DTO успешного ответа** | DailySummaryResponseDto |

## Логика обработки запроса

* gRPC **`GetDailySummary`**
* При `Unavailable` / `DeadlineExceeded` / `Unimplemented`: log warning → HTTP **200** с пустым valid DTO (streak=0, goals=0)

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **200** | Degraded — default DTO при падении downstream |

---

# SR-AGG-ANALYTICS-01: Heatmap: GET /api/analytics/heatmap

## Общая информация

Activity heatmap (GitHub-style) по project.

| Тип метода | GET |
| **Query** | `projectId`, optional date range |
| **DTO успешного ответа** | HeatmapResponseDto |

## Логика обработки запроса

* gRPC **`GetHeatmap`**

## Ошибки

| **400** | Missing projectId |
| **502** | Downstream |
