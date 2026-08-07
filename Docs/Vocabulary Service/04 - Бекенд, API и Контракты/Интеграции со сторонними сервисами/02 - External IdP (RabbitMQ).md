# Интеграции: MediaService и BillingService

Данный документ описывает gRPC-интеграцию `VocabularyService` с `MediaService` и `BillingService`.

---

## 1. Интеграция с MediaService (gRPC порт 5121)

`VocabularyService` использует `MediaService` (`media.proto`) для:
1. Загрузки изображений и аудиофайлов карточек (`UploadImage`, `UploadDocument`).
2. Получения публичных presigned-ссылок (`GetImageUrl`, `GetAudioUrl`) для отдачи клиентам.

---

## 2. Интеграция с BillingService (gRPC порт 5127)

`VocabularyService` проверяет права пользователя при подписке и покупке платных колод через `BillingService` (`billing.proto`):
- Вызов RPC `CheckEntitlement` для сверки наличия активной подписки или приобретенной лицензии на колоду (`UserEntitlement`).
