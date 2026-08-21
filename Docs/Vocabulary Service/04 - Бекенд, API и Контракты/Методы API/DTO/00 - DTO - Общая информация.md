# Введение

Настоящий документ содержит описание Объектов Передачи Данных (DTO) и структур полезной нагрузки (Payload) для микросервиса **Vocabulary Service**.

В микросервисе DTO используются как в качестве Protobuf-сообщений в **gRPC** контракте `vocabulary.proto`, так и в C# DTO моделях слоя **AggregatorService** при маппинге внешнего REST API.

---

# 1. Группы DTO

| Группа DTO | Описание |
| :--- | :--- |
| **Основные (общие) DTO** | Модели проектов, колод, обертки ответов, пагинация, превью карточек |
| **DTO Заметок, Полей и FSRS** | `NotePayload`, `NoteFieldDefinitionPayload`, `CardTemplatePayload`, `SrsSettings`, `UserCardProgressDto` |
| **DTO Синхронизации и Маркетплейса** | `SyncChanges`, `SyncDataResponse`, `BatchReviewItem`, `ProductDto`, `SubscriptionItemResponse` |
