---
name: steos-01-groups-audit
overview: Аудит папок «Возможности сервиса» всех Polyraspad-микросервисов на соответствие шаблону STEOS (steos-docs-folders-010305).
todos:
  - id: audit-matrix
    content: Сверка Aggregator, Authorization Module, Agent, Billing, Media с чеклистом шаблона.
    status: completed
  - id: fix-authmod-groups
    content: Довести Authorization Module группы 01–04 до полного SR-блока.
    status: completed
  - id: staging-issues-other
    content: ISSUE в 99 для Agent/Billing/Media — intro line и прочие пробелы.
    status: completed
isProject: false
---

# STEOS 01 Groups Template Audit

## Checklist (G1 / шаблон)

Каждый файл группы (не `00`):

1. `# Группа N: …`
2. `## Введение` + опционально `**Метафора:**`
3. `## Возможности данного раздела` + «Ниже представлен перечень…»
4. Таблица `| Код | Название и Описание |` — формат `**Title:** desc`
5. `# Детальная спецификация требований`
6. На каждый SR: §1, §2 (+ «Таким образом…»), §3 (сценарии), `{#SR-CODE}`
