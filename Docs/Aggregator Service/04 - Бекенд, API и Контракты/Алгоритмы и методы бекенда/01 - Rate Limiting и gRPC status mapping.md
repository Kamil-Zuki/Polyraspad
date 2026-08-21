# Введение

Cross-cutting алгоритмы BFF: маппинг gRPC статусов в HTTP, rate limiting публичных auth-маршрутов, production config guard.

# 1. gRPC Status → HTTP mapping

## Контекст

Единообразные REST-ответы при ошибках downstream. Реализация: interceptor или helper в контроллерах (`GrpcExceptionMapper` pattern).

## Таблица маппинга

| RpcException.StatusCode | HTTP | ProblemDetails message |
| :--- | :--- | :--- |
| InvalidArgument | 400 | Validation / bad input |
| NotFound | 404 | Resource not found |
| AlreadyExists | 409 | Conflict |
| PermissionDenied | 403 | Forbidden |
| Unauthenticated | 401 | Unauthorized |
| FailedPrecondition | 412 | Precondition failed |
| Unimplemented | 501 | Not implemented |
| Unavailable, DeadlineExceeded | 502 | Service unavailable |
| default | 502 | Internal gateway error |

## Псевдокод

```csharp
public static IActionResult Map(RpcException ex) => ex.StatusCode switch
{
    StatusCode.InvalidArgument => BadRequest(ex.Status.Detail),
    StatusCode.NotFound => NotFound(),
    StatusCode.PermissionDenied => Forbid(),
    StatusCode.Unauthenticated => Unauthorized(),
    _ => StatusCode(502, "Downstream error")
};
```

---

# 2. Rate limiting auth-public (SR-AGG-AUTH-08)

## Контекст

Защита `/api/Auth/register`, `/login`, `/refresh`, `/confirm` от brute-force без Redis на BFF — in-memory sliding window per IP (dev) или reverse-proxy limit (prod nginx).

## Входные данные

| Параметр | Описание |
| :--- | :--- |
| Client IP | X-Forwarded-For или connection remote |
| Route | Auth public endpoint |

## Выход

HTTP **429 Too Many Requests** при превышении порога (например 20 req/min/IP на login).

---

# 3. Production configuration guard (SR-AGG-OPS-03)

## Контекст

Fail-fast при старте если критичные secrets missing в non-Development.

## Проверки

* `JWT_SECRET` length ≥ 32
* Downstream base URLs не localhost в Production без explicit override
* `AI_COMPLETION_ENABLED=true` требует valid `Ai:ApiKey`

{#SR-AGG-AUTH-08}
{#SR-AGG-OPS-03}
