-- Migration: Fix orders.order_items.combo_id type mismatch
-- Phase: 1.2 - Type Mismatch Fixes (CRITICAL)
-- Target: orders.order_items
-- Changes: combo_id uuid → bigint (to match menu.combos.combo_id)

BEGIN;

-- Step 1: Create audit table for orphaned combos
CREATE TABLE IF NOT EXISTS audit.orphaned_combo_references (
    order_item_id uuid,
    combo_id_uuid uuid,
    order_id uuid,
    created_at timestamptz DEFAULT now()
);

-- Step 2: Add new column
ALTER TABLE orders.order_items 
    ADD COLUMN IF NOT EXISTS combo_id_new bigint;

-- Step 3: Migrate data (map uuid to bigint via menu.combos)
UPDATE orders.order_items oi
SET combo_id_new = c.combo_id
FROM menu.combos c
WHERE oi.combo_id::text = c.combo_id::text
  AND oi.combo_id IS NOT NULL;

-- Step 4: Log orphaned combos (combo_id exists in order_items but not in menu.combos)
INSERT INTO audit.orphaned_combo_references (order_item_id, combo_id_uuid, order_id)
SELECT oi.order_item_id, oi.combo_id, oi.order_id
FROM orders.order_items oi
WHERE oi.combo_id IS NOT NULL
  AND oi.combo_id_new IS NULL
  AND NOT EXISTS (
    SELECT 1 FROM menu.combos c WHERE c.combo_id::text = oi.combo_id::text
  );

-- Step 5: Verify migration
DO $$
DECLARE
    orphaned_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO orphaned_count
    FROM audit.orphaned_combo_references;
    
    IF orphaned_count > 0 THEN
        RAISE WARNING 'Found % orphaned combo references. Review audit.orphaned_combo_references', orphaned_count;
    END IF;
END $$;

-- Step 6: Drop old column
ALTER TABLE orders.order_items 
    DROP COLUMN IF EXISTS combo_id;

-- Step 7: Rename new column
ALTER TABLE orders.order_items 
    RENAME COLUMN combo_id_new TO combo_id;

-- Step 8: Recreate index
CREATE INDEX IF NOT EXISTS idx_order_items_combo_id ON orders.order_items(combo_id);

COMMIT;

-- Validation queries (run after migration)
-- SELECT COUNT(*) FROM audit.orphaned_combo_references; -- Review orphaned combos
-- SELECT COUNT(*) FROM orders.order_items oi LEFT JOIN menu.combos c ON oi.combo_id = c.combo_id WHERE oi.combo_id IS NOT NULL AND c.combo_id IS NULL; -- Should return 0 (after fixing orphaned)

