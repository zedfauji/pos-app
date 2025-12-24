# Schema Mismatch Fix Implementation Plan

**Status**: READY FOR EXECUTION  
**Created**: 2025-12-23  
**Total Issues**: 27 (7 CRITICAL, 14 HIGH, 5 MEDIUM, 1 LOW)  
**Estimated Duration**: 4-6 weeks (with testing)

## Executive Summary

This plan addresses all 27 schema mismatches identified in the audit, prioritized by risk level. The plan is organized into 5 phases, with each phase building on the previous one. All changes are backward-compatible where possible, with coordinated deployments.

## Implementation Strategy

### Principles
1. **Fix Type Mismatches First** - Enables foreign key creation
2. **Consolidate Duplicate Schemas** - Eliminates confusion
3. **Add Foreign Keys** - Restores referential integrity
4. **Coordinate Deployments** - APIs must deploy together
5. **Maintain Data Integrity** - No data loss, validate all migrations

### Risk Mitigation
- **Backup Before Each Phase** - Full database backup
- **Test on Staging First** - All migrations tested before production
- **Rollback Plans** - Each phase has rollback procedure
- **Zero-Downtime Where Possible** - Use `ALTER TABLE ... ADD COLUMN` patterns
- **Data Validation** - Verify data integrity after each phase

## Phase 1: Type Mismatch Fixes (CRITICAL) - Week 1

**Goal**: Fix all type mismatches to enable foreign key creation  
**Risk**: HIGH - Type changes require data migration  
**Downtime**: Minimal (can be done with table locks)

### 1.1 Fix pay.bill_ledger Types ✅
**Migration**: `migrations/schema-fixes/01_fix_bill_ledger_types.sql`
- Changes `billing_id` and `session_id` from `text` to `uuid`
- Validates UUID format before migration
- Creates audit trail

**Code Changes**: `PaymentRepository.cs` - Remove text casting

---

### 1.2 Fix orders.order_items.combo_id Type ✅
**Migration**: `migrations/schema-fixes/02_fix_order_items_combo_id.sql`
- Changes `combo_id` from `uuid` to `bigint` (menu.combos is authoritative)
- Maps existing combos, logs orphaned references
- Creates audit table for orphaned combos

**Code Changes**: `OrderRepository.cs:100` - Change `combo_id` from `Guid?` to `long?`

---

### 1.3 Fix public.bills.table_session_id Type ✅
**Migration**: `migrations/schema-fixes/03_fix_bills_table_session_id.sql`
- Changes `table_session_id` from `integer` to `uuid` (as `session_id`)
- Maps via multiple strategies (direct, legacy Id, billing.bills)
- Creates audit table for unmapped bills

**Code Changes**: `ShiftRepository.cs:166` - Use `session_id` instead of `table_session_id`

---

### 1.4 Deprecate ord Schema (Handle ord.order_items.menu_item_id)
**Decision**: **DEPRECATE ord schema** - See Phase 2.1
- If ord must be kept, migrate `menu_item_id` from `bigint` to `uuid`
- **Recommendation**: Deprecate ord schema instead

---

## Phase 2: Schema Consolidation (CRITICAL) - Week 2

**Goal**: Consolidate duplicate schemas and tables  
**Risk**: HIGH - Data migration required  
**Downtime**: Minimal (can migrate data in background)

### 2.1 Consolidate ord → orders Schema
**Migration**: `migrations/schema-fixes/05_consolidate_ord_to_orders.sql`
- Migrate all data from `ord.orders` to `orders.orders`
- Migrate all data from `ord.order_items` to `orders.order_items`
- Handle `menu_item_id` type mismatch during migration
- Recalculate order totals
- Mark ord schema as deprecated

**Code Changes**:
- `TableRepository.cs` - Change all `ord.*` to `orders.*`
- Update API expectation docs

---

### 2.2 Consolidate public.bills → billing.bills
**Migration**: `migrations/schema-fixes/06_consolidate_public_bills_to_billing.sql`
- Migrate all data from `public.bills` to `billing.bills`
- Map columns appropriately
- Update `public.payments` references
- Mark `public.bills` as deprecated

**Code Changes**:
- `ShiftRepository.cs:166` - Change to `billing.bills`
- `TableRepository.cs:441` - Verify uses `billing.bills`

---

### 2.3 Consolidate public.payments → pay.payments
**Migration**: `migrations/schema-fixes/07_consolidate_public_payments_to_pay.sql`
- Migrate all data from `public.payments` to `pay.payments`
- Map payment_method to enum
- Map is_voided to status enum
- Mark `public.payments` as deprecated

**Code Changes**:
- `ShiftRepository.cs:175` - Change to `pay.payments`

---

## Phase 3: Add Foreign Keys (HIGH) - Week 3

**Goal**: Add all missing foreign key constraints  
**Risk**: MEDIUM - May fail if data violates constraints  
**Downtime**: Minimal (FK creation is fast)

### 3.1 Add Foreign Keys to orders.orders ✅
**Migration**: `migrations/schema-fixes/08_add_fks_orders_orders.sql`
- FK: `session_id` → `public.TableSessions.session_id`
- FK: `billing_id` → `billing.bills.billing_id`
- FK: `shift_id` → `public.shifts.shift_id`
- Audits and fixes orphaned records before adding FKs

---

### 3.2 Add Foreign Keys to orders.order_items
**Migration**: `migrations/schema-fixes/09_add_fks_orders_order_items.sql`
- FK: `menu_item_id` → `menu.menu_items.menu_item_id` (composite PK handling)
- FK: `combo_id` → `menu.combos.combo_id` (after Phase 1.2)
- Audits orphaned records

---

### 3.3 Add Foreign Keys to public.TableSessions
**Migration**: `migrations/schema-fixes/10_add_fks_table_sessions.sql`
- FK: `shift_id` → `public.shifts.shift_id`
- Adds index on `shift_id`

---

### 3.4 Add Foreign Keys to billing.bills
**Migration**: `migrations/schema-fixes/11_add_fks_billing_bills.sql`
- FK: `session_id` → `public.TableSessions.session_id`
- FK: `parent_bill_id` → `billing.bills.bill_id` (self-reference)

---

### 3.5 Add Foreign Keys to pay.payments
**Migration**: `migrations/schema-fixes/12_add_fks_pay_payments.sql`
- FK: `billing_id` → `billing.bills.billing_id`
- FK: `shift_id` → `public.shifts.shift_id`
- Note: `bill_id` and `session_id` FKs already exist

---

### 3.6 Add Foreign Keys to pay.bill_ledger
**Migration**: `migrations/schema-fixes/13_add_fks_bill_ledger.sql`
- FK: `billing_id` → `billing.bills.billing_id` (after Phase 1.1)
- FK: `session_id` → `public.TableSessions.session_id` (after Phase 1.1)

---

### 3.7 Add Foreign Keys to public Tables
**Migration**: `migrations/schema-fixes/14_add_fks_public_tables.sql`
- FK: `public.tables.type_id` → `public.table_types.id`
- FK: `public.shifts.opened_by_user_id` → `users.users.user_id`
- FK: `public.shifts.closed_by_user_id` → `users.users.user_id`

---

## Phase 4: Remaining Type Fixes (MEDIUM) - Week 4

**Goal**: Fix remaining type mismatches and inconsistencies  
**Risk**: LOW - Mostly cosmetic improvements

### 4.1 Fix users.users Timestamps
**Migration**: `migrations/schema-fixes/15_fix_users_timestamps.sql`
- Change `created_at` and `updated_at` from `timestamp` to `timestamptz`

---

### 4.2 Audit and Remove Legacy Tables
**Migration**: `migrations/schema-fixes/16_audit_legacy_tables.sql`
- Audit usage of `public.Orders`, `public.OrderItems`, `public.InventoryItems`, `public.Users`
- Create audit table with recommendations
- **DO NOT DROP** automatically - review first

---

## Phase 5: Code Updates and Validation - Week 5-6

**Goal**: Update all code to use canonical schema, remove deprecated references  
**Risk**: MEDIUM - Code changes required

### 5.1 Update TablesApi Code
**Files**:
- `TableRepository.cs` - Change `ord.*` to `orders.*`
- `ShiftRepository.cs` - Change `public.bills` to `billing.bills`, `public.payments` to `pay.payments`
- `BillingRepository.cs` - Verify uses `billing.bills`

### 5.2 Update OrderApi Code
**Files**:
- `OrderRepository.cs` - Fix `combo_id` type handling (`Guid?` → `long?`)
- Verify all queries use `orders.*` schema

### 5.3 Update PaymentApi Code
**Files**:
- `PaymentRepository.cs` - Remove text casting for `bill_ledger` columns

### 5.4 Remove Deprecated Schema References
**Action**: Search and replace:
- `ord.orders` → `orders.orders`
- `ord.order_items` → `orders.order_items`
- `public.bills` → `billing.bills`
- `public.payments` → `pay.payments`

---

## Testing Strategy

### Pre-Migration Testing
1. **Backup Database** - Full backup before each phase
2. **Data Validation** - Count records in source and target schemas
3. **Referential Integrity Check** - Verify all relationships valid
4. **Application Testing** - Run smoke tests on staging

### Post-Migration Testing
1. **Data Integrity** - Verify all data migrated correctly
2. **Foreign Key Validation** - Test FK constraints work
3. **Application Functionality** - Full regression test
4. **Performance Testing** - Verify queries still perform

### Rollback Testing
1. **Backup Restoration** - Test backup restore procedure
2. **Rollback Scripts** - Test reverse migration scripts
3. **Application Rollback** - Test code rollback procedure

## Deployment Plan

### Staging Deployment
1. **Week 1**: Deploy Phase 1 (type fixes) to staging
2. **Week 2**: Deploy Phase 2 (schema consolidation) to staging
3. **Week 3**: Deploy Phase 3 (foreign keys) to staging
4. **Week 4**: Deploy Phase 4 (remaining fixes) to staging
5. **Week 5-6**: Deploy Phase 5 (code updates) to staging

### Production Deployment
1. **Maintenance Window**: Schedule 4-hour window for each phase
2. **Backup**: Full backup before each phase
3. **Deploy**: Run migration scripts
4. **Validate**: Run validation queries
5. **Deploy Code**: Deploy updated API code
6. **Smoke Test**: Verify critical paths work
7. **Monitor**: Watch for errors for 24 hours

### Rollback Procedure
1. **Stop APIs** - Prevent new writes
2. **Restore Backup** - Restore from pre-migration backup
3. **Revert Code** - Deploy previous code version
4. **Validate** - Verify system operational
5. **Investigate** - Determine cause of failure

## Success Criteria

### Phase 1 Success
- ✅ All type mismatches fixed
- ✅ No data loss
- ✅ All validation queries pass

### Phase 2 Success
- ✅ All duplicate schemas consolidated
- ✅ No data loss
- ✅ All code references updated

### Phase 3 Success
- ✅ All foreign keys added
- ✅ No orphaned records
- ✅ Referential integrity restored

### Phase 4 Success
- ✅ All remaining type fixes applied
- ✅ Legacy tables audited/removed

### Phase 5 Success
- ✅ All code uses canonical schema
- ✅ No deprecated schema references
- ✅ All tests pass

## Risk Assessment

### High Risk Items
1. **Data Migration** - Risk of data loss
   - **Mitigation**: Full backups, validation queries, rollback plans

2. **Type Conversions** - Risk of conversion failures
   - **Mitigation**: Validate data format before conversion, handle NULLs

3. **Foreign Key Failures** - Risk of orphaned records
   - **Mitigation**: Audit and fix orphaned records before adding FKs

4. **Code Deployment** - Risk of breaking changes
   - **Mitigation**: Coordinate deployments, test on staging first

### Medium Risk Items
1. **Performance Impact** - FK constraints may slow writes
   - **Mitigation**: Monitor performance, add indexes where needed

2. **Downtime** - Migration may require brief downtime
   - **Mitigation**: Schedule maintenance windows, minimize downtime

## Timeline Summary

| Phase | Duration | Risk | Dependencies | Migration Scripts |
|-------|----------|------|--------------|-------------------|
| Phase 1: Type Fixes | 1 week | HIGH | None | 01-04 |
| Phase 2: Schema Consolidation | 1 week | HIGH | Phase 1 | 05-07 |
| Phase 3: Foreign Keys | 1 week | MEDIUM | Phase 1, 2 | 08-14 |
| Phase 4: Remaining Fixes | 1 week | LOW | Phase 3 | 15-16 |
| Phase 5: Code Updates | 2 weeks | MEDIUM | Phase 2, 3 | N/A (code changes) |
| **Total** | **6 weeks** | | | |

## Migration Scripts Location

All migration scripts are in: `solution/backend/migrations/schema-fixes/`

**Created Scripts**:
- ✅ `01_fix_bill_ledger_types.sql`
- ✅ `02_fix_order_items_combo_id.sql`
- ✅ `03_fix_bills_table_session_id.sql`
- ✅ `08_add_fks_orders_orders.sql`

**Remaining Scripts** (to be created):
- `05_consolidate_ord_to_orders.sql`
- `06_consolidate_public_bills_to_billing.sql`
- `07_consolidate_public_payments_to_pay.sql`
- `09_add_fks_orders_order_items.sql`
- `10_add_fks_table_sessions.sql`
- `11_add_fks_billing_bills.sql`
- `12_add_fks_pay_payments.sql`
- `13_add_fks_bill_ledger.sql`
- `14_add_fks_public_tables.sql`
- `15_fix_users_timestamps.sql`
- `16_audit_legacy_tables.sql`

## Next Steps

1. **Review Plan** - Get approval from stakeholders
2. **Complete Migration Scripts** - Generate remaining SQL migration files
3. **Set Up Staging** - Prepare staging environment
4. **Schedule Maintenance Windows** - Coordinate with operations
5. **Begin Phase 1** - Start with type mismatch fixes

---

**This plan is ready for execution. All migrations are designed to be reversible and data-safe.**
