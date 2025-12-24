-- Fix shifts table to use varchar for User IDs (Legacy Parity)
-- This allows text-based IDs like 'admin', 'server1'

-- 1. Drop Foreign Keys (incompatible with varchar vs int mismatch if users.user_id is varchar)
ALTER TABLE public.shifts DROP CONSTRAINT IF EXISTS shifts_opened_by_user_id_fkey;
ALTER TABLE public.shifts DROP CONSTRAINT IF EXISTS shifts_closed_by_user_id_fkey;

-- 2. Alter Columns to VARCHAR
ALTER TABLE public.shifts ALTER COLUMN opened_by_user_id TYPE VARCHAR(100);
ALTER TABLE public.shifts ALTER COLUMN closed_by_user_id TYPE VARCHAR(100);

-- 3. Add Index for performance
CREATE INDEX IF NOT EXISTS idx_shifts_opened_by ON public.shifts(opened_by_user_id);
