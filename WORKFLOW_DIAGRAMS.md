# WORKFLOW DIAGRAMS - MagiDesk POS System

**Date**: 2025-01-XX  
**Commit**: 9239d9723a31e7906d8702cd22b4931f12d948dc  
**Purpose**: Complete step-by-step workflow documentation for all business processes

---

## TABLE OF CONTENTS

1. [Table Session Lifecycle](#1-table-session-lifecycle)
2. [Order Processing Workflow](#2-order-processing-workflow)
3. [Payment Processing Workflow](#3-payment-processing-workflow)
4. [Customer Registration & Management](#4-customer-registration--management)
5. [Inventory Management Workflows](#5-inventory-management-workflows)
6. [Menu Management Workflows](#6-menu-management-workflows)
7. [Discount Application Workflow](#7-discount-application-workflow)
8. [Bill Generation & Settlement](#8-bill-generation--settlement)
9. [Session Recovery Workflow](#9-session-recovery-workflow)
10. [Stale Session Cleanup](#10-stale-session-cleanup)

---

## 1. TABLE SESSION LIFECYCLE

### 1.1 Start Session Workflow

```
STEP 1: User Action
  → User selects table in TablesPage
  → Frontend calls GET /tables/{label}
  → Receives TableStatusDto

STEP 2: Validation
  → Check if table.occupied == false
  → If occupied: Show error "Table is already occupied"
  → If available: Proceed

STEP 3: Start Session Request
  → User clicks "Start Session"
  → Frontend calls POST /tables/{label}/start
  → Request body: { ServerId, ServerName }

STEP 4: Backend Processing (TablesApi)
  → Check for existing active session on table
  → If exists: Return 409 Conflict
  → Generate session_id (UUID)
  → Generate billing_id (UUID)
  → Create table_sessions record:
    - session_id
    - table_label
    - server_id
    - server_name
    - start_time (DateTimeOffset.UtcNow)
    - status = 'active'
    - billing_id
    - items = [] (JSONB)
  → Update table_status:
    - occupied = true
    - start_time = now
    - server = serverName
  → Return: { session_id, billing_id, start_time }

STEP 5: Frontend Processing
  → Store session_id in OrderContext
  → Store billing_id in OrderContext
  → Start heartbeat service
  → Navigate to menu/order page
  → Update UI (table shows as occupied)

STEP 6: Session Active
  → Timer starts (displayed in UI)
  → Orders can be placed
  → Items can be added to session
  → Heartbeat sent periodically
```

### 1.2 Stop Session Workflow

```
STEP 1: User Action
  → User clicks "Stop Session" on occupied table
  → Frontend validates session exists

STEP 2: Close Open Orders
  → Frontend calls OrderApi to get orders by session_id
  → Filter orders with status='open'
  → For each open order:
    → Call POST /api/orders/{orderId}/close
    → Mark order as closed

STEP 3: Stop Session Request
  → Frontend calls POST /tables/{label}/stop

STEP 4: Backend Processing (TablesApi)
  → Find active session for table
  → If not found: Return 404
  → Get table type (billiard/bar/restaurant)
  → Calculate duration:
    - start_time = session.start_time
    - end_time = DateTimeOffset.UtcNow
    - minutes = max(0, (end_time - start_time).TotalMinutes)
  → Load items from table_sessions.items (legacy)
  → Load items from ord.order_items (modern):
    - Query: SELECT items FROM ord.order_items WHERE order.session_id = session_id
    - Filter: is_deleted = false, delivered_quantity > 0
  → Merge items:
    - Create dictionary keyed by item_id
    - If duplicate item_id: Sum quantities
    - Use item name from orders (snapshot_name) if available
  → Calculate costs:
    - timeCost = (tableType == "billiard") ? ratePerMinute * minutes : 0
    - itemsCost = sum(item.price * item.quantity)
    - total = timeCost + itemsCost
  → Close session:
    - UPDATE table_sessions SET end_time = now, status = 'closed'
  → Create bill:
    - INSERT INTO bills (bill_id, billing_id, session_id, table_label, ...)
    - Status: 'awaiting_payment'
    - is_settled: false
    - payment_state: 'not-paid'
  → Free table:
    - UPDATE table_status SET occupied = false, start_time = NULL, server = NULL
  → Return BillResult

STEP 5: Frontend Processing
  → Display bill summary
  → Show payment options
  → Stop heartbeat service
  → Clear OrderContext
  → Update UI (table shows as available)
```

### 1.3 Move Session Workflow

```
STEP 1: User Action
  → User selects occupied table
  → User clicks "Move" or "Transfer"
  → Frontend shows available tables dialog

STEP 2: Validation
  → User selects destination table
  → Frontend calls GET /tables/{toLabel}
  → Check if destination.occupied == false
  → If occupied: Show error "Destination table is occupied"

STEP 3: Move Request
  → Frontend calls POST /tables/{fromLabel}/move?to={toLabel}

STEP 4: Backend Processing (TablesApi)
  → Find active session on source table
  → If not found: Return 404
  → Check destination table availability
  → If occupied: Return 409 Conflict
  → Begin transaction:
    → Create audit record:
      - INSERT INTO table_session_moves (session_id, from_label, to_label, moved_at)
    → Update session:
      - UPDATE table_sessions SET table_label = toLabel
    → Free source table:
      - UPDATE table_status SET occupied = false, start_time = NULL, server = NULL
    → Occupy destination table:
      - UPDATE table_status SET occupied = true, start_time = original_start_time, server = serverName
  → Commit transaction
  → Return: { session_id, billing_id, from, to }

STEP 5: Frontend Processing
  → Update UI (both tables)
  → Show success message
  → Refresh table list
```

---

## 2. ORDER PROCESSING WORKFLOW

### 2.1 Create Order Workflow

```
STEP 1: User Action
  → User selects menu items
  → User selects modifiers (if any)
  → User sets quantities
  → User confirms order

STEP 2: Frontend Processing
  → Build CreateOrderRequestDto:
    - session_id (from OrderContext)
    - billing_id (from OrderContext)
    - table_id
    - server_id
    - items: [{ menuItemId, comboId, quantity, modifiers }]

STEP 3: Order Creation Request
  → Frontend calls POST /api/orders
  → Request: CreateOrderRequestDto

STEP 4: Backend Processing (OrderApi - OrderService)
  → For each item in request:
    → If menuItemId:
      → Check availability: GetMenuItemFlagsAsync()
      → If not available: Throw "ITEM_UNAVAILABLE"
      → Get menu item snapshot: GetMenuItemSnapshotAsync()
      → Calculate modifier delta: ComputeModifierDeltaAsync()
      → Calculate line total: (basePrice + delta) * quantity
      → Calculate profit: (basePrice + delta - vendorPrice) * quantity
    → If comboId:
      → Check availability: GetComboFlagsAsync()
      → Validate combo items: ValidateComboItemsAvailabilityAsync()
      → Get combo snapshot: GetComboSnapshotAsync()
      → Calculate line total: comboPrice * quantity
      → Calculate profit: (comboPrice - vendorSum) * quantity
  → Calculate subtotal: sum(lineTotal)
  → Check inventory (drinks only):
    → Identify drink categories: ["Alcohol", "Beer", "Soda", "Juice", "Water", "Bottled Drinks"]
    → For each drink item:
      → Add to drinkSkuQty list
    → Check availability: CheckInventoryAvailabilityBySkuAsync()
    → If insufficient: Throw "INSUFFICIENT_DRINK_STOCK"
  → Create order:
    → INSERT INTO ord.orders (session_id, billing_id, status='open', delivery_status='pending', ...)
    → Get order_id
  → Create order items:
    → For each priced item:
      → INSERT INTO ord.order_items (order_id, menu_item_id, quantity, base_price, price_delta, line_total, profit, snapshot_name, snapshot_sku, ...)
  → Deduct inventory (drinks only):
    → For each drink:
      → DeductInventoryBySkuAsync(order_id, sku, quantity)
  → Create order log:
    → AppendLogAsync(order_id, "create", ...)
  → Return OrderDto

STEP 5: Frontend Processing
  → Display order confirmation
  → Update order list
  → Show order in kitchen display (if configured)
  → Refresh menu availability
```

### 2.2 Update Order Item Workflow

```
STEP 1: User Action
  → User selects order item
  → User changes quantity or modifiers
  → User saves changes

STEP 2: Update Request
  → Frontend calls PUT /api/orders/{orderId}/items/{itemId}
  → Request: UpdateOrderItemDto { quantity?, modifiers? }

STEP 3: Backend Processing (OrderApi - OrderService)
  → Get existing order
  → Find order item
  → Calculate new values:
    - newQuantity = quantity ?? existingQuantity
    - newLineTotal = (basePrice + priceDelta) * newQuantity
    - newProfit = newLineTotal * 0.3  // ISSUE: Hardcoded 30%
  → Update order item:
    → UPDATE ord.order_items SET quantity = newQuantity, line_total = newLineTotal, profit = newProfit
  → Recalculate order totals:
    → RecalculateTotalsAsync(order_id)
  → Create order log:
    → AppendLogAsync(order_id, "update_item", oldItem, newItem)
  → Return updated OrderDto

STEP 4: Frontend Processing
  → Update order display
  → Refresh totals
```

### 2.3 Mark Items Delivered Workflow

```
STEP 1: User Action
  → Kitchen staff marks items as delivered
  → User enters delivered quantities

STEP 2: Delivery Request
  → Frontend calls POST /api/orders/{orderId}/deliver
  → Request: [{ orderItemId, deliveredQuantity }]

STEP 3: Backend Processing (OrderApi - OrderService)
  → Update delivered quantities:
    → For each delivery:
      → UPDATE ord.order_items SET delivered_quantity = deliveredQuantity
  → Check if all items delivered:
    → allItemsDelivered = all items have deliveredQuantity >= quantity
  → Update order status:
    → If allItemsDelivered:
      → UPDATE orders SET delivery_status = 'delivered', status = 'delivered'
    → Else:
      → UPDATE orders SET delivery_status = 'partial'
  → Create order log:
    → AppendLogAsync(order_id, "mark_delivered", deliveries)
  → Return updated OrderDto

STEP 4: Frontend Processing
  → Update order display
  → Show delivery status
  → Update kitchen display (if configured)
```

---

## 3. PAYMENT PROCESSING WORKFLOW

### 3.1 Register Payment Workflow

```
STEP 1: User Action
  → User views bill (from stopped session)
  → User selects payment method(s)
  → User enters payment amount(s)
  → User applies discount (optional)
  → User adds tip (optional)
  → User confirms payment

STEP 2: Frontend Processing
  → Build RegisterPaymentRequestDto:
    - billing_id (from bill)
    - session_id (from bill)
    - server_id
    - total_due (from bill)
    - lines: [{ amount_paid, payment_method, discount_amount, tip_amount }]

STEP 3: Payment Registration Request
  → Frontend calls POST /api/payments/register
  → Request: RegisterPaymentRequestDto

STEP 4: Backend Processing (PaymentApi - PaymentService)
  → Validate request:
    → Check: lines != null && lines.Count > 0
    → Check: All amounts >= 0
    → Check: billing_id != Guid.Empty
    → Validate billing_id format: ImmutableIdService.IsValidBillingId()
    → Check billing_id exists in TablesApi (active sessions or bills)
      → Call GET /sessions/active
      → Check if billing_id in active sessions
      → If not found: Allow payment to proceed (workaround for completed bills)
  → Begin transaction:
    → Get current ledger: GetLedgerAsync(billing_id)
    → Insert payment records:
      → For each payment line:
        → INSERT INTO pay.payments (session_id, billing_id, amount_paid, payment_method, discount_amount, tip_amount, ...)
    → Calculate totals:
      - addPaid = sum(amount_paid)
      - addDisc = sum(discount_amount)
      - addTip = sum(tip_amount)
    → Upsert ledger:
      → INSERT/UPDATE pay.bill_ledger (billing_id, total_due, total_discount, total_paid, total_tip, status)
      → Calculate status:
        - if (total_paid + total_discount >= total_due): status = 'paid'
        - else if (total_paid > 0): status = 'partial-paid'
        - else: status = 'not-paid'
    → Create payment log:
      → AppendLogAsync(billing_id, "register_payment", oldLedger, newLedger)
  → Commit transaction
  → If fully paid:
    → Notify TablesApi:
      → Call POST /bills/by-billing/{billingId}/settle
      → Mark bill as settled
  → Return BillLedgerDto

STEP 5: Frontend Processing
  → Display payment confirmation
  → Show receipt options
  → Update payment status
  → If fully paid: Mark bill as settled
```

### 3.2 Split Payment Workflow

```
STEP 1: User Action
  → User selects "Split Payment"
  → User enters amounts for each payment method
  → User confirms split

STEP 2: Frontend Processing
  → Calculate split using SplitPaymentCalculator:
    → netAmount = totalAmount + tipAmount - discountAmount
    → Validate: sum(splitAmounts) == netAmount
    → For each payment method:
      → proportion = methodAmount / netAmount
      → methodTip = round(tipAmount * proportion, 2)
      → methodDiscount = round(discountAmount * proportion, 2)
    → Adjust for rounding differences

STEP 3: Payment Registration
  → For each split:
    → Create payment line:
      - amount_paid = splitAmount
      - payment_method = method
      - tip_amount = methodTip
      - discount_amount = methodDiscount
  → Call POST /api/payments/register with all lines

STEP 4: Backend Processing
  → Same as Register Payment Workflow
  → All lines processed in single transaction
```

---

## 4. CUSTOMER REGISTRATION & MANAGEMENT

### 4.1 Customer Registration Workflow

```
STEP 1: User Action
  → User navigates to Customer Registration page
  → User fills registration form:
    - First Name (required)
    - Last Name (required)
    - Email (optional, validated)
    - Phone (optional, validated)
    - Date of Birth (optional)
    - Membership Level (optional, defaults to basic)

STEP 2: Frontend Validation
  → Validate required fields
  → Validate email format (if provided)
  → Validate phone format (if provided)
  → Show validation errors if any

STEP 3: Registration Request
  → Frontend calls POST /api/customers
  → Request: CustomerCreateRequestDto

STEP 4: Backend Processing (CustomerApi - CustomerService)
  → Begin transaction:
    → Get default membership level (if not specified)
    → Create customer record:
      → INSERT INTO customers (first_name, last_name, email, phone, date_of_birth, membership_level_id, ...)
    → Create wallet:
      → INSERT INTO wallets (customer_id, balance=0, max_balance=membershipLevel.maxWalletBalance)
    → Initialize loyalty points: 0
  → Commit transaction
  → Return CustomerDto

STEP 5: Frontend Processing
  → Show success message
  → Navigate to customer details
  → Refresh customer list
```

### 4.2 Process Order for Customer Workflow

```
STEP 1: Order Completion
  → Order is completed and ready for payment
  → Customer is linked to order

STEP 2: Order Processing Request
  → Frontend/Backend calls POST /api/customers/{customerId}/process-order
  → Request: ProcessOrderRequest { orderAmount, walletAmountUsed?, loyaltyPointsRedeemed? }

STEP 3: Backend Processing (CustomerApi - CustomerService)
  → Begin transaction:
    → Get customer with membership level
    → Update customer stats:
      → customer.total_spent += orderAmount
      → customer.total_visits++
    → Process wallet deduction (if specified):
      → If walletAmountUsed > 0:
        → DeductFundsAsync(customerId, walletAmountUsed, description)
        → Create wallet transaction (debit)
    → Process loyalty redemption (if specified):
      → If loyaltyPointsRedeemed > 0:
        → RedeemLoyaltyPointsAsync(customerId, points, description)
        → Create loyalty transaction (redeemed)
    → Calculate and add loyalty points:
      → pointsEarned = CalculatePointsForOrderAsync(customerId, orderAmount)
      → If pointsEarned > 0:
        → AddLoyaltyPointsAsync(customerId, pointsEarned, description)
        → Create loyalty transaction (earned)
    → Check membership upgrade eligibility:
      → If currentSpend >= nextLevelMinimumSpend:
        → Recommend upgrade (or auto-upgrade if configured)
  → Commit transaction
  → Return success

STEP 4: Frontend Processing
  → Update customer display
  → Show loyalty points earned
  → Show membership upgrade notification (if eligible)
```

### 4.3 Membership Upgrade Workflow

```
STEP 1: Eligibility Check
  → Customer total spend calculated
  → Compare with membership level minimums
  → Identify eligible next level

STEP 2: Upgrade Request
  → Automatic (if configured) or Manual
  → Frontend calls PUT /api/customers/{customerId}/membership
  → Request: { membershipLevelId }

STEP 3: Backend Processing (CustomerApi - CustomerService)
  → Validate membership level exists and is active
  → Update customer:
    → UPDATE customers SET membership_level_id = newLevelId
    → If level has validity:
      → SET membership_expiry_date = now + validityMonths
  → Return updated CustomerDto

STEP 4: Frontend Processing
  → Update customer display
  → Show upgrade notification
  → Update membership benefits display
```

---

## 5. INVENTORY MANAGEMENT WORKFLOWS

### 5.1 Stock Adjustment Workflow

```
STEP 1: User Action
  → User identifies stock discrepancy
  → User navigates to inventory item
  → User clicks "Adjust Stock"

STEP 2: Adjustment Dialog
  → User enters:
    - Delta (positive or negative)
    - Reason (required)
    - User ID (optional)

STEP 3: Adjustment Request
  → Frontend calls POST /api/inventory/items/{id}/adjust
  → Request: { delta, reason, userId? }

STEP 4: Backend Processing (InventoryApi)
  → Validate:
    → Check: delta != 0
    → Check: reason != null && reason.Length > 0
    → Get current stock
    → Check: (currentStock + delta) >= 0
    → If negative: Throw error "INSUFFICIENT_STOCK"
  → Create adjustment record:
    → INSERT INTO stock_adjustments (item_id, delta, reason, user_id, created_at)
  → Update inventory:
    → UPDATE inventory_items SET stock = stock + delta
  → Check low stock alert:
    → If newStock <= lowStockThreshold:
      → Trigger low stock alert
  → Return updated ItemDto

STEP 5: Frontend Processing
  → Update inventory display
  → Show adjustment confirmation
  → Refresh stock levels
  → Show low stock alert (if applicable)
```

### 5.2 Restock Request Workflow

```
STEP 1: Low Stock Detection
  → System detects low stock
  → Frontend shows low stock alert
  → User clicks "Create Restock Request"

STEP 2: Restock Request Creation
  → Frontend calls POST /api/inventory/restock
  → Request: RestockRequestDto { item_id, quantity, vendor_id?, notes? }

STEP 3: Backend Processing (InventoryApi)
  → Create restock request:
    → INSERT INTO restock_requests (item_id, quantity, vendor_id, status='pending', ...)
  → Notify vendor (if configured):
    → Send notification via configured channel
  → Return restock request ID

STEP 4: Vendor Fulfillment
  → Vendor receives notification
  → Vendor fulfills order
  → Vendor updates restock status (if vendor portal exists)

STEP 5: Stock Received
  → User receives stock
  → User adjusts inventory:
    → POST /api/inventory/items/{id}/adjust
    → delta = received quantity
    → reason = "Restock from vendor"
  → Update restock request:
    → UPDATE restock_requests SET status = 'fulfilled', fulfilled_at = now
```

---

## 6. MENU MANAGEMENT WORKFLOWS

### 6.1 Create Menu Item Workflow

```
STEP 1: User Action
  → User navigates to Menu Management
  → User clicks "Create Menu Item"

STEP 2: Menu Item Dialog
  → User enters:
    - Name (required)
    - Description
    - Base Price (required)
    - Category
    - SKU (optional, links to inventory)
    - Picture URL
    - Modifiers (optional)
    - Availability (default: true)

STEP 3: Create Request
  → Frontend calls POST /api/menu/items
  → Request: CreateMenuItemDto

STEP 4: Backend Processing (MenuApi - MenuService)
  → Validate:
    → Check SKU uniqueness: ExistsSkuAsync(sku, excludeId)
    → If duplicate: Throw error
  → Create menu item:
    → INSERT INTO menu.menu_items (name, description, base_price, category, sku, ...)
  → Create menu version snapshot:
    → INSERT INTO menu.menu_item_versions (menu_item_id, version, base_price, ...)
  → Link to inventory (if SKU provided):
    → Update inventory item: is_menu_available = true
  → Return MenuItemDto

STEP 5: Frontend Processing
  → Refresh menu list
  → Show success message
  → Navigate to menu item details (optional)
```

### 6.2 Menu Item Availability Check Workflow

```
STEP 1: Menu Display
  → Frontend loads menu items
  → For each item, check availability

STEP 2: Availability Check (MenuApi - MenuService)
  → For each menu item:
    → If isDrinkCategory(item.category):
      → Call InventoryApi: CheckItemAvailabilityAsync(sku, 1)
      → If hasStock:
        → item.isAvailable = true
      → Else:
        → item.isAvailable = false (but still show item)
    → Else:
      → item.isAvailable = item.isAvailable (from menu item flag)

STEP 3: Frontend Display
  → Show available items normally
  → Show unavailable items with "Out of Stock" badge
  → Disable ordering for unavailable items
```

---

## 7. DISCOUNT APPLICATION WORKFLOW

### 7.1 Auto-Apply Discounts Workflow

```
STEP 1: Order/Bill Creation
  → Order created or bill generated
  → Customer ID available

STEP 2: Auto-Apply Request
  → Frontend/Backend calls POST /api/discounts/auto-apply
  → Request: { billing_id, customer_id }

STEP 3: Backend Processing (DiscountApi - DiscountService)
  → Get available discounts:
    → GetAvailableDiscountsAsync(customer_id, billing_id)
  → Filter auto-apply discounts:
    → discounts.Where(d => d.IsAutoApply == true)
    → Sort by priority (ascending)
  → Apply discounts in priority order:
    → For each auto-apply discount:
      → Check eligibility
      → Calculate discount amount
      → ApplyDiscountAsync(billing_id, discount_id)
      → Update bill total
  → Return: { appliedDiscounts, totalSavings }

STEP 4: Frontend Processing
  → Display applied discounts
  → Update bill total
  → Show savings amount
```

### 7.2 Manual Discount Application Workflow

```
STEP 1: User Action
  → User views bill
  → User clicks "Apply Discount"
  → Frontend shows available discounts

STEP 2: Discount Selection
  → User selects discount
  → Frontend shows discount details
  → User confirms application

STEP 3: Apply Request
  → Frontend calls POST /api/discounts/apply
  → Request: { billing_id, discount_id, discount_type }

STEP 4: Backend Processing (DiscountApi - DiscountService)
  → Validate discount eligibility
  → Calculate discount amount:
    → If percentage: orderAmount * (percentage / 100)
    → If fixed: min(fixedAmount, orderAmount)
  → Apply discount:
    → INSERT INTO applied_discounts (billing_id, discount_id, discount_amount, ...)
  → Update bill:
    → Calculate new total = oldTotal - discountAmount
  → Return: { success, discountAmount, newTotal }

STEP 5: Frontend Processing
  → Update bill display
  → Show discount applied
  → Update total amount
```

---

## 8. BILL GENERATION & SETTLEMENT

### 8.1 Bill Generation Workflow

```
STEP 1: Session Stop
  → User stops table session
  → TablesApi processes stop request

STEP 2: Bill Calculation (TablesApi)
  → Load items from table_sessions.items (legacy)
  → Load items from ord.order_items (modern)
  → Merge items by item_id
  → Calculate:
    - itemsCost = sum(item.price * item.quantity)
    - timeCost = (tableType == "billiard") ? ratePerMinute * minutes : 0
    - total = itemsCost + timeCost

STEP 3: Bill Creation
  → INSERT INTO bills:
    - bill_id (UUID)
    - billing_id (from session)
    - session_id
    - table_label
    - server_id, server_name
    - start_time, end_time
    - total_time_minutes
    - items (JSONB)
    - time_cost
    - items_cost
    - total_amount
    - status = 'awaiting_payment'
    - is_settled = false
    - payment_state = 'not-paid'

STEP 4: Return Bill
  → Return BillResult to frontend
  → Frontend displays bill summary
```

### 8.2 Bill Settlement Workflow

```
STEP 1: Payment Completion
  → Payment registered via PaymentApi
  → Payment ledger updated
  → If fully paid: status = 'paid'

STEP 2: Settlement Notification
  → PaymentApi calls TablesApi:
    → POST /bills/by-billing/{billingId}/settle

STEP 3: Bill Settlement (TablesApi)
  → Update bill:
    → UPDATE bills SET is_settled = true, payment_state = 'paid'
  → Return success

STEP 4: Frontend Processing
  → Mark bill as settled
  → Show settlement confirmation
  → Enable receipt printing
```

### 8.3 Bill Reopening Workflow

```
STEP 1: User Action
  → User views unsettled bill
  → User clicks "Reopen Bill"
  → Frontend shows reopen dialog

STEP 2: Reopen Request
  → Frontend calls POST /bills/{billingId}/reopen
  → Request: { targetTableLabel? } (optional, defaults to original table)

STEP 3: Backend Processing (TablesApi)
  → Get bill by billing_id:
    → SELECT bill WHERE billing_id = billingId AND is_settled = false
  → Load items from bill.items
  → Load items from ord.order_items (where order.billing_id = billingId)
  → Merge items by item_id
  → Determine target table:
    → targetTable = targetTableLabel ?? originalTableLabel
  → Check table availability:
    → If targetTable != originalTable AND targetTable.occupied:
      → Return 409 Conflict
  → Create new session:
    → Generate new session_id
    → Use existing billing_id (or create new)
    → INSERT INTO table_sessions (session_id, table_label, billing_id, items, status='active', ...)
  → Update table status:
    → UPDATE table_status SET occupied = true, start_time = now, server = serverName
  → Update orders:
    → UPDATE ord.orders SET session_id = newSessionId WHERE billing_id = billingId
  → Return: { sessionId, billingId, tableLabel }

STEP 4: Frontend Processing
  → Restore session context
  → Start heartbeat
  → Navigate to menu/order page
  → Show success message
```

---

## 9. SESSION RECOVERY WORKFLOW

### 9.1 App Startup Recovery

```
STEP 1: App Initialization
  → App.xaml.cs OnLaunched()
  → InitializeApiAsync() completes
  → RecoverActiveSessionsAsync() called

STEP 2: Fetch Active Sessions
  → Frontend calls GET /sessions/active
  → Receives list of active sessions

STEP 3: Recovery Dialog
  → If sessions found:
    → Show SessionRecoveryDialog
    → Display list of active sessions:
      - Table label
      - Server name
      - Start time
      - Duration
    → User options:
      - Resume selected session
      - Close all sessions

STEP 4: Resume Session
  → User selects session
  → Frontend calls ResumeSessionAsync(sessionId)
  → Set OrderContext:
    - CurrentSessionId = sessionId
    - CurrentBillingId = billingId (from session)
  → Start heartbeat service
  → Navigate to appropriate page

STEP 5: Close All Sessions
  → User selects "Close All"
  → For each session:
    → Close open orders
    → Call POST /tables/{tableLabel}/stop
    → Free table
```

### 9.2 Manual Session Recovery

```
STEP 1: User Action
  → User navigates to Sessions page
  → User views active sessions
  → User selects session to recover

STEP 2: Recovery Action
  → User clicks "Resume Session"
  → Frontend calls ResumeSessionAsync(sessionId)
  → Same as App Startup Recovery Step 4
```

---

## 10. STALE SESSION CLEANUP

### 10.1 Automatic Cleanup Workflow

```
STEP 1: Scheduled Trigger
  → Background job or manual trigger
  → Calls POST /sessions/cleanup-stale?timeoutMinutes=5

STEP 2: Backend Processing (TablesApi)
  → Get auto-stop config from SettingsApi:
    → GET /api/settings/app?host={host}
    → Read: extras.tables.session.autoStopMinutes
  → Find stale sessions:
    → SELECT sessions WHERE status='active' AND (last_heartbeat IS NULL OR last_heartbeat < now - timeoutMinutes)
  → For each stale session:
    → Close session:
      → UPDATE table_sessions SET end_time = now, status = 'closed'
    → Load items (from table_sessions.items and ord.order_items)
    → Merge items
    → Calculate bill:
      → timeCost = (tableType == "billiard") ? ratePerMinute * minutes : 0
      → itemsCost = sum(item.price * item.quantity)
      → total = timeCost + itemsCost
    → Create bill:
      → INSERT INTO bills (status='awaiting_payment', ...)
    → Free table:
      → UPDATE table_status SET occupied = false
  → Return: { cleanedCount, timeoutMinutes }

STEP 3: Notification (if configured)
  → Notify administrators of cleaned sessions
  → Log cleanup actions
```

### 10.2 Heartbeat Workflow

```
STEP 1: Session Active
  → Frontend HeartbeatService running
  → Periodically sends heartbeat

STEP 2: Heartbeat Request
  → Frontend calls POST /sessions/{sessionId}/heartbeat
  → Request: (empty body)

STEP 3: Backend Processing (TablesApi)
  → Update heartbeat timestamp:
    → UPDATE table_sessions SET last_heartbeat = now WHERE session_id = sessionId AND status = 'active'
  → Return: { sessionId, heartbeat = now }

STEP 4: Frontend Processing
  → Continue heartbeat cycle
  → If heartbeat fails: Attempt recovery
```

---

## CONCLUSION

These workflows document the complete step-by-step processes for all major business operations in the MagiDesk POS system. Each workflow includes:

1. User actions and UI interactions
2. Frontend processing and API calls
3. Backend processing and database operations
4. Validation and error handling
5. State updates and UI refreshes

**Key Patterns Identified**:
- Session-based ordering (all orders linked to table sessions)
- Dual item sources (legacy JSONB + modern orders table)
- Time-based charging only for billiard tables
- Inventory checks only for pre-made drinks
- Automatic discount application with priority
- Session recovery on app restart
- Stale session cleanup with configurable timeout

**Recommendations**:
1. Standardize on orders table (remove legacy JSONB support)
2. Implement scheduled stale session cleanup
3. Add proper error handling and retry logic
4. Implement transaction rollback on failures
5. Add audit logging for all state changes

---

**Document Status**: COMPLETE  
**Last Updated**: 2025-01-XX  
**Version**: 1.0

