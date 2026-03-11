# Detroit TDD, Then Commit and Push

Same as **/tdd**, but when TDD is complete, automatically run **/13-commit**.

## Invocation

Use `/tdd-13-commit` when you want autonomous Detroit TDD and then to stage, 13-commit, and push all changes.

## Sequence

1. **Run Detroit TDD** — Launch the **detroit-tdd-orchestrator** subagent (same as `/tdd`). Use the Task tool (`subagent_type: detroit-tdd-orchestrator`) and pass the task from the user's message. The orchestrator will compose tasks in `.cursor/tasks/`, delegate to workers, and run until verified complete.
2. **When TDD is done** — Apply the **13-commit** skill: stage, 13-commit, and push in all repositories. Ensure all changed repos are 13-committed and pushed.

Execute step 1 first. After the orchestrator reports completion (or the user confirms TDD is done), execute step 2 using the 13-commit skill workflow.
