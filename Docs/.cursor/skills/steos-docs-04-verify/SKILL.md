---
name: steos-docs-04-verify
description: Readonly audit of STEOS microservice folder 04 against 01/03/02 — consistency checks, ISSUE files in 99-Staging. Use after 04 writes, before marking 04 complete, or @docs-04-verifier.
---

# STEOS Docs — 04 Verifier

**Readonly** audit. Fix documentation only via ISSUE files — do not silently rewrite `01`/`03`/`04` unless user explicitly asks to fix.

## Inputs

- `<Service>/04 - Бекенд, API и Контракты/`
- `<Service>/01`, `03`, `02`
- Rules consistency list: `.cursor/rules/steos-docs-folder-04-coordinator.mdc` → Consistency Checks

## Workflow

1. Run [checklist.md](checklist.md) section by section.
2. For each failure → create or update ISSUE using [issue-template.md](issue-template.md) and [steos-docs-staging-issues.mdc](../../rules/steos-docs-staging-issues.mdc).
3. Update `99 - Staging/00 - Реестр проблем.md` table.
4. Return **verify report** (below).

## ISSUE numbering

Next ID = max existing `ISSUE-NNN` + 1. Slug: kebab-case, short (`rest-missing-grpc-link`).

## Verify report template

```markdown
# Verify report — {ServiceName} / 04

## Summary
- Checks run: N | passed: X | failed: Y
- New ISSUEs: …
- Open ISSUEs total: …

## Failed checks
| Check | File | ISSUE |
| :--- | :--- | :--- |

## Passed (sample)
- …

## Recommendation
Ready / Not ready — {reason}
```

## Do not

- Delete `99 - Staging` folder.
- Copy Auth business data to validate naming.
- Duplicate block-template rules from `.mdc` files in ISSUE text — cite file path and SR/field instead.
