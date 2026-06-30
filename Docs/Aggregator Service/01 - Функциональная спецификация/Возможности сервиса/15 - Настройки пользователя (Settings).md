# Группа 15: Настройки пользователя (Settings)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **VocabularyService** для **глобальных user settings** — GET/PUT `/api/settings`. Включает Reader preferences (например, mark blue words known on page turn), study defaults и прочие flags по контракту `UserSettingsResponseDto`.

Settings scoped **per user**, not per project — хранятся в VocabularyService; Aggregator stateless.

**Метафора:**

Представьте **пульт настроек профиля в личном кабинете**. Пользователь крутит переключатели в UI; Aggregator передаёт изменения в VocabularyService, где settings привязаны к user id.

Связь с Reader: настройка page-turn bulk-known влияет на [[06 - Reader и термины (Reader)#SR-AGG-READER-01|SR-AGG-READER-01]] — enforcement в VocabularyService.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к user settings.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-SETTINGS-01** | **Глобальные настройки пользователя:** Чтение и обновление Reader preferences и study defaults; scope per user, не per project. |

---

# Детальная спецификация требований

## SR-AGG-SETTINGS-01: Глобальные настройки пользователя {#SR-AGG-SETTINGS-01}

Чтение и обновление user-level preferences (Reader, study defaults). Scope — один пользователь из JWT; project-specific settings не входят в этот SR.

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

1. **App load:** frontend GET settings — applies Reader page-turn behavior, UI toggles, study defaults.
2. **User change:** PUT partial/full `UpdateUserSettingsDto` from settings page.
3. **Vocabulary** persists authoritative state; Aggregator returns mapped `UserSettingsResponseDto`.
4. **Reader bulk-known:** when enabled, page turn triggers bulk-known term ids (domain reads same settings).

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
