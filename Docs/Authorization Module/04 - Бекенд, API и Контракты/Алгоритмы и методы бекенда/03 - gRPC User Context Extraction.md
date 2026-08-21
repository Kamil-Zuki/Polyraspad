# gRPC User Context Extraction

## Введение

`GrpcContextHelper.GetUserId` — единый способ получить userId на protected gRPC handlers.

---

## Алгоритм

1. Read metadata header `user_id` (Aggregator injects after JWT validation).
2. Fallback: HTTP context claims `user_id` or `NameIdentifier`.
3. If missing → RpcException Unauthenticated.

## Roles

`GetRoles` — metadata `roles` comma-separated + ClaimTypes.Role.

---

## Mismatch guard

If request message contains `user_id` field and it ≠ metadata user_id → PermissionDenied.

**Rationale:** prevent caller from acting on behalf of another user when BFF already bound identity.
