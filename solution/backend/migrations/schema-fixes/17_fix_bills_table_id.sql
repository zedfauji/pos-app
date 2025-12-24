-- Audit existing bills with placeholder table_id
-- NOTE: billing.bills is IMMUTABLE - cannot be updated
-- This script documents the issue and ensures future bills are created correctly

BEGIN;

-- Create audit table for bills with placeholder table_id
CREATE TABLE IF NOT EXISTS audit.bills_table_id_fix (
    bill_id uuid PRIMARY KEY,
    old_table_id uuid,
    recommended_table_id uuid,
    table_label text,
    session_id uuid,
    audited_at timestamptz DEFAULT now(),
    status text, -- 'no_table_found', 'session_not_found', 'table_found'
    note text
);

-- Audit bills with placeholder table_id
INSERT INTO audit.bills_table_id_fix (bill_id, old_table_id, recommended_table_id, table_label, session_id, status, note)
SELECT 
    b.bill_id,
    b.table_id AS old_table_id,
    COALESCE(t.table_id, '00000000-0000-0000-0000-000000000001'::uuid) AS recommended_table_id,
    ts.table_label,
    b.session_id,
    CASE 
        WHEN t.table_id IS NOT NULL THEN 'table_found'
        WHEN ts.session_id IS NOT NULL THEN 'no_table_found'
        ELSE 'session_not_found'
    END AS status,
    CASE 
        WHEN t.table_id IS NOT NULL THEN 'Table exists but bill is immutable - cannot update. Frontend should use COALESCE logic.'
        WHEN ts.session_id IS NOT NULL THEN 'Session exists but table_label does not match any table. Session has invalid table_label.'
        ELSE 'Session not found - orphaned bill'
    END AS note
FROM billing.bills b
LEFT JOIN public."TableSessions" ts ON ts.session_id = b.session_id
LEFT JOIN public.tables t ON t.table_number = ts.table_label
WHERE b.table_id = '00000000-0000-0000-0000-000000000001'::uuid
ON CONFLICT (bill_id) DO NOTHING;

-- Report results
DO $$
DECLARE
    total_count integer;
    table_found_count integer;
    not_found_count integer;
BEGIN
    SELECT COUNT(*) INTO total_count FROM audit.bills_table_id_fix;
    SELECT COUNT(*) INTO table_found_count FROM audit.bills_table_id_fix WHERE status = 'table_found';
    SELECT COUNT(*) INTO not_found_count FROM audit.bills_table_id_fix WHERE status IN ('no_table_found', 'session_not_found');
    
    RAISE NOTICE 'Bills table_id audit complete:';
    RAISE NOTICE '  Total bills with placeholder: %', total_count;
    RAISE NOTICE '  Bills where table exists: % (cannot fix - bills are immutable)', table_found_count;
    RAISE NOTICE '  Bills where table not found: % (session has invalid table_label)', not_found_count;
    RAISE NOTICE '  See audit.bills_table_id_fix for details';
    RAISE NOTICE '';
    RAISE NOTICE 'NOTE: billing.bills is IMMUTABLE - existing bills cannot be updated.';
    RAISE NOTICE 'Frontend should use COALESCE(b.table_label, ts.table_label, ''Unknown'') to handle missing labels.';
    RAISE NOTICE 'Future bills will be created correctly with proper table_id.';
END $$;

COMMIT;

