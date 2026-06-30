# Backend Task

Plan ID: `agent-service-docs`
Agent: `backend-agent`
Status: done
Can run in parallel: no

## Objective

Написать полную STEOS-документацию для Agent Service в `Docs/Agent Service/` на основе кода `AgentService/`.

## Inputs

- Plan: `.cursor/plans/active/agent-service-docs_f2a8c3d1.plan.md`
- Rules: `Docs/.cursor/rules/steos-docs-core.mdc`, `steos-docs-folders-010305.mdc`
- Format etalon (layout only): `Docs/(Done) Authorization Service/`
- Table format etalon: `Docs/Aggregator Service/01 - Функциональная спецификация/Возможности сервиса/00 - Общая информация.md` — column `Название и Описание` with `**Title:** description`
- Code: `AgentService/` — Protos/agent.proto, Grpc/, Services/, Orchestration/, Data/Entities/

## Scope

1. Scaffold `Docs/Agent Service/` tree (01, 02, 03, 04, 99, README)
2. **03 first** — entity docs for all 6 DB entities + index
3. **01** — 00 overview, термины, NFR, ~8-11 capability group files with SR blocks
4. **02** — 00 architecture + КАР files (gRPC-only, orchestration, domain policy, LLM, vocabulary integration)
5. **04** — gRPC (all rpc from agent.proto), DTO, Integrations (VocabularyService), Algorithms
6. **99** — staging registry (empty or ISSUE if found)

## Out of Scope

- Code changes
- Folder 05
- REST API subfolder in 04 (Agent has no REST)
- Redis, RabbitMQ, Socket subfolders

## SR Groups (suggested from code)

| # | Group | Key SR areas |
|---|-------|--------------|
| 1 | Управление тредами (Thread Management) | ListThreads, CreateThread, GetThread, ArchiveThread |
| 2 | История сообщений (Message History) | ListMessages, pagination |
| 3 | Запуски агента (Agent Runs) | CreateRun, ExecuteRun |
| 4 | Доменная политика (Domain Policy) | AgentDomainPolicy.Classify, out-of-scope refusal |
| 5 | Маршрутизация намерений (Intent Routing) | AgentIntentRouter, tool selection |
| 6 | Инструменты обучения (Learning Tools) | explain_word, grammar_help, generate_example, build_card_draft, general_answer |
| 7 | Навигация и прогресс (Navigation & Progress) | navigate, get_progress tools |
| 8 | Артефакты (Artifacts) | CreateArtifact, ListArtifacts |
| 9 | Интеграция с Vocabulary (Vocabulary Integration) | project access, Analytics, AIService mining |
| 10 | LLM-провайдер (LLM Provider) | OpenAiCompatibleAgentLlmProvider, AiOptions |
| 11 | Платформенные контракты (Operations) | healthz, migrations, gRPC-only Kestrel |

## Deliverables

- All markdown files under `Docs/Agent Service/`
- README.md for service folder

## Verification

- rg `SR-AGENT-` in Docs/Agent Service/ — codes consistent 00 ↔ group files
- No SR-AUTH-* or Auth domain text
- 03 entity fields match AgentServiceContext.cs

## Handoff

Return: files created list, SR count, any ISSUEs written, blockers.
