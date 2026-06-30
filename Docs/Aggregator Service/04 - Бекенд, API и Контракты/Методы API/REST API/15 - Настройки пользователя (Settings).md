# Введение

User preferences (Reader, study, UI). JWT. Downstream: **VocabularyService** user settings gRPC или local profile extension через Auth — см. реализацию `UserSettingsController`.

# 1. Список эндпоинтов

| SR | Method | Route | gRPC |
| :--- | :--- | :--- | :--- |
| SR-AGG-SETTINGS-01 | GET | `/api/settings` | GetUserSettings |
| SR-AGG-SETTINGS-01 | PUT | `/api/settings` | UpdateUserSettings |

DTO: [[06 - Медиа, AI, интеграции и настройки (Media AI Integrations)]] — `UserSettingsResponseDto`, `UpdateUserSettingsDto`.

---

# SR-AGG-SETTINGS-01: Получение настроек: GET /api/settings

## Общая информация

Полный snapshot preferences текущего пользователя.

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | UserSettingsDto |

## Логика обработки запроса

* JWT → userId
* gRPC **`GetUserSettings`**
* Defaults merge на BFF если partial response

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **401** | JWT |
| **502** | Downstream |

---

# SR-AGG-SETTINGS-01: Обновление настроек: PUT /api/settings

## Общая информация

Partial или full update JSON merge semantics.

| Тип метода | PUT |
| :--- | :--- |
| **DTO запроса** | UpdateUserSettingsDto |
| **DTO успешного ответа** | UserSettingsDto |

## Логика обработки запроса

* FluentValidation на DTO
* gRPC **`UpdateUserSettings`**
* Reader flags: `markRemainingKnownOnPageTurn`, TTS voice, daily goals

## Ошибки

| Статус-код | Описание |
| :--- | :--- |
| **400** | Validation errors |
| **401** | JWT |
| **502** | Downstream |
