# Commit and Push (All Subrepositories)

Apply the **commit** skill to stage, commit, and push changes across the full monorepo.

## Invocation

Use `/commit` or run this command when the user asks to commit, push, or save changes to git.

## What This Does

1. **Commit all subrepositories** — Every submodule/subrepo with changes gets its own commit
2. **Push after each commit** — Each child repo is pushed immediately after its commit; root is pushed last
3. **Update parent references** — Root repo records updated submodule SHAs and is pushed

## Submodules in This Repo

- `AggregatorService`
- `VocabularyService`
- `authorization-module`
- `polyraspad-frontend`
- `inclusive`

Execute the commit skill workflow. Ensure **all** changed subrepos are committed and pushed before the root.
