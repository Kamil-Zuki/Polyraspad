# Backend Agent Task

Plan ID: `agent-persistence-phase4`
Agent: `backend-agent`
Status: done
Can run in parallel: no (starts after product locks lifecycle, or parallel on schema-only draft)

## Objective

Implement agent thread persistence in VocabularyService (Postgres + gRPC) and expose REST endpoints via Aggregator `AgentController`.

## Inputs

- Plan: `.cursor/plans/backlog/agent-persistence-phase4.plan.md`
- Decision: `context/decisions/agent-persistence-model.md`
- Patterns: `VocabularyService/Services/AnalyticsService.cs`, `AggregatorService/Controllers/AnalyticsController.cs`
- Proto: `VocabularyService/Protos/vocabulary.proto`

## Scope

### VocabularyService

- EF entities: `AgentThread`, `AgentMessage`, `AgentRun`, `AgentToolCall`, `AgentDomainDecision` (+ optional `AgentArtifact`)
- Non-destructive migration
- `IAgentService` / `AgentService` with user + project scoping
- `AgentGrpcService`
- FluentValidation for gRPC requests
- Verify `Project.UserId` before thread access

### Aggregator

- `AgentController` at `/api/agent`
- DTOs + AutoMapper profiles
- `IVocabularyServiceClient` agent methods
- Integration tests (`AgentControllerTests`, VocabularyService agent tests)

## Out of Scope

- Server-side `executeAgentTool` / LLM calls
- Separate AgentService microservice
- Streaming endpoints
- Frontend changes

## Deliverables

- gRPC `AgentService` in vocabulary.proto (sync Aggregator proto copy)
- Migration + entities + service + grpc service
- REST controller + tests
- Security: cross-user thread access returns 404/403

## Verification

```bash
dotnet test VocabularyService.Tests --filter "FullyQualifiedName~Agent"
dotnet test AggregatorService.Tests --filter "FullyQualifiedName~Agent"
```

## Handoff

- REST/gRPC DTO shapes (final camelCase REST names)
- migration file path
- any deviations from plan (e.g. artifacts deferred)
- blockers for frontend integration
