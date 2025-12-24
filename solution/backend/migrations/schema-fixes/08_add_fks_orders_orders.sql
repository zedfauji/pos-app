-- Migration: Add foreign keys to orders.orders
-- Phase: 3.1 - Add Foreign Keys (HIGH)
-- Target: orders.orders
-- Adds: FK to TableSessions, billing.bills, shifts

BEGIN;

-- Step 1: Create audit table for orphaned records
CREATE TABLE IF NOT EXISTS audit.orphaned_orders_pre_fk (
    order_id uuid,
    session_id uuid,
    billing_id uuid,
    shift_id uuid,
    reason text,
    created_at timestamptz DEFAULT now()
);

-- Step 2: Find orphaned session_ids
INSERT INTO audit.orphaned_orders_pre_fk (order_id, session_id, reason)
SELECT order_id, session_id, 'session_id not in TableSessions'
FROM orders.orders o
WHERE o.session_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM public."TableSessions" ts WHERE ts.session_id = o.session_id
  );

-- Step 3: Find orphaned billing_ids
INSERT INTO audit.orphaned_orders_pre_fk (order_id, billing_id, reason)
SELECT order_id, billing_id, 'billing_id not in billing.bills'
FROM orders.orders o
WHERE o.billing_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM billing.bills b WHERE b.billing_id = o.billing_id
  );

-- Step 4: Find orphaned shift_ids
INSERT INTO audit.orphaned_orders_pre_fk (order_id, shift_id, reason)
SELECT order_id, shift_id, 'shift_id not in shifts'
FROM orders.orders o
WHERE o.shift_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM public.shifts s WHERE s.shift_id = o.shift_id
  );

-- Step 5: Fix orphaned records (set to NULL)
UPDATE orders.orders 
SET session_id = NULL 
WHERE order_id IN (
    SELECT order_id FROM audit.orphaned_orders_pre_fk WHERE reason LIKE '%session_id%'
);

UPDATE orders.orders 
SET billing_id = NULL 
WHERE order_id IN (
    SELECT order_id FROM audit.orphaned_orders_pre_fk WHERE reason LIKE '%billing_id%'
);

UPDATE orders.orders 
SET shift_id = NULL 
WHERE order_id IN (
    SELECT order_id FROM audit.orphaned_orders_pre_fk WHERE reason LIKE '%shift_id%'
);

-- Step 6: Add foreign keys
ALTER TABLE orders.orders 
    ADD CONSTRAINT fk_orders_session 
    FOREIGN KEY (session_id) REFERENCES public."TableSessions"(session_id);

ALTER TABLE orders.orders 
    ADD CONSTRAINT fk_orders_billing 
    FOREIGN KEY (billing_id) REFERENCES billing.bills(billing_id);

ALTER TABLE orders.orders 
    ADD CONSTRAINT fk_orders_shift 
    FOREIGN KEY (shift_id) REFERENCES public.shifts(shift_id);

COMMIT;

-- Validation queries (run after migration)
-- SELECT constraint_name, table_name, column_name FROM information_schema.table_constraints tc JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name WHERE tc.table_schema = 'orders' AND tc.table_name = 'orders' AND tc.constraint_type = 'FOREIGN KEY';
-- Should show 3 FKs: fk_orders_session, fk_orders_billing, fk_orders_shift

