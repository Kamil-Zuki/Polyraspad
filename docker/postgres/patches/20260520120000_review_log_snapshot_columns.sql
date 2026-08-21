-- Idempotent patch: review_logs FSRS snapshot columns (matches EF migration 20260520120000_AddReviewLogSnapshotColumns)
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS step_before integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS step_after integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS reps_before integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS reps_after integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS lapses_before integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS lapses_after integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS elapsed_days_before integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS elapsed_days_after integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS scheduled_days_before integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS scheduled_days_after integer NOT NULL DEFAULT 0;
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS last_review_before timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE internal.review_logs ADD COLUMN IF NOT EXISTS last_review_after timestamp with time zone NOT NULL DEFAULT now();

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260520120000_AddReviewLogSnapshotColumns', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
