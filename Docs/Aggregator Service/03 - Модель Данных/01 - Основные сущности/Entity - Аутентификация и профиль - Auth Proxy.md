# Entity - Аутентификация и профиль - Auth Proxy

**Тип:** API Contract View (BFF не хранит credentials)

Downstream: `authorization-module` (gRPC).

## TokenResponse / AuthResponse (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| accessToken | string | JWT access |
| refreshToken | string | Refresh token |
| expiresIn / expiry | number/datetime | Срок access (по контракту DTO) |

## UserInfo (контракт)

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| id / userId | string | Id пользователя |
| email | string | Email |
| userName | string | Username |
| emailConfirmed | bool | Подтверждение email |
| avatarUrl | string? | URL аватара |

REST: `/api/Auth/*`. BFF валидирует JWT локально; мутации профиля проксирует в auth gRPC.
