# Schema Fix Quick Reference

**Last Updated**: 2025-12-23  
**Total Issues**: 27 (7 CRITICAL, 14 HIGH, 5 MEDIUM, 1 LOW)

## Critical Issues (Fix First)

| # | Issue | Migration | Risk |
|---|-------|-----------|------|
| 1 | `pay.bill_ledger.billing_id` text → uuid | `01_fix_bill_ledger_types.sql` | HIGH |
| 2 | `pay.bill_ledger.session_id` text → uuid | `01_fix_bill_ledger_types.sql` | HIGH |
| 3 | `orders.order_items.combo_id` uuid → bigint | `02_fix_order_items_combo_id.sql` | HIGH |
| 4 | `public.bills.table_session_id` integer → uuid | `03_fix_bills_table_session_id.sql` | HIGH |
| 5 | `ord` schema duplicate | `05_consolidate_ord_to_orders.sql` | HIGH |
| 6 | `public.bills` duplicate | `06_consolidate_public_bills_to_billing.sql` | HIGH |
| 7 | `public.payments` duplicate | `07_consolidate_public_payments_to_pay.sql` | HIGH |

## High Priority Issues (Fix After Types)

| # | Issue | Migration | Risk |
|---|-------|-----------|------|
| 8-21 | Missing Foreign Keys (14 total) | `08-14_add_fks_*.sql` | MEDIUM |

## Medium Priority Issues

| # | Issue | Migration | Risk |
|---|-------|-----------|------|
| 22 | `public.shifts.opened_by_user_id` type | `14_add_fks_public_tables.sql` | LOW |
| 23 | `users.users.user_id` vs `public.Users.Id` | Audit only | LOW |
| 24 | `public.tables.type_id` missing FK | `14_add_fks_public_tables.sql` | LOW |
| 25 | `public.payments` duplicate | `07_consolidate_public_payments_to_pay.sql` | MEDIUM |

## Low Priority Issues

| # | Issue | Migration | Risk |
|---|-------|-----------|------|
| 26 | `users.users` timestamps | `15_fix_users_timestamps.sql` | LOW |
| 27 | Legacy PascalCase tables | `16_audit_legacy_tables.sql` | LOW |

## Execution Phases

### Phase 1: Type Fixes (Week 1)
```bash
01_fix_bill_ledger_types.sql
02_fix_order_items_combo_id.sql
03_fix_bills_table_session_id.sql
```

### Phase 2: Schema Consolidation (Week 2)
```bash
05_consolidate_ord_to_orders.sql
06_consolidate_public_bills_to_billing.sql
07_consolidate_public_payments_to_pay.sql
```

### Phase 3: Foreign Keys (Week 3)
```bash
08_add_fks_orders_orders.sql
09_add_fks_orders_order_items.sql
10_add_fks_table_sessions.sql
11_add_fks_billing_bills.sql
12_add_fks_pay_payments.sql
13_add_fks_bill_ledger.sql
14_add_fks_public_tables.sql
```

### Phase 4: Remaining Fixes (Week 4)
```bash
15_fix_users_timestamps.sql
16_audit_legacy_tables.sql
```

### Phase 5: Code Updates (Week 5-6)
- Update all API code to use canonical schema
- Remove deprecated schema references

## Quick Validation

After each phase, run:

```sql
-- Check foreign keys
SELECT 
    tc.table_schema,
    tc.table_name,
    tc.constraint_name,
    kcu.column_name,
    ccu.table_schema AS foreign_table_schema,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu 
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage ccu 
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
ORDER BY tc.table_schema, tc.table_name;

-- Check for orphaned records
SELECT 'orders.orders' as table_name, COUNT(*) as orphaned_count
FROM orders.orders o
LEFT JOIN public."TableSessions" ts ON o.session_id = ts.session_id
WHERE o.session_id IS NOT NULL AND ts.session_id IS NULL
UNION ALL
SELECT 'orders.order_items' as table_name, COUNT(*) as orphaned_count
FROM orders.order_items oi
LEFT JOIN menu.menu_items mi ON oi.menu_item_id = mi.menu_item_id
WHERE oi.menu_item_id IS NOT NULL AND mi.menu_item_id IS NULL;
```

## Key Decisions

1. **ord schema** → Deprecate, migrate to `orders` schema
2. **public.bills** → Deprecate, migrate to `billing.bills`
3. **public.payments** → Deprecate, migrate to `pay.payments`
4. **combo_id type** → Change to `bigint` (menu.combos is authoritative)
5. **menu_item_id in ord** → Deprecate ord schema (handles this)

## Files Reference

- **Implementation Plan**: `docs/schema/IMPLEMENTATION_PLAN.md`
- **Mismatch Matrix**: `docs/schema/SYSTEM_SCHEMA_MISMATCH_MATRIX.md`
- **Canonical Schema**: `docs/schema/CANONICAL_DB_SCHEMA.md`
- **Migration Scripts**: `solution/backend/migrations/schema-fixes/`
- **Migration README**: `solution/backend/migrations/schema-fixes/README.md`

