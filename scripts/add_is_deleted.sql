-- Run once on your Neon PostgreSQL database
ALTER TABLE habits ADD COLUMN IF NOT EXISTS is_deleted boolean NOT NULL DEFAULT false;
