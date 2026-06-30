# AGENTS.md

Repository-level operating guide for AI coding agents working on **Polyraspad**.

Polyraspad is a language-learning platform built around a LingQ-style reader model. The system is a multi-service application: a Next.js frontend, several ASP.NET Core backend services communicating over gRPC, a Python microservice for FSRS scheduling and NLP, PostgreSQL/Redis/MinIO infrastructure, and a browser extension for content capture.

This file stays at the repository root on purpose (tooling and hierarchical agent docs expect a root entry point). Deeper guidance lives in `context/`; Cursor-specific wiring for **code and coordination** lives in `.cursor/`; Cursor-specific wiring for **STEOS microservice docs** lives in `Docs/.cursor/`.

> **Agent readers:** assume the reader of this file knows nothing about the project. Be precise, cite file paths, and do not generalize beyond what is actually in the repository.

---

## 1. Repository Map

| Path | Purpose |
|------|---------|
| `Docs/` | Authoritative, stable documentation for humans and the team. STEOS microservice docs follow rules in `Docs/.cursor/` (`steos-docs-*`). |
| `Docs/(Done) Authorization Service/` | **Formatting etalon only** — folder tree, heading depth, tables, block order for future service docs. Not a content source; do not copy Auth domain text into other services. |
| `context/` | Operational memory: agent instructions, rules, active implementation plans, skills, research, ADRs-in-progress. |
| `.cursor/` | Cursor-native executable material: agents, commands, always-applied rules (`.cursor/rules/`), skills, and lead-coordination plans/tasks. |
| `polyraspad-frontend/` | Next.js 16 application (App Router) — the main learning UI. |
| `polyraspad-landing/` | Next.js 16 marketing landing page (localized `ru/en/ko`). |
| `AggregatorService/` | Public-facing ASP.NET Core REST API / BFF. Proxies to downstream gRPC services. |
| `VocabularyService/` | Core domain service: projects, decks, cards, terms, study sessions, FSRS scheduling, analytics, marketplace. |
| `AgentService/` | AI assistant / agent threads and orchestration service. |
| `MediaService/` | Object-storage proxy (S3-compatible, default MinIO) for images, audio, documents, reader library books. |
| `BillingService/` | SaaS billing microservice: provider-agnostic subscriptions, entitlements, gRPC API (port `5127`). Submodule; flat Polyraspad layout (not full ZukoSun nested template). |
| `authorization-module/authorization-module.API/` | Identity service: ASP.NET Core Identity, JWT auth, email confirmation, gRPC auth. |
| `inclusive/` | Python gRPC microservice: FSRS card scheduling and text tokenization/lemmatization. |
| `inoriginal-capture-extension/` | Chrome Manifest V3 extension for capturing subtitles, audio, screenshots, and sending cards to Anki. |
| `*.Tests/` | Backend test projects (xUnit). |
| `docker/` | Postgres init scripts and related Docker assets. |
| `deploy/nginx/` | Production nginx reverse-proxy configuration. |

### Important: Git submodules

The following directories are **Git submodules** pointing to separate repositories under `https://github.com/Kamil-Zuki/`:

- `AggregatorService`
- `VocabularyService`
- `authorization-module`
- `polyraspad-frontend`
- `inclusive`
- `BillingService`

Changes inside these directories affect their own repositories. When you commit/push, treat submodule content separately from the root repository. The root `.gitmodules` declares them with `branch = master`.

### Two kinds of “plans” (do not confuse)

| Location | Purpose |
|----------|---------|
| `context/plans/` (`active/`, `backlog/`, `completed/`, … per `context/README.md`) | Product and implementation plans for the team; stable narrative of what we build. |
| `.cursor/plans/` + `.cursor/tasks/` | Short-lived coordination for multi-agent runs (`lead-agent`); moves **backlog → active → archive** when work finishes. |

> **Note:** `context/plans/` is referenced by `context/README.md` and `context/agents/AGENTS.md` but does **not currently exist** in the working tree. Active Cursor coordination plans live in `.cursor/plans/active/` instead.

---

## 2. Technology Stack

### Frontend

- **Framework:** Next.js 16, React 19, TypeScript (strict mode).
- **Router:** App Router only (`polyraspad-frontend/src/app/`); no `pages/` directory.
- **Styling:** Tailwind CSS v4 (CSS-first via `@import "tailwindcss"` in `globals.css`).
- **UI primitives:** Radix UI + `lucide-react`, `framer-motion`, `sonner`, `tailwindcss-animate`.
- **State / data fetching:** TanStack Query (`@tanstack/react-query`) with devtools.
- **Testing:** Vitest 4 + jsdom + React Testing Library.
- **Node version:** 22 (Docker images use `node:22-alpine`).

### Backend (.NET)

| Service | Target Framework | Runtime Port | Main Role |
|---------|------------------|--------------|-----------|
| `AggregatorService` | .NET 10 | `5206` (host `5000`) | Public REST API / BFF |
| `AgentService` | .NET 10 | `5131` | AI agent threads |
| `authorization-module.API` | .NET 10 | `5027` | Identity / auth |
| `VocabularyService` | .NET 8 | `5117` | Core vocabulary domain |
| `MediaService` | .NET 8 | `5121` | Media storage proxy |
| `BillingService` | .NET 8 | `5127` | SaaS billing / entitlements |

> **Known inconsistency:** three services run on .NET 10 while `VocabularyService` and `MediaService` remain on .NET 8. Prefer existing patterns in each project and avoid cross-service package version changes unless required.

Common libraries across .NET services:

- ASP.NET Core (Web API or gRPC server)
- gRPC (`Grpc.AspNetCore.Server`, `Grpc.Net.Client`, `Grpc.Tools`, `Google.Protobuf` 3.33.2)
- Entity Framework Core + Npgsql PostgreSQL provider
- AutoMapper 16.1.1
- FluentValidation 11.11.0
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Swashbuckle.AspNetCore 8 (Swagger)

### Python microservice

- **Runtime:** Python 3.11 (`python:3.11-slim` Docker image).
- **Server:** gRPC (`grpcio`, `grpcio-tools`, `protobuf`).
- **FSRS:** `fsrs>=4.0.0`.
- **NLP:** `nltk>=3.9.1` for tokenization, lemmatization, POS tagging.
- **Package manager:** `pip` + `requirements.txt` only; no `pyproject.toml`.
- **Tests:** `pytest` (`test_fsrs_review_card.py`).

### Infrastructure

- **Postgres** 13 (host port `5434`; init script creates `auth-module`, `vocabulary_service`, `agent_service`).
- **Redis** 7 (host port `6379`) — used by `VocabularyService`.
- **MinIO** (host ports `9000` API / `9001` console) — S3-compatible storage, bucket `polyraspad-media`.
- **Docker Compose** — single command local runtime.
- **Nginx** — production reverse proxy with Let’s Encrypt SSL.

---

## 3. Service Architecture & Communication

```
Browser / Extension
        │
        ▼
+----------------------------------------------------+
|  polyraspad-frontend  :3000                        |
|  (Next.js App Router, BFF /api/ai/*)               |
+----------------------------------------------------+
        │
        ▼
+----------------------------------------------------+
|  aggregator-service  :5000 (container :5206)       |
|  REST API ──┬── gRPC vocabulary-service :5117      |
|             ├── gRPC agent-service     :5131       |
|             ├── gRPC media-service     :5121       |
|             ├── gRPC billing-service   :5127       |
|             └── gRPC authorization-module :5027    |
+----------------------------------------------------+
        │
        ├──► vocabulary-service ──┬──► inclusive (Python) :40051
        │                         ├──► media-service :5121
        │                         └──► billing-service :5127 (entitlements)
        ├──► agent-service ───────► vocabulary-service
        ├──► media-service ───────► minio :9000
        └──► authorization-module ─► postgres :5432
```

- **External traffic** enters through `polyraspad-frontend` and `aggregator-service` only.
- **Internal services** use gRPC over plaintext HTTP/2 (`h2c`) with `SocketsHttpHandler.Http2UnencryptedSupport` enabled.
- **Migrations** are applied automatically at container startup for `authorization-module`, `VocabularyService`, and `AgentService` via `db.Database.Migrate()`.

### gRPC Proto Contract Matrix

| Proto | Server | Clients |
|-------|--------|---------|
| `vocabulary.proto` | `VocabularyService` | `AgentService`, `AggregatorService` |
| `agent.proto` | `AgentService` | `AggregatorService` |
| `media.proto` | `MediaService` | `VocabularyService`, `AggregatorService` |
| `authorization.proto` | `authorization-module` | `AggregatorService` |
| `billing.proto` | `BillingService` | `AggregatorService`, `VocabularyService` |
| `vocabulary-client.proto` | — | `AgentService` |
| `Inclusive/vocab.proto` | `inclusive` (Python) | `VocabularyService` |

---

## 4. Code Organization

### Backend (.NET) conventions

Each service follows a similar layered layout:

```
<Service>/
├── Program.cs                 # Entry point, DI registration, Kestrel config
├── <Service>.csproj           # Package references and proto definitions
├── Dockerfile
├── Controllers/               # REST controllers (Aggregator, authorization-module)
├── Grpc/                      # gRPC service implementations
├── Services/                  # Domain / business services
├── Data/                      # EF Core DbContext, migrations, entities
├── Dtos/                      # Request/response DTOs
├── Options/                   # Options-pattern config classes
├── Protos/                    # .proto contract files
├── Mappers/ or AutoMapperProfiles/  # AutoMapper profiles
└── Validations/               # FluentValidation validators
```

Key patterns:

- **Options pattern:** every service binds config via `builder.Services.Configure<TOptions>(...)`. Environment variables override settings in Docker Compose (e.g., `Storage__Endpoint`, `Jwt__Secret`).
- **Constructor injection** and typed `HttpClient` / gRPC client factory.
- **REST API style:** controller-based only. **Do not introduce Minimal APIs.**
- **C# style:** nullable reference types enabled (`<Nullable>enable</Nullable>`), implicit usings enabled, records for DTOs, async/await **without** `ConfigureAwait(false)`, collection expressions, pattern matching.
- **Migrations:** keep them non-destructive. Add nullable columns first, backfill data, then make required in a later migration.

### Frontend (Next.js) conventions

```
polyraspad-frontend/src/
├── app/                       # App Router pages and API routes
│   └── api/ai/                # BFF routes: generate, models, mining-draft
├── components/                # Feature + shared UI components
├── contexts/                  # Auth, Project, Editor, React-Query providers
├── hooks/                     # Custom React hooks
├── lib/
│   ├── api/                   # Service clients
│   ├── editor/                # Card/template rendering, AI patches
│   ├── agent/                 # Agent chat logic, tool registry, prompts
│   ├── react-query/           # Query keys/hooks
│   ├── server/                # Server-only BFF helpers
│   └── utils/                 # cn, media preview, CSV parsing
├── assets/
└── test/setup.ts              # Vitest setup
```

Key patterns:

- **Server Components by default.** Use `'use client'` only when interactivity or browser APIs are needed.
- **Data fetching:** default `cache: 'no-store'`; use `force-cache` or `next.revalidate` explicitly.
- **Path alias:** `@/*` maps to `./src/*`.
- **Types:** strict TypeScript; ESLint warns on `@typescript-eslint/no-explicit-any`.
- **Styling:** Tailwind only; avoid CSS modules. Design tokens include `bg-app-bg`/`bg-app-surface`/`bg-app-hover`, `text-brand-primary`/`text-brand-secondary`, and status colors (NEW blue, SAVED/LINGQ yellow, KNOWN white, IGNORED muted).

### Inclusive (Python)

```
inclusive/
├── main.py                    # gRPC server + VocabServiceServicer
├── config.json                # {"server_port": 40051}
├── requirements.txt
├── proto/
│   ├── vocab.proto            # Service definition
│   ├── vocab_pb2.py           # Generated messages
│   └── vocab_pb2_grpc.py      # Generated stubs
└── test_fsrs_review_card.py   # pytest contract tests
```

---

## 5. Build & Test Commands

### Full local stack (recommended)

From the repository root:

```powershell
# Copy environment template and fill secrets
cp .env.example .env

# Build and start everything
docker compose down
docker compose up --build -d

# Verify
docker compose ps
curl http://localhost:5000/healthz   # {"status":"ok"}
```

### Frontend only

```powershell
cd polyraspad-frontend
npm ci
npm run dev        # localhost:3000
npm run build
npm run lint
npm test -- --watchAll=false
```

### Backend (.NET)

Each service is built independently (no root solution file exists):

```powershell
cd AggregatorService
dotnet restore
dotnet build -c Release --no-restore
dotnet test ../AggregatorService.Tests/AggregatorService.Tests.csproj -c Release

cd ../BillingService
dotnet restore
dotnet build -c Release --no-restore
dotnet test ../BillingService.Tests/BillingService.Tests.csproj -c Release

cd ../VocabularyService
dotnet restore
dotnet build -c Release --no-restore
dotnet test ../VocabularyService.Tests/VocabularyService.Tests.csproj -c Release

cd ../authorization-module
dotnet restore
dotnet build -c Release --no-restore
```

> `authorization-module` is the only service with a `.sln` file: `authorization-module/authorization-module.sln`.

### Python inclusive

```bash
cd inclusive
pip install -r requirements.txt
python main.py

# Regenerate proto stubs if changed
python -m grpc_tools.protoc -I. --python_out=. --grpc_python_out=. proto/vocab.proto

# Tests
pytest test_fsrs_review_card.py
```

### Browser extension

```powershell
cd inoriginal-capture-extension
npm install
npm run build      # sync-public + vite build
npm run typecheck
```

---

## 6. Development Workflow

### First-time setup

1. Ensure Docker Desktop is running.
2. Copy `.env.example` → `.env` and fill required secrets:
   - `POSTGRES_PASSWORD`
   - `MINIO_ROOT_PASSWORD`
   - `JWT_SECRET` (at least 32 characters)
   - `SMTP_HOST`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_ADDRESS`
   - `AUTH_CONFIRMATION_LINK`
   - `AI_PROXY_API_KEY` (shared secret between Next.js and Aggregator; not the OpenAI/Mistral key)
3. Run `docker compose up --build -d`.

### Rebuilding a single service

```powershell
docker compose up --build -d <service-name>
```

Useful service names: `aggregator-service`, `vocabulary-service`, `authorization-module`, `polyraspad-frontend`, `agent-service`, `media-service`.

### Restart without rebuild

```powershell
docker compose restart <service-name>
```

### Common recovery

```powershell
# Recreate everything
docker compose down
docker compose up --build -d

# Recreate one service from scratch
docker compose rm -sf aggregator-service
docker compose up --build -d aggregator-service

# Inspect logs
docker compose logs aggregator-service --tail 100
docker compose logs -f vocabulary-service
```

### Non-Docker debugging

Allowed only for narrow debugging. Keep infrastructure in Compose and always verify the final result through `docker compose`. This is especially important for:

- gRPC between `AggregatorService` ↔ `authorization-module` / `VocabularyService`
- MinIO media URL resolution
- CORS behavior

---

## 7. Environment Configuration

Copy `.env.example` to `.env` and adjust values. Key groups:

| Group | Variables |
|-------|-----------|
| Postgres | `POSTGRES_USER`, `POSTGRES_PASSWORD` |
| MinIO | `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD`, `MINIO_PUBLIC_BASE_URL`, `MINIO_SERVER_FETCH_BASE_URL` |
| JWT (shared by `authorization-module` and `aggregator-service`) | `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` |
| Email | `AUTH_CONFIRMATION_LINK`, `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_ADDRESS`, `SMTP_DISPLAY_NAME` |
| Frontend URLs | `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_APP_URL`, `LANDING_NEXT_PUBLIC_APP_URL`, `LANDING_NEXT_PUBLIC_SITE_URL` |
| CORS | `CORS_ALLOWED_ORIGINS` |
| AI completion | `OPENAI_API_KEY`, `AI_COMPLETION_BASE_URL`, `AI_COMPLETION_MODEL`, `AI_COMPLETION_ENABLED` |
| AI proxy / BFF | `AI_PROXY_API_KEY` (must match `Ai__ProxyApiKey` in Aggregator) |
| Billing (SaaS) | `BILLING_DEFAULT_PROVIDER` (`mock` in dev), `BILLING_GRACE_PERIOD_DAYS`, `BILLING_WEBHOOK_API_KEY` (Aggregator webhook proxy), `YOOKASSA_*` for production payments |
| TTS | `AI_TTS_PROVIDER` (`espeak` for local Docker, `mistral` for external), `AI_TTS_MODEL`, `AI_TTS_VOICE_ID` |

### AI notes

- AI features flow through an external OpenAI-compatible API (key on the Aggregator side) and BFF routes in Next.js (`/api/ai/*`).
- The shared secret between Next.js and Aggregator is the `X-Ai-Proxy-Key` header.
- For free local TTS in Docker, set `AI_TTS_PROVIDER=espeak`; the Aggregator image includes `espeak-ng`.
- For Mistral TTS, you must provide a real saved voice ID in `AI_TTS_VOICE_ID`; OpenAI-style names like `alloy` or the placeholder `neutral_female` are invalid.

---

## 8. Code Style Guidelines

### C#

- Nullable reference types enabled everywhere.
- Use records for DTOs where appropriate.
- Use `async`/`await`; do **not** add `ConfigureAwait(false)`.
- Prefer collection expressions and pattern matching where the existing code already does.
- Constructor injection only; avoid service location.
- Keep controllers thin; domain logic belongs in `Services/`.
- DTOs, gRPC contracts, and frontend types must stay synchronized.

### TypeScript / Next.js

- Strict TypeScript; avoid `any`.
- Use Server Components by default.
- Use React Query patterns for server state.
- Use Tailwind for styling; avoid CSS modules.
- Use `lucide-react` for icons and Radix primitives for accessible components.

### General

- Prefer existing project patterns over new abstractions.
- Keep changes scoped to the user request.
- Do not revert unrelated user changes.
- Search with `rg` (ripgrep) before proposing architecture.
- Make small, reviewable edits.

---

## 9. Testing Strategy

### Backend

| Test Project | Target | Stack | Style |
|--------------|--------|-------|-------|
| `AggregatorService.Tests` | .NET 10 | xUnit, FluentAssertions 6, Moq, `Microsoft.AspNetCore.Mvc.Testing` | Integration tests via `WebApplicationFactory<Program>`; gRPC clients replaced with mocks; custom `TestAuthHandler`. |
| `BillingService.Tests` | .NET 8 | xUnit, FluentAssertions, EF Core InMemory | Access, entitlements, webhook idempotency; lives in **root repo** (not submodule). |
| `AgentService.Tests` | .NET 10 | xUnit, FluentAssertions 8, Moq, EF Core InMemory | Unit/service tests with in-memory `AgentServiceContext`. |
| `VocabularyService.Tests` | .NET 8 | xUnit, FluentAssertions 6, Moq, EF Core InMemory + SQLite | Unit + repository-style tests; `StudyServiceTestFactory` wires real services; covers study queue, FSRS, analytics, terms, cards. |

Guidance:

- Add unit/integration tests for service behavior changes.
- Add integration tests for API contracts, DTO/gRPC shape changes, and new endpoints.
- Add migration tests when data preservation matters.

### Frontend

- Vitest + jsdom + React Testing Library + user-event.
- Component/page tests for reader interactions.
- API client tests for contract changes.

### Mandatory LingQ regression tests

Any change touching vocabulary status, duplicates, or the reader must preserve:

- `sleep` and `slept` have separate statuses.
- `go` and `went` are not duplicate cards.
- An exact same phrase is a duplicate; component words are not.
- Page turn marks blue terms known **only** when the setting is enabled.
- Creating a card preserves the exact form/phrase.

---

## 10. CI/CD & Deployment

### GitHub Actions

- **`.github/workflows/ci.yml`** (monorepo, branch `master`)
  - Parallel jobs per deployable: `VocabularyService`, `AggregatorService`, `BillingService`, `AgentService`, `MediaService`, `authorization-module`, `inclusive`, `polyraspad-frontend`, `polyraspad-landing`
  - Integration tests run from root test projects (`*Service.Tests/`) where applicable
  - Final `docker` job: `docker compose build` (depends on all jobs above)
  - Private submodules require GitHub secret **`SUBMODULES_PAT`** (classic PAT with `repo` scope for `Kamil-Zuki/*`); jobs init only the submodule they need, except `docker` which uses `submodules: recursive`

- **Submodule repos** (own `.github/workflows/ci.yml`, triggered on push to submodule remote):
  - `BillingService`, `AggregatorService`, `VocabularyService`, `authorization-module`, `polyraspad-frontend`, `inclusive`
  - Each runs standalone build (+ Docker build for .NET/Node services; pytest for `inclusive`)

- **`.github/workflows/deploy.yml`**
  - Triggered manually (`workflow_dispatch`)
  - SSHs into the VPS, resets to `origin/master`, updates submodules, `docker compose build`, `docker compose up -d`, checks `GET /healthz` on aggregator

### Production nginx

`deploy/nginx/polyraspad.conf` routes:

- `polyraspad.online` → landing `:3002`
- `app.polyraspad.online` → frontend `:3000`
- `api.polyraspad.online` → aggregator `:5000`
- `/polyraspad-media/` → MinIO `:9000`
- SSL via Let’s Encrypt.

---

## 11. Documentation Boundaries

- **`Docs/`** — official, stable docs for humans. **STEOS microservice documentation** (folders `01`–`05`, `99 - Staging`) is governed by **`Docs/.cursor/`**, not by root `.cursor/`.
- **`context/`** — operational memory: active plans, agent instructions, rules, skills, prompts, research, ADRs-in-progress.
- **`.cursor/`** (repo root) — Cursor-executable instructions for **code, tests, and lead coordination**: agents, commands, auto rules, plans/tasks.
- **`Docs/.cursor/`** — Cursor-executable instructions for **generating and auditing STEOS docs**: rules (`steos-docs-*`), skills, subagents.

Promotion rule: when a `context/` document becomes stable and user-facing, keep the implementation trace in `context/` and add/update the official version in `Docs/`. Link them rather than duplicating large sections.

### `Docs/.cursor/` — STEOS documentation rules

When creating or editing files under `Docs/` (especially per-service folders and `Шаблон документации микросервиса STEOS/`), follow **`Docs/.cursor/rules/`**. Naming and groups — [`Docs/.cursor/rules/README.md`](Docs/.cursor/rules/README.md).

| Group | Rule(s) | Scope |
|-------|---------|-------|
| **G0 — Core** | `steos-docs-core.mdc` (`alwaysApply`) | Order `03→01→02→04`, BFF topology, anti-hallucination, staging |
| **G1 — Folders 01/02/03/05** | `steos-docs-folders-010305.mdc`, `steos-docs-staging-0103.mdc`, `steos-docs-staging-issues.mdc` | SR blocks, КАР, TOC; mandatory `01↔03` cross-check → ISSUE in `99` |
| **G2 — Folder 04 coordinator** | `steos-docs-folder-04-coordinator.mdc` | Tree, contract layers, alignment, consistency |
| **G3 — Folder 04 subfolders** | `steos-docs-folder-04-{dto,grpc,rest-api,socket,integrations,rabbitmq,redis,algorithms}.mdc` | Block templates per subfolder |

**Skills** (batch workflows for folder `04`): [`Docs/.cursor/skills/README.md`](Docs/.cursor/skills/README.md)

| Skill | Use when |
|-------|----------|
| `steos-docs-04-coordinator` | Plan or fill entire `04` for a service (manifest, batches) |
| `steos-docs-04-write` | Write/update `04` files from manifest (templates stay in rules) |
| `steos-docs-04-verify` | Readonly audit `04` vs `01`/`03`/`02`; ISSUE in `99 - Staging` |

**Subagents** (`Docs/.cursor/agents/`): `docs-04-coordinator`, `docs-04-writer`, `docs-04-verifier`.

Key constraints (details in G0/G1 rules):

- **`03` is read-only** unless the user explicitly asks to edit the data model.
- On **`01↔03` mismatch** → write ISSUE + update `00 - Реестр проблем.md` in `99 - Staging — Разрывы согласованности (DO NOT DELETE)/`; do not silently patch the other folder.
- Do not write **`04`** before **`01`/`03`** are stable.

### Formatting etalon — `(Done) Authorization Service/`

`Docs/(Done) Authorization Service/` exists **only as a completed example of how to format** subsequent microservice documentation. Use it to see:

- folder and file naming (`01`–`05`, group files, `00 - Общая информация`, etc.);
- heading hierarchy, table layouts, and block order per document type;
- expected depth and granularity for each section.

**Do not treat Auth as a content template:**

- do **not** copy, paraphrase, or reuse Auth **text** (SR descriptions, scenarios, DTO fields, endpoints, Redis keys, Rabbit flows, domain terms);
- do **not** paste Auth fragments as «examples» inside another service's docs;
- fill every new service from **that service's** `03` → `01` → `02` → `04`, plus `Docs/Шаблон документации микросервиса STEOS/` for short layout copies.

Do not rewrite `(Done) Authorization Service/` without an explicit user request. Pair with `Docs/.cursor/rules/steos-docs-core.mdc` → *Reference Artifacts*.

### Required first reads for non-trivial work

**Code / product implementation:**

1. `context/README.md`
2. `context/agents/AGENTS.md`
3. Relevant files under `context/rules/` for the area you touch.
4. Relevant **active** plan under `.cursor/plans/active/` when the work follows that roadmap.
5. `.cursor/rules/01-repo-operating-model.mdc`
6. If coordinating via `lead-agent`: `.cursor/plans/README.md` and `.cursor/tasks/README.md`.

**STEOS docs under `Docs/`:**

1. `Docs/.cursor/rules/steos-docs-core.mdc` (always applies when editing docs)
2. `Docs/.cursor/rules/README.md` — pick G1–G3 rules for the folder you edit
3. For batch `04` work: `Docs/.cursor/skills/README.md` and the matching `steos-docs-04-*` skill

---

## 12. Agent Work Rules

- Read the active plan and relevant rules before editing.
- Inspect code with `rg` before proposing architecture.
- Make the smallest useful implementation step.
- Verify with the narrowest useful test or build command.
- Update the plan when scope, risk, or implementation order changes.
- Prefer existing project patterns; avoid introducing new abstractions.
- Keep edits focused and reviewable; never revert unrelated user changes.
- Document new recurring rules in `context/rules/`, `.cursor/rules/`, or `Docs/.cursor/rules/` (STEOS docs).
- Document reusable workflows in `context/skills/`, `.cursor/skills/`, or `Docs/.cursor/skills/` (STEOS docs).
- If you change files/styles/structures/workflows described in this `AGENTS.md`, update this file.

### Specialist agent roles (Cursor subagents)

**Repo root** (`.cursor/agents/`):

- `lead-agent` — multi-area coordination.
- `product-agent` — behavior, acceptance criteria, LingQ rules (read-only).
- `backend-agent` — .NET services, controllers, gRPC, EF Core, migrations.
- `frontend-agent` — Next.js UI, Reader UX, React Query, API clients.
- `reviewer-agent` — regressions, unsafe migrations, contract mismatches (read-only).

**STEOS docs** (`Docs/.cursor/agents/`):

- `docs-04-coordinator` — manifest and batch orchestration for folder `04`.
- `docs-04-writer` — write/update `04` files using block templates from `Docs/.cursor/rules/`.
- `docs-04-verifier` — readonly consistency audit; ISSUE output to `99 - Staging`.

---

## 13. Current Product Direction

The active learning direction is the **LingQ-style reader model**:

- Learn through real word forms and phrases.
- Do **not** use lemmas as the basis for knowledge status, duplicate checks, statistics, or card creation.
- Duplicate detection uses exact normalized term/phrase (`trim + lowercase`).
- `ProjectTerm` stores `Text`, `NormalizedText`, `Type` (`WORD`/`PHRASE`), and `Language`.
- `UserTermStatus` stores status (`NEW`, `SAVED`, `KNOWN`, `IGNORED`; legacy `LINGQ`/`LEARNING` still exist in DB/UI mapping).
- The reader is the primary learning surface; word actions must not force navigation away.
- Phrase highlight has priority over individual word highlights.

---

## 14. Security Considerations

- **Never commit secrets.** `.env` is ignored; only `.env.example` is tracked.
- **JWT secret** must be at least 32 characters and shared between `authorization-module` and `aggregator-service`.
- **AI proxy key** (`AI_PROXY_API_KEY` / `Ai__ProxyApiKey`) is a shared secret between Next.js BFF and Aggregator; it is **not** the OpenAI/Mistral provider key.
- **gRPC internal traffic** currently uses plaintext HTTP/2 (`h2c`) inside the Docker network.
- **MinIO bucket** `polyraspad-media` is configured for public read access via `minio-init`.
- **EF Core migrations** run automatically on container startup. This is convenient for Docker but risky for production schema changes; review migrations for destructive operations.
- **Do not run destructive migrations** without a backfill plan and a migration test.
- The deploy workflow uses password-based SSH; ensure the VPS credentials are rotated and stored only in GitHub secrets.

---

## 15. Known Inconsistencies & Caveats

1. **Mixed .NET versions:** `AggregatorService`, `AgentService`, and `authorization-module` target .NET 10; `VocabularyService` and `MediaService` target .NET 8.
2. **No root .NET solution file:** only `authorization-module/authorization-module.sln` exists.
3. **`context/plans/` directory does not exist** even though `context/README.md` and `context/agents/AGENTS.md` reference it. Active Cursor coordination plans currently live in `.cursor/plans/`.
4. **Submodules:** six major components are separate Git repositories; changes inside them require separate submodule commits/pushes.
5. **Migrations at startup:** convenient for local Docker but should be reviewed carefully for production.
