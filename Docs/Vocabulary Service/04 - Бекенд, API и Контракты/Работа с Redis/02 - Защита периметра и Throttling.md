# Работа с Redis: Лимиты и Блокировки

Данный документ описывает механизмы ограничений вызовов (Throttling) в `VocabularyService` с использованием Redis.

---

## 1. Rate Limiting вызовов AI-Сервисов
- Для предотвращения превышения лимитов внешнего AI-провайдера (OpenAI / Mistral) при генерации контекста (`GenerateContext`) и объяснения грамматики (`ExplainGrammar`), используется скользящее окно (Sliding Window Rate Limiter) в Redis:
  - **Ключ:** `vocab:rate_limit:ai:{user_id}`
  - **Лимит:** 30 запросов в минуту на пользователя.
