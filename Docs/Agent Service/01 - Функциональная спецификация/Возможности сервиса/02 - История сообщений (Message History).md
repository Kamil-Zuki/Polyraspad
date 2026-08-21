# Группа 2: История сообщений (Message History)

## Введение

Каждый thread хранит хронологию сообщений ролей `user`, `assistant`, `system`, `tool`. UI загружает историю **cursor-based** пагинацией для длинных диалогов.

**Метафора:** история сообщений — **киноплёнка диалога**. UI сначала видит последний кадр (новые реплики), а курсор `before` отматывает плёнку к более ранним фрагментам без перезагрузки всего фильма.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к История сообщений (Message History).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-MSG-01** | **List messages с cursor:** Newest-first fetch; `before` id для предыдущей страницы; reverse to chronological в response. |

---

# Детальная спецификация требований

## SR-AGENT-MSG-01: List messages с cursor {#SR-AGENT-MSG-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Limit clamp** | 1..100; default 100 если `limit <= 0`. |
| **Cursor `before`** | UUID message id — вернуть сообщения **старше** этого created_at. |
| **next_before** | Если есть ещё старые — id для следующей страницы. |
| **Ownership** | Thread must belong to user; иначе NOT_FOUND. |
| **metadata_json** | JSONB для UI actions (navigate, editor draft). |

### 2. Высокоуровневое описание

Представим загрузку истории как **просмотр киноплёнки с конца — сначала последние кадры, затем отмотка назад**.

1. **Ownership gate:** `ListMessages` проверяет, что thread принадлежит `user_id` из metadata; иначе NOT_FOUND без утечки.
2. **Initial page:** запрос без `before` — SELECT по `created_at DESC`, limit clamp 1..100 (default 100); берётся limit+1 row для детекта overflow.
3. **Cursor pagination:** если есть «лишний» row — `next_before` = его id; UI передаёт его как `before` для следующей страницы старых сообщений.
4. **Chronological response:** выбранные rows reverse → хронологический порядок для чата; `metadata_json` сохраняет UI actions (navigate, editor draft).

Таким образом, PolyGuide загружает длинные диалоги порциями без полной перезагрузки thread и без нарушения порядка реплик в UI.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Initial load (Happy Path)

1. gRPC `ListMessages(thread_id, limit=50)`.
2. Response: до 50 messages + optional `next_before`.

#### Сценарий Б: Load older (Happy Path)

1. gRPC `ListMessages(..., before=<next_before>)`.
2. Response: более старые сообщения.

#### Сценарий В: Invalid before id (Edge)

1. `before` не найден в thread → пустой items, next_before null.

---

*Следующая группа: [[03 - Запуски агента (Agent Runs)]].*
