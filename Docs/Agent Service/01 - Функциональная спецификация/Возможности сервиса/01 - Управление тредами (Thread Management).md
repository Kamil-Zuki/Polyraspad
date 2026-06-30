# Группа 1: Управление тредами (Thread Management)

## Введение

AI-ассистент PolyGuide работает в **контексте языкового проекта**. Каждый **thread** — отдельная беседа пользователя в рамках `project_id`. Сервис хранит треды в PostgreSQL и проверяет доступ к проекту через VocabularyService перед list/create.

**Метафора:** тред — **закладка разговора в папке проекта**. Пользователь может открыть несколько закладок (threads), вернуться к старой или убрать её в архив, не удаляя историю.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Управление тредами (Thread Management).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-THREAD-01** | **Список тредов проекта:** Активные треды user+project по `updated_at` DESC; archived скрыты. |
| **SR-AGENT-THREAD-02** | **Создание треда:** Пустой тред после EnsureProjectAccess. |
| **SR-AGENT-THREAD-03** | **Получение треда:** Get by id + user ownership. |
| **SR-AGENT-THREAD-04** | **Архивация треда:** Soft archive; блок новых runs на archived thread. |

---

# Детальная спецификация требований

## SR-AGENT-THREAD-01: Список тредов проекта {#SR-AGENT-THREAD-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Project-scoped** | Фильтр `user_id` + `project_id`; archived (`archived_at IS NOT NULL`) исключены. |
| **Access gate** | `EnsureProjectAccessAsync` до query. |
| **Sort** | `updated_at DESC` — недавние диалоги первыми. |
| **Default title** | Если `title` null — UI title из `AgentThreadTitleHelper.DefaultTitle`. |

### 2. Высокоуровневое описание

Представим список тредов как **оглавление папки с закладками в PolyGuide**.

1. **Запрос sidebar:** UI передаёт `project_id`; identity (`user_id`, roles) приходит из gRPC metadata Aggregator.
2. **Access gate:** до SQL-запроса вызывается `EnsureProjectAccessAsync` через Vocabulary ContentService.
3. **Фильтр активных:** из `agent_threads` выбираются только строки user+project без `archived_at`.
4. **Сортировка:** `updated_at DESC` — недавно обновлённые диалоги первыми; пустой title заменяется default helper-ом для UI.

Таким образом, sidebar PolyGuide показывает только доступные и неархивные беседы текущего пользователя в выбранном проекте.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Sidebar threads (Happy Path)

1. Aggregator → gRPC `ListThreads` с metadata `user_id`, `roles`.
2. AgentService валидирует project access.
3. Response: массив активных тредов.

#### Сценарий Б: Project недоступен (Negative Path)

1. ContentService → NotFound / PermissionDenied.
2. gRPC → `NOT_FOUND` «Project … not found or access denied».

---

## SR-AGENT-THREAD-02: Создание треда {#SR-AGENT-THREAD-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Empty thread** | Без начальных messages; title null до первого run. |
| **UUID v4** | Client-generated id на стороне БД default. |
| **Timestamps** | `created_at` = `updated_at` = UTC now. |

### 2. Высокоуровневое описание

Представим создание треда как **новую пустую закладку в папке проекта**.

1. **Инициатор:** пользователь нажимает «New chat» в PolyGuide после выбора project context.
2. **Проверка доступа:** `CreateThread` не создаёт row, пока project access не подтверждён через Vocabulary.
3. **Пустой контейнер:** в `agent_threads` появляется запись без messages; `title` остаётся null до первого run.
4. **Ответ клиенту:** gRPC возвращает новый thread id — UI переходит к пустому диалогу.

Таким образом, каждая новая беседа начинается с изолированного thread container, привязанного к project и user.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: New chat (Happy Path)

1. User нажимает «New chat» в PolyGuide.
2. gRPC `CreateThread` → новый thread id.
3. UI переходит к пустому треду.

---

## SR-AGENT-THREAD-03: Получение треда {#SR-AGENT-THREAD-03}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Ownership** | `thread.user_id == metadata user_id`. |
| **Archived visible** | GetThread возвращает archived тред (с `archived_at`) — для read-only просмотра. |

### 2. Высокоуровневое описание

Представим получение треда как **открытие конкретной закладки по id**.

1. **Lookup:** `GetThread` ищет row по thread id и проверяет `user_id` из metadata.
2. **Archived допустим:** archived тред возвращается с `archived_at` — для read-only просмотра истории.
3. **Чужой или missing:** NOT_FOUND без утечки, существует ли thread у другого пользователя.
4. **UI режим:** при `archived_at` set новые runs блокируются на уровне CreateRun/ExecuteRun.

Таким образом, клиент может безопасно открыть как активный, так и архивный диалог, не получая доступ к чужим тредам.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Open archived thread (Happy Path)

1. User открывает archived тред из history link.
2. gRPC `GetThread` → 200 с `archived_at` set.
3. UI read-only; новый run блокируется на archived.

---

## SR-AGENT-THREAD-04: Архивация треда {#SR-AGENT-THREAD-04}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Soft delete** | `archived_at = UTC now`; данные сохраняются. |
| **Idempotent** | Повторный archive → success без изменений. |
| **Run block** | CreateRun на archived → `FailedPrecondition`. |

### 2. Высокоуровневое описание

Представим архивацию треда как **убрать закладку из оглавления, не вырывая страницы**.

1. **Инициатор:** пользователь удаляет чат из sidebar PolyGuide — вызывается gRPC `ArchiveThread`.
2. **Soft delete:** в `agent_threads` проставляется `archived_at = UTC now`; messages, runs и domain decisions сохраняются.
3. **Sidebar filter:** `ListThreads` больше не возвращает archived row; `GetThread` по-прежнему отдаёт тред для read-only просмотра.
4. **Run guard:** повторный `CreateRun`/`ExecuteRun` на archived thread → gRPC `FailedPrecondition`; повторный archive идемпотентен.

Таким образом, пользователь очищает активный список диалогов, не теряя историю и не нарушая audit trail runs.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Archive from UI (Happy Path)

1. User удаляет чат из списка.
2. gRPC `ArchiveThread`.
3. Thread исчезает из sidebar list.

---

*Следующая группа: [[02 - История сообщений (Message History)]].*
