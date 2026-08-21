# Введение

PolyGuide ограничен **language learning**. Domain policy — regex gate перед дорогими LLM вызовами.

## Контекст и проблема

Пользователи могут просить написать код, решить homework или general trivia — это вне product scope и риск brand/safety.

## Принятое решение

1. `AgentDomainPolicy.Classify` — learning override, hard out-of-scope, language signals.
2. LLM tool ids (`ExplainWord`, `GrammarHelp`, …) при `!allowed` → `OutOfScope` tool.
3. `AgentDomainDecision` row на каждый run.
4. `BuildOutOfScopeRefusal` — static templates + suggested prompts.

## Обоснование и последствия

### Плюсы

* Предсказуемый refuse без LLM cost.
* Audit category в БД для analytics.

### Последствия

* Regex false positives/negatives возможны.
* *Решение:* LearningMaterialOverride для «vocabulary from this code snippet»; iterate patterns in tests.
