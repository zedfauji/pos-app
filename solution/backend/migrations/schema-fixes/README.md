# Schema Fix Migration Scripts

**Purpose**: Fix all 27 schema mismatches identified in the audit  
**Location**: `solution/backend/migrations/schema-fixes/`  
**Execution Order**: Run scripts in numerical order within each phase

## Execution Order

### Phase 1: Type Mismatch Fixes (CRITICAL)
1. `01_fix_bill_ledger_types.sql` - Fix pay.bill_ledger types (text → uuid)
2. `02_fix_order_items_combo_id.sql` - Fix orders.order_items.combo_id (uuid → bigint)
3. `03_fix_bills_table_session_id.sql` - Fix public.bills.table_session_id (integer → uuid)
4. `04_fix_ord_order_items_menu_item_id.sql` - **SKIP** (ord schema deprecated in Phase 2)

### Phase 2: Schema Consolidation (CRITICAL)
5. `05_consolidate_ord_to_orders.sql` - Migrate ord schema to orders schema
6. `06_consolidate_public_bills_to_billing.sql` - Migrate public.bills to billing.bills
7. `07_consolidate_public_payments_to_pay.sql` - Migrate public.payments to pay.payments

### Phase 3: Add Foreign Keys (HIGH)
8. `08_add_fks_orders_orders.sql` - Add FKs to orders.orders
9. `09_add_fks_orders_order_items.sql` - Add FKs to orders.order_items
10. `10_add_fks_table_sessions.sql` - Add FKs to public.TableSessions
11. `11_add_fks_billing_bills.sql` - Add FKs to billing.bills
12. `12_add_fks_pay_payments.sql` - Add FKs to pay.payments
13. `13_add_fks_bill_ledger.sql` - Add FKs to pay.bill_ledger
14. `14_add_fks_public_tables.sql` - Add FKs to public tables

### Phase 4: Remaining Fixes (MEDIUM)
15. `15_fix_users_timestamps.sql` - Fix users.users timestamps
16. `16_audit_legacy_tables.sql` - Audit legacy tables (review before dropping)

## Pre-Migration Checklist

- [ ] **Backup Database** - Full backup before starting
- [ ] **Review Plan** - Read `docs/schema/IMPLEMENTATION_PLAN.md`
- [ ] **Test on Staging** - Run all migrations on staging first
- [ ] **Coordinate Deployments** - Schedule maintenance window
- [ ] **Prepare Rollback** - Have rollback procedure ready

## Running Migrations

### Single Migration
```sql
\i solution/backend/migrations/schema-fixes/01_fix_bill_ledger_types.sql
```

### All Phase 1 Migrations
```bash
# PowerShell
Get-ChildItem solution/backend/migrations/schema-fixes/0[1-3]*.sql | ForEach-Object {
    Write-Host "Running $($_.Name)..."
    psql -d your_database -f $_.FullName
}
```

### Using psql
```bash
psql -d your_database -f solution/backend/migrations/schema-fixes/01_fix_bill_ledger_types.sql
```

## Validation Queries

After each migration, run the validation queries included in the script comments.

### Example Validation
```sql
-- After 01_fix_bill_ledger_types.sql
SELECT COUNT(*) FROM pay.bill_ledger WHERE billing_id IS NULL OR session_id IS NULL; -- Should return 0
SELECT COUNT(*) FROM pay.bill_ledger WHERE billing_id::text !~ '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'; -- Should return 0
```

## Rollback

Each migration script uses transactions (`BEGIN`/`COMMIT`). If a migration fails:

1. **Check Error** - Review the error message
2. **Fix Data** - Fix any data issues identified
3. **Re-run** - Re-run the migration script

If rollback is needed:
1. **Restore Backup** - Restore from pre-migration backup
2. **Review Audit Tables** - Check audit tables for issues
3. **Fix Data** - Fix data issues before re-running

## Audit Tables

Migrations create audit tables to track:
- Orphaned records
- Unmapped data
- Consolidation logs
- Legacy table usage

Review these tables after each migration:
- `audit.orphaned_combo_references`
- `audit.unmapped_bills`
- `audit.orphaned_orders_pre_fk`
- `audit.orphaned_order_items_pre_fk`
- `audit.orphaned_sessions_pre_fk`
- `audit.orphaned_bills_pre_fk`
- `audit.orphaned_payments_pre_fk`
- `audit.orphaned_ledger_pre_fk`
- `audit.schema_consolidation_log`
- `audit.bills_consolidation_log`
- `audit.payments_consolidation_log`
- `audit.legacy_table_usage`

## Notes

- All scripts use transactions for safety
- Scripts validate data before making changes
- Orphaned records are logged, not deleted automatically
- Review audit tables before proceeding to next phase
- Some migrations may require manual data fixes

## Support

For questions or issues:
1. Review `docs/schema/IMPLEMENTATION_PLAN.md`
2. Check `docs/schema/SYSTEM_SCHEMA_MISMATCH_MATRIX.md`
3. Review audit tables for specific issues

