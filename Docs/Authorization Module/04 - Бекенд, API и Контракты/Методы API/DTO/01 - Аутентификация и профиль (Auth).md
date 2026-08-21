# DTO — Аутентификация и профиль (Auth)

## Введение

DTO для register/login/profile flows. Поля согласованы с [[Entity - Пользователь и Identity (ApplicationUser)]] и `authorization.proto`.

---

# 1. Список DTO

| DTO | Поля | SR / RPC |
| :--- | :--- | :--- |
| **UserRegistrationRequest** | Email, Password, ConfirmPassword | RegisterUser |
| **UserLoginRequest** | Email, Password | LoginUser |
| **TokenDto** | AccessToken, RefreshToken | LoginUser, RefreshToken |
| **RefreshTokenRequest** | RefreshToken | RefreshToken |
| **ConfirmEmailRequest** | UserId, Token | ConfirmEmail |
| **UserInfoDto** | Id, UserName, Email, EmailConfirmed, AvatarUrl | GetUserInfo, FindUserByEmail |
| **LogoutRequest** | RefreshToken | LogoutUser |
| **UpdateUsernameRequest** | UserName | UpdateUsername |
| **UpdatePasswordRequest** | CurrentPassword, NewPassword | UpdatePassword |
| **StringResultDto** | Data (message) | Message responses |

---

<span id="dto-UserRegistrationRequest"></span>

## UserRegistrationRequest

| Поле | Тип | Валидация |
| :--- | :--- | :--- |
| Email | string | NotEmpty, EmailAddress |
| Password | string | Min 6, ≥1 uppercase, ≥1 special |
| ConfirmPassword | string | Equal Password |

---

<span id="dto-TokenDto"></span>

## TokenDto / TokenResponse

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| AccessToken | string | JWT string |
| RefreshToken | string | Opaque Base64 |

---

<span id="dto-UserInfoDto"></span>

## UserInfoDto / UserInfoResponse

| Поле | Тип | Entity mapping |
| :--- | :--- | :--- |
| Id | string | ApplicationUser.Id |
| UserName | string | ApplicationUser.UserName |
| Email | string | ApplicationUser.Email |
| EmailConfirmed | bool | ApplicationUser.EmailConfirmed |
| AvatarUrl | string | ApplicationUser.AvatarUrl |
