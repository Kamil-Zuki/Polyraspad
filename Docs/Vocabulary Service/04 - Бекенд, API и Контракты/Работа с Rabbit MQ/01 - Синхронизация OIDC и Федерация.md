# Работа с RabbitMQ: Доменные События

Данный документ содержит спецификацию доменных событий `VocabularyService`.

---

## 1. Событие `vocab.term.status_changed`
- **Exchange:** `polyraspad.vocabulary.events` (Topic)
- **Routing Key:** `vocab.term.status_changed`
- **Payload:**
  ```json
  {
    "userId": "uuid",
    "projectId": "uuid",
    "termText": "slept",
    "oldStatus": "SAVED",
    "newStatus": "KNOWN",
    "timestamp": "2026-08-07T18:50:00Z"
  }
  ```

---

## 2. Событие `vocab.deck.published`
- **Exchange:** `polyraspad.vocabulary.events`
- **Routing Key:** `vocab.deck.published`
- **Payload:**
  ```json
  {
    "productId": "uuid",
    "deckId": "uuid",
    "authorId": "uuid",
    "title": "Advanced Spanish Verbs",
    "version": 1
  }
  ```
