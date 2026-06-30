---
name: billingservice-docs
overview: STEOS документация микросервиса Billing Service — папки 03→01→02 по правилам Docs/.cursor и коду BillingService submodule.
todos:
  - id: folder-03
    content: "03 - Модель Данных — entities, список, staging"
    status: completed
  - id: folder-01
    content: "01 - Функциональная спецификация — 00, термины, NFR, 9 групп SR-BILL-*"
    status: completed
  - id: folder-02
    content: "02 - Архитектура — 00 + КАР-1..5"
    status: completed
  - id: verify-close
    content: README, сверка 01↔03, archive plan
    status: completed
isProject: false
---

# Billing Service — STEOS docs

## Goal

Создать `Docs/Billing Service/` по эталону формата `(Done) Authorization Service/` и правилам `Docs/.cursor/rules/steos-docs-*`. Содержание — только из `BillingService/` (gRPC, EF, providers).

## Out of Scope

- Полный `04` (gRPC endpoint blocks batch) — только README stub
- Изменения кода BillingService
- Копирование текста Authorization Service

## Agents

Документация STEOS — lead выполняет по skills/rules; code subagents не требуются.
