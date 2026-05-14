# backend-agent Task

Plan ID: `02-reader-mvp-mining-2026-05-14`
Agent: `backend-agent`
Status: done
Can run in parallel: yes

## Objective

Заменить Ollama на единый слой `Ai` (OpenAI-compatible chat completions): прокси для редактора, mining-draft, Study Copilot feedback.

## Verification

- `dotnet build` solution; при наличии тестов — `dotnet test` для затронутых проектов.
