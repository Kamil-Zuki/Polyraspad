# 13-commit

Apply the **13-commit** skill to stage, commit, and push changes across the full monorepo.

## Invocation

Use `/13-commit` or run this command when the user asks to commit, push, or save changes to git.

## What This Does

1. **Commit all repositories** — Every repo with changes gets its own commit
2. **Push after each commit** — Each child repo is pushed immediately after its commit; root is pushed last

Execute the 13-commit skill workflow. Ensure **all** changed repos are committed and pushed.
