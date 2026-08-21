# Manifest template

Copy into coordinator report or a scratch file. Update **Status** as work progresses.

```markdown
# Manifest — {ServiceName} / 04

**Source groups:** `01/…/00 - Общая информация.md`
**Etalon:** `(Done) Authorization Service/04 - Бекенд, API и Контракты/`

| # | Path | Group (01) | Status | Notes |
| :--- | :--- | :--- | :--- | :--- |
| 1 | `04 - …/Методы API/gRPC/00 - gRPC - Общая информация.md` | — | missing | |
| 2 | `04 - …/Методы API/gRPC/01 - {Group}.md` | G1 | stub | |
| … | | | | |

## Subfolder totals

| Subfolder | Files | done | partial | missing |
| :--- | :---: | :---: | :---: | :---: |
| gRPC | | | | |
| DTO | | | | |
| REST API | | | | |
| Socket | | | | |
| Integrations | | | | |
| Rabbit MQ | | | | |
| Redis | | | | |
| Algorithms | | | | |
```

**Status definitions**

| Status | Meaning |
| :--- | :--- |
| `missing` | File or folder absent |
| `stub` | Template/short copy only — needs full content |
| `partial` | Some groups/blocks filled; tables or links incomplete |
| `done` | Matches depth of Auth etalon for this service's scope |
