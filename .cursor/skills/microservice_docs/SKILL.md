---
name: Microservice Documentation Rules
description: Provides guidelines, rules, and structures for writing, maintaining, or updating microservice documentation. Triggers when asked to write or update documentation.
---

# Microservice Documentation Skill

You must strictly follow the corporate documentation standards when writing or modifying documentation in this repository.

## Rule References
Detailed rules for each aspect of documentation can be found in the `references/` subdirectory of this skill. Whenever you are tasked with creating or editing documentation, **read the relevant reference files first**:

- **Core Principles**: `references/docs-core.md` (Core standards and file structure conventions)
- **Functional & Data Models (Folders 01, 03)**: `references/docs-folders-010305.md`
- **Backend & Contracts (Folder 04)**:
  - `references/docs-folder-04-grpc.md` (gRPC APIs)
  - `references/docs-folder-04-rest-api.md` (REST APIs)
  - `references/docs-folder-04-dto.md` (Data Transfer Objects)
  - `references/docs-folder-04-coordinator.md` (Coordinator patterns)
  - `references/docs-folder-04-integrations.md` (Integrations)
  - `references/docs-folder-04-rabbitmq.md` (RabbitMQ events)
  - `references/docs-folder-04-redis.md` (Redis caching)
  - `references/docs-folder-04-socket.md` (WebSockets)
  - `references/docs-folder-04-algorithms.md` (Backend Algorithms)
- **Staging & Consistency Management**:
  - `references/docs-staging-0103.md` (Handling disparities between 01 and 03)
  - `references/docs-staging-issues.md` (Recording ISSUES in 99 - Staging)

## Agent Instructions
1. **Never guess the structure**: If you are writing documentation for a service, read `docs-core.md` and the appropriate folder-specific rules.
2. **Follow the dependency chain**: Remember the order 03 -> 01 -> 02 -> 04. Folder 04 should not be finalized before 01 and 03 are stable.
3. **Handle inconsistencies properly**: If you detect a mismatch between the data model (03) and functional spec (01), do not silently fix it—follow the Staging rules to record an issue.
