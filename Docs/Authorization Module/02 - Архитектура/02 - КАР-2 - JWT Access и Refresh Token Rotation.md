# Введение

Access tokens — **stateless JWT** (HMAC-SHA256). Refresh tokens — **persistent opaque strings** с rotation при каждом refresh.

## Контекст и проблема

Pure JWT без refresh заставляет часто re-login. Длинный JWT без revoke — риск при утечке. Refresh в БД позволяет revoke и rotation.

## Принятое решение

1. Access TTL: `Jwt:Expire` minutes (config, default 30).
2. Refresh: 64 random bytes, Base64, stored in `RefreshTokens`, TTL 7 days.
3. On refresh: revoke old, issue new pair.
4. On logout: revoke provided refresh (access expires naturally).
5. Aggregator validates access JWT locally — auth-module не вызывается на каждый API request.

## Обоснование и последствия

### Плюсы

* Масштабируемая валидация на BFF
* Revocable refresh sessions

### Последствия

* Stolen access JWT valid until exp — короткий TTL mitigates
* *Решение:* короткий access TTL + refresh rotation

Связанные SR: [[02 - Аутентификация и JWT-токены (Authentication)#SR-AUTHMOD-AUTH-02|SR-AUTHMOD-AUTH-02]].
