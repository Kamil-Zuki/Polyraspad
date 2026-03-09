# Detroit TDD Orchestration

Launch the **detroit-tdd-orchestrator** subagent for autonomous Detroit TDD. Use `/detroit-tdd-orchestrator` or the Task tool (`subagent_type: detroit-tdd-orchestrator`) and pass the task from the user's message.

The orchestrator will:
1. Compose tasks as MD files in `.cursor/tasks/`
2. Delegate each task to worker subagents
3. Coordinate and manage workers until verified complete
4. Delete the tasks folder when all done

Execute now.
