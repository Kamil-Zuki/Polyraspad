# Authorization Module — документация микросервиса

Микросервис **identity и аутентификации** платформы Polyraspad. Владеет учётными записями пользователей (ASP.NET Core Identity + PostgreSQL), выдаёт JWT access/refresh tokens, подтверждает email через SMTP и предоставляет **gRPC API** для Aggregator Service.

## Структура

| Папка | Содержание |
| :--- | :--- |
| [[01 - Функциональная спецификация]] | SR-группы и сервисные требования (`SR-AUTHMOD-*`) |
| [[02 - Архитектура]] | КАР, слои, интеграции |
| [[03 - Модель Данных]] | `ApplicationUser`, `RefreshToken` (PostgreSQL) |
| [[04 - Бекенд, API и Контракты]] | gRPC, DTO, REST legacy, SMTP, алгоритмы |
| [[99 - Staging — Разрывы согласованности (DO NOT DELETE)]] | Реестр ISSUE при расхождениях между папками |

## Эталон формата

- Полный образец layout: `(Done) Authorization Service/` — **только формат**, не домен
- Актуальный Polyraspad-пример: `Aggregator Service/`
- Правила: `Docs/.cursor/rules/`

## Код

Реализация: `authorization-module/` (submodule, .NET 10, gRPC порт `5027`, БД `auth-module`).

## Отличия от STEOS Authorization Service

| Polyraspad authorization-module | STEOS `(Done) Authorization Service` |
| :--- | :--- |
| JWT access + refresh в PostgreSQL | Phantom Token + Redis sessions |
| Локальный Identity (email/password) | OIDC / STEOS ID |
| gRPC primary, REST legacy | REST + WebSocket + RabbitMQ |
| Нет guest sessions, audit WORM, workspace routing | Полный enterprise gatekeeper |
