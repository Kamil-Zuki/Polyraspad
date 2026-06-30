---
name: steos-01-s2-full-volume
overview: Довести §2 «Высокоуровневое описание» всех SR из «Основных возможностей» до эталона Aggregator (метафора + bold steps + «Таким образом,»).
todos:
  - id: audit-coverage
    content: Сверить 00 ↔ group files (SR parity, group count)
    status: completed
  - id: fix-agent-s2
    content: Agent Service — 25 SR §2 до этalona
    status: pending
  - id: fix-media-s2
    content: Media Service — 17 SR §2 до этalona
    status: pending
  - id: fix-billing-s2
    content: Billing Service — 14 SR §2 (метафора + bold steps)
    status: pending
  - id: fix-authmod-s2
    content: Authorization Module — 15 SR §2 (метафора + bold steps)
    status: pending
  - id: fix-agg-s2
    content: Aggregator SR-AGG-MEDIA-02 — bold steps в §2
    status: pending
  - id: verify-rerun
    content: Повторный аудит §2 — 111/111 compliant
    status: pending
isProject: false
---

# Plan: Полный объём «Основных возможностей»

## Результат аудита (2026-06-27)

### Покрытие (00 → group files) — OK

| Сервис | Групп | SR в 00 | SR в groups | Пропуски |
|--------|-------|---------|-------------|----------|
| Aggregator | 16/16 | 40 | 40 | 0 |
| Authorization Module | 4/4 | 15 | 15 | 0 |
| Billing | 9/9 | 14 | 14 | 0 |
| Agent | 11/11 | 25 | 25 | 0 |
| Media | 4/4 | 17 | 17 | 0 |

Таблицы SR в `00` и group-файлах — **0 расхождений**.

### Глубина §2 (эталон Aggregator) — GAPS

72/111 SR не соответствуют полному эталону §2.

Эталон: метафора → `N. **Title:**` steps → «Таким образом, …».

## Out of Scope

- Vocabulary Service (нет папки 01)
- `(Done) Authorization Service`
- Правки `03` / `04`
