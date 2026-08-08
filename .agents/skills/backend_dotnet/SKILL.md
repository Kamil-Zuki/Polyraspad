---
name: Backend Patterns (.NET)
description: Explains standard architecture, frameworks, and patterns used in the ASP.NET Core backend services (gRPC, EF Core, FluentValidation). Triggers when modifying backend logic.
---

# Backend Skill (.NET)

This skill provides guidelines for developing ASP.NET Core backend services in Polyraspad (`AggregatorService`, `VocabularyService`, `AgentService`, etc.).

## 1. Architecture & Layers
All backend services follow a standard layered layout:
- `Controllers/`: REST endpoints (mostly in Aggregator or authorization).
- `Grpc/`: gRPC service implementations.
- `Services/`: Domain and business logic services. Keep controllers and gRPC handlers thin.
- `Data/`: EF Core DbContext, migrations, and entities.
- `Dtos/`: Request/Response models using `record` types.
- `Options/`: Strongly-typed configuration objects.
- `Protos/`: gRPC contracts.

## 2. Coding Standards
- Enable nullable reference types (`<Nullable>enable</Nullable>`).
- Use implicit usings.
- Use `async`/`await` without `ConfigureAwait(false)`.
- Use constructor dependency injection. Service Locator pattern is strictly forbidden.
- Use pattern matching and collection expressions where appropriate.

## 3. Entity Framework Core (EF Core)
- **Migrations:** All database changes must be non-destructive.
  - *Adding a required column:* First add it as nullable, write a backfill script/job, and then change it to required in a subsequent migration.
  - *Deleting columns/tables:* Strongly discouraged without a thorough review and explicit permission.
- Use `db.Database.Migrate()` on startup (already configured).

## 4. gRPC Communication
- Internal service-to-service communication happens via gRPC over plaintext HTTP/2 (`h2c`).
- Use typed `HttpClient` or `gRPC` client factories injected via DI.

## 5. Validation & Mapping
- Use **FluentValidation** (`Validations/` directory) for input validation.
- Use **AutoMapper** (`Mappers/` directory) for Entity -> DTO conversion.
