# ISSUE-003: ExecuteRun — LLM tool loop vs classic intent pipeline

## Тип

Противоречие

## В двух словах

Раньше docs/`04`/`02` описывали ExecuteRun как Domain classify → Intent route → classic tools (`explain_word`, `navigate`, …). В коде primary path — LLM `CompleteChatAsync` + `AvailableTools` / `ExecuteToolCoreAsync`. IntentRouter почти не влияет (кроме GeneratePractice); domain в persist всегда `language_learning`. Folder `01` обновлён под код; `02`/`04` могут ещё описывать classic path.

## Где проблема

| Источник | Якорь | Что не сходится |
| :--- | :--- | :--- |
| 01 | SR-AGENT-RUN-02, TOOL-01..05, INTENT-01, DOM-01 | Обновлены: LLM loop + intent hints |
| 02 / 04 | КАР tool calling / алгоритмы ExecuteRun | Могут всё ещё описывать classic dispatch |
| код | `AgentOrchestrator.ExecuteRunAsync` | LLM tools only; domain hardcoded |

Путь (вторично): `AgentService/Services/AgentOrchestrator.cs`

## Доказательство

`ExecuteToolCoreAsync` cases: `create_deck`, `create_card`, `get_user_vocabulary_stats`, … — нет `explain_word`/`navigate`. Persist: `new AgentDomainDecision(true, LanguageLearning)`.

## Рекомендуемое действие

При следующем проходе `02`/`04` — переписать оркестрацию под LLM tool loop; не восстанавливать classic handlers в `01` без кода.

## Статус

Open
