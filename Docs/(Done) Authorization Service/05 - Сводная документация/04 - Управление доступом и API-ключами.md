# Введение

Данная группа REST API методов предназначена для управления локальным доступом и программной автоматизацией. Она реализует концепции Enterprise-управления: выдачу долгоживущих токенов (Personal Access Tokens - PAT) для CLI и скриптов разработчиков, вычисление эффективных прав пользователя (для динамической адаптации SPA интерфейса) и управление настройками периметра на уровне рабочего пространства (Geo-Fencing & IP Whitelisting).

# 1. Список эндпоинтов

Ниже приведен список методов REST API для управления доступом и API-ключами.

| Код требования | Метод  | Эндпоинт                          | Назначение                                                  |
| :------------- | :----: | :-------------------------------- | :---------------------------------------------------------- |
| SR-AUTH-AC-04  |  POST  | `/access/pat`                     | Создание персонального API-ключа (PAT) для разработчиков.   |
| SR-AUTH-AC-04  |  GET   | `/access/pat`                     | Получение списка всех выпущенных PAT пользователя.          |
| SR-AUTH-AC-04  | DELETE | `/access/pat/{keyId}`             | Отзыв персонального API-ключа (Revocation).                 |
| SR-AUTH-AC-01  |  POST  | `/access/permissions/check`       | Проверка наличия права у пользователя (ABAC/RBAC) для UI.   |
| SR-AUTH-AC-05  |  GET   | `/workspaces/{wsId}/geo-policies` | Просмотр настроек Geo-Fencing (откуда разрешен вход).       |
| SR-AUTH-AC-05  |  PUT   | `/workspaces/{wsId}/geo-policies` | Настройка белого списка IP-адресов и регионов (Owner only). |

---

# SR-AUTH-AC-04: Создание ключа доступа: Create PAT

## Общая информация

Разработчики (клиенты Платформы) могут генерировать Personal Access Tokens (PAT) для использования их в своих скриптах, пайплайнах (CI/CD) или CLI-утилитах вместо Phantom Cookies. Токен является статическим и передается в заголовке `Authorization: Bearer <PAT>`.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | CreatePatRequestDto (название, дата истечения `expiresAt`, скоупы `scopes`) |
| **DTO успешного ответа** | PatResponseDto (с не замаскированным ключом) |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

*   BFF аутентифицирует пользователя по Phantom Cookie и валидирует тело (`name`, `expiresAt`, `scopes`).
*   BFF вызывает gRPC [`CreatePersonalAccessToken`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-CreatePersonalAccessToken).
*   Полный токен возвращается **один раз** в теле ответа (`PatResponseDto`).

## Успешный ответ

```json
{
  "success": true,
  "data": {
    "id": "key_8891a",
    "name": "CI/CD Deployment Key",
    "token": "stp_9f8b2c1a4e5d6...",
    "expiresAt": "2026-12-31T23:59:59Z",
    "warning": "Скопируйте токен. Он больше не будет показан."
  }
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | Не указано название или запрошены невалидные скоупы. |
| **403 Forbidden** | Превышен лимит созданных ключей на аккаунт. |

---

# SR-AUTH-AC-04: Список ключей и Отзыв: List & Revoke

## Общая информация

Методы для страницы "Разработчикам -> API Ключи" в личном кабинете. Позволяют пользователю видеть список активных интеграций и мгновенно отзывать скомпрометированные ключи.

| Тип метода | GET / DELETE |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | Массив объектов `PatDetailsDto` (для GET) |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `keyId` | `string` | В методе `DELETE`: Уникальный идентификатор ключа в БД (не сам токен). |

## Логика обработки запроса

*   BFF определяет пользователя по Cookie.
*   **`GET`:** BFF вызывает gRPC [`ListPersonalAccessTokens`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-ListPersonalAccessTokens); ответ — маскированные `PatDetailsDto`.
*   **`DELETE`:** BFF вызывает gRPC [`RevokePersonalAccessToken`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-RevokePersonalAccessToken) с `keyId`.

## Успешный ответ (GET)

```json
{
  "success": true,
  "data": [
    {
      "id": "key_8891a",
      "name": "CI/CD Deployment Key",
      "maskedToken": "stp_9f8b***1a4",
      "createdAt": "2026-03-25T14:10:00Z",
      "lastUsedAt": "2026-03-25T15:20:00Z"
    }
  ]
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **404 Not Found** | Попытка удалить несуществующий или чужой ключ. |

---

# SR-AUTH-AC-01: Проверка эффективных прав: Check Permissions

## Общая информация

Крайне важный метод для SPA (Frontend). Вместо того чтобы фронтенд дублировал логику вычисления разрешений (RBAC/ABAC) у себя в коде, он может спросить Auth Service: "Имеет ли текущий юзер право нажать эту кнопку для этого ресурса?". Это позволяет динамически скрывать UI-элементы.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | PermissionCheckRequestDto (действие `action`, ресурс `resource`) |
| **DTO успешного ответа** | PermissionCheckResponseDto (`is_allowed`, `reason`) |

## Параметры URL

Параметры отсутствуют (контекст сессии из Phantom Cookie).

## Логика обработки запроса

*   BFF извлекает контекст сессии из Cookie и передаёт в Auth тело `PermissionCheckRequestDto` (`action`, `resource`).
*   BFF вызывает gRPC [`CheckPermission`](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md#grpc-CheckPermission).
*   Результат маппится в `PermissionCheckResponseDto`.

## Успешный ответ

```json
{
  "success": true,
  "data": {
    "isAllowed": true,
    "reason": "Granted by 'workspace_admin' role"
  }
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | Неизвестное действие (Action) или формат запроса. |

---

# SR-AUTH-AC-05: Гео-политики и IP Whitelisting: Geo-Policies

## Общая информация

Функционал Enterprise-уровня (Local Firewall). Владелец Workspace (Owner) может настроить строгие правила доступа к своему рабочему пространству: например, "Сотрудники могут входить в это пространство только с корпоративного VPN (определенный IP) или находясь на территории конкретной страны". 

| Тип метода | GET / PUT |
| :--- | :--- |
| **DTO запроса (PUT)** | WorkspaceGeoPolicyDto (`allowedIps`, `allowedCountries`, `blockVpnProxies`) |
| **DTO успешного ответа** | WorkspaceGeoPolicyDto |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `wsId` | `string` | ID рабочего пространства, для которого запрашиваются/меняются политики. |

## Логика обработки запроса

*   BFF аутентифицирует вызывающего и проверяет право Owner на `wsId`.
*   **`GET`:** BFF вызывает gRPC [`GetWorkspaceGeoPolicy`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-GetWorkspaceGeoPolicy).
*   **`PUT`:** BFF валидирует тело и вызывает gRPC [`UpdateWorkspaceGeoPolicy`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-UpdateWorkspaceGeoPolicy).

## Успешный ответ (GET / PUT)

```json
{
  "success": true,
  "data": {
    "workspaceId": "ws_alpha_1",
    "allowedIps": ["192.168.100.0/24", "10.0.0.5"],
    "allowedCountries": ["RU", "KZ"],
    "blockVpnProxies": true
  }
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **403 Forbidden** | У пользователя нет прав Owner в данном рабочем пространстве. |
| **404 Not Found** | Workspace не существует. |