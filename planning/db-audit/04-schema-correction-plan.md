# 04 - Schema Correction Plan

**Status**: PLANNING (NO EXECUTION YET)  
**Date**: 2025-12-23  
**Auditor**: Principal Database Architect

---

## Purpose

This document outlines all proposed schema changes to align the database with API expectations and fix identified issues. **NO CHANGES WILL BE EXECUTED** until Step 7 (Go/No-Go) approval.

---

## Change Categories

1. **Add Missing Columns** - Add columns expected by APIs but missing in DB
2. **Fix Type Mismatches** - Correct data type inconsistencies
3. **Add Missing Foreign Keys** - Add FK constraints for referential integrity
4. **Add Missing Constraints** - Add NOT NULL, CHECK, UNIQUE constraints
5. **Deprecate Legacy Tables** - Mark legacy tables for removal
6. **Rename Columns** - Standardize naming conventions

---

## 1. Add Missing Columns

### 1.1 `billing.bills` Table

| Column | Type | Nullable | Default | Reason | Impact |
|--------|------|----------|---------|--------|--------|
| `server_id` | `varchar(255)` | YES | NULL | Required by `BillResult` DTO | Low - Add nullable column |
| `start_time` | `timestamptz` | YES | NULL | Required by `BillResult` DTO | Low - Add nullable column |
| `end_time` | `timestamptz` | YES | NULL | Required by `BillResult` DTO | Low - Add nullable column |

**Migration Strategy**:
```sql
ALTER TABLE billing.bills
    ADD COLUMN server_id VARCHAR(255),
    ADD COLUMN start_time TIMESTAMPTZ,
    ADD COLUMN end_time TIMESTAMPTZ;

-- Populate from sessions table
UPDATE billing.bills b
SET 
    server_id = s.started_by,
    start_time = s.start_time,
    end_time = s.end_time
FROM tables.sessions s
WHERE b.session_id = s.session_id;
```

**Rollback Strategy**: Drop columns if needed
```sql
ALTER TABLE billing.bills
    DROP COLUMN server_id,
    DROP COLUMN start_time,
    DROP COLUMN end_time;
```

---

### 1.2 `orders.orders` Table

| Column | Type | Nullable | Default | Reason | Impact |
|--------|------|----------|---------|--------|--------|
| `table_id` | `varchar(255)` | YES | NULL | Required by `OrderDto` DTO | Low - Add nullable column |
| `server_id` | `varchar(255)` | YES | NULL | Required by `CreateOrderRequestDto` DTO | Low - Add nullable column |
| `server_name` | `varchar(255)` | YES | NULL | Required by `CreateOrderRequestDto` DTO | Low - Add nullable column |

**Migration Strategy**:
```sql
ALTER TABLE orders.orders
    ADD COLUMN table_id VARCHAR(255),
    ADD COLUMN server_id VARCHAR(255),
    ADD COLUMN server_name VARCHAR(255);

-- Populate from sessions table
UPDATE orders.orders o
SET 
    table_id = (SELECT t.table_number FROM public.tables t JOIN tables.sessions s ON t.table_id = s.table_id WHERE s.session_id = o.session_id),
    server_id = s.started_by,
    server_name = (SELECT server_name FROM tables.sessions WHERE session_id = o.session_id LIMIT 1)
FROM tables.sessions s
WHERE o.session_id = s.session_id;
```

**Rollback Strategy**: Drop columns if needed

---

### 1.3 `pay.payment_ledger` Table

| Column | Type | Nullable | Default | Reason | Impact |
|--------|------|----------|---------|--------|--------|
| `session_id` | `uuid` | YES | NULL | Required by `BillLedgerDto` DTO | Low - Add nullable column |

**Migration Strategy**:
```sql
ALTER TABLE pay.payment_ledger
    ADD COLUMN session_id UUID;

-- Populate from bills table
UPDATE pay.payment_ledger pl
SET session_id = b.session_id
FROM billing.bills b
WHERE pl.billing_id = b.billing_id;
```

**Rollback Strategy**: Drop column if needed

---

### 1.4 `pay.payments` Table

| Column | Type | Nullable | Default | Reason | Impact |
|--------|------|----------|---------|--------|--------|
| `session_id` | `uuid` | YES | NULL | Required by `PaymentDto` DTO | Low - Add nullable column |
| `discount_reason` | `varchar(500)` | YES | NULL | Required by `PaymentDto` DTO | Low - Add nullable column |
| `meta` | `jsonb` | YES | NULL | Required by `PaymentDto` DTO | Low - Add nullable column |
| `created_by` | `varchar(255)` | YES | NULL | Required by `PaymentDto` DTO | Low - Add nullable column |
| `notes` | `text` | YES | NULL | Required by `PaymentDto` DTO | Low - Add nullable column |

**Migration Strategy**:
```sql
ALTER TABLE pay.payments
    ADD COLUMN session_id UUID,
    ADD COLUMN discount_reason VARCHAR(500),
    ADD COLUMN meta JSONB,
    ADD COLUMN created_by VARCHAR(255),
    ADD COLUMN notes TEXT;

-- Populate session_id from bills table
UPDATE pay.payments p
SET session_id = b.session_id
FROM billing.bills b
WHERE p.bill_id = b.bill_id;
```

**Rollback Strategy**: Drop columns if needed

---

### 1.5 `menu.menu_items` Table

| Column | Type | Nullable | Default | Reason | Impact |
|--------|------|----------|---------|--------|--------|
| `group_name` | `varchar(255)` | YES | NULL | Required by `MenuItemDto` DTO | Low - Add nullable column |
| `picture_url` | `varchar(500)` | YES | NULL | Required by `MenuItemDto` DTO | Low - Add nullable column |
| `is_discountable` | `boolean` | NO | `true` | Required by `MenuItemDto` DTO | Low - Add column with default |

**Migration Strategy**:
```sql
ALTER TABLE menu.menu_items
    ADD COLUMN group_name VARCHAR(255),
    ADD COLUMN picture_url VARCHAR(500),
    ADD COLUMN is_discountable BOOLEAN NOT NULL DEFAULT true;
```

**Rollback Strategy**: Drop columns if needed

---

### 1.6 `orders.order_items` Table

| Column | Type | Nullable | Default | Reason | Impact |
|--------|------|----------|---------|--------|--------|
| `combo_id` | `uuid` | YES | NULL | Required by `OrderItemDto` DTO | Low - Add nullable column |

**Note**: `combo_id` may be stored in `modifiers` JSONB. Need to verify if separate column is needed.

**Migration Strategy**:
```sql
ALTER TABLE orders.order_items
    ADD COLUMN combo_id UUID;
```

**Rollback Strategy**: Drop column if needed

---

## 2. Fix Type Mismatches

### 2.1 DTO Type Mismatches (Code Changes Required)

**Issue**: DTOs use `long` for IDs but DB uses `uuid`

**Tables Affected**:
- `orders.orders.order_id` (uuid) vs DTO `Id` (long)
- `orders.order_items.order_item_id` (uuid) vs DTO `Id` (long)
- `orders.order_items.menu_item_id` (uuid) vs DTO `MenuItemId` (long)
- `menu.menu_items.menu_item_id` (uuid) vs DTO `Id` (long)

**Action**: **CODE CHANGE REQUIRED** - Update DTOs to use `Guid` instead of `long`

**Database Action**: None (DB is correct)

---

### 2.2 `public.shifts.opened_by_user_id` Type Mismatch

**Issue**: `shifts.opened_by_user_id` is `integer` but should reference `users.users.user_id` (varchar)

**Current State**:
- `public.shifts.opened_by_user_id` → `integer` (FK to `public.Users.Id`)
- `users.users.user_id` → `varchar` (PK)

**Options**:
1. **Option A**: Change `users.users.user_id` to `integer` (BREAKING CHANGE)
2. **Option B**: Change `shifts.opened_by_user_id` to `varchar` and update FK
3. **Option C**: Create mapping table between `public.Users` and `users.users`

**Recommended**: **Option B** - Change `shifts.opened_by_user_id` to `varchar`

**Migration Strategy**:
```sql
-- Step 1: Add new column
ALTER TABLE public.shifts
    ADD COLUMN opened_by_user_id_new VARCHAR(255);

-- Step 2: Populate from Users table (if mapping exists)
UPDATE public.shifts s
SET opened_by_user_id_new = u.user_id
FROM public.Users u
WHERE s.opened_by_user_id = u.Id;

-- Step 3: Drop old column and rename new column
ALTER TABLE public.shifts
    DROP COLUMN opened_by_user_id,
    RENAME COLUMN opened_by_user_id_new TO opened_by_user_id;

-- Step 4: Add FK constraint
ALTER TABLE public.shifts
    ADD CONSTRAINT shifts_opened_by_user_id_fkey_new
    FOREIGN KEY (opened_by_user_id) REFERENCES users.users(user_id);
```

**Rollback Strategy**: Revert column type and FK constraint

---

### 2.3 `ord.order_items.menu_item_id` Type Mismatch

**Issue**: `ord.order_items.menu_item_id` is `bigint` but should be `uuid`

**Action**: **DEPRECATE `ord` SCHEMA** (see Section 5)

---

## 3. Add Missing Foreign Keys

### 3.1 `pay.payments.bill_id` → `billing.bills.bill_id`

**Issue**: FK constraint exists but shows NULL foreign_table_schema (may be broken)

**Action**: Recreate FK constraint

**Migration Strategy**:
```sql
-- Drop existing FK if broken
ALTER TABLE pay.payments
    DROP CONSTRAINT IF EXISTS payments_bill_id_fkey;

-- Add new FK constraint
ALTER TABLE pay.payments
    ADD CONSTRAINT payments_bill_id_fkey
    FOREIGN KEY (bill_id) REFERENCES billing.bills(bill_id)
    ON DELETE RESTRICT;
```

**Rollback Strategy**: Drop FK constraint

---

### 3.2 `pay.payment_ledger.bill_id` → `billing.bills.bill_id`

**Issue**: FK constraint exists but shows NULL foreign_table_schema (may be broken)

**Action**: Recreate FK constraint

**Migration Strategy**:
```sql
-- Drop existing FK if broken
ALTER TABLE pay.payment_ledger
    DROP CONSTRAINT IF EXISTS payment_ledger_bill_id_fkey;

-- Add new FK constraint
ALTER TABLE pay.payment_ledger
    ADD CONSTRAINT payment_ledger_bill_id_fkey
    FOREIGN KEY (bill_id) REFERENCES billing.bills(bill_id)
    ON DELETE RESTRICT;
```

**Rollback Strategy**: Drop FK constraint

---

### 3.3 `pay.payments.session_id` → `tables.sessions.session_id`

**Action**: Add FK constraint after adding `session_id` column

**Migration Strategy**:
```sql
ALTER TABLE pay.payments
    ADD CONSTRAINT payments_session_id_fkey
    FOREIGN KEY (session_id) REFERENCES tables.sessions(session_id)
    ON DELETE RESTRICT;
```

**Rollback Strategy**: Drop FK constraint

---

### 3.4 `pay.payment_ledger.session_id` → `tables.sessions.session_id`

**Action**: Add FK constraint after adding `session_id` column

**Migration Strategy**:
```sql
ALTER TABLE pay.payment_ledger
    ADD CONSTRAINT payment_ledger_session_id_fkey
    FOREIGN KEY (session_id) REFERENCES tables.sessions(session_id)
    ON DELETE RESTRICT;
```

**Rollback Strategy**: Drop FK constraint

---

## 4. Add Missing Constraints

### 4.1 NOT NULL Constraints

#### `billing.bills.billing_id`
**Action**: Already NOT NULL ✅

#### `billing.bills.session_id`
**Action**: Already NOT NULL ✅

#### `orders.orders.billing_id`
**Action**: Already NOT NULL ✅

#### `orders.orders.session_id`
**Action**: Already NOT NULL ✅

---

### 4.2 CHECK Constraints

#### `billing.bills.total_amount >= 0`
**Action**: Add CHECK constraint

**Migration Strategy**:
```sql
ALTER TABLE billing.bills
    ADD CONSTRAINT bills_total_amount_non_negative
    CHECK (total_amount >= 0);
```

**Rollback Strategy**: Drop constraint

---

#### `pay.payments.amount_paid >= 0`
**Action**: Add CHECK constraint

**Migration Strategy**:
```sql
ALTER TABLE pay.payments
    ADD CONSTRAINT payments_amount_paid_non_negative
    CHECK (amount_paid >= 0);
```

**Rollback Strategy**: Drop constraint

---

#### `tables.sessions.end_time >= start_time`
**Action**: Add CHECK constraint

**Migration Strategy**:
```sql
ALTER TABLE tables.sessions
    ADD CONSTRAINT sessions_end_time_after_start_time
    CHECK (end_time IS NULL OR end_time >= start_time);
```

**Rollback Strategy**: Drop constraint

---

### 4.3 UNIQUE Constraints

**No additional UNIQUE constraints needed** - existing constraints are sufficient

---

## 5. Deprecate Legacy Tables

### 5.1 `public.TableSessions`

**Action**: 
1. Migrate any remaining data to `tables.sessions`
2. Update `BillsController` to use `tables.sessions`
3. Rename table to `public.TableSessions_legacy`
4. Create view `public.TableSessions` that points to `tables.sessions` (for backward compatibility)

**Migration Strategy**:
```sql
-- Step 1: Migrate data (if any unique data exists)
INSERT INTO tables.sessions (session_id, table_id, billing_id, start_time, end_time, is_active, state, accumulated_cost, guest_count, started_by)
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
    ts.server_id
FROM public."TableSessions" ts
LEFT JOIN public.tables t ON t.table_number = ts.table_label
WHERE NOT EXISTS (SELECT 1 FROM tables.sessions s WHERE s.session_id = ts.session_id);

-- Step 2: Rename legacy table
ALTER TABLE public."TableSessions" RENAME TO "TableSessions_legacy";

-- Step 3: Create view for backward compatibility (if needed)
CREATE VIEW public."TableSessions" AS
SELECT 
    s.session_id,
    t.table_number as table_label,
    s.started_by as server_id,
    NULL as server_name, -- Not available in tables.sessions
    s.start_time,
    s.end_time,
    CASE WHEN s.is_active THEN 'active' ELSE 'ended' END as status,
    s.billing_id,
    NULL::jsonb as items, -- Not available in tables.sessions
    NULL::uuid as shift_id -- Not available in tables.sessions
FROM tables.sessions s
JOIN public.tables t ON s.table_id = t.table_id;
```

**Rollback Strategy**: Drop view and rename table back

---

### 5.2 `public.Orders` and `public.OrderItems`

**Action**: 
1. Verify no active data
2. Rename to `public.Orders_legacy` and `public.OrderItems_legacy`
3. Drop after confirmation period

**Migration Strategy**:
```sql
ALTER TABLE public."Orders" RENAME TO "Orders_legacy";
ALTER TABLE public."OrderItems" RENAME TO "OrderItems_legacy";
```

**Rollback Strategy**: Rename back

---

### 5.3 `public.bills` and `public.payments`

**Action**: 
1. Verify no active data
2. Rename to `public.bills_legacy` and `public.payments_legacy`
3. Drop after confirmation period

**Migration Strategy**:
```sql
ALTER TABLE public.bills RENAME TO bills_legacy;
ALTER TABLE public.payments RENAME TO payments_legacy;
```

**Rollback Strategy**: Rename back

---

### 5.4 `public.Users`

**Action**: 
1. Verify mapping between `public.Users` and `users.users`
2. Migrate any unique data
3. Rename to `public.Users_legacy`
4. Update `public.shifts.opened_by_user_id` to reference `users.users.user_id`

**Migration Strategy**:
```sql
-- Step 1: Migrate users (if needed)
INSERT INTO users.users (user_id, username, password_hash, role, is_active, is_deleted, created_at, updated_at)
SELECT 
    u."Id"::text,
    u."Username",
    COALESCE(u.passwordhash, ''),
    u."Role",
    u."IsActive",
    false,
    u."CreatedAt",
    u."CreatedAt"
FROM public."Users" u
WHERE NOT EXISTS (SELECT 1 FROM users.users u2 WHERE u2.user_id = u."Id"::text);

-- Step 2: Rename legacy table
ALTER TABLE public."Users" RENAME TO "Users_legacy";
```

**Rollback Strategy**: Rename back

---

### 5.5 `public.InventoryItems`

**Action**: 
1. Verify no active data
2. Rename to `public.InventoryItems_legacy`
3. Drop after confirmation period

**Migration Strategy**:
```sql
ALTER TABLE public."InventoryItems" RENAME TO "InventoryItems_legacy";
```

**Rollback Strategy**: Rename back

---

### 5.6 `ord` Schema

**Action**: 
1. Verify no active data (already confirmed - 0 rows)
2. Rename schema to `ord_legacy`
3. Drop after confirmation period

**Migration Strategy**:
```sql
ALTER SCHEMA ord RENAME TO ord_legacy;
```

**Rollback Strategy**: Rename back

---

### 5.7 `pay.bill_ledger`

**Action**: 
1. Verify if still used (0 rows currently)
2. If not used, rename to `pay.bill_ledger_legacy`
3. If used, migrate to `pay.payment_ledger` format

**Migration Strategy**:
```sql
-- If not used, rename
ALTER TABLE pay.bill_ledger RENAME TO bill_ledger_legacy;
```

**Rollback Strategy**: Rename back

---

## 6. Execution Order

### Phase 1: Add Missing Columns (Non-Breaking)
1. Add columns to `billing.bills`
2. Add columns to `orders.orders`
3. Add columns to `pay.payment_ledger`
4. Add columns to `pay.payments`
5. Add columns to `menu.menu_items`
6. Add columns to `orders.order_items`

### Phase 2: Fix Foreign Keys (Non-Breaking)
1. Recreate `pay.payments.bill_id` FK
2. Recreate `pay.payment_ledger.bill_id` FK
3. Add `pay.payments.session_id` FK
4. Add `pay.payment_ledger.session_id` FK

### Phase 3: Add Constraints (Non-Breaking)
1. Add CHECK constraints for non-negative amounts
2. Add CHECK constraint for session time ranges

### Phase 4: Fix Type Mismatches (Breaking - Requires Code Changes)
1. Update DTOs to use `Guid` instead of `long`
2. Fix `shifts.opened_by_user_id` type

### Phase 5: Deprecate Legacy Tables (Breaking - Requires Code Changes)
1. Migrate `public.TableSessions` data
2. Update `BillsController` to use `tables.sessions`
3. Rename legacy tables
4. Rename `ord` schema

---

## 7. Risk Assessment

### Low Risk Changes
- Adding nullable columns
- Adding CHECK constraints
- Adding FK constraints (if data is clean)

### Medium Risk Changes
- Populating new columns from existing data
- Recreating FK constraints

### High Risk Changes
- Changing column types
- Renaming/deprecating legacy tables
- Dropping schemas

---

## 8. Testing Strategy

### Pre-Migration Testing
1. Backup database
2. Run all data quality queries
3. Verify no orphaned data
4. Test FK constraints

### Post-Migration Testing
1. Verify all new columns exist
2. Verify FK constraints work
3. Verify CHECK constraints work
4. Test API endpoints
5. Verify data integrity

---

## 9. Rollback Plan

Each change includes a rollback strategy. In case of issues:
1. Stop migration immediately
2. Review error logs
3. Execute rollback SQL for affected changes
4. Restore from backup if necessary

---

## Next Steps

1. **Step 5**: Create data migration plan to fix orphaned data
2. **Step 6**: Create production hardening plan
3. **Step 7**: Go/No-Go checkpoint for approval

---

**END OF SCHEMA CORRECTION PLAN**

**⚠️ NO CHANGES EXECUTED YET - AWAITING APPROVAL**

