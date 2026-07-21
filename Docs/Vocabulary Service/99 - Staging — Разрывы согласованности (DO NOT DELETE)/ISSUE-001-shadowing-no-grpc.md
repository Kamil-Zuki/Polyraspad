# ISSUE-001: ShadowingAttempt есть в модели, публичного gRPC нет

## Тип

Пробел

## В двух словах

В `03` и `SR-VOC-ACT-03` описана сущность `ShadowingAttempt`, но в VocabularyService нет отдельного gRPC CRUD для создания/чтения попыток shadowing — только EF-модель.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-VOC-ACT-03 «Shadowing Practice» | Описывает запись голоса и self-rating как пользовательский сценарий |
| 03 | Entity `ShadowingAttempt` | Поля есть в EF / snapshot |
| код | gRPC services | Нет dedicated Shadowing RPC |

Путь к файлу (вторично): `01/…/SR-VOC-06_ActivityAssessment.md`, `03/…/Entity - Активность и Оценка Навыков - Activity & Assessment.md`

## Доказательство

`DbSet<ShadowingAttempt>` в `VocabularyServiceContext`; поиск gRPC методов create/list shadowing в сервисе не даёт контракта.

## Рекомендуемое действие

Либо добавить gRPC/API для Shadowing, либо явно пометить SR как «модель зарезервирована / UI пишет иначе» и сузить сценарий в `01`.

## Статус

Open
