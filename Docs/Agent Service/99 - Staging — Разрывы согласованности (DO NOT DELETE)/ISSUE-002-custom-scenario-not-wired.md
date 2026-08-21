# ISSUE-002: CustomScenario есть в модели, но не exposed

## Тип

Пробел

## В двух словах

В `03` сущность `CustomScenario` и FK `AgentThread.custom_scenario_id` описаны как часть модели. В коде нет gRPC CRUD, CreateThread не принимает scenario id, ExecuteRun не читает сценарий. SR-AGENT-TOOL-06 помечен reserved — живого path нет.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 03 | Entity `CustomScenario`, `AgentThread.custom_scenario_id` | Entity/FK существуют |
| 01 | SR-AGENT-TOOL-06 «custom_roleplay (reserved)» | Нет live tool / API |
| код | `CustomScenario` DbSet; нет RPC; `AvailableTools` без roleplay | Dead wiring |

Путь (вторично): `03/…/Entity - Кастомные Сценарии - Custom Scenarios.md`

## Доказательство

Миграция `AddCustomScenario`; `AgentOrchestrator.AvailableTools` не содержит scenario/roleplay; `CreateThreadAsync` не выставляет `CustomScenarioId`.

## Рекомендуемое действие

Либо реализовать CRUD + CreateThread/ExecuteRun binding, либо оставить TOOL-06 reserved и не обещать feature в product UI до wiring.

## Статус

Open
