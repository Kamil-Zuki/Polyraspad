# Ревизия: пайплайн изучения слов (Anki-style FSRS)

**Дата:** 2026-03-18  
**Цель:** Сопоставить документацию (Docs) и реализацию пайплайна обучения по требованиям SR-LRN и оценить близость к «завершению» сценария как в Anki + FSRS.

---

## 1. Требования из документации (SR-LRN)

| ID | Описание | Документ |
|----|----------|----------|
| **SR-LRN-01** | Генерация очереди обучения (приоритеты: Lapses → Reviews → New, лимиты, иерархия колод) | Основные возможности, REST API |
| **SR-LRN-02** | Cloze Deletion (скрытие слова в контексте) | Основные возможности |
| **SR-LRN-03** | FSRS-расчёт интервалов (Stability, Difficulty, target retention) | Основные возможности, REST API |
| **SR-LRN-04** | Sibling Burying (не показывать одну лемму дважды в сессии) | REST API, Entities |
| **SR-LRN-05** | Обнаружение и обработка Leech-фраз (подвешивание после N lapses) | Основные возможности, REST API |
| **SR-LRN-06** | Fuzzy Answer Matching (прощение опечаток при вводе ответа) | Основные возможности |
| **SR-LRN-07** | Списки синонимов (при валидации ответа) | Основные возможности |
| **SR-LRN-08** | Undo (отмена последнего ответа, откат SRS и возврат карты в очередь) | REST API, DTO Description |

Дополнительно в REST API и «Основные возможности» упоминаются:

- **Learn ahead** (Anki-style): если в очереди нет других карт, показывать карточки в LEARNING досрочно (в пределах лимита, например 20 мин).
- **Кэширование очереди в Redis** для низкой задержки (SR-PERF).
- **Re-queue при «Again»**: карточка возвращается в текущую сессию для повторного показа.

---

## 2. Реализация по пунктам

### SR-LRN-01: Очередь обучения

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Приоритеты Lapses → Review → New | Да | `StudyService.StartStudySessionAsync`: сбор deck IDs, запрос к `user_card_progress`, сортировка (Learning/Relearning, Review, New) | ✅ |
| Дневные лимиты (new/review) | Да | Учитываются через `_userSettingsService` (daily goals) при формировании очереди | ✅ |
| Иерархия колод (рекурсия) | Да | `_deckService` / рекурсивный сбор дочерних колод | ✅ |
| Хранение очереди | Redis (Docs) | **Redis List** `study:session:{sessionId}:queue` (StackExchange.Redis), TTL 24 ч; при пустом списке — пересборка очереди из БД (`RebuildSessionQueueAsync`) | ✅ |

**Итог:** Логика очереди и хранение в Redis соответствуют описанию в Docs (SR-PERF / кэш очереди).

---

### SR-LRN-02: Cloze Deletion

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Отдача карточки с targetIndex / скрытие слова | Да | `CardStudyDto` с `content.sentence`, `targetIndex`, `translation`; фронт рендерит предложение и скрывает слово до «Reveal» | ✅ |
| Отображение интервалов на кнопках (Again/Hard/Good/Easy) | Не явно в SR-LRN-02 | `GetNextCardAsync` вызывает `CalculateNextIntervalsAsync` → `nextIntervals` в `CardStudyDto`; фронт передаёт `intervals={currentCard?.nextIntervals}` в контролы | ✅ |

**Итог:** Cloze и превью интервалов на кнопках есть.

---

### SR-LRN-03: FSRS-расчёт

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Алгоритм FSRS (S, D, интервал, retention) | Да | `IFsrsScheduler` → `InclusiveFsrsScheduler` (gRPC к inclusive/py-fsrs) с fallback на `FsrsCalculatorScheduler` | ✅ |
| Настройки проекта (request_retention, maximum_interval, w) | Да | `project.FsrsSettings` передаются в `GetNextStateAsync` | ✅ |
| Обновление user_card_progress (state, stability, difficulty, due) | Да | `SubmitReviewAsync` вызывает `_fsrsScheduler.GetNextStateAsync`, затем обновляет progress и пишет `ReviewLog` | ✅ |
| Fuzzing интервала (документ) | Упомянут в «Основные возможности» | После ответа inclusive: `InclusiveFsrsScheduler` применяет `FsrsCalculator.ApplyFuzzing` для состояний REVIEW (2) и MATURE (3), если интервал ≥ 1 дня | ✅ |

**Итог:** Ядро FSRS реализовано (inclusive + fallback + fuzzing на стороне VocabularyService после gRPC), поведение соответствует описанию в Docs.

---

### SR-LRN-04: Sibling Burying

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Не показывать одну лемму дважды в сессии | Да | **Redis Set** `study:session:{sessionId}:seen_lemmas` (`SetContains` / `SetAdd`); при совпадении карта откладывается (due = завтра в БД), берётся следующая из очереди | ✅ |

**Итог:** Реализовано.

---

### SR-LRN-05: Leech

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Порог lapses (например 5/8/10) | Да | В коде порог **8** lapses → `progress.IsSuspended = true`, `isLeech = true` | ✅ |
| Возврат флага isLeech в ответе review | Да | `ReviewResponseDto.IsLeech`, проброс в gRPC | ✅ |
| Уведомление пользователя о leech в UI | Не формализовано | Фронт показывает уведомление по `reviewResult.isLeech` (например `setLeechNotification`, toast) | ✅ |

**Итог:** Бэкенд и UI полностью закрывают SR-LRN-05.

---

### SR-LRN-06: Fuzzy Answer Matching

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Проверка ответа (точное / fuzzy / синонимы) | Да | `IAnswerValidationService` с порогом 0.85, синонимы из карточки | ✅ |
| Понижение оценки до Again при неверном ответе | Да | В `SubmitReviewAsync`: если передан `userAnswer` и ответ неверный и не fuzzy → `rating = 1` | ✅ |
| Передача userAnswer с фронта | REST API | Фронт передаёт `userAnswer`: поле ввода в `study-card.tsx` (prop userAnswer), состояние в `session/page.tsx`, вызов `apiClient.study.submitReview` с payload cardId, rating, durationMs, **userAnswer** | ✅ |

**Итог:** Полный сценарий «ввод ответа + fuzzy matching» реализован end-to-end: поле ввода на фронте, передача userAnswer в submitReview, проверка и понижение до Again на бэкенде.

---

### SR-LRN-07: Синонимы

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Учёт синонимов при валидации | Да | `AnswerValidationService` проверяет синонимы карточки | ✅ |
| Хранение синонимов на карточке | Entities/DTO | Поле synonyms на карточке / в контенте | ✅ |

**Итог:** Реализовано на бэкенде; активное использование завязано на передачу `userAnswer` (SR-LRN-06).

---

### SR-LRN-08: Undo

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Откат последнего ответа (review_log, user_card_progress) | Да | `UndoReviewAsync`: последняя запись в `ReviewLogs`, откат progress, удаление лога, возврат cardId в очередь | ✅ |
| Возврат карты в очередь сессии | Да | `ListLeftPush` в Redis `study:session:{sessionId}:queue` | ✅ |
| Кнопка Undo на фронте | IA / UX | Есть `handleUndo`, кнопка «Undo (Ctrl+Z)» в `StudyControls`, `canUndo` по `session.cardsReviewed > 0` | ✅ |

**Итог:** Полностью реализовано.

---

### Learn ahead (Anki-style)

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Показ карточек в LEARNING досрочно при пустой очереди | Упомянут в описании сценариев | `StudyService`: лимит 20 мин (`LearnAheadLimitMinutes`), учёт при выборе следующей карты | ✅ |

**Итог:** Реализовано.

---

### Re-queue при «Again»

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Карточка с рейтингом Again возвращается в сессию | REST API | В `SubmitReviewAsync`: при rating == 1 — `ListLeftPush` в Redis `study:session:{sessionId}:queue` | ✅ |

**Итог:** Реализовано.

---

### Redis: очередь, леммы, TTL

| Аспект | Документ | Реализация | Статус |
|--------|----------|------------|--------|
| Очередь сессии | Redis List (SR-LRN-01, SR-PERF) | `study:session:{sessionId}:queue` | ✅ |
| Увиденные леммы (sibling burying) | — | `study:session:{sessionId}:seen_lemmas` (Set) | ✅ |
| TTL | Низкая задержка + ограничение жизни кэша | `SessionDataTtl` = 24 ч на ключи очереди и seen_lemmas | ✅ |
| Потеря ключей / пустая очередь | — | Пересборка из БД; learn ahead; счётчики сессии (`cardsReviewed`, `newLearned`) в **PostgreSQL** (`study_sessions`) | ✅ |

**Итог:** Очередь и набор лемм в Redis соответствуют целевой архитектуре; метаданные активной сессии и статистика ответов сохраняются в БД. При общем Redis для нескольких инстансов VocabularyService очередь сессии доступна с любого пода (при условии sticky-сессии или того же sessionId).

---

## 3. Сводная таблица: близость к «завершению» пайплайна

| Требование | Реализовано | Не реализовано / частично | Примечание |
|------------|-------------|---------------------------|------------|
| SR-LRN-01  | ✅          | —                         | Очередь в Redis List + пересборка |
| SR-LRN-02  | ✅          | —                         | Cloze + интервалы на кнопках |
| SR-LRN-03  | ✅          | —                         | FSRS (inclusive + fallback + fuzzing после ответа inclusive) |
| SR-LRN-04  | ✅          | —                         | Sibling Burying |
| SR-LRN-05  | ✅          | —                         | Leech: бэкенд + UI (уведомление) |
| SR-LRN-06  | ✅          | —                         | Ввод ответа + fuzzy, userAnswer передаётся с фронта |
| SR-LRN-07  | ✅          | —                         | Завязано на userAnswer |
| SR-LRN-08  | ✅          | —                         | Undo полностью |
| Learn ahead| ✅          | —                         | 20 мин |
| Again re-queue | ✅       | —                         | В начало очереди |

---

## 4. Вывод: насколько близко к «завершению» пайплайна как в Anki FSRS

- **Ядро пайплайна (старт сессии → следующая карта → оценка → FSRS → следующая карта) закрыто и соответствует документации.** Очередь, приоритеты, FSRS, Sibling Burying, Leech, Undo, Learn ahead и Re-queue при Again реализованы.
- **Сценарий ввода ответа (SR-LRN-06/07):** закрыт. Фронт передаёт `userAnswer` (поле ввода в study-card, payload в submitReview); бэкенд проверяет ответ (точное / fuzzy / синонимы) и при неверном ответе понижает оценку до Again.
- **Отклонения от Docs:** на уровне пайплайна обучения (SR-LRN + Redis + FSRS) существенных расхождений нет. Дополнительные улучшения (например, единый префикс ключей в документации REST и в коде — см. `study:session:…`) носят косметический характер.

**Оценка:** пайплайн изучения слов в стиле Anki FSRS **соответствует** описанному в Docs по очереди (Redis), FSRS (inclusive + fallback), fuzzing, Sibling Burying, Leech, Undo, Learn ahead и Re-queue при Again.
