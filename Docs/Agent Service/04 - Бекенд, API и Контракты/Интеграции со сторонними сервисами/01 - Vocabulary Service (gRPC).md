# Vocabulary Service (gRPC)

## ContentService

| Method | Caller | Request | SR |
| :--- | :--- | :--- | :--- |
| `GetProjectDetails` | `VocabularyProjectAccessValidator` | user_id, project_id + metadata | SR-AGENT-VOC-01 |

**Purpose:** Validate project exists and user has access; load `source_lang`, `target_lang`, `title`.

**Errors:** NotFound / PermissionDenied → NOT_FOUND to gRPC client.

---

## AnalyticsService

| Method | Caller | SR |
| :--- | :--- | :--- |
| `GetVocabularyStats` | `VocabularyGrpcClient` | SR-AGENT-VOC-02, SR-AGENT-NAV-02 |
| `GetDailySummary` | `VocabularyGrpcClient` | SR-AGENT-NAV-02 |

---

## AIService

| Method | Caller | SR |
| :--- | :--- | :--- |
| `ExplainGrammar` | `VocabularyGrpcClient` | SR-AGENT-TOOL-02 |
| `GenerateContext` | `VocabularyGrpcClient` | SR-AGENT-TOOL-03, SR-AGENT-TOOL-04 |

## Metadata outbound

```text
user_id: <guid>
roles: <comma-separated>
```

Proto imports: `AgentService/Protos/vocabulary-client.proto` (generated from VocabularyService).
