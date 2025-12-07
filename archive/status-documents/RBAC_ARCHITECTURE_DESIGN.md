# RBAC Architecture Design Document
## MagiDesk POS System - Complete Role-Based Access Control Implementation Plan

**Document Version:** 1.0  
**Date:** 2025-01-02  
**Status:** Analysis Complete - Awaiting Approval

---

## EXECUTIVE SUMMARY

This document provides a comprehensive analysis and implementation plan for a complete Role-Based Access Control (RBAC) system for the MagiDesk POS application. The system consists of a WinUI 3 desktop frontend and multiple ASP.NET Core backend APIs, with PostgreSQL as the database.

**Key Findings:**
- Backend RBAC infrastructure is **partially implemented** (UsersApi with RbacService, Permissions class, database schema exists)
- Frontend has **minimal authorization** (basic role checking in MainPage only)
- **No permission-based access control** in ViewModels, commands, or UI elements
- **No authorization middleware** in backend APIs
- **70+ views/pages** require protection
- **27 ViewModels** with commands need permission checks
- **38 backend controllers** across 9 APIs need authorization
- **Production APIs are deployed** and must remain backward compatible

**Recommended Approach:** Hybrid RBAC with centralized permission store, backend-enforced authorization, and frontend shadow enforcement for UX. **API versioning (v1/v2)** ensures zero breaking changes to existing deployments.

---

## STAGE 1: FULL CODEBASE ANALYSIS

### 1.1 Frontend Structure Analysis

#### Views/Pages Identified (70+ pages)
**Core Operations:**
- `LoginPage` - Authentication entry point
- `MainPage` - Navigation hub with NavigationView
- `DashboardPage` / `ModernDashboardPage` - Main dashboard
- `TablesPage` - Table management and layout
- `OrdersPage` / `OrdersManagementPage` - Order management
- `BillingPage` - Billing operations
- `PaymentPage` / `AllPaymentsPage` - Payment processing
- `MenuSelectionPage` - Menu item selection
- `SessionsPage` - Session management

**Management Pages:**
- `MenuManagementPage` / `EnhancedMenuManagementPage` - Menu CRUD
- `InventoryManagementPage` / `InventoryCrudPage` - Inventory operations
- `CustomerManagementPage` / `CustomerDashboardPage` / `CustomerDetailsPage` - Customer management
- `VendorsManagementPage` / `VendorDetailsPage` / `VendorsInventoryPage` / `VendorOrdersPage` - Vendor operations
- `UsersPage` - User management (currently admin-only via basic check)
- `CashFlowPage` - Cash flow tracking
- `RestockPage` - Inventory restocking

**Settings Pages:**
- `HierarchicalSettingsPage` - System settings (with category navigation)
- `GeneralSettingsPage`, `PosSettingsPage`, `InventorySettingsPage`, `PaymentsSettingsPage`, `PrinterSettingsPage`, `ReceiptSettingsPage`, `NotificationsSettingsPage`, `SecuritySettingsPage`, `IntegrationsSettingsPage`, `SystemSettingsPage`, `CustomersSettingsPage` - Category-specific settings

**Analytics & Reports:**
- `AuditReportsPage` - Audit logs
- `CampaignManagementPage` / `CampaignEditDialog` / `CampaignAnalyticsDialog` - Marketing campaigns
- `SegmentDashboardPage` / `SegmentEditDialog` / `SegmentCustomersDialog` - Customer segmentation
- `DiscountManagementPage` / `DiscountEditDialog` / `DiscountAnalyticsDialog` / `DiscountDemoPage` - Discount management
- `WalletManagementPage` / `VoucherEditDialog` - Customer wallet/vouchers

**Dialogs (24 dialogs):**
- `BillSummaryDialog`, `BillDetailsDialog`, `ReopenBillDialog`
- `MenuItemCrudDialog`, `MenuItemDialog`, `MenuSelectionDialog`
- `ModifierCrudDialog`, `ModifierOptionCrudDialog`
- `TableStatusDialog`, `SessionRecoveryDialog`
- `VendorCrudDialog`, `VendorDetailsDialog`
- `OrderDetailsDialog`, `OrderReceiptDialog`
- `PurchaseOrderDialog`, `RestockRequestDialog`, `StockAdjustmentDialog`
- `ReportGenerationDialog`
- And more...

#### ViewModels Identified (27 ViewModels)
All ViewModels use `RelayCommand` or `AsyncRelayCommand` pattern:
- `UsersViewModel` - User CRUD operations
- `OrdersViewModel` / `OrdersManagementViewModel` / `OrderDetailViewModel` - Order operations
- `PaymentViewModel` / `AllPaymentsViewModel` - Payment operations
- `BillingViewModel` / `UnsettledBillsViewModel` - Billing operations
- `MenuViewModel` / `MenuAnalyticsViewModel` / `MenuItemDetailsViewModel` - Menu operations
- `InventoryManagementViewModel` / `InventoryCrudViewModel` / `InventorySettingsViewModel` / `InventoryViewModel` - Inventory operations
- `CashFlowViewModel` - Cash flow operations
- `DashboardViewModel` / `ModernDashboardViewModel` - Dashboard data
- `VendorsManagementViewModel` / `VendorOrdersViewModel` - Vendor operations
- `HierarchicalSettingsViewModel` / `SettingsViewModel` / `ReceiptSettingsViewModel` - Settings operations
- `ReceiptFormatDesignerViewModel` - Receipt design
- `RestockViewModel` - Restock operations
- `AuditReportsViewModel` - Audit reports

#### Services Identified (51 services)
**API Services:**
- `UserApiService` - User management API
- `MenuApiService` / `MenuAnalyticsService` / `MenuBulkOperationService` - Menu APIs
- `PaymentApiService` - Payment API
- `OrderApiService` - Order API
- `VendorOrdersApiService` - Vendor orders API
- `CustomerApiService` / `CustomerIntelligenceService` - Customer APIs
- `SettingsApiService` / `HierarchicalSettingsApiService` - Settings APIs
- `InventorySettingsService` - Inventory settings
- `ApiService` - Legacy backend API
- `TablesApiService` - Tables API

**Business Logic Services:**
- `SessionService` - **Current user session storage** (stores UserId, Username, Role, LastLoginAt)
- `BillingService` / `BillingDiscountService` - Billing operations
- `ReceiptService` / `ReceiptBuilder` / `ReceiptFormatter` - Receipt generation
- `InventoryService` - Inventory operations
- `VendorService` / `VendorOrderService` - Vendor operations
- `DiscountService` - Discount operations
- `RestockService` - Restock operations
- `AuditService` - Audit logging
- `NotificationService` - Notifications
- `HeartbeatService` - Session heartbeat

#### Current Authentication Flow
1. User logs in via `LoginPage.xaml.cs`
2. `UserApiService.LoginAsync()` calls `/api/auth/login`
3. Backend returns `LoginResponse` with `UserId`, `Username`, `Role`, `LastLoginAt`
4. `SessionService.SaveAsync()` stores session locally (JSON file)
5. `SessionService.Current` provides access to current user (but **NO permissions**)
6. `MainPage.xaml.cs` checks role string for admin (lines 94-107) to show/hide Users menu item
7. **No other permission checks exist in frontend**

### 1.2 Backend Structure Analysis

#### Backend APIs (9 microservices)
1. **UsersApi** - User management, authentication, RBAC
2. **MenuApi** - Menu item management
3. **OrderApi** - Order processing
4. **PaymentApi** - Payment processing
5. **InventoryApi** - Inventory management
6. **SettingsApi** - System settings
7. **CustomerApi** - Customer management
8. **DiscountApi** - Discount management
9. **TablesApi** - Table/session management

#### Controllers Identified (38 controllers)
**UsersApi:**
- `AuthController` - Login, credential validation
- `UsersController` - User CRUD
- `RolesController` - Role management
- `RbacController` - RBAC operations
- `DebugController` - Debug endpoints

**Other APIs:**
- `MenuItemsController`, `ModifiersController`, `CombosController`, `MenuVersioningController`, `MenuBulkOperationController`, `MenuAnalyticsController`, `HistoryController` (MenuApi)
- `OrdersController`, `OrderLogsController` (OrderApi)
- `PaymentsController` (PaymentApi)
- `VendorsController`, `ItemsController`, `StockController`, `RestockController`, `CashFlowController`, `VendorOrdersController`, `AuditController`, `ReportsController`, `OrdersController`, `DbController` (InventoryApi)
- `SettingsController`, `HierarchicalSettingsController` (SettingsApi)
- `CustomersController`, `CustomerSegmentsController`, `CampaignsController`, `CommunicationsController`, `LoyaltyController`, `MembershipController`, `WalletController`, `BehavioralTriggersController` (CustomerApi)
- `DiscountsController`, `HealthController` (DiscountApi)

#### Current Backend Authorization
- **NO authorization middleware** in any API
- **NO permission checks** in controllers
- **NO user context** passed to controllers
- All endpoints are **publicly accessible** (only authentication required, no authorization)

#### Database Schema (PostgreSQL)
**Existing RBAC Tables (in `users` schema):**
- `users.roles` - Role definitions (role_id, name, description, is_system_role, is_active, created_at, updated_at, is_deleted)
- `users.role_permissions` - Role-to-permission mapping (role_id, permission) - **Many-to-many**
- `users.role_inheritance` - Role inheritance (child_role_id, parent_role_id) - **Many-to-many**
- `users.users` - User accounts (user_id, username, password_hash, role, created_at, updated_at, is_active, is_deleted)

**System Roles Defined:**
- `system-owner` - Full access (all permissions)
- `system-administrator` - Admin operations
- `system-manager` - Store operations
- `system-server` - Order taking, table management
- `system-cashier` - Payment processing
- `system-host` - Customer seating, reservations

**Permissions Defined (47 permissions in `Permissions.cs`):**
- User Management: `user:view`, `user:create`, `user:update`, `user:delete`, `user:manage_roles`
- Order Management: `order:view`, `order:create`, `order:update`, `order:delete`, `order:cancel`, `order:complete`
- Table Management: `table:view`, `table:manage`, `table:assign`, `table:clear`
- Menu Management: `menu:view`, `menu:create`, `menu:update`, `menu:delete`, `menu:price_change`
- Payment Management: `payment:view`, `payment:process`, `payment:refund`, `payment:void`
- Inventory Management: `inventory:view`, `inventory:update`, `inventory:adjust`, `inventory:restock`
- Reports: `report:view`, `report:sales`, `report:inventory`, `report:user_activity`, `report:export`
- Settings: `settings:view`, `settings:update`, `settings:system`, `settings:receipt`
- Customer Management: `customer:view`, `customer:create`, `customer:update`, `customer:delete`
- Reservation Management: `reservation:view`, `reservation:create`, `reservation:update`, `reservation:cancel`
- Vendor Management: `vendor:view`, `vendor:create`, `vendor:update`, `vendor:delete`, `vendor:order`
- Session Management: `session:view`, `session:manage`, `session:close`

### 1.3 Business Logic Operations Requiring Protection

#### Data Read Operations (View Permissions)
- Viewing users, orders, tables, menu items, inventory, customers, vendors, payments, sessions, reports, settings
- Dashboard data aggregation
- Analytics and reports

#### Data Write Operations (Create/Update Permissions)
- Creating/updating users, orders, menu items, inventory, customers, vendors
- Processing payments, refunds, voids
- Managing table assignments, sessions
- Adjusting inventory, restocking
- Updating system settings

#### Data Delete Operations (Delete Permissions)
- Deleting users, orders, menu items, customers, vendors
- Canceling orders, reservations
- Closing sessions

#### Administrative Operations (Special Permissions)
- User role management (`user:manage_roles`)
- System settings (`settings:system`)
- Receipt configuration (`settings:receipt`)
- Price changes (`menu:price_change`)
- Payment voids (`payment:void`)
- Inventory adjustments (`inventory:adjust`)

### 1.4 Access Control Requirements Map

#### Page-Level Access Control
**High Priority (Administrative):**
- `UsersPage` → `user:view`
- `HierarchicalSettingsPage` (System category) → `settings:system`
- `SecuritySettingsPage` → `settings:system` + `user:manage_roles`
- `AuditReportsPage` → `report:user_activity`

**Medium Priority (Management):**
- `MenuManagementPage` → `menu:view`
- `InventoryManagementPage` → `inventory:view`
- `CustomerManagementPage` → `customer:view`
- `VendorsManagementPage` → `vendor:view`
- `OrdersManagementPage` → `order:view`
- `AllPaymentsPage` → `payment:view`
- `SessionsPage` → `session:view`
- `CashFlowPage` → `report:view`

**Low Priority (Operational):**
- `TablesPage` → `table:view`
- `OrdersPage` → `order:view`
- `PaymentPage` → `payment:view`
- `MenuSelectionPage` → `menu:view`
- `BillingPage` → `order:view` + `payment:view`

#### Command-Level Access Control
**Create Operations:**
- `CreateUserCommand` → `user:create`
- `CreateOrderCommand` → `order:create`
- `CreateMenuItemCommand` → `menu:create`
- `CreateCustomerCommand` → `customer:create`
- `CreateVendorCommand` → `vendor:create`
- `ProcessPaymentCommand` → `payment:process`
- `StartSessionCommand` → `session:manage`

**Update Operations:**
- `EditUserCommand` → `user:update`
- `UpdateOrderCommand` → `order:update`
- `UpdateMenuItemCommand` → `menu:update`
- `ChangePriceCommand` → `menu:price_change`
- `UpdateInventoryCommand` → `inventory:update`
- `AdjustInventoryCommand` → `inventory:adjust`
- `UpdateSettingsCommand` → `settings:update`

**Delete Operations:**
- `DeleteUserCommand` → `user:delete`
- `DeleteOrderCommand` → `order:delete`
- `DeleteMenuItemCommand` → `menu:delete`
- `CancelOrderCommand` → `order:cancel`
- `DeleteCustomerCommand` → `customer:delete`
- `DeleteVendorCommand` → `vendor:delete`

**Special Operations:**
- `RefundPaymentCommand` → `payment:refund`
- `VoidPaymentCommand` → `payment:void`
- `CloseSessionCommand` → `session:close`
- `AssignRoleCommand` → `user:manage_roles`
- `ExportReportCommand` → `report:export`

#### UI Element Visibility Control
**Navigation Items:**
- "Users" menu item → `user:view`
- "Settings" menu item → `settings:view`
- "Inventory" menu item → `inventory:view`
- "Reports" menu item → `report:view`

**Command Bar Buttons:**
- "Add" button → Context-dependent (create permission)
- "Edit" button → Context-dependent (update permission)
- "Delete" button → Context-dependent (delete permission)

**Action Buttons:**
- "Process Payment" → `payment:process`
- "Refund" → `payment:refund`
- "Void" → `payment:void`
- "Start Session" → `session:manage`
- "Close Session" → `session:close`
- "Adjust Inventory" → `inventory:adjust`
- "Change Price" → `menu:price_change`

---

## STAGE 2: RBAC OPTIONS EVALUATION

### 2.1 Option 1: Centralized Role → Permission Matrix (RECOMMENDED)

**Description:** Store roles and permissions in PostgreSQL with many-to-many relationships. Backend enforces authorization, frontend caches permissions for UX.

**Pros:**
- ✅ **Already partially implemented** (database schema exists, RbacService exists)
- ✅ **Scalable** - Easy to add new permissions/roles
- ✅ **Flexible** - Supports role inheritance, custom roles
- ✅ **Centralized** - Single source of truth in database
- ✅ **Auditable** - Can track permission changes
- ✅ **Postgres-compatible** - Uses existing schema
- ✅ **Backend-enforced** - Security at API level
- ✅ **Frontend shadow** - Good UX with cached permissions

**Cons:**
- ⚠️ Requires permission caching in frontend
- ⚠️ Need to sync permissions on login/role change

**Best For:** Enterprise applications, multi-tenant systems, complex permission requirements.

**Fit Score:** 9/10 - **EXCELLENT FIT**

### 2.2 Option 2: Claims-Based Authorization

**Description:** Use JWT tokens with claims, ASP.NET Core authorization policies.

**Pros:**
- ✅ Standard ASP.NET Core pattern
- ✅ Stateless (no session storage)
- ✅ Works well with microservices

**Cons:**
- ❌ **Not implemented** - Would require JWT infrastructure
- ❌ Token size grows with permissions
- ❌ Token refresh complexity
- ❌ Frontend needs to parse/validate tokens
- ❌ Doesn't fit current session-based auth

**Best For:** Stateless APIs, distributed systems, OAuth2/OIDC integration.

**Fit Score:** 4/10 - **POOR FIT** (requires major refactoring)

### 2.3 Option 3: Attribute-Based Authorization

**Description:** Use `[Authorize(Policy = "PermissionName")]` attributes on controllers/actions.

**Pros:**
- ✅ Declarative, easy to read
- ✅ ASP.NET Core native
- ✅ Can combine with policy-based auth

**Cons:**
- ⚠️ Requires policy registration for each permission
- ⚠️ Still needs backend permission checking
- ⚠️ Doesn't solve frontend authorization

**Best For:** Simple applications, API-only systems.

**Fit Score:** 6/10 - **MODERATE FIT** (good for backend, doesn't solve frontend)

### 2.4 Option 4: Route-Level Guards Only

**Description:** Only protect navigation, no command/UI element protection.

**Pros:**
- ✅ Simple to implement
- ✅ Minimal code changes

**Cons:**
- ❌ **Insufficient security** - Users can still access features via direct API calls
- ❌ Poor UX - Buttons visible but disabled
- ❌ Not enterprise-grade

**Best For:** Prototypes, low-security applications.

**Fit Score:** 3/10 - **POOR FIT** (insufficient)

### 2.5 Option 5: Enum-Based Roles (No Permissions)

**Description:** Use simple role enum, check role strings in code.

**Pros:**
- ✅ Very simple
- ✅ Type-safe (if using enum)

**Cons:**
- ❌ **Not flexible** - Hard to add new roles/permissions
- ❌ **Not scalable** - Code changes for every permission
- ❌ **Current approach** - Already insufficient
- ❌ No fine-grained control

**Best For:** Very simple applications, fixed role sets.

**Fit Score:** 2/10 - **POOR FIT** (current approach, needs replacement)

### 2.6 Option 6: Hybrid Local Caching + Backend Enforcement

**Description:** Backend enforces permissions via API, frontend caches permissions locally for UX, refreshes on login/role change.

**Pros:**
- ✅ **Best of both worlds** - Security + Performance
- ✅ Works offline (cached permissions)
- ✅ Fast UI updates (no API calls for every check)
- ✅ Backend is source of truth
- ✅ Fits WinUI 3 desktop app pattern

**Cons:**
- ⚠️ Cache invalidation complexity
- ⚠️ Need sync mechanism

**Best For:** Desktop applications, offline-capable apps, WinUI 3 apps.

**Fit Score:** 10/10 - **PERFECT FIT** ✅

### 2.7 Option 7: Policy-Based Authorization Patterns

**Description:** ASP.NET Core policies with requirement handlers, combine with permission checks.

**Pros:**
- ✅ Flexible policy composition
- ✅ Can combine multiple requirements
- ✅ Testable

**Cons:**
- ⚠️ More complex setup
- ⚠️ Still needs permission infrastructure
- ⚠️ Doesn't solve frontend

**Best For:** Complex authorization rules, multi-requirement scenarios.

**Fit Score:** 7/10 - **GOOD FIT** (complements Option 1)

### 2.8 Evaluation Summary

| Option | Backend Fit | Frontend Fit | Implementation Effort | Maintainability | Security | **Overall Score** |
|--------|------------|--------------|----------------------|-----------------|----------|-------------------|
| **1. Centralized Matrix** | 9/10 | 8/10 | Medium | 9/10 | 9/10 | **8.8/10** ✅ |
| **6. Hybrid Caching** | 9/10 | 10/10 | Medium | 8/10 | 9/10 | **9.0/10** ✅✅ |
| **3. Attribute-Based** | 8/10 | 4/10 | Low | 7/10 | 8/10 | 6.8/10 |
| **7. Policy-Based** | 8/10 | 4/10 | High | 8/10 | 9/10 | 7.2/10 |
| **2. Claims-Based** | 6/10 | 5/10 | Very High | 7/10 | 8/10 | 6.5/10 |
| **4. Route Guards Only** | 3/10 | 5/10 | Low | 4/10 | 3/10 | 3.8/10 |
| **5. Enum Roles** | 2/10 | 3/10 | Low | 2/10 | 4/10 | 2.8/10 |

**WINNER: Option 6 (Hybrid Local Caching + Backend Enforcement) combined with Option 1 (Centralized Permission Matrix)**

---

## STAGE 3: SELECTED RBAC MODEL

### 3.1 Chosen Architecture: **Hybrid RBAC with Centralized Permission Store**

**Core Components:**
1. **Backend:** Centralized permission store in PostgreSQL, RbacService for permission evaluation, authorization middleware for API protection
2. **Frontend:** PermissionManager service with local caching, permission-based UI visibility, command enablement, navigation guards
3. **Integration:** Permission sync on login, refresh on role change, backend always source of truth

### 3.2 Why This Model?

#### Technical Justification

1. **Leverages Existing Infrastructure:**
   - Database schema already exists (`users.roles`, `users.role_permissions`)
   - `RbacService` already implemented
   - `Permissions` class already defined
   - Minimal new code required

2. **Fits WinUI 3 + MVVM Pattern:**
   - PermissionManager can be injected into ViewModels
   - Commands can check permissions before execution
   - XAML bindings can use permission converters
   - Navigation can be guarded

3. **Desktop App Optimizations:**
   - Local caching = fast UI updates (no network latency)
   - Works offline (cached permissions)
   - Reduces API calls (only sync on login/role change)

4. **Security + UX Balance:**
   - Backend enforces (security)
   - Frontend shadows (UX)
   - Best of both worlds

5. **Scalable & Maintainable:**
   - Add new permissions = update `Permissions.cs` + database
   - No code changes for new roles (database-driven)
   - Easy to audit (all in database)

6. **Postgres-Compatible:**
   - Uses existing schema
   - Efficient queries with indexes
   - Supports role inheritance

#### Why Alternatives Are Inferior

**Claims-Based (Option 2):**
- ❌ Requires JWT infrastructure (not implemented)
- ❌ Doesn't fit session-based auth
- ❌ Token refresh complexity
- ❌ Major refactoring required

**Enum Roles (Option 5):**
- ❌ Current approach - already insufficient
- ❌ Not flexible (code changes for permissions)
- ❌ No fine-grained control

**Route Guards Only (Option 4):**
- ❌ Insufficient security
- ❌ Poor UX
- ❌ Users can bypass via API

**Attribute-Based Only (Option 3):**
- ❌ Doesn't solve frontend
- ❌ Still needs permission infrastructure
- ❌ Partial solution

### 3.3 Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND (WinUI 3)                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐      ┌──────────────────┐                │
│  │ SessionService│─────▶│ PermissionManager│                │
│  │ (Current User)│      │ (Cached Perms)   │                │
│  └──────────────┘      └──────────────────┘                │
│         │                      │                            │
│         │                      ├──▶ ViewModels              │
│         │                      ├──▶ Commands                │
│         │                      ├──▶ XAML Bindings           │
│         │                      └──▶ Navigation Guards        │
│         │                                                      │
│         └──────────────────────────────────────────────────┐  │
│                                                            │  │
│  ┌──────────────┐      ┌──────────────────┐               │  │
│  │ UserApiService│─────▶│ /api/auth/login │               │  │
│  └──────────────┘      │ /api/rbac/perms  │               │  │
│                        └──────────────────┘               │  │
│                                                              │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            │ HTTP/HTTPS
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                 BACKEND (ASP.NET Core)                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐      ┌──────────────────┐                │
│  │ AuthController│      │  RbacController  │                │
│  │  (Login)     │      │ (Get Permissions)│                │
│  └──────────────┘      └──────────────────┘                │
│         │                      │                            │
│         │                      ▼                            │
│         │              ┌──────────────────┐                  │
│         │              │   RbacService    │                  │
│         │              │ (Permission Eval)│                  │
│         │              └──────────────────┘                  │
│         │                      │                            │
│         └──────────────────────┼──────────────────────────┐ │
│                                │                          │ │
│                        ┌───────▼────────┐                  │ │
│                        │ Authorization │                  │ │
│                        │  Middleware   │                  │ │
│                        └───────┬───────┘                  │ │
│                                │                          │ │
│                        ┌───────▼───────┐                  │ │
│                        │  Controllers  │                  │ │
│                        │  (Protected)  │                  │ │
│                        └───────────────┘                  │ │
│                                                              │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            │ Npgsql
                            │
┌───────────────────────────▼─────────────────────────────────┐
│              DATABASE (PostgreSQL)                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  users.users          users.roles                           │
│  users.role_permissions  users.role_inheritance             │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## STAGE 4: IMPLEMENTATION PLAN

### 4.1 Database Schema (PostgreSQL)

#### Current Schema Status
✅ **ALREADY EXISTS** - No changes needed:
- `users.roles` - Role definitions
- `users.role_permissions` - Role-to-permission mapping
- `users.role_inheritance` - Role inheritance
- `users.users` - User accounts

#### Required Migrations
**None** - Schema is complete. However, we need to ensure:
1. All system roles are initialized (done by `DatabaseInitializer`)
2. All permissions are assigned to roles (done by `DatabaseInitializer`)
3. Default admin user has correct role (done by `DatabaseInitializer`)

#### Verification Queries
```sql
-- Verify system roles exist
SELECT role_id, name FROM users.roles WHERE is_system_role = true;

-- Verify permissions are assigned
SELECT r.name, COUNT(rp.permission) as permission_count
FROM users.roles r
LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
WHERE r.is_system_role = true
GROUP BY r.role_id, r.name;

-- Verify admin user exists
SELECT user_id, username, role FROM users.users WHERE username = 'admin';
```

### 4.2 Backend / Service Layer

#### A. Enhance RbacService (UsersApi)

**File:** `solution/backend/UsersApi/Services/RbacService.cs`

**Current Status:** ✅ Already implemented with:
- `GetUserPermissionsAsync(userId)` - Gets all permissions for user
- `HasPermissionAsync(userId, permission)` - Checks single permission
- `HasAnyPermissionAsync(userId, permissions[])` - Checks any permission
- `HasAllPermissionsAsync(userId, permissions[])` - Checks all permissions

**Required Enhancements:**
1. Add `GetUserPermissionsForApiAsync(userId)` - Returns permissions as API response
2. Add caching layer (optional, for performance)
3. Add permission change notification (for frontend sync)

**New Method:**
```csharp
public async Task<string[]> GetUserPermissionsForApiAsync(string userId, CancellationToken ct = default)
{
    var permissions = await GetUserPermissionsAsync(userId, ct);
    return permissions; // Already returns string[]
}
```

#### B. Create Authorization Middleware

**New File:** `solution/backend/UsersApi/Middleware/AuthorizationMiddleware.cs`

**Purpose:** Intercept API requests, extract user context, check permissions, allow/deny access.

**Implementation:**
```csharp
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRbacService _rbacService;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, IRbacService rbacService, ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _rbacService = rbacService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract user ID from request (header, query, or session)
        var userId = ExtractUserId(context);
        
        if (userId != null)
        {
            // Get required permission from route/attribute
            var requiredPermission = ExtractRequiredPermission(context);
            
            if (requiredPermission != null)
            {
                var hasPermission = await _rbacService.HasPermissionAsync(userId, requiredPermission, context.RequestAborted);
                if (!hasPermission)
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Forbidden: Insufficient permissions");
                    return;
                }
            }
        }
        
        await _next(context);
    }
    
    private string? ExtractUserId(HttpContext context)
    {
        // Option 1: From header
        if (context.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            return userIdHeader.ToString();
        
        // Option 2: From query string (for now, until JWT implemented)
        if (context.Request.Query.TryGetValue("userId", out var userIdQuery))
            return userIdQuery.ToString();
        
        // Option 3: From session (if session management implemented)
        // return context.Session.GetString("UserId");
        
        return null;
    }
    
    private string? ExtractRequiredPermission(HttpContext context)
    {
        // Extract from route metadata or attribute
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RequiresPermissionAttribute>() is { } attr)
            return attr.Permission;
        
        return null;
    }
}
```

**Alternative Approach (Recommended):** Use ASP.NET Core Authorization Policies instead of custom middleware.

#### C. Create Authorization Attributes

**New File:** `solution/backend/UsersApi/Attributes/RequiresPermissionAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresPermissionAttribute : Attribute
{
    public string Permission { get; }
    
    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission;
    }
}
```

#### D. Create Authorization Policy Handler

**New File:** `solution/backend/UsersApi/Authorization/PermissionRequirementHandler.cs`

```csharp
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IRbacService _rbacService;
    
    public PermissionRequirementHandler(IRbacService rbacService)
    {
        _rbacService = rbacService;
    }
    
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirst("UserId")?.Value;
        if (userId != null)
        {
            var hasPermission = await _rbacService.HasPermissionAsync(userId, requirement.Permission);
            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
```

#### E. Update Controllers with Authorization

**Example:** `solution/backend/UsersApi/Controllers/UsersController.cs`

```csharp
[HttpGet]
[RequiresPermission(Permissions.USER_VIEW)] // Add this
public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request, CancellationToken ct = default)
{
    // ... existing code
}

[HttpPost]
[RequiresPermission(Permissions.USER_CREATE)] // Add this
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct = default)
{
    // ... existing code
}
```

**Apply to ALL controllers across ALL APIs:**
- UsersApi: `UsersController`, `RolesController`, `RbacController`
- MenuApi: `MenuItemsController`, `ModifiersController`, etc.
- OrderApi: `OrdersController`
- PaymentApi: `PaymentsController`
- InventoryApi: `VendorsController`, `ItemsController`, etc.
- SettingsApi: `SettingsController`, `HierarchicalSettingsController`
- CustomerApi: `CustomersController`, etc.
- DiscountApi: `DiscountsController`
- TablesApi: (Minimal API, needs route-level checks)

#### F. Update AuthController to Return Permissions

**File:** `solution/backend/UsersApi/Controllers/AuthController.cs`

**Modify Login endpoint:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default)
{
    // ... existing validation code ...
    
    // Get user permissions
    var permissions = await _rbacService.GetUserPermissionsAsync(user.UserId, ct);
    
    var response = new LoginResponse
    {
        UserId = user.UserId,
        Username = user.Username,
        Role = user.Role,
        Permissions = permissions, // ADD THIS
        LastLoginAt = DateTime.UtcNow
    };
    
    return Ok(response);
}
```

**Update DTO:**
```csharp
// solution/shared/DTOs/Auth/LoginResponse.cs
public record LoginResponse
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string[] Permissions { get; init; } = Array.Empty<string>(); // ADD THIS
    public DateTime LastLoginAt { get; init; }
}
```

#### G. Create RbacController Endpoint for Permission Refresh

**File:** `solution/backend/UsersApi/Controllers/RbacController.cs`

**Add endpoint:**
```csharp
[HttpGet("users/{userId}/permissions")]
public async Task<IActionResult> GetUserPermissions(string userId, CancellationToken ct = default)
{
    var permissions = await _rbacService.GetUserPermissionsAsync(userId, ct);
    return Ok(permissions);
}
```

### 4.3 WinUI 3 (Frontend)

#### A. Create PermissionManager Service

**New File:** `solution/frontend/Services/PermissionManager.cs`

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MagiDesk.Shared.DTOs.Users;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Manages user permissions with local caching for fast UI updates
/// </summary>
public class PermissionManager
{
    private readonly UserApiService _userApiService;
    private HashSet<string> _cachedPermissions = new();
    private bool _isInitialized = false;

    public PermissionManager(UserApiService userApiService)
    {
        _userApiService = userApiService;
    }

    /// <summary>
    /// Initialize permissions from session or API
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        var session = SessionService.Current;
        if (session != null)
        {
            // Try to get permissions from session (if stored)
            if (session.Permissions != null && session.Permissions.Length > 0)
            {
                _cachedPermissions = new HashSet<string>(session.Permissions);
                _isInitialized = true;
                return;
            }

            // Otherwise, fetch from API
            await RefreshPermissionsAsync();
        }
    }

    /// <summary>
    /// Refresh permissions from API
    /// </summary>
    public async Task RefreshPermissionsAsync()
    {
        var session = SessionService.Current;
        if (session == null)
        {
            _cachedPermissions.Clear();
            _isInitialized = false;
            return;
        }

        try
        {
            var permissions = await _userApiService.GetUserPermissionsAsync(session.UserId);
            _cachedPermissions = new HashSet<string>(permissions);
            _isInitialized = true;

            // Update session with permissions
            var updatedSession = session with { Permissions = permissions };
            await SessionService.SaveAsync(updatedSession);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to refresh permissions", ex);
            // Keep existing permissions on error
        }
    }

    /// <summary>
    /// Check if user has a specific permission
    /// </summary>
    public bool HasPermission(string permission)
    {
        if (!_isInitialized) return false;
        return _cachedPermissions.Contains(permission);
    }

    /// <summary>
    /// Check if user has any of the specified permissions
    /// </summary>
    public bool HasAnyPermission(params string[] permissions)
    {
        if (!_isInitialized) return false;
        return permissions.Any(p => _cachedPermissions.Contains(p));
    }

    /// <summary>
    /// Check if user has all of the specified permissions
    /// </summary>
    public bool HasAllPermissions(params string[] permissions)
    {
        if (!_isInitialized) return false;
        return permissions.All(p => _cachedPermissions.Contains(p));
    }

    /// <summary>
    /// Get all cached permissions
    /// </summary>
    public IReadOnlySet<string> GetAllPermissions()
    {
        return _cachedPermissions;
    }

    /// <summary>
    /// Clear cached permissions (on logout)
    /// </summary>
    public void Clear()
    {
        _cachedPermissions.Clear();
        _isInitialized = false;
    }
}
```

#### B. Update SessionService to Store Permissions

**File:** `solution/frontend/Services/SessionService.cs`

**Update SessionDto:**
```csharp
// solution/shared/DTOs/Auth/SessionDto.cs
public record SessionDto
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string[] Permissions { get; init; } = Array.Empty<string>(); // ADD THIS
    public DateTime LastLoginAt { get; init; }
}
```

#### C. Update UserApiService to Fetch Permissions

**File:** `solution/frontend/Services/UserApiService.cs`

**Add method:**
```csharp
public async Task<string[]> GetUserPermissionsAsync(string userId)
{
    var response = await _http.GetAsync($"api/rbac/users/{userId}/permissions");
    response.EnsureSuccessStatusCode();
    var permissions = await response.Content.ReadFromJsonAsync<string[]>();
    return permissions ?? Array.Empty<string>();
}
```

#### D. Create Permission-Based Value Converters

**New File:** `solution/frontend/Converters/PermissionToVisibilityConverter.cs`

```csharp
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Converters;

public class PermissionToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string permission && App.PermissionManager != null)
        {
            return App.PermissionManager.HasPermission(permission) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
```

**New File:** `solution/frontend/Converters/PermissionToEnabledConverter.cs`

```csharp
public class PermissionToEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string permission && App.PermissionManager != null)
        {
            return App.PermissionManager.HasPermission(permission);
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
```

#### E. Update App.xaml.cs to Initialize PermissionManager

**File:** `solution/frontend/App.xaml.cs`

**Add static property:**
```csharp
public static Services.PermissionManager? PermissionManager { get; private set; }
```

**Initialize in `InitializeApiAsync`:**
```csharp
// After UsersApi is initialized
if (UsersApi != null)
{
    PermissionManager = new Services.PermissionManager(UsersApi);
    await PermissionManager.InitializeAsync();
}
```

#### F. Update LoginPage to Store Permissions

**File:** `solution/frontend/Views/LoginPage.xaml.cs`

**Modify Login_Click:**
```csharp
var res = await usersApi.LoginAsync(new LoginRequest { Username = UsernameBox.Text.Trim(), Password = PasswordBox.Password });
if (res == null)
{
    Info.Message = App.I18n.T("invalid_credentials");
    Info.IsOpen = true; return;
}

// Save session with permissions
await SessionService.SaveAsync(new SessionDto 
{ 
    UserId = res.UserId, 
    Username = res.Username, 
    Role = res.Role, 
    Permissions = res.Permissions, // ADD THIS
    LastLoginAt = res.LastLoginAt 
});

// Initialize PermissionManager
if (App.PermissionManager != null)
{
    await App.PermissionManager.RefreshPermissionsAsync();
}

NavigateToMain();
```

#### G. Create Navigation Guard

**New File:** `solution/frontend/Services/NavigationGuard.cs`

```csharp
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Views;

namespace MagiDesk.Frontend.Services;

public static class NavigationGuard
{
    /// <summary>
    /// Check if user can navigate to a page based on permissions
    /// </summary>
    public static bool CanNavigateTo(string pageTag, out string? requiredPermission)
    {
        requiredPermission = GetRequiredPermissionForPage(pageTag);
        
        if (requiredPermission == null)
            return true; // No permission required
        
        if (App.PermissionManager == null)
            return false;
        
        return App.PermissionManager.HasPermission(requiredPermission);
    }

    private static string? GetRequiredPermissionForPage(string pageTag)
    {
        return pageTag switch
        {
            "UsersPage" => Permissions.USER_VIEW,
            "MenuManagementPage" => Permissions.MENU_VIEW,
            "EnhancedMenuManagementPage" => Permissions.MENU_VIEW,
            "InventoryManagementPage" => Permissions.INVENTORY_VIEW,
            "CustomerManagementPage" => Permissions.CUSTOMER_VIEW,
            "VendorsManagementPage" => Permissions.VENDOR_VIEW,
            "OrdersManagementPage" => Permissions.ORDER_VIEW,
            "AllPaymentsPage" => Permissions.PAYMENT_VIEW,
            "SessionsPage" => Permissions.SESSION_VIEW,
            "CashFlowPage" => Permissions.REPORT_VIEW,
            "AuditReportsPage" => Permissions.REPORT_USER_ACTIVITY,
            "HierarchicalSettingsPage" => Permissions.SETTINGS_VIEW,
            _ => null // No permission required
        };
    }
}
```

#### H. Update MainPage Navigation to Use Guard

**File:** `solution/frontend/Views/MainPage.xaml.cs`

**Modify NavView_ItemInvoked:**
```csharp
private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
{
    if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
    {
        // Check permission before navigation
        if (!NavigationGuard.CanNavigateTo(tag, out var requiredPermission))
        {
            // Show error message
            var dialog = new ContentDialog
            {
                Title = "Access Denied",
                Content = $"You do not have permission to access this page. Required permission: {requiredPermission}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
            return;
        }

        // Proceed with navigation
        switch (tag)
        {
            // ... existing cases ...
        }
    }
}
```

#### I. Update ViewModels to Check Permissions

**Example:** `solution/frontend/ViewModels/UsersViewModel.cs`

**Modify commands:**
```csharp
public UsersViewModel(UserApiService userApiService)
{
    _userApiService = userApiService;
    
    // Check permissions before enabling commands
    CreateUserCommand = new RelayCommand(
        async () => await CreateUserAsync(), 
        () => App.PermissionManager?.HasPermission(Permissions.USER_CREATE) ?? false);
    
    EditUserCommand = new RelayCommand(
        async () => await EditUserAsync(), 
        () => SelectedUser != null && (App.PermissionManager?.HasPermission(Permissions.USER_UPDATE) ?? false));
    
    DeleteUserCommand = new RelayCommand(
        async () => await DeleteUserAsync(), 
        () => SelectedUser != null && (App.PermissionManager?.HasPermission(Permissions.USER_DELETE) ?? false));
}
```

**Apply to ALL ViewModels:**
- Check permissions in command constructors
- Re-evaluate command CanExecute when permissions change
- Show error messages if user tries to execute without permission

#### J. Update XAML to Use Permission Converters

**Example:** `solution/frontend/Views/UsersPage.xaml`

```xml
<Page.Resources>
    <converters:PermissionToVisibilityConverter x:Key="PermissionToVisibility"/>
    <converters:PermissionToEnabledConverter x:Key="PermissionToEnabled"/>
</Page.Resources>

<Grid>
    <!-- Hide "Add" button if user doesn't have create permission -->
    <Button Content="Add User" 
            Visibility="{Binding Source={StaticResource PermissionToVisibility}, ConverterParameter='user:create'}"/>
    
    <!-- Disable "Delete" button if user doesn't have delete permission -->
    <Button Content="Delete" 
            IsEnabled="{Binding Source={StaticResource PermissionToEnabled}, ConverterParameter='user:delete'}"/>
</Grid>
```

**Apply to ALL pages:**
- Navigation items visibility
- Command bar buttons
- Action buttons
- Context menu items

### 4.4 Integration Points

#### A. Page-to-Permission Mapping

**Complete Mapping Table:**

| Page | Required Permission | Notes |
|------|-------------------|-------|
| `UsersPage` | `user:view` | Admin only |
| `MenuManagementPage` | `menu:view` | |
| `EnhancedMenuManagementPage` | `menu:view` | |
| `InventoryManagementPage` | `inventory:view` | |
| `CustomerManagementPage` | `customer:view` | |
| `VendorsManagementPage` | `vendor:view` | |
| `OrdersManagementPage` | `order:view` | |
| `OrdersPage` | `order:view` | |
| `PaymentPage` | `payment:view` | |
| `AllPaymentsPage` | `payment:view` | |
| `BillingPage` | `order:view` + `payment:view` | |
| `TablesPage` | `table:view` | |
| `SessionsPage` | `session:view` | |
| `CashFlowPage` | `report:view` | |
| `AuditReportsPage` | `report:user_activity` | Admin only |
| `HierarchicalSettingsPage` | `settings:view` | |
| `HierarchicalSettingsPage?category=system` | `settings:system` | Admin only |
| `HierarchicalSettingsPage?category=security` | `settings:system` + `user:manage_roles` | Admin only |
| `CampaignManagementPage` | `customer:view` | |
| `SegmentDashboardPage` | `customer:view` | |
| `DiscountManagementPage` | `customer:view` | |
| `WalletManagementPage` | `customer:view` | |

#### B. Command-to-Permission Mapping

**Complete Mapping Table:**

| Command | Required Permission | ViewModel |
|---------|-------------------|-----------|
| `CreateUserCommand` | `user:create` | `UsersViewModel` |
| `EditUserCommand` | `user:update` | `UsersViewModel` |
| `DeleteUserCommand` | `user:delete` | `UsersViewModel` |
| `AssignRoleCommand` | `user:manage_roles` | `UsersViewModel` |
| `CreateOrderCommand` | `order:create` | `OrdersViewModel` |
| `UpdateOrderCommand` | `order:update` | `OrdersViewModel` |
| `DeleteOrderCommand` | `order:delete` | `OrdersViewModel` |
| `CancelOrderCommand` | `order:cancel` | `OrdersViewModel` |
| `CompleteOrderCommand` | `order:complete` | `OrdersViewModel` |
| `CreateMenuItemCommand` | `menu:create` | `MenuViewModel` |
| `UpdateMenuItemCommand` | `menu:update` | `MenuViewModel` |
| `DeleteMenuItemCommand` | `menu:delete` | `MenuViewModel` |
| `ChangePriceCommand` | `menu:price_change` | `MenuViewModel` |
| `ProcessPaymentCommand` | `payment:process` | `PaymentViewModel` |
| `RefundPaymentCommand` | `payment:refund` | `PaymentViewModel` |
| `VoidPaymentCommand` | `payment:void` | `PaymentViewModel` |
| `UpdateInventoryCommand` | `inventory:update` | `InventoryManagementViewModel` |
| `AdjustInventoryCommand` | `inventory:adjust` | `InventoryManagementViewModel` |
| `RestockCommand` | `inventory:restock` | `RestockViewModel` |
| `CreateCustomerCommand` | `customer:create` | `CustomerManagementViewModel` |
| `UpdateCustomerCommand` | `customer:update` | `CustomerManagementViewModel` |
| `DeleteCustomerCommand` | `customer:delete` | `CustomerManagementViewModel` |
| `CreateVendorCommand` | `vendor:create` | `VendorsManagementViewModel` |
| `UpdateVendorCommand` | `vendor:update` | `VendorsManagementViewModel` |
| `DeleteVendorCommand` | `vendor:delete` | `VendorsManagementViewModel` |
| `PlaceVendorOrderCommand` | `vendor:order` | `VendorOrdersViewModel` |
| `StartSessionCommand` | `session:manage` | `TablesPage` |
| `CloseSessionCommand` | `session:close` | `TablesPage` |
| `AssignTableCommand` | `table:assign` | `TablesPage` |
| `ClearTableCommand` | `table:clear` | `TablesPage` |
| `UpdateSettingsCommand` | `settings:update` | `HierarchicalSettingsViewModel` |
| `UpdateSystemSettingsCommand` | `settings:system` | `HierarchicalSettingsViewModel` |
| `ExportReportCommand` | `report:export` | `AuditReportsViewModel` |

#### C. Full Permission List

**All 47 Permissions (from `Permissions.cs`):**

**User Management (5):**
- `user:view`, `user:create`, `user:update`, `user:delete`, `user:manage_roles`

**Order Management (6):**
- `order:view`, `order:create`, `order:update`, `order:delete`, `order:cancel`, `order:complete`

**Table Management (4):**
- `table:view`, `table:manage`, `table:assign`, `table:clear`

**Menu Management (5):**
- `menu:view`, `menu:create`, `menu:update`, `menu:delete`, `menu:price_change`

**Payment Management (4):**
- `payment:view`, `payment:process`, `payment:refund`, `payment:void`

**Inventory Management (4):**
- `inventory:view`, `inventory:update`, `inventory:adjust`, `inventory:restock`

**Reports (5):**
- `report:view`, `report:sales`, `report:inventory`, `report:user_activity`, `report:export`

**Settings (4):**
- `settings:view`, `settings:update`, `settings:system`, `settings:receipt`

**Customer Management (4):**
- `customer:view`, `customer:create`, `customer:update`, `customer:delete`

**Reservation Management (4):**
- `reservation:view`, `reservation:create`, `reservation:update`, `reservation:cancel`

**Vendor Management (5):**
- `vendor:view`, `vendor:create`, `vendor:update`, `vendor:delete`, `vendor:order`

**Session Management (3):**
- `session:view`, `session:manage`, `session:close`

### 4.5 Migration & Rollout Plan

#### Phase 1: Backend Foundation (Week 1)
1. ✅ Verify database schema is complete
2. ✅ Verify system roles and permissions are initialized
3. ✅ Test `RbacService.GetUserPermissionsAsync()` works correctly
4. ✅ Update `AuthController.Login` to return permissions
5. ✅ Update `LoginResponse` DTO to include permissions
6. ✅ Add `GetUserPermissions` endpoint to `RbacController`
7. ✅ Test permission retrieval via API

#### Phase 2: Frontend Permission Manager (Week 1-2)
1. ✅ Create `PermissionManager` service
2. ✅ Update `SessionService` to store permissions
3. ✅ Update `UserApiService` to fetch permissions
4. ✅ Initialize `PermissionManager` in `App.xaml.cs`
5. ✅ Update `LoginPage` to store permissions on login
6. ✅ Test permission caching and retrieval

#### Phase 3: Frontend UI Integration (Week 2-3)
1. ✅ Create permission converters (`PermissionToVisibilityConverter`, `PermissionToEnabledConverter`)
2. ✅ Update `MainPage` navigation to use `NavigationGuard`
3. ✅ Update `MainPage` to hide/show navigation items based on permissions
4. ✅ Update `UsersPage` XAML to use permission converters
5. ✅ Test navigation guards and UI visibility

#### Phase 4: ViewModel Integration (Week 3-4)
1. ✅ Update `UsersViewModel` commands to check permissions
2. ✅ Update `OrdersViewModel` commands
3. ✅ Update `PaymentViewModel` commands
4. ✅ Update `MenuViewModel` commands
5. ✅ Update `InventoryManagementViewModel` commands
6. ✅ Update remaining ViewModels (10+ more)
7. ✅ Test command enablement/disablement

#### Phase 5: Backend Authorization & API Versioning (Week 4-5)
1. ✅ Implement API versioning strategy (v1/v2 routing)
2. ✅ Create v2 controllers (or add conditional authorization)
3. ✅ Add `ApiVersionMiddleware` (optional)
4. ✅ Configure feature flags for v2 endpoints
5. ✅ Deploy v2 endpoints (disabled initially)
6. ✅ Create `RequiresPermissionAttribute`
7. ✅ Create `PermissionRequirementHandler`
8. ✅ Register authorization policies in `Program.cs`
9. ✅ Add `[RequiresPermission]` attributes to v2 `UsersController`
10. ✅ Add attributes to v2 `MenuItemsController`
11. ✅ Add attributes to v2 `OrdersController`
12. ✅ Add attributes to v2 `PaymentsController`
13. ✅ Add attributes to remaining v2 controllers (30+ more)
14. ✅ Test v1 endpoints (backward compatibility)
15. ✅ Test v2 endpoints with RBAC (403 responses)
16. ✅ Enable v2 endpoints (with monitoring)

#### Phase 6: Testing & Validation (Week 5-6)
1. ✅ Test with different user roles (Owner, Admin, Manager, Server, Cashier, Host)
2. ✅ Verify permissions are correctly assigned to roles
3. ✅ Test navigation guards block unauthorized access
4. ✅ Test commands are disabled without permissions
5. ✅ Test API returns 403 for unauthorized requests
6. ✅ Test permission refresh on role change
7. ✅ Test offline behavior (cached permissions)

#### Phase 7: Documentation & Training (Week 6)
1. ✅ Document permission system for developers
2. ✅ Create permission assignment guide for admins
3. ✅ Update user manual with role descriptions
4. ✅ Create troubleshooting guide

### 4.6 API Versioning & Backward Compatibility Strategy

#### Problem Statement
The MagiDesk POS system has **deployed APIs in production** that are currently being used by:
- Existing WinUI 3 frontend clients
- Potentially other clients (mobile apps, web apps, third-party integrations)
- Legacy systems that may not support RBAC

**Critical Requirement:** Introducing RBAC must **NOT break existing deployments** or require immediate client updates.

#### Solution: API Versioning Strategy

**Approach:** Implement **URL-based API versioning** with **backward-compatible defaults**.

##### A. Versioning Scheme

**Format:** `/api/v{version}/{controller}/{action}`

**Versions:**
- **v1** (Legacy) - No RBAC enforcement, existing behavior
- **v2** (RBAC-enabled) - RBAC enforcement, permission checks required

**Default Behavior:**
- Requests without version → Route to **v1** (backward compatible)
- Requests with `/api/v1/` → Explicitly use v1
- Requests with `/api/v2/` → Use v2 with RBAC

##### B. Implementation Strategy

**1. Route Configuration (All APIs)**

**File:** `solution/backend/{ApiName}/Program.cs`

```csharp
// Version 1 (Legacy) - No RBAC
var v1 = app.MapGroup("/api/v1");
v1.MapControllers(); // Existing controllers without [RequiresPermission]

// Version 2 (RBAC-enabled) - With authorization
var v2 = app.MapGroup("/api/v2");
v2.MapControllers() // Controllers with [RequiresPermission]
   .RequireAuthorization(); // Global authorization requirement

// Default (v1) - For backward compatibility
app.MapControllers(); // Routes to v1 by default
```

**2. Controller Versioning**

**Option A: Separate Controllers (Recommended for Clean Separation)**

**New File:** `solution/backend/UsersApi/Controllers/V2/UsersController.cs`

```csharp
namespace UsersApi.Controllers.V2;

[ApiController]
[Route("api/v2/[controller]")]
[RequireAuthorization] // Global authorization for v2
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly IRbacService _rbacService;

    [HttpGet]
    [RequiresPermission(Permissions.USER_VIEW)] // RBAC enforcement
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request, CancellationToken ct = default)
    {
        // Same implementation as v1
        var result = await _usersService.GetUsersAsync(request, ct);
        return Ok(result);
    }

    [HttpPost]
    [RequiresPermission(Permissions.USER_CREATE)] // RBAC enforcement
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct = default)
    {
        // Same implementation as v1
        var user = await _usersService.CreateUserAsync(request, ct);
        return CreatedAtAction(nameof(GetUser), new { userId = user.UserId }, user);
    }
}
```

**Keep Existing:** `solution/backend/UsersApi/Controllers/UsersController.cs` (v1, no changes)

```csharp
namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")] // Default route (v1)
[Route("api/v1/[controller]")] // Explicit v1 route
public class UsersController : ControllerBase
{
    // NO [RequiresPermission] attributes - backward compatible
    // Existing implementation unchanged
}
```

**Option B: Conditional Authorization (Simpler, Less Clean)**

**Modify Existing Controller:**

```csharp
[ApiController]
[Route("api/[controller]")] // Default (v1)
[Route("api/v1/[controller]")] // Explicit v1
[Route("api/v2/[controller]")] // v2 route
public class UsersController : ControllerBase
{
    [HttpGet]
    [RequiresPermission(Permissions.USER_VIEW, ApiVersion = "v2")] // Only enforce in v2
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request, CancellationToken ct = default)
    {
        // Check if this is v2 request
        var isV2 = HttpContext.Request.Path.StartsWithSegments("/api/v2");
        
        if (isV2)
        {
            // Enforce RBAC for v2
            var userId = ExtractUserId(HttpContext);
            if (userId == null || !await _rbacService.HasPermissionAsync(userId, Permissions.USER_VIEW))
            {
                return Forbid();
            }
        }
        // v1 continues without RBAC check
        
        var result = await _usersService.GetUsersAsync(request, ct);
        return Ok(result);
    }
}
```

**Recommendation:** Use **Option A (Separate Controllers)** for cleaner separation and easier maintenance.

##### C. Version Detection Middleware

**New File:** `solution/backend/UsersApi/Middleware/ApiVersionMiddleware.cs`

```csharp
public class ApiVersionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiVersionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract API version from path
        var path = context.Request.Path.Value ?? "";
        var version = ExtractVersion(path);
        
        // Store version in HttpContext for controllers to use
        context.Items["ApiVersion"] = version ?? "v1"; // Default to v1
        
        await _next(context);
    }

    private string? ExtractVersion(string path)
    {
        // Match /api/v{number}/...
        var match = System.Text.RegularExpressions.Regex.Match(path, @"/api/v(\d+)/");
        if (match.Success)
        {
            return $"v{match.Groups[1].Value}";
        }
        return null; // No version = v1 (default)
    }
}
```

**Register in Program.cs:**
```csharp
app.UseMiddleware<ApiVersionMiddleware>();
```

##### D. Frontend Version Selection

**Strategy:** Frontend can opt-in to v2 when ready.

**File:** `solution/frontend/Services/UserApiService.cs`

```csharp
public class UserApiService
{
    private readonly HttpClient _http;
    private readonly string _apiVersion; // "v1" or "v2"

    public UserApiService(HttpClient http, string apiVersion = "v1")
    {
        _http = http;
        _apiVersion = apiVersion;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Use versioned endpoint
        var endpoint = _apiVersion == "v2" ? "api/v2/auth/login" : "api/auth/login";
        var response = await _http.PostAsJsonAsync(endpoint, request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>() ?? throw new Exception("Invalid response");
    }
}
```

**Configuration:**

**File:** `solution/frontend/appsettings.json`

```json
{
  "Api": {
    "BaseUrl": "https://magidesk-backend-904541739138.us-central1.run.app",
    "Version": "v1"  // Default to v1 for backward compatibility
  }
}
```

**File:** `solution/frontend/App.xaml.cs`

```csharp
private async Task InitializeApiAsync()
{
    // ... existing code ...
    
    var apiVersion = config["Api:Version"] ?? "v1"; // Default to v1
    
    // Initialize services with version
    UsersApi = new Services.UserApiService(http, apiVersion);
    Menu = new Services.MenuApiService(menuHttp, apiVersion);
    Payments = new Services.PaymentApiService(payHttp, apiVersion);
    // ... etc
}
```

##### E. Migration Path for Clients

**Phase 1: Deploy v2 APIs (Week 1)**
- Deploy v2 endpoints alongside v1
- v1 endpoints remain unchanged (no RBAC)
- v2 endpoints require RBAC
- **No breaking changes** - existing clients continue using v1

**Phase 2: Update Frontend to v2 (Week 2-3)**
- Update WinUI 3 frontend to use v2 endpoints
- Implement PermissionManager
- Test thoroughly
- **Can rollback** by changing config to v1

**Phase 3: Gradual Migration (Week 4-6)**
- Monitor v1 vs v2 usage
- Update other clients (mobile, web) to v2
- **No pressure** - v1 remains available

**Phase 4: Deprecation (Future)**
- Announce v1 deprecation (6+ months notice)
- Provide migration guide
- Set deprecation date
- Eventually remove v1 endpoints

##### F. Version Negotiation

**Option: Content Negotiation via Header**

**Request Header:**
```
X-API-Version: v2
```

**Middleware:**
```csharp
public class ApiVersionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Check header first, then path
        var version = context.Request.Headers["X-API-Version"].FirstOrDefault()
                   ?? ExtractVersionFromPath(context.Request.Path)
                   ?? "v1";
        
        context.Items["ApiVersion"] = version;
        await _next(context);
    }
}
```

**Recommendation:** Use **URL-based versioning** (simpler, more explicit, easier to debug).

##### G. Backward Compatibility Guarantees

**v1 Endpoints (Legacy):**
- ✅ **No RBAC enforcement** - All requests succeed (if authenticated)
- ✅ **Same response format** - No breaking changes
- ✅ **Same error codes** - Consistent behavior
- ✅ **No new required headers** - Works with existing clients
- ✅ **No new required parameters** - Backward compatible

**v2 Endpoints (RBAC-enabled):**
- ✅ **RBAC enforcement** - Permission checks required
- ✅ **Same response format** - Compatible with v1 responses
- ✅ **403 Forbidden** - New error code for unauthorized access
- ✅ **Optional: Enhanced error messages** - Include permission info

**Response Format Compatibility:**

```csharp
// v1 Response
{
  "userId": "123",
  "username": "admin",
  "role": "Administrator"
}

// v2 Response (same format, with optional permissions)
{
  "userId": "123",
  "username": "admin",
  "role": "Administrator",
  "permissions": ["user:view", "user:create", ...] // Optional addition
}
```

##### H. Deployment Strategy

**1. Staged Rollout:**

```
Week 1: Deploy v2 endpoints (disabled/feature flag)
Week 2: Enable v2 endpoints, monitor
Week 3: Update frontend to v2 (with rollback capability)
Week 4: Monitor both versions
Week 5: Gradually migrate other clients
Week 6: Full v2 adoption
```

**2. Feature Flags:**

**File:** `solution/backend/{ApiName}/appsettings.json`

```json
{
  "Api": {
    "EnableV2": false,  // Start disabled
    "EnableV2RBAC": false  // RBAC enforcement flag
  }
}
```

**Program.cs:**
```csharp
var enableV2 = builder.Configuration.GetValue<bool>("Api:EnableV2", false);
var enableRBAC = builder.Configuration.GetValue<bool>("Api:EnableV2RBAC", false);

if (enableV2)
{
    var v2 = app.MapGroup("/api/v2");
    v2.MapControllers();
    
    if (enableRBAC)
    {
        v2.RequireAuthorization();
    }
}
```

**3. Monitoring & Rollback:**

- Monitor v1 vs v2 request counts
- Monitor error rates (403s in v2)
- Monitor performance impact
- **Easy rollback** - Disable v2 via feature flag
- **Gradual migration** - Move clients one at a time

##### I. Testing Strategy

**1. Backward Compatibility Tests:**

```csharp
[Fact]
public async Task V1_Endpoints_Work_Without_RBAC()
{
    // v1 should work without permission checks
    var response = await _client.GetAsync("/api/users");
    response.EnsureSuccessStatusCode();
}

[Fact]
public async Task V2_Endpoints_Require_Permissions()
{
    // v2 should return 403 without permissions
    var response = await _client.GetAsync("/api/v2/users");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task V2_Endpoints_Work_With_Permissions()
{
    // v2 should work with correct permissions
    _client.DefaultRequestHeaders.Add("X-User-Id", "admin-user-id");
    var response = await _client.GetAsync("/api/v2/users");
    response.EnsureSuccessStatusCode();
}
```

**2. Version Coexistence Tests:**

- Test v1 and v2 endpoints simultaneously
- Test same endpoint with different versions
- Test default routing (no version = v1)
- Test explicit version routing

##### J. Implementation Checklist

**Backend (All 9 APIs):**
- [ ] Add version routing (`/api/v1/`, `/api/v2/`)
- [ ] Create v2 controllers (or add conditional logic)
- [ ] Add `ApiVersionMiddleware` (optional)
- [ ] Configure feature flags for v2
- [ ] Deploy v2 endpoints (disabled initially)
- [ ] Test v1 endpoints still work
- [ ] Test v2 endpoints with RBAC
- [ ] Monitor both versions

**Frontend:**
- [ ] Add API version configuration
- [ ] Update service constructors to accept version
- [ ] Default to v1 in config
- [ ] Test with v1 (backward compatible)
- [ ] Test with v2 (RBAC-enabled)
- [ ] Add version switching capability (for testing)

**Deployment:**
- [ ] Deploy v2 APIs (feature flag disabled)
- [ ] Verify v1 endpoints unchanged
- [ ] Enable v2 endpoints (monitor)
- [ ] Update frontend config to v2 (with rollback plan)
- [ ] Monitor error rates
- [ ] Gradual client migration

##### K. Rollback Plan

**If Issues Arise:**

1. **Immediate Rollback:**
   - Set `"Api:Version": "v1"` in frontend config
   - Restart frontend clients
   - **No backend changes needed** - v1 still works

2. **Backend Rollback:**
   - Disable v2 endpoints via feature flag
   - Revert to v1-only deployment
   - **No data loss** - v1 endpoints unchanged

3. **Partial Rollback:**
   - Keep v2 enabled but disable RBAC enforcement
   - Allows testing without security impact
   - Gradual re-enablement

**Rollback Time:** < 5 minutes (config change + restart)

---

## STAGE 5: OUTPUT

### 5.1 Complete RBAC Architecture Document

**This document** serves as the complete architecture document.

### 5.2 Recommended Permissions.cs Structure

**File:** `solution/shared/DTOs/Users/Permissions.cs`

**Status:** ✅ **ALREADY COMPLETE** - No changes needed.

The existing structure is excellent:
- Constants for all permissions
- `AllPermissions` array
- `PermissionDescriptions` dictionary
- `PermissionCategories` dictionary
- `IsValidPermission()` method
- Helper methods for category lookup

### 5.3 Required Code Changes Summary

#### Backend Changes (9 APIs)

**UsersApi:**
- ✅ `AuthController.cs` - Return permissions in login response
- ✅ `RbacController.cs` - Add GetUserPermissions endpoint
- ✅ `Services/RbacService.cs` - Already complete
- ✅ `Attributes/RequiresPermissionAttribute.cs` - **NEW FILE**
- ✅ `Authorization/PermissionRequirementHandler.cs` - **NEW FILE**
- ✅ `Program.cs` - Register authorization policies

**All Other APIs:**
- ✅ Add `[RequiresPermission]` attributes to controllers (38 controllers)
- ✅ Register authorization middleware (if using custom middleware)

#### Frontend Changes

**Services:**
- ✅ `Services/PermissionManager.cs` - **NEW FILE**
- ✅ `Services/SessionService.cs` - Update SessionDto
- ✅ `Services/UserApiService.cs` - Add GetUserPermissions method
- ✅ `Services/NavigationGuard.cs` - **NEW FILE**
- ✅ `App.xaml.cs` - Initialize PermissionManager

**Converters:**
- ✅ `Converters/PermissionToVisibilityConverter.cs` - **NEW FILE**
- ✅ `Converters/PermissionToEnabledConverter.cs` - **NEW FILE**

**Views:**
- ✅ `Views/LoginPage.xaml.cs` - Store permissions on login
- ✅ `Views/MainPage.xaml.cs` - Use NavigationGuard, hide/show menu items
- ✅ `Views/UsersPage.xaml` - Use permission converters
- ✅ All other views - Add permission converters to XAML

**ViewModels:**
- ✅ All 27 ViewModels - Check permissions in command constructors

**DTOs:**
- ✅ `Shared/DTOs/Auth/LoginResponse.cs` - Add Permissions property
- ✅ `Shared/DTOs/Auth/SessionDto.cs` - Add Permissions property

### 5.4 Optional Enhancements

#### A. Custom Attributes for ViewModels

**New File:** `solution/frontend/Attributes/RequiresPermissionAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiresPermissionAttribute : Attribute
{
    public string Permission { get; }
    
    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission;
    }
}
```

**Usage in ViewModel:**
```csharp
[RequiresPermission(Permissions.USER_VIEW)]
public class UsersViewModel : INotifyPropertyChanged
{
    // ...
}
```

**Note:** WinUI 3 doesn't support reflection-based attributes for ViewModels easily. Manual permission checks in constructors are more practical.

#### B. Permission-Based Navigation Guards

**Already included** in `NavigationGuard.cs` (Section 4.3.G)

#### C. Logging of Unauthorized Access Attempts

**Backend:**
```csharp
// In PermissionRequirementHandler
if (!hasPermission)
{
    _logger.LogWarning("User {UserId} attempted to access {Permission} without authorization", userId, requirement.Permission);
    context.Fail();
}
```

**Frontend:**
```csharp
// In NavigationGuard
if (!CanNavigateTo(tag, out var requiredPermission))
{
    Log.Warning($"User {SessionService.Current?.Username} attempted to navigate to {tag} without permission {requiredPermission}");
    // Show error dialog
}
```

#### D. Permission Change Notifications

**New File:** `solution/frontend/Services/PermissionChangeNotifier.cs`

```csharp
public class PermissionChangeNotifier
{
    public event EventHandler<PermissionChangedEventArgs>? PermissionChanged;
    
    public void NotifyPermissionChanged(string[] newPermissions)
    {
        PermissionChanged?.Invoke(this, new PermissionChangedEventArgs(newPermissions));
    }
}

public class PermissionChangedEventArgs : EventArgs
{
    public string[] NewPermissions { get; }
    
    public PermissionChangedEventArgs(string[] newPermissions)
    {
        NewPermissions = newPermissions;
    }
}
```

**Usage:** When user's role changes, refresh permissions and notify all ViewModels to re-evaluate commands.

---

## IMPLEMENTATION CHECKLIST

### Backend
- [ ] Verify database schema completeness
- [ ] **API Versioning:**
  - [ ] Implement v1/v2 routing in all APIs
  - [ ] Create v2 controllers (or add conditional logic)
  - [ ] Add `ApiVersionMiddleware` (optional)
  - [ ] Configure feature flags for v2
  - [ ] Deploy v2 endpoints (disabled initially)
- [ ] Update `AuthController.Login` to return permissions (v2)
- [ ] Update `LoginResponse` DTO (add Permissions field)
- [ ] Add `GetUserPermissions` endpoint to `RbacController` (v2)
- [ ] Create `RequiresPermissionAttribute`
- [ ] Create `PermissionRequirementHandler`
- [ ] Register authorization policies in `Program.cs` (v2 only)
- [ ] Add `[RequiresPermission]` to v2 `UsersController` (5 endpoints)
- [ ] Add `[RequiresPermission]` to v2 `MenuItemsController` (8 endpoints)
- [ ] Add `[RequiresPermission]` to v2 `OrdersController` (6 endpoints)
- [ ] Add `[RequiresPermission]` to v2 `PaymentsController` (6 endpoints)
- [ ] Add `[RequiresPermission]` to v2 `VendorsController` (6 endpoints)
- [ ] Add `[RequiresPermission]` to remaining v2 controllers (20+ endpoints)
- [ ] **Verify v1 endpoints unchanged (backward compatibility)**
- [ ] Test v2 API authorization (403 responses)
- [ ] Enable v2 endpoints (with monitoring)

### Frontend
- [ ] **API Versioning:**
  - [ ] Add API version configuration to `appsettings.json`
  - [ ] Update service constructors to accept version parameter
  - [ ] Default to v1 in configuration (backward compatible)
  - [ ] Test with v1 endpoints (verify no breaking changes)
- [ ] Create `PermissionManager` service
- [ ] Update `SessionService` to store permissions
- [ ] Update `UserApiService` to fetch permissions (v2 endpoint)
- [ ] Initialize `PermissionManager` in `App.xaml.cs`
- [ ] Update `LoginPage` to store permissions (v2 login response)
- [ ] Create `PermissionToVisibilityConverter`
- [ ] Create `PermissionToEnabledConverter`
- [ ] Create `NavigationGuard` service
- [ ] Update `MainPage` navigation
- [ ] Update `MainPage` menu items visibility
- [ ] Update `UsersViewModel` commands
- [ ] Update `OrdersViewModel` commands
- [ ] Update `PaymentViewModel` commands
- [ ] Update `MenuViewModel` commands
- [ ] Update `InventoryManagementViewModel` commands
- [ ] Update remaining ViewModels (22 more)
- [ ] Update `UsersPage.xaml` with converters
- [ ] Update remaining pages with converters (69 more)
- [ ] **Switch to v2 endpoints (with rollback capability)**
- [ ] Test navigation guards
- [ ] Test command enablement
- [ ] Test UI visibility
- [ ] Test v2 API integration

### Testing
- [ ] **Backward Compatibility:**
  - [ ] Test v1 endpoints work without RBAC
  - [ ] Test v1 endpoints unchanged (same responses)
  - [ ] Test default routing (no version = v1)
  - [ ] Test explicit v1 routing
- [ ] **Version Coexistence:**
  - [ ] Test v1 and v2 endpoints simultaneously
  - [ ] Test same endpoint with different versions
  - [ ] Test version switching (v1 ↔ v2)
- [ ] **RBAC Testing (v2):**
  - [ ] Test with Owner role (all permissions)
  - [ ] Test with Administrator role
  - [ ] Test with Manager role
  - [ ] Test with Server role
  - [ ] Test with Cashier role
  - [ ] Test with Host role
  - [ ] Test permission refresh on role change
  - [ ] Test offline behavior (cached permissions)
  - [ ] Test v2 API 403 responses (unauthorized)
  - [ ] Test navigation blocking
  - [ ] Test command disabling
- [ ] **Rollback Testing:**
  - [ ] Test rollback to v1 (config change)
  - [ ] Test feature flag disable (v2 endpoints)
  - [ ] Verify no data loss on rollback

---

## CONCLUSION

This RBAC architecture provides a **complete, scalable, and maintainable** solution for the MagiDesk POS system. It leverages existing infrastructure, fits the WinUI 3 + MVVM pattern, and provides both security (backend enforcement) and excellent UX (frontend shadow enforcement).

**Next Steps:**
1. Review and approve this architecture
2. Begin Phase 1 implementation (Backend Foundation)
3. Implement API versioning (v1/v2) to ensure backward compatibility
4. Deploy v2 endpoints alongside v1 (no breaking changes)
5. Iterate through phases with testing at each stage
6. Migrate frontend to v2 (with rollback capability)
7. Deploy to production with monitoring

**Estimated Implementation Time:** 6-7 weeks (with 1 developer, including versioning)

**Risk Level:** **Very Low** (leveraging existing infrastructure, incremental changes, **backward compatible**)

**Maintenance Burden:** Low (database-driven, minimal code changes for new permissions)

**Backward Compatibility:** ✅ **Guaranteed** - v1 endpoints remain unchanged, v2 is opt-in

---

**END OF DOCUMENT**

