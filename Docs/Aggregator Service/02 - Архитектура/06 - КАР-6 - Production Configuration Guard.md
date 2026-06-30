# Введение

При старте в non-Development окружении `ValidateAggregatorConfiguration` проверяет обязательные secrets и URLs; при ошибке — `InvalidOperationException` (fail-fast).

## Контекст и проблема

Deploy с placeholder JWT или `*` CORS — критический security incident.

## Принятое решение

Проверки:

* `Jwt:Secret` — не placeholder, length ≥ 32
* `Jwt:Issuer`, `Jwt:Audience` — configured
* `Cors:AllowedOrigins` — не пусто, без `*`
* Service base URLs — valid absolute HTTP(S)
* `Ai:ProxyApiKey` — не dev default в production

Development — skip validation.

## Обоснование и последствия

### Плюсы

* Раннее обнаружение misconfiguration

### Последствия

* Container не стартует до fix config — intentional

{#КАР-6}
