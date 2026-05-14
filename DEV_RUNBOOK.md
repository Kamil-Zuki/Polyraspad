# Dev Runbook

## Purpose

This runbook is the default workflow for local development and MVP smoke testing.

The recommended mode is:

- keep infrastructure and service-to-service integration in `docker compose`
- use local non-Docker runs only for narrow debugging sessions
- always re-check the final result through `docker compose`

## Prerequisites

- Docker Desktop is running
- the repository root contains a filled `.env`
- required secrets are set in `.env`:
  - `POSTGRES_PASSWORD`
  - `MINIO_ROOT_PASSWORD`
  - `JWT_SECRET`
  - `SMTP_HOST`
  - `SMTP_USERNAME`
  - `SMTP_PASSWORD`
  - `SMTP_ADDRESS`
  - `AUTH_CONFIRMATION_LINK`

## First Start

From the repository root:

```powershell
docker compose down
docker compose up --build -d
```

Check service state:

```powershell
docker compose ps
```

Check key logs:

```powershell
docker compose logs aggregator-service --tail 100
docker compose logs authorization-module --tail 100
docker compose logs vocabulary-service --tail 100
docker compose logs polyraspad-frontend --tail 100
```

## Main URLs

- Frontend: `http://localhost:3000`
- Aggregator API: `http://localhost:5000`
- MinIO API: `http://localhost:9000`
- MinIO Console: `http://localhost:9001`

Internal-only services are intentionally not published to the host by default.

## Health Checks

Check the public BFF health endpoint:

```powershell
curl http://localhost:5000/healthz
```

Expected result:

```json
{"status":"ok"}
```

## Rebuild Only One Service

Aggregator:

```powershell
docker compose up --build -d aggregator-service
docker compose logs aggregator-service --tail 100
```

Authorization:

```powershell
docker compose up --build -d authorization-module
docker compose logs authorization-module --tail 100
```

Vocabulary:

```powershell
docker compose up --build -d vocabulary-service
docker compose logs vocabulary-service --tail 100
```

Frontend:

```powershell
docker compose up --build -d polyraspad-frontend
docker compose logs polyraspad-frontend --tail 100
```

If contracts or shared integration behavior changed, rebuild the full backend slice:

```powershell
docker compose up --build -d authorization-module vocabulary-service aggregator-service polyraspad-frontend
```

## Restart Without Rebuild

```powershell
docker compose restart aggregator-service
docker compose restart authorization-module
docker compose restart vocabulary-service
docker compose restart polyraspad-frontend
```

## Live Logs

```powershell
docker compose logs -f aggregator-service
docker compose logs -f authorization-module
docker compose logs -f vocabulary-service
docker compose logs -f polyraspad-frontend
```

## Stop Stack

```powershell
docker compose down
```

If you explicitly need to remove volumes too:

```powershell
docker compose down -v
```

Use `-v` carefully because it removes local Postgres, Redis, MinIO and other compose volumes.

## MVP Smoke Test

After any meaningful backend or frontend change, verify:

1. frontend opens at `http://localhost:3000`
2. login works
3. card list opens
4. card details open
5. image on card renders
6. one create or update flow succeeds
7. `http://localhost:5000/healthz` returns `200`

If auth-related code changed, also verify:

1. registration
2. email confirmation link format
3. token refresh
4. authenticated endpoint like `GET /api/Auth/me`

## When Local Non-Docker Run Is Acceptable

You can temporarily run one service outside Docker only for focused debugging.

Recommended rule:

- infrastructure stays in `docker compose`
- one target service may run locally
- after the fix, switch back to full `docker compose` verification

This is especially important for:

- gRPC between `AggregatorService` and `authorization-module`
- gRPC between `AggregatorService` and `VocabularyService`
- MinIO media URLs
- CORS behavior

## Common Recovery Commands

Recreate everything:

```powershell
docker compose down
docker compose up --build -d
```

Recreate one service from scratch:

```powershell
docker compose rm -sf aggregator-service
docker compose up --build -d aggregator-service
```

Inspect container environment:

```powershell
docker compose exec aggregator-service printenv
```

Inspect recent failures:

```powershell
docker compose logs aggregator-service --since 10m
docker compose logs authorization-module --since 10m
docker compose logs vocabulary-service --since 10m
```

## Notes

- `authorization-module`, `vocabulary-service`, `postgres`, `redis`, and `inclusive` are internal by default in compose.
- browser traffic should go through `polyraspad-frontend` and `aggregator-service`.
- media URLs are expected to resolve through public MinIO access on `localhost:9000`.
