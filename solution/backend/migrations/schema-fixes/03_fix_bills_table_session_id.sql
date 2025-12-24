-- Migration: Fix public.bills.table_session_id type mismatch
-- Phase: 1.3 - Type Mismatch Fixes (CRITICAL)
-- Target: public.bills
-- Changes: table_session_id integer → session_id uuid

BEGIN;

-- Step 1: Create audit table for unmapped bills
CREATE TABLE IF NOT EXISTS audit.unmapped_bills (
    bill_id uuid,
    table_session_id_old integer,
    session_id_found uuid,
    created_at timestamptz DEFAULT now()
);

-- Step 2: Add new column
ALTER TABLE public.bills 
    ADD COLUMN IF NOT EXISTS session_id_new uuid;

-- Step 3: Map integer table_session_id to uuid session_id
-- Try multiple mapping strategies
UPDATE public.bills b
SET session_id_new = ts.session_id
FROM public."TableSessions" ts
WHERE (
    -- Strategy 1: Direct mapping if table_session_id was meant to be session_id (unlikely)
    b.table_session_id::text = ts.session_id::text
    OR
    -- Strategy 2: If TableSessions has integer Id column (legacy)
    EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
          AND table_name = 'TableSessions' 
          AND column_name = 'Id'
    )
    AND b.table_session_id = (SELECT "Id" FROM public."TableSessions" ts2 WHERE ts2."Id" = b.table_session_id LIMIT 1)
    OR
    -- Strategy 3: Map via bill_id to billing.bills.session_id
    EXISTS (
        SELECT 1 FROM billing.bills bb 
        WHERE bb.bill_id = b.bill_id 
          AND bb.session_id IS NOT NULL
    )
    AND b.session_id_new = (SELECT bb.session_id FROM billing.bills bb WHERE bb.bill_id = b.bill_id LIMIT 1)
);

-- Step 4: Log unmapped bills
INSERT INTO audit.unmapped_bills (bill_id, table_session_id_old, session_id_found)
SELECT b.bill_id, b.table_session_id, b.session_id_new
FROM public.bills b
WHERE b.session_id_new IS NULL
  AND b.table_session_id IS NOT NULL;

-- Step 5: Verify migration
DO $$
DECLARE
    unmapped_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO unmapped_count
    FROM audit.unmapped_bills;
    
    IF unmapped_count > 0 THEN
        RAISE WARNING 'Found % unmapped bills. Review audit.unmapped_bills', unmapped_count;
    END IF;
END $$;

-- Step 6: Drop old column and index
DROP INDEX IF EXISTS idx_bills_table_session_id;
ALTER TABLE public.bills 
    DROP COLUMN IF EXISTS table_session_id;

-- Step 7: Rename new column
ALTER TABLE public.bills 
    RENAME COLUMN session_id_new TO session_id;

-- Step 8: Create new index
CREATE INDEX IF NOT EXISTS idx_bills_session_id ON public.bills(session_id);

COMMIT;

-- Validation queries (run after migration)
-- SELECT COUNT(*) FROM audit.unmapped_bills; -- Review unmapped bills
-- SELECT COUNT(*) FROM public.bills b LEFT JOIN public."TableSessions" ts ON b.session_id = ts.session_id WHERE b.session_id IS NOT NULL AND ts.session_id IS NULL; -- Should be 0 or handle NULLs

