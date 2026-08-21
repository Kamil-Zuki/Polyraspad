# 05 - Сообщество, биллинг, агент, уроки и автопилот (Community Billing Agent Lessons)

## Community

См. `Dtos/Community/` — `CreateContributionDto`, `ContributionResponseDto`, `PublishDeckDto`, `PublishedDeckResponseDto`, `ForkDeckDto`, `EntitlementDto`, `ProductResponseDto`, marketplace DTO.

## Billing (records) {#dto-AccessDto}

### AccessDto {#dto-AccessDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| hasAccess | bool | Доступен ли SaaS функционал |
| planCode | string | Код активного тарифного плана |
| status | string | active / trialing / expired |
| currentPeriodEnd | datetime? | Окончание оплаченного периода |

### EntitlementsDto, SubscriptionDto, PlanDto, CheckoutRequestDto, CheckoutResponseDto, InvoiceDto

См. `Billing/BillingDtos.cs`.

## Agent {#dto-AgentThreadDto}

См. `Agent/AgentDtos.cs` — `CreateAgentThreadDto`, `AgentThreadDto`, `AgentMessageDto`, `CreateAgentRunDto`, `AgentRunDto`.

## Lessons & Autopilot {#dto-LessonsDto}

### LessonDto / UserLessonProgressDto / LessonWithProgressDto
- `LessonDto`: `id`, `title`, `description`, `category`, `difficulty`, `contentMarkdown`, `cefrLevel`, `orderIndex`, `estimatedMinutes`.
- `UserLessonProgressDto`: `id`, `userId`, `lessonId`, `status` (0=NotStarted, 1=InProgress, 2=Completed), `agentThreadId`, `startedAt`, `completedAt`, `scorePercent`, `timeSpentSeconds`.

### AutopilotPlanDto / DailyAutopilotTaskDto
- `AutopilotPlanDto`: `planDate`, `suggestedMinutes`, `suggestedNewCards`, `suggestedReviews`, `backlogRiskScore`, `sessionMode`, `nextBestActions`.

## Automation {#dto-CopilotRequestDto}

См. `AutomationDtos.cs` — copilot stub request/response.
