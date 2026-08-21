# Entity - Карточки и обучение - Cards Study

**Тип:** API Contract View

Downstream: `VocabularyService` CardService, StudyService, AnalyticsService.

## CardResponse (контракт)

Ключевые поля (см. `CardResponseDto`): id, deckId, note fields, templates, media refs, search text, timestamps. FSRS state — на стороне Vocabulary при study.

## StudySession / Review (контракт)

| Контракт | Описание |
| :--- | :--- |
| StartSession | Создание сессии (project/deck filters) |
| GetNextCard | Следующая карточка очереди |
| SubmitReview | Rating 1–4 → обновлённый progress |
| UndoReview | Откат последнего review |

## Analytics (контракт)

| Контракт | Описание |
| :--- | :--- |
| VocabularyStats | Сводка словаря |
| Heatmap | Активность по дням |
| DailySummary | Дневная сводка (graceful default при ошибке) |
| SkillBalance | Баланс навыков (`GET /api/analytics/skills`) |

## Card maintenance (REST, код)

Помимо CRUD: `DELETE /api/Cards/{id}`, bulk-delete, move, bulk-reset-progress, leeches, missing-media.

REST: `/api/Cards`, `/api/study`, `/api/analytics`.
