---
description: "[G3 · 04 · Socket] Event block template, gRPC и DTO links"
globs: "**/04 - Бекенд, API и Контракты/**/Методы API/Socket/**"
alwaysApply: false
---

# WebSocket (`Методы API/Socket/`)

Real-time contract on **API Gateway (BFF)**. Handshake and fan-out on Gateway; state changes via microservice gRPC.

## `00 - WebSocket API - Общая информация.md`

1. `# Введение` — WSS paths, SR source in `01`.
2. Optional: service metadata table (version, base paths).
3. `# 1. Поток: API Gateway → gRPC` — numbered flow.
4. `# 2. Группы событий WebSocket` — table with links to group files.
5. `# 3. Сводная таблица событий` — SR | Event | Direction | Description.

## Group file `NN - [Group name].md`

1. `# Введение` — SR link from `01`.
2. `# 1. Список событий` — event table.
3. Each event — block below; separate with `---`.

## Event Block Template

```markdown
# Событие: `[event_name]`

| **Название события** | `[event_name]` |
| :------------------- | :------------- |
| **Тип** | Server → Client \| Client → Server |
| **Описание** | … (SR codes) |
| **DTO / полезная нагрузка** | [DtoName](../DTO/….md#dto-DtoName) |

**Параметры сообщения**

| Название параметра | Тип данных | Описание |
| :--- | :--- | :--- |
| … | … | … |

**Логика обработки**

1. Gateway / microservice calls gRPC [`MethodName`](../gRPC/….md#grpc-MethodName)
2. State persisted (Redis/DB) …
3. Event published via backplane (SR-WS-* if applicable)
4. Client action (e.g. refresh via REST `GET …` → `GetSessionContext`)
```

Link payload fields to DTO anchors; link triggers to `#grpc-*`.
