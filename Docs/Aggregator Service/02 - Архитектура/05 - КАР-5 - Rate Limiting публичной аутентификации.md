# Введение

Policy **`auth-public`**: fixed window rate limiter на register, login, refresh-token, confirm-email.

## Контекст и проблема

Публичные auth endpoints — цель credential stuffing и spam registration.

## Принятое решение

* `PermitLimit = 10`, `Window = 1 minute`, partition by IP (X-Forwarded-For first hop)
* RejectionStatusCode 429, JSON body

## Обоснование и последствия

### Плюсы

* Простая защита без Redis (in-memory per instance)

### Последствия

* Лимит per-instance при нескольких replicas — не глобальный
* *Решение:* nginx rate limit или Redis store при масштабировании

{#КАР-5}
