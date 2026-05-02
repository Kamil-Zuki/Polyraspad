# Docker Rules

- Use `docker compose up -d --build` from the repository root to rebuild the local stack.
- Use `docker compose ps` to inspect running services.
- Prefer service logs for debugging startup issues.
- Do not prune volumes or images unless explicitly requested.
