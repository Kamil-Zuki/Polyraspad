# Введение

**Vocabulary Service не потребляет события внешнего IdP из RabbitMQ.** Сессии и back-channel logout — зона `authorization-module` / Aggregator, не Vocabulary.

Файл сохранён как placeholder имени в дереве `04/Интеграции` (исторически копировал Auth layout).

# Статус

| Поле | Значение |
| :--- | :--- |
| **Применимо к Vocabulary** | Нет (out of scope) |
| **Актуальные асинхронные темы Vocab** | Redis study/cache (см. `Работа с Redis/`); события marketplace — по мере появления в коде |
