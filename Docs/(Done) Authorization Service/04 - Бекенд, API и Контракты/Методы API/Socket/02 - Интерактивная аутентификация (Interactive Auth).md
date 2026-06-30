# Введение

Данный документ описывает события WebSocket группы **«Интерактивная аутентификация (Interactive Auth)»**: сценарии **SR-AUTH-OI-08** (кросс-девайс / QR-Intent), **SR-AUTH-OI-09** (passwordless / magic link) и **SR-AUTH-AC-02** (step-up MFA, в т.ч. push-подтверждение).

Источники требований: [02 - OIDC Infrastructure](../../../../01%20-%20Функциональная%20спецификация/Возможности%20сервиса/02%20-%20Инфраструктура%20OIDC%20и%20Интеграция%20-%20OIDC%20Infrastructure.md), [05 - Access Control](../../../../01%20-%20Функциональная%20спецификация/Возможности%20сервиса/05%20-%20Локальная%20Авторизация%20и%20Политики%20Доступа%20-%20Access%20Control.md).

Общий контракт канала: [00 - WebSocket API - Общая информация](00%20-%20WebSocket%20API%20-%20Общая%20информация.md).

# 1. Список событий

| **Код требования** | **Событие** | **Направление** | **Описание** |
| :----------------- | :---------- | :-------------- | :----------- |
| **SR-AUTH-OI-08** | `qr_login_success` | Server → Client | Подтверждение Intent на доверенном устройстве — завершить обмен и установить Phantom Cookie на исходном клиенте. |
| **SR-AUTH-AC-02** | `mfa_push_approved` | Server → Client | Step-up MFA подтверждён (push / device approval); продолжить критичную операцию или поток логина. |
| **SR-AUTH-OI-09** | `magic_link_clicked` | Server → Client | Одноразовая ссылка использована — вкладка в «режиме ожидания» может завершить exchange. |

---

# Событие: `qr_login_success`

| **Название события** | `qr_login_success` |
| :------------------- | :----------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-OI-08**: мобильное приложение или второй доверенный клиент подтвердил Intent, связанный с QR на десктопе. Событие **не заменяет** установку сессии: дальше клиент выполняет согласованный **HTTP exchange** (REST → соответствующий gRPC на стороне Auth), чтобы получить Cookie. |
| **DTO** | [QrLoginResultNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-QrLoginResultNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **intentId** | string | Временный идентификатор попытки входа. |
| **status** | string | `SUCCESS` или `REJECTED` (при отклонении на телефоне). |
| **oneTimeCode** | string | При успехе — код для обмена на сессию (если применимо в контракте BFF). |

**Логика обработки**

1. BFF/Gateway получает для клиента WSS URL через `BuildWsConnectUrl` (внутри — `IssueTicket`, **SR-AUTH-OT-01…03**); при handshake шлюз вызывает `ValidateTicket` и при необходимости `ValidateSession` ([01 - Validation Core](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md)).
2. Клиент открывает WSS к API Gateway с билетом/подпиской на канал Intent (без полноценной Phantom Cookie).
3. После подтверждения на мобильном устройстве микросервис фиксирует результат (обмен билетов через `IssueTicket` / `ValidateTicket`) и публикует `qr_login_success` (**SR-AUTH-WS-01**).
4. Десктоп выполняет REST `POST …/tickets/validate` → на стороне Auth выполняется `ValidateTicket`; ответ — Set-Cookie.

---

# Событие: `mfa_push_approved`

| **Название события** | `mfa_push_approved` |
| :------------------- | :------------------ |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-AC-02**: подтверждение step-up MFA на доверенном устройстве; используется при логине с риском или внутри сессии перед критичной операцией. Отказ может сопровождаться отдельным событием/кодом в том же DTO (политика продукта). |
| **DTO** | [MfaPushResolutionNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-MfaPushResolutionNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **transactionId** | string | Идентификатор MFA challenge (`mfa_transaction_id` из ответа challenge). |
| **resolution** | string | `APPROVED` или `REJECTED`. |
| **geolocation** | string | Опционально — регион подтверждения для UI. |

**Логика обработки**

1. Клиент инициирует challenge через API Gateway (`POST /auth/mfa/challenge` → Unary `StartStepUpMfaChallenge`, [06 - Access Control](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md); обзор REST — [REST 00](../REST%20API/00%20-%20REST%20API%20-%20Общая%20информация.md)).
2. Пользователь подтверждает на доверенном устройстве; микросервис выполняет `VerifyStepUpMfa`, после успеха публикует `mfa_push_approved` в сессионную группу (**SR-AUTH-WS-01**).
3. UI закрывает модал ожидания и продолжает критичную операцию или выдачу временного scope.

---

# Событие: `magic_link_clicked`

| **Название события** | `magic_link_clicked` |
| :------------------- | :------------------- |
| **Тип** | Входящее сообщение (Server → Client) |
| **Описание** | **SR-AUTH-OI-09**: почтовый клиент открыл одноразовую ссылку; «ожидающая» вкладка получает сигнал завершить обмен без опроса. |
| **DTO** | [MagicLinkProgressNotificationDto](../DTO/08%20-%20WebSocket%20и%20real-time%20уведомления%20(WebSocket%20Notifications).md#dto-MagicLinkProgressNotificationDto) |

**Параметры сообщения** (канонические имена полей JSON — в DTO)

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| **intentId** | string | Идентификатор транзакции логина на исходном клиенте. |
| **exchangeCode** | string | Одноразовый код для обмена на Phantom Cookie (семантика как у билетов **SR-AUTH-OT-02**). |

**Логика обработки**

1. Пользователь запросил magic link; микросервис создаёт одноразовый билет через `IssueTicket` ([01 - Validation Core](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md)).
2. После перехода по ссылке микросервис фиксирует факт и рассылает `magic_link_clicked` в подписанный канал Intent (**SR-AUTH-WS-01**).
3. Исходная вкладка вызывает согласованный REST exchange; на стороне Auth выполняется `ValidateTicket`, устанавливается Phantom Cookie.
