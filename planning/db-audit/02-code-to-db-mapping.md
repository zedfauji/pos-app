# 02 - Code & API Expectation Map

**Status**: READ-ONLY DISCOVERY  
**Date**: 2025-12-23  
**Auditor**: Principal Database Architect

---

## Purpose

This document maps API DTOs and domain models to database tables/columns, identifying:
- Expected entities and fields
- Expected relationships
- Expected constraints
- Missing columns
- Extra columns
- Naming mismatches
- Type mismatches
- Nullability mismatches

---

## Mapping Methodology

For each API/domain aggregate:
1. Identify the DTO structure
2. Map to domain model (if exists)
3. Map to database table/column
4. Flag discrepancies

---

## 1. Tables & Sessions Domain

### API: TablesApi

#### DTO: `TableStatusDto`
**Source**: `solution/shared/DTOs/Tables/TablesDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `TableId` | `Guid` | `public.tables.table_id` (uuid) | ✅ uuid | ✅ MATCH |
| `Label` | `string` | `public.tables.table_number` | ✅ varchar | ✅ MATCH |
| `Type` | `string` | `public.tables.table_type` (int) | ⚠️ integer | ⚠️ TYPE MISMATCH |
| `TypeId` | `int` | `public.tables.type_id` (int) | ✅ integer | ✅ MATCH |
| `TypeName` | `string` | `public.table_types.name` | ✅ varchar | ✅ MATCH (via FK) |
| `Capacity` | `int` | `public.tables.capacity` | ✅ integer | ✅ MATCH |
| `IsActive` | `bool?` | `public.tables.is_active` | ✅ boolean | ✅ MATCH |
| `Occupied` | `bool` | Computed from `tables.sessions.is_active` | ✅ Computed | ✅ MATCH |
| `CurrentSessionId` | `Guid?` | `tables.sessions.session_id` | ✅ uuid | ✅ MATCH |
| `StartTime` | `DateTimeOffset?` | `tables.sessions.start_time` | ✅ timestamptz | ✅ MATCH |
| `Server` | `string?` | `tables.sessions.started_by` | ✅ varchar | ✅ MATCH |

**Issues**:
- ⚠️ `Type` is string in DTO but integer in DB (expected - uses `TypeId`)

---

#### DTO: `SessionOverview` (inferred from `ITableRepository`)
**Expected Structure**:

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `SessionId` | `Guid` | `tables.sessions.session_id` | ✅ uuid | ✅ MATCH |
| `TableId` | `Guid` | `tables.sessions.table_id` | ✅ uuid | ✅ MATCH |
| `BillingId` | `Guid` | `tables.sessions.billing_id` | ✅ uuid | ✅ MATCH |
| `StartTime` | `DateTimeOffset` | `tables.sessions.start_time` | ✅ timestamptz | ✅ MATCH |
| `EndTime` | `DateTimeOffset?` | `tables.sessions.end_time` | ✅ timestamptz | ✅ MATCH |
| `IsActive` | `bool` | `tables.sessions.is_active` | ✅ boolean | ✅ MATCH |
| `State` | `string` | `tables.sessions.state` | ✅ varchar | ✅ MATCH |
| `AccumulatedCost` | `decimal` | `tables.sessions.accumulated_cost` | ✅ numeric | ✅ MATCH |
| `GuestCount` | `int` | `tables.sessions.guest_count` | ✅ integer | ✅ MATCH |
| `StartedBy` | `string?` | `tables.sessions.started_by` | ✅ varchar | ✅ MATCH |

**Issues**: None

---

#### DTO: `BillPreviewDto`
**Source**: `solution/shared/DTOs/Tables/BillPreviewDto.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `Items` | `List<ItemLine>` | Computed from `orders.order_items` | ✅ Computed | ✅ MATCH |
| `Subtotal` | `decimal` | Computed from `orders.orders.subtotal` | ✅ Computed | ✅ MATCH |
| `TaxAmount` | `decimal` | Computed from `orders.orders.tax` | ✅ Computed | ✅ MATCH |
| `DiscountAmount` | `decimal` | Computed from `orders.orders.discount` | ✅ Computed | ✅ MATCH |
| `TotalAmount` | `decimal` | Computed from `orders.orders.total` | ✅ Computed | ✅ MATCH |

**Issues**: None

---

#### DTO: `BillResult`
**Source**: `solution/shared/DTOs/Tables/TablesDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `BillId` | `Guid` | `billing.bills.bill_id` | ✅ uuid | ✅ MATCH |
| `BillingId` | `Guid?` | `billing.bills.billing_id` | ✅ uuid | ✅ MATCH |
| `SessionId` | `Guid?` | `billing.bills.session_id` | ✅ uuid | ✅ MATCH |
| `TableLabel` | `string` | `billing.bills.table_label` | ✅ varchar | ✅ MATCH |
| `ServerId` | `string` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN |
| `ServerName` | `string` | `billing.bills.server_name` | ✅ varchar | ✅ MATCH |
| `StartTime` | `DateTime` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN |
| `EndTime` | `DateTime` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN |
| `TotalTimeMinutes` | `int` | `billing.bills.time_minutes` | ✅ integer | ✅ MATCH |
| `Items` | `List<ItemLine>` | Computed from `billing.bill_items` | ✅ Computed | ✅ MATCH |
| `TimeCost` | `decimal` | `billing.bills.time_total` | ✅ numeric | ✅ MATCH |
| `ItemsCost` | `decimal` | `billing.bills.items_total` | ✅ numeric | ✅ MATCH |
| `TotalAmount` | `decimal` | `billing.bills.total_amount` | ✅ numeric | ✅ MATCH |
| `PaymentMethod` | `PaymentMethod` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN (expected - in payments) |
| `AmountTendered` | `decimal` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN (expected - in payments) |
| `ChangeDue` | `decimal` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN (expected - computed) |
| `TipAmount` | `decimal` | Not in `billing.bills` | ❌ MISSING | ❌ MISSING COLUMN (expected - in payments) |
| `DiscountAmount` | `decimal` | `billing.bills.discounts` | ✅ numeric | ✅ MATCH |

**Issues**:
- ❌ `ServerId` not in `billing.bills` (but `ServerName` exists)
- ❌ `StartTime` and `EndTime` not in `billing.bills` (available in `tables.sessions`)

---

### API: TablesApi - BillsController

#### DTO: `BillDto` (from `BillsController.cs`)

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `BillId` | `Guid` | `billing.bills.bill_id` | ✅ uuid | ✅ MATCH |
| `BillingId` | `Guid` | `billing.bills.billing_id` | ✅ uuid | ✅ MATCH |
| `SessionId` | `Guid` | `billing.bills.session_id` | ✅ uuid | ✅ MATCH |
| `TableId` | `Guid` | `billing.bills.table_id` | ✅ uuid | ✅ MATCH |
| `TableLabel` | `string?` | `billing.bills.table_label` | ✅ varchar | ✅ MATCH |
| `ServerName` | `string?` | `billing.bills.server_name` | ✅ varchar | ✅ MATCH |
| `StartTime` | `DateTimeOffset?` | From `public.TableSessions.start_time` | ⚠️ Legacy table | ⚠️ LEGACY REFERENCE |
| `EndTime` | `DateTimeOffset?` | From `public.TableSessions.end_time` | ⚠️ Legacy table | ⚠️ LEGACY REFERENCE |
| `ItemsTotal` | `decimal` | `billing.bills.items_total` | ✅ numeric | ✅ MATCH |
| `TimeTotal` | `decimal` | `billing.bills.time_total` | ✅ numeric | ✅ MATCH |
| `Subtotal` | `decimal` | `billing.bills.subtotal` | ✅ numeric | ✅ MATCH |
| `Discounts` | `decimal` | `billing.bills.discounts` | ✅ numeric | ✅ MATCH |
| `Tax` | `decimal` | `billing.bills.tax` | ✅ numeric | ✅ MATCH |
| `TotalAmount` | `decimal` | `billing.bills.total_amount` | ✅ numeric | ✅ MATCH |
| `TimeMinutes` | `int` | `billing.bills.time_minutes` | ✅ integer | ✅ MATCH |
| `Status` | `string` | `billing.bills.status` (enum) | ✅ enum | ✅ MATCH |
| `CreatedAt` | `DateTimeOffset` | `billing.bills.created_at` | ✅ timestamptz | ✅ MATCH |

**Issues**:
- ⚠️ `BillsController` queries `public.TableSessions` for `StartTime` and `EndTime` - should use `tables.sessions`

---

## 2. Orders Domain

### API: OrderApi

#### DTO: `OrderDto`
**Source**: `solution/backend/OrderApi/Models/OrderDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `Id` | `long` | `orders.orders.order_id` (uuid) | ⚠️ uuid | ⚠️ TYPE MISMATCH |
| `SessionId` | `Guid` | `orders.orders.session_id` | ✅ uuid | ✅ MATCH |
| `TableId` | `string` | Not in `orders.orders` | ❌ MISSING | ❌ MISSING COLUMN |
| `Status` | `string` | `orders.orders.status` (enum) | ✅ enum | ✅ MATCH |
| `DeliveryStatus` | `string` | Computed from `orders.order_items.delivery_status` | ✅ Computed | ✅ MATCH |
| `Subtotal` | `decimal` | `orders.orders.subtotal` | ✅ numeric | ✅ MATCH |
| `DiscountTotal` | `decimal` | `orders.orders.discount` | ✅ numeric | ✅ MATCH |
| `TaxTotal` | `decimal` | `orders.orders.tax` | ✅ numeric | ✅ MATCH |
| `Total` | `decimal` | `orders.orders.total` | ✅ numeric | ✅ MATCH |
| `ProfitTotal` | `decimal` | Computed from `orders.order_items.profit` | ✅ Computed | ✅ MATCH |
| `Items` | `List<OrderItemDto>` | `orders.order_items` | ✅ Table exists | ✅ MATCH |

**Issues**:
- ⚠️ `Id` is `long` in DTO but `uuid` in DB (should be `Guid`)
- ❌ `TableId` not in `orders.orders` (available via `tables.sessions.table_id`)

---

#### DTO: `OrderItemDto`
**Source**: `solution/backend/OrderApi/Models/OrderDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `Id` | `long` | `orders.order_items.order_item_id` (uuid) | ⚠️ uuid | ⚠️ TYPE MISMATCH |
| `MenuItemId` | `long?` | `orders.order_items.menu_item_id` (uuid) | ⚠️ uuid | ⚠️ TYPE MISMATCH |
| `ComboId` | `long?` | Not in `orders.order_items` | ❌ MISSING | ❌ MISSING COLUMN |
| `Quantity` | `int` | `orders.order_items.quantity` | ✅ integer | ✅ MATCH |
| `DeliveredQuantity` | `int` | `orders.order_items.delivered_quantity` | ✅ integer | ✅ MATCH |
| `BasePrice` | `decimal` | `orders.order_items.base_price` | ✅ numeric | ✅ MATCH |
| `PriceDelta` | `decimal` | `orders.order_items.price_delta` | ✅ numeric | ✅ MATCH |
| `LineTotal` | `decimal` | `orders.order_items.line_total` | ✅ numeric | ✅ MATCH |
| `Profit` | `decimal` | `orders.order_items.profit` | ✅ numeric | ✅ MATCH |

**Issues**:
- ⚠️ `Id` and `MenuItemId` are `long` in DTO but `uuid` in DB (should be `Guid`)
- ❌ `ComboId` not in `orders.order_items` (may be in modifiers JSONB)

---

#### DTO: `CreateOrderRequestDto`
**Source**: `solution/backend/OrderApi/Models/OrderDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `SessionId` | `Guid` | `orders.orders.session_id` | ✅ uuid | ✅ MATCH |
| `BillingId` | `Guid?` | `orders.orders.billing_id` | ✅ uuid | ✅ MATCH |
| `TableId` | `string` | Not in `orders.orders` | ❌ MISSING | ❌ MISSING COLUMN |
| `ServerId` | `string` | Not in `orders.orders` | ❌ MISSING | ❌ MISSING COLUMN |
| `ServerName` | `string?` | Not in `orders.orders` | ❌ MISSING | ❌ MISSING COLUMN |
| `Items` | `List<CreateOrderItemDto>` | `orders.order_items` | ✅ Table exists | ✅ MATCH |

**Issues**:
- ❌ `TableId`, `ServerId`, `ServerName` not in `orders.orders` (available via `tables.sessions`)

---

## 3. Payments Domain

### API: PaymentApi

#### DTO: `BillLedgerDto`
**Source**: `solution/shared/DTOs/Payments/PaymentDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `BillingId` | `Guid` | `pay.payment_ledger.billing_id` | ✅ uuid | ✅ MATCH |
| `SessionId` | `Guid` | Not in `pay.payment_ledger` | ❌ MISSING | ❌ MISSING COLUMN |
| `TotalDue` | `decimal` | `pay.payment_ledger.total_due` | ✅ numeric | ✅ MATCH |
| `TotalDiscount` | `decimal` | `pay.payment_ledger.total_discount` | ✅ numeric | ✅ MATCH |
| `TotalPaid` | `decimal` | `pay.payment_ledger.total_paid` | ✅ numeric | ✅ MATCH |
| `TotalTip` | `decimal` | `pay.payment_ledger.total_tip` | ✅ numeric | ✅ MATCH |
| `Status` | `string` | `pay.payment_ledger.status` (enum) | ✅ enum | ✅ MATCH |

**Issues**:
- ❌ `SessionId` not in `pay.payment_ledger` (available via `billing.bills.session_id`)

---

#### DTO: `RegisterPaymentRequestDto`
**Source**: `solution/shared/DTOs/Payments/PaymentDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `SessionId` | `Guid` | `pay.payments` (indirect) | ✅ Available | ✅ MATCH |
| `BillingId` | `Guid` | `pay.payments.billing_id` | ✅ uuid | ✅ MATCH |
| `TotalDue` | `decimal?` | `pay.payment_ledger.total_due` | ✅ numeric | ✅ MATCH |
| `Lines` | `List<RegisterPaymentLineDto>` | `pay.payments` | ✅ Table exists | ✅ MATCH |
| `ServerId` | `string?` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |
| `AmountTendered` | `decimal?` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |

**Issues**:
- ❌ `ServerId` and `AmountTendered` not in `pay.payments`

---

#### DTO: `PaymentDto`
**Source**: `solution/shared/DTOs/Payments/PaymentDtos.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `PaymentId` | `Guid` | `pay.payments.payment_id` | ✅ uuid | ✅ MATCH |
| `SessionId` | `Guid` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |
| `BillingId` | `Guid` | `pay.payments.billing_id` | ✅ uuid | ✅ MATCH |
| `AmountPaid` | `decimal` | `pay.payments.amount_paid` | ✅ numeric | ✅ MATCH |
| `PaymentMethod` | `string` | `pay.payments.method` (enum) | ✅ enum | ✅ MATCH |
| `DiscountAmount` | `decimal` | `pay.payments.discount_amount` | ✅ numeric | ✅ MATCH |
| `DiscountReason` | `string?` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |
| `TipAmount` | `decimal` | `pay.payments.tip_amount` | ✅ numeric | ✅ MATCH |
| `ExternalRef` | `string?` | `pay.payments.external_reference` | ✅ varchar | ✅ MATCH |
| `Meta` | `object?` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |
| `CreatedBy` | `string?` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |
| `CreatedAt` | `DateTimeOffset` | `pay.payments.created_at` | ✅ timestamptz | ✅ MATCH |
| `Notes` | `string?` | Not in `pay.payments` | ❌ MISSING | ❌ MISSING COLUMN |

**Issues**:
- ❌ `SessionId` not in `pay.payments` (available via `billing.bills.session_id`)
- ❌ `DiscountReason`, `Meta`, `CreatedBy`, `Notes` not in `pay.payments`

---

## 4. Menu Domain

### API: MenuApi

#### DTO: `MenuItemDto`
**Source**: `solution/shared/DTOs/Menu/MenuItemDto.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `Id` | `long` | `menu.menu_items.menu_item_id` (uuid) | ⚠️ uuid | ⚠️ TYPE MISMATCH |
| `Sku` | `string` | `menu.menu_items.sku` | ✅ varchar | ✅ MATCH |
| `Name` | `string` | `menu.menu_items.name` | ✅ varchar | ✅ MATCH |
| `Description` | `string?` | `menu.menu_items.description` | ✅ text | ✅ MATCH |
| `Category` | `string` | `menu.menu_items.category` | ✅ varchar | ✅ MATCH |
| `GroupName` | `string?` | Not in `menu.menu_items` | ❌ MISSING | ❌ MISSING COLUMN |
| `SellingPrice` | `decimal` | `menu.menu_items.base_price` | ✅ numeric | ✅ MATCH |
| `Price` | `decimal?` | `menu.menu_items.base_price` | ✅ numeric | ✅ MATCH |
| `PictureUrl` | `string?` | Not in `menu.menu_items` | ❌ MISSING | ❌ MISSING COLUMN |
| `IsDiscountable` | `bool` | Not in `menu.menu_items` | ❌ MISSING | ❌ MISSING COLUMN |
| `IsPartOfCombo` | `bool` | `menu.menu_items.is_combo` | ✅ boolean | ✅ MATCH |
| `IsAvailable` | `bool` | `menu.menu_items.is_available` | ✅ boolean | ✅ MATCH |
| `Version` | `int` | `menu.menu_items.version` | ✅ integer | ✅ MATCH |

**Issues**:
- ⚠️ `Id` is `long` in DTO but `uuid` in DB (should be `Guid`)
- ❌ `GroupName`, `PictureUrl`, `IsDiscountable` not in `menu.menu_items`

---

## 5. Users Domain

### API: UsersApi

#### DTO: `UserDto`
**Source**: `solution/shared/DTOs/Users/UserModels.cs`

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `UserId` | `string` | `users.users.user_id` (varchar) | ✅ varchar | ✅ MATCH |
| `Username` | `string` | `users.users.username` | ✅ varchar | ✅ MATCH |
| `Role` | `string` | `users.users.role` | ✅ varchar | ✅ MATCH |
| `IsActive` | `bool` | `users.users.is_active` | ✅ boolean | ✅ MATCH |

**Issues**: None

---

## 6. Shifts Domain

### API: TablesApi - ShiftsController

#### DTO: `ShiftDto` (inferred)
**Expected Structure**:

| DTO Property | Type | Expected DB | Actual DB | Status |
|--------------|------|-------------|-----------|--------|
| `ShiftId` | `Guid` | `public.shifts.shift_id` | ✅ uuid | ✅ MATCH |
| `ShiftNumber` | `int` | `public.shifts.shift_number` | ✅ integer | ✅ MATCH |
| `OpenedByUserId` | `int` | `public.shifts.opened_by_user_id` | ⚠️ integer | ⚠️ TYPE MISMATCH |
| `OpenedByName` | `string?` | `public.shifts.opened_by_name` | ✅ varchar | ✅ MATCH |
| `OpenedAt` | `DateTimeOffset` | `public.shifts.opened_at` | ✅ timestamptz | ✅ MATCH |
| `StartingCash` | `decimal` | `public.shifts.starting_cash` | ✅ numeric | ✅ MATCH |
| `ClosedByUserId` | `int?` | `public.shifts.closed_by_user_id` | ⚠️ integer | ⚠️ TYPE MISMATCH |
| `ClosedAt` | `DateTimeOffset?` | `public.shifts.closed_at` | ✅ timestamptz | ✅ MATCH |
| `Status` | `string` | `public.shifts.status` | ✅ varchar | ✅ MATCH |

**Issues**:
- ⚠️ `OpenedByUserId` and `ClosedByUserId` are `int` but FK to `public.Users.Id` (integer) - should reference `users.users.user_id` (varchar)

---

## Enum Mappings

### Bill Status
| DTO Value | DB Enum | Schema | Status |
|-----------|---------|--------|--------|
| `"AwaitingPayment"` | `billing.bill_status` | `billing` | ✅ MATCH |
| `"Paid"` | `billing.bill_status` | `billing` | ✅ MATCH |
| `"Cancelled"` | `billing.bill_status` | `billing` | ✅ MATCH |

### Order Status
| DTO Value | DB Enum | Schema | Status |
|-----------|---------|--------|--------|
| `"open"` | `orders.order_status` | `orders` | ✅ MATCH |
| `"inprogress"` | `orders.order_status` | `orders` | ✅ MATCH |
| `"delivered"` | `orders.order_status` | `orders` | ✅ MATCH |
| `"closed"` | `orders.order_status` | `orders` | ✅ MATCH |

### Delivery Status
| DTO Value | DB Enum | Schema | Status |
|-----------|---------|--------|--------|
| `"pending"` | `orders.delivery_status` | `orders` | ✅ MATCH |
| `"partial"` | `orders.delivery_status` | `orders` | ✅ MATCH |
| `"delivered"` | `orders.delivery_status` | `orders` | ✅ MATCH |

### Payment Method
| DTO Value | DB Enum | Schema | Status |
|-----------|---------|--------|--------|
| `"Cash"` | `pay.payment_method` | `pay` | ✅ MATCH |
| `"Card"` | `pay.payment_method` | `pay` | ✅ MATCH |
| `"Wallet"` | `pay.payment_method` | `pay` | ✅ MATCH |
| `"External"` | `pay.payment_method` | `pay` | ✅ MATCH |

### Payment Status
| DTO Value | DB Enum | Schema | Status |
|-----------|---------|--------|--------|
| `"NotPaid"` | `pay.payment_status` | `pay` | ✅ MATCH |
| `"PartialPaid"` | `pay.payment_status` | `pay` | ✅ MATCH |
| `"Paid"` | `pay.payment_status` | `pay` | ✅ MATCH |
| `"PartialRefunded"` | `pay.payment_status` | `pay` | ✅ MATCH |
| `"Refunded"` | `pay.payment_status` | `pay` | ✅ MATCH |
| `"Cancelled"` | `pay.payment_status` | `pay` | ✅ MATCH |

---

## Summary of Issues

### Critical Issues (Must Fix)
1. ❌ **Missing Foreign Key Constraints**
   - `pay.payments.bill_id` → `billing.bills.bill_id` (FK exists but shows NULL)
   - `pay.payment_ledger.bill_id` → `billing.bills.bill_id` (FK exists but shows NULL)

2. ❌ **Type Mismatches**
   - `orders.orders.order_id` (uuid) vs DTO `Id` (long)
   - `orders.order_items.order_item_id` (uuid) vs DTO `Id` (long)
   - `orders.order_items.menu_item_id` (uuid) vs DTO `MenuItemId` (long)
   - `menu.menu_items.menu_item_id` (uuid) vs DTO `Id` (long)
   - `public.shifts.opened_by_user_id` (integer) vs `users.users.user_id` (varchar)

3. ❌ **Missing Columns**
   - `billing.bills.server_id` (string)
   - `billing.bills.start_time` (timestamptz)
   - `billing.bills.end_time` (timestamptz)
   - `orders.orders.table_id` (string)
   - `orders.orders.server_id` (string)
   - `orders.orders.server_name` (string)
   - `pay.payment_ledger.session_id` (uuid)
   - `pay.payments.session_id` (uuid)
   - `pay.payments.discount_reason` (varchar)
   - `pay.payments.meta` (jsonb)
   - `pay.payments.created_by` (varchar)
   - `pay.payments.notes` (text)
   - `menu.menu_items.group_name` (varchar)
   - `menu.menu_items.picture_url` (varchar)
   - `menu.menu_items.is_discountable` (boolean)

### Warning Issues (Should Fix)
1. ⚠️ **Legacy Table References**
   - `BillsController` queries `public.TableSessions` instead of `tables.sessions`

2. ⚠️ **Legacy Schema**
   - `ord` schema should be deprecated (0 rows, different structure)

3. ⚠️ **Legacy Tables in `public` Schema**
   - `public.TableSessions`, `public.Orders`, `public.OrderItems`, `public.bills`, `public.payments`, `public.Users`, `public.InventoryItems`

---

## Next Steps

1. **Step 3**: Perform data quality audit
2. **Step 4**: Create schema correction plan
3. **Step 5**: Create data migration plan

---

**END OF CODE-TO-DB MAPPING**

