# 01 - Аутентификация и профиль (Auth)

DTO для проксирования authorization-module.

## UserRegistrationDto {#dto-UserRegistrationDto}

| Поле | Тип | Обязательно | Описание |
| :--- | :--- | :---: | :--- |
| email | string | да | Email пользователя |
| password | string | да | Пароль |
| userName | string? | нет | Отображаемое имя |

## UserLoginDto {#dto-UserLoginDto}

| Поле | Тип | Обязательно |
| :--- | :--- | :---: |
| email | string | да |
| password | string | да |

## TokenResponseDto {#dto-TokenResponseDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| accessToken | string | JWT access |
| refreshToken | string | Refresh token |
| expiresIn | int | Seconds |

## UserInfoDto {#dto-UserInfoDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id | string | User id |
| email | string | |
| userName | string? | |
| avatarUrl | string? | |

## AuthResponseDto {#dto-AuthResponseDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| message | string | Status message |

## RefreshTokenDto, ConfirmEmailDto, LogoutDto, UpdateUsernameDto, UpdatePasswordDto, UpdateAvatarUrlDto

См. `AggregatorService/Dtos/Auth/` — поля соответствуют gRPC messages authorization.proto.
