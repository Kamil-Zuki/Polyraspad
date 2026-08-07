# Интеграция с AgentService

Данный документ описывает gRPC-интеграцию `VocabularyService` с `AgentService` (порт **5131**).

---

## 1. Назначение интеграции

`AgentService` использует gRPC API `VocabularyService` (`vocabulary.proto`) для:
1. Чтения словаря пользователя и извлечения незнакомых терминов при диалоге.
2. Привязки потоков чата агента к CEFR-урокам (`StartLesson` передает `AgentThreadId`).
3. Создания карточек по запросу пользователя в чате ассистента.
