# REST API — Общая информация

## Введение

Legacy/direct REST surface: `AccountsController`, base route **`/api/v1/auth`**.

Production browser traffic идёт через **Aggregator** (`/api/Auth/*`), который вызывает gRPC. REST на auth-module используется для dev, отладки и backward compatibility.

JWT Bearer auth на protected endpoints (`[Authorize]`). Claim `NameIdentifier` = userId.

---

# 1. Список endpoints

| Метод | Route | Auth | gRPC equivalent |
| :--- | :--- | :---: | :--- |
| POST | `/register` | — | #grpc-RegisterUser |
| POST | `/login` | — | #grpc-LoginUser |
| POST | `/refresh-token` | — | #grpc-RefreshToken |
| GET | `/confirm-email` | — | #grpc-ConfirmEmail |
| GET | `/me` | JWT | #grpc-GetUserInfo |
| POST | `/logout` | JWT | #grpc-LogoutUser |
| PUT | `/username` | JWT | #grpc-UpdateUsername |
| PUT | `/password` | JWT | #grpc-UpdatePassword |

> **Note:** `UpdateAvatarUrl` доступен только через gRPC в текущем коде (нет REST route в AccountsController).

Детали: [[01 - Аутентификация (Legacy REST)]], [[02 - Платформенные контракты (Operations)]].
