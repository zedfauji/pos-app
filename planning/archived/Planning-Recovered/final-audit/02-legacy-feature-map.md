# 02 - Legacy System Feature Extraction

## 1. Core Operations
**Description**: The daily bread-and-butter of the restaurant.
- **Table Management**: Table map, status tracking (Occupied/Free).
- **Ordering**: Taking orders, sending to kitchen (assumed implicit print or strict workflow).
- **Bill Printing**: Generating physical receipts.
- **Status**: **Mapped** (All exist in New System).

## 2. Finance / Billing
**Description**: Money handling.
- **Bill Calculation**: Tax, Discounts, Service Charge. (Legacy likely had this hardcoded).
- **Split Bill**: Crucial for groups. (Complex legacy feature).
- **Payment Methods**: Cash, Card, Vouchers.
- **Status**: **Functionally Mapped** (New System has logic), but Split Bill needs verification.

## 3. Reporting (Legacy High Value)
**Description**: Managers relying on these for closing.
- **End-of-Day Report (Z-Report)**: **CRITICAL**. Total sales, total cash, total card.
- **Shift Report (X-Report)**: Current status.
- **Item Sales Report**: What sold the most?
- **Status**: **UNKNOWN / MISSING** in New System.

## 4. Admin / Maintenance
**Description**: Managing the system.
- **Menu Management**: Adding items/prices.
- **User Management**: Waiter/Admin pins.
- **Status**: **Mapped** (`InventoryApi`, `UsersApi`).

## 5. Hidden / Edge Cases (Implicit Legacy)
- **Offline Mode**: Legacy likely worked offline (local DB). New System is "Read-Only" when offline. **CHANGE OF CONTRACT**.
- **Cash Drawer Kick**: Opening the drawer on print. (New system needs to ensure printer command sends pulse).
- **Refunds / Voids**: How to correct a closed order? (Legacy usually allows admin void). NOT explicit in New System Flows.
