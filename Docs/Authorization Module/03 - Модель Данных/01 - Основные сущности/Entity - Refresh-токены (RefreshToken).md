# Entity — Refresh-токены (RefreshToken)

## Введение

`RefreshToken` хранит **opaque refresh token** для продления access JWT без повторного ввода пароля. При успешном refresh старый token помечается `IsRevoked = true`, выдаётся новая пара (rotation).

**Таблица:** `RefreshTokens`

---

## Поля

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `Id` | `Guid` | да | PK, default `Guid.NewGuid()` |
| `Token` | `string` | да | Base64, 64 random bytes; unique lookup key |
| `UserId` | `string` | да | FK → `ApplicationUser.Id` |
| `ExpiryDate` | `DateTime` (UTC) | да | Default: `UtcNow + 7 days` при создании |
| `IsRevoked` | `bool` | да | `true` после refresh или logout |

---

## Жизненный цикл

1. **Login:** новая запись, `IsRevoked = false`, TTL 7 дней. Существующие refresh **не отзываются** (multi-device).
2. **Refresh:** старый token → `IsRevoked = true`; создаётся новый token + новый access JWT.
3. **Logout:** если передан refresh token — matching row → `IsRevoked = true`.
4. **Validation failure:** expired, revoked или unknown token → ошибка «Invalid or expired refresh token».

---

## SR в `01`

| SR | Операция |
| :--- | :--- |
| SR-AUTHMOD-AUTH-02 | RefreshToken |
| SR-AUTHMOD-AUTH-03 | LogoutUser (revoke refresh) |

---

## Индексы и ограничения

- Lookup по `Token` (FirstOrDefault в `AuthService.RefreshToken`)
- Явный unique index в миграции — проверить при production hardening (ISSUE при необходимости)
