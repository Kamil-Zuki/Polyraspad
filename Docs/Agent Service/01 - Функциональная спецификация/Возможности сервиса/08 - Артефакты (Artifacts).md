# Группа 8: Артефакты (Artifacts)

## Введение

**Artifacts** — опциональное хранение structured JSON, связанного с run (drafts, exports). Основной UX использует message `metadata_json`; artifacts — для явного persist/reload.

**Метафора:** артефакты — **полка сохранённых черновиков**. Пользователь может вернуться к структурированному результату run (карточка, экспорт) без повторного запроса к LLM.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Артефакты (Artifacts).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-ART-01** | **Create artifact:** kind + payload_json; run must exist in thread. |
| **SR-AGENT-ART-02** | **List artifacts:** By thread; optional run_id filter. |

---

# Детальная спецификация требований

## SR-AGENT-ART-01: Create artifact {#SR-AGENT-ART-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Thread ownership** | User must own thread. |
| **Run integrity** | run_id exists and belongs to thread. |
| **404** | Thread or run mismatch → NOT_FOUND gRPC. |

### 2. Высокоуровневое описание

Представим create artifact как **положить черновик на полку с меткой run**.

1. **gRPC CreateArtifact:** client передаёт `kind` + `payload_json` для structured result (card draft, export).
2. **Ownership gate:** user must own thread; `run_id` must exist and belong to that thread.
3. **404 on mismatch:** thread или run integrity violation → gRPC NOT_FOUND без partial persist.
4. **UX complement:** основной flow — message `metadata_json`; artifact — explicit persist/reload для tools panel.

Таким образом, editor draft или export можно вернуть позже без повторного ExecuteRun или LLM call.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Save card draft artifact (Happy Path)

1. После ExecuteRun UI сохраняет editor draft как artifact kind `card_draft`.
2. gRPC `CreateArtifact` с `run_id`, `kind`, `payload_json`.
3. Response: `AgentArtifactItem` с id и timestamps.

---

## SR-AGENT-ART-02: List artifacts {#SR-AGENT-ART-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Order** | created_at DESC. |
| **Empty** | Unknown thread → empty list (not error). |

### 2. Высокоуровневое описание

Представим list artifacts как **каталог сохранённых черновиков треда**.

1. **Thread scope:** gRPC `ListArtifacts(thread_id)` с optional `run_id` filter для subset одного эпизода.
2. **Order:** `created_at DESC` — последние payloads первыми для tools panel UI.
3. **Empty soft:** unknown thread → empty list, not error — без утечки существования чужих тредов.
4. **Reload path:** UI rehydrates Editor из `payload_json` без нового LLM или ExecuteRun.

Таким образом, archived и длинные threads сохраняют доступ к structured drafts через gRPC read path.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Reload drafts for thread (Happy Path)

1. User reopens archived conversation tools panel.
2. gRPC `ListArtifacts(thread_id)`.
3. UI отображает последние payloads для re-open в Editor.

---

*Следующая группа: [[09 - Интеграция с Vocabulary (Vocabulary Integration)]].*
