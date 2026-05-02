# Testing Rules

## Backend

- Add unit or integration tests for service behavior changes.
- Test API contract mapping when DTO/gRPC shape changes.
- Test migrations when data preservation matters.

## Frontend

- Add component or page tests for reader interactions when practical.
- Test behavior, not styling details, unless visual state carries meaning.

## LingQ Regression Tests

Required cases for reader/vocabulary work:

- `sleep` and `slept` have separate statuses.
- `go` and `went` are not duplicate cards.
- exact same phrase is a duplicate; component words are not.
- page turn marks blue terms known only when the setting is enabled.
