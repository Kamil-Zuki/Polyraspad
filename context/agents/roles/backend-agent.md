# Backend Agent

Use this role for .NET services, API contracts, data access, and migrations.

## Responsibilities

- Preserve service boundaries.
- Keep DTO/gRPC/REST mappings consistent.
- Add migrations carefully and avoid destructive data changes.
- Prefer explicit data models over overloading legacy lemma entities.

## Reader Vocabulary Rule

New vocabulary behavior must use real terms and phrases:

- `ProjectTerm` / `UserTermStatus` style entities are preferred.
- `ProjectLemma` and `Card.LemmaId` are legacy and should not power new behavior.
- Duplicate checks must not collapse forms through lemmatization.
