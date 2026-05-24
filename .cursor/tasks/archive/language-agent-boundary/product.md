# Product Task

Plan ID: `language-agent-boundary`
Agent: `product-agent`
Status: done
Can run in parallel: yes

## Objective
Lock allowed/disallowed PolyGuide domains and refusal UX copy.

## Deliverables
- Allowed: vocabulary, grammar, reading, cards, progress, navigation, language material from arbitrary text
- Disallowed: code generation, general programming, unrelated homework/business
- Refusal: short, helpful redirect with learning alternatives

## Handoff
- Domain categories: `language_learning`, `product_navigation`, `progress`, `out_of_scope`
- Code-as-material allowed when framed as vocabulary/translation (e.g. C# snippet terms)
- No AgentService in this slice; persistence documented for Phase 4+
