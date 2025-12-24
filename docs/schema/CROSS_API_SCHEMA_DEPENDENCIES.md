# Cross-API Schema Dependencies

**Purpose**: Document how APIs depend on each other's schema  
**Last Updated**: 2025-12-23

## Dependency Graph

```
TablesApi
    ├──→ orders.orders (reads/writes)
    ├──→ orders.order_items (reads for totals)
    ├──→ billing.bills (writes)
    ├──→ public.TableSessions (reads/writes)
    ├──→ public.shifts (reads for gating)
    └──→ menu.menu_items (reads - blocked by type mismatch)

OrderApi
    ├──→ orders.orders (reads/writes)
    ├──→ orders.order_items (reads/writes)
    ├──→ billing.bills (reads/writes)
    ├──→ menu.menu_items (reads - type mismatch issue)
    ├──→ menu.combos (reads - type mismatch issue)
    ├──→ menu.combo_items (reads)
    ├──→ menu.modifiers (reads)
    ├──→ menu.modifier_options (reads)
    └──→ inventory.items (reads via HTTP API, not direct DB)

PaymentApi
    ├──→ pay.payments (reads/writes)
    ├──→ pay.bill_ledger (reads/writes)
    ├──→ pay.payment_logs (writes)
    ├──→ billing.bills (reads)
    └──→ public.TableSessions (reads via session_id)

MenuApi
    ├──→ menu.menu_items (reads/writes)
    ├──→ menu.combos (reads/writes)
    ├──→ menu.combo_items (reads/writes)
    ├──→ menu.modifiers (reads/writes)
    ├──→ menu.modifier_options (reads/writes)
    ├──→ menu.menu_item_modifiers (reads/writes)
    ├──→ menu.menu_history (writes)
    └──→ inventory.items (reads via SKU lookup)

InventoryApi
    ├──→ inventory.items (reads/writes)
    ├──→ inventory.transactions (writes)
    └──→ inventory.v_items_current (reads - view)

UsersApi
    ├──→ users.users (reads/writes)
    ├──→ users.roles (reads)
    ├──→ users.role_permissions (reads)
    └──→ users.role_inheritance (reads)

SettingsApi
    ├──→ settings.hierarchical_settings (reads/writes)
    └──→ settings.settings_audit (writes)

CustomerApi
    └──→ customers.customers (reads/writes)

DiscountApi
    ├──→ discounts.campaigns (reads/writes)
    ├──→ discounts.vouchers (reads/writes)
    ├──→ discounts.applied_discounts (writes)
    └──→ discounts.customer_segments (reads)
```

## Critical Dependencies

### OrderApi → MenuApi
**Dependency**: `orders.order_items.menu_item_id` → `menu.menu_items.menu_item_id`
**Status**: **BROKEN** - Type mismatch (uuid vs bigint in ord schema)
**Impact**: Cannot JOIN, must use application-level lookups
**Fix**: Align types or use orders.order_items (uuid) with menu.menu_items (uuid)

### OrderApi → InventoryApi
**Dependency**: HTTP API calls, not direct DB access
**Status**: **WORKING** - But fragile (HTTP dependency)
**Impact**: Network failures break inventory checks
**Fix**: Consider direct DB access or message queue

### TablesApi → OrderApi
**Dependency**: `orders.orders.session_id` → `public.TableSessions.session_id`
**Status**: **MISSING FK** - Implicit join, no constraint
**Impact**: Orphaned orders if session deleted
**Fix**: Add foreign key constraint

### PaymentApi → OrderApi
**Dependency**: `billing.bills.billing_id` → `orders.orders.billing_id`
**Status**: **MISSING FK** - Implicit join, no constraint
**Impact**: Orphaned bills, reconciliation failures
**Fix**: Add foreign key constraint

### TablesApi → PaymentApi
**Dependency**: `billing.bills.session_id` → `public.TableSessions.session_id`
**Status**: **MISSING FK** - Implicit join, no constraint
**Impact**: Orphaned bills
**Fix**: Add foreign key constraint

## Schema Ownership

### TablesApi Owns
- `public.TableSessions` (primary)
- `public.tables` (primary)
- `public.table_types` (primary)
- `public.shifts` (primary)
- `public.table_session_moves` (primary)

### OrderApi Owns
- `orders.orders` (primary)
- `orders.order_items` (primary)
- `orders.order_logs` (primary)

### PaymentApi Owns
- `pay.payments` (primary)
- `pay.bill_ledger` (primary)
- `pay.payment_logs` (primary)

### MenuApi Owns
- `menu.menu_items` (primary)
- `menu.combos` (primary)
- `menu.combo_items` (primary)
- `menu.modifiers` (primary)
- `menu.modifier_options` (primary)
- `menu.menu_item_modifiers` (primary)
- `menu.menu_history` (primary)
- `menu.menu_categories` (primary)

### InventoryApi Owns
- `inventory.items` (primary)
- `inventory.transactions` (primary)

### UsersApi Owns
- `users.users` (primary)
- `users.roles` (primary)
- `users.role_permissions` (primary)
- `users.role_inheritance` (primary)

### SettingsApi Owns
- `settings.hierarchical_settings` (primary)
- `settings.settings_audit` (primary)
- `settings.system_settings` (primary)
- `settings.business_settings` (primary)
- `settings.billiard_settings` (primary)

## Shared Schemas

### billing Schema
**Shared By**: TablesApi, OrderApi, PaymentApi
**Ownership**: **CONTESTED** - Multiple APIs write to billing.bills
**Risk**: Write conflicts, data inconsistency
**Fix**: Designate single owner (PaymentApi) or use event sourcing

### orders Schema
**Shared By**: TablesApi, OrderApi
**Ownership**: OrderApi (primary), TablesApi (reads for totals)
**Risk**: Low - TablesApi only reads
**Fix**: None required, but add FK constraints

## Breaking Changes Impact

### If MenuApi Changes menu.menu_items.menu_item_id Type
**Impact**: OrderApi breaks (if using orders.order_items with uuid)
**Affected APIs**: OrderApi, TablesApi
**Mitigation**: Coordinate migration, update all references

### If TablesApi Changes public.TableSessions.session_id Type
**Impact**: All APIs break
**Affected APIs**: OrderApi, PaymentApi, TablesApi
**Mitigation**: **FORBIDDEN** - session_id is anchor point

### If PaymentApi Changes billing.bills Structure
**Impact**: OrderApi, TablesApi break
**Affected APIs**: OrderApi, TablesApi, PaymentApi
**Mitigation**: Version API, coordinate migration

## Dependency Violations

### Direct DB Access Across API Boundaries
**Violation**: OrderApi reads menu.menu_items directly (should use MenuApi HTTP)
**Location**: `OrderRepository.cs:284`
**Risk**: Tight coupling, breaks microservices boundaries
**Fix**: Use MenuApi HTTP endpoint or shared library

### Implicit Joins Without FK
**Violation**: Multiple APIs join across schemas without FK constraints
**Examples**:
- `orders.orders.session_id` → `public.TableSessions.session_id` (no FK)
- `billing.bills.session_id` → `public.TableSessions.session_id` (no FK)
- `orders.orders.billing_id` → `billing.bills.billing_id` (no FK)
**Risk**: Orphaned records, data integrity violations
**Fix**: Add foreign key constraints

## Recommendations

1. **Add Foreign Keys**: All cross-schema references must have FK constraints
2. **Type Alignment**: Fix all type mismatches before adding FKs
3. **Schema Consolidation**: Remove duplicate schemas (ord, public.bills, public.payments)
4. **API Boundaries**: Use HTTP APIs for cross-service data access, not direct DB
5. **Ownership Clarity**: Each schema/table must have single owner API
6. **Migration Coordination**: Cross-API schema changes require coordination

