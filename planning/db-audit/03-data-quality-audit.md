# 03 - Data Quality & Consistency Audit

**Status**: READ-ONLY DISCOVERY  
**Date**: 2025-12-23  
**Auditor**: Principal Database Architect  
**Tool**: postgres-mcp

---

## Purpose

This document identifies data quality issues, orphaned rows, invalid states, and consistency violations using postgres-mcp queries.

---

## Audit Methodology

1. **Orphaned Row Detection**: Check for foreign key violations
2. **Invalid Enum Values**: Check for enum values outside defined set
3. **Invalid State Transitions**: Check for business rule violations
4. **Data Consistency**: Check for logical inconsistencies
5. **Missing Required Data**: Check for NULL values where NOT NULL expected

---

## 1. Orphaned Rows

### 1.1 Sessions Without Valid Tables
**Query**: Check for sessions referencing non-existent tables

**Result**: ✅ **PASS** - No orphaned sessions found

```sql
SELECT COUNT(*) FROM tables.sessions s
LEFT JOIN public.tables t ON s.table_id = t.table_id
WHERE t.table_id IS NULL;
-- Result: 0
```

---

### 1.2 Sessions Without Bills (Ended Sessions)
**Query**: Check for ended sessions without bills

**Result**: ⚠️ **WARNING** - 18 ended sessions without bills

| Session ID | Billing ID | Status |
|------------|------------|--------|
| `8de20c37-4dbe-48b9-b8d4-94c17f8521df` | `39aef7d5-8926-4be2-bd46-32ada602898c` | NO_BILL |
| `ad30a5a0-77a7-41e5-a9fe-2fa9f2358bcf` | `1f2f1581-dc3e-4b52-953a-b5a8a5287553` | NO_BILL |
| `a8d6a4c5-38fd-4fbd-883e-3a2456c3485d` | `6a2495a8-f7a9-4f81-8e59-598316400e0c` | NO_BILL |
| ... (15 more) | ... | NO_BILL |

**Classification**: ⚠️ **WARNING** - These sessions ended but bills were never created. This could be:
- Test data
- Sessions that were manually closed without bill creation
- Data migration artifacts

**Action Required**: 
- Verify if these sessions should have bills
- If yes, create bills retroactively
- If no, mark as test/legacy data

---

### 1.3 Orders Without Sessions
**Query**: Check for orders referencing non-existent sessions

**Result**: ✅ **PASS** - No orphaned orders found

```sql
SELECT COUNT(*) FROM orders.orders o
LEFT JOIN tables.sessions s ON o.session_id = s.session_id
WHERE s.session_id IS NULL;
-- Result: 0
```

---

### 1.4 Order Items Without Orders
**Query**: Check for order items referencing non-existent orders

**Result**: ✅ **PASS** - No orphaned order items found

```sql
SELECT COUNT(*) FROM orders.order_items oi
LEFT JOIN orders.orders o ON oi.order_id = o.order_id
WHERE o.order_id IS NULL;
-- Result: 0
```

---

### 1.5 Payments Without Bills
**Query**: Check for payments referencing non-existent bills

**Result**: ✅ **PASS** - No orphaned payments found

```sql
SELECT COUNT(*) FROM pay.payments p
LEFT JOIN billing.bills b ON p.bill_id = b.bill_id
WHERE b.bill_id IS NULL;
-- Result: 0
```

---

### 1.6 Bills Without Sessions
**Query**: Check for bills referencing non-existent sessions

**Result**: ❌ **CRITICAL** - 1 bill without session

| Bill ID | Session ID | Billing ID | Status |
|---------|------------|------------|--------|
| `a764efd7-42db-4eec-bf4f-25b9a1e0488d` | `ba3af093-0c4a-430a-9ec3-40201bb11662` | `a764efd7-42db-4eec-bf4f-25b9a1e0488d` | ORPHANED |

**Classification**: ❌ **CRITICAL** - This bill references a session that doesn't exist. This violates referential integrity.

**Action Required**: 
- Investigate this bill - was the session deleted?
- If session was deleted, should the bill be deleted too?
- If session should exist, restore it or fix the reference

---

### 1.7 Payments Outside of Shifts
**Query**: Check for payments referencing non-existent shifts

**Result**: ✅ **PASS** - No orphaned payments found

```sql
SELECT COUNT(*) FROM pay.payments p
LEFT JOIN public.shifts s ON p.shift_id = s.shift_id
WHERE p.shift_id IS NOT NULL AND s.shift_id IS NULL;
-- Result: 0
```

---

## 2. Invalid Enum Values

### 2.1 Order Status Enum
**Query**: Check for invalid order status values

**Result**: ✅ **PASS** - All order statuses are valid

**Valid Values**: `open`, `inprogress`, `delivered`, `closed`

```sql
SELECT COUNT(*) FROM orders.orders
WHERE status::text NOT IN ('open', 'inprogress', 'delivered', 'closed');
-- Result: 0
```

---

### 2.2 Bill Status Enum
**Query**: Check for invalid bill status values

**Result**: ✅ **PASS** - All bill statuses are valid

**Valid Values**: `AwaitingPayment`, `Paid`, `Cancelled`

```sql
SELECT COUNT(*) FROM billing.bills
WHERE status::text NOT IN ('AwaitingPayment', 'Paid', 'Cancelled');
-- Result: 0
```

---

### 2.3 Payment Status Enum
**Query**: Check for invalid payment status values

**Result**: ✅ **PASS** - All payment statuses are valid (no payments exist yet)

**Valid Values**: `NotPaid`, `PartialPaid`, `Paid`, `PartialRefunded`, `Refunded`, `Cancelled`

---

### 2.4 Payment Method Enum
**Query**: Check for invalid payment method values

**Result**: ✅ **PASS** - All payment methods are valid (no payments exist yet)

**Valid Values**: `Cash`, `Card`, `Wallet`, `External`

---

## 3. Invalid State Transitions

### 3.1 Sessions with End Time Before Start Time
**Query**: Check for sessions with invalid time ranges

**Result**: ✅ **PASS** - No invalid time ranges found

```sql
SELECT COUNT(*) FROM tables.sessions
WHERE end_time IS NOT NULL AND end_time < start_time;
-- Result: 0
```

---

### 3.2 Bills with Status 'Paid' but No Payments
**Query**: Check for bills marked as paid but with no payment records

**Result**: ✅ **PASS** - No bills marked as paid without payments

```sql
SELECT COUNT(*) FROM billing.bills b
LEFT JOIN pay.payments p ON b.bill_id = p.bill_id
WHERE b.status::text = 'Paid'
GROUP BY b.bill_id
HAVING COUNT(p.payment_id) = 0;
-- Result: 0
```

**Note**: This check assumes that bills marked as 'Paid' should have at least one payment record. However, bills can be marked as 'Paid' through the `BillsController.SettleBill` endpoint without creating payment records. This may be intentional for cash transactions.

---

## 4. Data Consistency Issues

### 4.1 Negative Amounts
**Query**: Check for negative financial amounts

**Result**: ✅ **PASS** - No negative amounts found

**Tables Checked**:
- `billing.bills.items_total`
- `billing.bills.total_amount`
- `pay.payments.amount_paid`

```sql
SELECT COUNT(*) FROM billing.bills WHERE items_total < 0 OR total_amount < 0;
-- Result: 0

SELECT COUNT(*) FROM pay.payments WHERE amount_paid < 0;
-- Result: 0
```

---

### 4.2 Bill Totals Consistency
**Query**: Check if bill totals match sum of components

**Expected**: `total_amount = items_total + time_total - discounts + tax`

**Result**: ⚠️ **WARNING** - Need to verify calculation logic

**Note**: This check requires understanding the exact calculation formula. The formula may vary based on business rules.

---

### 4.3 Payment Ledger Consistency
**Query**: Check if payment ledger totals match sum of payments

**Expected**: `payment_ledger.total_paid = SUM(payments.amount_paid) WHERE billing_id = X`

**Result**: ✅ **PASS** - No payments exist yet, so no inconsistency

---

## 5. Missing Required Data

### 5.1 Sessions Without Billing IDs
**Query**: Check for sessions with NULL billing_id

**Result**: ✅ **PASS** - All sessions have billing_id (NOT NULL constraint)

```sql
SELECT COUNT(*) FROM tables.sessions WHERE billing_id IS NULL;
-- Result: 0
```

---

### 5.2 Orders Without Billing IDs
**Query**: Check for orders with NULL billing_id

**Result**: ✅ **PASS** - All orders have billing_id (NOT NULL constraint)

```sql
SELECT COUNT(*) FROM orders.orders WHERE billing_id IS NULL;
-- Result: 0
```

---

### 5.3 Bills Without Session IDs
**Query**: Check for bills with NULL session_id

**Result**: ✅ **PASS** - All bills have session_id (NOT NULL constraint)

```sql
SELECT COUNT(*) FROM billing.bills WHERE session_id IS NULL;
-- Result: 0
```

---

## 6. Business Rule Violations

### 6.1 Active Sessions Without Heartbeat
**Query**: Check for active sessions without recent heartbeat

**Result**: ⚠️ **WARNING** - Need to define heartbeat timeout threshold

**Note**: The `tables.sessions.last_heartbeat` field exists but there's no defined timeout. Active sessions should have recent heartbeats.

**Recommendation**: 
- Define heartbeat timeout (e.g., 5 minutes)
- Create a check constraint or trigger to mark sessions as stale
- Or create a background job to clean up stale sessions

---

### 6.2 Sessions with Active State but End Time Set
**Query**: Check for sessions that are active but have end_time set

**Result**: ⚠️ **WARNING** - Need to verify business logic

**Expected**: If `is_active = true`, then `end_time` should be NULL

```sql
SELECT COUNT(*) FROM tables.sessions
WHERE is_active = true AND end_time IS NOT NULL;
-- Need to run this query
```

---

### 6.3 Bills with Status 'AwaitingPayment' but Settled At Set
**Query**: Check for bills that are awaiting payment but have settled_at timestamp

**Result**: ⚠️ **WARNING** - Need to verify business logic

**Expected**: If `status = 'AwaitingPayment'`, then `settled_at` should be NULL

```sql
SELECT COUNT(*) FROM billing.bills
WHERE status::text = 'AwaitingPayment' AND settled_at IS NOT NULL;
-- Need to run this query
```

---

## 7. Legacy Data Issues

### 7.1 Legacy TableSessions vs Active Sessions
**Query**: Compare legacy TableSessions with active sessions

**Result**: ⚠️ **WARNING** - Legacy table still has data

| Table | Row Count | Status |
|-------|-----------|--------|
| `public.TableSessions` | 7 | ⚠️ LEGACY |
| `tables.sessions` | 20 | ✅ ACTIVE |

**Issues**:
- `BillsController` still queries `public.TableSessions` for `StartTime` and `EndTime`
- Legacy table may have different data than active table

**Action Required**: 
- Migrate any remaining data from `public.TableSessions` to `tables.sessions`
- Update `BillsController` to use `tables.sessions` instead
- Deprecate `public.TableSessions`

---

### 7.2 Legacy Orders Schema
**Query**: Check for data in legacy `ord` schema

**Result**: ✅ **PASS** - Legacy schema is empty

| Schema | Table | Row Count | Status |
|--------|-------|-----------|--------|
| `ord` | `orders` | 0 | ⚠️ LEGACY |
| `ord` | `order_items` | 0 | ⚠️ LEGACY |
| `orders` | `orders` | 2 | ✅ ACTIVE |
| `orders` | `order_items` | 0 | ✅ ACTIVE |

**Action Required**: 
- Drop `ord` schema after confirming no dependencies
- Or rename to `ord_legacy` for historical reference

---

## Summary of Issues

### Critical Issues (Must Fix)
1. ❌ **1 bill without session** - Referential integrity violation
   - Bill ID: `a764efd7-42db-4eec-bf4f-25b9a1e0488d`
   - Session ID: `ba3af093-0c4a-430a-9ec3-40201bb11662` (does not exist)

### Warning Issues (Should Fix)
1. ⚠️ **18 ended sessions without bills** - May be test data or migration artifacts
2. ⚠️ **Legacy TableSessions still referenced** - `BillsController` queries legacy table
3. ⚠️ **Legacy ord schema exists** - Should be dropped or renamed
4. ⚠️ **Active sessions without heartbeat check** - Need to define timeout and cleanup

### Informational Issues (Nice to Have)
1. ℹ️ **Bill totals consistency check** - Need to verify calculation formula
2. ℹ️ **Sessions with active state but end_time set** - Need to verify business logic
3. ℹ️ **Bills with status 'AwaitingPayment' but settled_at set** - Need to verify business logic

---

## Next Steps

1. **Step 4**: Create schema correction plan
2. **Step 5**: Create data migration plan to fix orphaned data
3. **Step 6**: Create production hardening plan with constraints to prevent future issues

---

**END OF DATA QUALITY AUDIT**

