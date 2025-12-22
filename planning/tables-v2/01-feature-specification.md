# Feature Specification: Flexible Table Types & Session Workflow

## 1. Overview
This feature transforms the system from a hardcoded "Bar vs. Billiard" model to a fully data-driven configuration system. It allows business owners to define custom `Table Types` (e.g., "Patio", "VIP", "To-Go") with specific behavioral rules. Additionally, it introduces a "Pre-Session" state in the UI to prevent accidental billing and enforces strict financial rules when moving sessions between tables.

## 2. Core Configurable Behaviors (The "Table Type")
Every table in the system will belong to a `Table Type`. The `Table Type` defines **Rules**, not just a label.

### Configurable Properties:
1.  **Name**: Display name (e.g., "Pool Table", "Bar Seat", "VIP Room").
2.  **HasTimer**: 
    *   `true` = Session tracks time and calculates cost based on duration.
    *   `false` = Session is purely for tracking orders (flat fee or consumption only).
3.  **HourlyRate**: (Decimal) Cost per hour if `HasTimer` is true.
4.  **AllowOrders**: 
    *   `true` = Users can add menu items to the bill.
    *   `false` = Time-only billing (rare, but requested flexibility).
5.  **RequiresServer**: 
    *   `true` = Starting a session REQUIRES selecting an employee.
    *   `false` = Can be started anonymously (not recommended, but configurable).

## 3. Workflow Changes

### A. access Workflow (Clicking a Table)
*   **Current**: Clicking an empty table in Legacy *might* start a session or do nothing.
*   **New**: Clicking ANY table (Occupied or Empty) opens the **Table Workspace**.
    *   **Crucial Change**: Opening the workspace does **NOT** start a session/timer.

### B. "Pre-Session" State (Empty Table)
When the Workspace opens for an empty table:
*   **Status Bar**: Shows "Session Not Started".
*   **Actions Available**:
    *   `Start Session` (Primary Action).
    *   `Back/Close` (Exit without changes).
*   **Inputs Required**:
    *   **Server Name**: Must be selected/entered before clicking Start.

### C. Active Session State (Occupied Table)
When the Workspace opens for an active table:
*   **Status Bar**: Shows Timer (if `HasTimer`), Start Time, Server Name.
*   **Actions Available**:
    *   `Add Order Items` (if `AllowOrders`).
    *   `Move Table` (New Logic).
    *   `Stop Session / Print Bill`.

## 4. Move Logic (The "Risk" Engine)
Moving a session is no longer just updating a `TableId`. It requires reconciling the *Rules* of the Source vs. the Destination.

### Scenarios:
1.  **Time-to-Time (Billiard -> Billiard)**:
    *   Timer continues running.
    *   Accrued time moves to new table.
2.  **Time-to-Flat (Billiard -> Bar)**:
    *   **Timer MUST Stop**.
    *   Accrued time cost is calculated, finalized, and added as a "line item" to the bill.
    *   Table status changes to "Occupied" (no timer).
3.  **Flat-to-Time (Bar -> Billiard)**:
    *   Timer STARTS from 00:00.
    *   Existing orders are carried over.
4.  **Flat-to-Flat (Bar -> Patio)**:
    *   Simple transfer of orders.
