# SR-VOC-01: Управление Учебным Планом (Curriculum)

## Описание
Пошаговая программа обучения по уровням CEFR: уроки, прогресс, placement и knowledge check. Сущности: `Lesson`, `UserLessonProgress` (`LessonStatus`), `UserCefrProgress`.

---

## Возможности данного раздела

| Код | Название и Описание |
| :--- | :--- |
| **SR-VOC-CUR-01** | **Жизненный цикл урока:** Get/Start/Complete (и restart) с `AgentThreadId`, `ScorePercent`, `TimeSpentSeconds`. |
| **SR-VOC-CUR-02** | **SetPlacementLevel:** установка стартового CEFR и корректировка прогресса младших/старших уровней. |
| **SR-VOC-CUR-03** | **SubmitKnowledgeCheckResult:** фиксация результата knowledge check. |
| **SR-VOC-CUR-04** | **Линейная разблокировка:** `UnlocksAfterLessonId` + агрегация `UserCefrProgress`. |

---

# Детальная спецификация требований

## SR-VOC-CUR-01: Жизненный цикл урока {#SR-VOC-CUR-01}
### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| Статусы | `LessonStatus`: NotStarted / InProgress / Completed. |
| AI thread | `AgentThreadId` связывает урок с AgentService. |

### 2. Высокоуровневое описание
Урок содержит `SystemPrompt`, `ContentMarkdown`, `CefrLevel`, `TargetSkills`, `EstimatedMinutes`. Старт создаёт/обновляет `UserLessonProgress`; complete выставляет score и CompletedAt.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Complete lesson
1. Агент/клиент вызывает CompleteLesson.
2. Status=Completed, обновляется `UserCefrProgress.CompletedLessons`.

---

## SR-VOC-CUR-02: SetPlacementLevel {#SR-VOC-CUR-02}
### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| Jump ahead | Младшие уровни помечаются выполненными. |
| Downgrade | Прогресс старших уровней сбрасывается при понижении placement. |

### 2. Высокоуровневое описание
Диагностический агент вызывает `SetPlacementLevel` с целевым CEFR.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Placement B1
1. Агент определяет B1.
2. A1/A2 уроки completed; открывается B1.

---

## SR-VOC-CUR-03: SubmitKnowledgeCheckResult {#SR-VOC-CUR-03}
### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| Результат проверки | Фиксация outcome knowledge check для curriculum/analytics. |

### 2. Высокоуровневое описание
RPC принимает результат проверки знаний и обновляет связанный прогресс/логи.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Успешный check
1. Клиент/агент отправляет результат.
2. Сервис сохраняет outcome и отражает в прогрессе.

---

## SR-VOC-CUR-04: Линейная разблокировка {#SR-VOC-CUR-04}
### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| UnlocksAfter | Урок недоступен, пока не пройден предыдущий. |
| Cefr aggregate | `UserCefrProgress` с `IsLevelCompleted`, counts. |

### 2. Высокоуровневое описание
UI Curriculum Map блокирует уровни согласно прогрессу и placement.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Locked lesson
1. Пользователь открывает урок с UnlocksAfterLessonId.
2. Сервис/UI отказывает, пока предыдущий не Completed.
