-- One-time copy of agent persistence tables from vocabulary_service to agent_service.
-- Run manually after AgentService is deployed: psql -f docker/postgres/patches/20260524130000_migrate_agent_tables_to_agent_service.sql

INSERT INTO agent_service.internal.agent_threads (
    id, user_id, project_id, title, created_at, updated_at, archived_at)
SELECT id, user_id, project_id, title, created_at, updated_at, archived_at
FROM vocabulary_service.internal.agent_threads
ON CONFLICT (id) DO NOTHING;

INSERT INTO agent_service.internal.agent_messages (
    id, thread_id, role, content, metadata_json, created_at)
SELECT id, thread_id, role, content, metadata_json, created_at
FROM vocabulary_service.internal.agent_messages
ON CONFLICT (id) DO NOTHING;

INSERT INTO agent_service.internal.agent_runs (
    id, thread_id, status, model, started_at, completed_at, error)
SELECT id, thread_id, status, model, started_at, completed_at, error
FROM vocabulary_service.internal.agent_runs
ON CONFLICT (id) DO NOTHING;

INSERT INTO agent_service.internal.agent_tool_calls (
    id, run_id, tool_name, input_json, output_json, status, created_at)
SELECT id, run_id, tool_name, input_json, output_json, status, created_at
FROM vocabulary_service.internal.agent_tool_calls
ON CONFLICT (id) DO NOTHING;

INSERT INTO agent_service.internal.agent_domain_decisions (
    id, run_id, allowed, category, reason, user_text_preview, created_at)
SELECT id, run_id, allowed, category, reason, user_text_preview, created_at
FROM vocabulary_service.internal.agent_domain_decisions
ON CONFLICT (id) DO NOTHING;

-- Do NOT drop vocabulary_service.internal.agent_* tables here.
-- Explicit cleanup requires a separate approved migration.
