# System Schema Mismatch Matrix

**Audit Date**: 2025-12-23  
**Status**: CRITICAL - Multiple schema violations detected

## Mismatch Categories

- **CRITICAL**: Data integrity risk, will cause runtime failures
- **HIGH**: Data integrity risk, may cause silent data corruption
- **MEDIUM**: Potential issues, may cause query failures
- **LOW**: Minor inconsistencies, unlikely to cause immediate issues

## Mismatch Matrix

### CRITICAL MISMATCHES

#### 1. Type Mismatch: ord.order_items.menu_item_id
- **API**: TablesApi, OrderApi
- **File**: `TableRepository.cs:340`, `OrderRepository.cs:284`
- **Expected**: `uuid` (to match menu.menu_items.menu_item_id)
- **Actual**: `bigint`
- **Impact**: Cannot JOIN ord.order_items with menu.menu_items. Code explicitly avoids this join.
- **Location**: `ord.order_items.menu_item_id` (bigint) vs `menu.menu_items.menu_item_id` (uuid)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Migration to change ord.order_items.menu_item_id to uuid, or change menu.menu_items to bigint (not recommended)

#### 2. Type Mismatch: orders.order_items.combo_id
- **API**: OrderApi
- **File**: `OrderRepository.cs:100`
- **Expected**: `uuid` (code uses Guid)
- **Actual**: `bigint` (menu.combos.combo_id is bigint)
- **Impact**: Cannot create foreign key constraint. Type casting may fail.
- **Location**: `orders.order_items.combo_id` (uuid) vs `menu.combos.combo_id` (bigint)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Align types - either change orders.order_items.combo_id to bigint or menu.combos.combo_id to uuid

#### 3. Schema Duplication: Two Orders Schemas
- **API**: All APIs
- **Schemas**: `ord` and `orders`
- **Issue**: Both contain `orders` and `order_items` tables with different structures
- **Impact**: Code confusion, data inconsistency, unclear which schema is authoritative
- **Location**: 
  - `ord.orders` - Simple structure (session_id, table_label, status, is_deleted)
  - `orders.orders` - Complex structure (billing_id, subtotal, discount, tax, total, profit_total, delivery_status)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Consolidate to single schema, migrate data, update all code references

#### 4. Schema Duplication: Two Bills Tables
- **API**: TablesApi, PaymentApi
- **Tables**: `public.bills` and `billing.bills`
- **Issue**: Different structures, different purposes, code uses both
- **Impact**: Data written to wrong table, queries fail, financial discrepancies
- **Location**:
  - `public.bills` - Used by ShiftRepository (table_session_id as integer)
  - `billing.bills` - Used by TableRepository.EndSessionAsync (session_id as uuid)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Consolidate to single table, clarify purpose, update all code references

#### 5. Type Mismatch: public.bills.table_session_id
- **API**: TablesApi (ShiftRepository)
- **File**: `ShiftRepository.cs:166`
- **Expected**: `uuid` (to match public.TableSessions.session_id)
- **Actual**: `integer`
- **Impact**: Foreign key constraint cannot be created. References wrong column (TableSessions.Id doesn't exist as integer).
- **Location**: `public.bills.table_session_id` (integer) vs `public.TableSessions.session_id` (uuid)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Change public.bills.table_session_id to uuid, or create integer Id column in TableSessions (not recommended)

#### 6. Type Mismatch: pay.bill_ledger.billing_id
- **API**: PaymentApi
- **File**: `PaymentRepository.cs:66,102`
- **Expected**: `uuid` (to match billing.bills.billing_id)
- **Actual**: `text`
- **Impact**: Cannot create foreign key constraint. Type casting required in queries.
- **Location**: `pay.bill_ledger.billing_id` (text) vs `billing.bills.billing_id` (uuid)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Change pay.bill_ledger.billing_id to uuid

#### 7. Type Mismatch: pay.bill_ledger.session_id
- **API**: PaymentApi
- **File**: `PaymentRepository.cs:102,115`
- **Expected**: `uuid` (to match public.TableSessions.session_id)
- **Actual**: `text`
- **Impact**: Cannot create foreign key constraint. Type casting required.
- **Location**: `pay.bill_ledger.session_id` (text) vs `public.TableSessions.session_id` (uuid)
- **Risk Level**: **CRITICAL**
- **Fix Required**: Change pay.bill_ledger.session_id to uuid

### HIGH RISK MISMATCHES

#### 8. Missing FK: ord.orders.session_id
- **API**: TablesApi, OrderApi
- **File**: `TableRepository.cs:72,340`, `OrderRepository.cs:69`
- **Expected**: Foreign key to `public.TableSessions.session_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned orders if session deleted, no referential integrity
- **Location**: `ord.orders.session_id` (uuid) → `public.TableSessions.session_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 9. Missing FK: ord.orders.shift_id
- **API**: TablesApi, OrderApi
- **File**: `OrderRepository.cs:69,87`
- **Expected**: Foreign key to `public.shifts.shift_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned orders if shift deleted, audit trail broken
- **Location**: `ord.orders.shift_id` (uuid) → `public.shifts.shift_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 10. Missing FK: orders.orders.session_id
- **API**: OrderApi
- **File**: `OrderRepository.cs:69,171,190`
- **Expected**: Foreign key to `public.TableSessions.session_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned orders, no referential integrity
- **Location**: `orders.orders.session_id` (uuid) → `public.TableSessions.session_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 11. Missing FK: orders.orders.billing_id
- **API**: OrderApi
- **File**: `OrderRepository.cs:45,54,69,76`
- **Expected**: Foreign key to `billing.bills.billing_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned orders, billing reconciliation failures
- **Location**: `orders.orders.billing_id` (uuid) → `billing.bills.billing_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 12. Missing FK: orders.orders.shift_id
- **API**: OrderApi
- **File**: `OrderRepository.cs:69,87`
- **Expected**: Foreign key to `public.shifts.shift_id`
- **Actual**: No foreign key constraint
- **Impact**: Audit trail broken, shift closure validation fails
- **Location**: `orders.orders.shift_id` (uuid) → `public.shifts.shift_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 13. Missing FK: orders.order_items.menu_item_id
- **API**: OrderApi
- **File**: `OrderRepository.cs:92,99,284`
- **Expected**: Foreign key to `menu.menu_items.menu_item_id`
- **Actual**: No foreign key constraint (type matches, but no FK)
- **Impact**: Orphaned order items if menu item deleted
- **Location**: `orders.order_items.menu_item_id` (uuid) → `menu.menu_items.menu_item_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint (note: menu_items has composite PK, need to handle versioning)

#### 14. Missing FK: orders.order_items.combo_id
- **API**: OrderApi
- **File**: `OrderRepository.cs:92,100`
- **Expected**: Foreign key to `menu.combos.combo_id`
- **Actual**: No foreign key constraint (type mismatch prevents FK)
- **Impact**: Orphaned combo references, type casting failures
- **Location**: `orders.order_items.combo_id` (uuid) → `menu.combos.combo_id` (bigint) - **TYPE MISMATCH**
- **Risk Level**: **HIGH**
- **Fix Required**: Fix type mismatch first, then add foreign key

#### 15. Missing FK: public.TableSessions.shift_id
- **API**: TablesApi
- **File**: `TableRepository.cs:120`, `ShiftRepository.cs:157`
- **Expected**: Foreign key to `public.shifts.shift_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned sessions if shift deleted, shift closure validation unreliable
- **Location**: `public.TableSessions.shift_id` (uuid) → `public.shifts.shift_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 16. Missing FK: public.bills.shift_id
- **API**: TablesApi (ShiftRepository)
- **File**: `ShiftRepository.cs:166`
- **Expected**: Foreign key to `public.shifts.shift_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned bills, shift closure validation fails
- **Location**: `public.bills.shift_id` (uuid) → `public.shifts.shift_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 17. Missing FK: public.payments.shift_id
- **API**: TablesApi (ShiftRepository)
- **File**: `ShiftRepository.cs:175`
- **Expected**: Foreign key to `public.shifts.shift_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned payments, cash reconciliation failures
- **Location**: `public.payments.shift_id` (uuid) → `public.shifts.shift_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 18. Missing FK: pay.payments.billing_id
- **API**: PaymentApi
- **File**: `PaymentRepository.cs:35,46,102,115`
- **Expected**: Foreign key to `billing.bills.billing_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned payments, billing reconciliation failures
- **Location**: `pay.payments.billing_id` (uuid) → `billing.bills.billing_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 19. Missing FK: pay.payments.shift_id
- **API**: PaymentApi
- **File**: `PaymentRepository.cs:35,55`
- **Expected**: Foreign key to `public.shifts.shift_id`
- **Actual**: No foreign key constraint
- **Impact**: Audit trail broken, shift closure validation fails
- **Location**: `pay.payments.shift_id` (uuid) → `public.shifts.shift_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 20. Missing FK: billing.bills.session_id
- **API**: TablesApi, OrderApi
- **File**: `TableRepository.cs:441`, `OrderRepository.cs:45,54`
- **Expected**: Foreign key to `public.TableSessions.session_id`
- **Actual**: No foreign key constraint
- **Impact**: Orphaned bills, session-bill relationship broken
- **Location**: `billing.bills.session_id` (uuid) → `public.TableSessions.session_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

#### 21. Missing FK: billing.bills.parent_bill_id
- **API**: PaymentApi (split payments)
- **File**: (implicit in split payment logic)
- **Expected**: Foreign key to `billing.bills.bill_id` (self-reference)
- **Actual**: No foreign key constraint
- **Impact**: Invalid parent bill references, split payment tracking broken
- **Location**: `billing.bills.parent_bill_id` (uuid) → `billing.bills.bill_id` (uuid)
- **Risk Level**: **HIGH**
- **Fix Required**: Add foreign key constraint

### MEDIUM RISK MISMATCHES

#### 22. Type Mismatch: public.shifts.opened_by_user_id
- **API**: TablesApi (ShiftRepository)
- **File**: `ShiftRepository.cs:45,97`
- **Expected**: `integer` (to match public.Users.Id)
- **Actual**: `varchar`
- **Impact**: Foreign key constraint cannot be created. Type casting required.
- **Location**: `public.shifts.opened_by_user_id` (varchar) vs `public.Users.Id` (integer) or `users.users.user_id` (varchar)
- **Risk Level**: **MEDIUM**
- **Fix Required**: Align types - determine authoritative user ID type

#### 23. Type Mismatch: users.users.user_id
- **API**: UsersApi
- **File**: `UsersRepository.cs:18,21,92`
- **Expected**: `string` (varchar) - matches code
- **Actual**: `varchar`
- **Impact**: None if code uses string, but public.Users.Id is integer - confusion
- **Location**: `users.users.user_id` (varchar) vs `public.Users.Id` (integer) - **SCHEMA DUPLICATION**
- **Risk Level**: **MEDIUM**
- **Fix Required**: Consolidate user tables, determine authoritative schema

#### 24. Missing FK: public.tables.type_id
- **API**: TablesApi
- **File**: `TableRepository.cs:45,500,529`
- **Expected**: Foreign key to `public.table_types.id`
- **Actual**: No foreign key constraint
- **Impact**: Invalid table types, data integrity risk
- **Location**: `public.tables.type_id` (integer) → `public.table_types.id` (integer)
- **Risk Level**: **MEDIUM**
- **Fix Required**: Add foreign key constraint

#### 25. Schema Duplication: Two Payments Tables
- **API**: PaymentApi, TablesApi
- **Tables**: `public.payments` and `pay.payments`
- **Issue**: Different structures, different purposes
- **Impact**: Code confusion, data inconsistency
- **Location**:
  - `public.payments` - Simple structure (bill_id, amount_paid, payment_method, is_voided)
  - `pay.payments` - Complex structure (billing_id, session_id, discount_amount, tip_amount, method enum, status enum, meta jsonb)
- **Risk Level**: **MEDIUM**
- **Fix Required**: Consolidate to single table, clarify purpose

### LOW RISK MISMATCHES

#### 26. Type Mismatch: users.users timestamps
- **API**: UsersApi
- **File**: `UsersRepository.cs:21,92`
- **Expected**: `timestamptz` (with timezone)
- **Actual**: `timestamp` (without timezone)
- **Impact**: Timezone handling issues, but may work if all in same timezone
- **Location**: `users.users.created_at`, `users.users.updated_at` (timestamp) vs code may expect timestamptz
- **Risk Level**: **LOW**
- **Fix Required**: Change to timestamptz for consistency

#### 27. Legacy Tables: PascalCase Tables
- **API**: Various (legacy EF Core)
- **Tables**: `public.Orders`, `public.OrderItems`, `public.InventoryItems`, `public.Users`
- **Issue**: Legacy EF Core tables, may be orphaned
- **Impact**: Confusion, potential data duplication
- **Location**: `public` schema
- **Risk Level**: **LOW**
- **Fix Required**: Audit usage, remove if unused

## Summary Statistics

- **Total Mismatches**: 27
- **CRITICAL**: 7
- **HIGH**: 14
- **MEDIUM**: 5
- **LOW**: 1

## Priority Fix Order

1. **Fix type mismatches** (CRITICAL) - Blocks foreign key creation
2. **Consolidate duplicate schemas** (CRITICAL) - Prevents data inconsistency
3. **Add missing foreign keys** (HIGH) - Restores referential integrity
4. **Fix remaining type mismatches** (MEDIUM) - Improves consistency
5. **Clean up legacy tables** (LOW) - Reduces confusion

