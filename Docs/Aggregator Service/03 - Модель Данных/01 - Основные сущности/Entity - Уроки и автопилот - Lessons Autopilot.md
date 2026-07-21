# Entity - Уроки и автопилот - Lessons Autopilot

**Тип:** API Contract View (protobuf passthrough / DTO)

Downstream: Vocabulary Lesson + Autopilot RPCs. Feature flags на BFF: `EnableAdvancedModules` (Lessons), `EnableAIAgents` (Autopilot).

## Lesson / UserLessonProgress (контракт)

| Поле (логическое) | Описание |
| :--- | :--- |
| lessonId | Id урока |
| title, description, cefrLevel, orderIndex | Метаданные |
| systemPrompt, contentMarkdown, targetSkills, estimatedMinutes | Контент/промпт |
| status | NotStarted / InProgress / Completed |
| scorePercent, timeSpentSeconds, agentThreadId | Прогресс |
| completedAt | Завершение |

REST: `/api/projects/{projectId}/lessons` — list, get, start, restart, complete.

## Daily Autopilot Plan (контракт)

| Поле (логическое) | Описание |
| :--- | :--- |
| projectId | Контекст проекта |
| plan / actions | План дня от Vocabulary Autopilot |
| track-skill | POST track skill activity (`…/autopilot/track-skill`) |

REST: `/api/v1/projects/{projectId}/autopilot/daily-plan`, `…/track-skill`.

## Automation Job (in-memory, не EF)

| Поле | Описание |
| :--- | :--- |
| jobId | Id задачи |
| status / result | Состояние in-memory orchestrator |

REST: `/api/automation/jobs` (POST/GET). Copilot feedback остаётся stub (см. ISSUE-001).
