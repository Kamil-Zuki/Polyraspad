# Список сущностей микросервиса Authorization Module

## Введение

Authorization Module хранит **учётные записи пользователей** и **refresh-токены** в PostgreSQL (`auth-module`). Схема построена на **ASP.NET Core Identity** (`AspNetUsers`, `AspNetRoles`, …) с расширением `ApplicationUser` и отдельной таблицей `RefreshTokens`.

Публичный контракт — gRPC `Pvs.Auth.Grpc.AuthService`; REST `/api/v1/auth` — legacy/direct access (dev, отладка).

---

## Индекс сущностей

| Сущность | Файл | Назначение |
| :--- | :--- | :--- |
| **ApplicationUser** | [[Entity - Пользователь и Identity (ApplicationUser)]] | Учётная запись: email, пароль (hash), username, avatar, email confirmed |
| **RefreshToken** | [[Entity - Refresh-токены (RefreshToken)]] | Долгоживущий refresh token для rotation access JWT |

---

## Связи (кратко)

```
ApplicationUser 1 ── * RefreshToken   (логическая связь; DB FK на RefreshTokens.UserId отсутствует)
     │
     └── Identity tables (AspNetUserClaims, AspNetUserLogins, …) — стандарт Identity
```

---

## Вне scope персистентности

- JWT access tokens — **stateless**, не хранятся в БД
- Сессии браузера, Phantom Cookie, Redis — **не используются**
- Роли (`IdentityRole`) — зарегистрированы в DI, в текущем коде Polyraspad **не назначаются** при регистрации
