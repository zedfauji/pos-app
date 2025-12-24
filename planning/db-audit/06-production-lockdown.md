# 06 - Production Hardening & Lockdown Plan

**Status**: PLANNING (NO EXECUTION YET)  
**Date**: 2025-12-23  
**Auditor**: Principal Database Architect

---

## Purpose

This document outlines production hardening measures to enforce data integrity, prevent invalid states, and effectively "lock" the schema to prevent future drift.

**After this point, schema changes require a formal migration process.**

---

## Hardening Principles

1. **Enforce Invariants at DB Level** - Don't rely on application code
2. **Prevent Invalid States** - Use constraints to make invalid states impossible
3. **Append-Only for Financial Data** - Critical tables become immutable after creation
4. **Audit Trail** - Track all changes to financial data
5. **Referential Integrity** - All foreign keys must be enforced

---

## 1. Required NOT NULL Constraints

### Already Enforced ✅
- `billing.bills.billing_id` - NOT NULL
- `billing.bills.session_id` - NOT NULL
- `billing.bills.table_id` - NOT NULL
- `orders.orders.billing_id` - NOT NULL
- `orders.orders.session_id` - NOT NULL
- `tables.sessions.billing_id` - NOT NULL
- `tables.sessions.table_id` - NOT NULL

### Additional NOT NULL Constraints Needed

#### `billing.bills.status`
**Action**: Already NOT NULL ✅

#### `orders.orders.status`
**Action**: Already NOT NULL ✅

#### `pay.payments.method`
**Action**: Already NOT NULL ✅

#### `pay.payments.status`
**Action**: Already NOT NULL ✅

**No additional NOT NULL constraints needed** - existing constraints are sufficient

---

## 2. Foreign Key Constraints

### Already Enforced ✅
- `orders.order_items.order_id` → `orders.orders.order_id`
- `billing.bill_items.bill_id` → `billing.bills.bill_id`
- `inventory.transactions.item_id` → `inventory.items.id`

### Additional Foreign Keys Needed

#### `pay.payments.bill_id` → `billing.bills.bill_id`
**Status**: ⚠️ FK exists but may be broken (shows NULL foreign_table_schema)

**Action**: Recreate FK constraint (see Step 4, Section 3.1)

```sql
ALTER TABLE pay.payments
    DROP CONSTRAINT IF EXISTS payments_bill_id_fkey;

ALTER TABLE pay.payments
    ADD CONSTRAINT payments_bill_id_fkey
    FOREIGN KEY (bill_id) REFERENCES billing.bills(bill_id)
    ON DELETE RESTRICT;
```

---

#### `pay.payment_ledger.bill_id` → `billing.bills.bill_id`
**Status**: ⚠️ FK exists but may be broken (shows NULL foreign_table_schema)

**Action**: Recreate FK constraint (see Step 4, Section 3.2)

```sql
ALTER TABLE pay.payment_ledger
    DROP CONSTRAINT IF EXISTS payment_ledger_bill_id_fkey;

ALTER TABLE pay.payment_ledger
    ADD CONSTRAINT payment_ledger_bill_id_fkey
    FOREIGN KEY (bill_id) REFERENCES billing.bills(bill_id)
    ON DELETE RESTRICT;
```

---

#### `pay.payments.session_id` → `tables.sessions.session_id`
**Status**: ❌ Missing (column needs to be added first)

**Action**: Add FK constraint after adding `session_id` column

```sql
ALTER TABLE pay.payments
    ADD CONSTRAINT payments_session_id_fkey
    FOREIGN KEY (session_id) REFERENCES tables.sessions(session_id)
    ON DELETE RESTRICT;
```

---

#### `pay.payment_ledger.session_id` → `tables.sessions.session_id`
**Status**: ❌ Missing (column needs to be added first)

**Action**: Add FK constraint after adding `session_id` column

```sql
ALTER TABLE pay.payment_ledger
    ADD CONSTRAINT payment_ledger_session_id_fkey
    FOREIGN KEY (session_id) REFERENCES tables.sessions(session_id)
    ON DELETE RESTRICT;
```

---

#### `billing.bills.session_id` → `tables.sessions.session_id`
**Status**: ❌ Missing

**Action**: Add FK constraint

```sql
ALTER TABLE billing.bills
    ADD CONSTRAINT bills_session_id_fkey
    FOREIGN KEY (session_id) REFERENCES tables.sessions(session_id)
    ON DELETE RESTRICT;
```

---

#### `billing.bills.table_id` → `public.tables.table_id`
**Status**: ❌ Missing

**Action**: Add FK constraint

```sql
ALTER TABLE billing.bills
    ADD CONSTRAINT bills_table_id_fkey
    FOREIGN KEY (table_id) REFERENCES public.tables(table_id)
    ON DELETE RESTRICT;
```

---

#### `orders.orders.session_id` → `tables.sessions.session_id`
**Status**: ❌ Missing

**Action**: Add FK constraint

```sql
ALTER TABLE orders.orders
    ADD CONSTRAINT orders_session_id_fkey
    FOREIGN KEY (session_id) REFERENCES tables.sessions(session_id)
    ON DELETE RESTRICT;
```

---

#### `tables.sessions.table_id` → `public.tables.table_id`
**Status**: ❌ Missing

**Action**: Add FK constraint

```sql
ALTER TABLE tables.sessions
    ADD CONSTRAINT sessions_table_id_fkey
    FOREIGN KEY (table_id) REFERENCES public.tables(table_id)
    ON DELETE RESTRICT;
```

---

## 3. Unique Constraints

### Already Enforced ✅
- `users.users.username` - UNIQUE
- `menu.menu_categories.name` - UNIQUE
- `public.shifts.shift_number` - UNIQUE
- `pay.payments(billing_id, external_reference)` - UNIQUE WHERE `external_reference IS NOT NULL`

### Additional Unique Constraints Needed

**No additional unique constraints needed** - existing constraints are sufficient

---

## 4. Check Constraints

### Already Enforced ✅
- `billing.bills.items_total >= 0`
- `billing.bills.time_total >= 0`
- `billing.bills.subtotal >= 0`
- `billing.bills.discounts >= 0`
- `billing.bills.tax >= 0`
- `billing.bills.total_amount >= 0`
- `billing.bills.time_minutes >= 0`
- `orders.order_items.base_price >= 0`
- `orders.order_items.vendor_price >= 0`
- `orders.order_items.line_total >= 0`
- `orders.order_items.quantity > 0`
- `pay.payments.amount_paid >= 0`
- `pay.payments.discount_amount >= 0`
- `pay.payments.tip_amount >= 0`

### Additional Check Constraints Needed

#### `tables.sessions.end_time >= start_time`
**Action**: Add CHECK constraint

```sql
ALTER TABLE tables.sessions
    ADD CONSTRAINT sessions_end_time_after_start_time
    CHECK (end_time IS NULL OR end_time >= start_time);
```

---

#### `billing.bills.settled_at >= created_at`
**Action**: Add CHECK constraint

```sql
ALTER TABLE billing.bills
    ADD CONSTRAINT bills_settled_at_after_created_at
    CHECK (settled_at IS NULL OR settled_at >= created_at);
```

---

#### `public.shifts.closed_at >= opened_at`
**Action**: Add CHECK constraint

```sql
ALTER TABLE public.shifts
    ADD CONSTRAINT shifts_closed_at_after_opened_at
    CHECK (closed_at IS NULL OR closed_at >= opened_at);
```

---

#### `tables.sessions.is_active` and `end_time` consistency
**Action**: Add CHECK constraint

```sql
ALTER TABLE tables.sessions
    ADD CONSTRAINT sessions_active_state_consistency
    CHECK (
        (is_active = true AND end_time IS NULL) OR
        (is_active = false AND end_time IS NOT NULL)
    );
```

**Note**: This may be too strict if business logic allows active sessions with end_time. Verify business rules first.

---

#### `billing.bills.status` and `settled_at` consistency
**Action**: Add CHECK constraint

```sql
ALTER TABLE billing.bills
    ADD CONSTRAINT bills_status_settled_consistency
    CHECK (
        (status::text = 'AwaitingPayment' AND settled_at IS NULL) OR
        (status::text = 'Paid' AND settled_at IS NOT NULL) OR
        (status::text = 'Cancelled')
    );
```

---

## 5. Enum Enforcement

### Already Enforced ✅
- `billing.bills.status` - ENUM type
- `orders.orders.status` - ENUM type
- `orders.order_items.delivery_status` - ENUM type
- `pay.payments.method` - ENUM type
- `pay.payments.status` - ENUM type
- `pay.payment_ledger.status` - ENUM type

**No additional enum enforcement needed** - existing enums are sufficient

---

## 6. Index Strategy

### Already Indexed ✅
- Primary keys on all tables
- Foreign key columns (most)
- Status columns (most)
- Active sessions (`tables.sessions(table_id, is_active) WHERE is_active = true`)

### Additional Indexes Needed

#### `billing.bills.created_at`
**Purpose**: Query bills by date range

```sql
CREATE INDEX IF NOT EXISTS idx_bills_created_at 
ON billing.bills(created_at);
```

---

#### `pay.payments.created_at`
**Purpose**: Query payments by date range

```sql
CREATE INDEX IF NOT EXISTS idx_payments_created_at 
ON pay.payments(created_at);
```

---

#### `tables.sessions.created_at`
**Purpose**: Query sessions by date range

```sql
CREATE INDEX IF NOT EXISTS idx_sessions_created_at 
ON tables.sessions(created_at);
```

---

#### `orders.orders.created_at`
**Purpose**: Query orders by date range

```sql
CREATE INDEX IF NOT EXISTS idx_orders_created_at 
ON orders.orders(created_at);
```

---

## 7. Append-Only Tables (Immutable After Creation)

### Financial Tables (Must Be Append-Only)

#### `pay.payments`
**Action**: Create trigger to prevent updates/deletes

```sql
CREATE OR REPLACE FUNCTION prevent_payment_modification()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'UPDATE' THEN
        -- Only allow status updates for cancellation/refund
        IF OLD.status::text = NEW.status::text AND 
           OLD.amount_paid = NEW.amount_paid AND
           OLD.discount_amount = NEW.discount_amount AND
           OLD.tip_amount = NEW.tip_amount THEN
            RETURN NEW;
        END IF;
        RAISE EXCEPTION 'Payments cannot be modified after creation. Use cancellation/refund instead.';
    ELSIF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Payments cannot be deleted. Use cancellation instead.';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER prevent_payment_modification_trigger
BEFORE UPDATE OR DELETE ON pay.payments
FOR EACH ROW
EXECUTE FUNCTION prevent_payment_modification();
```

**Note**: Allow status changes for cancellation/refund, but prevent amount changes.

---

#### `billing.bills`
**Action**: Create trigger to prevent deletion

```sql
CREATE OR REPLACE FUNCTION prevent_bill_deletion()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Bills cannot be deleted. Use cancellation instead.';
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER prevent_bill_deletion_trigger
BEFORE DELETE ON billing.bills
FOR EACH ROW
EXECUTE FUNCTION prevent_bill_deletion();
```

**Note**: Allow updates (for status changes), but prevent deletion.

---

#### `public.shifts`
**Action**: Create trigger to prevent modification of closed shifts

```sql
CREATE OR REPLACE FUNCTION prevent_closed_shift_modification()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'UPDATE' AND OLD.status = 'closed' THEN
        -- Only allow status changes back to open (reopen)
        IF NEW.status = 'open' AND OLD.status = 'closed' THEN
            RETURN NEW;
        END IF;
        RAISE EXCEPTION 'Closed shifts cannot be modified. Use reopen instead.';
    ELSIF TG_OP = 'DELETE' THEN
        RAISE EXCEPTION 'Shifts cannot be deleted.';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER prevent_closed_shift_modification_trigger
BEFORE UPDATE OR DELETE ON public.shifts
FOR EACH ROW
EXECUTE FUNCTION prevent_closed_shift_modification();
```

---

## 8. Views for Read-Only Data

### 8.1 `billing.bills_view` (Read-Only)
**Purpose**: Provide read-only view of bills with joined data

```sql
CREATE OR REPLACE VIEW billing.bills_view AS
SELECT 
    b.bill_id,
    b.billing_id,
    b.session_id,
    b.table_id,
    b.status,
    b.items_total,
    b.time_total,
    b.subtotal,
    b.discounts,
    b.tax,
    b.total_amount,
    b.time_minutes,
    b.created_at,
    b.settled_at,
    b.table_label,
    b.server_name,
    s.start_time,
    s.end_time,
    s.started_by as server_id,
    t.table_number,
    t.table_name
FROM billing.bills b
JOIN tables.sessions s ON b.session_id = s.session_id
JOIN public.tables t ON b.table_id = t.table_id;
```

---

### 8.2 `pay.payments_view` (Read-Only)
**Purpose**: Provide read-only view of payments with joined data

```sql
CREATE OR REPLACE VIEW pay.payments_view AS
SELECT 
    p.payment_id,
    p.bill_id,
    p.billing_id,
    p.session_id,
    p.amount_paid,
    p.discount_amount,
    p.tip_amount,
    p.method,
    p.status,
    p.external_reference,
    p.created_at,
    p.cancelled_at,
    p.shift_id,
    b.table_label,
    b.total_amount as bill_total,
    s.start_time,
    s.end_time
FROM pay.payments p
JOIN billing.bills b ON p.bill_id = b.bill_id
LEFT JOIN tables.sessions s ON p.session_id = s.session_id;
```

---

## 9. Audit Trail

### 9.1 Enable Audit Logging
**Action**: Use existing `audit.events` table

**Verify audit triggers exist** (from `migrations/01_audit_schema.sql` and `migrations/02_immutability_triggers.sql`)

---

## 10. Execution Order

### Phase 1: Add Foreign Keys (Non-Breaking)
1. Recreate `pay.payments.bill_id` FK
2. Recreate `pay.payment_ledger.bill_id` FK
3. Add `billing.bills.session_id` FK
4. Add `billing.bills.table_id` FK
5. Add `orders.orders.session_id` FK
6. Add `tables.sessions.table_id` FK
7. Add `pay.payments.session_id` FK (after column added)
8. Add `pay.payment_ledger.session_id` FK (after column added)

### Phase 2: Add Check Constraints (Non-Breaking)
1. Add `sessions.end_time >= start_time` constraint
2. Add `bills.settled_at >= created_at` constraint
3. Add `shifts.closed_at >= opened_at` constraint
4. Add `sessions.is_active` and `end_time` consistency constraint
5. Add `bills.status` and `settled_at` consistency constraint

### Phase 3: Add Indexes (Non-Breaking)
1. Add `billing.bills.created_at` index
2. Add `pay.payments.created_at` index
3. Add `tables.sessions.created_at` index
4. Add `orders.orders.created_at` index

### Phase 4: Add Immutability Triggers (Breaking - Requires Code Changes)
1. Add `prevent_payment_modification` trigger
2. Add `prevent_bill_deletion` trigger
3. Add `prevent_closed_shift_modification` trigger

### Phase 5: Create Views (Non-Breaking)
1. Create `billing.bills_view`
2. Create `pay.payments_view`

---

## 11. Schema Lock Statement

**After executing this plan, the database schema is effectively "locked".**

**Future schema changes require:**
1. Formal migration script
2. Code changes to match
3. Testing in staging environment
4. Approval from database architect
5. Deployment during maintenance window

---

## 12. Rollback Plan

### If Hardening Fails
1. **Stop immediately** - Do not proceed with remaining steps
2. **Review error logs** - Identify which constraint/trigger failed
3. **Drop failed constraints/triggers**:
   ```sql
   ALTER TABLE <table> DROP CONSTRAINT IF EXISTS <constraint>;
   DROP TRIGGER IF EXISTS <trigger> ON <table>;
   ```
4. **Fix data issues** - Resolve data that violates constraints
5. **Retry hardening** - After data is fixed

---

## Next Steps

1. **Step 7**: Go/No-Go checkpoint for approval
2. **Step 8**: Execute hardening (after approval)
3. **Step 9**: Final verification

---

**END OF PRODUCTION HARDENING PLAN**

**⚠️ NO HARDENING EXECUTED YET - AWAITING APPROVAL**

