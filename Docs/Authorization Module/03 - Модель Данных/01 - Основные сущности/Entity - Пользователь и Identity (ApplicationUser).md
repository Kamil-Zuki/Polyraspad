# Entity — Пользователь и Identity (ApplicationUser)

## Введение

`ApplicationUser` — расширение `IdentityUser` для Polyraspad. Хранит credentials, email confirmation state и публичный URL аватара.

**Таблица:** `AspNetUsers` (EF Core Identity)

---

## Поля

| Поле | Тип | Обязательное | Описание |
| :--- | :--- | :---: | :--- |
| `Id` | `string` (UUID) | да | PK, генерируется Identity |
| `UserName` | `string` | да | Отображаемое имя; при регистрации — `User_{8 hex}` |
| `Email` | `string` | да | Уникальный логин; нормализация через Identity |
| `EmailConfirmed` | `bool` | да | `false` до confirm-email; login блокируется |
| `PasswordHash` | `string` | да | Hash пароля (Identity hasher) |
| `AvatarUrl` | `string?` | нет | Абсолютный http/https URL аватара (≤ 2048 символов) |
| `RefreshToken` | `string?` | нет | Legacy поле на entity; **активные refresh** — в таблице `RefreshTokens` |
| `RefreshTokenExpiryTime` | `DateTime` | да | Legacy; не используется rotation flow в `AuthService` |

Стандартные поля Identity (`PhoneNumber`, `LockoutEnd`, `AccessFailedCount`, …) — доступны, lockout при login **отключён** (`lockoutOnFailure: false`).

---

## Жизненный цикл

1. **Register:** создаётся запись с `EmailConfirmed = false`, отправляется SMTP confirm link.
2. **Confirm email:** `EmailConfirmed = true`; удаляются прочие неподтверждённые записи с тем же email.
3. **Login:** требует `EmailConfirmed = true`.
4. **Profile update:** `UserName`, `PasswordHash`, `AvatarUrl` через `UserManager`.

---

## Связи

| Связь | Тип | Сущность |
| :--- | :--- | :--- |
| Refresh tokens | 1:N | [[Entity - Refresh-токены (RefreshToken)]] |

---

## SR в `01`

| SR | Операция |
| :--- | :--- |
| SR-AUTHMOD-REG-01 | Register |
| SR-AUTHMOD-REG-02 | Confirm email |
| SR-AUTHMOD-AUTH-01 | Login |
| SR-AUTHMOD-PROF-01 | GetUserInfo |
| SR-AUTHMOD-PROF-02 | UpdateUsername |
| SR-AUTHMOD-PROF-03 | UpdatePassword |
| SR-AUTHMOD-PROF-04 | UpdateAvatarUrl |
| SR-AUTHMOD-PROF-05 | FindUserByEmail |
