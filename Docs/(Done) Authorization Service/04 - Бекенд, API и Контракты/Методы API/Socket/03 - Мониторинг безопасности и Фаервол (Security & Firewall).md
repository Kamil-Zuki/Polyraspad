# Введение

Данный документ описывает события WebSocket группы **«Мониторинг безопасности и фаервол (Security & Firewall)»**: алерты для SOC/админов (**SR-AUTH-SQ-07**), локальный бан субъектов (**SR-AUTH-SQ-03**) и нарушения периметра Workspace (**SR-AUTH-AC-05**).

Источники SR: [03 - Security Quotas](../../../../01%20-%20Функциональная%20спецификация/Возможности%20сервиса/03%20-%20Защита%20Периметра%20и%20Гостевые%20Квоты%20-%20Security%20Quotas.md), [05 - Access Control](../../../../01%20-%20Функциональная%20спецификация/Возможности%20сервиса/05%20-%20Локальная%20Авторизация%20и%20Политики%20Доступа%20-%20Access%20Control.md).

Канал доставки для чувствительных дашбордов может выделяться отдельным путём (`/ws/v1/auth/security-alerts`) — см. [00 - WebSocket API - Общая информация](00%20-%20WebSocket%20API%20-%20Общая%20информация.md).

# 1. Список событий

| **Код требования** | **Событие** | **Направление** | **Описание** |
| :----------------- | :---------- | :-------------- | :----------- |
| **SR-AUTH-SQ-07** | `security_alert_raised` | Server → Client | Новый инцидент или предупреждение (brute force, новое устройство, hijack и т.д.). |
| **SR-AUTH-SQ-03** | `subject_banned` | Server → Client | IP, CIDR или fingerprint добавлены в локальный deny-list. |
| **SR-AUTH-AC-05** | `geo_policy_violated` | Server → Client | Попытка доступа нарушает geo-/IP-политику Workspace. |

---

# Событие: `security_alert_raised`

| **Название события** | `security_alert_raised` |
| :------------------- | :---------------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-SQ-07**: доставка нового или обновлённого алерта в UI SecOps/владельца без обновления страницы. Запись инцидента и lifecycle ведётся в домене Auth; просмотр списков — отдельными RPC/REST. Справочное представление алерта — [SecurityAlertDto](../DTO/06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-SecurityAlertDto). |
| **DTO** | [SecurityAlertRaisedNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-SecurityAlertRaisedNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **alertId** | string (uuid) | Идентификатор алерта для последующего `ResolveSecurityAlert`. |
| **severity** | string | `LOW` … `CRITICAL`. |
| **type** | string | Тип (как `alertType` в [SecurityAlertDto](../DTO/06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-SecurityAlertDto): `NEW_DEVICE`, `GEO_JUMP`, `BRUTEFORCE_ATTEMPT`, `HIJACK_DETECTED`, …). |
| **summary** | string | Краткий текст для UI. |

**Логика обработки**

1. Инцидент создаётся в домене Auth после цепочки, зависящей от сценария: вход по `ValidateSession` ([01 - Validation Core](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md)), репорт от другого сервиса через `ReportSuspiciousActivity`, либо следствие `BanIpAddress` / `BanDeviceFingerprint` ([04 - Security & Quotas](../gRPC/04%20-%20Защита%20периметра%20и%20Гостевые%20квоты%20(Security%20&%20Quotas).md)).
2. Запись алерта сохраняется; через **SR-AUTH-WS-01** выполняется рассылка `security_alert_raised` в группы ролей/пользователя.
3. Клиент показывает toast или обновляет таблицу; список и резолюция в админке — через `QuerySecurityAlerts` и `ResolveSecurityAlert` ([05 - Audit & Analytics](../gRPC/05%20-%20Аудит%20доступа%20и%20Аналитика%20(Audit%20&%20Analytics).md)), паритет REST — по [REST 00](../REST%20API/00%20-%20REST%20API%20-%20Общая%20информация.md).

---

# Событие: `subject_banned`

| **Название события** | `subject_banned` |
| :------------------- | :--------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-SQ-03**: прозрачность автоматических блокировок для админки; активные сессии нарушителя могут быть отозваны смежными RPC (см. `RevokeSession` в [02 - Session Lifecycle](../gRPC/02%20-%20Управление%20жизненным%20циклом%20сессий%20(Session%20Lifecycle).md)). Запись в списке блокировок — [BlocklistEntryDto](../DTO/06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-BlocklistEntryDto). |
| **DTO** | [SubjectBannedNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-SubjectBannedNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **target** | string | IP, CIDR или fingerprint. |
| **type** | string | `IP_ADDRESS`, `FINGERPRINT`, … |
| **ttlSeconds** | integer | Длительность бана. |
| **reason** | string | Нормализованный код причины (**SR-AUTH-LA-02**). |

**Логика обработки**

1. API Gateway передаёт в микросервис Unary `BanIpAddress` или `BanDeviceFingerprint` ([04 - Security & Quotas](../gRPC/04%20-%20Защита%20периметра%20и%20Гостевые%20квоты%20(Security%20&%20Quotas).md)); при необходимости снимок списка для UI запрашивается через `QueryBlocklist`.
2. После фиксации записи в deny-list публикуется `subject_banned` (**SR-AUTH-WS-01**).

---

# Событие: `geo_policy_violated`

| **Название события** | `geo_policy_violated` |
| :------------------- | :-------------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-AC-05**: попытка входа или удержания контекста в Workspace противоречит политике (регион, VPN, whitelist). У владельца Workspace и/или у пользователя отображается факт нарушения; сам пользователь может получить `session_revoked` / `context_updated` — см. [01 - Session & Access](01%20-%20Управление%20сессиями%20и%20правами%20(Session%20&%20Access%20Management).md). Настройки периметра — [WorkspaceGeoPolicyDto](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md#dto-WorkspaceGeoPolicyDto). |
| **DTO** | [GeoPolicyViolatedNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-GeoPolicyViolatedNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **workspaceId** | string (uuid) | Пространство, для которого сработала политика. |
| **violatorId** | string (uuid) | Пользователь-нарушитель (если применимо). |
| **ipAddress** | string | Источник попытки. |
| **reason** | string | `NOT_IN_WHITELIST`, `REGION_DENIED`, … |

**Логика обработки**

1. Политики периметра читаются/обновляются через `GetWorkspaceGeoPolicy` и `UpdateWorkspaceGeoPolicy` ([06 - Access Control](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md)); персистентность — в Workspace Service.
2. При входе или удержании контекста нарушение оценивается в цепочке `ValidateSession` ([01 - Validation Core](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md)); при необходимости применяется `ForceContextSwitch` ([02 - Session Lifecycle](../gRPC/02%20-%20Управление%20жизненным%20циклом%20сессий%20(Session%20Lifecycle).md)).
3. После фиксации факта нарушения публикуется `geo_policy_violated` (**SR-AUTH-WS-01**).
