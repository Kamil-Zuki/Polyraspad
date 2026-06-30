# ISSUE-001: Copilot review-feedback — stub на BFF

**Тип:** Пробел  
**SR-ID:** SR-AGG-AUTO-01  
**Статус:** Open

## Описание

`OpenAiCompatibleStudyCopilotFeedbackService` зарегистрирован в DI, но `AutomationController` POST `/api/automation/copilot/review-feedback` возвращает пустой neutral stub без вызова LLM.

## Ожидаемое поведение (01)

SR-AGG-AUTO-01 описывает copilot feedback после review — ожидается интеграция с LLM или явная пометка «не реализовано» в 01.

## Факт (код)

Controller возвращает hardcoded stub.

## Рекомендация

Либо подключить `IStudyCopilotFeedbackService`, либо понизить SR до «planned» и обновить 01.
