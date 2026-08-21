# gRPC Методы: Study, Lesson, Sync, AI, Text & Marketplace Services

Данный документ содержит спецификацию методов gRPC для обучения, синхронизации, AI-инструментов, токенизации и торговой площадки.

---

## 1. StudyService (Интервальные Повторения FSRS)

### StartStudySession
- **Сигнатура:** `rpc StartStudySession (StartStudySessionRequest) returns (StartStudySessionResponse)`
- **Требование:** SR-LRN-01 / SR-VOC-02
- **Описание:** Инициализирует новую сессию обучения для заданной колоды и формирует очередь карточек (New, Learning, Review).

### GetNextCard / SubmitReview / UndoReview
- **Сигнатуры:**
  - `rpc GetNextCard (GetNextCardRequest) returns (GetNextCardResponse)`
  - `rpc SubmitReview (SubmitReviewRequest) returns (SubmitReviewResponse)`
  - `rpc UndoReview (UndoReviewRequest) returns (UndoReviewResponse)`
- **Требование:** SR-LRN-02 / SR-LRN-03 / SR-LRN-08
- **Описание:** Выдача очередной карточки, отправка оценки FSRS (Again=1, Hard=2, Good=3, Easy=4) с пересчетом параметров удержания и отмена последнего ответа.

---

## 2. LessonService (Учебный План CEFR)

### GetLessons / GetLesson / StartLesson / CompleteLesson
- **Сигнатуры:**
  - `rpc GetLessons (GetLessonsRequest) returns (GetLessonsResponse)`
  - `rpc GetLesson (GetLessonRequest) returns (GetLessonResponse)`
  - `rpc StartLesson (StartLessonRequest) returns (StartLessonResponse)`
  - `rpc CompleteLesson (CompleteLessonRequest) returns (google.protobuf.Empty)`
- **Требование:** SR-VOC-CUR-01 / SR-VOC-01
- **Описание:** Управление прохождением уроков глобальной программы с отслеживанием привязанного потока агента (`AgentThreadId`), процента выполнения и времени сессии.

### SetPlacementLevel / SubmitKnowledgeCheckResult
- **Сигнатуры:**
  - `rpc SetPlacementLevel (SetPlacementLevelRequest) returns (google.protobuf.Empty)`
  - `rpc SubmitKnowledgeCheckResult (SubmitKnowledgeCheckResultRequest) returns (google.protobuf.Empty)`
- **Требование:** SR-VOC-CUR-02 / SR-VOC-CUR-03
- **Описание:** Корректировка стартового уровня пользователя (Placement Test) и сохранения результатов периодических проверок знаний.

---

## 3. SyncService & AIService & TextService

### SyncData / BatchSubmitReviews
- **Сигнатуры:**
  - `rpc SyncData (SyncDataRequest) returns (SyncDataResponse)`
  - `rpc BatchSubmitReviews (BatchSubmitReviewsRequest) returns (BatchSubmitReviewsResponse)`
- **Требование:** SR-SNC-01 / SR-SNC-03
- **Описание:** Получение дельта-изменений по токену синхронизации для оффлайн-клиентов и пакетная отправка сохраненных ответов повторений.

### GenerateContext / ExplainGrammar
- **Сигнатуры:**
  - `rpc GenerateContext (GenerateContextRequest) returns (GenerateContextResponse)`
  - `rpc ExplainGrammar (ExplainGrammarRequest) returns (ExplainGrammarResponse)`
- **Требование:** SR-AI-01 / SR-AI-02
- **Описание:** AI-генерация примера использования слова с учетом уровня CEFR и разъяснение грамматических конструкций фразы.

### AnalyzeText
- **Сигнатура:** `rpc AnalyzeText (AnalyzeTextRequest) returns (AnalyzeTextResponse)`
- **Требование:** SR-TXT-01 / SR-VOC-05
- **Описание:** Токенизация текста с сопоставлением сохраненных фраз и определением статусов изученности каждого слова для визуализации в ридере.

---

## 4. AutonomyService & Marketplace (Community & Subscriptions)

### GetDailyAutopilot / GetNextBestActions
- **Сигнатуры:**
  - `rpc GetDailyAutopilot (GetDailyAutopilotRequest) returns (GetDailyAutopilotResponse)`
  - `rpc GetNextBestActions (GetNextBestActionsRequest) returns (GetNextBestActionsResponse)`
- **Требование:** SR-VOC-06
- **Описание:** Расчет рекомендаций автопилота на день и формирование списка приоритетных действий (Next Best Actions).

### ListSubscriptions / Subscribe / Unsubscribe / CommunityService
- **Сигнатуры:**
  - `rpc ListSubscriptions (ListSubscriptionsRequest) returns (ListSubscriptionsResponse)`
  - `rpc Subscribe (SubscribeRequest) returns (SubscriptionItemResponse)`
  - `rpc Unsubscribe (UnsubscribeRequest) returns (google.protobuf.Empty)`
- **Требование:** SR-VOC-07
- **Описание:** Управление подписками на колоды из каталога Marketplace и работа с отзывами.
