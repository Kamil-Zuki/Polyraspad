---
description: "[G3 · 04 · Integrations] HTTP/gRPC outward, file template"
globs: "**/04 - Бекенд, API и Контракты/**/Интеграции со сторонними сервисами/**"
alwaysApply: false
---

# Интеграции (`Интеграции со сторонними сервисами/`)

Outbound HTTP/gRPC to external systems and internal platform services. **RabbitMQ consume/publish** — document in `docs-folder-04-rabbitmq.mdc`, not here.

## `00 - … - Общая информация.md`

1. `# Введение` — purpose, SR codes covered.
2. `# 1. Список интеграций` — Integration | SR | Protocol | Purpose.

## Integration file `NN - [Service name] ([Protocol]).md`

Structure for each external/internal integration:

```markdown
# Введение

(Scope: what this integration enables)

# Общая информация

| Параметр | Описание |
| :--- | :--- |
| **Версия API** | … |
| **Название сервиса** | … |
| **Владелец** | … |

# Доступ и аутентификация

| Параметр | Описание |
| :--- | :--- |
| **Метод аутентификации** | mTLS / Bearer / Basic / … |
| **Хранение учётных данных** | Vault / K8s Secrets — never frontend |
| **Среды** | Prod / Stage / Dev |

# Ключевые методы HTTP/REST (или gRPC)

| Метод | Описание | SR | Использование в сервисе |
| :--- | :--- | :--- | :--- |

# Логика обработки запросов

* Retry, timeout, circuit breaker, caching policies

# Обработка ошибок

| Тип ошибки | Причина | Реакция сервиса |
| :--- | :--- | :--- |
```

Each row must reference SR from `01`. Link to related gRPC/Rabbit if side effects trigger async flows.
