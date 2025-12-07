# Caja System - Codebase Analysis

**Date:** 2025-01-27  
**Branch:** `implement-caja-system`  
**Status:** Analysis Complete

## Executive Summary

This document provides a comprehensive analysis of the MagiDesk POS codebase in preparation for implementing a complete "Caja System" (Cash Register System) with opening/closing functionality. The analysis covers database architecture, transaction flows, RBAC implementation, UI structure, and identifies all areas that require modification.

---

## 1. Database Architecture Analysis

### Current Schema Organization

The system uses **PostgreSQL 17** with **9 schemas** (one per service domain):

1. `users` - User management and RBAC
2. `ord` - Orders and order items
3. `pay` - Payments, refunds, bill ledger
4. `public` - Tables, sessions, bills
5. `inventory` - Inventory items, stock, transactions
6. `customers` - Customer data, wallets, loyalty
7. `discounts` - Campaigns, vouchers, offers
8. `settings` - Application settings
9. `menu` - Menu items and configurations

### Key Tables Relevant to Caja System

#### Payment Tables (`pay` schema)
- **`pay.payments`**: Individual payment records
  - Fields: `payment_id`, `session_id`, `billing_id`, `amount_paid`, `payment_method`, `discount_amount`, `tip_amount`, `created_by`, `created_at`
  - **Impact**: All payments must link to `caja_session_id`

- **`pay.bill_ledger`**: Aggregated bill state
  - Fields: `billing_id`, `session_id`, `total_due`, `total_discount`, `total_paid`, `total_tip`, `status`
  - **Impact**: Used for calculating system totals per caja session

- **`pay.refunds`**: Refund records
  - Fields: `refund_id`, `payment_id`, `billing_id`, `refund_amount`, `refund_method`, `created_by`
  - **Impact**: Refunds must be tracked per caja session

#### Order Tables (`ord` schema)
- **`ord.orders`**: Order records
  - Fields: `order_id`, `session_id`, `billing_id`, `table_id`, `server_id`, `status`, `total`, `created_at`
  - **Impact**: Orders must check for active caja before creation

- **`ord.order_items`**: Order line items
  - **Impact**: Indirectly affected through orders

#### Table/Session Tables (`public` schema)
- **`public.table_sessions`**: Active table sessions
  - Fields: `session_id`, `table_label`, `server_id`, `start_time`, `end_time`, `status`, `billing_id`
  - **Impact**: Cannot close caja if active sessions exist

- **`public.bills`**: Generated bills
  - Fields: `bill_id`, `session_id`, `total_amount`, `status`, `created_at`
  - **Impact**: Bills must be linked to caja sessions

### Migration Strategy

**Pattern Observed:**
- Each API has a `DatabaseInitializer` service that runs on startup
- Migrations are executed via SQL files or embedded resources
- Schema creation uses `CREATE SCHEMA IF NOT EXISTS`
- Tables use `CREATE TABLE IF NOT EXISTS` for safety

**Recommended Approach:**
- Create new `caja` schema for isolation
- Use `DatabaseInitializer` pattern for consistency
- Implement safe migrations with rollback capability

---

## 2. Transaction Flow Analysis

### Payment Flow

```
1. Table Session Starts → Creates session_id
2. Orders Created → Linked to session_id
3. Table Session Stops → Creates billing_id
4. Payment Registered → Links to billing_id and session_id
5. Bill Ledger Updated → Aggregates totals
```

**Current Flow Issues:**
- ❌ No caja session validation
- ❌ No caja_session_id tracking
- ❌ Payments can be processed without open caja

**Required Changes:**
- ✅ Add caja session check before payment registration
- ✅ Link all payments to `caja_session_id`
- ✅ Block payments if no active caja

### Order Flow

```
1. User selects table
2. Creates order → Links to session_id
3. Adds items → Updates order totals
4. Order status changes → open → waiting → delivered → closed
```

**Current Flow Issues:**
- ❌ No caja validation before order creation
- ❌ Orders can be created without open caja

**Required Changes:**
- ✅ Validate active caja before order creation
- ✅ Link orders to `caja_session_id` (via session_id relationship)

### Refund Flow

```
1. User initiates refund
2. Refund created → Links to payment_id and billing_id
3. Bill ledger updated → Reduces total_paid
```

**Current Flow Issues:**
- ❌ No caja validation for refunds
- ❌ Refunds not tracked per caja session

**Required Changes:**
- ✅ Validate active caja before refund
- ✅ Track refunds per caja session

---

## 3. RBAC Analysis

### Current RBAC Implementation

**System Roles:**
1. **Owner** - Full access (all 47 permissions)
2. **Admin** - Administrative access
3. **Manager** - Management operations
4. **Server** - Order and table management
5. **Cashier** - Payment processing
6. **Host** - Table management only

**Permission Categories (47 permissions across 11 categories):**
- User Management (5)
- Order Management (6)
- Payment Management (5)
- Table Management (4)
- Menu Management (6)
- Inventory Management (5)
- Reports & Analytics (4)
- System Settings (3)
- Customer Management (4)
- Reservation Management (2)
- Vendor Management (3)

### Required RBAC Changes

**New Permissions Needed:**
- `caja:open` - Open caja session
- `caja:close` - Close caja session
- `caja:view` - View caja status and reports
- `caja:view_history` - View historical caja sessions

**Role Permissions:**
- **Admin**: All caja permissions
- **Manager**: `caja:open`, `caja:close`, `caja:view`, `caja:view_history`
- **Cashier**: `caja:close` (if open), `caja:view`
- **Server**: None (cannot open/close, but operations blocked if no caja)

### RBAC Integration Points

**Backend:**
- `RequiresPermissionAttribute` already implemented
- `PermissionRequirementHandler` in shared library
- `IRbacService` interface for permission checks

**Frontend:**
- Permission-based UI visibility (needs implementation)
- Role-based navigation (needs caja menu items)

---

## 4. Backend API Structure

### Current API Modules

1. **PaymentApi** (`solution/backend/PaymentApi/`)
   - Port: 5005
   - Schema: `pay`
   - **Impact**: HIGH - Must add caja validation

2. **OrderApi** (`solution/backend/OrderApi/`)
   - Port: 5004
   - Schema: `ord`
   - **Impact**: HIGH - Must add caja validation

3. **TablesApi** (`solution/backend/TablesApi/`)
   - Port: 5010
   - Schema: `public`
   - **Impact**: MEDIUM - Check for active sessions before caja close

4. **UsersApi** (`solution/backend/UsersApi/`)
   - Port: 5001
   - Schema: `users`
   - **Impact**: LOW - RBAC already implemented

### Service Patterns

**Common Pattern:**
```csharp
public interface IService
{
    Task<ResultDto> OperationAsync(RequestDto request, CancellationToken ct);
}

public class Service : IService
{
    private readonly IRepository _repository;
    // Implementation
}
```

**Repository Pattern:**
- Uses `NpgsqlDataSource` for connections
- Transaction support via `ExecuteInTransactionAsync`
- Dapper or raw SQL queries

### Middleware Structure

**Current Middleware:**
- `UserIdExtractionMiddleware` - Extracts user ID from headers
- `AuthorizationExceptionHandlerMiddleware` - Handles auth errors

**Required New Middleware:**
- `CajaSessionValidationMiddleware` - Validates active caja for protected endpoints

---

## 5. Frontend Structure Analysis

### WinUI 3 Architecture

**Project Structure:**
```
frontend/
├── Views/              # 70+ XAML pages
├── ViewModels/         # 27 ViewModels
├── Services/           # 51+ Services
├── Dialogs/            # 24+ Modal dialogs
├── Models/             # Data models
└── Assets/             # Images, icons
```

### Key Pages Affected

1. **MainPage.xaml** - Navigation hub
   - **Impact**: Add "Caja" menu item
   - **Location**: `solution/frontend/Views/MainPage.xaml`

2. **TablesPage** - Table management
   - **Impact**: Show caja status indicator
   - **Location**: `solution/frontend/Views/TablesPage.xaml`

3. **PaymentPage** - Payment processing
   - **Impact**: Validate caja before payment
   - **Location**: `solution/frontend/Views/PaymentPage.xaml`

4. **OrdersManagementPage** - Order management
   - **Impact**: Validate caja before order creation
   - **Location**: `solution/frontend/Views/OrdersManagementPage.xaml`

### Navigation Structure

**Current Menu Items:**
- Dashboard
- Tables
- Orders Management
- Billing
- Payments
- Menu Management
- Inventory Management
- Cash Flow
- Users
- Settings

**Required Additions:**
- Caja (with submenu: Open, Close, Reports)

### UI Patterns

**Dialog Pattern:**
- Uses `ContentDialog` for modals
- ViewModels handle business logic
- Services handle API calls

**Service Pattern:**
- API services in `Services/` folder
- Uses `HttpClient` for API calls
- DTOs match backend models

---

## 6. Existing Caja-Like Logic

### Search Results

**Found References:**
- `cash_flow` in `I18nService.cs` (Spanish translation: "Flujo de caja")
- No existing caja session management
- No caja opening/closing logic

### Cash Flow vs Caja

**Current Cash Flow (`inventory.cash_flow` table):**
- Simple cash amount tracking
- Employee name, date, cash amount, notes
- **NOT** a session-based system
- **NOT** linked to transactions

**Caja System Requirements:**
- Session-based (open/close)
- Links to all transactions
- Calculates system totals
- Tracks differences
- Enforces business rules

**Conclusion:** Cash flow is separate and should remain. Caja system is new functionality.

---

## 7. Critical Assumptions That Must Change

### Current Assumptions

1. **❌ Payments can be processed anytime**
   - **Change**: Require active caja session

2. **❌ Orders can be created anytime**
   - **Change**: Require active caja session

3. **❌ Refunds can be processed anytime**
   - **Change**: Require active caja session

4. **❌ No session tracking for cash operations**
   - **Change**: All transactions linked to caja session

5. **❌ No opening/closing workflow**
   - **Change**: Mandatory open/close workflow

### Business Rules to Enforce

1. **Only one active caja session at a time**
2. **Cannot close caja with open table sessions**
3. **Cannot close caja with pending payments**
4. **All transactions require active caja**
5. **Manager/Admin required for open/close**

---

## 8. Model Fields That Need Extension

### Payment Models

**Current:**
```csharp
public record PaymentDto(
    Guid PaymentId,
    Guid SessionId,
    Guid BillingId,
    // ... other fields
);
```

**Required:**
```csharp
public record PaymentDto(
    Guid PaymentId,
    Guid SessionId,
    Guid BillingId,
    Guid CajaSessionId,  // NEW
    // ... other fields
);
```

### Order Models

**Current:**
```csharp
public record OrderDto(
    long Id,
    Guid SessionId,
    // ... other fields
);
```

**Required:**
- Link via `session_id` → `table_sessions` → `caja_session_id` (indirect)
- Or add direct `caja_session_id` field

### Bill Ledger

**Current:**
```csharp
public record BillLedgerDto(
    Guid BillingId,
    Guid SessionId,
    // ... totals
);
```

**Required:**
- Add `CajaSessionId` for filtering and reporting

---

## 9. Functions That Must Enforce Caja Session State

### PaymentApi

**`PaymentService.RegisterPaymentAsync`**
- ✅ Check for active caja session
- ✅ Link payment to caja_session_id
- ❌ Currently: No validation

**`PaymentService.ProcessRefundAsync`**
- ✅ Check for active caja session
- ✅ Track refund in caja transactions
- ❌ Currently: No validation

### OrderApi

**`OrderService.CreateOrderAsync`**
- ✅ Check for active caja session
- ❌ Currently: No validation

**`OrderService.UpdateOrderAsync`**
- ✅ Check for active caja session (if modifying totals)
- ❌ Currently: No validation

### TablesApi

**`TablesService.StopSessionAsync`**
- ✅ Check for active caja before allowing bill generation
- ❌ Currently: No validation

---

## 10. Database Schema Design

### New Schema: `caja`

**Table: `caja.caja_sessions`**
```sql
CREATE TABLE caja.caja_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    opened_by_user_id TEXT NOT NULL,  -- FK to users.users
    closed_by_user_id TEXT NULL,      -- FK to users.users
    opened_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    closed_at TIMESTAMPTZ NULL,
    opening_amount NUMERIC(10,2) NOT NULL,
    closing_amount NUMERIC(10,2) NULL,
    system_calculated_total NUMERIC(10,2) NULL,
    difference NUMERIC(10,2) NULL,
    status TEXT NOT NULL CHECK (status IN ('open', 'closed')) DEFAULT 'open',
    notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_caja_sessions_status ON caja.caja_sessions(status);
CREATE INDEX idx_caja_sessions_opened_at ON caja.caja_sessions(opened_at);
CREATE UNIQUE INDEX idx_caja_sessions_active ON caja.caja_sessions(status) WHERE status = 'open';
```

**Table: `caja.caja_transactions`**
```sql
CREATE TABLE caja.caja_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    caja_session_id UUID NOT NULL REFERENCES caja.caja_sessions(id) ON DELETE CASCADE,
    transaction_id UUID NOT NULL,  -- References payment_id, order_id, or refund_id
    transaction_type TEXT NOT NULL CHECK (transaction_type IN ('sale', 'refund', 'tip', 'deposit', 'withdrawal')),
    amount NUMERIC(10,2) NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_caja_transactions_session ON caja.caja_transactions(caja_session_id);
CREATE INDEX idx_caja_transactions_type ON caja.caja_transactions(transaction_type);
```

### Schema Modifications

**`pay.payments`**
```sql
ALTER TABLE pay.payments 
ADD COLUMN caja_session_id UUID NULL REFERENCES caja.caja_sessions(id);

CREATE INDEX idx_payments_caja_session ON pay.payments(caja_session_id);
```

**`ord.orders`**
```sql
-- Optional: Add direct link, or use session_id relationship
ALTER TABLE ord.orders 
ADD COLUMN caja_session_id UUID NULL REFERENCES caja.caja_sessions(id);

CREATE INDEX idx_orders_caja_session ON ord.orders(caja_session_id);
```

---

## 11. Impact Report on Existing Modules

### High Impact Modules

| Module | Impact Level | Changes Required |
|--------|-------------|-------------------|
| PaymentApi | **HIGH** | Add caja validation, link payments to caja |
| OrderApi | **HIGH** | Add caja validation, link orders to caja |
| TablesApi | **MEDIUM** | Check active sessions before caja close |
| Frontend PaymentPage | **HIGH** | Add caja validation UI, show status |
| Frontend OrdersPage | **HIGH** | Add caja validation UI, show status |
| Frontend MainPage | **MEDIUM** | Add Caja menu item |

### Medium Impact Modules

| Module | Impact Level | Changes Required |
|--------|-------------|-------------------|
| UsersApi | **MEDIUM** | Add caja permissions to RBAC |
| Frontend TablesPage | **MEDIUM** | Show caja status indicator |
| Frontend Navigation | **MEDIUM** | Add Caja menu items |

### Low Impact Modules

| Module | Impact Level | Changes Required |
|--------|-------------|-------------------|
| CustomerApi | **LOW** | None (indirect via payments) |
| MenuApi | **LOW** | None |
| InventoryApi | **LOW** | None |
| SettingsApi | **LOW** | None |

---

## 12. Edge Cases Identified

### Critical Edge Cases

1. **Multiple Users Opening Caja Simultaneously**
   - Solution: Database unique constraint on active status

2. **Closing Caja with Open Tables**
   - Solution: Validation check before close

3. **Closing Caja with Pending Payments**
   - Solution: Validation check before close

4. **Transaction During Caja Close**
   - Solution: Lock mechanism or status check

5. **System Crash During Caja Open**
   - Solution: Recovery mechanism to detect orphaned sessions

6. **Negative Differences**
   - Solution: Allow but require manager override with notes

7. **Historical Data Migration**
   - Solution: Create "legacy" caja session for existing data

---

## 13. Testing Strategy

### Unit Tests Required

1. **CajaService Tests**
   - Open caja validation
   - Close caja validation
   - Calculate totals
   - Check active sessions

2. **PaymentService Tests**
   - Caja validation before payment
   - Link payment to caja

3. **OrderService Tests**
   - Caja validation before order
   - Link order to caja

### Integration Tests Required

1. **End-to-End Caja Flow**
   - Open → Process transactions → Close

2. **Error Scenarios**
   - Payment without caja
   - Close with open tables
   - Multiple open attempts

### UI Tests Required

1. **Caja Modals**
   - Open caja dialog
   - Close caja dialog
   - Reports page

2. **Status Indicators**
   - Caja open banner
   - Blocked operations

---

## 14. Documentation Requirements

### Developer Documentation

1. **Architecture Document**
   - System design
   - Database schema
   - API endpoints
   - Flow diagrams

2. **Integration Guide**
   - How to add caja validation
   - How to link transactions
   - Error handling

3. **Testing Guide**
   - Test scenarios
   - Test data setup
   - Edge cases

### User Documentation

1. **User Guide**
   - How to open caja
   - How to close caja
   - How to view reports

2. **Troubleshooting**
   - Common issues
   - Error messages
   - Recovery procedures

---

## 15. Next Steps

### Phase 1: Database & Backend Core
1. Create `caja` schema and tables
2. Implement `CajaService`
3. Add caja validation to PaymentApi
4. Add caja validation to OrderApi

### Phase 2: RBAC & Middleware
1. Add caja permissions
2. Implement caja validation middleware
3. Update role permissions

### Phase 3: Frontend Core
1. Create CajaService (API client)
2. Create CajaViewModel
3. Create Open/Close dialogs
4. Add navigation menu items

### Phase 4: Integration
1. Update existing pages with caja validation
2. Add status indicators
3. Implement reports page

### Phase 5: Testing & Documentation
1. Write unit tests
2. Write integration tests
3. Write UI tests
4. Create documentation

---

## Conclusion

The codebase analysis reveals a well-structured POS system with clear separation of concerns. The caja system implementation will require:

- **New database schema** (`caja`)
- **New backend service** (`CajaService`)
- **Middleware for validation**
- **RBAC permission updates**
- **Frontend UI components**
- **Integration with existing flows**

All changes can be implemented without breaking existing functionality by using safe migration patterns and backward-compatible API changes.

**Estimated Complexity:** High  
**Estimated Timeline:** 2-3 weeks  
**Risk Level:** Medium (well-isolated changes)

---

**Next Document:** [02-implementation-plan.md](./02-implementation-plan.md)
