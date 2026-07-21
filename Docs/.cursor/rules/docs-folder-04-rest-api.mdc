---
description: "[G3 · 04 · REST] Endpoint block template, ссылки на gRPC"
globs: "**/04 - Бекенд, API и Контракты/**/Методы API/REST API/**"
alwaysApply: false
---

# REST API (`Методы API/REST API/`)

External HTTP contract on **API Gateway (BFF)**. Domain logic lives in microservice gRPC — not in REST controllers.

## `00 - REST API - Общая информация.md`

1. `# Введение` — Gateway owns routes; Auth/microservice owns gRPC.
2. `# 1. Группы методов REST API` — summary table.
3. `# 2 … N` — per-group: SR | Method | Endpoint | Description.

## Group file `NN - [Group name].md`

1. `# Введение` — scope for authenticated/public SPA flows.
2. `# 1. Список эндпоинтов` — endpoint table.
3. Each endpoint — block below; separate with `---`.

## Endpoint Block Template

```markdown
# [SR-CODE]: [Краткое имя]: [route]

## Общая информация

(1–2 sentences for frontend/API consumers)

| Тип метода | GET \| POST \| … |
| :--- | :--- |
| **DTO запроса** | [DtoName](../DTO/….md#dto-DtoName) \| N/A |
| **DTO успешного ответа** | [DtoName](../DTO/….md#dto-DtoName) |

## Параметры URL

| Название | Тип | Описание |
| :--- | :--- | :--- |
| … | … | … |

(Or: «Параметры отсутствуют.»)

## Логика обработки запроса

* BFF extracts Cookie/headers …
* BFF calls gRPC [`MethodName`](../gRPC/….md#grpc-MethodName) — **required link**
* Maps RPC response to JSON DTO

## Успешный ответ

\`\`\`json
{ … }
\`\`\`

## Ошибки

| Статус-код | Описание ошибки |
| :--- | :--- |
| **401 Unauthorized** | … |
| **403 Forbidden** | … |
| **404 Not Found** | … |
```

Never document business logic on BFF without naming the delegated gRPC method.
