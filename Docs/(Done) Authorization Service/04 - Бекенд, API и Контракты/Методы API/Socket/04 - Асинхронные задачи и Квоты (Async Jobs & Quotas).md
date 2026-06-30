# Введение

Данный документ описывает события WebSocket группы **«Асинхронные задачи и квоты (Async Jobs & Quotas)»**: завершение слияния гостевых данных (**SR-AUTH-GQ-04**), готовность архива аудита (**SR-AUTH-LA-05**) и мягкие предупреждения по гостевым лимитам (**SR-AUTH-SQ-02**).

Источники SR: [00 - Общая информация (Возможности сервиса)](../../../../01%20-%20Функциональная%20спецификация/Возможности%20сервиса/00%20-%20Общая%20информация.md) (таблицы групп 3 и 4).

Общий контракт: [00 - WebSocket API - Общая информация](00%20-%20WebSocket%20API%20-%20Общая%20информация.md).

# 1. Список событий

| **Код требования** | **Событие** | **Направление** | **Описание** |
| :----------------- | :---------- | :-------------- | :----------- |
| **SR-AUTH-GQ-04** | `guest_merged` | Server → Client | Асинхронное слияние данных гостя в профиль пользователя завершено (или итог с частичным успехом). |
| **SR-AUTH-LA-05** | `audit_export_completed` | Server → Client | Фоновая задача экспорта WORM-аудита завершена; доступна ссылка на загрузку. |
| **SR-AUTH-SQ-02** | `quota_exceeded_warning` | Server → Client | Приближение к исчерпанию гостевой квоты / мягкий paywall в UI. |

---

# Событие: `guest_merged`

| **Название события** | `guest_merged` |
| :------------------- | :------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-GQ-04**: после `ConvertGuestSession` и фоновой миграции артефактов клиент снимает блокирующий loader и обновляет данные. Событие брокера см. [GuestMergeEventDto](../DTO/04%20-%20Управление%20гостевым%20доступом%20(Guest%20Sessions).md#dto-GuestMergeEventDto). |
| **DTO** | [GuestMergedNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-GuestMergedNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **userId** | string (uuid) | Постоянный глобальный пользователь. |
| **oldGuestId** | string | Идентификатор гостевой сессии до конвертации. |
| **status** | string | `COMPLETED` / `PARTIAL_SUCCESS` / `FAILED`. |
| **message** | string | Резюме для пользователя. |

**Логика обработки**

1. После регистрации пользователя выполняется `ConvertGuestSession`, затем по запросу на слияние — Unary `MergeGuestData` ([04 - Security & Quotas](../gRPC/04%20-%20Защита%20периметра%20и%20Гостевые%20квоты%20(Security%20&%20Quotas).md)); публичный путь — `POST /guests/merge` через API Gateway.
2. По завершении фоновой оркестрации микросервис фиксирует статус и публикует `guest_merged` в персональный канал пользователя (**SR-AUTH-WS-01**).
3. SPA обновляет списки сущностей (проекты, черновики) с бэкенда.

---

# Событие: `audit_export_completed`

| **Название события** | `audit_export_completed` |
| :------------------- | :----------------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-LA-05**: долгая подготовка архива не удерживает HTTP; UI отслеживает `jobId` и получает push при готовности. Опрос статуса — [ExportArchiveJobStatusDto](../DTO/06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-ExportArchiveJobStatusDto). |
| **DTO** | [AuditExportCompletedNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-AuditExportCompletedNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **jobId** | string | Идентификатор задачи (как в ответе `ExportAuditArchive` / REST `POST /audit/export-jobs`). |
| **status** | string | `SUCCEEDED` или `FAILED` (согласовано с [ExportArchiveJobStatusDto](../DTO/06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-ExportArchiveJobStatusDto)). |
| **downloadUrl** | string | Presigned URL при `SUCCEEDED`. |
| **errorMessage** | string | При `FAILED` — причина для администратора (как в опросе статуса). |

**Логика обработки**

1. Инициатор запускает экспорт через API Gateway; на стороне Auth выполняется Unary `ExportAuditArchive` ([05 - Audit & Analytics](../gRPC/05%20-%20Аудит%20доступа%20и%20Аналитика%20(Audit%20&%20Analytics).md)).
2. Фоновый воркер завершает архив; статус при необходимости опрашивается через `GetExportArchiveJobStatus` (тот же документ gRPC).
3. Микросервис рассылает `audit_export_completed` инициатору (**SR-AUTH-WS-01**); UI активирует скачивание по `downloadUrl`.

---

# Событие: `quota_exceeded_warning`

| **Название события** | `quota_exceeded_warning` |
| :------------------- | :----------------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-SQ-02**: проактивное предупреждение до жёсткого отказа (`RESOURCE_EXHAUSTED` / HTTP 429 на периметре). Агрегированное состояние квот — [GuestQuotaStatusDto](../DTO/04%20-%20Управление%20гостевым%20доступом%20(Guest%20Sessions).md#dto-GuestQuotaStatusDto). |
| **DTO** | [QuotaWarningNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-QuotaWarningNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **quotaType** | string | Идентификатор лимита (например, `ai_generations`, `projects`). |
| **remaining** | integer | Остаток единиц. |
| **thresholdReached** | boolean | Пересечён ли порог предупреждения (например, 90%). |

**Логика обработки**

1. Бизнес-операция гостя завершается списанием через Unary `ConsumeGuestQuota`; при плановых проверках используются `CheckGuestQuota` и `GetGuestAccessStatus` ([04 - Security & Quotas](../gRPC/04%20-%20Защита%20периметра%20и%20Гостевые%20квоты%20(Security%20&%20Quotas).md)).
2. Если остаток ниже порога (политика продукта), микросервис публикует `quota_exceeded_warning` в канал гостевой/пользовательской сессии (**SR-AUTH-WS-01**).
3. UI показывает баннер регистрации или апгрейда до исчерпания лимита.
