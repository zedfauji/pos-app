-- Migration: Fix pay.bill_ledger type mismatches
-- Phase: 1.1 - Type Mismatch Fixes (CRITICAL)
-- Target: pay.bill_ledger
-- Changes: billing_id text → uuid, session_id text → uuid

BEGIN;

-- Step 1: Validate data format (UUID strings)
DO $$
DECLARE
    invalid_billing_ids INTEGER;
    invalid_session_ids INTEGER;
BEGIN
    -- Check for invalid UUID format in billing_id
    SELECT COUNT(*) INTO invalid_billing_ids
    FROM pay.bill_ledger
    WHERE billing_id IS NOT NULL
      AND billing_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    -- Check for invalid UUID format in session_id
    SELECT COUNT(*) INTO invalid_session_ids
    FROM pay.bill_ledger
    WHERE session_id IS NOT NULL
      AND session_id !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$';
    
    IF invalid_billing_ids > 0 OR invalid_session_ids > 0 THEN
        RAISE EXCEPTION 'Invalid UUID format detected: billing_id=%, session_id=%', 
            invalid_billing_ids, invalid_session_ids;
    END IF;
END $$;

-- Step 2: Add new columns with correct type
ALTER TABLE pay.bill_ledger 
    ADD COLUMN IF NOT EXISTS billing_id_new uuid,
    ADD COLUMN IF NOT EXISTS session_id_new uuid;

-- Step 3: Migrate data
UPDATE pay.bill_ledger 
SET 
    billing_id_new = billing_id::uuid,
    session_id_new = session_id::uuid
WHERE 
    billing_id IS NOT NULL 
    AND session_id IS NOT NULL;

-- Step 4: Verify migration
DO $$
DECLARE
    migrated_count INTEGER;
    total_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO migrated_count
    FROM pay.bill_ledger
    WHERE billing_id_new IS NOT NULL AND session_id_new IS NOT NULL;
    
    SELECT COUNT(*) INTO total_count
    FROM pay.bill_ledger
    WHERE billing_id IS NOT NULL AND session_id IS NOT NULL;
    
    IF migrated_count != total_count THEN
        RAISE EXCEPTION 'Migration failed: migrated=%, total=%', migrated_count, total_count;
    END IF;
END $$;

-- Step 5: Drop old columns and constraints
ALTER TABLE pay.bill_ledger DROP CONSTRAINT IF EXISTS bill_ledger_pkey;
ALTER TABLE pay.bill_ledger DROP COLUMN IF EXISTS billing_id;
ALTER TABLE pay.bill_ledger DROP COLUMN IF EXISTS session_id;

-- Step 6: Rename new columns
ALTER TABLE pay.bill_ledger 
    RENAME COLUMN billing_id_new TO billing_id;

ALTER TABLE pay.bill_ledger 
    RENAME COLUMN session_id_new TO session_id;

-- Step 7: Recreate primary key
ALTER TABLE pay.bill_ledger 
    ADD PRIMARY KEY (billing_id);

-- Step 8: Recreate indexes
CREATE INDEX IF NOT EXISTS ix_bill_ledger_status ON pay.bill_ledger(status);

-- Step 9: Add NOT NULL constraints
ALTER TABLE pay.bill_ledger 
    ALTER COLUMN billing_id SET NOT NULL,
    ALTER COLUMN session_id SET NOT NULL;

COMMIT;

-- Validation queries (run after migration)
-- SELECT COUNT(*) FROM pay.bill_ledger WHERE billing_id IS NULL OR session_id IS NULL; -- Should return 0
-- SELECT COUNT(*) FROM pay.bill_ledger WHERE billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'; -- Should return 0

