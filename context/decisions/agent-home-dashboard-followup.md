# Agent Home Dashboard — Follow-up Work

Decision date: 2026-05-24

## What shipped in MVP

- `/dashboard` is now an AI-first learning command center with PolyGuide chat, suggested prompts, and a compact context rail (goals, decks, progress).
- Client-side rule-based intent routing and a project-scoped tool registry.
- Reused PolyGuide agents for explain, grammar, example, and card draft flows.
- Safe navigation actions and editor draft handoff via session storage.
- Shared language tool functions extracted for Editor and Dashboard.

## Backend follow-up

1. **`POST /api/ai/chat`**
   - Accept `messages[]` instead of single `prompt` strings.
   - Support authenticated, user/project-scoped requests.

2. **Streaming**
   - SSE from Next.js BFF and Aggregator for assistant responses.
   - Progressive UI in `AgentChatThread`.

3. **Server-side tool execution**
   - Typed tool schemas for progress, vocabulary, card draft, navigation metadata.
   - Idempotent mutations with explicit confirmation.

4. **Automation endpoints**
   - Wire real implementations for:
     - `/api/automation/autopilot`
     - `/api/automation/recommendations`
     - `/api/automation/mining/suggest`
     - `/api/automation/mining/approve`
   - Connect study copilot feedback service currently stubbed in `AutomationController`.

## Product follow-up

1. **Root routing**
   - Keep `/` → `/projects` until agent home proves useful.
   - Then consider authenticated redirect to `/dashboard`.

2. **Persistent threads**
   - Move from localStorage MVP to backend conversation storage per user/project.

3. **Rich embeds**
   - Inline heatmap, deck list, and vocabulary tables inside chat messages.

4. **Multi-step plans**
   - Notion-style workflows: import text → analyze → suggest terms → open Reader/Editor.

## Guardrails to preserve

- Term-first vocabulary model: exact surface forms and phrases only.
- No lemma labels as learning status or duplicate identity.
- Mutations require preview/confirmation; prefer link-out over silent writes.

## Key files

- [`polyraspad-frontend/src/components/dashboard/agent-chat/agent-dashboard-shell.tsx`](../../polyraspad-frontend/src/components/dashboard/agent-chat/agent-dashboard-shell.tsx)
- [`polyraspad-frontend/src/lib/agent/use-agent-chat.ts`](../../polyraspad-frontend/src/lib/agent/use-agent-chat.ts)
- [`polyraspad-frontend/src/lib/agent/agent-tool-registry.ts`](../../polyraspad-frontend/src/lib/agent/agent-tool-registry.ts)
- [`polyraspad-frontend/src/lib/polyguide/language-tool-functions.ts`](../../polyraspad-frontend/src/lib/polyguide/language-tool-functions.ts)
