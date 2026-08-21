# Группа 4: Платформенные контракты (Operations)

## Введение

Эксплуатационные и контрактные возможности Media Service: health-check, gRPC-only surface и правила identity context.

Media Service **не** валидирует JWT — доверяет `user_id` из trusted caller (Aggregator).

**Метафора:** платформенные контракты Media Service — **щитовая и паспортный стол**. Health-check показывает, что «электричество в здании есть»; `user_id` в metadata — как штамп «вход разрешён trusted BFF».

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Платформенные контракты (Operations).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-MEDIA-OPS-01** | **Health-check:** Liveness `GET /healthz` → `{ status: ok }`. |
| **SR-MEDIA-OPS-02** | **gRPC identity context:** Mandatory `user_id` metadata для library RPC; `Unauthenticated` если invalid. |

---

# Детальная спецификация требований

## SR-MEDIA-OPS-01: Health-check {#SR-MEDIA-OPS-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **HTTP/1.1** | Minimal endpoint alongside gRPC Kestrel. |
| **No storage probe** | Не проверяет MinIO (fast liveness). |

### 2. Высокоуровневое описание

Представим health-check как **индикатор «электричество в здании есть» на щитовой**.

1. **Liveness probe:** Docker compose и CI отправляют `GET /healthz` на HTTP/1.1 endpoint alongside gRPC Kestrel.
2. **Minimal check:** Endpoint не проверяет MinIO (fast liveness) — только «процесс жив».
3. **Ответ:** HTTP 200, JSON `{ status: ok }`.
4. **Эксплуатация:** Orchestrator использует результат для restart/recreate decision без нагрузки на S3.

Таким образом, `/healthz` даёт быстрый сигнал liveness без зависимости от доступности object storage.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Compose health (Happy Path)

1. **GET** `/healthz`.
2. **Ответ:** HTTP 200, JSON `status=ok`.

---

## SR-MEDIA-OPS-02: gRPC identity context {#SR-MEDIA-OPS-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Header `user_id`** | UUID string в gRPC metadata. |
| **Library RPC** | Все List/Save/Delete/Share require valid user. |
| **Upload RPC** | Upload не требует user_id в текущей реализации; identity на BFF. |
| **Zero Trust internal** | Downstream callers must not forge user_id — network policy + BFF only. |

### 2. Высокоуровневое описание

Представим gRPC identity context как **штамп «вход разрешён trusted BFF» на паспортном столе**.

1. **Извлечение identity на BFF:** Aggregator извлекает user из JWT и прокидывает UUID string в gRPC client metadata header `user_id`.
2. **Mandatory для library RPC:** Все List/Save/Delete/Share require valid user; missing/invalid → `Unauthenticated`.
3. **Изоляция в S3:** Media изолирует данные по owner `userId` в S3 key paths (`reader-library/{userId}/…`, `reader-collections/{userId}/…`).
4. **Upload без user_id:** Upload RPC не требует `user_id` в текущей реализации; identity на BFF; Zero Trust internal — downstream callers must not forge `user_id`.

Таким образом, Media Service не валидирует JWT, но доверяет `user_id` только от trusted caller (Aggregator) и изолирует library data по owner paths.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Library call with identity (Happy Path)

1. **gRPC:** `ListReaderLibraryBooks` + metadata `user_id=…`.
2. **Ответ:** books for that user index.

#### Сценарий Б: Missing user_id (Negative Path)

1. **gRPC:** library RPC без header.
2. **Ответ:** `Unauthenticated` — Valid user_id header is required.

---

*Конец функциональной спецификации Media Service (группы 1–4).*
