# frontend-agent Task

Plan ID: `02-reader-mvp-mining-2026-05-14`
Agent: `frontend-agent`
Status: done
Can run in parallel: yes

## Objective

BFF `/api/ai/*`, клиент редактора, reader mining-draft и отображение контекста в инспекторе; убрать зависимость от локального Ollama в UI-потоках.

## Verification

- `npm test` в `polyraspad-frontend` (ollama-client / reader при необходимости).
