---
name: Docker Operations
description: Guidelines for managing the local Docker Compose stack, checking logs, and debugging services. Triggers when working with containers or infrastructure.
---

# Docker Operations Skill

This skill explains how to interact with the local Polyraspad infrastructure running in Docker.

## 1. Local Development Philosophy
- **Infrastructure stays in Docker Compose.** (Postgres, Redis, MinIO, internal microservices).
- Running services non-Docker locally is allowed ONLY for focused debugging (e.g., gRPC inspection).
- Always verify the final result by returning the service to the Docker Compose stack.

## 2. Common Commands

**Start everything:**
```powershell
docker compose up --build -d
```

**Rebuild a single service:**
```powershell
docker compose up --build -d <service-name>
```

**Restart without rebuild:**
```powershell
docker compose restart <service-name>
```

**Tail logs:**
```powershell
docker compose logs -f <service-name>
```

## 3. Core Services

- `aggregator-service` (BFF)
- `vocabulary-service` (Core domain)
- `authorization-module` (Identity)
- `billing-service` (SaaS features)
- `media-service` (Storage proxy)
- `agent-service` (AI threads)
- `inclusive` (Python FSRS/NLP)
- `polyraspad-frontend` (Next.js App Router)

*Third-party infrastructure:* `postgres`, `redis`, `minio`

## 4. Troubleshooting
- If a service fails to start, immediately read its logs. Usually, it's a migration failure, missing `.env` variable, or a port conflict.
- Ensure the `docker-compose.yml` environment block binds `.env` correctly. Do not hardcode secrets into the compose file.
