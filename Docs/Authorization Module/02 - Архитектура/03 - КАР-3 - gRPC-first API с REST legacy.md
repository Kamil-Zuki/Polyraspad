# Введение

**Primary contract** для production traffic — gRPC `Pvs.Auth.Grpc.AuthService` на порту 5027. REST `/api/v1/auth` сохранён для direct access и dev tooling.

## Контекст и проблема

Aggregator — единственный public REST. Auth domain должен быть internal gRPC, но REST полезен для отладки и backward compatibility.

## Принятое решение

1. `MapGrpcService<AuthService>()` — все 10 RPC methods.
2. Protected gRPC: identity via metadata header `user_id` (injected by Aggregator).
3. REST controller mirrors same `IAuthService` with JWT Bearer on protected routes.
4. Kestrel listens HTTP/2 on 5027 for h2c gRPC inside Docker network.

## Обоснование и последствия

### Плюсы

* Typed contract (`authorization.proto`) shared with Aggregator
* Thin duplication — one domain service

### Последствия

* Two surfaces to maintain — changes must update proto + REST DTOs
* *Решение:* contract-first via `.proto`; REST as secondary

См. [[04 - Бекенд, API и Контракты/Методы API/gRPC/00 - gRPC - Общая информация]].
