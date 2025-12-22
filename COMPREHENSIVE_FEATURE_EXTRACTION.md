# COMPREHENSIVE FEATURE EXTRACTION - MagiDesk POS System

**Date**: 2025-01-XX  
**Commit**: 9239d9723a31e7906d8702cd22b4931f12d948dc  
**Purpose**: Complete extraction of ALL features, logic, workflows, and behaviors from the legacy application

---

## TABLE OF CONTENTS

1. [Full Feature Inventory](#1-full-feature-inventory)
2. [Business Logic Extraction](#2-business-logic-extraction)
3. [Processing Workflows](#3-processing-workflows)
4. [UI Intent Extraction](#4-ui-intent-extraction)
5. [Infrastructure & Architectural Logic](#5-infrastructure--architectural-logic)
6. [Hidden/Implicit Knowledge](#6-hiddenimplicit-knowledge)
7. [New Architecture Blueprint](#7-new-architecture-blueprint)
8. [Migration Map](#8-migration-map)
9. [Missing/Incomplete Logic Report](#9-missingincomplete-logic-report)

---

## 1. FULL FEATURE INVENTORY

### 1.1 Backend APIs (9 Microservices)

#### CustomerApi
**Location**: `solution/backend/CustomerApi/`

**Controllers**:
- `CustomersController` - Customer CRUD operations
- `MembershipController` - Membership level management
- `WalletController` - Customer wallet operations
- `LoyaltyController` - Loyalty points management
- `CustomerSegmentsController` - Customer segmentation
- `BehavioralTriggersController` - Behavioral trigger management
- `CampaignsController` - Marketing campaign management
- `CommunicationsController` - Customer communication (Email/SMS/WhatsApp)

**Services**:
- `CustomerService` - Core customer operations
- `MembershipService` - Membership level logic
- `WalletService` - Wallet balance and transactions
- `LoyaltyService` - Loyalty points calculation and redemption
- `SegmentationService` - Customer segmentation logic
- `BehavioralTriggerService` - Trigger execution logic
- `CampaignService` - Campaign management
- `CommunicationService` - Multi-channel communication
- `EmailProvider`, `SmsProvider`, `WhatsAppProvider` - Communication providers

**Features**:
- Customer registration and management
- Membership level system with automatic upgrades
- Wallet system with balance tracking
- Loyalty points earning and redemption
- Customer segmentation (behavioral, demographic)
- Behavioral triggers (automated actions based on customer behavior)
- Marketing campaigns
- Multi-channel communication (Email, SMS, WhatsApp)
- Customer search and filtering
- Customer statistics and analytics
- Order processing integration (discounts, loyalty points)

#### MenuApi
**Location**: `solution/backend/MenuApi/`

**Controllers**:
- `MenuItemsController` - Menu item CRUD
- `ModifiersController` - Modifier management
- `CombosController` - Combo/package management
- `MenuAnalyticsController` - Menu analytics
- `MenuBulkOperationController` - Bulk operations
- `MenuVersioningController` - Menu versioning
- `HistoryController` - Menu history

**Services**:
- `MenuService` - Core menu operations
- `InventoryService` - Inventory integration
- `MenuAnalyticsService` - Analytics and reporting
- `MenuBulkOperationService` - Bulk operations
- `MenuVersioningService` - Version control

**Features**:
- Menu item management (CRUD)
- Modifier groups and options
- Combo/package items
- Menu versioning and history
- Menu analytics (popular items, profitability)
- Bulk operations (import/export, batch updates)
- Inventory integration (availability checking)
- Price management
- Category organization

#### OrderApi
**Location**: `solution/backend/OrderApi/`

**Controllers**:
- `OrdersController` - Order management
- `OrderLogsController` - Order logging

**Services**:
- `OrderService` - Core order operations
- `KitchenService` - Kitchen display system integration

**Features**:
- Order creation and management
- Order status tracking (open, in-progress, delivered, closed)
- Order item management
- Session-based ordering (table sessions)
- Kitchen display integration
- Order logging and audit trail
- Order history
- Order cancellation
- Order modification

#### PaymentApi
**Location**: `solution/backend/PaymentApi/`

**Controllers**:
- `PaymentsController` - Payment processing

**Services**:
- `PaymentService` - Payment processing logic
- `ImmutableIdService` - Billing ID validation

**Features**:
- Payment registration
- Payment method support (cash, card, etc.)
- Discount application
- Tip handling
- Split payment support
- Payment ledger management
- Billing ID validation (immutable format)
- Payment status tracking
- Payment history

#### SettingsApi
**Location**: `solution/backend/SettingsApi/`

**Controllers**:
- `SettingsController` - Settings management
- `HierarchicalSettingsController` - Hierarchical settings

**Services**:
- `SettingsService` - Settings operations
- `HierarchicalSettingsService` - Hierarchical settings logic

**Features**:
- Frontend settings
- Backend settings
- App-level settings
- Hierarchical settings (host-specific overrides)
- Settings audit trail
- Default settings management

#### TablesApi
**Location**: `solution/backend/TablesApi/`

**Features** (from Program.cs - minimal API):
- Table status management
- Table session management (start/stop)
- Bill generation
- Session movement between tables
- Session heartbeat
- Stale session cleanup
- Auto-stop enforcement (based on SettingsApi config)
- Rate per minute configuration
- Bill reopening
- Bill settlement

**Endpoints**:
- `/tables` - List all tables
- `/tables/{label}` - Get/update table status
- `/tables/{label}/start` - Start session
- `/tables/{label}/stop` - Stop session and generate bill
- `/tables/{label}/items` - Get/update session items
- `/tables/{fromLabel}/move` - Move session between tables
- `/sessions/active` - List active sessions
- `/sessions` - List sessions with filters
- `/bills` - List bills
- `/bills/{billId}` - Get bill details
- `/bills/{billId}/settle` - Mark bill as settled
- `/bills/{billingId}/reopen` - Reopen a bill
- `/sessions/{sessionId}/heartbeat` - Update heartbeat
- `/sessions/cleanup-stale` - Cleanup stale sessions
- `/sessions/enforce` - Auto-stop enforcement

#### UsersApi
**Location**: `solution/backend/UsersApi/`

**Controllers**:
- `UsersController` - User management
- `AuthController` - Authentication
- `RolesController` - Role management
- `RbacController` - RBAC operations
- `DebugController` - Debug endpoints

**Services**:
- `UsersService` - User operations
- `RbacService` - Role-based access control

**Features**:
- User CRUD operations
- Authentication (login)
- Role management
- RBAC (Role-Based Access Control)
- Permission management
- User status management
- Password management

#### InventoryApi
**Location**: `solution/backend/InventoryApi/`

**Controllers**:
- `ItemsController` - Inventory item management
- `VendorsController` - Vendor management
- `StockController` - Stock operations
- `RestockController` - Restock requests
- `VendorOrdersController` - Vendor order management
- `CashFlowController` - Cash flow tracking
- `ReportsController` - Inventory reports
- `AuditController` - Audit operations
- `DbController` - Database operations
- `OrdersController` - Order integration

**Repositories**:
- `InventoryRepository` - Inventory data access
- `VendorRepository` - Vendor data access
- `CategoryRepository` - Category data access
- `CashFlowRepository` - Cash flow data access

**Features**:
- Inventory item CRUD
- Stock level management
- Stock adjustments (with reason tracking)
- Low stock alerts
- Vendor management
- Vendor order management
- Restock request workflow
- Cash flow tracking
- Inventory reports (low stock, margins)
- SKU-based operations
- Category management
- Inventory availability checking
- Profit margin calculations

#### DiscountApi
**Location**: `solution/backend/DiscountApi/`

**Controllers**:
- `DiscountsController` - Discount management
- `HealthController` - Health checks

**Services**:
- `DiscountService` - Discount logic
- `CustomerAnalysisService` - Customer analysis
- `VoucherService` - Voucher management
- `ComboService` - Combo discount logic
- `MigrationService` - Database migrations

**Features**:
- Discount rule management
- Customer-based discounts
- Voucher system
- Combo discounts
- Customer analysis for discount eligibility
- Discount application logic

### 1.2 Frontend Features (WinUI 3 Desktop App)

**Location**: `solution/frontend/`

#### Views (70 XAML pages)
1. **DashboardPage** - Main dashboard with analytics
2. **ModernDashboardPage** - Enhanced dashboard
3. **TablesPage** - Table management
4. **MenuPage** - Menu display
5. **MenuSelectionPage** - Menu item selection
6. **MenuManagementPage** - Menu administration
7. **EnhancedMenuManagementPage** - Advanced menu management
8. **OrdersPage** - Order display
9. **OrdersManagementPage** - Order administration
10. **PaymentPage** - Payment processing
11. **EphemeralPaymentPage** - Quick payment
12. **AllPaymentsPage** - Payment history
13. **BillingPage** - Billing management
14. **ReceiptPage** - Receipt display
15. **InventoryManagementPage** - Inventory administration
16. **InventoryCrudPage** - Inventory CRUD operations
17. **InventorySettingsPage** - Inventory settings
18. **VendorsManagementPage** - Vendor administration
19. **VendorsInventoryPage** - Vendor inventory view
20. **VendorOrdersPage** - Vendor order management
21. **VendorDetailsPage** - Vendor details
22. **CustomerManagementPage** - Customer administration
23. **CustomerDetailsPage** - Customer details
24. **CustomerDashboardPage** - Customer analytics
25. **CustomerRegistrationPage** - Customer registration
26. **SegmentDashboardPage** - Customer segment analytics
27. **UsersPage** - User management
28. **SettingsPage** - Settings management
29. **SystemSettingsPage** - System settings
30. **GeneralSettingsPage** - General settings
31. **SecuritySettingsPage** - Security settings
32. **PosSettingsPage** - POS settings
33. **PaymentsSettingsPage** - Payment settings
34. **ReceiptSettingsPage** - Receipt settings
35. **PrinterSettingsPage** - Printer settings
36. **InventorySettingsPage** - Inventory settings
37. **CustomersSettingsPage** - Customer settings
38. **NotificationsSettingsPage** - Notification settings
39. **IntegrationsSettingsPage** - Integration settings
40. **HierarchicalSettingsPage** - Hierarchical settings
41. **BaseSettingsPage** - Base settings page
42. **CashFlowPage** - Cash flow display
43. **SessionsPage** - Session management
44. **RestockPage** - Restock management
45. **DiscountManagementPage** - Discount administration
46. **DiscountDemoPage** - Discount demonstration
47. **CampaignManagementPage** - Campaign administration
48. **WalletManagementPage** - Wallet management
49. **AuditReportsPage** - Audit reports
50. **ReceiptFormatDesignerPage** - Receipt format designer
51. **MainPage** - Main navigation page
52. **LoginPage** - Authentication

#### Dialogs (11 dialogs)
1. **BillSummaryDialog** - Bill summary display
2. **MenuItemDialog** - Menu item details
3. **MenuItemCrudDialog** - Menu item CRUD
4. **MenuSelectionDialog** - Menu item selection
5. **ModifierCrudDialog** - Modifier CRUD
6. **ModifierOptionCrudDialog** - Modifier option CRUD
7. **ReopenBillDialog** - Bill reopening
8. **SessionRecoveryDialog** - Session recovery
9. **TableStatusDialog** - Table status
10. **VendorCrudDialog** - Vendor CRUD
11. **VendorDetailsDialog** - Vendor details

#### ViewModels (27 ViewModels)
1. **DashboardViewModel** - Dashboard logic
2. **ModernDashboardViewModel** - Enhanced dashboard logic
3. **MenuViewModel** - Menu display logic
4. **MenuItemDetailsViewModel** - Menu item details
5. **MenuAnalyticsViewModel** - Menu analytics
6. **OrdersViewModel** - Order display logic
7. **OrdersManagementViewModel** - Order administration logic
8. **OrderDetailViewModel** - Order details
9. **PaymentViewModel** - Payment processing logic
10. **AllPaymentsViewModel** - Payment history
11. **BillingViewModel** - Billing logic
12. **UnsettledBillsViewModel** - Unsettled bills
13. **InventoryViewModel** - Inventory display
14. **InventoryManagementViewModel** - Inventory administration
15. **InventoryCrudViewModel** - Inventory CRUD
16. **InventorySettingsViewModel** - Inventory settings
17. **VendorsManagementViewModel** - Vendor administration
18. **VendorOrdersViewModel** - Vendor orders
19. **RestockViewModel** - Restock logic
20. **CashFlowViewModel** - Cash flow logic
21. **SettingsViewModel** - Settings logic
22. **HierarchicalSettingsViewModel** - Hierarchical settings
23. **ReceiptSettingsViewModel** - Receipt settings
24. **ReceiptFormatDesignerViewModel** - Receipt format designer
25. **UsersViewModel** - User management
26. **ReceiptData** - Receipt data model

#### Services (51 services)
1. **ApiService** - Main API client
2. **MenuApiService** - Menu API client
3. **OrderApiService** - Order API client
4. **PaymentApiService** - Payment API client
5. **UserApiService** - User API client
6. **CustomerApiService** - Customer API client
7. **VendorOrdersApiService** - Vendor orders API client
8. **SettingsApiService** - Settings API client
9. **TablesApiService** - Tables API client
10. **InventoryService** - Inventory operations
11. **VendorService** - Vendor operations
12. **VendorOrderService** - Vendor order operations
13. **ReceiptService** - Receipt generation
14. **ReceiptBuilder** - Receipt building
15. **ReceiptFormatter** - Receipt formatting
16. **ReceiptMigrationService** - Receipt migration
17. **BillingService** - Billing operations
18. **BillingDiscountService** - Billing discount logic
19. **DiscountService** - Discount operations
20. **SplitPaymentCalculator** - Split payment calculations
21. **TableRepository** - Table data access
22. **SessionService** - Session management
23. **HeartbeatService** - Session heartbeat
24. **RestockService** - Restock operations
25. **CashFlowService** - Cash flow operations
26. **AuditService** - Audit operations
27. **NotificationService** - Notifications
28. **ThemeService** - Theme management
29. **I18nService** - Internationalization
30. **PaneManager** - Pane management
31. **OrderContext** - Order context management
32. **PaymentIdResolver** - Payment ID resolution
33. **MenuAnalyticsService** - Menu analytics
34. **MenuBulkOperationService** - Menu bulk operations
35. **CustomerIntelligenceService** - Customer intelligence
36. **InventorySettingsService** - Inventory settings
37. **HierarchicalSettingsApiService** - Hierarchical settings API
38. **FirestoreSettingsService** - Firestore settings (legacy)
39. **ComprehensiveTracingService** - Tracing
40. **HttpLoggingHandler** - HTTP logging
41. **SafeDispatcher** - Thread-safe dispatcher
42. **SafeFileOperations** - File operations
43. **RelayCommand** - Command pattern
44. **SimpleLogger** - Logging
45. **DebugLogger** - Debug logging
46. **NullLogger** - Null logger
47. **Log** - Logging utility
48. **IToolbarConsumer** - Toolbar interface

#### Converters (14 converters)
1. **CurrencyConverter** - Currency formatting
2. **BoolToVisibilityConverter** - Boolean to visibility
3. **BoolToOpacityConverter** - Boolean to opacity
4. **DateShortConverter** - Date formatting
5. **ItemCountConverter** - Item count formatting
6. **ObjectToVisibilityConverter** - Object to visibility
7. **OccupiedToColorConverter** - Table status color
8. **PaymentConverters** - Payment formatting
9. **SessionConverters** - Session formatting
10. **StringEqualsConverter** - String comparison
11. **StringToBrushConverter** - String to brush
12. **UserStatusConverters** - User status formatting
13. **DashboardConverters** - Dashboard formatting
14. **CustomizeButtonTextConverter** - Button text

### 1.3 Shared DTOs and Models

**Location**: `solution/shared/DTOs/`

#### Core DTOs
- **OrderDto** - Order representation
- **OrderItemDto** - Order item
- **CartDraftDto** - Cart draft
- **OrderNotificationDto** - Order notifications
- **OrdersJobDto** - Order jobs

#### Tables DTOs
- **TableStatusDto** - Table status
- **SessionDtos** - Session data
- **BillResult** - Bill result
- **ItemLine** - Bill item line

#### Billing DTOs
- **BillingDto** - Billing entity
- **BillingSessionDto** - Billing session
- **BillingOrderDto** - Billing order
- **BillingOrderItemDto** - Billing order item
- **BillingSummaryDto** - Billing summary
- **SessionMoveRequest** - Session move request
- **SessionMoveResponse** - Session move response
- **CreateBillingRequest** - Create billing request
- **CreateBillingResponse** - Create billing response

#### Customer DTOs
- **CustomerDto** - Customer data
- **CustomerCreateRequestDto** - Create customer
- **CustomerUpdateRequestDto** - Update customer
- **CustomerSearchRequestDto** - Search customer
- **CustomerSearchResponseDto** - Search results
- **CustomerStatsDto** - Customer statistics
- **MembershipLevelDto** - Membership level
- **WalletDto** - Wallet data
- **WalletTransactionDto** - Wallet transaction
- **LoyaltyTransactionDto** - Loyalty transaction
- **CustomerIntelligenceDTOs** - Customer intelligence data

#### Inventory DTOs
- **InventoryItem** - Inventory item
- **ItemDto** - Item data
- **VendorDto** - Vendor data
- **ExtendedVendorDto** - Extended vendor data
- **VendorOrderDto** - Vendor order
- **RestockRequestDto** - Restock request

#### Settings DTOs
- **HierarchicalSettingsModels** - Hierarchical settings

#### Auth DTOs
- **AuthDtos** - Authentication data
- **UserDto** - User data

#### Users DTOs
- **UserModels** - User models
- **UserRoles** - User roles
- **Permissions** - Permissions

#### Other DTOs
- **CashFlow** - Cash flow data
- **JobStatusDto** - Job status
- **AuditDtos** - Audit data

---

## 2. BUSINESS LOGIC EXTRACTION

### 2.1 Order Processing Logic

**Location**: `solution/backend/OrderApi/Services/OrderService.cs`

**Rules**:
- Orders are session-based (linked to table sessions)
- Orders have status: open, in-progress, delivered, closed
- Order items track delivered quantity separately from ordered quantity
- Orders can be modified before delivery
- Orders can be cancelled
- Orders track base price and price delta (for modifications)
- Orders snapshot menu item data at creation time
- Orders link to billing_id for payment processing

**Calculations**:
- Order total = sum of (base_price + price_delta) * quantity for all items
- Profit = sum of (base_price - vendor_price) * quantity
- Discounts applied at order level or item level
- Tax calculated on subtotal after discounts

### 2.2 Payment Processing Logic

**Location**: `solution/backend/PaymentApi/Services/PaymentService.cs`

**Rules**:
- Billing ID must be in immutable format (validated by ImmutableIdService)
- Billing ID must exist in TablesApi (active session or bill)
- Payment lines must have non-negative amounts
- Payment ledger tracks cumulative payments per billing_id
- Payment status: not-paid, partial-paid, paid, partial-refunded, refunded, cancelled
- Multiple payment methods can be used (split payment)
- Discounts and tips tracked separately
- External reference tracking for payment processors

**Calculations**:
- Total paid = sum of all payment line amounts
- Remaining balance = bill total - total paid
- Payment is complete when total paid >= bill total

### 2.3 Table Session Logic

**Location**: `solution/backend/TablesApi/Program.cs`

**Rules**:
- Table types: billiard, bar, restaurant
- Only billiard tables charge time-based fees
- Session start creates billing_id (UUID)
- Session stop creates bill with time cost + items cost
- Sessions can be moved between tables
- Sessions track heartbeat for stale detection
- Auto-stop enforced based on SettingsApi config
- Bills can be reopened (creates new session)

**Calculations**:
- Time cost = rate_per_minute * minutes (only for billiard tables)
- Items cost = sum of item prices * quantities
- Total = time cost + items cost
- Minutes = (end_time - start_time) in minutes

### 2.4 Inventory Logic

**Location**: `solution/backend/InventoryApi/`

**Rules**:
- Stock adjustments require reason
- Stock cannot go negative (enforced at service level)
- Low stock alerts based on threshold
- SKU must be unique per vendor
- Inventory items link to menu items
- Stock availability checked before order fulfillment
- Profit margin = (selling_price - vendor_price) / selling_price

**Calculations**:
- Available stock = current_stock - reserved_stock
- Margin = (price - cost) / price * 100
- Total value = sum of (stock * cost) for all items

### 2.5 Customer Logic

**Location**: `solution/backend/CustomerApi/Services/`

**Rules**:
- Membership levels have minimum spend requirements
- Automatic membership upgrades based on total spend
- Loyalty points earned based on order amount and membership multiplier
- Loyalty points expire after validity period
- Wallet balance cannot exceed max balance for membership level
- Wallet transactions are immutable (credit/debit only)
- Customer segments based on behavior and demographics

**Calculations**:
- Loyalty points = (order_amount / points_per_dollar) * membership_multiplier
- Discount amount = order_amount * membership_discount_percentage
- Membership upgrade eligibility = current_spend >= next_level_minimum_spend

### 2.6 Discount Logic

**Location**: `solution/backend/DiscountApi/Services/DiscountService.cs`

**Rules**:
- Discounts can be percentage or fixed amount
- Discounts can be customer-based, item-based, or order-based
- Discounts can have minimum order amount requirements
- Discounts can have validity periods
- Vouchers are single-use or multi-use
- Combo discounts apply when combo items are ordered together
- Discounts stack (multiple discounts can apply)

**Calculations**:
- Percentage discount = order_amount * (discount_percentage / 100)
- Final amount = order_amount - discount_amount
- Discount eligibility checked before application

### 2.7 Menu Logic

**Location**: `solution/backend/MenuApi/Services/MenuService.cs`

**Rules**:
- Menu items have base price
- Menu items can have modifiers (required or optional)
- Modifiers can have multiple options
- Combo items bundle multiple menu items
- Menu items track availability (linked to inventory)
- Menu versioning preserves historical prices
- Menu items can be temporarily unavailable

**Calculations**:
- Item total = base_price + sum of modifier option prices
- Combo price = sum of component prices - combo_discount

---

## 3. PROCESSING WORKFLOWS

### 3.1 Table Session Workflow

```
1. User selects table → Frontend calls GET /tables/{label}
2. Check if table is occupied → If occupied, show error
3. User clicks "Start Session" → Frontend calls POST /tables/{label}/start
   - Request: { ServerId, ServerName }
   - Backend creates:
     - session_id (UUID)
     - billing_id (UUID)
     - table_sessions record (status='active')
     - Updates table_status (occupied=true)
4. Session active → Timer starts, orders can be placed
5. User places orders → OrderApi creates orders linked to session_id
6. User clicks "Stop Session" → Frontend calls POST /tables/{label}/stop
   - Backend:
     - Closes session (status='closed', end_time=now)
     - Calculates time cost (rate_per_minute * minutes)
     - Fetches items from table_sessions.items (legacy)
     - Fetches items from orders (ord.order_items)
     - Merges items (deduplicates by item_id)
     - Calculates items_cost = sum(item.price * item.quantity)
     - Calculates total = time_cost + items_cost
     - Creates bill record
     - Frees table (occupied=false)
   - Returns BillResult
7. User views bill → Frontend displays bill details
8. User processes payment → PaymentApi registers payment
9. Payment complete → Bill marked as settled
```

### 3.2 Order Processing Workflow

```
1. User selects menu items → Frontend builds order
2. User confirms order → Frontend calls POST /api/orders
   - Request: OrderDto with items
   - Backend:
     - Validates session_id exists
     - Validates inventory availability
     - Creates order record (status='open')
     - Creates order_items with snapshot data
     - Updates inventory (reserves stock)
     - Sends to kitchen (if KitchenService configured)
3. Kitchen prepares order → Kitchen updates order status
4. Order delivered → Backend updates delivered_quantity
5. Order closed → Backend marks order as closed
6. Inventory updated → Stock reduced by delivered_quantity
```

### 3.3 Payment Processing Workflow

```
1. User views bill → Frontend displays bill total
2. User selects payment method → Frontend shows payment dialog
3. User enters payment details → Frontend calls POST /api/payments/register
   - Request: RegisterPaymentRequestDto
     - billing_id (required, immutable format)
     - lines: [{ amount_paid, payment_method, discount_amount, tip_amount }]
   - Backend:
     - Validates billing_id format (ImmutableIdService)
     - Validates billing_id exists (TablesApi)
     - Creates payment records
     - Updates payment ledger
     - Calculates total_paid = sum(amount_paid)
     - If total_paid >= bill_total:
       - Marks bill as paid
       - Updates payment_state = 'paid'
4. Payment complete → Frontend shows receipt
5. Receipt printed → ReceiptService generates PDF
```

### 3.4 Customer Registration Workflow

```
1. User opens customer registration → Frontend shows registration form
2. User enters customer details → Frontend validates input
3. User submits → Frontend calls POST /api/customers
   - Request: CustomerCreateRequestDto
   - Backend:
     - Creates customer record
     - Creates wallet (balance=0)
     - Assigns default membership level
     - Returns CustomerDto
4. Customer created → Frontend shows success message
5. Customer can now place orders → Linked to customer_id
```

### 3.5 Inventory Restock Workflow

```
1. User identifies low stock → Frontend shows low stock alert
2. User creates restock request → Frontend calls POST /api/inventory/restock
   - Request: RestockRequestDto
   - Backend:
     - Creates restock request
     - Notifies vendor (if configured)
     - Returns restock request ID
3. Vendor fulfills order → Vendor updates restock status
4. Stock received → User adjusts inventory
   - Frontend calls POST /api/inventory/items/{id}/adjust
   - Request: { delta, reason, userId }
   - Backend:
     - Validates delta (cannot make stock negative)
     - Creates stock adjustment record
     - Updates inventory stock level
     - Logs adjustment with reason
```

### 3.6 Menu Management Workflow

```
1. User opens menu management → Frontend loads menu items
2. User creates/edits menu item → Frontend shows dialog
3. User saves → Frontend calls POST/PUT /api/menu/items
   - Request: MenuItemDto
   - Backend:
     - Validates item data
     - Links to inventory item (if applicable)
     - Creates/updates menu item
     - Creates menu version snapshot
     - Returns MenuItemDto
4. Menu item saved → Frontend refreshes menu list
5. Menu item available → Customers can order
```

---

## 4. UI INTENT EXTRACTION

### 4.1 Main Navigation Structure

**MainPage.xaml** - Navigation hub
- Left sidebar: Navigation menu
- Main content: Current page
- Top bar: User info, settings, notifications

**Navigation Items**:
1. Dashboard
2. Tables
3. Menu
4. Orders
5. Payments
6. Billing
7. Inventory
8. Vendors
9. Customers
10. Users
11. Settings
12. Reports

### 4.2 Table Management UI

**TablesPage.xaml** - Table grid view
- Grid of table cards
- Color coding: Green (available), Red (occupied)
- Table info: Label, type, server, start time
- Actions: Start, Stop, Move, View Details

**Intent**:
- Visual representation of all tables
- Quick status overview
- One-click table operations
- Real-time status updates

### 4.3 Order Management UI

**OrdersPage.xaml** - Order list
- List of active orders
- Order details: Items, status, table, server
- Actions: View details, Modify, Cancel, Mark delivered

**OrdersManagementPage.xaml** - Order administration
- Filter by status, date, table, server
- Order history
- Order analytics
- Bulk operations

**Intent**:
- Track all orders in real-time
- Manage order lifecycle
- Kitchen display integration
- Order history and reporting

### 4.4 Payment UI

**PaymentPage.xaml** - Payment processing
- Bill summary
- Payment method selection
- Amount entry
- Discount application
- Tip entry
- Split payment options
- Receipt generation

**Intent**:
- Streamlined payment processing
- Multiple payment methods
- Discount and tip handling
- Receipt printing

### 4.5 Menu UI

**MenuPage.xaml** - Customer-facing menu
- Category navigation
- Menu item grid
- Item details on selection
- Modifier selection
- Add to order

**MenuManagementPage.xaml** - Menu administration
- Menu item CRUD
- Category management
- Price management
- Availability toggles
- Bulk operations

**Intent**:
- Easy menu browsing
- Quick item selection
- Modifier customization
- Administrative control

### 4.6 Inventory UI

**InventoryManagementPage.xaml** - Inventory overview
- Item list with stock levels
- Low stock alerts
- Quick stock adjustments
- Vendor links
- Category filters

**InventoryCrudPage.xaml** - Item details
- Item information
- Stock management
- Price management
- Vendor assignment
- SKU management

**Intent**:
- Real-time stock tracking
- Quick stock updates
- Vendor management
- Low stock alerts

### 4.7 Customer UI

**CustomerManagementPage.xaml** - Customer list
- Customer search and filters
- Customer details
- Membership status
- Loyalty points
- Wallet balance
- Order history

**CustomerRegistrationPage.xaml** - Registration form
- Personal information
- Contact details
- Membership selection
- Initial wallet funding

**Intent**:
- Customer relationship management
- Membership tracking
- Loyalty program management
- Wallet management

### 4.8 Settings UI

**SettingsPage.xaml** - Settings hub
- Category navigation
- Setting groups
- Save/Cancel actions

**Category Pages**:
- General Settings
- POS Settings
- Payment Settings
- Receipt Settings
- Printer Settings
- Inventory Settings
- Customer Settings
- Security Settings
- Notification Settings
- Integration Settings

**Intent**:
- Centralized configuration
- Category organization
- Host-specific overrides (hierarchical)
- Audit trail

---

## 5. INFRASTRUCTURE & ARCHITECTURAL LOGIC

### 5.1 API Architecture

**Microservices Pattern**:
- 9 independent APIs
- Each API has its own database schema
- APIs communicate via HTTP
- Shared DTOs in separate project

**API Communication**:
- MenuApi → InventoryApi (availability checking)
- OrderApi → InventoryApi (stock reservation)
- TablesApi → SettingsApi (auto-stop config)
- TablesApi → PaymentApi (bill settlement)
- PaymentApi → TablesApi (billing_id validation)

### 5.2 Database Architecture

**PostgreSQL**:
- Multiple schemas: `ord`, `menu`, `pay`, `inv`, `cust`, `public`
- Connection pooling via NpgsqlDataSource
- Cloud Run socket connections in production
- Local connections in development

**Schema Organization**:
- `ord` - Orders and order items
- `menu` - Menu items, modifiers, combos
- `pay` - Payments and payment ledger
- `inv` - Inventory items, vendors, categories
- `cust` - Customers, memberships, wallets, loyalty
- `public` - Tables, sessions, bills, settings

### 5.3 Frontend Architecture

**WinUI 3 Desktop App**:
- MVVM pattern (ViewModels)
- Dependency injection (ServiceProvider)
- Service layer for API communication
- Converters for data formatting
- Dialogs for modal operations

**Service Initialization**:
- App.xaml.cs initializes all services
- Configuration from appsettings.json
- Fallback URLs for development
- HTTP clients with certificate validation bypass (dev)

### 5.4 Error Handling

**Backend**:
- Try-catch in controllers
- Returns appropriate HTTP status codes
- Error messages in response body
- Logging to console/debug

**Frontend**:
- Try-catch in async methods
- User-friendly error messages
- Logging to file (crash-debug.log)
- Graceful degradation

### 5.5 Session Management

**Table Sessions**:
- Session ID (UUID) tracks active sessions
- Billing ID (UUID) links sessions to bills
- Heartbeat mechanism prevents stale sessions
- Session recovery on app restart

**Order Context**:
- OrderContext static class tracks current session
- Session ID, billing ID, order ID stored
- Cleared on session end or app close

### 5.6 Receipt Generation

**ReceiptService**:
- PDF generation using PDFSharp
- Receipt templates
- Format customization
- Printer integration

**Receipt Data**:
- Bill information
- Items list
- Payment details
- Customer information (if available)

---

## 6. HIDDEN/IMPLICIT KNOWLEDGE

### 6.1 Naming Conventions

**Billing ID Format**:
- Immutable format enforced by ImmutableIdService
- Format validation prevents modification
- Used for payment tracking

**Session Status**:
- 'active' - Session is ongoing
- 'closed' - Session ended, bill created
- NULL - Legacy support, treated as active if end_time is NULL

**Payment State**:
- 'not-paid' - No payments
- 'partial-paid' - Some payments made
- 'paid' - Fully paid
- 'partial-refunded' - Partial refund
- 'refunded' - Full refund
- 'cancelled' - Cancelled

### 6.2 Partial Implementations

**Kitchen Display System**:
- KitchenService exists but integration incomplete
- Order status updates to kitchen not fully implemented

**Customer Intelligence**:
- Segmentation service exists
- Behavioral triggers defined but execution incomplete
- Campaign management UI incomplete

**Menu Versioning**:
- Versioning service exists
- History tracking implemented
- UI for version comparison missing

### 6.3 TODOs and Comments

**Payment Validation**:
- Comment: "CRITICAL FIX: Also fetch items from orders"
- Indicates legacy support for table_sessions.items
- Modern approach uses orders table

**Session Recovery**:
- Recovery dialog implemented
- Automatic recovery on app start
- Manual recovery option

**Stale Session Cleanup**:
- Heartbeat mechanism implemented
- Cleanup endpoint exists
- Auto-cleanup not scheduled (manual trigger)

### 6.4 Legacy Support

**Table Sessions Items**:
- table_sessions.items (JSONB) maintained for legacy
- Modern approach uses orders table
- Both sources merged when generating bills

**Billing ID Type**:
- Migration from TEXT to UUID in progress
- Both formats supported during transition
- Error handling for type conversion

### 6.5 Configuration Patterns

**Connection Strings**:
- Development: LocalConnectionString
- Production: CloudRunSocketConnectionString
- Environment variable fallbacks

**API Base URLs**:
- Configurable per API
- Fallback to localhost in development
- Environment variable support

---

## 7. NEW ARCHITECTURE BLUEPRINT

### 7.1 Proposed Module Structure

**Domain Modules**:
1. **Core Domain**
   - Orders
   - Payments
   - Billing
   - Sessions

2. **Menu Domain**
   - Menu Items
   - Modifiers
   - Combos
   - Categories

3. **Inventory Domain**
   - Items
   - Stock
   - Vendors
   - Restock

4. **Customer Domain**
   - Customers
   - Memberships
   - Loyalty
   - Wallets

5. **Table Domain**
   - Tables
   - Sessions
   - Bills

6. **Settings Domain**
   - Application Settings
   - User Preferences
   - System Configuration

### 7.2 API Consolidation

**Proposed APIs**:
1. **Core API** - Orders, Payments, Billing
2. **Menu API** - Menu management
3. **Inventory API** - Inventory and vendors
4. **Customer API** - Customer management
5. **Tables API** - Table and session management
6. **Settings API** - Settings management
7. **Auth API** - Authentication and authorization

**Benefits**:
- Reduced API count (9 → 7)
- Clearer boundaries
- Easier maintenance
- Better scalability

### 7.3 Frontend Architecture

**Proposed Structure**:
- **Pages** - Main views
- **Components** - Reusable UI components
- **ViewModels** - Business logic
- **Services** - API communication
- **Models** - Data models
- **Utils** - Utilities and helpers

**Modern UI Patterns**:
- Fluent Design System
- Responsive layouts
- Accessibility support
- Dark/Light theme
- Localization ready

### 7.4 Database Design

**Proposed Schema**:
- Single database with clear schema separation
- Foreign key relationships
- Indexes for performance
- Audit trails
- Soft deletes

**Migration Strategy**:
- Preserve existing data
- Gradual migration
- Backward compatibility during transition

---

## 8. MIGRATION MAP

### 8.1 Backend API Mapping

| Old API | New API | Notes |
|---------|---------|-------|
| OrderApi | Core API | Merge with PaymentApi |
| PaymentApi | Core API | Merge with OrderApi |
| MenuApi | Menu API | Keep separate |
| InventoryApi | Inventory API | Keep separate |
| CustomerApi | Customer API | Keep separate |
| TablesApi | Tables API | Keep separate |
| SettingsApi | Settings API | Keep separate |
| UsersApi | Auth API | Rename and expand |
| DiscountApi | Core API | Merge into Core API |

### 8.2 Frontend Page Mapping

| Old Page | New Page | Changes |
|----------|----------|---------|
| DashboardPage | DashboardPage | Enhanced analytics |
| ModernDashboardPage | DashboardPage | Merge into single page |
| TablesPage | TablesPage | Improved UI |
| MenuPage | MenuPage | Better navigation |
| MenuManagementPage | MenuManagementPage | Enhanced features |
| OrdersPage | OrdersPage | Real-time updates |
| PaymentPage | PaymentPage | Streamlined flow |
| InventoryManagementPage | InventoryPage | Unified inventory |
| CustomerManagementPage | CustomersPage | Enhanced CRM |

### 8.3 Service Mapping

| Old Service | New Service | Changes |
|-------------|-------------|---------|
| ApiService | CoreApiService | Consolidated |
| MenuApiService | MenuApiService | Keep |
| OrderApiService | CoreApiService | Merged |
| PaymentApiService | CoreApiService | Merged |
| CustomerApiService | CustomerApiService | Keep |
| TablesApiService | TablesApiService | Keep |
| SettingsApiService | SettingsApiService | Keep |

---

## 9. MISSING/INCOMPLETE LOGIC REPORT

### 9.1 Incomplete Features

1. **Kitchen Display Integration**
   - Service exists but not fully integrated
   - Order status updates incomplete
   - Kitchen UI missing

2. **Customer Intelligence**
   - Segmentation logic exists
   - Behavioral triggers defined but execution incomplete
   - Campaign execution incomplete
   - Analytics dashboard incomplete

3. **Menu Versioning UI**
   - Backend support exists
   - Version comparison UI missing
   - Rollback functionality missing

4. **Receipt Format Designer**
   - UI exists but functionality incomplete
   - Template system incomplete
   - Preview functionality missing

5. **Stale Session Cleanup**
   - Manual cleanup endpoint exists
   - Automatic cleanup not scheduled
   - Cleanup UI missing

### 9.2 Inconsistent Logic

1. **Billing ID Format**
   - Migration from TEXT to UUID in progress
   - Both formats supported
   - Type conversion errors possible

2. **Table Session Items**
   - Legacy: table_sessions.items (JSONB)
   - Modern: orders table
   - Both sources merged (potential duplicates)

3. **Payment Validation**
   - Only checks active sessions
   - Completed bills not validated
   - Payment fails for completed bills

### 9.3 Missing Validations

1. **Stock Negative Prevention**
   - Service-level validation exists
   - Database constraints missing
   - Race conditions possible

2. **Order Modification**
   - Modification allowed after delivery
   - No validation for delivered items
   - Refund logic incomplete

3. **Session Movement**
   - Movement allowed without validation
   - No check for active orders
   - Bill reconciliation incomplete

### 9.4 Performance Issues

1. **Session Heartbeat**
   - Frequent API calls
   - No batching
   - Network overhead

2. **Menu Loading**
   - All items loaded at once
   - No pagination
   - Slow for large menus

3. **Order History**
   - No pagination
   - All orders loaded
   - Performance degradation over time

---

## CONCLUSION

This document provides a comprehensive extraction of all features, logic, workflows, and behaviors from the legacy MagiDesk POS system. Use this as a reference for building the new enterprise-grade application.

**Next Steps**:
1. Review and validate extracted information
2. Design new architecture based on findings
3. Plan migration strategy
4. Implement new system with modern patterns
5. Migrate data and functionality

---

**Document Status**: IN PROGRESS  
**Last Updated**: 2025-01-XX  
**Version**: 1.0

