---
name: commit
description: Stages changes, writes conventional commit messages, commits and pushes in ALL subrepositories (submodules) and root. Each child repo is committed and pushed; then root is updated and pushed. Use when the user asks to commit, push, make a commit, or save changes to git.
---

# Commit and Push (All Subrepositories)

## When to use

Apply this skill when the user asks to:
- commit (changes)
- push
- make a commit
- save changes to git

## Mandatory behavior

- **Commit all subrepositories**: Every submodule with changes must be committed and pushed.
- **Push after each commit**: Never leave a repo committed without pushing. Push child repos first, then root.

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
- If a directory appears as modified but file changes are not visible in the root repo, it is likely a **child repo/submodule**; inspect it from inside that directory.

### 2. Where to commit

- **Only child repo/submodule files changed** (e.g. under `polyraspad-frontend/`):  
  Run git commands from that child repo first. Then update the parent repo’s submodule reference (step 4).
- **Only root files changed** (e.g. `AggregatorService/`, `Docs/`, `.cursor/`):  
  Run git commands from the repo root.
- **Both root and child repos changed:** Commit and push in each child repo first, then commit and push in root.

### 3. Stage, commit, push (inside each repo that has direct file changes)

- Stage the files that belong to the commit (avoid build artifacts: `bin/`, `obj/`, `.next/`, `node_modules/`).
- Commit with a message following the format above.
- Push (e.g. `git push`). Use `git_write` and `network` permissions when running git commands.

### 4. If you committed in one or more child repos/submodules

- From **repo root**: stage each updated child repo path, commit the updated submodule references, then push root.
- Example:
  - `git add polyraspad-frontend`
  - `git commit -m "chore: update polyraspad-frontend (<short reason>)"`
  - `git push`
- If several child repos changed, stage all of them in the same root commit when they belong to one logical change.

### 5. Order of operations (commit then push each; all subrepos)

1. For each child repo with changes: commit, then push immediately.
2. Commit root repo with updated child repo references, then push root.
3. Never commit without pushing — push after every commit.

### 6. Multi-repo requirement (all subrepos, push always)

- **All** subrepos with changes must be committed and pushed — never skip a changed child repo.
- Do not collapse multiple child repos into a root-only commit.
- Create a real commit in each changed child repo first, push immediately after each.
- Then create one root commit that records the updated child repo references, then push root.
- Push is mandatory: push child repos first, then push root. Report completion with commit SHAs for each.

## Shell notes

- **PowerShell:** Use `;` to chain commands, not `&&`.
- **Paths:** Prefer the workspace root or the relevant child repo as the working directory for git; use absolute or repo-relative paths as appropriate.

## Checklist

- [ ] Commit message follows conventional format (type + optional scope + description).
- [ ] Only intended source/config files staged (no bin, obj, .next, node_modules).
- [ ] **All** changed child repos/submodules were committed and pushed (each pushed right after its commit).
- [ ] Parent repo references to child repos/submodules were updated and pushed.
