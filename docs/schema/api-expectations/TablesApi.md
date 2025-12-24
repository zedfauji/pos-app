# TablesApi Schema Expectations

**API**: TablesApi  
**Location**: `solution/backend/TablesApi`  
**Repository**: `MagiDesk.Infrastructure.Repositories.TableRepository`

## Tables Used

### public.TableSessions
**Expected Columns**:
- `session_id` (uuid, PK, NOT NULL)
- `table_label` (text, NOT NULL)
- `server_id` (text, NULLABLE)
- `server_name` (text, NULLABLE)
- `start_time` (timestamptz, NOT NULL, default now())
- `end_time` (timestamptz, NULLABLE)
- `status` (text, NOT NULL, default 'active')
- `billing_id` (uuid, NULLABLE)
- `items` (jsonb, NULLABLE) - Legacy JSON items
- `shift_id` (uuid, NULLABLE) - Foreign key to public.shifts

**Code References**:
- `TableRepository.cs:29-57` - GetAllAsync() query
- `TableRepository.cs:59-84` - GetActiveSessionsAsync() query
- `TableRepository.cs:120-122` - StartSessionAsync() INSERT
- `TableRepository.cs:146` - StopSessionAsync() UPDATE

### public.tables
**Expected Columns**:
- `table_id` (uuid, PK)
- `table_name` (text) - Maps to `table_label` in TableSessions
- `type_id` (integer) - Foreign key to public.table_types
- `capacity` (integer, NULLABLE)
- `is_active` (boolean, NULLABLE)

**Code References**:
- `TableRepository.cs:44` - JOIN in GetAllAsync()
- `TableRepository.cs:500-507` - AddTableAsync() INSERT
- `TableRepository.cs:529-534` - UpdateTableAsync() UPDATE

### public.table_types
**Expected Columns**:
- `id` (integer, PK)
- `name` (text)
- `has_timer` (boolean)
- `hourly_rate` (numeric)
- `allow_orders` (boolean)
- `requires_server` (boolean)

**Code References**:
- `TableRepository.cs:45` - JOIN in GetAllAsync()
- `TableRepository.cs:482-496` - GetTableTypesAsync()
- `TableRepository.cs:556-579` - UpdateTableTypeAsync()

### ord.orders
**Expected Columns**:
- `order_id` (uuid, PK)
- `session_id` (uuid, NOT NULL) - Foreign key to public.TableSessions.session_id
- `table_label` (text, NOT NULL)
- `created_at` (timestamptz, NOT NULL, default now())
- `status` (text, NOT NULL, default 'submitted')
- `is_deleted` (boolean, NOT NULL, default false)
- `shift_id` (uuid, NULLABLE) - Foreign key to public.shifts

**Code References**:
- `TableRepository.cs:72-78` - JOIN in GetActiveSessionsAsync() for totals calculation
- `TableRepository.cs:263-269` - INSERT in MoveSessionAsync()
- `TableRepository.cs:340-349` - JOIN in GetBillPreviewAsync()
- `TableRepository.cs:427-430` - JOIN in EndSessionAsync() for total calculation

### ord.order_items
**Expected Columns**:
- `order_item_id` (uuid, PK)
- `order_id` (uuid, NOT NULL) - Foreign key to ord.orders.order_id
- `menu_item_id` (bigint, NOT NULL) - **NOTE**: Type mismatch - menu.menu_items uses uuid
- `quantity` (integer, NOT NULL, default 1)
- `base_price` (numeric(12,2), NOT NULL, default 0.00)
- `price_delta` (numeric(12,2), NOT NULL, default 0.00)
- `is_deleted` (boolean, NOT NULL, default false)
- `created_at` (timestamptz, NOT NULL, default now())
- `delivered_quantity` (integer, NOT NULL, default 0)
- `status` (text, NOT NULL, default 'pending')
- `snapshot_name` (text, NULLABLE)

**Code References**:
- `TableRepository.cs:74-77` - JOIN in GetActiveSessionsAsync() for totals
- `TableRepository.cs:275-278` - INSERT in MoveSessionAsync() for time cost
- `TableRepository.cs:347-349` - JOIN in GetBillPreviewAsync()
- `TableRepository.cs:428-430` - JOIN in EndSessionAsync() for total

### public.shifts
**Expected Columns**:
- `shift_id` (uuid, PK)
- `shift_number` (integer, UNIQUE, NOT NULL)
- `opened_by_user_id` (integer, NOT NULL) - Foreign key to public.Users.Id
- `opened_by_name` (varchar(100), NULLABLE)
- `opened_at` (timestamptz, NOT NULL, default now())
- `starting_cash` (decimal(10,2), NOT NULL)
- `closed_by_user_id` (integer, NULLABLE)
- `closed_by_name` (varchar(100), NULLABLE)
- `closed_at` (timestamptz, NULLABLE)
- `declared_cash` (decimal(10,2), NULLABLE)
- `expected_cash` (decimal(10,2), NULLABLE)
- `difference` (decimal(10,2), NULLABLE)
- `difference_category` (varchar(50), NULLABLE)
- `close_reason` (text, NULLABLE)
- `status` (varchar(20), NOT NULL, default 'open')
- `idempotency_key` (uuid, NULLABLE)

**Code References**:
- `ShiftRepository.cs:38-64` - GetCurrentOpenShiftAsync()
- `ShiftRepository.cs:153-159` - HasActiveTablesAsync() - checks TableSessions.shift_id

### public.bills
**Expected Columns**:
- `bill_id` (uuid, PK)
- `shift_id` (uuid, NOT NULL) - Foreign key to public.shifts.shift_id
- `table_session_id` (integer, NOT NULL) - Foreign key to public.TableSessions.Id (INTEGER)
- `total_amount` (numeric, NOT NULL)
- `status` (varchar(20), NOT NULL, default 'unsettled')
- `created_at` (timestamptz, NOT NULL, default now())
- `created_by_user_id` (integer, NULLABLE) - Foreign key to public.Users.Id

**Code References**:
- `ShiftRepository.cs:162-168` - HasUnsettledBillsAsync()
- `TableRepository.cs:441-463` - INSERT in EndSessionAsync() - **NOTE**: Uses billing.bills, not public.bills

### billing.bills
**Expected Columns**:
- `bill_id` (uuid, PK)
- `billing_id` (uuid, NOT NULL)
- `session_id` (uuid, NOT NULL) - Foreign key to public.TableSessions.session_id
- `table_id` (uuid, NOT NULL)
- `table_label` (varchar, NULLABLE)
- `server_name` (varchar, NULLABLE)
- `items_total` (numeric, NOT NULL, default 0)
- `time_total` (numeric, NOT NULL, default 0)
- `subtotal` (numeric, NOT NULL, default 0)
- `discounts` (numeric, NOT NULL, default 0)
- `tax` (numeric, NOT NULL, default 0)
- `total_amount` (numeric, NOT NULL, default 0)
- `time_minutes` (integer, NOT NULL, default 0)
- `status` (billing.bill_status enum, NOT NULL, default 'AwaitingPayment')
- `created_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `settled_at` (timestamptz, NULLABLE)
- `updated_at` (timestamptz, NOT NULL, default CURRENT_TIMESTAMP)
- `parent_bill_id` (uuid, NULLABLE)
- `server_id` (varchar, NULLABLE)
- `start_time` (timestamptz, NULLABLE)
- `end_time` (timestamptz, NULLABLE)

**Code References**:
- `TableRepository.cs:441-463` - INSERT in EndSessionAsync()

### public.table_session_moves
**Expected Columns**:
- `move_id` (bigserial, PK)
- `session_id` (uuid, NOT NULL) - Foreign key to public.TableSessions.session_id
- `from_label` (text, NOT NULL)
- `to_label` (text, NOT NULL)
- `moved_at` (timestamptz, NOT NULL, default now())

**Code References**:
- `TableRepository.cs:297` - INSERT in MoveSessionAsync()

## PK Expectations

1. **public.TableSessions**: `session_id` (uuid) - PRIMARY KEY
2. **public.tables**: `table_id` (uuid) - PRIMARY KEY
3. **public.table_types**: `id` (integer) - PRIMARY KEY
4. **ord.orders**: `order_id` (uuid) - PRIMARY KEY
5. **ord.order_items**: `order_item_id` (uuid) - PRIMARY KEY
6. **public.shifts**: `shift_id` (uuid) - PRIMARY KEY, `shift_number` (integer) - UNIQUE
7. **public.bills**: `bill_id` (uuid) - PRIMARY KEY
8. **billing.bills**: `bill_id` (uuid) - PRIMARY KEY

## FK Expectations

### Explicit Foreign Keys (Expected)
1. **ord.order_items.order_id** → `ord.orders.order_id` - **EXISTS** (confirmed in DB)
2. **public.TableSessions.shift_id** → `public.shifts.shift_id` - **EXPECTED** (code references it)
3. **ord.orders.shift_id** → `public.shifts.shift_id` - **EXPECTED** (code references it)
4. **public.bills.shift_id** → `public.shifts.shift_id` - **EXPECTED** (code references it)
5. **public.bills.table_session_id** → `public.TableSessions.Id` (INTEGER) - **EXPECTED** (but TableSessions uses uuid session_id, not integer Id)
6. **public.bills.created_by_user_id** → `public.Users.Id` - **EXPECTED** (confirmed in DB)
7. **billing.bills.session_id** → `public.TableSessions.session_id` - **EXPECTED** (implicit join)

### Missing Foreign Keys (Implicit Joins)
1. **ord.orders.session_id** → `public.TableSessions.session_id` - **IMPLICIT** (no FK constraint, but used in JOINs)
2. **public.tables.type_id** → `public.table_types.id` - **IMPLICIT** (no FK constraint, but used in JOINs)

## Join Paths

1. **GetAllAsync()**: `public.tables` LEFT JOIN `public.table_types` ON `tables.type_id = table_types.id` LEFT JOIN `public.TableSessions` ON `tables.table_name = TableSessions.table_label AND TableSessions.status = 'active'`

2. **GetActiveSessionsAsync()**: `public.TableSessions` LEFT JOIN subquery on `ord.orders` JOIN `ord.order_items` ON `order_items.order_id = orders.order_id`

3. **GetBillPreviewAsync()**: `public.TableSessions` JOIN `ord.orders` ON `orders.session_id = TableSessions.session_id` JOIN `ord.order_items` ON `order_items.order_id = orders.order_id`

4. **MoveSessionAsync()**: `public.TableSessions` JOIN `public.tables` JOIN `public.table_types` for config lookup

## Cross-API Dependencies

1. **OrderApi**: TablesApi reads from `ord.orders` and `ord.order_items` (shared schema)
2. **PaymentApi**: TablesApi creates bills in `billing.bills` which PaymentApi uses
3. **MenuApi**: TablesApi references `menu.menu_items` via `ord.order_items.menu_item_id` (but type mismatch: bigint vs uuid)

## Assumptions & Risks

### HIGH RISK
1. **Type Mismatch**: `ord.order_items.menu_item_id` is `bigint` but `menu.menu_items.menu_item_id` is `uuid`. This will cause JOIN failures.
   - **Location**: `TableRepository.cs:340` comment mentions "Cannot join menu.menu_items because menu_item_id types differ"
   - **Impact**: Cannot get menu item names/prices from menu schema

2. **Schema Confusion**: Two `bills` tables exist:
   - `public.bills` (used by ShiftRepository)
   - `billing.bills` (used by TableRepository.EndSessionAsync)
   - **Risk**: Code may write to wrong table

3. **TableSessions Primary Key Mismatch**: 
   - Code expects `session_id` (uuid) as PK
   - But `public.bills.table_session_id` references `TableSessions.Id` (INTEGER)
   - **Risk**: Foreign key constraint will fail or reference wrong column

### MEDIUM RISK
1. **Missing FK on ord.orders.session_id**: No foreign key constraint, relies on application logic
2. **Missing FK on public.tables.type_id**: No foreign key constraint, relies on application logic
3. **Legacy JSON Items**: `TableSessions.items` (jsonb) is still used alongside SQL-based orders

### LOW RISK
1. **Soft Delete Pattern**: Uses `is_deleted` boolean flag instead of actual DELETE
2. **Status Field**: Uses text instead of enum type for status values

