# BUSINESS LOGIC WORKBOOK - MagiDesk POS System

**Date**: 2025-01-XX  
**Commit**: 9239d9723a31e7906d8702cd22b4931f12d948dc  
**Purpose**: Complete extraction of ALL business rules, calculations, validations, and formulas

---

## TABLE OF CONTENTS

1. [Order Processing Calculations](#1-order-processing-calculations)
2. [Payment Processing Calculations](#2-payment-processing-calculations)
3. [Table Session Calculations](#3-table-session-calculations)
4. [Inventory Calculations](#4-inventory-calculations)
5. [Customer & Loyalty Calculations](#5-customer--loyalty-calculations)
6. [Discount Calculations](#6-discount-calculations)
7. [Menu & Pricing Calculations](#7-menu--pricing-calculations)
8. [Validation Rules](#8-validation-rules)
9. [Business Constraints](#9-business-constraints)
10. [Edge Cases & Special Rules](#10-edge-cases--special-rules)

---

## 1. ORDER PROCESSING CALCULATIONS

### 1.1 Order Item Price Calculation

**Location**: `solution/backend/OrderApi/Services/OrderService.cs:32-33`

**Formula**:
```
lineTotal = (basePrice + modifierDelta) * quantity
profit = (basePrice + modifierDelta - vendorPrice) * quantity
```

**Where**:
- `basePrice` = Menu item base price (snapshot at order creation)
- `modifierDelta` = Sum of all selected modifier option prices
- `vendorPrice` = Vendor cost for the item
- `quantity` = Ordered quantity (minimum 1, enforced by `Math.Max(1, quantity)`)

**Example**:
- Base price: $10.00
- Modifier options: +$2.00 (size), +$1.50 (extra cheese)
- Modifier delta: $3.50
- Vendor price: $5.00
- Quantity: 2
- Line total: ($10.00 + $3.50) * 2 = $27.00
- Profit: ($10.00 + $3.50 - $5.00) * 2 = $17.00

### 1.2 Combo Price Calculation

**Location**: `solution/backend/OrderApi/Services/OrderService.cs:42-46`

**Formula**:
```
comboPrice = sum(componentPrices) - comboDiscount
lineTotal = comboPrice * quantity
profit = (comboPrice - vendorSum) * quantity
```

**Where**:
- `componentPrices` = Prices of all items in the combo
- `comboDiscount` = Discount applied to combo (if any)
- `vendorSum` = Sum of vendor costs for all combo components
- `modifierDelta` = 0 (combo-level modifiers not implemented)

**Note**: Combo-level modifiers are NOT implemented (delta = 0)

### 1.3 Order Total Calculation

**Location**: `solution/backend/OrderApi/Repositories/OrderRepository.cs:421`

**Formula**:
```
subtotal = sum(lineTotal) for all non-deleted items
total = subtotal - discountTotal + taxTotal
profitTotal = sum(profit) for all non-deleted items
```

**Where**:
- `subtotal` = Sum of all line totals
- `discountTotal` = Total discount applied to order
- `taxTotal` = Total tax calculated on (subtotal - discountTotal)
- `profitTotal` = Sum of all item profits

**Recalculation Trigger**:
- When items are added
- When items are updated
- When items are deleted
- When quantity changes

### 1.4 Order Item Update Calculation

**Location**: `solution/backend/OrderApi/Services/OrderService.cs:162-163`

**Formula**:
```
newLineTotal = (basePrice + priceDelta) * newQuantity
newProfit = newLineTotal * 0.3  // HARDCODED 30% profit margin
```

**Issue**: Profit calculation uses hardcoded 30% instead of actual vendor price difference.

**Should be**:
```
newProfit = (basePrice + priceDelta - vendorPrice) * newQuantity
```

### 1.5 Order Status Transitions

**Valid States**:
- `open` → `waiting` → `in-progress` → `delivered` → `closed`
- `open` → `closed` (cancellation)
- `delivered` → `closed` (completion)

**Delivery Status**:
- `pending` - No items delivered
- `partial` - Some items delivered
- `delivered` - All items delivered

**Calculation**:
```
allItemsDelivered = all items have deliveredQuantity >= quantity
if allItemsDelivered:
    deliveryStatus = "delivered"
    orderStatus = "delivered"
else:
    deliveryStatus = "partial"
```

---

## 2. PAYMENT PROCESSING CALCULATIONS

### 2.1 Payment Ledger Calculation

**Location**: `solution/backend/PaymentApi/Services/PaymentService.cs:128`

**Formula**:
```
totalPaid = sum(amountPaid) for all payment lines
totalDiscount = sum(discountAmount) for all payment lines
totalTip = sum(tipAmount) for all payment lines

status = 
    if (totalPaid + totalDiscount >= totalDue): "paid"
    else if (totalPaid > 0): "partial-paid"
    else: "not-paid"
```

**Where**:
- `totalDue` = Bill total amount
- Payment can be complete via: `totalPaid + totalDiscount >= totalDue`
- Tips are separate and don't count toward payment completion

### 2.2 Split Payment Calculation

**Location**: `solution/frontend/Services/SplitPaymentCalculator.cs:27-49`

**Formula**:
```
netAmount = totalAmount + tipAmount - discountAmount

// Validate split amounts sum to net amount
splitTotal = sum(splitAmounts)
if |splitTotal - netAmount| > 0.01:
    throw error

// Proportional distribution
for each payment method:
    proportion = methodAmount / netAmount
    methodTip = round(tipAmount * proportion, 2)
    methodDiscount = round(discountAmount * proportion, 2)
    methodNet = methodAmount - methodDiscount

// Adjust for rounding differences
tipDifference = tipAmount - sum(methodTips)
discountDifference = discountAmount - sum(methodDiscounts)
// Apply difference to largest split
```

**Rounding Adjustment**:
- Differences > $0.01 are adjusted
- Adjustment applied to largest split amount
- Ensures totals match exactly

### 2.3 Payment Validation Rules

**Location**: `solution/backend/PaymentApi/Services/PaymentService.cs:21-35`

**Validations**:
1. Payment lines must exist: `req.Lines != null && req.Lines.Count > 0`
2. All amounts non-negative: `amountPaid >= 0 && discountAmount >= 0 && tipAmount >= 0`
3. Billing ID not empty: `req.BillingId != Guid.Empty`
4. Billing ID format valid: `ImmutableIdService.IsValidBillingId()`
5. Billing ID exists in TablesApi (active session or bill)

**Payment Status**:
- `not-paid` - No payments registered
- `partial-paid` - Some payment made but not complete
- `paid` - Fully paid (totalPaid + totalDiscount >= totalDue)
- `partial-refunded` - Partial refund issued
- `refunded` - Full refund issued
- `cancelled` - Payment cancelled

---

## 3. TABLE SESSION CALCULATIONS

### 3.1 Time Cost Calculation

**Location**: `solution/backend/TablesApi/Program.cs:744-746`

**Formula**:
```
minutes = max(0, (endTime - startTime).TotalMinutes)
timeCost = 
    if (tableType == "billiard"):
        ratePerMinute * minutes
    else:
        0  // Bar and restaurant tables don't charge time
```

**Where**:
- `ratePerMinute` = Configurable rate from `app_settings` table (key: 'Tables.RatePerMinute')
- Default: 0 if not configured
- Only billiard tables charge time-based fees

**Example**:
- Start: 10:00 AM
- End: 11:30 AM
- Minutes: 90
- Rate: $0.50/minute
- Time cost: $45.00

### 3.2 Bill Total Calculation

**Location**: `solution/backend/TablesApi/Program.cs:259-261`

**Formula**:
```
itemsCost = sum(item.price * item.quantity) for all items
timeCost = (calculated above, only for billiard tables)
totalAmount = timeCost + itemsCost
```

**Item Sources** (merged):
1. Legacy: `table_sessions.items` (JSONB)
2. Modern: `ord.order_items` where `order.session_id = session_id`

**Merging Logic**:
- Items deduplicated by `item_id`
- Quantities merged if same item_id exists in both sources
- Final items list = merged unique items

### 3.3 Session Duration Calculation

**Location**: `solution/backend/TablesApi/Program.cs:651`

**Formula**:
```
startTime = session.start_time (DateTimeOffset)
endTime = DateTimeOffset.UtcNow (or session.end_time if closed)
minutes = max(0, (endTime - startTime).TotalMinutes)
```

**Edge Cases**:
- If endTime < startTime: minutes = 0
- If session not closed: uses current time for calculation
- Minutes always >= 0 (no negative durations)

---

## 4. INVENTORY CALCULATIONS

### 4.1 Profit Margin Calculation

**Location**: Multiple locations

**Formula**:
```
margin = (sellingPrice - vendorPrice) / sellingPrice * 100
profit = (sellingPrice - vendorPrice) * quantity
marginPercentage = profit / (sellingPrice * quantity) * 100
```

**Where**:
- `sellingPrice` = Price charged to customer
- `vendorPrice` = Cost from vendor
- `quantity` = Quantity sold

**Example**:
- Selling price: $10.00
- Vendor price: $6.00
- Quantity: 5
- Profit: ($10.00 - $6.00) * 5 = $20.00
- Margin: ($10.00 - $6.00) / $10.00 * 100 = 40%

### 4.2 Stock Availability Calculation

**Location**: `solution/backend/InventoryApi/`

**Formula**:
```
availableStock = currentStock - reservedStock
canFulfill = availableStock >= requestedQuantity
```

**Where**:
- `currentStock` = Physical stock in inventory
- `reservedStock` = Stock reserved for pending orders
- `requestedQuantity` = Quantity needed for order

**Validation**:
- Stock cannot go negative (enforced at service level)
- Database constraints may be missing (race conditions possible)

### 4.3 Inventory Value Calculation

**Formula**:
```
totalValue = sum(currentStock * vendorPrice) for all items
categoryValue = sum(currentStock * vendorPrice) for items in category
```

**Low Stock Alert**:
```
isLowStock = currentStock <= lowStockThreshold
```

**Where**:
- `lowStockThreshold` = Configurable per item or category
- Default threshold may be set in settings

---

## 5. CUSTOMER & LOYALTY CALCULATIONS

### 5.1 Loyalty Points Calculation

**Location**: `solution/backend/CustomerApi/Services/LoyaltyService.cs:38-42`

**Formula**:
```
basePoints = floor(orderAmount)  // 1 point per dollar
multipliedPoints = floor(basePoints * membershipMultiplier)
```

**Where**:
- `orderAmount` = Total order amount (decimal)
- `membershipMultiplier` = Multiplier from membership level (e.g., 1.0, 1.5, 2.0)
- Points are floored (no fractional points)

**Example**:
- Order amount: $47.50
- Membership multiplier: 1.5
- Base points: floor(47.50) = 47
- Final points: floor(47 * 1.5) = 70 points

### 5.2 Loyalty Points Expiry

**Location**: `solution/backend/CustomerApi/Services/LoyaltyService.cs:67`

**Formula**:
```
expiryDate = earnedDate + 2 years  // HARDCODED
isExpired = expiryDate < currentDate
```

**Expiry Processing**:
- Points expire after 2 years (hardcoded)
- Expired points are deducted from customer balance
- Expiry transaction created for audit

### 5.3 Membership Upgrade Eligibility

**Location**: `solution/backend/CustomerApi/Services/CustomerService.cs`

**Formula**:
```
isEligible = currentTotalSpent >= nextLevelMinimumSpend
recommendedLevel = lowest level where minimumSpend <= currentTotalSpent
```

**Where**:
- `currentTotalSpent` = Customer's lifetime spending
- `nextLevelMinimumSpend` = Minimum spend required for next membership level
- Upgrade is automatic or manual (depending on implementation)

### 5.4 Wallet Balance Calculation

**Location**: `solution/backend/CustomerApi/Services/WalletService.cs`

**Formula**:
```
currentBalance = sum(creditTransactions) - sum(debitTransactions)
maxBalance = membershipLevel.maxWalletBalance
canAddFunds = (currentBalance + amount) <= maxBalance
```

**Where**:
- Wallet transactions are immutable (credit/debit only)
- Balance cannot exceed max balance for membership level
- Transactions tracked for audit

### 5.5 Birthday Bonus Calculation

**Location**: `solution/backend/CustomerApi/Services/LoyaltyService.cs:318`

**Formula**:
```
isBirthday = (today.month == customer.birthday.month) && 
             (today.day == customer.birthday.day)
bonusPoints = membershipLevel.birthdayBonusPoints
```

**Rules**:
- Bonus given once per year
- Only if membership level has birthday bonus configured
- Bonus points added to loyalty balance

---

## 6. DISCOUNT CALCULATIONS

### 6.1 Percentage Discount

**Location**: `solution/backend/DiscountApi/Services/DiscountService.cs`

**Formula**:
```
discountAmount = orderAmount * (discountPercentage / 100)
finalAmount = orderAmount - discountAmount
```

**Where**:
- `discountPercentage` = Discount percentage (0-100)
- `orderAmount` = Original order total
- `discountAmount` = Amount to deduct

**Example**:
- Order amount: $100.00
- Discount: 15%
- Discount amount: $100.00 * 0.15 = $15.00
- Final amount: $100.00 - $15.00 = $85.00

### 6.2 Fixed Amount Discount

**Formula**:
```
discountAmount = min(fixedDiscountAmount, orderAmount)
finalAmount = orderAmount - discountAmount
```

**Where**:
- `fixedDiscountAmount` = Fixed discount amount
- Discount cannot exceed order amount

### 6.3 Discount Eligibility Rules

**Location**: `solution/backend/DiscountApi/Services/DiscountService.cs:27-129`

**New Customer Discount**:
```
eligible = (customer.totalVisits <= 1)
discountPercentage = 15%
autoApply = true
priority = 1
```

**Returning Customer Discount**:
```
eligible = (customer.totalVisits > 1) && (daysSinceLastVisit > 30)
discountPercentage = 10%
autoApply = true
priority = 2
```

**VIP Customer Discount**:
```
eligible = (customer.totalSpent > 1000)
discountPercentage = 25%
autoApply = false
priority = 1
```

**Frequent Customer Discount**:
```
eligible = (customer.totalVisits >= 10)
discountPercentage = 15%
autoApply = false
priority = 2
```

**Birthday Discount**:
```
eligible = (today matches customer birthday)  // Mock logic in code
discountPercentage = 20%
autoApply = false
priority = 1
```

### 6.4 Discount Stacking

**Rules**:
- Multiple discounts can apply
- Applied in priority order
- Auto-apply discounts applied first
- Manual discounts applied after

**Calculation**:
```
remainingAmount = orderAmount
for each discount in priority order:
    if eligible:
        discountAmount = calculateDiscount(remainingAmount, discount)
        remainingAmount -= discountAmount
        totalDiscount += discountAmount
```

---

## 7. MENU & PRICING CALCULATIONS

### 7.1 Menu Item Price with Modifiers

**Location**: `solution/backend/MenuApi/Services/MenuService.cs`

**Formula**:
```
itemTotal = basePrice + sum(modifierOptionPrices)
```

**Where**:
- `basePrice` = Menu item base price
- `modifierOptionPrices` = Prices of selected modifier options
- Modifiers can be required or optional

### 7.2 Combo Price Calculation

**Location**: `solution/backend/MenuApi/Services/MenuService.cs:95`

**Formula**:
```
computedPrice = sum(componentItemPrices) - comboDiscount
```

**Where**:
- `componentItemPrices` = Prices of all items in combo
- `comboDiscount` = Discount applied to combo (if any)
- Combo price can be less than sum of components

### 7.3 Menu Item Availability

**Location**: `solution/backend/MenuApi/Services/MenuService.cs:26-44`

**Logic**:
```
if (isDrinkCategory(item.category)):
    hasStock = inventoryService.CheckItemAvailability(item.sku, 1)
    isAvailable = hasStock && item.isAvailable
else:
    isAvailable = item.isAvailable  // Food items don't need inventory check
```

**Drink Categories** (pre-made only):
- "Alcohol", "Beer", "Soda", "Juice", "Water", "Bottled Drinks"
- Coffee, tea, etc. are made-to-order (no inventory check)

---

## 8. VALIDATION RULES

### 8.1 Order Validation

**Location**: `solution/backend/OrderApi/Services/OrderService.cs`

**Rules**:
1. Item or Combo required: `menuItemId != null || comboId != null`
2. Quantity minimum: `quantity >= 1` (enforced by `Math.Max(1, quantity)`)
3. Menu item availability: `isAvailable == true`
4. Combo availability: All combo items must be available
5. Inventory check: Only for drinks (pre-made categories)
6. Session exists: `sessionId` must exist in `table_sessions`

### 8.2 Payment Validation

**Location**: `solution/backend/PaymentApi/Services/PaymentService.cs`

**Rules**:
1. Payment lines required: `lines != null && lines.Count > 0`
2. Amounts non-negative: `amountPaid >= 0 && discountAmount >= 0 && tipAmount >= 0`
3. Billing ID format: Must be valid UUID and immutable format
4. Billing ID exists: Must exist in TablesApi (active session or bill)
5. Ledger settlement: `totalPaid + totalDiscount >= totalDue` for completion

### 8.3 Table Session Validation

**Location**: `solution/backend/TablesApi/Program.cs`

**Rules**:
1. No active session: Table must not have active session before starting
2. Destination available: Target table must not be occupied for moves
3. Session exists: Session must exist before stopping
4. Table type valid: Must be "billiard", "bar", or "restaurant"

### 8.4 Inventory Validation

**Location**: `solution/backend/InventoryApi/`

**Rules**:
1. Stock non-negative: `currentStock >= 0` (enforced at service level)
2. SKU uniqueness: SKU must be unique per vendor
3. Adjustment reason: Stock adjustments require reason
4. Quantity positive: Adjustment quantity must be > 0

### 8.5 Customer Validation

**Location**: `solution/backend/CustomerApi/Services/CustomerService.cs`

**Rules**:
1. First name required: `firstName != null && firstName.Length > 0`
2. Last name required: `lastName != null && lastName.Length > 0`
3. Email format: Valid email format if provided
4. Phone format: Valid phone format if provided
5. Membership level exists: `membershipLevelId` must exist and be active
6. Wallet balance: `balance <= maxBalance` for membership level

### 8.6 Discount Validation

**Location**: `solution/backend/DiscountApi/Services/DiscountService.cs`

**Rules**:
1. Discount amount positive: `discountAmount > 0`
2. Discount not exceed order: `discountAmount <= orderAmount`
3. Validity period: Discount must be within validity dates
4. Minimum order: Order amount must meet minimum requirement
5. Customer eligibility: Customer must meet discount criteria

---

## 9. BUSINESS CONSTRAINTS

### 9.1 Immutability Constraints

**Billing ID**:
- Format must be immutable (validated by ImmutableIdService)
- Cannot be modified after creation
- Used for payment tracking and audit

**Payment Records**:
- Once created, cannot be modified
- Refunds create new records
- Ledger is append-only

**Wallet Transactions**:
- Transactions are immutable (credit/debit only)
- Cannot be deleted or modified
- Audit trail maintained

### 9.2 Data Integrity Constraints

**Order Items**:
- Snapshot data preserved at order creation
- Menu item changes don't affect existing orders
- Price changes don't affect historical orders

**Session Items**:
- Legacy: `table_sessions.items` (JSONB) maintained
- Modern: Items from `ord.order_items`
- Both sources merged when generating bills

**Stock Levels**:
- Cannot go negative (service-level enforcement)
- Database constraints may be missing
- Race conditions possible in concurrent scenarios

### 9.3 Business Rules

**Table Types**:
- Billiard tables: Charge time-based fees
- Bar tables: No time charges
- Restaurant tables: No time charges

**Order Fulfillment**:
- Only drinks (pre-made) require inventory check
- Food items are made-to-order (no inventory)
- Inventory deducted only for drinks

**Membership Levels**:
- Automatic upgrade based on total spend
- Manual upgrade allowed
- Expiry dates enforced
- Default level assigned to new customers

---

## 10. EDGE CASES & SPECIAL RULES

### 10.1 Order Item Update Profit Calculation

**Issue**: Hardcoded 30% profit margin
**Location**: `solution/backend/OrderApi/Services/OrderService.cs:163`

**Current**:
```csharp
var newProfit = newLineTotal * 0.3m;  // WRONG
```

**Should be**:
```csharp
var newProfit = (cur.BasePrice + cur.PriceDelta - vendorPrice) * qty;
```

### 10.2 Payment Validation for Completed Bills

**Issue**: Payment validation only checks active sessions
**Location**: `solution/backend/PaymentApi/Services/PaymentService.cs:55-91`

**Problem**:
- Completed bills exist in `bills` table
- Payment validation checks `sessions/active` endpoint
- Completed bills not in active sessions
- Payment fails for completed bills

**Workaround**: Code allows payment to proceed even if not found in active sessions (line 89-91)

### 10.3 Session Item Merging

**Location**: `solution/backend/TablesApi/Program.cs:213-255`

**Logic**:
1. Load items from `table_sessions.items` (legacy)
2. Load items from `ord.order_items` (modern)
3. Merge by `item_id`
4. Sum quantities if duplicate `item_id`
5. Use item name from orders (snapshot_name) if available

**Edge Cases**:
- Same item in both sources: Quantities merged
- Different items: Both included
- Missing item_id: New GUID generated

### 10.4 Stale Session Cleanup

**Location**: `solution/backend/TablesApi/Program.cs:1390-1554`

**Logic**:
```
staleTimeout = timeoutMinutes (default: 5 minutes)
staleSessions = sessions where (last_heartbeat IS NULL OR last_heartbeat < now - timeoutMinutes)

for each stale session:
    1. Close session (status='closed', end_time=now)
    2. Calculate bill (time cost + items cost)
    3. Create bill record
    4. Free table (occupied=false)
```

**Note**: Auto-cleanup not scheduled (manual trigger only)

### 10.5 Session Recovery

**Location**: `solution/frontend/App.xaml.cs:601-627`

**Logic**:
1. On app startup, fetch active sessions from server
2. If sessions found, show recovery dialog
3. User can:
   - Resume selected session
   - Close all sessions
4. Resumed session: Restore session context, start heartbeat

### 10.6 Bill Reopening

**Location**: `solution/backend/TablesApi/Program.cs:1046-1246`

**Logic**:
1. Find unsettled bill by `billing_id`
2. Load items from bill and orders
3. Merge items (deduplicate by item_id)
4. Create new session with merged items
5. Update orders to link to new `session_id`
6. Table can be same or different (if available)

**Rules**:
- Can reopen to same table even if occupied
- Cannot reopen to different occupied table
- Original bill remains for history

### 10.7 Auto-Stop Enforcement

**Location**: `solution/backend/TablesApi/Program.cs:125-290`

**Logic**:
1. Fetch `tables.session.autoStopMinutes` from SettingsApi
2. Find active sessions older than autoStopMinutes
3. For each stale session:
   - Close session
   - Generate bill
   - Free table

**Configuration**:
- Setting: `tables.session.autoStopMinutes` (in SettingsApi app settings)
- Only applies to billiard tables (time-based charging)
- Bar/restaurant tables not auto-stopped

---

## CONCLUSION

This workbook documents all business logic, calculations, validations, and constraints found in the legacy MagiDesk POS system. Use this as a reference for implementing the new enterprise-grade application.

**Key Findings**:
1. Some calculations use hardcoded values (e.g., 30% profit, 2-year expiry)
2. Payment validation has workarounds for completed bills
3. Session items merged from legacy and modern sources
4. Multiple discount types with priority-based application
5. Time-based charging only for billiard tables

**Recommendations**:
1. Make all constants configurable
2. Fix payment validation to check bills table
3. Standardize on orders table for items (remove legacy support)
4. Implement proper discount engine with rules engine
5. Add database constraints for stock non-negative

---

**Document Status**: COMPLETE  
**Last Updated**: 2025-01-XX  
**Version**: 1.0

