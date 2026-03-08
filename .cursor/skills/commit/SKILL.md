---
name: commit
description: Stages changes, writes conventional commit messages, and pushes. Handles monorepo with polyraspad-frontend submodule. Use when the user asks to commit, push, make a commit, or save changes to git.
---

# Commit and Push

## When to use

Apply this skill when the user asks to:
- commit (changes)
- push
- make a commit
- save changes to git

## Commit message format

Use **Conventional Commits**:

```
<type>(<scope>): <short description>

Optional body: one or more lines with details.
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `perf`

**Scope:** area of the codebase (e.g. `study`, `auth`, `library`, `api`). Omit scope for repo-wide or unclear scope.

**Examples:**

- `feat(study): Anki-style FSRS session with undo and SRS badges`
- `fix(auth): correct token refresh on 401`
- `chore: update polyraspad-frontend submodule`
- `docs: add API contract for study session`

Keep the first line under ~72 characters. Add a blank line and body only when extra context helps.

## Workflow

### 1. See what changed

- **Root repo:** `git status` (and `git diff --name-only` if needed).
- If `polyraspad-frontend` appears as modified, it is a **submodule**; changes live inside it.

### 2. Where to commit

- **Only frontend files changed** (e.g. under `polyraspad-frontend/src/`):  
  Run all git commands from `polyraspad-frontend/`. Then update the parent repo’s submodule reference (step 4).
- **Only root files changed** (e.g. `AggregatorService/`, `Docs/`, `.cursor/`):  
  Run git commands from the repo root.
- **Both:** Commit in submodule first, then in root (stage submodule, commit, push both).

### 3. Stage, commit, push (in the repo that has the changes)

- Stage the files that belong to the commit (avoid build artifacts: `bin/`, `obj/`, `.next/`, `node_modules/`).
- Commit with a message following the format above.
- Push (e.g. `git push`). Use `git_write` and `network` permissions when running git commands.

### 4. If you committed in polyraspad-frontend

- From **repo root**: `git add polyraspad-frontend` then `git commit -m "chore: update polyraspad-frontend (<short reason>)"` and `git push`.

## Shell notes

- **PowerShell:** Use `;` to chain commands, not `&&`.
- **Paths:** Prefer the workspace root or `polyraspad-frontend` as the working directory for git; use absolute or repo-relative paths as appropriate.

## Checklist

- [ ] Commit message follows conventional format (type + optional scope + description).
- [ ] Only intended source/config files staged (no bin, obj, .next, node_modules).
- [ ] If frontend was committed, parent repo’s submodule reference updated and pushed.
