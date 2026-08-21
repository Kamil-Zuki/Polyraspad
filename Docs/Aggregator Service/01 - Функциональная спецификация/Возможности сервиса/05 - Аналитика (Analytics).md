# Группа 5: Аналитика (Analytics)

## Введение

В этом разделе описывается REST-прокси к **VocabularyService.AnalyticsService** — **дашбордная аналитика** обучения: словарный запас, heatmap активности, дневная сводка и streak.

Aggregator агрегирует только HTTP; расчёты и хранение метрик — в VocabularyService. Исключение: при недоступности downstream для **daily summary** BFF возвращает **graceful default** (нули), чтобы dashboard не ломался.

**Метафора:**

Представьте **информационное табло в холле школы**. Вы показываете пропуск (JWT); диспетчер (Aggregator) запрашивает у бухгалтерии обучения (AnalyticsService) цифры и выводит их на экран — сам считать успеваемость диспетчер не умеет.

REST-контракты: `04/.../REST API/` (Analytics — при следующем проходе 04).

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к analytics API.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-ANALYTICS-01** | **Аналитика словаря для dashboard:** Статистика vocabulary, heatmap активности и daily summary; при сбое Vocabulary daily возвращает безопасный default с нулями. |

---

# Детальная спецификация требований

## SR-AGG-ANALYTICS-01: Vocabulary stats, heatmap, daily summary {#SR-AGG-ANALYTICS-01}

Три read-only endpoint для dashboard и progress widgets. Все требуют JWT.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Read-only BFF** | Нет мутаций analytics на Aggregator. |
| **projectId** | Обязателен для vocabulary stats; опционален для heatmap. |
| **Graceful degradation** | `GetDailySummary`: при Unavailable/DeadlineExceeded/Unimplemented — default DTO с нулями. |
| **Timezone** | Daily summary принимает optional `timezoneOffset` (minutes from UTC). |

### 2. Высокоуровневое описание

Представим analytics как **три разных отчёта из одного архива**.

1. **Словарный запас (vocabulary):** «сколько слов в каких статусах в project X» — один snapshot для LingQ-style counters и CEFR breakdown.
2. **Heatmap:** календарь активности за год — сколько reviews/new per day; optional filter by project.
3. **Daily summary:** сегодняшние цели (new cards, reviews), streak, time spent — с учётом timezone пользователя.

Aggregator для каждого отчёта: JWT → metadata → один gRPC call → map DTO. **Единственная** особенная ветка — daily при падении Vocabulary: BFF **синтезирует** пустой но валидный `DailySummaryResponseDto`, чтобы frontend widgets не показывали error state.

Таким образом, analytics остаётся **eventually consistent** с domain data, а UX dashboard защищён от кратковременной недоступности микросервиса.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `AnalyticsController`, base `/api/analytics`.
* **JWT:** обязателен на все actions.

#### Сценарий А: Dashboard vocabulary widget (Happy Path)

1. **GET** `/api/analytics/vocabulary?projectId={id}`.
2. **gRPC:** `GetVocabularyStats`.
3. **Ответ:** HTTP **200**, `VocabularyStatsResponseDto`.

#### Сценарий Б: Heatmap за год (Happy Path)

1. **GET** `/api/analytics/heatmap?year=2026&projectId={id}` (project optional).
2. **gRPC:** `GetHeatmap`.
3. **Ответ:** HTTP **200**, `HeatmapResponseDto`.

#### Сценарий В: Daily summary при живом Vocabulary (Happy Path)

1. **GET** `/api/analytics/daily?timezoneOffset=180`.
2. **gRPC:** `GetDailySummary`.
3. **Ответ:** HTTP **200**, streak + goal progress.

#### Сценарий Г: Vocabulary недоступен — daily fallback (Degraded Path)

1. **GET** `/api/analytics/daily`.
2. **gRPC:** `Unavailable` / `DeadlineExceeded` / `Unimplemented`.
3. **BFF:** лог warning → HTTP **200** с default (streak=0, goals current=0).
4. **UI:** показывает нули, не error banner.

#### Сценарий Д: Missing projectId для vocabulary (Negative Path)

1. **GET** `/api/analytics/vocabulary` без query.
2. **BFF validation:** HTTP **400** `{ "error": "ProjectId is required" }`.

---

## SR-AGG-ANALYTICS-02: Баланс навыков {#SR-AGG-ANALYTICS-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| Read-only | `GET /api/analytics/skills` |
| Downstream | Vocabulary Analytics SkillBalance |

### 2. Высокоуровневое описание
Dashboard виджет баланса Reading/Listening/Writing/Speaking проксируется без локального кэша на BFF.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Happy Path
1. GET `/api/analytics/skills?projectId=…` с JWT.
2. gRPC SkillBalance → HTTP 200 JSON.

---

*Следующая группа: [[06 - Reader и термины (Reader)]].*
