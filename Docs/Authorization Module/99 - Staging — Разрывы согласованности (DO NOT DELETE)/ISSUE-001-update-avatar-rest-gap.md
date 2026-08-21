# ISSUE-001 — UpdateAvatarUrl: legacy REST vs BFF

| Поле | Значение |
| :--- | :--- |
| **ID** | ISSUE-001 |
| **Тип** | Пробел (документированный) |
| **Область** | 01 ↔ 04 REST |
| **SR** | SR-AUTHMOD-PROF-04 |
| **Статус** | **Resolved (docs)** |

## В двух словах

`UpdateAvatarUrl` реализован в gRPC и на **Aggregator** (`PUT /api/auth/avatar-url`), но **не** в legacy `AccountsController` (`/api/v1/auth`). Это задокументировано как intentional split: публичный браузерный путь — только BFF.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 04 REST legacy | `AccountsController` | Нет avatar route |
| 04 REST BFF | Aggregator `AuthController` | ✅ `PUT /api/auth/avatar-url` |
| 04 gRPC | `#grpc-UpdateAvatarUrl` | ✅ реализован |

## Рекомендуемое действие

Не добавлять legacy REST без product-запроса. При необходимости прямого access к auth-module — использовать gRPC или Aggregator BFF.

## Ссылки

- [[../04 - Бекенд, API и Контракты/Методы API/gRPC/03 - Управление профилем (Profile Management)#grpc-UpdateAvatarUrl]]
- [[../04 - Бекенд, API и Контракты/Методы API/REST API/01 - Аутентификация (Legacy REST)#rest-avatar-out-of-scope]]
