# Task: Full capabilities ### 2 alignment

**Plan:** `steos-01-full-capabilities`  
**Status:** in_progress

## Scope

Привести все SR-блоки `### 2. Высокоуровневое описание` к эталону Aggregator (метафора + шаги + «Таким образом»).

## Subagents

| Agent | Service(s) | SR count |
|-------|------------|----------|
| ce0f2430 | Agent Service | 25 |
| 55a740f4 | Auth Module, Billing, Media | 45 |

## Verification

```powershell
python .cursor/tmp_audit_main_caps.py
# Expect: thin_h2=0 for all services
```
