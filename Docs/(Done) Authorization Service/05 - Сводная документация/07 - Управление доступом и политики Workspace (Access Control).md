# Введение

Данный документ описывает DTO группы **Access Control**: персональные API-ключи (PAT) и политики периметра Workspace (Geo-Fencing, IP Whitelisting). **Источник истины по смыслу** — функциональная спецификация ([[01 - Функциональная спецификация/Возможности сервиса/05 - Локальная Авторизация и Политики Доступа - Access Control]]): **SR-AUTH-AC-04** (PAT), **SR-AUTH-AC-05** (geo/IP). Структуры согласованы с REST ([04 - Управление доступом и API-ключами](../REST%20API/04%20-%20Управление%20доступом%20и%20API-ключами.md)) и gRPC ([06 - Управление доступом, PAT и политики Workspace (Access Control)](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md)) — коды SR-AUTH-AC-xx совпадают.

Персистентность политик безопасности Workspace хранится в **Workspace Service**; поля ниже отражают контракт, которым обмениваются BFF и Auth, а Auth — с Workspace по gRPC.

# 1. Список DTO

В таблице ниже перечислены DTO группы Access Control (PAT и geo-политики Workspace).

| **SR** | **Название DTO** | **Назначение** |
| :--- | :--- | :--- |
| SR-AUTH-AC-04 | **CreatePatRequestDto** | Запрос: создание PAT (`name`, `expiresAt`, `scopes`). |
| SR-AUTH-AC-04 | **PatResponseDto** | Ответ: создание PAT с полным токеном (один раз). |
| SR-AUTH-AC-04 | **PatDetailsDto** | Ответ: элемент списка PAT с маскированием. |
| SR-AUTH-AC-04 | **ListPersonalAccessTokensRequest** | Запрос: список ключей (контекст пользователя из сессии). |
| SR-AUTH-AC-04 | **ListPersonalAccessTokensResponse** | Ответ: список `PatDetailsDto`. |
| SR-AUTH-AC-04 | **RevokePersonalAccessTokenRequest** | Запрос: отзыв по `keyId`. |
| SR-AUTH-AC-05 | **WorkspaceGeoPolicyDto** | Ответ/тело: политики Geo/IP для Workspace. |
| SR-AUTH-AC-05 | **GetWorkspaceGeoPolicyRequest** | Запрос: `workspaceId`. |
| SR-AUTH-AC-05 | **UpdateWorkspaceGeoPolicyRequest** | Запрос: `workspaceId` + поля политики. |

---

# DTO: CreatePatRequestDto

## Контекст и назначение

Запрос на выпуск PAT для CLI/CI; соответствует `POST /access/pat` и gRPC `CreatePersonalAccessToken`.

**Назначение:** Запрос (создание).  
**Реализация сущности:** Таблица `api_keys` (хранится только хэш секрета).

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `name` | `string` | Человекочитаемое имя ключа (CI/CD, ноутбук). |
| `expiresAt` | `datetime` | Срок окончания действия (UTC). |
| `scopes` | `array<string>` | Разрешённые области действия ключа. |

## Пример работы (JSON)

Тело запроса `POST /access/pat` / gRPC `CreatePersonalAccessToken`.

```json
{
  "name": "CI deploy",
  "expiresAt": "2027-01-01T00:00:00Z",
  "scopes": ["workspace:read", "api:invoke"]
}
```

---

# DTO: PatResponseDto

## Контекст и назначение

Ответ при успешном создании PAT. Поле `token` возвращается **только при создании**; повторно полный секрет не отдаётся.

**Назначение:** Ответ (создание).  
**Реализация сущности:** Формируется один раз из сгенерированной строки до хэширования в БД.

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `id` | `string` | Идентификатор записи ключа (`key_...`). |
| `name` | `string` | Имя ключа. |
| `token` | `string` | Полное значение `stp_...` (однократно). |
| `expiresAt` | `datetime` | Срок действия. |
| `warning` | `string` | Текст предупреждения о копировании. |

## Пример работы (JSON)

Ответ сразу после создания PAT (поле `token` показывается один раз).

```json
{
  "id": "key_01HZX9",
  "name": "CI deploy",
  "token": "stp_xxxxxxxxxxxxxxxx",
  "expiresAt": "2027-01-01T00:00:00Z",
  "warning": "Сохраните токен — повторно он не будет показан."
}
```

---

# DTO: PatDetailsDto

## Контекст и назначение

Элемент списка для `GET /access/pat` и gRPC `ListPersonalAccessTokens`; секрет всегда маскирован.

**Назначение:** Ответ (элемент списка).  
**Реализация сущности:** Строка `api_keys` без открытого токена.

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `id` | `string` | Идентификатор ключа. |
| `name` | `string` | Имя. |
| `maskedToken` | `string` | Маскированное представление (`stp_9f8b***1a4`). |
| `createdAt` | `datetime` | Создание. |
| `lastUsedAt` | `datetime` | Последнее использование (может быть null). |
| `expiresAt` | `datetime` | Окончание срока. |

## Пример работы (JSON)

Элемент списка `GET /access/pat` / ответ gRPC `ListPersonalAccessTokens`.

```json
{
  "id": "key_01HZX9",
  "name": "CI deploy",
  "maskedToken": "stp_9f8b***1a4",
  "createdAt": "2026-01-10T08:00:00Z",
  "lastUsedAt": "2026-03-20T14:30:00Z",
  "expiresAt": "2027-01-01T00:00:00Z"
}
```

---

<span id="dto-WorkspaceGeoPolicyDto"></span>

# DTO: WorkspaceGeoPolicyDto

## Контекст и назначение

Фрагмент политик периметра Workspace (Geo/IP); источник правды в **Workspace Service** (`security_policies`), Auth читает/проксирует по gRPC.

**Назначение:** Ответ / тело обновления.  
**Реализация сущности:** JSONB в Workspace; кэш/оценка — в Auth при валидации сессии.

## Структура данных

| **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- |
| `workspaceId` | `string` | Идентификатор Workspace. |
| `allowedIps` | `array<string>` | CIDR или отдельные IP. |
| `allowedCountries` | `array<string>` | ISO-коды стран (например, `RU`, `KZ`). |
| `blockVpnProxies` | `boolean` | Блокировка известных VPN/прокси при оценке риска. |

## Пример работы (JSON)

Фрагмент политики периметра Workspace (в ответе `GET` или в теле `PUT`).

```json
{
  "workspaceId": "ws_01HZZZZZZZZZZZZZZZZZZZZZZ",
  "allowedIps": ["203.0.113.0/24", "198.51.100.42"],
  "allowedCountries": ["RU", "KZ"],
  "blockVpnProxies": true
}
```

---

# DTO: GetWorkspaceGeoPolicyRequest / UpdateWorkspaceGeoPolicyRequest

## Контекст и назначение

Запросы для `GET`/`PUT /workspaces/{wsId}/geo-policies` и gRPC `GetWorkspaceGeoPolicy` / `UpdateWorkspaceGeoPolicy`.

## Структура данных

| **DTO** | **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- | :--- |
| **GetWorkspaceGeoPolicyRequest** | `workspaceId` | `string` | Идентификатор Workspace. |
| **UpdateWorkspaceGeoPolicyRequest** | `workspaceId` | `string` | Идентификатор Workspace. |
| **UpdateWorkspaceGeoPolicyRequest** | `allowedIps` | `array<string>` | Как в `WorkspaceGeoPolicyDto`. |
| **UpdateWorkspaceGeoPolicyRequest** | `allowedCountries` | `array<string>` | Как в `WorkspaceGeoPolicyDto`. |
| **UpdateWorkspaceGeoPolicyRequest** | `blockVpnProxies` | `boolean` | Как в `WorkspaceGeoPolicyDto`. |

## Пример работы (JSON)

**Запрос** `GetWorkspaceGeoPolicy` / параметры `GET /workspaces/{wsId}/geo-policies`:

```json
{
  "workspaceId": "ws_01HZZZZZZZZZZZZZZZZZZZZZZ"
}
```

**Тело** `UpdateWorkspaceGeoPolicy` / `PUT /workspaces/{wsId}/geo-policies`:

```json
{
  "workspaceId": "ws_01HZZZZZZZZZZZZZZZZZZZZZZ",
  "allowedIps": ["203.0.113.0/24"],
  "allowedCountries": ["RU"],
  "blockVpnProxies": false
}
```

---

# DTO: ListPersonalAccessTokensRequest / ListPersonalAccessTokensResponse / RevokePersonalAccessTokenRequest

## Контекст и назначение

Вспомогательные сообщения для gRPC `ListPersonalAccessTokens` и `RevokePersonalAccessToken` (REST: список без тела, отзыв по `keyId` в пути).

## Структура данных

| **DTO** | **Имя поля (JSON)** | **Тип данных** | **Описание** |
| :--- | :--- | :--- | :--- |
| **ListPersonalAccessTokensRequest** | — | — | Обычно пустое тело; пользователь из контекста вызова. |
| **ListPersonalAccessTokensResponse** | `items` | `array<PatDetailsDto>` | Список ключей (или плоский массив по соглашению proto). |
| **RevokePersonalAccessTokenRequest** | `keyId` | `string` | Идентификатор ключа (`key_...`). |

## Пример работы (JSON)

**Ответ** списка PAT (обёртка с `items`):

```json
{
  "items": [
    {
      "id": "key_01HZX9",
      "name": "Ноутбук",
      "maskedToken": "stp_9f8b***1a4",
      "createdAt": "2026-01-10T08:00:00Z",
      "lastUsedAt": null,
      "expiresAt": "2027-01-01T00:00:00Z"
    }
  ]
}
```

**Запрос** отзыва по `keyId` (тело gRPC; в REST — идентификатор в пути):

```json
{
  "keyId": "key_01HZX9"
}
```
