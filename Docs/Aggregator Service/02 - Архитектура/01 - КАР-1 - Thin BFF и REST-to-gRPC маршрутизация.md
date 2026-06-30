# Введение

Aggregator Service реализует паттерн **Thin BFF**: REST-контроллеры не содержат доменной логики Polyraspad — только валидацию входа, маппинг DTO ↔ protobuf и трансляцию gRPC status → HTTP status.

## Контекст и проблема

Клиенту нужен единый HTTPS endpoint с JSON и JWT. Внутренние сервисы общаются по gRPC и не должны быть доступны из браузера напрямую.

## Принятое решение

1. Все публичные маршруты — в Aggregator (`/api/*`, `/healthz`).
2. Каждый controller action вызывает один или несколько gRPC методов downstream-сервиса.
3. Facade-интерфейсы (`IVocabularyServiceClient`, `IAuthorizationServiceClient`, …) инкапсулируют клиентов.
4. AutoMapper и hand-mappers преобразуют типы на границе.

## Обоснование и последствия

### Плюсы

* Чёткая граница: изменения домена — в VocabularyService, не в BFF
* Единая точка CORS, rate limit, JWT validation
* Swagger для frontend-разработчиков

### Последствия

* Дублирование DTO и proto message shapes — синхронизация через контрактные тесты
* *Решение:* AggregatorService.Tests + integration tests на WebApplicationFactory

{#КАР-1}
