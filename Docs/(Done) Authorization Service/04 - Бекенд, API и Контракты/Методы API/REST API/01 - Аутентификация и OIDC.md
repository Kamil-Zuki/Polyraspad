# Введение

В данном документе описаны эндпоинты группы «Аутентификация и OIDC» — **контракт публичной поверхности** (как правило реализуемой **API Gateway — агрегатором** с проксированием в Auth по gRPC). Эта группа — точка входа для начала пользовательской сессии. **Authorization Service** выступает в роли OIDC Relying Party (Клиента): редирект на STEOS ID, обработка кода авторизации, установка Phantom Cookie — в доменной логике AUTH; наружу то же поведение отдаёт **периметр (Gateway)**.

# 1. Список эндпоинтов

Ниже приведен список методов REST API, отвечающих за авторизацию, обмен токенов и завершение сеанса.

| Код требования | Метод | Эндпоинт                 | Назначение                                          |
| :------------- | :---: | :----------------------- | :-------------------------------------------------- |
| SR-AUTH-OI-01  |  GET  | `/auth/oidc/login`       | Инициализация OIDC-протокола (PKCE).                |
| SR-AUTH-OI-01  |  GET  | `/auth/oidc/callback`    | Обработка `code` и `state`, получение токенов.      |
| SR-AUTH-SM-06  | POST  | `/auth/logout`           | Уничтожение локальной сессии и очистка Cookie.      |
| SR-AUTH-OI-04  | POST  | `/auth/oidc/backchannel` | Webhook логаута от STEOS ID.                        |
| SR-AUTH-SM-07  | POST  | `/auth/refresh`          | Тихое продление (Silent Refresh) OIDC-токенов.      |
| SR-AUTH-AC-02  | POST  | `/auth/mfa/challenge`    | Инициация локального Step-up MFA.                   |
| SR-AUTH-AC-02  | POST  | `/auth/mfa/verify`       | Проверка MFA-кода (TOTP) и выдача временного Scope. |

---

# SR-AUTH-OI-01: Инициализация OIDC: Login

## Общая информация

Метод начинает Authorization Code Flow с PKCE. **BFF** (периметр) генерирует криптографические `state`, `code_verifier` и `code_challenge`, сохраняет связку во временном хранилище (например Redis) и перенаправляет браузер на страницу логина провайдера (STEOS ID).

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | N/A |
| **DTO успешного ответа** | N/A (HTTP 302 Redirect) |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `redirect_uri` | `string` | Опциональный локальный URL (внутри SPA) для возврата после успешного логина. |
| `prompt` | `string` | Опционально `login` для принудительного ввода пароля или `none` для SSO-проверки. |

## Логика обработки запроса

*   BFF проверяет `redirect_uri` на соответствие whitelist платформы.
*   BFF генерирует PKCE (`state`, `code_verifier`, `code_challenge`) и сохраняет связку во временном хранилище (например Redis), чтобы на callback сопоставить `state` и обменять `code` на токены с тем же `code_verifier`.
*   BFF формирует URL авторизации STEOS ID и отвечает **302** с заголовком `Location` (отдельного unary gRPC под этот шаг нет — это чистый HTTP-периметр).
*   После успешного callback создаётся локальная сессия в Authorization Service — доменные шаги см. в gRPC [`GetSessionContext`](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md#grpc-GetSessionContext) (контекст сессии после установки).

## Успешный ответ

Сервер не возвращает JSON, он возвращает HTTP-заголовок редиректа.

```http
HTTP/1.1 302 Found
Location: https://id.steos.com/authorize?client_id=auth-svc&response_type=code&scope=openid+profile...
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | Передан недоверенный `redirect_uri` (не в Whitelist платформы). |

---

# SR-AUTH-OI-01: Обработка OIDC: Callback

## Общая информация

Эндпоинт принимает обратный вызов (Callback) от STEOS ID после успешной аутентификации пользователя. Выполняет обмен кода (Authorization Code) на токены и инициализирует локальную сессию (Phantom Token).

| Тип метода | GET |
| :--- | :--- |
| **DTO запроса** | OidcCallbackRequestDto (в виде Query-параметров) |
| **DTO успешного ответа** | OidcAuthResponseDto / (HTTP 302) |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| `code` | `string` | Временный код авторизации от STEOS ID. |
| `state` | `string` | Строка состояния для защиты от CSRF. |
| `error` | `string` | Код ошибки (если пользователь отменил логин). |

## Логика обработки запроса

*   BFF извлекает `code` и `state`, сверяет `state` с сохранённым при `/login` значением (защита от CSRF).
*   Обмен `code` на токены выполняется по **HTTP** между BFF и STEOS ID (не unary RPC Auth); проверка/интроспекция токенов на стороне Auth при необходимости — gRPC [`IntrospectToken`](../gRPC/03%20-%20Интеграция%20OIDC%20и%20Внутренние%20токены%20(OIDC%20Infrastructure).md#grpc-IntrospectToken).
*   BFF инициирует создание/обновление локальной сессии и выдачу Phantom Cookie через Authorization Service — см. gRPC [`GetSessionContext`](../gRPC/01%20-%20Ядро%20валидации%20и%20инъекции%20(Validation%20Core).md#grpc-GetSessionContext).
*   Ответ клиенту: JSON (API-клиент) или **302** с редиректом в SPA (браузер).

## Успешный ответ

(В случае API-only клиента возвращается JSON, для браузера — HTTP 302)
```json
{
  "success": true,
  "data": {
    "sessionId": "b3f...a1c",
    "user": {
      "id": "usr_9982",
      "email": "user@example.com"
    },
    "expiresIn": 3600
  }
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | Несовпадение `state` (CSRF) или истек TTL кода. |
| **401 Unauthorized** | STEOS ID отклонил `code` или недействительный `code_verifier`. |
| **502 Bad Gateway** | Сервер STEOS ID временно недоступен. |

---

# SR-AUTH-SM-06: Логаут: Logout

## Общая информация

Уничтожает активную сессию пользователя. Очищает локальный Redis-кэш и отправляет браузеру команду на удаление Phantom Cookie.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | LogoutRequestDto |
| **DTO успешного ответа** | SuccessResponseDto |

## Параметры URL

Параметры отсутствуют (Cookie `session_id` передается в заголовках).

## Логика обработки запроса

*   BFF извлекает идентификатор сессии из Cookie и передаёт в Auth запрос на завершение локальной сессии (инвалидация в Redis/БД, аудит) — gRPC [`RevokeSession`](../gRPC/02%20-%20Управление%20жизненным%20циклом%20сессий%20(Session%20Lifecycle).md#grpc-RevokeSession).
*   BFF очищает Phantom Cookie в ответе HTTP.
*   При флаге глобального логаута в теле запроса дополнительно инициируется отзыв/редирект на стороне IdP (HTTP, не gRPC).

## Успешный ответ

```json
{
  "success": true,
  "message": "Сессия успешно завершена",
  "timestamp": "2026-03-25T14:00:00Z"
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **401 Unauthorized** | Cookie отсутствует или сессия уже недействительна. |

---

# SR-AUTH-OI-04: Асинхронный логаут: Backchannel Webhook

## Общая информация

Эндпоинт, реализующий стандарт `OpenID Connect Back-Channel Logout`. STEOS ID вызывает его, когда пользователь сбрасывает пароль, блокируется администратором или нажимает "Выйти со всех устройств" в глобальном хабе.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | BackchannelLogoutRequestDto (`logout_token` JWT) |
| **DTO успешного ответа** | Пустой ответ 200 OK |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

*   BFF принимает тело с `logout_token` (JWT), проверяет подпись и claims по правилам OIDC Back-Channel Logout.
*   BFF вызывает Authorization Service по gRPC: [`RevokeAllUserSessions`](../gRPC/02%20-%20Управление%20жизненным%20циклом%20сессий%20(Session%20Lifecycle).md#grpc-RevokeAllUserSessions).
*   Возвращается **200 OK** без тела при успешной обработке сигнала.

## Успешный ответ

По стандарту OIDC должен возвращаться HTTP 200 OK без тела, указывающий, что сигнал принят.

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | Неверный формат токена, истекший срок или отсутствие обязательных полей. |

---

# SR-AUTH-SM-07: Тихое обновление: Silent Refresh

## Общая информация

Метод вызывается SPA-клиентом, когда он получает статус `401 Token Expired` от бизнес-сервисов, либо в фоновом режиме перед истечением сессии. Обновляет OIDC-токены "под капотом", не требуя от пользователя повторного ввода пароля.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса** | TokenRefreshRequestDto |
| **DTO успешного ответа** | SuccessResponseDto |

## Параметры URL

Параметры отсутствуют (Session ID берется из Cookie).

## Логика обработки запроса

*   BFF по Cookie определяет текущую сессию и извлекает привязанный refresh token (или идентификатор для его получения).
*   Обновление OIDC access/refresh токенов выполняется по **HTTP** (`grant_type=refresh_token`) между BFF и STEOS ID.
*   BFF синхронизирует кэш локальной сессии в Authorization Service — gRPC [`UpdateSessionContext`](../gRPC/02%20-%20Управление%20жизненным%20циклом%20сессий%20(Session%20Lifecycle).md#grpc-UpdateSessionContext).

## Успешный ответ

```json
{
  "success": true,
  "message": "Сессия успешно продлена"
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **401 Unauthorized** | Refresh Token истек или был отозван на стороне STEOS ID (требуется полный перелогин). |

---

# SR-AUTH-AC-02: Запрос локального MFA: Challenge / Verify

## Общая информация

Если пользователь пытается выполнить высококритичное действие (например, удалить проект или изменить биллинг), SPA-клиент делает вызов `challenge` для отправки пуша/SMS или генерации TOTP экрана, а затем `verify` для подтверждения. Это Step-up Authentication внутри уже существующей сессии.

| Тип метода | POST |
| :--- | :--- |
| **DTO запроса (Challenge)** | MfaChallengeDto (тип фактора) |
| **DTO ответа (Challenge)** | SuccessResponseDto (MFA Transaction ID) |
| **DTO запроса (Verify)** | MfaVerifyRequestDto (Transaction ID, код из SMS/TOTP) |
| **DTO ответа (Verify)** | SuccessResponseDto |

## Параметры URL

Параметры отсутствуют.

## Логика обработки запроса

*   BFF проверяет активную пользовательскую сессию (Phantom Cookie).
*   **`POST /auth/mfa/challenge`:** BFF вызывает gRPC [`StartStepUpMfaChallenge`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-StartStepUpMfaChallenge).
*   **`POST /auth/mfa/verify`:** BFF вызывает gRPC [`VerifyStepUpMfa`](../gRPC/06%20-%20Управление%20доступом,%20PAT%20и%20политики%20Workspace%20(Access%20Control).md#grpc-VerifyStepUpMfa) с идентификатором транзакции и кодом; при успехе выдаётся временный elevated scope для критичных операций.

## Успешный ответ (Verify)

```json
{
  "success": true,
  "message": "MFA успешно подтвержден. Временные повышенные права выданы на 5 минут."
}
```

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **400 Bad Request** | Неверный код подтверждения. |
| **429 Too Many Requests** | Исчерпано количество попыток ввода MFA. |