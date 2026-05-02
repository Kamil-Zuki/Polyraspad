# Implementation Workflow

Use this workflow for feature work.

1. Read the active plan in `context/plans/active/`.
2. Find existing code paths with `rg`.
3. Identify the narrowest first vertical slice.
4. Update backend contracts before frontend consumers when API shape changes.
5. Add or update focused tests.
6. Run the smallest useful verification command.
7. Update the active plan if implementation order changes.
8. Move completed plans to `context/plans/completed/` only after the feature is merged or clearly finished.
