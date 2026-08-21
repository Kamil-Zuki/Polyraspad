# Введение

gRPC **AuthService** — основной контракт authorization-module. Package: `pvs.auth.v1`, C# namespace `Pvs.Auth.Grpc`.

Источник истины proto: [[authorization.proto]] (копия `authorization-module.API/Protos/authorization.proto`).

Aggregator вызывает RPC по h2c (`authorization-module:5027`). Protected methods ожидают inbound metadata `user_id` (и опционально `roles`) после JWT на BFF.

---

# 1. Группы методов gRPC

| Группа `01` | Файл `04` | RPC |
| :--- | :--- | :---: |
| Регистрация и подтверждение email | [[01 - Регистрация и подтверждение email (Registration)]] | 3 |
| Аутентификация и JWT-токены | [[02 - Аутентификация и JWT-токены (Authentication)]] | 3 |
| Управление профилем | [[03 - Управление профилем (Profile Management)]] | 5 |
| Платформенные контракты | [[../Алгоритмы и методы бекенда/04 - Platform Operations]] | 0 RPC |

---

# 2. Сводная таблица RPC

| SR | gRPC Method | Тип | Название и Описание |
| :--- | :--- | :--- | :--- |
| SR-AUTHMOD-REG-01 | `RegisterUser` | unary | Регистрация → SMTP confirm |
| SR-AUTHMOD-REG-02 | `ConfirmEmail` | unary | Подтверждение email |
| SR-AUTHMOD-REG-03 | `ResendConfirmationEmail` | unary | Повторная отправка письма подтверждения |
| SR-AUTHMOD-AUTH-01 | `LoginUser` | unary | Login → TokenResponse |
| SR-AUTHMOD-AUTH-02 | `RefreshToken` | unary | Refresh token rotation |
| SR-AUTHMOD-AUTH-03 | `LogoutUser` | unary | Logout + revoke refresh |
| SR-AUTHMOD-PROF-01 | `GetUserInfo` | unary | Profile read |
| SR-AUTHMOD-PROF-05 | `FindUserByEmail` | unary | Internal lookup by email |
| SR-AUTHMOD-PROF-02 | `UpdateUsername` | unary | Rename |
| SR-AUTHMOD-PROF-03 | `UpdatePassword` | unary | Password change |
| SR-AUTHMOD-PROF-04 | `UpdateAvatarUrl` | unary | Avatar URL (empty clears) |

---

# Inbound metadata

| Key | Источник | Описание |
| :--- | :--- | :--- |
| `user_id` | Aggregator `GrpcContextHelper` | Guid пользователя (protected RPC) |
| `roles` | JWT claims | Optional для downstream |

Поля `user_id` в proto-request **дублируют** metadata для проверки mismatch; канонический caller id — metadata.
