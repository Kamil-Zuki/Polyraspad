# Оркестрация ExecuteRun

**SR:** **SR-AGENT-RUN-02**, **SR-AGENT-LLM-01** (via `IAgentLlmProvider`), **SR-AGENT-TOOL-02…05** (tool dispatch), **SR-AGENT-NAV-01** / **SR-AGENT-NAV-02** (Navigate, GetProgress tools).

Класс: `AgentOrchestrator` (`AgentService/Services/AgentOrchestrator.cs`).

## Pipeline

```
ExecuteRunAsync
  ├─ validate user_text
  ├─ EnsureProjectAccess → project langs
  ├─ AgentIntentRouter.Route(user_text)
  ├─ if LLM tool && !domain.Allowed → OutOfScope intent
  ├─ ExecuteToolAsync (switch AgentToolId)
  │    ├─ Navigate / GetProgress
  │    ├─ ExplainWord / GeneralAnswer → LLM
  │    ├─ GrammarHelp / GenerateExample / BuildCardDraft → Vocabulary AIService
  │    └─ OutOfScope → static refusal
  ├─ on exception → error assistant + failed tool
  └─ AgentThreadService.CreateRunAsync (persist)
```

## Tool → side effects

| ToolId | External calls | metadata actions |
| :--- | :--- | :--- |
| Navigate | none | navigate action cards |
| GetProgress | Analytics x2 | Study + Vocabulary actions |
| ExplainWord | LLM | editor draft, vocabulary |
| GrammarHelp | AIService ExplainGrammar | — |
| GenerateExample | AIService GenerateContext | editor draft |
| BuildCardDraft | optional GenerateContext | editor draft |
| GeneralAnswer | LLM | optional refusal flag |
| OutOfScope | none | suggested prompts |

## Metadata

`AgentMessageMetadataBuilder.Build` — JSON for UI: intent_category, actions[], refusal, suggested_prompts.
