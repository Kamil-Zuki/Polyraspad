# Detroit TDD, Then Commit and Push

Same as **/tdd**, but when TDD is complete, automatically run **/commit**.

## Invocation

Use `/tdd-commit` when you want autonomous Detroit TDD and then to stage, commit, and push all changes.

## Sequence

1. **Run Detroit TDD** — Launch the **detroit-tdd-orchestrator** subagent (same as `/tdd`). Use the Task tool (`subagent_type: detroit-tdd-orchestrator`) and pass the task from the user's message. The orchestrator will compose tasks in `.cursor/tasks/`, delegate to workers, and run until verified complete.
2. **When TDD is done** — Apply the **commit** skill: stage, commit, and push in all subrepositories (each submodule with changes, then root). Ensure all changed subrepos are committed and pushed.

Execute step 1 first. After the orchestrator reports completion (or the user confirms TDD is done), execute step 2 using the commit skill workflow.
