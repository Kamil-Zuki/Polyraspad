# Группа 4: Сессии обучения (Study)

## Введение

В этом разделе описывается REST-прокси Aggregator Service к **VocabularyService.StudyService** — управление **сессиями FSRS-обучения**: старт очереди, выдача следующей карточки, отправка оценки (Again/Hard/Good/Easy), отмена последнего review.

Очередь карточек, интервалы FSRS и состояние сессии хранятся в VocabularyService. Aggregator обеспечивает JSON API для Study UI (`polyraspad-frontend`).

**Метафора:**

Представьте **тренера в зале**, который не считает повторения сам, а только принимает ваш билет (JWT), передаёт команды «следующее упражнение» и «оценка 4/5» в **систему планирования тренировок** (StudyService) и возвращает вам результат на табло.

REST-контракты: [[04 - Бекенд, API и Контракты/Методы API/REST API/04 - Сессии обучения (Study)|REST API — Study]].

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к study session API.

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGG-STUDY-01** | **Запуск сессии обучения:** Создание активной FSRS-сессии с фильтрами project/deck; очередь карточек формирует StudyService. |
| **SR-AGG-STUDY-02** | **Интерактивный цикл review:** Выдача следующей карточки, приём FSRS-оценки и отмена последнего review в рамках session. |

---

# Детальная спецификация требований

## SR-AGG-STUDY-01: Старт study session {#SR-AGG-STUDY-01}

Перед началом Review пользователь создаёт **активную сессию** в контексте project/deck filters. Session id используется во всех последующих вызовах next/review/undo.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Thin BFF** | Построение очереди и FSRS — в StudyService. |
| **JWT обязателен** | `StudyController` — `[Authorize]`. |
| **201 Created** | Успех — `StudySessionDto` + `CreatedAtAction` на GetNextCard. |
| **Маппинг ошибок** | gRPC `NotFound` → 404, `PermissionDenied` → 403, `InvalidArgument` → 400. |
| **Identity** | metadata `user_id`, `roles` на каждый StudyService call. |

### 2. Высокоуровневое описание

Представим старт сессии как **получение браслета в фитнес-клубе на конкретную тренировку**.

1. **Клиент (Study UI):** выбирает project/deck filters и нажимает «Start Review».
2. **Ресепшен (Aggregator):** проверяет JWT, упаковывает `StartSessionRequestDto` → protobuf.
3. **Планировщик (StudyService):** собирает очередь due/new cards, создаёт session record, возвращает id и stats.
4. **Клиент:** сохраняет `sessionId` и запрашивает первую карточку через SR-AGG-STUDY-02.

Таким образом, Aggregator **не знает** состав очереди — только транспортирует параметры старта и возвращает opaque session id.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Маршрут:** POST `/api/study/session`.
* **Body:** `StartSessionRequestDto` (projectId, deck filters по контракту DTO).
* **Downstream:** gRPC `StartStudySession`.

#### Сценарий А: Старт Review (Happy Path)

**Сценарий:** Пользователь начинает сессию по project.

1. **POST** `/api/study/session` + Bearer JWT.
2. **Identity + gRPC:** metadata → `StartStudySession`.
3. **Ответ:** HTTP **201**, `StudySessionDto` (id, queue stats).

#### Сценарий Б: Project не найден (Negative Path)

**Сценарий:** Старт сессии с несуществующим projectId.

1. **POST** `/api/study/session` с invalid projectId.
2. **gRPC:** `NotFound`.
3. **Ответ (BFF):** HTTP **404**, `{ "error": "<detail>" }`.

#### Сценарий В: Нет доступа к project (Negative Path)

1. **gRPC:** `PermissionDenied`.
2. **Ответ (BFF):** HTTP **403**.

---

## SR-AGG-STUDY-02: Next card, submit review, undo {#SR-AGG-STUDY-02}

Ядро Study loop: выдать карточку → принять FSRS rating → опционально отменить последний review.

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **204 = queue empty** | GET next без карточки → **204 No Content** (сессия исчерпана). |
| **Rating в body** | POST review — `ReviewCardRequestDto` (cardId, rating). |
| **Undo** | POST undo восстанавливает последнее FSRS-состояние в рамках session. |
| **Session ownership** | Domain проверяет, что session принадлежит userId из metadata. |
| **Invalid review** | `InvalidArgument` → HTTP **400**. |

### 2. Высокоуровневое описание

Представим цикл как **конвейер flashcard-тренажёра**.

1. **Выдача (GetNextCard):** UI запрашивает «следующую карточку» по session id; если очередь пуста — пустой ответ 204, UI показывает «Session complete».
2. **Оценка (SubmitReview):** пользователь нажимает Again/Hard/Good/Easy; BFF передаёт rating в StudyService, который пересчитывает FSRS и возвращает `nextReviewDate`.
3. **Отмена (UndoReview):** пользователь ошибся кнопкой — один шаг назад в session history; domain восстанавливает card state.
4. **Aggregator на каждом шаге:** JWT → userId в metadata + sessionId в URL/body.

Таким образом, весь **алгоритм FSRS** и **история session** живут в VocabularyService; BFF — тонкий REST-адаптер для Study SPA.

### 3. Примеры взаимодействия (логические сценарии)

**Общие исходные данные:**

* **Base:** `/api/study/session/{sessionId}/…`
* **Downstream:** `GetNextCard`, `SubmitReview`, `UndoReview`.

#### Сценарий А: Полный цикл одной карточки (Happy Path)

1. **GET** `/api/study/session/{id}/next` → HTTP **200**, `CardStudyDto`.
2. **POST** `/api/study/session/{id}/review` с rating Good → **200**, `ReviewResponseDto`.
3. **GET** next снова → следующая карточка или **204**.

#### Сценарий Б: Undo после ошибочного rating (Happy Path)

1. **POST** review с неверным rating.
2. **POST** `/api/study/session/{id}/undo` → **200**, `UndoResponseDto` с `restoredCardId`.

#### Сценарий В: Сессия завершена (Happy Path)

**Сценарий:** Очередь карточек исчерпана.

1. **GET** next когда очередь пуста.
2. **gRPC** возвращает card = null → BFF → HTTP **204 No Content**.
3. **UI:** показывает «Session complete».

#### Сценарий Г: Чужая session (Negative Path)

1. **GET** next с sessionId другого пользователя.
2. **gRPC:** `PermissionDenied` или `NotFound`.
3. **Ответ:** HTTP **403** или **404**.

---

*Следующая группа: [[05 - Аналитика (Analytics)]].*
