# 01 - Database Structure Inventory

**Status**: READ-ONLY DISCOVERY  
**Date**: 2025-12-23  
**Auditor**: Principal Database Architect  
**Tool**: postgres-mcp

---

## Executive Summary

This document provides a complete inventory of all database schemas, tables, and their current state. This is **read-only discovery** - no changes have been made.

**Key Findings**:
- **17 user schemas** identified
- **Duplicate schemas** detected: `ord` vs `orders`
- **Legacy tables** in `public` schema (TableSessions, Orders, OrderItems, bills, payments, Users)
- **Data inconsistencies** between duplicate schemas
- **Missing foreign key constraints** on critical relationships

---

## Schema Overview

| Schema | Purpose | Status | Row Count (Total) |
|--------|---------|--------|-------------------|
| `public` | Legacy/EF Core tables | ⚠️ MIXED | 117 |
| `tables` | Active sessions | ✅ ACTIVE | 20 |
| `orders` | Active order domain | ✅ ACTIVE | 2 |
| `ord` | Legacy order domain | ⚠️ LEGACY | 0 |
| `billing` | Bill management | ✅ ACTIVE | 1 |
| `pay` | Payment processing | ✅ ACTIVE | 0 |
| `menu` | Menu items | ✅ ACTIVE | 12 |
| `users` | User management | ✅ ACTIVE | 1 |
| `inventory` | Inventory tracking | ✅ ACTIVE | 0 |
| `customers` | Customer data | ✅ ACTIVE | 0 |
| `discounts` | Discount campaigns | ✅ ACTIVE | 6 |
| `settings` | System settings | ✅ ACTIVE | 3 |
| `audit` | Audit logging | ✅ ACTIVE | 1 |
| `sync` | Sync events | ✅ ACTIVE | 0 |

---

## Tables by Domain

### 1. Tables & Sessions Domain

#### `tables.sessions` (ACTIVE)
**Purpose**: Active table sessions  
**Row Count**: 20  
**Key Columns**:
- `session_id` (uuid, PK)
- `table_id` (uuid, NOT NULL)
- `billing_id` (uuid, NOT NULL)
- `start_time` (timestamptz, NOT NULL)
- `end_time` (timestamptz, nullable)
- `is_active` (boolean, NOT NULL, default: true)
- `state` (varchar, NOT NULL, default: 'Active')
- `accumulated_cost` (numeric, NOT NULL, default: 0)
- `guest_count` (integer, NOT NULL, default: 1)

**Indexes**:
- Primary key on `session_id`
- Index on `(table_id, is_active)` WHERE `is_active = true`
- Index on `billing_id`

**Status**: ✅ **ACTIVE** - This is the canonical sessions table

#### `public.tables` (ACTIVE)
**Purpose**: Table definitions  
**Row Count**: 10  
**Key Columns**:
- `table_id` (uuid, PK)
- `table_number` (varchar, NOT NULL)
- `table_name` (varchar, nullable)
- `table_type` (integer, NOT NULL, default: 0)
- `type_id` (integer, nullable, FK to `table_types.id`)
- `capacity` (integer, NOT NULL, default: 4)
- `is_active` (boolean, NOT NULL, default: true)

**Status**: ✅ **ACTIVE** - Canonical table definitions

#### `public.TableSessions` (LEGACY)
**Purpose**: Legacy session tracking  
**Row Count**: 7  
**Key Columns**:
- `session_id` (uuid, PK)
- `table_label` (text, NOT NULL)
- `server_id` (text, nullable)
- `status` (text, NOT NULL, default: 'active')
- `billing_id` (uuid, nullable)
- `items` (jsonb, nullable)

**Status**: ⚠️ **LEGACY** - Still referenced by BillsController but should be deprecated

---

### 2. Orders Domain

#### `orders.orders` (ACTIVE)
**Purpose**: Order aggregate root  
**Row Count**: 2  
**Key Columns**:
- `order_id` (uuid, PK, default: gen_random_uuid())
- `billing_id` (uuid, NOT NULL)
- `session_id` (uuid, NOT NULL)
- `status` (enum: `orders.order_status`, NOT NULL, default: 'open')
- `subtotal` (numeric, NOT NULL, default: 0)
- `discount` (numeric, NOT NULL, default: 0)
- `tax` (numeric, NOT NULL, default: 0)
- `tip` (numeric, NOT NULL, default: 0)
- `total` (numeric, NOT NULL, default: 0)
- `created_at` (timestamptz, NOT NULL)
- `delivered_at` (timestamptz, nullable)
- `closed_at` (timestamptz, nullable)

**Indexes**:
- Primary key on `order_id`
- Index on `billing_id`
- Index on `session_id`
- Index on `status`

**Status**: ✅ **ACTIVE** - Canonical orders table

#### `orders.order_items` (ACTIVE)
**Purpose**: Order line items  
**Row Count**: 0  
**Key Columns**:
- `order_item_id` (uuid, PK, default: gen_random_uuid())
- `order_id` (uuid, NOT NULL, FK to `orders.orders`)
- `menu_item_id` (uuid, NOT NULL)
- `menu_item_version` (integer, NOT NULL)
- `name` (varchar, NOT NULL)
- `base_price` (numeric, NOT NULL)
- `price_delta` (numeric, NOT NULL, default: 0)
- `vendor_price` (numeric, NOT NULL, default: 0)
- `line_total` (numeric, NOT NULL)
- `profit` (numeric, NOT NULL)
- `quantity` (integer, NOT NULL)
- `delivered_quantity` (integer, NOT NULL, default: 0)
- `delivery_status` (enum: `orders.delivery_status`, NOT NULL, default: 'pending')
- `modifiers` (jsonb, nullable, default: '[]')
- `original_session_id` (uuid, nullable)

**Indexes**:
- Primary key on `order_item_id`
- Index on `order_id`
- Index on `menu_item_id`

**Status**: ✅ **ACTIVE** - Canonical order items table

#### `ord.orders` (LEGACY)
**Purpose**: Legacy order tracking  
**Row Count**: 0  
**Key Columns**:
- `order_id` (uuid, PK)
- `session_id` (uuid, NOT NULL)
- `table_label` (text, NOT NULL)
- `status` (text, NOT NULL, default: 'submitted')
- `is_deleted` (boolean, NOT NULL, default: false)
- `shift_id` (uuid, nullable)

**Status**: ⚠️ **LEGACY** - Different structure from `orders.orders`, appears unused

#### `ord.order_items` (LEGACY)
**Purpose**: Legacy order items  
**Row Count**: 0  
**Key Columns**:
- `order_item_id` (uuid, PK)
- `order_id` (uuid, NOT NULL, FK to `ord.orders`)
- `menu_item_id` (bigint, NOT NULL) ⚠️ **TYPE MISMATCH** - should be uuid
- `quantity` (integer, NOT NULL, default: 1)
- `base_price` (numeric, NOT NULL, default: 0.00)
- `status` (text, NOT NULL, default: 'pending')
- `snapshot_name` (text, nullable)

**Status**: ⚠️ **LEGACY** - Different structure, type mismatch on `menu_item_id`

#### `public.Orders` (LEGACY)
**Purpose**: Legacy EF Core orders  
**Row Count**: 0  
**Key Columns**:
- `Id` (integer, PK)
- `TableSessionId` (integer, NOT NULL)
- `ServerId` (integer, nullable, FK to `Users.Id`)
- `Status` (varchar, NOT NULL)
- `Subtotal` (numeric, NOT NULL)
- `TotalAmount` (numeric, NOT NULL)
- `shift_id` (uuid, nullable)

**Status**: ⚠️ **LEGACY** - EF Core naming, integer IDs, should be deprecated

#### `public.OrderItems` (LEGACY)
**Purpose**: Legacy EF Core order items  
**Row Count**: 0  
**Key Columns**:
- `Id` (integer, PK)
- `OrderId` (integer, NOT NULL, FK to `Orders.Id`)
- `InventoryItemId` (integer, NOT NULL, FK to `InventoryItems.Id`)
- `ItemName` (varchar, NOT NULL)
- `Quantity` (integer, NOT NULL)
- `UnitPrice` (numeric, NOT NULL)
- `TotalPrice` (numeric, NOT NULL)

**Status**: ⚠️ **LEGACY** - EF Core naming, integer IDs, references legacy InventoryItems

---

### 3. Billing Domain

#### `billing.bills` (ACTIVE)
**Purpose**: Bill aggregate root  
**Row Count**: 1  
**Key Columns**:
- `bill_id` (uuid, PK, default: gen_random_uuid())
- `billing_id` (uuid, NOT NULL)
- `session_id` (uuid, NOT NULL)
- `table_id` (uuid, NOT NULL)
- `status` (enum: `billing.bill_status`, NOT NULL, default: 'AwaitingPayment')
- `items_total` (numeric, NOT NULL, default: 0)
- `time_total` (numeric, NOT NULL, default: 0)
- `subtotal` (numeric, NOT NULL, default: 0)
- `discounts` (numeric, NOT NULL, default: 0)
- `tax` (numeric, NOT NULL, default: 0)
- `total_amount` (numeric, NOT NULL, default: 0)
- `time_minutes` (integer, NOT NULL, default: 0)
- `settled_at` (timestamptz, nullable)
- `parent_bill_id` (uuid, nullable)
- `table_label` (varchar, nullable)
- `server_name` (varchar, nullable)

**Indexes**:
- Primary key on `bill_id`
- Index on `billing_id`
- Index on `session_id`
- Index on `status`
- Index on `parent_bill_id`

**Status**: ✅ **ACTIVE** - Canonical bills table

#### `billing.bill_items` (ACTIVE)
**Purpose**: Bill line items  
**Row Count**: 0  
**Key Columns**:
- `bill_item_id` (uuid, PK, default: gen_random_uuid())
- `bill_id` (uuid, NOT NULL, FK to `billing.bills`)
- `order_item_id` (uuid, NOT NULL)
- `menu_item_id` (uuid, NOT NULL)
- `name` (varchar, NOT NULL)
- `base_price` (numeric, NOT NULL)
- `price_delta` (numeric, NOT NULL, default: 0)
- `line_total` (numeric, NOT NULL)
- `quantity` (integer, NOT NULL)

**Status**: ✅ **ACTIVE** - Canonical bill items table

#### `public.bills` (LEGACY)
**Purpose**: Legacy bill tracking  
**Row Count**: 0  
**Key Columns**:
- `bill_id` (uuid, PK)
- `shift_id` (uuid, NOT NULL)
- `table_session_id` (integer, NOT NULL)
- `total_amount` (numeric, NOT NULL)
- `status` (varchar, NOT NULL, default: 'unsettled')
- `items` (jsonb, nullable, default: '[]')
- `is_settled` (boolean, nullable, default: false)

**Status**: ⚠️ **LEGACY** - Different structure, references legacy `TableSessions`

---

### 4. Payments Domain

#### `pay.payments` (ACTIVE)
**Purpose**: Payment transactions  
**Row Count**: 0  
**Key Columns**:
- `payment_id` (uuid, PK, default: gen_random_uuid())
- `bill_id` (uuid, NOT NULL) ⚠️ **FK MISSING** - references `billing.bills.bill_id` but FK constraint shows NULL
- `billing_id` (uuid, NOT NULL)
- `amount_paid` (numeric, NOT NULL, default: 0)
- `discount_amount` (numeric, NOT NULL, default: 0)
- `tip_amount` (numeric, NOT NULL, default: 0)
- `method` (enum: `pay.payment_method`, NOT NULL)
- `status` (enum: `pay.payment_status`, NOT NULL, default: 'Paid')
- `external_reference` (varchar, nullable)
- `is_settled` (boolean, NOT NULL, default: false)
- `shift_id` (uuid, nullable)

**Indexes**:
- Primary key on `payment_id`
- Index on `bill_id`
- Index on `billing_id`
- Unique index on `(billing_id, external_reference)` WHERE `external_reference IS NOT NULL`

**Status**: ✅ **ACTIVE** - But missing FK constraint on `bill_id`

#### `pay.payment_ledger` (ACTIVE)
**Purpose**: Payment ledger (aggregate)  
**Row Count**: 0  
**Key Columns**:
- `billing_id` (uuid, PK)
- `bill_id` (uuid, NOT NULL) ⚠️ **FK MISSING** - references `billing.bills.bill_id` but FK constraint shows NULL
- `total_due` (numeric, NOT NULL)
- `total_paid` (numeric, NOT NULL, default: 0)
- `total_discount` (numeric, NOT NULL, default: 0)
- `total_tip` (numeric, NOT NULL, default: 0)
- `status` (enum: `pay.payment_status`, NOT NULL, default: 'NotPaid')

**Indexes**:
- Primary key on `billing_id`
- Index on `bill_id`

**Status**: ✅ **ACTIVE** - But missing FK constraint on `bill_id`

#### `pay.bill_ledger` (LEGACY?)
**Purpose**: Legacy bill ledger  
**Row Count**: 0  
**Key Columns**:
- `billing_id` (text, PK) ⚠️ **TYPE MISMATCH** - should be uuid
- `session_id` (text, NOT NULL) ⚠️ **TYPE MISMATCH** - should be uuid
- `total_due` (numeric, NOT NULL, default: 0.00)
- `total_discount` (numeric, NOT NULL, default: 0.00)
- `total_paid` (numeric, NOT NULL, default: 0.00)
- `total_tip` (numeric, NOT NULL, default: 0.00)
- `status` (text, NOT NULL, default: 'unpaid')

**Status**: ⚠️ **LEGACY** - Text types instead of uuid, different structure from `payment_ledger`

#### `public.payments` (LEGACY)
**Purpose**: Legacy payment tracking  
**Row Count**: 0  
**Key Columns**:
- `payment_id` (uuid, PK)
- `shift_id` (uuid, NOT NULL)
- `bill_id` (uuid, NOT NULL, FK to `public.bills.bill_id`)
- `amount_paid` (numeric, NOT NULL)
- `payment_method` (varchar, NOT NULL)
- `is_voided` (boolean, NOT NULL, default: false)
- `created_by_user_id` (integer, nullable, FK to `Users.Id`)

**Status**: ⚠️ **LEGACY** - References legacy `public.bills` and `Users`

---

### 5. Menu Domain

#### `menu.menu_items` (ACTIVE)
**Purpose**: Menu items with versioning  
**Row Count**: 10  
**Key Columns**:
- `menu_item_id` (uuid, NOT NULL)
- `version` (integer, NOT NULL)
- `name` (varchar, NOT NULL)
- `base_price` (numeric, NOT NULL)
- `category` (varchar, NOT NULL)
- `sku` (varchar, nullable)
- `is_available` (boolean, NOT NULL, default: true)
- `is_combo` (boolean, NOT NULL, default: false)
- `is_current_version` (boolean, NOT NULL, default: true)

**Constraints**:
- Primary key on `(menu_item_id, version)`
- Unique index on `menu_item_id` WHERE `is_current_version = true`

**Status**: ✅ **ACTIVE** - Canonical menu items with versioning

#### `menu.menu_categories` (ACTIVE)
**Purpose**: Menu categories  
**Row Count**: 2  
**Key Columns**:
- `id` (uuid, PK, default: gen_random_uuid())
- `name` (varchar, NOT NULL, UNIQUE)
- `is_active` (boolean, NOT NULL, default: true)
- `display_order` (integer, NOT NULL, default: 0)

**Status**: ✅ **ACTIVE** - Canonical categories

---

### 6. Users Domain

#### `users.users` (ACTIVE)
**Purpose**: User accounts  
**Row Count**: 1  
**Key Columns**:
- `user_id` (varchar, PK) ⚠️ **TYPE MISMATCH** - should be uuid or integer
- `username` (varchar, NOT NULL, UNIQUE)
- `password_hash` (varchar, NOT NULL)
- `role` (varchar, NOT NULL, default: 'Server')
- `is_active` (boolean, NOT NULL, default: true)
- `is_deleted` (boolean, NOT NULL, default: false)
- `created_at` (timestamp, NOT NULL)
- `updated_at` (timestamp, NOT NULL)

**Indexes**:
- Primary key on `user_id`
- Unique index on `username`
- Partial indexes on `username`, `role`, `is_active`, `created_at` WHERE `is_deleted = false`

**Status**: ✅ **ACTIVE** - But `user_id` type is varchar (inconsistent with other domains)

#### `users.roles` (ACTIVE)
**Purpose**: Role definitions  
**Row Count**: 6  
**Status**: ✅ **ACTIVE**

#### `users.role_permissions` (ACTIVE)
**Purpose**: Role permissions  
**Row Count**: 182  
**Status**: ✅ **ACTIVE**

#### `users.role_inheritance` (ACTIVE)
**Purpose**: Role inheritance  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `public.Users` (LEGACY)
**Purpose**: Legacy EF Core users  
**Row Count**: 1  
**Key Columns**:
- `Id` (integer, PK)
- `Username` (varchar, NOT NULL)
- `Email` (varchar, NOT NULL)
- `FirstName` (varchar, NOT NULL)
- `LastName` (varchar, NOT NULL)
- `Role` (varchar, NOT NULL)
- `passwordhash` (varchar, nullable)
- `staff_member_id` (uuid, nullable)

**Status**: ⚠️ **LEGACY** - EF Core naming, integer IDs, different structure from `users.users`

---

### 7. Shifts Domain

#### `public.shifts` (ACTIVE)
**Purpose**: Shift management  
**Row Count**: 4  
**Key Columns**:
- `shift_id` (uuid, PK, default: gen_random_uuid())
- `shift_number` (integer, NOT NULL, UNIQUE, sequence)
- `opened_by_user_id` (integer, NOT NULL) ⚠️ **TYPE MISMATCH** - FK to `public.Users.Id` (integer) but should reference `users.users.user_id` (varchar)
- `opened_by_name` (varchar, nullable)
- `opened_at` (timestamptz, NOT NULL, default: now())
- `starting_cash` (numeric, NOT NULL)
- `closed_by_user_id` (integer, nullable)
- `closed_at` (timestamptz, nullable)
- `status` (varchar, NOT NULL, default: 'open')
- `idempotency_key` (uuid, nullable)

**Indexes**:
- Primary key on `shift_id`
- Unique index on `shift_number`
- Index on `status`
- Index on `opened_at`

**Status**: ✅ **ACTIVE** - But FK type mismatch with users

---

### 8. Inventory Domain

#### `inventory.items` (ACTIVE)
**Purpose**: Inventory items  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `inventory.transactions` (ACTIVE)
**Purpose**: Inventory transactions  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `public.InventoryItems` (LEGACY)
**Purpose**: Legacy EF Core inventory  
**Row Count**: 0  
**Status**: ⚠️ **LEGACY** - EF Core naming, integer IDs

---

### 9. Customers Domain

#### `customers.customers` (ACTIVE)
**Purpose**: Customer data  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

---

### 10. Discounts Domain

#### `discounts.campaigns` (ACTIVE)
**Purpose**: Discount campaigns  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `discounts.vouchers` (ACTIVE)
**Purpose**: Vouchers  
**Row Count**: 3  
**Status**: ✅ **ACTIVE**

#### `discounts.applied_discounts` (ACTIVE)
**Purpose**: Applied discounts  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `discounts.combo_offers` (ACTIVE)
**Purpose**: Combo offers  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `discounts.customer_segments` (ACTIVE)
**Purpose**: Customer segments  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `discounts.customer_histories` (ACTIVE)
**Purpose**: Customer purchase history  
**Row Count**: 3  
**Status**: ✅ **ACTIVE**

---

### 11. Settings Domain

#### `settings.system_settings` (ACTIVE)
**Purpose**: System settings  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

#### `settings.business_settings` (ACTIVE)
**Purpose**: Business settings  
**Row Count**: 1  
**Status**: ✅ **ACTIVE**

#### `settings.billiard_settings` (ACTIVE)
**Purpose**: Billiard-specific settings  
**Row Count**: 1  
**Status**: ✅ **ACTIVE**

#### `settings.app_system_settings` (ACTIVE)
**Purpose**: App system settings  
**Row Count**: 1  
**Status**: ✅ **ACTIVE**

---

### 12. Audit Domain

#### `audit.events` (ACTIVE)
**Purpose**: Audit events  
**Row Count**: 1  
**Status**: ✅ **ACTIVE**

---

### 13. Sync Domain

#### `sync.processed_sync_events` (ACTIVE)
**Purpose**: Processed sync events  
**Row Count**: 0  
**Status**: ✅ **ACTIVE**

---

## Critical Issues Identified

### 1. Duplicate Schemas
- **`ord` vs `orders`**: Two schemas with similar purpose but different structures
  - `ord.orders`: Legacy structure, 0 rows
  - `orders.orders`: Active structure, 2 rows
  - **Action Required**: Deprecate `ord` schema

### 2. Legacy Tables in `public` Schema
- `TableSessions` (7 rows) - Still referenced by BillsController
- `Orders` (0 rows) - EF Core legacy
- `OrderItems` (0 rows) - EF Core legacy
- `bills` (0 rows) - Legacy structure
- `payments` (0 rows) - Legacy structure
- `Users` (1 row) - EF Core legacy, different from `users.users`
- `InventoryItems` (0 rows) - EF Core legacy

### 3. Type Mismatches
- `shifts.opened_by_user_id` (integer) vs `users.users.user_id` (varchar)
- `ord.order_items.menu_item_id` (bigint) vs `orders.order_items.menu_item_id` (uuid)
- `pay.bill_ledger.billing_id` (text) vs `pay.payment_ledger.billing_id` (uuid)
- `users.users.user_id` (varchar) - inconsistent with other domains using uuid

### 4. Missing Foreign Key Constraints
- `pay.payments.bill_id` → `billing.bills.bill_id` (FK constraint exists but shows NULL foreign_table_schema)
- `pay.payment_ledger.bill_id` → `billing.bills.bill_id` (FK constraint exists but shows NULL foreign_table_schema)

### 5. Data Inconsistencies
- `public.TableSessions` (7 rows) vs `tables.sessions` (20 rows) - Different session tracking
- `public.Users` (1 row) vs `users.users` (1 row) - Different user systems

---

## Next Steps

1. **Step 2**: Map code/API expectations to database schema
2. **Step 3**: Perform data quality audit
3. **Step 4**: Create schema correction plan
4. **Step 5**: Create data migration plan
5. **Step 6**: Create production hardening plan

---

**END OF INVENTORY**

