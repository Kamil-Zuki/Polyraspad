# 05 - Сообщество, биллинг и агент (Community Billing Agent)

## Community

См. `Dtos/Community/` — `CreateContributionDto`, `ContributionResponseDto`, `PublishDeckDto`, `PublishedDeckResponseDto`, `ForkDeckDto`, `EntitlementDto`, `ProductResponseDto`, marketplace DTO.

## Billing (records) {#dto-AccessDto}

### AccessDto {#dto-AccessDto}

| Поле | Тип | Описание |
| :--- | :--- | :--- |
| hasAccess | bool | |
| planCode | string | |
| status | string | active / trialing / … |
| currentPeriodEnd | datetime? | |

### EntitlementsDto, SubscriptionDto, PlanDto, CheckoutRequestDto, CheckoutResponseDto, InvoiceDto

См. `Billing/BillingDtos.cs`.

## Agent {#dto-AgentThreadDto}

См. `Agent/AgentDtos.cs` — `CreateAgentThreadDto`, `AgentThreadDto`, `AgentMessageDto`, `CreateAgentRunDto`, `AgentRunDto`.

## Automation {#dto-CopilotRequestDto}

См. `AutomationDtos.cs` — copilot stub request/response.
