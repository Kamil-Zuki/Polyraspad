# Группа 9: Интеграция с Vocabulary (Vocabulary Integration)

## Введение

Agent Service **не дублирует** project/content domain. Доступ к проекту и обучающие AI helpers — через gRPC клиенты к **VocabularyService** (ContentService, AnalyticsService, AIService).

**Метафора:** интеграция с Vocabulary — **мост к отделу словаря**. Агент не хранит проекты и статистику сам — он звонит «соседнему зданию» по внутренней линии gRPC.

---

## Возможности данного раздела

Ниже представлен перечень функциональных требований (SR) к Интеграция с Vocabulary (Vocabulary Integration).

| Код | Название и Описание |
| :---------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **SR-AGENT-VOC-01** | **Project access validation:** GetProjectDetails с metadata user_id + roles. |
| **SR-AGENT-VOC-02** | **Analytics и AI helpers:** Stats, daily summary, grammar, context generation. |

---

# Детальная спецификация требований

## SR-AGENT-VOC-01: Project access validation {#SR-AGENT-VOC-01}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Gate** | ListThreads, CreateThread, ExecuteRun. |
| **NotFound mapping** | gRPC NotFound/PermissionDenied → KeyNotFoundException → NOT_FOUND. |
| **ProjectResponse** | source_lang, target_lang, title для orchestration. |

### 2. Высокоуровневое описание

Представим project access validation как **пропуск через мост к отделу словаря**.

1. **Gate points:** ListThreads, CreateThread, ExecuteRun вызывают validator до своей domain logic.
2. **gRPC call:** ContentService `GetProjectDetails` с metadata `user_id` + `roles` из Aggregator.
3. **NotFound mapping:** gRPC NotFound/PermissionDenied → KeyNotFoundException → NOT_FOUND клиенту.
4. **ProjectResponse:** `source_lang`, `target_lang`, title передаются orchestrator для downstream tools.

Таким образом, Agent не хранит project ACL — единый источник прав и языкового контекста в VocabularyService.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: Create thread in owned project (Happy Path)

1. gRPC `CreateThread` с валидным `project_id`.
2. ContentService подтверждает доступ.
3. Thread row создаётся в PostgreSQL.

---

## SR-AGENT-VOC-02: Analytics и AI helpers {#SR-AGENT-VOC-02}

### 1. Цель и ключевые принципы

| Принцип | Описание |
| :--- | :--- |
| **Metadata propagation** | user_id + roles на каждый outbound call. |
| **AnalyticsService** | GetVocabularyStats, GetDailySummary. |
| **AIService** | ExplainGrammar, GenerateContext. |

### 2. Высокоуровневое описание

Представим analytics и AI helpers как **внутреннюю линию к Vocabulary RPC**.

1. **Encapsulation:** `VocabularyGrpcClient` скрывает proto details AnalyticsService и AIService от orchestrator.
2. **Metadata propagation:** `user_id` + `roles` на каждый outbound call — Vocabulary применяет свои ACL.
3. **AnalyticsService:** `GetVocabularyStats`, `GetDailySummary` для get_progress tool.
4. **AIService:** `ExplainGrammar`, `GenerateContext` для grammar_help, generate_example и build_card_draft.

Таким образом, learning tools переиспользуют Vocabulary AI/analytics без дублирования domain data в Agent PostgreSQL.

### 3. Примеры взаимодействия (логические сценарии)

#### Сценарий А: ExecuteRun without project access (Negative Path)

1. User removed from project.
2. EnsureProjectAccess throws.
3. gRPC NOT_FOUND to client.

---

*Следующая группа: [[10 - LLM-провайдер (LLM Provider)]].*
