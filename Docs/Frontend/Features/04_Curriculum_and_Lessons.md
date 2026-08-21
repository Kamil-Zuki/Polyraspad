# Фича: Учебный План CEFR и Уроки (Curriculum & Lessons)

**Статус:** Implemented  
**Связанный бекенд:** Vocabulary Service (`LessonService.GetLessons`, `StartLesson`, `CompleteLesson`, `SetPlacementLevel`), Agent Service.

---

## 1. UX-сценарий (User Journey)

* **Шаг 1: Карта уроков (`/lessons`).** Пользователь просматривает дорожную карту уроков, сгруппированных по уровням CEFR (A1, A2, B1, B2, C1, C2).
* **Шаг 2: Диагностика Placement Test.** Пользователь может пройти диагностику (`SetPlacementLevel`), после чего все уроки базовых уровней автоматически становятся `Completed`.
* **Шаг 3: Старт урока.** Клиент нажимает на доступный урок. Вызывается `StartLesson`, инициализирующий `AgentThreadId` для работы AI-репетитора.
* **Шаг 4: Чат-сессия.** Пользователь ведет интерактивный диалог с AI-ассистентом, выполняющим инструкции из `SystemPrompt` урока.
* **Шаг 5: Завершение и оценка.** По окончании упражнения вызывается `CompleteLesson` с передачей набранного балла (`ScorePercent`) и времени. Урок помечается пройденным, и разблокируется следующий урок.

---

## 2. Маршрутизация и Страницы (Routing)

* `src/app/lessons/page.tsx` — дорожная карта уроков CEFR.
* `src/app/lessons/[id]/page.tsx` — экран интерактивного прохождения урока.

---

## 3. Дерево компонентов (Component Architecture)

```
<LessonsPage> (Client)
├── <CefrLevelTabs> — переключатель уровней (A1..C2)
├── <PlacementBanner> — баннер запуска Placement Test
└── <LessonRoadmap> — линейная цепочка карточек уроков
    └── <LessonCard> — карточка урока (статус Locked / InProgress / Completed)

<LessonInteractiveSession> (Client)
├── <LessonHeader> — название урока, CEFR уровень, таймер и прогресс-бар
├── <AgentChatWindow> — окно чата с AI-репетитором
└── <LessonActionBar> — кнопки отправки ответа, подсказки и завершения урока
```

---

## 4. Интеграция с API (Data Fetching & BFF)

* **Чтение (Queries):**
  * `GET /api/v1/lessons` (`LessonService.GetLessons`) — список уроков с текущим прогрессом пользователя.
  * `GET /api/v1/lessons/{id}` (`LessonService.GetLesson`) — детали урока и сопутствующий прогресс.
* **Мутации (Mutations):**
  * `POST /api/v1/lessons/{id}/start` (`LessonService.StartLesson`) — старт урока и генерация треда агента.
  * `POST /api/v1/lessons/{id}/complete` (`LessonService.CompleteLesson`) — завершение урока.
  * `POST /api/v1/lessons/placement` (`LessonService.SetPlacementLevel`) — прохождение Placement Test.

---

## 5. Управление состоянием (State Management)

* **Локальное состояние:**
  * `currentStepIndex`: текущее упражнение в уроке.
  * `agentMessages`: история сообщений в чате урока.
* **Кэш React Query:**
  * `['lessons', userId]` — кэш состояния учебного плана. Инвалидируется при `CompleteLesson` и `SetPlacementLevel`.

---

## 6. Стратегия тестирования фронтенда (UI Testing)

* **Компонентные тесты (`src/components/lessons/lessons.test.tsx`):**
  * Проверка блокировки уроков с неудовлетворенным `UnlocksAfterLessonId`.
  * Проверка вызова `SetPlacementLevel` при выборе уровня в Placement Test.
  * Проверка корректного обновления статуса урока на `Completed`.
