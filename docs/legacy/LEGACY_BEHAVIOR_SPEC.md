# Legacy Behavior Specification
**Status**: APPROVED LEGACY BASLINE
**Purpose**: Defines the behavioral contract for the Parity Phase.

## 1. Table Management & Sessions

### 1.1 Session Start
- **Precondition**: Table must be `Free`.
- **Action**: User clicks "Start Table".
- **Legacy Behavior**: System creates a `table_sessions` record and a `billing_id`. Table becomes `Occupied`.
- **Weird Behavior**: If a previous session crashed or wasn't closed properly, the system might show it as free but fail to start. *Legacy Fix*: System identifies stale sessions and allows force-close.
- **Side Effects**: Creates `billing_id` (UUID) immediately. Sets table to `Occupied`.

### 1.2 Session Stop (Bill Generation)
- **Precondition**: Session is `Active`.
- **Calculation**:
  - **Billiard**: `(EndTime - StartTime).TotalMinutes * Rate`.
  - **Other**: `0` time cost.
  - **Items**: Merges `table_sessions.items` (Legacy JSON) + `ord.order_items` (Modern). *Deduplicates by ItemId*.
- **Weird Behavior**: If `EndTime < StartTime` (system clock skew), minutes = 0. Never negative.
- **Side Effects**: Generates `Bill` record. Frees table.
- **Output**: `BillResult` object via API.

## 2. Orders

### 2.1 Order Creation
- **Precondition**: Active Session exists.
- **Action**: User adds items -> "Submit Order".
- **Inventory Check**: Only for "Drink" categories (Beer, Soda, etc.). Food is *always* available (no check).
- **Snapshotting**: Copies `Price` and `Name` from Menu at moment of order. Future menu changes *do not* affect active orders.

### 2.2 Order Modification
- **Status Rules**: Can modify only if `Status != Delivered`.
- **Financial Rule**: `Profit` calculation on update is HARDCODED to `Total * 0.3` (30%). *Do not fix this.* This is a known legacy math quirk.
- **Weird Behavior**: Changing quantity to 0 deletes the line item.

## 3. Payments

### 3.1 Payment Registration
- **Precondition**: `BillingId` exists.
- **Validation**: Accepts payment even for "Closed" tables (late settlement).
- **Split Logic**:
  - Allows N-users to split.
  - Tip/Discount distributed proportionally.
  - **Rounding**: Any penny difference (`Total - Sum(Splits)`) is dumped into the *largest* split share.
- **Completion**: `TotalPaid + TotalDiscount >= TotalDue`.
- **Legacy Workaround**: Payment API manually ignores "Active Session" check if `BillingId` refers to a `Bill` (historic data).

### 3.2 Immutability (Legacy vs Parity)
- **Legacy**: Allowed some limited mutation cleanup.
- **Parity Rule**: STRICT APPEND-ONLY. If legacy allowed mutation, we must now use **Compensating Transactions** (e.g., Void = Negative Insert).

## 4. Shifts & Cash (Caja)
- **Shift Open**: Required before any payment.
- **Shift Close**: locks system.
- **Reports**:
  - "End of Day": Sums all `Payments` where `CreatedAt` is within Shift window.
  - **Weird Behavior**: If a session spans across two shifts, payment counts towards the *Shift that received the money*, not the Session start time.

## 5. Known "Weird" Flows (Must Preserve)
1.  **Zombie Sessions**: If heartbeat fails > 5 mins, session is considered "Stale" but not closed until manual user action or "Cleanup" button press.
2.  **Reopen Bill**: Can reopen a settled bill. *Effect*: Creates a NEW session, clones items. Original bill stays "Settled". Double-counting risk if not careful (Parity must duplicate this logic exactly).
3.  **Partial Cash**: A session cannot be closed if `TotalPaid < TotalDue`. *However*, legacy UI sometimes allowed "Store Credit" or "Write-off" (implemented as 100% discount).

## 6. Financial Invariants (Non-Negotiable)
- **Rounding**: Banker's Rounding (MidpointEven) for unit prices. Truncation OK for display, but backend stores generic `numeric`.
- **Currency**: Always `decimal` in code.
- **Negative Stock**: `InventoryApi` prevents it at service level. *If parity migration finds negative stock, it must error.*
