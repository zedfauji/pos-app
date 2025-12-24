# Canonical Database Schema Truth

**Status**: AUTHORITATIVE  
**Last Updated**: 2025-12-23  
**Purpose**: Single source of truth for database schema

⚠️ **THIS DOCUMENT IS LAW** - All schema changes must align with this document.

## Schema Organization

### Primary Schemas (Active)
1. **public** - Core application tables (sessions, shifts, tables, bills, payments)
2. **orders** - Order management (authoritative orders schema)
3. **billing** - Billing and bill management
4. **pay** - Payment processing and ledger
5. **menu** - Menu items, combos, modifiers
6. **inventory** - Inventory management
7. **users** - User authentication and authorization
8. **settings** - System settings
9. **customers** - Customer management
10. **discounts** - Discounts and campaigns
11. **audit** - Audit logging

### Deprecated Schemas (To Be Removed)
1. **ord** - Legacy orders schema (duplicate of orders schema)
2. **tables** - Legacy table schema (if exists)
3. **sync** - Legacy sync schema (if exists)

## Canonical Table Definitions

### public.TableSessions
**Purpose**: Table session tracking
**Primary Key**: `session_id` (uuid)
**Foreign Keys**:
- `shift_id` → `public.shifts.shift_id` (uuid, NULLABLE) - **REQUIRED FK**

**Columns**:
```sql
session_id uuid PRIMARY KEY NOT NULL,
table_label text NOT NULL,
server_id text,
server_name text,
start_time timestamptz NOT NULL DEFAULT now(),
end_time timestamptz,
status text NOT NULL DEFAULT 'active',
billing_id uuid,
items jsonb,  -- Legacy, to be removed
shift_id uuid REFERENCES public.shifts(shift_id)
```

**Indexes**:
- PRIMARY KEY: `session_id`
- INDEX: `idx_tablesessions_status` (status)
- INDEX: `idx_tablesessions_label` (table_label)
- INDEX: `idx_tablesessions_shift_id` (shift_id) - **REQUIRED**

### public.shifts
**Purpose**: Shift management and cash reconciliation
**Primary Key**: `shift_id` (uuid)
**Unique Constraint**: `shift_number` (integer)

**Columns**:
```sql
shift_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
shift_number integer UNIQUE NOT NULL,
opened_by_user_id varchar NOT NULL,  -- REFERENCES users.users(user_id)
opened_by_name varchar(100),
opened_at timestamptz NOT NULL DEFAULT now(),
starting_cash numeric(10,2) NOT NULL,
closed_by_user_id varchar,
closed_by_name varchar(100),
closed_at timestamptz,
declared_cash numeric(10,2),
expected_cash numeric(10,2),
difference numeric(10,2),
difference_category varchar(50),
close_reason text,
status varchar(20) NOT NULL DEFAULT 'open',
idempotency_key uuid
```

**Foreign Keys**:
- `opened_by_user_id` → `users.users.user_id` (varchar) - **REQUIRED FK**
- `closed_by_user_id` → `users.users.user_id` (varchar) - **REQUIRED FK**

**Indexes**:
- PRIMARY KEY: `shift_id`
- UNIQUE: `shift_number`
- INDEX: `idx_shifts_status` (status)
- INDEX: `idx_shifts_opened_at` (opened_at)
- INDEX: `idx_shifts_opened_by` (opened_by_user_id)

**Constraints**:
- CHECK: Only one row with status = 'open'
- CHECK: `closed_at >= opened_at`

### orders.orders
**Purpose**: Order management (AUTHORITATIVE orders table)
**Primary Key**: `order_id` (uuid)

**Columns**:
```sql
order_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
billing_id uuid NOT NULL,  -- REFERENCES billing.bills(billing_id)
session_id uuid NOT NULL,  -- REFERENCES public.TableSessions(session_id)
status orders.order_status NOT NULL DEFAULT 'open',
subtotal numeric NOT NULL DEFAULT 0,
discount numeric NOT NULL DEFAULT 0,
tax numeric NOT NULL DEFAULT 0,
tip numeric NOT NULL DEFAULT 0,
total numeric NOT NULL DEFAULT 0,
created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
delivered_at timestamptz,
closed_at timestamptz,
updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
note text,
table_id varchar,
server_id varchar,
server_name varchar,
delivery_status varchar NOT NULL DEFAULT 'pending',
profit_total numeric NOT NULL DEFAULT 0,
shift_id uuid,  -- REFERENCES public.shifts(shift_id)
is_deleted boolean NOT NULL DEFAULT false
```

**Foreign Keys**:
- `session_id` → `public.TableSessions.session_id` (uuid) - **REQUIRED FK**
- `billing_id` → `billing.bills.billing_id` (uuid) - **REQUIRED FK**
- `shift_id` → `public.shifts.shift_id` (uuid) - **REQUIRED FK**

**Indexes**:
- PRIMARY KEY: `order_id`
- INDEX: `idx_orders_billing_id` (billing_id)
- INDEX: `idx_orders_session_id` (session_id)
- INDEX: `idx_orders_status` (status)
- INDEX: `idx_orders_created_at` (created_at)
- INDEX: `idx_orders_shift_id` (shift_id)
- INDEX: `idx_orders_delivery_status` (delivery_status)
- INDEX: `idx_orders_is_deleted` (is_deleted)

### orders.order_items
**Purpose**: Order line items
**Primary Key**: `order_item_id` (uuid)

**Columns**:
```sql
order_item_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
order_id uuid NOT NULL REFERENCES orders.orders(order_id),
menu_item_id uuid NOT NULL,  -- REFERENCES menu.menu_items(menu_item_id) - composite PK handling required
menu_item_version integer NOT NULL,
name varchar NOT NULL,
base_price numeric NOT NULL,
price_delta numeric NOT NULL DEFAULT 0,
vendor_price numeric NOT NULL DEFAULT 0,
line_total numeric NOT NULL,
profit numeric NOT NULL,
quantity integer NOT NULL,
delivered_quantity integer NOT NULL DEFAULT 0,
delivery_status orders.delivery_status NOT NULL DEFAULT 'pending',
special_instructions text,
created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
note text,
modifiers jsonb DEFAULT '[]'::jsonb,
original_session_id uuid,
combo_id bigint,  -- REFERENCES menu.combos(combo_id) - TYPE MISMATCH TO FIX
snapshot_name varchar,
snapshot_sku varchar,
snapshot_category varchar,
snapshot_group varchar,
snapshot_version integer,
snapshot_picture_url text,
line_discount numeric NOT NULL DEFAULT 0,
is_deleted boolean NOT NULL DEFAULT false
```

**Foreign Keys**:
- `order_id` → `orders.orders.order_id` (uuid) - **EXISTS**
- `menu_item_id` → `menu.menu_items.menu_item_id` (uuid) - **REQUIRED FK** (handle composite PK)
- `combo_id` → `menu.combos.combo_id` (bigint) - **REQUIRED FK** (after type fix)

**Indexes**:
- PRIMARY KEY: `order_item_id`
- INDEX: `idx_order_items_order_id` (order_id)
- INDEX: `idx_order_items_menu_item_id` (menu_item_id)
- INDEX: `idx_order_items_is_deleted` (is_deleted)

### billing.bills
**Purpose**: Bill management (AUTHORITATIVE bills table)
**Primary Key**: `bill_id` (uuid)

**Columns**:
```sql
bill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
billing_id uuid NOT NULL UNIQUE,  -- Business identifier
session_id uuid NOT NULL,  -- REFERENCES public.TableSessions(session_id)
table_id uuid NOT NULL,
status billing.bill_status NOT NULL DEFAULT 'AwaitingPayment',
items_total numeric NOT NULL DEFAULT 0,
time_total numeric NOT NULL DEFAULT 0,
subtotal numeric NOT NULL DEFAULT 0,
discounts numeric NOT NULL DEFAULT 0,
tax numeric NOT NULL DEFAULT 0,
total_amount numeric NOT NULL DEFAULT 0,
time_minutes integer NOT NULL DEFAULT 0,
created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
settled_at timestamptz,
updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
parent_bill_id uuid,  -- REFERENCES billing.bills(bill_id) - self-reference for splits
table_label varchar,
server_name varchar,
server_id varchar,
start_time timestamptz,
end_time timestamptz
```

**Foreign Keys**:
- `session_id` → `public.TableSessions.session_id` (uuid) - **REQUIRED FK**
- `parent_bill_id` → `billing.bills.bill_id` (uuid) - **REQUIRED FK** (self-reference)

**Indexes**:
- PRIMARY KEY: `bill_id`
- UNIQUE: `billing_id`
- INDEX: `idx_bills_billing_id` (billing_id)
- INDEX: `idx_bills_session_id` (session_id)
- INDEX: `idx_bills_status` (status)
- INDEX: `idx_bills_parent_bill_id` (parent_bill_id)
- INDEX: `idx_bills_created_at` (created_at)

### pay.payments
**Purpose**: Payment processing (AUTHORITATIVE payments table)
**Primary Key**: `payment_id` (uuid)

**Columns**:
```sql
payment_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
bill_id uuid NOT NULL,  -- REFERENCES billing.bills(bill_id)
billing_id uuid NOT NULL,  -- REFERENCES billing.bills(billing_id)
amount_paid numeric NOT NULL DEFAULT 0,
discount_amount numeric NOT NULL DEFAULT 0,
tip_amount numeric NOT NULL DEFAULT 0,
method pay.payment_method NOT NULL,
status pay.payment_status NOT NULL DEFAULT 'Paid',
external_reference varchar,
created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
cancelled_at timestamptz,
provider varchar,
authorization_code varchar,
card_last4 varchar,
is_settled boolean NOT NULL DEFAULT false,
shift_id uuid,  -- REFERENCES public.shifts(shift_id)
session_id uuid REFERENCES public.TableSessions(session_id),
discount_reason varchar,
meta jsonb,
created_by varchar,
notes text
```

**Foreign Keys**:
- `bill_id` → `billing.bills.bill_id` (uuid) - **REQUIRED FK**
- `billing_id` → `billing.bills.billing_id` (uuid) - **REQUIRED FK**
- `session_id` → `public.TableSessions.session_id` (uuid) - **EXISTS**
- `shift_id` → `public.shifts.shift_id` (uuid) - **REQUIRED FK**

**Indexes**:
- PRIMARY KEY: `payment_id`
- INDEX: `idx_payments_bill_id` (bill_id)
- INDEX: `idx_payments_billing_id` (billing_id)
- INDEX: `idx_payments_external_ref` (UNIQUE on billing_id, external_reference WHERE external_reference IS NOT NULL)
- INDEX: `ix_payments_billing` (billing_id)
- INDEX: `idx_payments_created_at` (created_at)
- INDEX: `idx_payments_shift_id` (shift_id) - **REQUIRED**

### pay.bill_ledger
**Purpose**: Payment ledger tracking
**Primary Key**: `billing_id` (uuid) - **FIX TYPE FROM TEXT**

**Columns**:
```sql
billing_id uuid PRIMARY KEY,  -- REFERENCES billing.bills(billing_id) - FIX FROM TEXT
session_id uuid NOT NULL,  -- REFERENCES public.TableSessions(session_id) - FIX FROM TEXT
total_due numeric NOT NULL DEFAULT 0.00,
total_discount numeric NOT NULL DEFAULT 0.00,
total_paid numeric NOT NULL DEFAULT 0.00,
total_tip numeric NOT NULL DEFAULT 0.00,
status text NOT NULL DEFAULT 'unpaid',
updated_at timestamptz NOT NULL DEFAULT now()
```

**Foreign Keys**:
- `billing_id` → `billing.bills.billing_id` (uuid) - **REQUIRED FK** (after type fix)
- `session_id` → `public.TableSessions.session_id` (uuid) - **REQUIRED FK** (after type fix)

**Indexes**:
- PRIMARY KEY: `billing_id`
- INDEX: `ix_bill_ledger_status` (status)

### menu.menu_items
**Purpose**: Menu item catalog
**Primary Key**: (`menu_item_id`, `version`) - Composite PK

**Columns**:
```sql
menu_item_id uuid NOT NULL,
version integer NOT NULL,
name varchar NOT NULL,
description text,
base_price numeric NOT NULL,
category varchar NOT NULL,
sku varchar,
is_available boolean NOT NULL DEFAULT true,
is_combo boolean NOT NULL DEFAULT false,
is_current_version boolean NOT NULL DEFAULT true,
created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
updated_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
group_name varchar,
picture_url varchar,
is_discountable boolean NOT NULL DEFAULT true,
is_part_of_combo boolean NOT NULL DEFAULT false,
PRIMARY KEY (menu_item_id, version)
```

**Constraints**:
- UNIQUE: Only one row per menu_item_id with is_current_version = true

**Indexes**:
- PRIMARY KEY: (`menu_item_id`, `version`)
- UNIQUE: `idx_menu_items_one_current_version` (menu_item_id WHERE is_current_version = true)

### menu.combos
**Purpose**: Combo meal definitions
**Primary Key**: `combo_id` (bigint) - **CONSIDER CHANGING TO UUID**

**Columns**:
```sql
combo_id bigint PRIMARY KEY,  -- CONSIDER UUID for consistency
name text NOT NULL,
description text,
price numeric NOT NULL,
is_discountable boolean NOT NULL DEFAULT true,
is_available boolean NOT NULL DEFAULT true,
version integer NOT NULL DEFAULT 1,
is_deleted boolean NOT NULL DEFAULT false,
picture_url text,
created_by text,
updated_by text,
created_at timestamptz NOT NULL DEFAULT now(),
updated_at timestamptz NOT NULL DEFAULT now()
```

**Indexes**:
- PRIMARY KEY: `combo_id`

### users.users
**Purpose**: User authentication
**Primary Key**: `user_id` (varchar)

**Columns**:
```sql
user_id varchar PRIMARY KEY,
username varchar NOT NULL UNIQUE,
password_hash varchar NOT NULL,
role varchar NOT NULL DEFAULT 'Server',
created_at timestamp NOT NULL DEFAULT now(),  -- CONSIDER timestamptz
updated_at timestamp NOT NULL DEFAULT now(),  -- CONSIDER timestamptz
is_active boolean NOT NULL DEFAULT true,
is_deleted boolean NOT NULL DEFAULT false
```

**Indexes**:
- PRIMARY KEY: `user_id`
- UNIQUE: `username`
- INDEX: `idx_users_username` (username WHERE is_deleted = false)
- INDEX: `idx_users_role` (role WHERE is_deleted = false)
- INDEX: `idx_users_active` (is_active WHERE is_deleted = false)
- INDEX: `idx_users_created_at` (created_at WHERE is_deleted = false)

## Canonical Relationships

### Session → Order → Payment Flow
```
public.TableSessions (session_id)
    ↓
orders.orders (session_id FK, billing_id FK)
    ↓
orders.order_items (order_id FK, menu_item_id FK)
    ↓
billing.bills (session_id FK, billing_id)
    ↓
pay.payments (billing_id FK, session_id FK)
    ↓
pay.bill_ledger (billing_id FK, session_id FK)
```

### Shift Gating
```
public.shifts (shift_id)
    ↓
public.TableSessions (shift_id FK)
    ↓
orders.orders (shift_id FK)
    ↓
pay.payments (shift_id FK)
    ↓
public.bills (shift_id FK) - DEPRECATED, use billing.bills
```

## Immutability Rules

1. **Closed Shifts**: Once `public.shifts.status = 'closed'`, shift is immutable
2. **Settled Bills**: Once `billing.bills.status = 'Settled'`, bill is immutable
3. **Finalized Payments**: Once `pay.payments.status = 'Settled'`, payment is immutable
4. **Closed Orders**: Once `orders.orders.status = 'closed'`, order is immutable

## Naming Conventions

- **Primary Keys**: `{table_name}_id` (e.g., `order_id`, `session_id`)
- **Foreign Keys**: `{referenced_table}_id` (e.g., `shift_id`, `session_id`)
- **Timestamps**: `created_at`, `updated_at`, `closed_at`, `settled_at` (all timestamptz)
- **Status Fields**: Use enum types where possible, text otherwise
- **Soft Delete**: `is_deleted` (boolean, default false)

## Required Migrations

1. **Remove ord schema** - Migrate data to orders schema, update all references
2. **Remove public.bills** - Migrate data to billing.bills, update all references
3. **Remove public.payments** - Migrate data to pay.payments, update all references
4. **Fix pay.bill_ledger types** - Change billing_id and session_id from text to uuid
5. **Fix orders.order_items.combo_id** - Change from uuid to bigint OR change menu.combos.combo_id to uuid
6. **Add all missing foreign keys** - See mismatch matrix
7. **Remove legacy PascalCase tables** - public.Orders, public.OrderItems, etc.

## Schema Authority

**This document is the single source of truth.**
- All API code must align with these definitions
- All migrations must preserve these relationships
- All new tables must follow these conventions
- Violations must be documented and fixed immediately

