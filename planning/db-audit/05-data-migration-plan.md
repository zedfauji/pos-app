# 05 - Data Migration & Fix Plan

**Status**: PLANNING (NO EXECUTION YET)  
**Date**: 2025-12-23  
**Auditor**: Principal Database Architect

---

## Purpose

This document outlines data migration strategies to fix orphaned data, invalid states, and data inconsistencies identified in Step 3.

---

## Data Issues Summary

### Critical Issues
1. **1 bill without session** - Bill ID: `a764efd7-42db-4eec-bf4f-25b9a1e0488d`

### Warning Issues
1. **18 ended sessions without bills** - Sessions ended but bills never created

---

## 1. Fix Orphaned Bill

### Issue
Bill `a764efd7-42db-4eec-bf4f-25b9a1e0488d` references session `ba3af093-0c4a-430a-9ec3-40201bb11662` which doesn't exist.

### Investigation Query
```sql
SELECT 
    b.bill_id,
    b.billing_id,
    b.session_id,
    b.status,
    b.total_amount,
    b.created_at,
    b.settled_at
FROM billing.bills b
WHERE b.bill_id = 'a764efd7-42db-4eec-bf4f-25b9a1e0488d';

-- Check if session exists in legacy table
SELECT * FROM public."TableSessions" 
WHERE session_id = 'ba3af093-0c4a-430a-9ec3-40201bb11662';

-- Check if session exists in active table
SELECT * FROM tables.sessions 
WHERE session_id = 'ba3af093-0c4a-430a-9ec3-40201bb11662';
```

### Fix Strategy Options

#### Option A: Delete Orphaned Bill (If No Payments)
**Condition**: Bill has no payments and is not settled

**SQL**:
```sql
-- Check for payments
SELECT COUNT(*) FROM pay.payments 
WHERE bill_id = 'a764efd7-42db-4eec-bf4f-25b9a1e0488d';

-- If no payments, delete bill
DELETE FROM billing.bills 
WHERE bill_id = 'a764efd7-42db-4eec-bf4f-25b9a1e0488d'
  AND NOT EXISTS (
      SELECT 1 FROM pay.payments p 
      WHERE p.bill_id = billing.bills.bill_id
  );
```

#### Option B: Create Missing Session (If Session Data Exists)
**Condition**: Session data exists in legacy table or can be reconstructed

**SQL**:
```sql
-- If session exists in legacy table, migrate it
INSERT INTO tables.sessions (
    session_id,
    table_id,
    billing_id,
    start_time,
    end_time,
    is_active,
    state,
    accumulated_cost,
    guest_count,
    started_by,
    created_at,
    updated_at
)
SELECT 
    ts.session_id,
    t.table_id,
    ts.billing_id,
    ts.start_time,
    ts.end_time,
    ts.status = 'active',
    ts.status,
    0,
    1,
    ts.server_id,
    ts.start_time,
    COALESCE(ts.end_time, ts.start_time)
FROM public."TableSessions" ts
JOIN public.tables t ON t.table_number = ts.table_label
WHERE ts.session_id = 'ba3af093-0c4a-430a-9ec3-40201bb11662'
  AND NOT EXISTS (
      SELECT 1 FROM tables.sessions s 
      WHERE s.session_id = ts.session_id
  );
```

#### Option C: Update Bill to Reference Valid Session (If Session Moved)
**Condition**: Session was moved/merged and new session_id exists

**SQL**:
```sql
-- Find valid session for this billing_id
SELECT s.session_id 
FROM tables.sessions s
WHERE s.billing_id = 'a764efd7-42db-4eec-bf4f-25b9a1e0488d'
LIMIT 1;

-- Update bill to reference valid session
UPDATE billing.bills
SET session_id = (SELECT s.session_id 
                  FROM tables.sessions s
                  WHERE s.billing_id = billing.bills.billing_id
                  LIMIT 1)
WHERE bill_id = 'a764efd7-42db-4eec-bf4f-25b9a1e0488d'
  AND session_id NOT IN (SELECT session_id FROM tables.sessions);
```

### Recommended Action
**Investigate first**, then choose appropriate fix:
1. Check if bill has payments
2. Check if session exists in legacy table
3. Check if valid session exists for billing_id
4. Apply appropriate fix

---

## 2. Fix Ended Sessions Without Bills

### Issue
18 ended sessions (`is_active = false`) have `billing_id` but no corresponding bill in `billing.bills`.

### Investigation Query
```sql
SELECT 
    s.session_id,
    s.billing_id,
    s.table_id,
    s.start_time,
    s.end_time,
    s.accumulated_cost,
    s.state,
    s.started_by
FROM tables.sessions s
WHERE s.is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = s.billing_id
  )
ORDER BY s.end_time DESC;
```

### Fix Strategy Options

#### Option A: Create Bills Retroactively (If Data is Valid)
**Condition**: Sessions have valid data and should have bills

**SQL**:
```sql
-- Create bills for ended sessions without bills
INSERT INTO billing.bills (
    bill_id,
    billing_id,
    session_id,
    table_id,
    status,
    items_total,
    time_total,
    subtotal,
    discounts,
    tax,
    total_amount,
    time_minutes,
    created_at,
    updated_at,
    table_label,
    server_name
)
SELECT 
    s.billing_id as bill_id, -- Use billing_id as bill_id
    s.billing_id,
    s.session_id,
    s.table_id,
    'AwaitingPayment'::billing.bill_status,
    COALESCE((
        SELECT SUM(oi.line_total)
        FROM orders.order_items oi
        JOIN orders.orders o ON oi.order_id = o.order_id
        WHERE o.session_id = s.session_id
    ), 0) as items_total,
    s.accumulated_cost as time_total,
    COALESCE((
        SELECT SUM(oi.line_total)
        FROM orders.order_items oi
        JOIN orders.orders o ON oi.order_id = o.order_id
        WHERE o.session_id = s.session_id
    ), 0) + s.accumulated_cost as subtotal,
    0 as discounts,
    0 as tax,
    COALESCE((
        SELECT SUM(oi.line_total)
        FROM orders.order_items oi
        JOIN orders.orders o ON oi.order_id = o.order_id
        WHERE o.session_id = s.session_id
    ), 0) + s.accumulated_cost as total_amount,
    EXTRACT(EPOCH FROM (s.end_time - s.start_time)) / 60 as time_minutes,
    s.end_time as created_at,
    s.end_time as updated_at,
    t.table_number as table_label,
    s.started_by as server_name
FROM tables.sessions s
JOIN public.tables t ON s.table_id = t.table_id
WHERE s.is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = s.billing_id
  )
  AND s.end_time IS NOT NULL;
```

#### Option B: Mark as Test Data (If Data is Invalid)
**Condition**: Sessions are test data or invalid

**SQL**:
```sql
-- Add a flag or move to archive
-- Option 1: Add test flag (requires schema change)
ALTER TABLE tables.sessions ADD COLUMN is_test BOOLEAN DEFAULT false;

UPDATE tables.sessions
SET is_test = true
WHERE is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = tables.sessions.billing_id
  );

-- Option 2: Move to archive table
CREATE TABLE IF NOT EXISTS tables.sessions_archive 
    (LIKE tables.sessions INCLUDING ALL);

INSERT INTO tables.sessions_archive
SELECT * FROM tables.sessions
WHERE is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = tables.sessions.billing_id
  );

DELETE FROM tables.sessions
WHERE is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = tables.sessions.billing_id
  );
```

#### Option C: Delete Sessions (If Definitely Invalid)
**Condition**: Sessions are definitely invalid and can be safely deleted

**SQL**:
```sql
-- Delete sessions without bills (ONLY if confirmed invalid)
DELETE FROM tables.sessions
WHERE is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = tables.sessions.billing_id
  )
  AND NOT EXISTS (
      SELECT 1 FROM orders.orders o 
      WHERE o.session_id = tables.sessions.session_id
  )
  AND end_time < NOW() - INTERVAL '30 days'; -- Only old sessions
```

### Recommended Action
**Investigate first**, then choose appropriate fix:
1. Check if sessions have orders
2. Check if sessions have accumulated_cost > 0
3. Check if sessions are recent or old
4. If valid data: Create bills (Option A)
5. If test data: Mark as test (Option B)
6. If invalid old data: Delete (Option C)

---

## 3. Populate New Columns

### 3.1 Populate `billing.bills.server_id`, `start_time`, `end_time`

**SQL**:
```sql
UPDATE billing.bills b
SET 
    server_id = s.started_by,
    start_time = s.start_time,
    end_time = s.end_time
FROM tables.sessions s
WHERE b.session_id = s.session_id
  AND (b.server_id IS NULL OR b.start_time IS NULL OR b.end_time IS NULL);
```

---

### 3.2 Populate `orders.orders.table_id`, `server_id`, `server_name`

**SQL**:
```sql
UPDATE orders.orders o
SET 
    table_id = t.table_number,
    server_id = s.started_by,
    server_name = COALESCE(s.started_by, 'Unknown')
FROM tables.sessions s
LEFT JOIN public.tables t ON s.table_id = t.table_id
WHERE o.session_id = s.session_id
  AND (o.table_id IS NULL OR o.server_id IS NULL OR o.server_name IS NULL);
```

---

### 3.3 Populate `pay.payment_ledger.session_id`

**SQL**:
```sql
UPDATE pay.payment_ledger pl
SET session_id = b.session_id
FROM billing.bills b
WHERE pl.billing_id = b.billing_id
  AND pl.session_id IS NULL;
```

---

### 3.4 Populate `pay.payments.session_id`

**SQL**:
```sql
UPDATE pay.payments p
SET session_id = b.session_id
FROM billing.bills b
WHERE p.bill_id = b.bill_id
  AND p.session_id IS NULL;
```

---

## 4. Data Validation Queries

### 4.1 Verify All Bills Have Sessions
```sql
SELECT COUNT(*) as orphaned_bills
FROM billing.bills b
WHERE NOT EXISTS (
    SELECT 1 FROM tables.sessions s 
    WHERE s.session_id = b.session_id
);
-- Expected: 0
```

### 4.2 Verify All Ended Sessions Have Bills
```sql
SELECT COUNT(*) as sessions_without_bills
FROM tables.sessions s
WHERE s.is_active = false
  AND NOT EXISTS (
      SELECT 1 FROM billing.bills b 
      WHERE b.billing_id = s.billing_id
  );
-- Expected: 0 (after migration)
```

### 4.3 Verify All Payments Have Bills
```sql
SELECT COUNT(*) as orphaned_payments
FROM pay.payments p
WHERE NOT EXISTS (
    SELECT 1 FROM billing.bills b 
    WHERE b.bill_id = p.bill_id
);
-- Expected: 0
```

### 4.4 Verify All Orders Have Sessions
```sql
SELECT COUNT(*) as orphaned_orders
FROM orders.orders o
WHERE NOT EXISTS (
    SELECT 1 FROM tables.sessions s 
    WHERE s.session_id = o.session_id
);
-- Expected: 0
```

---

## 5. Migration Execution Plan

### Phase 1: Investigation
1. Run investigation queries for orphaned bill
2. Run investigation queries for ended sessions without bills
3. Determine appropriate fix strategy for each issue

### Phase 2: Fix Orphaned Data
1. Fix orphaned bill (choose appropriate option)
2. Fix ended sessions without bills (choose appropriate option)
3. Verify fixes with validation queries

### Phase 3: Populate New Columns
1. Populate `billing.bills` new columns
2. Populate `orders.orders` new columns
3. Populate `pay.payment_ledger.session_id`
4. Populate `pay.payments.session_id`
5. Verify all columns populated

### Phase 4: Final Validation
1. Run all validation queries
2. Verify no orphaned data
3. Verify referential integrity
4. Document any remaining issues

---

## 6. Rollback Plan

### If Migration Fails
1. **Stop immediately** - Do not proceed with remaining steps
2. **Review error logs** - Identify which step failed
3. **Rollback affected changes**:
   - If bills created: Delete created bills
   - If sessions deleted: Restore from backup
   - If columns populated: Set to NULL (if nullable)
4. **Restore from backup** if necessary

### Backup Strategy
```sql
-- Create backup tables before migration
CREATE TABLE billing.bills_backup AS SELECT * FROM billing.bills;
CREATE TABLE tables.sessions_backup AS SELECT * FROM tables.sessions;
CREATE TABLE pay.payments_backup AS SELECT * FROM pay.payments;
CREATE TABLE pay.payment_ledger_backup AS SELECT * FROM pay.payment_ledger;
```

---

## 7. Success Criteria

### Data Integrity
- ✅ All bills have valid sessions
- ✅ All ended sessions have bills (or marked as test/invalid)
- ✅ All payments have valid bills
- ✅ All orders have valid sessions
- ✅ All new columns populated (where applicable)

### Data Quality
- ✅ No orphaned rows
- ✅ No invalid enum values
- ✅ No negative amounts
- ✅ No invalid time ranges

---

## Next Steps

1. **Step 6**: Create production hardening plan
2. **Step 7**: Go/No-Go checkpoint for approval
3. **Step 8**: Execute migration (after approval)

---

**END OF DATA MIGRATION PLAN**

**⚠️ NO MIGRATIONS EXECUTED YET - AWAITING APPROVAL**

