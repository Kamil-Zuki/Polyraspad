---
name: 13-commit
description: Stages changes, writes conventional commit messages in English, commits and pushes in all repositories (child repos and root). Use when the user asks to commit, push, make a commit, or save changes to git.
---

# Commit and Push (All Repositories)

## When to use

Apply this skill when the user asks to:
- commit (changes)
- push
- make a commit
- save changes to git

## Mandatory behavior

- **Commit all repositories**: Every repo with changes must be committed and pushed.
- **Push after each commit**: Never leave a repo committed without pushing. Push child repos first, then root.
- **English Only**: All commit messages MUST be written strictly in English, regardless of the language used in the codebase or prompts.

## Commit message format

Use **Conventional Commits** in English:

```
<type>(<scope>): <short description in English>

Optional body: one or more lines with details in English.
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`, `perf`

**Scope:** area of the codebase (e.g. `study`, `auth`, `library`, `api`). Omit scope for repo-wide or unclear scope.

**Examples:**

- `feat(study): Anki-style FSRS session with undo and SRS badges`
- `fix(auth): correct token refresh on 401`
- `docs: add API contract for study session`

Keep the first line under ~72 characters. Add a blank line and body only when extra context helps.

## Workflow

### 1. See what changed

- **Root repo:** `git status` (and `git diff --name-only` if needed).
- If a directory appears as modified but file changes are not visible in the root repo, inspect it from inside that directory (child repo).

### 2. Where to commit

- **Only child repo files changed:**  
  Run git commands from that child repo first, then from root if needed.
- **Only root files changed:** Run git commands from the repo root.
- **Both root and child repos changed:** Commit and push in each child repo first, then commit and push in root.

### 3. Stage, commit, push (inside each repo that has direct file changes)

- Stage the files that belong to the commit (avoid build artifacts: `bin/`, `obj/`, `.next/`, `node_modules/`).
- Commit with a message following the format above. MUST BE IN ENGLISH.
- Push (e.g. `git push`). Use `git_write` and `network` permissions when running git commands.

### 4. Order of operations (commit then push each; all repos)

1. For each child repo with changes: commit, then push immediately.
2. Commit and push root repo if it has changes.
3. Never commit without pushing — push after every commit.

### 5. Multi-repo requirement (push always)

- **All** repos with changes must be committed and pushed — never skip a changed child repo.
- Create a real commit in each changed child repo first, push immediately after each.
- Then commit and push root if it has changes.
- Push is mandatory: push child repos first, then push root. Report completion with commit SHAs for each.

## Shell notes

- **PowerShell:** Use `;` to chain commands, not `&&`.
- **Paths:** Prefer the workspace root or the relevant child repo as the working directory for git; use absolute or repo-relative paths as appropriate.

## Checklist

- [ ] Commit message is strictly in English.
- [ ] Commit message follows conventional format (type + optional scope + description).
- [ ] Only intended source/config files staged (no bin, obj, .next, node_modules).
- [ ] **All** changed child repos were committed and pushed (each pushed right after its commit).
- [ ] Root was committed and pushed if it had changes.
