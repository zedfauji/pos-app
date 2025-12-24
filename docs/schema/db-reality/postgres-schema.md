# PostgreSQL Database Schema Reality

**Audit Date**: 2025-12-23  
**Database**: PostgreSQL (Cloud SQL)  
**Connection**: bola8pos:northamerica-south1:pos-app-1

## Schema Inventory

### User Schemas
1. `public` - Main application schema
2. `ord` - Orders schema (legacy/new hybrid)
3. `orders` - Orders schema (new structure)
4. `pay` - Payments schema
5. `billing` - Billing schema
6. `menu` - Menu items schema
7. `inventory` - Inventory schema
8. `users` - Users and authentication schema
9. `customers` - Customer management schema
10. `settings` - System settings schema
11. `discounts` - Discounts and campaigns schema
12. `audit` - Audit logging schema
13. `tables` - Table management schema (legacy?)
14. `sync` - Synchronization schema

## Table Inventory by Schema

### public Schema

#### TableSessions
**Columns**:
- `session_id` (uuid, PK, NOT NULL)
- `table_label` (text, NOT NULL)
- `server_id` (text, NULLABLE)
- `server_name` (text, NULLABLE)
- `start_time` (timestamptz, NOT NULL, default now())
- `end_time` (timestamptz, NULLABLE)
- `status` (text, NOT NULL, default 'active')
- `billing_id` (uuid, NULLABLE)
- `items` (jsonb, NULLABLE)
- `shift_id` (uuid, NULLABLE)

**Constraints**:
- PRIMARY KEY: `session_id`
- **MISSING FK**: `shift_id` → `public.shifts.shift_id`

**Indexes**:
- `TableSessions_pkey` (UNIQUE on session_id)
- `idx_tablesessions_status` (on status)
- `idx_tablesessions_label` (on table_label)

#### shifts
**Columns**:
- `shift_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `shift_number` (integer, NOT NULL, UNIQUE, SERIAL)
- `opened_by_user_id` (varchar, NOT NULL) - **TYPE MISMATCH**: Code expects integer, DB has varchar
- `opened_by_name` (varchar(100), NULLABLE)
- `opened_at` (timestamptz, NOT NULL, default now())
- `starting_cash` (numeric, NOT NULL)
- `closed_by_user_id` (varchar, NULLABLE) - **TYPE MISMATCH**: Code expects integer
- `closed_by_name` (varchar(100), NULLABLE)
- `closed_at` (timestamptz, NULLABLE)
- `declared_cash` (numeric, NULLABLE)
- `expected_cash` (numeric, NULLABLE)
- `difference` (numeric, NULLABLE)
- `difference_category` (varchar(50), NULLABLE)
- `close_reason` (text, NULLABLE)
- `status` (varchar(20), NOT NULL, default 'open')
- `idempotency_key` (uuid, NULLABLE)

**Constraints**:
- PRIMARY KEY: `shift_id`
- UNIQUE: `shift_number`
- CHECK: `closed_at_after_opened_at`

**Indexes**:
- `shifts_pkey` (UNIQUE on shift_id)
- `shifts_shift_number_key` (UNIQUE on shift_number)
- `idx_shifts_status` (on status)
- `idx_shifts_opened_at` (on opened_at)
- `idx_shifts_opened_by` (on opened_by_user_id)

**MISSING FK**: `opened_by_user_id` → `public.Users.Id` or `users.users.user_id`

#### bills
**Columns**:
- `bill_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `shift_id` (uuid, NOT NULL)
- `table_session_id` (integer, NOT NULL) - **TYPE MISMATCH**: TableSessions uses uuid session_id
- `total_amount` (numeric, NOT NULL)
- `status` (varchar(20), NOT NULL, default 'unsettled')
- `created_at` (timestamptz, NOT NULL, default now())
- `created_by_user_id` (integer, NULLABLE)
- `table_label` (varchar, NULLABLE)
- `server_id` (varchar, NULLABLE)
- `server_name` (varchar, NULLABLE)
- `start_time` (timestamptz, NULLABLE)
- `end_time` (timestamptz, NULLABLE)
- `total_time_minutes` (integer, NULLABLE, default 0)
- `time_cost` (numeric, NULLABLE, default 0)
- `items_cost` (numeric, NULLABLE, default 0)
- `items` (jsonb, NULLABLE, default '[]'::jsonb)
- `is_settled` (boolean, NULLABLE, default false)

**Constraints**:
- PRIMARY KEY: `bill_id`
- FOREIGN KEY: `shift_id` → **MISSING** (no FK constraint exists)
- FOREIGN KEY: `table_session_id` → **MISSING** (references integer Id, but TableSessions has uuid)
- FOREIGN KEY: `created_by_user_id` → `public.Users.Id` - **EXISTS**

**Indexes**:
- `bills_pkey` (UNIQUE on bill_id)
- `idx_bills_shift_id` (on shift_id)
- `idx_bills_table_session_id` (on table_session_id)

#### payments
**Columns**:
- `payment_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `shift_id` (uuid, NOT NULL)
- `bill_id` (uuid, NOT NULL)
- `amount_paid` (numeric, NOT NULL)
- `payment_method` (varchar, NOT NULL)
- `is_voided` (boolean, NOT NULL, default false)
- `created_at` (timestamptz, NOT NULL, default now())
- `created_by_user_id` (integer, NULLABLE)

**Constraints**:
- PRIMARY KEY: `payment_id`
- FOREIGN KEY: `bill_id` → `public.bills.bill_id` - **EXISTS**
- FOREIGN KEY: `created_by_user_id` → `public.Users.Id` - **EXISTS**
- **MISSING FK**: `shift_id` → `public.shifts.shift_id`

**Indexes**:
- `payments_pkey` (UNIQUE on payment_id)
- `idx_payments_shift_id` (on shift_id)
- `idx_payments_bill_id` (on bill_id)

### ord Schema

#### orders
**Columns**:
- `order_id` (uuid, PK, NOT NULL)
- `session_id` (uuid, NOT NULL)
- `table_label` (text, NOT NULL)
- `created_at` (timestamptz, NOT NULL, default now())
- `status` (text, NOT NULL, default 'submitted')
- `is_deleted` (boolean, NOT NULL, default false)
- `shift_id` (uuid, NULLABLE)

**Constraints**:
- PRIMARY KEY: `order_id`
- **MISSING FK**: `session_id` → `public.TableSessions.session_id`
- **MISSING FK**: `shift_id` → `public.shifts.shift_id`

**Indexes**:
- `orders_pkey` (UNIQUE on order_id)
- `ix_orders_session` (on session_id)
- `idx_orders_shift_id` (on shift_id)

#### order_items
**Columns**:
- `order_item_id` (uuid, PK, NOT NULL)
- `order_id` (uuid, NOT NULL)
- `menu_item_id` (bigint, NOT NULL) - **TYPE MISMATCH**: menu.menu_items uses uuid
- `quantity` (integer, NOT NULL, default 1)
- `base_price` (numeric, NOT NULL, default 0.00)
- `price_delta` (numeric, NOT NULL, default 0.00)
- `is_deleted` (boolean, NOT NULL, default false)
- `created_at` (timestamptz, NOT NULL, default now())
- `delivered_quantity` (integer, NOT NULL, default 0)
- `status` (text, NOT NULL, default 'pending')
- `snapshot_name` (text, NULLABLE)

**Constraints**:
- PRIMARY KEY: `order_item_id`
- FOREIGN KEY: `order_id` → `ord.orders.order_id` - **EXISTS**
- **MISSING FK**: `menu_item_id` → `menu.menu_items.menu_item_id` (type mismatch prevents FK)

**Indexes**:
- `order_items_pkey` (UNIQUE on order_item_id)
- `ix_order_items_order` (on order_id)

### orders Schema (Separate from ord)

#### orders
**Columns**:
- `order_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `billing_id` (uuid, NOT NULL)
- `session_id` (uuid, NOT NULL)
- `status` (orders.order_status enum, NOT NULL, default 'open')
- `subtotal` (numeric, NOT NULL, default 0)
- `discount` (numeric, NOT NULL, default 0)
- `tax` (numeric, NOT NULL, default 0)
- `tip` (numeric, NOT NULL, default 0)
- `total` (numeric, NOT NULL, default 0)
- `created_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `delivered_at` (timestamptz, NULLABLE)
- `closed_at` (timestamptz, NULLABLE)
- `updated_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `note` (text, NULLABLE)
- `table_id` (varchar, NULLABLE)
- `server_id` (varchar, NULLABLE)
- `server_name` (varchar, NULLABLE)
- `delivery_status` (varchar, NOT NULL, default 'pending')
- `profit_total` (numeric, NOT NULL, default 0)
- `shift_id` (uuid, NULLABLE)
- `is_deleted` (boolean, NOT NULL, default false)

**Constraints**:
- PRIMARY KEY: `order_id`
- CHECK constraints on subtotal, discount, tax, tip, total
- **MISSING FK**: `session_id` → `public.TableSessions.session_id`
- **MISSING FK**: `billing_id` → `billing.bills.billing_id`
- **MISSING FK**: `shift_id` → `public.shifts.shift_id`

**Indexes**:
- `orders_pkey` (UNIQUE on order_id)
- `idx_orders_billing_id` (on billing_id)
- `idx_orders_session_id` (on session_id)
- `idx_orders_status` (on status)
- `idx_orders_created_at` (on created_at)
- `idx_orders_shift_id` (on shift_id)
- `idx_orders_delivery_status` (on delivery_status)
- `idx_orders_is_deleted` (on is_deleted)

#### order_items
**Columns**:
- `order_item_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `order_id` (uuid, NOT NULL)
- `menu_item_id` (uuid, NOT NULL) - **CORRECT TYPE** (matches menu.menu_items)
- `menu_item_version` (integer, NOT NULL)
- `name` (varchar, NOT NULL)
- `base_price` (numeric, NOT NULL)
- `price_delta` (numeric, NOT NULL, default 0)
- `vendor_price` (numeric, NOT NULL, default 0)
- `line_total` (numeric, NOT NULL)
- `profit` (numeric, NOT NULL)
- `quantity` (integer, NOT NULL)
- `delivered_quantity` (integer, NOT NULL, default 0)
- `delivery_status` (orders.delivery_status enum, NOT NULL, default 'pending')
- `special_instructions` (text, NULLABLE)
- `created_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `updated_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `note` (text, NULLABLE)
- `modifiers` (jsonb, NULLABLE, default '[]'::jsonb)
- `original_session_id` (uuid, NULLABLE)
- `combo_id` (uuid, NULLABLE)
- `snapshot_name` (varchar, NULLABLE)
- `snapshot_sku` (varchar, NULLABLE)
- `snapshot_category` (varchar, NULLABLE)
- `snapshot_group` (varchar, NULLABLE)
- `snapshot_version` (integer, NULLABLE)
- `snapshot_picture_url` (text, NULLABLE)
- `line_discount` (numeric, NOT NULL, default 0)
- `is_deleted` (boolean, NOT NULL, default false)

**Constraints**:
- PRIMARY KEY: `order_item_id`
- FOREIGN KEY: `order_id` → `orders.orders.order_id` - **EXISTS**
- **MISSING FK**: `menu_item_id` → `menu.menu_items.menu_item_id`
- **MISSING FK**: `combo_id` → `menu.combos.combo_id`

**Indexes**:
- `order_items_pkey` (UNIQUE on order_item_id)
- `idx_order_items_order_id` (on order_id)
- `idx_order_items_menu_item_id` (on menu_item_id)
- `idx_order_items_is_deleted` (on is_deleted)

### pay Schema

#### payments
**Columns**:
- `payment_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `bill_id` (uuid, NOT NULL)
- `billing_id` (uuid, NOT NULL)
- `amount_paid` (numeric, NOT NULL, default 0)
- `discount_amount` (numeric, NOT NULL, default 0)
- `tip_amount` (numeric, NOT NULL, default 0)
- `method` (pay.payment_method enum, NOT NULL)
- `status` (pay.payment_status enum, NOT NULL, default 'Paid')
- `external_reference` (varchar, NULLABLE)
- `created_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `cancelled_at` (timestamptz, NULLABLE)
- `provider` (varchar, NULLABLE)
- `authorization_code` (varchar, NULLABLE)
- `card_last4` (varchar, NULLABLE)
- `is_settled` (boolean, NOT NULL, default false)
- `shift_id` (uuid, NULLABLE)
- `session_id` (uuid, NULLABLE)
- `discount_reason` (varchar, NULLABLE)
- `meta` (jsonb, NULLABLE)
- `created_by` (varchar, NULLABLE)
- `notes` (text, NULLABLE)

**Constraints**:
- PRIMARY KEY: `payment_id`
- FOREIGN KEY: `bill_id` → **MISSING** (expected billing.bills.bill_id or public.bills.bill_id)
- FOREIGN KEY: `session_id` → `public.TableSessions.session_id` - **EXISTS**
- **MISSING FK**: `billing_id` → `billing.bills.billing_id`
- **MISSING FK**: `shift_id` → `public.shifts.shift_id`

**Indexes**:
- `payments_pkey` (UNIQUE on payment_id)
- `idx_payments_bill_id` (on bill_id)
- `idx_payments_billing_id` (on billing_id)
- `idx_payments_external_ref` (UNIQUE on billing_id, external_reference WHERE external_reference IS NOT NULL)
- `ix_payments_billing` (on billing_id)
- `idx_payments_created_at` (on created_at)

#### bill_ledger
**Columns**:
- `billing_id` (text, PK, NOT NULL) - **TYPE MISMATCH**: Should be uuid
- `session_id` (text, NOT NULL) - **TYPE MISMATCH**: Should be uuid
- `total_due` (numeric, NOT NULL, default 0.00)
- `total_discount` (numeric, NOT NULL, default 0.00)
- `total_paid` (numeric, NOT NULL, default 0.00)
- `total_tip` (numeric, NOT NULL, default 0.00)
- `status` (text, NOT NULL, default 'unpaid')
- `updated_at` (timestamptz, NOT NULL, default now())

**Constraints**:
- PRIMARY KEY: `billing_id`
- CHECK: `bill_ledger_immutable_billing_id`
- **MISSING FK**: `billing_id` → `billing.bills.billing_id` (type mismatch: text vs uuid)
- **MISSING FK**: `session_id` → `public.TableSessions.session_id` (type mismatch: text vs uuid)

**Indexes**:
- `bill_ledger_pkey` (UNIQUE on billing_id)
- `ix_bill_ledger_status` (on status)

### billing Schema

#### bills
**Columns**:
- `bill_id` (uuid, PK, NOT NULL, default gen_random_uuid())
- `billing_id` (uuid, NOT NULL)
- `session_id` (uuid, NOT NULL)
- `table_id` (uuid, NOT NULL)
- `status` (billing.bill_status enum, NOT NULL, default 'AwaitingPayment')
- `items_total` (numeric, NOT NULL, default 0)
- `time_total` (numeric, NOT NULL, default 0)
- `subtotal` (numeric, NOT NULL, default 0)
- `discounts` (numeric, NOT NULL, default 0)
- `tax` (numeric, NOT NULL, default 0)
- `total_amount` (numeric, NOT NULL, default 0)
- `time_minutes` (integer, NOT NULL, default 0)
- `created_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `settled_at` (timestamptz, NULLABLE)
- `updated_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `parent_bill_id` (uuid, NULLABLE)
- `table_label` (varchar, NULLABLE)
- `server_name` (varchar, NULLABLE)
- `server_id` (varchar, NULLABLE)
- `start_time` (timestamptz, NULLABLE)
- `end_time` (timestamptz, NULLABLE)

**Constraints**:
- PRIMARY KEY: `bill_id`
- CHECK constraints on items_total, time_total, subtotal, discounts, tax, total_amount, time_minutes
- CHECK: `settled_at_after_created_at`
- **MISSING FK**: `session_id` → `public.TableSessions.session_id`
- **MISSING FK**: `billing_id` → (self-reference or separate table?)
- **MISSING FK**: `parent_bill_id` → `billing.bills.bill_id`

**Indexes**:
- `bills_pkey` (UNIQUE on bill_id)
- `idx_bills_billing_id` (on billing_id)
- `idx_bills_session_id` (on session_id)
- `idx_bills_status` (on status)
- `idx_bills_parent_bill_id` (on parent_bill_id)
- `idx_bills_created_at` (on created_at)

### menu Schema

#### menu_items
**Columns**:
- `menu_item_id` (uuid, PK, NOT NULL) - **COMPOSITE PK**: (menu_item_id, version)
- `version` (integer, PK, NOT NULL)
- `name` (varchar, NOT NULL)
- `description` (text, NULLABLE)
- `base_price` (numeric, NOT NULL)
- `category` (varchar, NOT NULL)
- `sku` (varchar, NULLABLE)
- `is_available` (boolean, NOT NULL, default true)
- `is_combo` (boolean, NOT NULL, default false)
- `is_current_version` (boolean, NOT NULL, default true)
- `created_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `updated_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `group_name` (varchar, NULLABLE)
- `picture_url` (varchar, NULLABLE)
- `is_discountable` (boolean, NOT NULL, default true)
- `is_part_of_combo` (boolean, NOT NULL, default false)

**Constraints**:
- PRIMARY KEY: (`menu_item_id`, `version`)
- UNIQUE: `idx_menu_items_one_current_version` (on menu_item_id WHERE is_current_version = true)
- CHECK: `menu_items_base_price_check`

**Indexes**:
- `pk_menu_items` (UNIQUE on menu_item_id, version)
- `idx_menu_items_one_current_version` (UNIQUE on menu_item_id WHERE is_current_version = true)

#### combos
**Columns**:
- `combo_id` (bigint, PK, NOT NULL, SERIAL) - **TYPE MISMATCH**: Code may expect uuid
- `name` (text, NOT NULL)
- `description` (text, NULLABLE)
- `price` (numeric, NOT NULL)
- `is_discountable` (boolean, NOT NULL, default true)
- `is_available` (boolean, NOT NULL, default true)
- `version` (integer, NOT NULL, default 1)
- `is_deleted` (boolean, NOT NULL, default false)
- `picture_url` (text, NULLABLE)
- `created_by` (text, NULLABLE)
- `updated_by` (text, NULLABLE)
- `created_at` (timestamptz, NOT NULL, default now())
- `updated_at` (timestamptz, NOT NULL, default now())

**Constraints**:
- PRIMARY KEY: `combo_id`

**Indexes**:
- `combos_pkey` (UNIQUE on combo_id)

### inventory Schema

#### items
**Columns**:
- `id` (bigint, PK, NOT NULL, SERIAL)
- `name` (text, NOT NULL)
- `unit` (text, NOT NULL, default 'unit')
- `quantity` (numeric, NOT NULL, default 0)
- `reorder_level` (numeric, NOT NULL, default 10)
- `created_at` (timestamptz, NOT NULL, default now())
- `updated_at` (timestamptz, NOT NULL, default now())

**Constraints**:
- PRIMARY KEY: `id`

**Indexes**:
- `items_pkey` (UNIQUE on id)
- `ix_items_name` (on name)

**NOTE**: InventoryApi uses `inventory.inventory_items` (view or table?), but DB shows `inventory.items`

### users Schema

#### users
**Columns**:
- `user_id` (varchar, PK, NOT NULL) - **TYPE MISMATCH**: Code may expect integer or uuid
- `username` (varchar, NOT NULL, UNIQUE)
- `password_hash` (varchar, NOT NULL)
- `role` (varchar, NOT NULL, default 'Server')
- `created_at` (timestamp, NOT NULL, default now()) - **TYPE MISMATCH**: No timezone
- `updated_at` (timestamp, NOT NULL, default now()) - **TYPE MISMATCH**: No timezone
- `is_active` (boolean, NOT NULL, default true)
- `is_deleted` (boolean, NOT NULL, default false)

**Constraints**:
- PRIMARY KEY: `user_id`
- UNIQUE: `username`

**Indexes**:
- `users_pkey` (UNIQUE on user_id)
- `users_username_key` (UNIQUE on username)
- `idx_users_username` (on username WHERE is_deleted = false)
- `idx_users_role` (on role WHERE is_deleted = false)
- `idx_users_active` (on is_active WHERE is_deleted = false)
- `idx_users_created_at` (on created_at WHERE is_deleted = false)

## Critical Findings

### Missing Foreign Keys (High Priority)

1. **ord.orders.session_id** → `public.TableSessions.session_id` - **MISSING**
2. **ord.orders.shift_id** → `public.shifts.shift_id` - **MISSING**
3. **ord.order_items.menu_item_id** → `menu.menu_items.menu_item_id` - **MISSING** (type mismatch prevents FK)
4. **orders.orders.session_id** → `public.TableSessions.session_id` - **MISSING**
5. **orders.orders.billing_id** → `billing.bills.billing_id` - **MISSING**
6. **orders.orders.shift_id** → `public.shifts.shift_id` - **MISSING**
7. **orders.order_items.menu_item_id** → `menu.menu_items.menu_item_id` - **MISSING**
8. **orders.order_items.combo_id** → `menu.combos.combo_id` - **MISSING** (type mismatch: uuid vs bigint)
9. **public.TableSessions.shift_id** → `public.shifts.shift_id` - **MISSING**
10. **public.bills.shift_id** → `public.shifts.shift_id` - **MISSING**
11. **public.bills.table_session_id** → **INVALID** (references integer, but TableSessions uses uuid)
12. **public.payments.shift_id** → `public.shifts.shift_id` - **MISSING**
13. **pay.payments.billing_id** → `billing.bills.billing_id` - **MISSING**
14. **pay.payments.shift_id** → `public.shifts.shift_id` - **MISSING**
15. **pay.bill_ledger.billing_id** → `billing.bills.billing_id` - **MISSING** (type mismatch: text vs uuid)
16. **pay.bill_ledger.session_id** → `public.TableSessions.session_id` - **MISSING** (type mismatch: text vs uuid)
17. **billing.bills.session_id** → `public.TableSessions.session_id` - **MISSING**
18. **billing.bills.parent_bill_id** → `billing.bills.bill_id` - **MISSING**

### Type Mismatches (Critical)

1. **ord.order_items.menu_item_id**: `bigint` vs `menu.menu_items.menu_item_id`: `uuid`
2. **orders.order_items.combo_id**: `uuid` vs `menu.combos.combo_id`: `bigint`
3. **public.shifts.opened_by_user_id**: `varchar` vs code expects `integer`
4. **public.bills.table_session_id**: `integer` vs `public.TableSessions.session_id`: `uuid`
5. **pay.bill_ledger.billing_id**: `text` vs `billing.bills.billing_id`: `uuid`
6. **pay.bill_ledger.session_id**: `text` vs `public.TableSessions.session_id`: `uuid`
7. **users.users.user_id**: `varchar` vs code may expect `integer` or `uuid`
8. **users.users.created_at/updated_at**: `timestamp` (no timezone) vs code may expect `timestamptz`

### Schema Duplication

1. **Two orders schemas**: `ord` and `orders` - both contain orders and order_items tables
2. **Two bills tables**: `public.bills` and `billing.bills` - different structures
3. **Two payments tables**: `public.payments` and `pay.payments` - different structures

### Orphaned Tables

1. **public.Orders** (PascalCase) - Legacy EF Core table?
2. **public.OrderItems** (PascalCase) - Legacy EF Core table?
3. **public.InventoryItems** (PascalCase) - Legacy EF Core table?
4. **public.Users** (PascalCase) - Legacy EF Core table?

