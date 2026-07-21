# ISSUE-005: Неполный REST surface относительно vocabulary.proto

## Тип

REST↔gRPC

## В двух словах

В `vocabulary.proto` есть RPC (SyncService, AutonomyService, Suspend/Unsuspend card, GetCardsByDeck, AIService, Lesson SetPlacementLevel / SubmitKnowledgeCheckResult и др.), которые Aggregator REST не экспонирует. Старый ISSUE-002 упоминал DeleteCard — он уже есть в REST.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-AGG-* catalog | Описывает только REST-группы |
| код | `AggregatorService/Controllers` vs `Protos/vocabulary.proto` | Не все RPC имеют REST-фасад |

Путь к файлу (вторично): `01/…/00 - Общая информация.md`, `AggregatorService/Controllers/`

## Доказательство

Controllers: Auth, Projects, Decks, Cards (incl. delete/bulk), Study, Analytics(+skills), Terms, Text, Community, Subscriptions, Media, Billing, Agent(+persist), AiProxy, Automation(+jobs), Integration, Settings, Lessons, Autopilot(+track-skill). Нет REST для SyncData / Autonomy / SuspendCard и части Lesson RPCs.

## Рекомендуемое действие

Либо добавить REST-фасады, либо явно пометить RPC как internal-only в `04` и закрыть ISSUE-002/005.

## Статус

Open
