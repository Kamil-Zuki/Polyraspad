# Agent Service — документация микросервиса

**AI assistant** платформы Polyraspad: треды диалога в контексте языкового проекта, история сообщений, оркестрированные запуски с intent routing, domain policy и инструментами обучения. Доступ — только по **gRPC** (публичный REST — через Aggregator Service).

## Структура

| Папка | Содержание |
| :--- | :--- |
| [[01 - Функциональная спецификация]] | SR-группы и сервисные требования (`SR-AGENT-*`) |
| [[02 - Архитектура]] | КАР, слои, интеграции |
| [[03 - Модель Данных]] | PostgreSQL-сущности (schema `internal`) |
| [[04 - Бекенд, API и Контракты]] | gRPC, DTO, интеграции, алгоритмы |
| [[99 - Staging — Разрывы согласованности (DO NOT DELETE)]] | Реестр ISSUE при расхождениях между папками |

## Эталон формата

- Полный образец: `(Done) Authorization Service/` (только layout)
- Ближайший sibling: `Aggregator Service/`
- Правила агентов: `Docs/.cursor/rules/`

## Код

Реализация: `AgentService/` (.NET 10, gRPC port `5131`, PostgreSQL schema `internal`).

## Отличия от BFF

- **Есть** собственная PostgreSQL (threads, messages, runs, tool calls, domain decisions, artifacts)
- **Нет** REST API — только gRPC-сервер
- **Нет** Redis, RabbitMQ, WebSocket
- Публичный контракт для frontend — REST на Aggregator → gRPC `AgentService`
