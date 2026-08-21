# Domain policy и Intent routing

**SR:** **SR-AGENT-DOM-01**, **SR-AGENT-DOM-02** (`AgentDomainPolicy`), **SR-AGENT-INTENT-01** (`AgentIntentRouter`).

## AgentDomainPolicy.Classify

Order of evaluation:

1. Empty text → out_of_scope (`empty`).
2. `LearningMaterialOverride` regex → language_learning allowed.
3. `HardOutOfScope` regex → out_of_scope blocked.
4. `LanguageLearningSignals` regex → language_learning allowed.
5. Default → out_of_scope (`not_language_learning`).

`BuildOutOfScopeRefusal` — code-aware vs generic template.

## AgentIntentRouter.Route

Priority chain (first match wins):

1. Navigation keywords → Navigate + destination enum.
2. Progress keywords → GetProgress.
3. Grammar patterns → GrammarHelp + word/sentence extract.
4. Example patterns → GenerateExample.
5. Card patterns → BuildCardDraft.
6. Explain patterns → ExplainWord.
7. Domain classify → GeneralAnswer or OutOfScope.

## ExtractTargetTerm

Quoted `"term"`, `word X`, explain/define patterns, card for/about patterns.

## SanitizeLemmaLabels

Strip `Lemma:` lines from LLM output before persist/display.

Tests: extend `AgentService.Tests` when changing patterns (regex regression).
