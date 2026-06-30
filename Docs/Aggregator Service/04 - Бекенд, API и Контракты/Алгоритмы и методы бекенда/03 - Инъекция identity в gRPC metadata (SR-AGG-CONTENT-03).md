# Введение

Cross-cutting алгоритм BFF: передача **user_id** и **roles** из JWT в gRPC metadata при вызовах VocabularyService, AgentService, MediaService (library) и BillingService.

Реализует **SR-AGG-CONTENT-03**. Код: `MappingHelper`, `VocabularyServiceClient`, typed gRPC clients в контроллерах.

# 1. Список алгоритмов

| Название | SR | Где |
| :--- | :--- | :--- |
| **JWT → user_id extraction** | SR-AGG-CONTENT-03 | `MappingHelper.GetUserId` |
| **JWT → roles extraction** | SR-AGG-CONTENT-03 | `MappingHelper.GetRoles` |
| **gRPC metadata injection** | SR-AGG-CONTENT-03 | `VocabularyServiceClient`, `AgentServiceClient`, … |

---

# Алгоритм JWT → caller context (SR-AGG-CONTENT-03)

## Контекст и область применения

Aggregator валидирует JWT локально (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:Secret`). Downstream **не** повторяет JWT parse — получает metadata.

## user_id

1. Claim `sub` (`JwtRegisteredClaimNames.Sub`) — primary (authorization-module).
2. Fallback: `ClaimTypes.NameIdentifier`, `user_id`.
3. Test fallback: header `X-User-Id` (Guid).
4. Отсутствие → `UnauthorizedAccessException` → HTTP **401**.

## roles

1. Claims `ClaimTypes.Role` из JWT.
2. Fallback: header `X-User-Roles` (comma-separated).
3. Distinct list; пустой список допустим.

## gRPC metadata (VocabularyServiceClient pattern)

```csharp
var headers = new Metadata
{
    { "user_id", userId.ToString() },
    { "roles", string.Join(",", roles) }
};
```

Все Card/Content/Study/Term/Community/Subscription/Text RPC через typed clients получают этот metadata. **ACL и project access** — в VocabularyService (`GrpcContextHelper`).

## Agent / Media / Billing

| Downstream | Metadata |
| :--- | :--- |
| AgentService | `user_id`, `roles` (AgentGrpcService + validators) |
| MediaService (library RPC) | `user_id` в metadata (owner context) |
| BillingService | `user_id` в request body + trusted caller from Aggregator |

## Ошибки

| Слой | Поведение |
| :--- | :--- |
| BFF | 401 если JWT invalid / no user_id |
| Vocabulary gRPC | `UNAUTHENTICATED` / `PERMISSION_DENIED` при ACL fail |
