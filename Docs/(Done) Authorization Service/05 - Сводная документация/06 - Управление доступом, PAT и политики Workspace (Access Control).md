# Введение

Данная группа gRPC-методов покрывает **программный доступ** (Personal Access Tokens — PAT) для CLI/CI/CD, **локальный Step-up MFA** (SR-AUTH-AC-02: повторное подтверждение личности внутри уже установленной Phantom-сессии) и **Enterprise-политики периметра Workspace** (Geo-Fencing, IP Whitelisting). Нормативное описание сценариев и принципов — в **функциональной спецификации**; отображение на HTTP/JSON для BFF и клиентов задают REST-контракт и отдельная документация DTO (папка `DTO/`).

Эти RPC вызываются API Gateway (BFF); публичные маршруты для той же логики перечислены в REST ([04 - Управление доступом и API-ключами](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md), [01 - Аутентификация и OIDC, SR-AUTH-AC-02](../REST%20API/01%20-%20Аутентификация%20и%20OIDC.md)).

Персистентное хранение полей `security_policies` (JSONB) относится к **Workspace Service**. `Authorization Service` не дублирует источник правды: при чтении и обновлении geo-политик выполняется **синхронный gRPC-вызов** в контракт Workspace Service. Точные имена RPC и сообщений protobuf — в документации **Workspace Service**; ниже зафиксированы логические имена и поведение со стороны Auth.

Важно: **STEOS ID**, внешние IdP и объектные хранилища не являются частью данного файла; при упоминании интеграций указывается принятый транспорт (gRPC, RabbitMQ).

Структуры JSON для BFF: [03 - Аутентификация и OIDC (DTO)](../DTO/03%20-%20Аутентификация%20и%20OIDC%20(Auth%20&%20OIDC).md), [07 - Управление доступом и политики Workspace (DTO)](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md).

# 1. Список методов

Перечень процедур для PAT, Step-up MFA и политик периметра Workspace. Соответствие требованиям SR-AUTH-AC-xx — по функциональной спецификации; имена HTTP-маршрутов в таблице ниже — для сопоставления с REST, не как отдельная норма.

| Код требования | gRPC Метод | Тип RPC | Описание |
| :--- | :--- | :---: | :--- |
| SR-AUTH-AC-04 | `CreatePersonalAccessToken` | Unary | Создание PAT: хэш в `api_keys`, секрет в ответе один раз. |
| SR-AUTH-AC-04 | `ListPersonalAccessTokens` | Unary | Список ключей пользователя с маскированием. |
| SR-AUTH-AC-04 | `RevokePersonalAccessToken` | Unary | Отзыв PAT по идентификатору ключа (`keyId` в REST/BFF). |
| SR-AUTH-AC-02 | `StartStepUpMfaChallenge` | Unary | SR-AUTH-AC-02: challenge step-up MFA; BFF — `POST /auth/mfa/challenge`. |
| SR-AUTH-AC-02 | `VerifyStepUpMfa` | Unary | SR-AUTH-AC-02: verify step-up MFA; BFF — `POST /auth/mfa/verify`. |
| SR-AUTH-AC-05 | `GetWorkspaceGeoPolicy` | Unary | Чтение политик через gRPC в Workspace Service. |
| SR-AUTH-AC-05 | `UpdateWorkspaceGeoPolicy` | Unary | Запись политик (Owner) через gRPC в Workspace Service + аудит в Auth. |

---

<span id="grpc-StartStepUpMfaChallenge"></span>

# SR-AUTH-AC-02: Step-up MFA — Challenge: StartStepUpMfaChallenge

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-02: Step-Up MFA для высокорисковых действий]] (принципы: повторное подтверждение внутри сессии, короткоживущий scope-результат, несколько каналов фактора).

**Сопоставление с REST и JSON (справочно):** [01 - Аутентификация и OIDC (REST)](../REST%20API/01%20-%20Аутентификация%20и%20OIDC.md); имена и форма JSON на периметре — [03 - Аутентификация и OIDC (DTO)](../DTO/03%20-%20Аутентификация%20и%20OIDC%20(Auth%20&%20OIDC).md).

Вызывается при попытке выполнить высококритичное действие в уже авторизованной сессии (см. сценарии в функциональной спецификации): создаётся короткоживущая MFA-транзакция в Redis, инициируется отправка SMS/Push или подготовка UI для TOTP. Идентификатор транзакции возвращается клиенту для шага `VerifyStepUpMfa`.

| Сигнатура | `rpc StartStepUpMfaChallenge(StartStepUpMfaChallengeRequest) returns (StartStepUpMfaChallengeResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `StartStepUpMfaChallengeRequest` (`mfa_method` — выбранный фактор; опционально `intent` или `resource_hint` для аудита критичной операции) |
| **Сообщение ответа** | `StartStepUpMfaChallengeResponse` (`mfa_transaction_id`, маскированный контакт, метаданные канала — семантика как у challenge в [03 - DTO](../DTO/03%20-%20Аутентификация%20и%20OIDC%20(Auth%20&%20OIDC).md)) |

Имена полей в protobuf — `snake_case`; для JSON-совместимости с BFF допускается `json_name`, совпадающий с полями REST-контракта.

## Логика обработки запроса

1. Убедиться, что вызов идёт в контексте валидной Phantom-сессии (идентификация субъекта из метаданных gRPC / внутреннего контекста шлюза).
2. Проверить допустимость выбранного `mfa_method` для пользователя (политика продукта, наличие TOTP-секрета, телефона и т.д.).
3. Создать запись MFA-транзакции в Redis с TTL, лимитом попыток verify и привязкой к `session_id`.
4. Для SMS/Push — инициировать доставку через **STEOS ID** или согласованный канал (синхронный **gRPC**/HTTP S2S к IdP по принятой интеграции); для TOTP — не отправлять код, только вернуть метаданные для клиента.
5. Записать событие аудита (например, `MFA_STEP_UP_CHALLENGE_STARTED`) в `access_audit_logs` при политике логирования чувствительных операций.
6. Вернуть `mfa_transaction_id` и при необходимости маскированный контакт.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **INVALID_ARGUMENT** | Не указан или неподдерживаемый `mfa_method`. |
| **FAILED_PRECONDITION** | У пользователя не настроен выбранный фактор. |
| **RESOURCE_EXHAUSTED** | Слишком частые challenge-запросы (throttling по пользователю/сессии). |

---

<span id="grpc-VerifyStepUpMfa"></span>

# SR-AUTH-AC-02: Step-up MFA — Verify: VerifyStepUpMfa

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-02: Step-Up MFA для высокорисковых действий]] (успешная проверка завершает step-up и выдаёт временное право на класс операции в смысле требований).

**Сопоставление с REST и JSON (справочно):** логика verify в [01 - Аутентификация и OIDC (REST)](../REST%20API/01%20-%20Аутентификация%20и%20OIDC.md); для полей ввода кода — [03 - Аутентификация и OIDC (Auth & OIDC)](../DTO/03%20-%20Аутентификация%20и%20OIDC%20(Auth%20&%20OIDC).md) (в Step-up вместо `mfa_token` логина используется **`mfa_transaction_id`** из ответа challenge).

Проверяет одноразовый код (или подтверждение Push) и при успехе записывает в кэш сессии отметку `mfa_verified_at` (и при необходимости TTL «окна доверия», например 5 минут), чтобы API Gateway мог прокидывать заголовок `X-Mfa-Verified: true` на последующие запросы.

| Сигнатура | `rpc VerifyStepUpMfa(VerifyStepUpMfaRequest) returns (VerifyStepUpMfaResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `VerifyStepUpMfaRequest` (`mfa_transaction_id`, `code` — OTP/TOTP; при Push — согласованное поле подтверждения) |
| **Сообщение ответа** | `VerifyStepUpMfaResponse` (признак успеха и сообщение; JSON-обёртка на BFF — [03 - DTO](../DTO/03%20-%20Аутентификация%20и%20OIDC%20(Auth%20&%20OIDC).md)) |

## Логика обработки запроса

1. Загрузить MFA-транзакцию из Redis по `mfa_transaction_id`; убедиться, что она принадлежит текущей сессии и не истекла.
2. Проверить лимит попыток (например, не более 3 неверных вводов — затем инвалидация транзакции).
3. Валидировать код: локально для TOTP либо запрос к **STEOS ID** для SMS/Push по принятой интеграции.
4. При успехе: обновить hot-state сессии в Redis — `mfa_verified_at = now()`, окно действия согласно политике; удалить или пометить использованной MFA-транзакцию.
5. Записать аудит `MFA_STEP_UP_VERIFIED` (или `FAILED` при неверном коде).
6. Вернуть `VerifyStepUpMfaResponse`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **INVALID_ARGUMENT** | Пустой `mfa_transaction_id` или код. |
| **NOT_FOUND** | Транзакция не найдена или срок истёк. |
| **PERMISSION_DENIED** | Код неверен (альтернатива — **FAILED_PRECONDITION** по внутренней конвенции; зафиксировать единообразно в proto и BFF). |
| **RESOURCE_EXHAUSTED** | Исчерпан лимит попыток (аналог REST **429**). |

---

<span id="grpc-CreatePersonalAccessToken"></span>

# SR-AUTH-AC-04: Создание PAT: CreatePersonalAccessToken

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-04: Персональные API-ключи (PAT)]].  
**Сопоставление с REST и JSON (справочно):** `POST /access/pat` — [04 - Управление доступом и API-ключами (REST)](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md); поля тела и ответа для BFF — [07 - Управление доступом и политики Workspace (DTO)](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md).

Выпуск долгоживущего токена для скриптов и CI вместо Phantom Cookie. Секрет хранится только в виде хэша в таблице `api_keys`.

| Сигнатура | `rpc CreatePersonalAccessToken(CreatePersonalAccessTokenRequest) returns (CreatePersonalAccessTokenResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `CreatePersonalAccessTokenRequest` (`name`, `expires_at`, `scopes`; `json_name` может совпадать с полями, описанными в [07 - Управление доступом (DTO)](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md)) |
| **Сообщение ответа** | `CreatePersonalAccessTokenResponse` (полный секрет **один раз**, идентификатор ключа, предупреждение) |

## Логика обработки запроса

1. Проверить лимит количества PAT на аккаунт и валидность `scopes` против платформенного реестра (при необходимости — запрос к **Platform Service** по **gRPC**).
2. Сгенерировать строку токена (`stp_...`), вычислить хэш, сохранить запись в `api_keys` без хранения открытого значения.
3. Записать событие аудита `API_KEY_CREATED` в `access_audit_logs`.
4. Вернуть открытый токен в ответе единожды.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **INVALID_ARGUMENT** | Пустое имя или недопустимые `scopes`. |
| **RESOURCE_EXHAUSTED** | Превышен лимит PAT на аккаунт. |

---

<span id="grpc-ListPersonalAccessTokens"></span>

# SR-AUTH-AC-04: Список PAT: ListPersonalAccessTokens

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-04: Персональные API-ключи (PAT)]].  
**Сопоставление с REST и JSON (справочно):** `GET /access/pat` — [04 - REST](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md); маскирование и поля элементов списка — [07 - DTO](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md).

Возвращает все активные PAT пользователя с маскированием секрета (паритет REST `GET /access/pat`).

| Сигнатура | `rpc ListPersonalAccessTokens(ListPersonalAccessTokensRequest) returns (ListPersonalAccessTokensResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `ListPersonalAccessTokensRequest` (пустой или фильтр; user из контекста вызова) |
| **Сообщение ответа** | `ListPersonalAccessTokensResponse` (повторяющееся сообщение `PersonalAccessTokenSummary` — маскированный секрет, метаданные; отображение в JSON см. [07 - DTO](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md)) |

## Логика обработки запроса

1. Выбрать строки из `api_keys` по `global_steos_id` текущего пользователя.
2. Отдать маскированные значения, `created_at`, `last_used_at`, `expires_at`.
3. Никогда не возвращать полный секрет.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **PERMISSION_DENIED** | Попытка запросить чужие ключи. |

---

<span id="grpc-RevokePersonalAccessToken"></span>

# SR-AUTH-AC-04: Отзыв PAT: RevokePersonalAccessToken

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-04: Персональные API-ключи (PAT)]].  
**Сопоставление с REST и JSON (справочно):** `DELETE /access/pat/{keyId}` — [04 - REST](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md); поле идентификатора ключа на периметре — [07 - DTO](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md).

Удаляет хэш из `api_keys` и инвалидирует кэш шлюза при необходимости (паритет REST `DELETE /access/pat/{keyId}`).

| Сигнатура | `rpc RevokePersonalAccessToken(RevokePersonalAccessTokenRequest) returns (RevokePersonalAccessTokenResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `RevokePersonalAccessTokenRequest` (`key_id` в proto; `json_name` согласован с BFF) |
| **Сообщение ответа** | `RevokePersonalAccessTokenResponse` (признак успеха; при необходимости — `google.protobuf.Empty`) |

## Логика обработки запроса

1. Проверить владение ключом.
2. Удалить запись из `api_keys`; очистить записи кэша API Gateway, если PAT кэшировался.
3. Записать аудит `API_KEY_REVOKED`.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **NOT_FOUND** | Ключ не существует или принадлежит другому пользователю. |

---

<span id="grpc-GetWorkspaceGeoPolicy"></span>

# SR-AUTH-AC-05: Чтение geo-политик: GetWorkspaceGeoPolicy

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-05: Geo-Policies и IP Whitelisting Workspace]].  
**Сопоставление с REST и JSON (справочно):** `GET /workspaces/{wsId}/geo-policies` — [04 - REST](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md); поля политики в JSON — [07 - DTO](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md).

Возвращает снимок geo/IP-политики для `workspace_id`. Источник правды — **Workspace Service**; Auth вызывает **gRPC** (условные имена RPC в proto Workspace: например `GetSecurityPolicies` / `GetWorkspaceSettings` — уточнить в репозитории Workspace).

| Сигнатура | `rpc GetWorkspaceGeoPolicy(GetWorkspaceGeoPolicyRequest) returns (GetWorkspaceGeoPolicyResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `GetWorkspaceGeoPolicyRequest` (`workspace_id`) |
| **Сообщение ответа** | `GetWorkspaceGeoPolicyResponse` (вложенное сообщение `WorkspaceGeoPolicy`: списки разрешённых IP/CIDR, стран, флаги VPN и т.д.; маппинг в JSON — [07 - DTO](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md)) |

## Логика обработки запроса

1. Проверить, что вызывающий имеет право просматривать политики (участник Workspace или выше).
2. Вызвать **Workspace Service** по **gRPC** и получить фрагмент настроек, соответствующий geo/IP whitelist (поле `security_policies` или эквивалент).
3. Смапить ответ в `WorkspaceGeoPolicy` (внутри `GetWorkspaceGeoPolicyResponse`) и вернуть BFF.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **NOT_FOUND** | Workspace не существует (проксируется из Workspace Service). |
| **PERMISSION_DENIED** | Нет доступа к настройкам пространства. |

---

<span id="grpc-UpdateWorkspaceGeoPolicy"></span>

# SR-AUTH-AC-05: Обновление geo-политик: UpdateWorkspaceGeoPolicy

## Общая информация

**Источник истины:** [[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control#SR-AUTH-AC-05: Geo-Policies и IP Whitelisting Workspace]].  
**Сопоставление с REST и JSON (справочно):** `PUT /workspaces/{wsId}/geo-policies` — [04 - REST](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md); тело и ответ на BFF — [07 - DTO](../DTO/07%20-%20Управление%20доступом%20и%20политики%20Workspace%20(Access%20Control).md).

Доступно только **workspace_owner**. Запись выполняется в **Workspace Service** по **gRPC**; **Authorization Service** фиксирует WORM-событие `WORKSPACE_POLICY_CHANGED` и при политике продукта может инициировать проверку активных сессий (внутренняя логика Auth) или публикацию уведомления в **RabbitMQ** для асинхронной инвалидации сессий, нарушающих новый периметр.

| Сигнатура | `rpc UpdateWorkspaceGeoPolicy(UpdateWorkspaceGeoPolicyRequest) returns (UpdateWorkspaceGeoPolicyResponse)` |
| :--- | :--- |
| **Сообщение запроса** | `UpdateWorkspaceGeoPolicyRequest` (`workspace_id` + вложенное `WorkspaceGeoPolicy` с новыми значениями) |
| **Сообщение ответа** | `UpdateWorkspaceGeoPolicyResponse` (подтверждённое состояние `WorkspaceGeoPolicy`) |

## Логика обработки запроса

1. Проверить роль `workspace_owner` для данного `workspace_id` (через контекст сессии и при необходимости подтверждение членства по **gRPC** в **Workspace Service**).
2. Вызвать **Workspace Service** по **gRPC** для обновления JSONB `security_policies` (или отдельного поля geo-policies).
3. Записать событие в `access_audit_logs` (`WORKSPACE_POLICY_CHANGED`).
4. При необходимости опубликовать событие в **RabbitMQ** для фоновой ревизии сессий (тема/маршрутизация — по платформенной конвенции; потребители — воркеры Auth).
5. Вернуть подтверждённое состояние политики клиенту.

## Статус-коды gRPC при ошибках

| Статус-код | Описание ошибки |
| :--- | :--- |
| **PERMISSION_DENIED** | Пользователь не Owner. |
| **NOT_FOUND** | Workspace не существует. |
| **FAILED_PRECONDITION** | Отклонено валидацией Workspace (например, неверный CIDR). |
