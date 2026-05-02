# Skill: Docker Compose

Use this skill for local stack work.

## Commands

```powershell
docker compose up -d --build
docker compose ps
docker compose logs --tail=200 <service>
```

## Rules

- Run commands from the repository root.
- Do not remove volumes unless explicitly requested.
- Report which services are unhealthy or failing if startup does not complete.
