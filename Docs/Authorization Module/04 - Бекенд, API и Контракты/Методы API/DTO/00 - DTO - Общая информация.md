# DTO — Общая информация

## Введение

Authorization Module использует C# records/classes в `Dtos/` и protobuf messages в `authorization.proto`. AutoMapper профиль `AutoMappingProfile` связывает gRPC ↔ domain DTO.

---

# 1. Группы DTO

| Группа | Файл |
| :--- | :--- |
| Аутентификация и профиль | [[01 - Аутентификация и профиль (Auth)]] |

---

# 2. Маппинг слоёв

| Слой | Расположение |
| :--- | :--- |
| gRPC proto | `Protos/authorization.proto` |
| REST JSON | `Dtos/*.cs` |
| Domain service | `IAuthService` method parameters |
