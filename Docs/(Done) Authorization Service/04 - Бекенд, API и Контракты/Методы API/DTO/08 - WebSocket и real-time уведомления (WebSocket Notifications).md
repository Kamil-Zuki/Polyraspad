# Введение

Данный документ описывает **DTO полезной нагрузки** сообщений WebSocket (Server → Client), доставляемых через **API Gateway** после доменных решений микросервиса **Authorization Service**. Сериализация на периметре (JSON) выполняется агрегатором; канонические имена типов и полей совпадают с контрактом, отдаваемым клиенту.

**Связь с Socket API:** [00 - WebSocket API - Общая информация](../Socket/00%20-%20WebSocket%20API%20-%20Общая%20информация.md). Детализация по группам событий — [01](../Socket/01%20-%20Управление%20сессиями%20и%20правами%20(Session%20&%20Access%20Management).md)–[04](../Socket/04%20-%20Асинхронные%20задачи%20и%20Квоты%20(Async%20Jobs%20&%20Quotas).md). Имена событий и маршрутизация — в этих документах; ниже — только структура DTO полезной нагрузки по одному разделу на тип.

<span id="dto-SessionRevokedNotificationDto"></span>

# DTO: SessionRevokedNotificationDto

## Контекст и назначение

Тело сообщения WebSocket при принудительном отзыве сессии; событие `session_revoked` (**SR-AUTH-SM-06**, **SR-AUTH-OI-04**). См. также [SecurityAlertDto](06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-SecurityAlertDto) для справочных алертов (другой канал данных).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание**                                                                                             |
| :------------------ | :------------- | :------------------------------------------------------------------------------------------------------- |
| `sessionId`         | `uuid`         | Идентификатор отозванной сессии.                                                                         |
| `reasonCode`        | `string`       | Код причины (`ADMIN_REVOKED`, `TERMINATED_FROM_OTHER_DEVICE`, `SECURITY_BREACH`, `OIDC_BACKCHANNEL`, …). |
| `message`           | `string`       | Текст для отображения пользователю.                                                                      |
| `timestamp`         | `datetime`     | Время отзыва (UTC).                                                                                      |

## Пример работы (JSON)

Полезная нагрузка при событии `session_revoked`.

```json
{
  "sessionId": "a1b2c3d4-e5f6-7777-8888-9999aaaabbbb",
  "reasonCode": "ADMIN_REVOKED",
  "message": "Сессия завершена администратором.",
  "timestamp": "2026-04-16T12:00:00Z"
}
```

---

<span id="dto-ContextUpdatedNotificationDto"></span>

# DTO: ContextUpdatedNotificationDto

## Контекст и назначение

Сигнал-триггер о смене серверного контекста без полного логина; событие `context_updated` (**SR-AUTH-SM-10**). Полный снимок — через [SessionContextDto](02%20-%20Управление%20сессиями%20(Session%20Management).md#dto-SessionContextDto) (`GetSessionContext` / `GET /sessions/me`).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `userId` | `uuid` | Глобальный идентификатор пользователя. |
| `triggerEvent` | `string` | Причина (`ROLES_CHANGED`, `SUBSCRIPTION_UPGRADED`, …). |
| `timestamp` | `datetime` | Время фиксации (UTC). |

## Пример работы (JSON)

Полезная нагрузка при событии `context_updated`.

```json
{
  "userId": "42424242-4242-4242-4242-424242424242",
  "triggerEvent": "ROLES_CHANGED",
  "timestamp": "2026-04-16T12:00:00Z"
}
```

---

<span id="dto-ImpersonationNotificationDto"></span>

# DTO: ImpersonationNotificationDto

## Контекст и назначение

Уведомление пользователя о старте или завершении имперсонации поддержки; события `impersonation_started` / `impersonation_stopped` (**SR-AUTH-SM-05**). Различие сценариев — полем `action`.

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `ticketId` | `string` | Номер тикета helpdesk / обоснование. |
| `supportAgentName` | `string` | Отображаемое имя агента (маскирование по политике). |
| `action` | `string` | `STARTED` или `STOPPED`. |
| `timestamp` | `datetime` | Время события (UTC). |

## Пример работы (JSON)

Полезная нагрузка при событии `impersonation_started`.

```json
{
  "ticketId": "HD-1024",
  "supportAgentName": "Support Agent",
  "action": "STARTED",
  "timestamp": "2026-04-16T12:00:00Z"
}
```

---

<span id="dto-QrLoginResultNotificationDto"></span>

# DTO: QrLoginResultNotificationDto

## Контекст и назначение

Результат кросс-девайсного подтверждения Intent (**SR-AUTH-OI-08**); событие `qr_login_success`. Установка Phantom Cookie выполняется отдельным REST/gRPC exchange (`ValidateTicket`).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `intentId` | `string` | Идентификатор попытки входа (Intent). |
| `status` | `string` | `SUCCESS` или `REJECTED`. |
| `oneTimeCode` | `string` | При `SUCCESS` — код обмена на сессию (если предусмотрено контрактом BFF). |

## Пример работы (JSON)

Полезная нагрузка при событии `qr_login_success`.

```json
{
  "intentId": "intent-abc-123",
  "status": "SUCCESS",
  "oneTimeCode": "otl_xxxxxxxx"
}
```

---

<span id="dto-MfaPushResolutionNotificationDto"></span>

# DTO: MfaPushResolutionNotificationDto

## Контекст и назначение

Разрешение step-up MFA (**SR-AUTH-AC-02**); событие `mfa_push_approved`. Связано с [MfaChallengeDto](03%20-%20Аутентификация%20и%20OIDC%20(Auth%20&%20OIDC).md#dto-MfaChallengeDto) / challenge flow.

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `transactionId` | `string` | Идентификатор MFA challenge (`mfa_transaction_id`). |
| `resolution` | `string` | `APPROVED` или `REJECTED`. |
| `geolocation` | `string` | Опционально — регион подтверждения для UI. |

## Пример работы (JSON)

Полезная нагрузка при событии `mfa_push_approved`.

```json
{
  "transactionId": "mfa-txn-001",
  "resolution": "APPROVED",
  "geolocation": "EU-WEST"
}
```

---

<span id="dto-MagicLinkProgressNotificationDto"></span>

# DTO: MagicLinkProgressNotificationDto

## Контекст и назначение

Прогресс passwordless / magic link (**SR-AUTH-OI-09**) для «ожидающей» вкладки; событие `magic_link_clicked`.

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `intentId` | `string` | Идентификатор транзакции логина на исходном клиенте. |
| `exchangeCode` | `string` | Одноразовый код для обмена (семантика билета **SR-AUTH-OT-02**). |

## Пример работы (JSON)

Полезная нагрузка при событии `magic_link_clicked`.

```json
{
  "intentId": "intent-ml-456",
  "exchangeCode": "ot_xxxxxxxx"
}
```

---

<span id="dto-SecurityAlertRaisedNotificationDto"></span>

# DTO: SecurityAlertRaisedNotificationDto

## Контекст и назначение

Push-обёртка для нового/актуализированного инцидента; событие `security_alert_raised` (**SR-AUTH-SQ-07**). Поле `type` по смыслу соответствует `alertType` в [SecurityAlertDto](06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-SecurityAlertDto) (сокращённая полезная нагрузка WebSocket).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `alertId` | `uuid` | Идентификатор алерта (`ResolveSecurityAlert`). |
| `severity` | `string` | `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`. |
| `type` | `string` | Тип угрозы (как `alertType` в `SecurityAlertDto`: `NEW_DEVICE`, `GEO_JUMP`, `BRUTEFORCE_ATTEMPT`, `HIJACK_DETECTED`, …). |
| `summary` | `string` | Краткое описание для UI. |

## Пример работы (JSON)

Полезная нагрузка при событии `security_alert_raised`.

```json
{
  "alertId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "severity": "HIGH",
  "type": "NEW_DEVICE",
  "summary": "Вход с нового устройства."
}
```

---

<span id="dto-SubjectBannedNotificationDto"></span>

# DTO: SubjectBannedNotificationDto

## Контекст и назначение

Уведомление о записи в локальном deny-list; событие `subject_banned` (**SR-AUTH-SQ-03**). Коды в поле `reason` — из того же перечня, что и `reasonCode` в [BlocklistEntryDto](06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-BlocklistEntryDto) (см. **SR-AUTH-LA-02**).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `target` | `string` | IP, CIDR или fingerprint. |
| `type` | `string` | `IP_ADDRESS`, `FINGERPRINT`, … |
| `ttlSeconds` | `integer` | Длительность бана. |
| `reason` | `string` | Код причины (**SR-AUTH-LA-02**). |

## Пример работы (JSON)

Полезная нагрузка при событии `subject_banned`.

```json
{
  "target": "203.0.113.10",
  "type": "IP_ADDRESS",
  "ttlSeconds": 3600,
  "reason": "BRUTE_FORCE"
}
```

---

<span id="dto-GeoPolicyViolatedNotificationDto"></span>

# DTO: GeoPolicyViolatedNotificationDto

## Контекст и назначение

Нарушение периметра Workspace; событие `geo_policy_violated` (**SR-AUTH-AC-05**). Канонические правила — [WorkspaceGeoPolicyDto](07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md#dto-WorkspaceGeoPolicyDto).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `workspaceId` | `uuid` | Пространство, для которого сработала политика. |
| `violatorId` | `uuid` | Пользователь, совершивший попытку. |
| `ipAddress` | `string` | Источник попытки. |
| `reason` | `string` | `NOT_IN_WHITELIST`, `REGION_DENIED`, `VPN_PROXY_DETECTED`, … |

## Пример работы (JSON)

Полезная нагрузка при событии `geo_policy_violated`.

```json
{
  "workspaceId": "cccccccc-cccc-cccc-cccc-cccccccccccc",
  "violatorId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
  "ipAddress": "198.51.100.5",
  "reason": "REGION_DENIED"
}
```

---

<span id="dto-GuestMergedNotificationDto"></span>

# DTO: GuestMergedNotificationDto

## Контекст и назначение

Завершение асинхронного слияния данных гостя (**SR-AUTH-GQ-04**); событие `guest_merged`. См. также [GuestMergeEventDto](04%20-%20Управление%20гостевым%20доступом%20(Guest%20Sessions).md#dto-GuestMergeEventDto) (RabbitMQ).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `userId` | `uuid` | Постоянный глобальный пользователь. |
| `oldGuestId` | `string` | Идентификатор конвертированной гостевой сессии. |
| `status` | `string` | `COMPLETED`, `PARTIAL_SUCCESS`, `FAILED`. |
| `message` | `string` | Резюме для пользователя. |

## Пример работы (JSON)

Полезная нагрузка при событии `guest_merged`.

```json
{
  "userId": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
  "oldGuestId": "guest-shadow-789",
  "status": "COMPLETED",
  "message": "Данные гостевой сессии перенесены в профиль."
}
```

---

<span id="dto-AuditExportCompletedNotificationDto"></span>

# DTO: AuditExportCompletedNotificationDto

## Контекст и назначение

Готовность фоновой выгрузки WORM; событие `audit_export_completed` (**SR-AUTH-LA-05**). Терминальные значения `status` и поле ошибки согласованы с [ExportArchiveJobStatusDto](06%20-%20Аудит%20и%20безопасность%20(Audit%20&%20Security).md#dto-ExportArchiveJobStatusDto) (опрос `GetExportArchiveJobStatus`).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `jobId` | `string` | Идентификатор задачи (как в `ExportArchiveJobStatusDto` / ответе `ExportAuditArchive`). |
| `status` | `string` | `SUCCEEDED` или `FAILED` (как терминальные состояния в опросе статуса). |
| `downloadUrl` | `string` | Presigned URL при `SUCCEEDED`. |
| `errorMessage` | `string` | При `FAILED` — причина (как в `ExportArchiveJobStatusDto`). |

## Пример работы (JSON)

Полезная нагрузка при событии `audit_export_completed`.

```json
{
  "jobId": "export_job_01HZY1",
  "status": "SUCCEEDED",
  "downloadUrl": "https://storage.example/presigned/audit.zip",
  "errorMessage": null
}
```

---

<span id="dto-QuotaWarningNotificationDto"></span>

# DTO: QuotaWarningNotificationDto

## Контекст и назначение

Мягкое предупреждение по гостевой квоте (**SR-AUTH-SQ-02**); событие `quota_exceeded_warning`. Агрегированное состояние — [GuestQuotaStatusDto](04%20-%20Управление%20гостевым%20доступом%20(Guest%20Sessions).md#dto-GuestQuotaStatusDto).

**Назначение:** Уведомление (Server → Client).  
**Реализация сущности:** N/A (полезная нагрузка WebSocket).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `quotaType` | `string` | Идентификатор лимита (фича). |
| `remaining` | `integer` | Остаток единиц. |
| `thresholdReached` | `boolean` | Порог предупреждения (например, 90%). |

## Пример работы (JSON)

Полезная нагрузка при событии `quota_exceeded_warning`.

```json
{
  "quotaType": "guest_api_calls",
  "remaining": 50,
  "thresholdReached": true
}
```

---
