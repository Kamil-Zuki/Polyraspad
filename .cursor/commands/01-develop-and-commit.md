# 01-develop-and-commit

Same as **/01-develop**, but when development is complete, automatically run **/13-commit**.

## Invocation

Use `/01-develop-and-commit` when you want autonomous development and then to stage, commit, and push all changes.

## Sequence

1. **Run Development** — Launch the **detroit-tdd-orchestrator** subagent (same as `/01-develop`). Use the Task tool (`subagent_type: detroit-tdd-orchestrator`) and pass the task from the user's message. The orchestrator will compose tasks in `.cursor/tasks/`, delegate to workers, and run until verified complete.
2. **When Done** — Apply the **13-commit** skill: stage, commit, and push in all repositories. Ensure all changed repos are committed and pushed.

Execute step 1 first. After the orchestrator reports completion (or the user confirms it is done), execute step 2 using the 13-commit skill workflow.
