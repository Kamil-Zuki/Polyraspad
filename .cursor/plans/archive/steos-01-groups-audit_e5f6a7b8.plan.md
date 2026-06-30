---
name: steos-01-groups-audit
overview: Аудит файлов «Возможности сервиса» Aggregator и Billing против шаблона STEOS и steos-docs-folders-010305.
todos:
  - id: audit-aggregator
    content: Проверить 16 групп Aggregator Service на структуру SR-блоков
    status: completed
  - id: audit-billing
    content: Проверить 9 групп Billing Service и исправить расхождения
    status: completed
  - id: fix-billing-gaps
    content: Дописать ###3, метафоры, intro lines, 00 обзор
    status: completed
isProject: false
---

# STEOS 01 Groups Audit — complete

## Checklist (steos-docs-folders-010305)

| Элемент | Aggregator (16) | Billing (9) |
| :--- | :--- | :--- |
| `# Группа N:` | OK | OK |
| `## Введение` | OK | OK |
| `**Метафора:**` (domain-fit) | OK all 16 | OK all 9 (fixed 06,08,09) |
| `## Возможности данного раздела` | OK | OK (fixed 05–09) |
| «Ниже представлен перечень…» перед таблицей | OK all | OK all (fixed) |
| Таблица `\| Код \| Название и Описание \|` | OK | OK |
| `---` + `# Детальная спецификация` | OK | OK |
| SR: `### 1` / `### 2` / `### 3` | OK (SR count = ###3) | OK (fixed 06: 3×###3) |
| Intro sentence после `## SR-*` | OK | OK |

## Fixes applied

- `Billing Service/00 - Общая информация.md` — intro перед таблицей групп и «Описание возможностей»
- `06 - Payment Providers` — полная переработка (3 SR с ###3)
- `05, 07, 08, 09` — intro lines, метафоры, intro SR, «Таким образом…»

## Aggregator

Структура соответствует эталону; изменений не требовалось.
