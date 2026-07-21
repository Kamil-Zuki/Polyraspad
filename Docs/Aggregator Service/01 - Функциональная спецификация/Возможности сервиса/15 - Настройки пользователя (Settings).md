# Группа 15: Настройки пользователя (Settings)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **VocabularyService** для **глобальных user settings** — GET/PUT `/api/settings` по контракту `UserSettingsResponseDto` / `UpdateUserSettingsDto`.

Фактические поля DTO: `RolloverHour`, `DailyGoalNew`, `DailyGoalReview`, `InterfaceLanguage`, `CurrentStreak`, `MaxStreak`. Поля **AutoMarkAsKnownOnPageTurn нет** — page-turn mark-known инициирует клиент через `BulkMarkKnown` (`SR-AGG-READER-01`).

Settings scoped **per user**, not per project — хранятся в VocabularyService; Aggregator stateless.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к user settings.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-SETTINGS-01** | **Глобальные настройки пользователя:** Goals, rollover, language, streaks; без server-side page-turn flag. |

---

# Детальная спецификация требований

## SR-AGG-SETTINGS-01: Глобальные настройки пользователя {#SR-AGG-SETTINGS-01}

Чтение и обновление user-level study/UI defaults. Scope — один пользователь из JWT. Page-turn behaviour не хранится в settings DTO.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Global scope** | Settings per user, not per project. |
| **JWT обязателен** | `UserSettingsController` — `[Authorize]`. |
| **AutoMapper** | REST DTO ↔ protobuf `GetUserSettingsRequest` / `UpdateUserSettingsRequest`. |
| **Identity metadata** | userId, roles на gRPC calls. |
| **Error mapping** | `InvalidArgument` → 400, `NotFound` → 404, `PermissionDenied` → 403. |
| **Idempotent GET** | Safe read anytime on app load. |

### 2. Высокоуровневое описание

Представим settings как **preferences file в облаке**.

1. **App load:** frontend GET settings — goals, rollover, language, streaks.
2. **User change:** PUT `UpdateUserSettingsDto` from settings page.
3. **Vocabulary** persists authoritative state; Aggregator returns mapped `UserSettingsResponseDto`.
4. **Reader bulk-known:** when enabled, page turn triggers bulk-known term ids (domain reads same settings).
5. **Rollover Hour & Goals:** `RolloverHour` defines when the study day rolls over for daily streak calculations, and `DailyGoalNew`/`DailyGoalReview` set daily spaced repetition targets.

Aggregator не кэширует settings between requests — React Query cache on frontend.

Таким образом, **user preferences** single source of truth в Vocabulary, REST — thin sync channel.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Controller:** `UserSettingsController`, base `/api/settings`.
* **Downstream:** `GetUserSettingsAsync`, `UpdateUserSettingsAsync`.

#### Сценарий А: Load settings on login (Happy Path)

**Сценарий:** SPA hydrates user preferences after auth.

1. **GET** `/api/settings` + Bearer JWT.
2. **gRPC:** `GetUserSettings` with metadata.
3. **Ответ:** HTTP **200**, `UserSettingsResponseDto`.

#### Сценарий Б: Toggle Reader page-turn preference (Happy Path)

**Сценарий:** User enables «mark remaining blue words as known on page turn».

1. **PUT** `/api/settings`, body `UpdateUserSettingsDto` with flag true.
2. **gRPC:** `UpdateUserSettings`.
3. **Ответ:** HTTP **200**, updated DTO.
4. **Later Reader page turn:** Vocabulary applies bulk-known per setting.

#### Сценарий В: Unauthorized (Negative Path)

1. **GET** without JWT.
2. **Middleware:** HTTP **401**.

#### Сценарий Г: Invalid payload (Negative Path)

1. **PUT** with domain-invalid field values.
2. **gRPC:** `InvalidArgument` → HTTP **400**.

---

*Следующая группа: [[16 - Платформенные контракты (Operations)]].*
