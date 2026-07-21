# Введение

**Vocabulary Service не интегрируется с внешним IdP по HTTP.** Идентичность пользователя приходит от **Aggregator** (и при необходимости Agent) в gRPC metadata (`user_id`).

Файл сохранён как placeholder имени в дереве `04/Интеграции` (исторически копировал Auth layout). Для Vocab используйте:

| Реальная интеграция | Документ / код |
| :--- | :--- |
| inclusive (NLP/FSRS) | `02` КАР-3; gRPC client в `VocabularyService/` |
| JWT / login UX | `authorization-module` + Aggregator REST |

# Статус

| Поле | Значение |
| :--- | :--- |
| **Применимо к Vocabulary** | Нет (out of scope) |
| **Замена** | См. `00 - Интеграции…` и интеграцию с inclusive |
