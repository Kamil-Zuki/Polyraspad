---
name: steos-01-capabilities-audit
overview: Аудит всех файлов «Возможности сервиса» на соответствие шаблону STEOS (steos-docs-folders-010305) и исправление выявленных gaps.
todos:
  - id: audit-all-services
    content: Сопоставить group files и 00-обзоры с шаблоном; составить матрицу gaps.
    status: completed
  - id: fix-template-gaps
    content: Исправить отсутствующие ### 2/### 3, Media 00 «Файлы групп», неполные SR-блоки.
    status: completed
  - id: verify-and-archive
    content: Повторный grep-проход; archive plan.
    status: completed
isProject: false
---

# STEOS 01 Capabilities Template Audit

## Goal

Проверить все `01/Возможности сервиса/` на соответствие шаблону из `steos-docs-folders-010305.mdc` и `Шаблон документации микросервиса STEOS/`.

## Out of Scope

- `(Done) Authorization Service/` — эталон, не правим
- Папки `02`/`03`/`04`
- Переписывание содержания SR
