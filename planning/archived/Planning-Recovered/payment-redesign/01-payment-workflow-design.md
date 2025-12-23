# Payment Workflow Design

## Overview
This document outlines the UX and workflow for the new page-based Payment Workspace. 
The goal is to move from a dialog-based flow to a comprehensive Payment Hub and Workspace, enabling granular control over payments while keeping the UI logic-free.

## 1. High-Level Screens

### A. Payment Hub Page
*   **Purpose**: Dashboard showing all active table sessions that require payment or management.
*   **Entry**: Main Navigation -> "Payments".
*   **UI Components**:
    *   **Grid/List of Cards**: Each card represents an active session (Table #, Server Name, Open Time, Current Total).
    *   **Status Indicators**: "Occupied", "Bill Printed", "Partial Payment", "Overdue".
    *   **Quick Actions**: "Pay" button (Primary) -> Navigates to Payment Workspace.

### B. Payment Workspace Page
*   **Purpose**: Dedicated screen for processing payment for a specific session.
*   **Entry**: From Payment Hub (Click "Pay") or Table Map (Click "Pay").
*   **Layout**: Two-Column Layout.
    *   **Left Column (Action Area)**: Payment controls, split options, number pad, tender inputs.
    *   **Right Column (Bill View)**: Receipt preview, list of items, subtotal, tax, discount, total, **Remaining Due**.

## 2. Detailed Workflows

### Scenario 1: Full Payment (Standard)
1.  **User Details**: User lands on Workspace.
2.  **Default State**: "Pay Full Amount" mode is active.
3.  **Display**: Right column shows full bill. "Remaining Due" is highlighted.
4.  **User Action**: Selects Payment Method (Cash/Card).
    *   *If Cash*: Enters Amount Tendered (or selects "Exact").
    *   *If Card*: Triggers terminal integration (simulated for now) or Manual Entry.
5.  **Process**: User clicks "Process Payment".
6.  **Backend Interaction**: `POST /payments` with full amount.
7.  **Response**: Backend checks totals. Returns success + Change Due (if cash).
8.  **Completion**: 
    *   System shows "Payment Successful".
    *   "Change Due" displayed prominently.
    *   If Remaining == 0, "Close Session" button appears (or auto-closes after timeout).

### Scenario 2: Split by Amount (Partial)
1.  **User Action**: Clicks "Split / Partial" tab in Left Column.
2.  **Selection**: Selects "Split by Amount".
3.  **Input**: Enters amount (e.g., $20.00).
4.  **Validation**: UI sends amount to backend (optional pre-check) or just allows entry. (Strictly: UI just sends intent).
5.  **Process**: User clicks "Process Payment".
6.  **Backend Interaction**: `POST /payments` with $20.00.
7.  **Update**:
    *   Right column updates: "Paid: $20.00", "Remaining: $X.XX".
    *   Session remains Open.
    *   User can continue processing next payment.

### Scenario 3: Split by Item
1.  **User Action**: Clicks "Split by Item" tab.
2.  **Interaction**: Right column becomes selectable. User ticks items (e.g., "Burger", "Coke").
3.  **Calculation (Server-Side)**:
    *   UI sends `[ItemId1, ItemId2]` to `POST /billing/calculate-split` (Proposed).
    *   Backend returns `{ TotalToPay: 15.50 }`.
4.  **Display**: Action Area shows "$15.50 selected".
5.  **Process**: User pays $15.50.
6.  **Update**: Items are marked as "Paid" visually (if backend supports status) or just Ledger updates total paid.

### Scenario 4: Split by Percentage
1.  **User Action**: Clicks "Split by %".
2.  **Input**: Selects "50%" or enters custom %.
3.  **Calculation**: UI requests calculation from backend (or if backend sends distinct "GrandTotal", UI *could* display 50% purely visually, but safer to ask Backend "What is 50% of Remaining?").
    *   *Decision*: UI sends `Percentage: 50` to backend calc endpoint? Or logic-free: The backend endpoint `POST /billing/calculate-split` can accept `{ percentage: 50 }`.
4.  **Process**: User pays calculated amount.

## 3. Navigation States & Transitions

*   **Payment Hub -> Workspace**: Pass `SessionID`.
*   **Workspace -> Payment Hub**: Back button (Top Left).
*   **Workspace -> Close**:
    *   When Balance == 0:
    *   Prominent "Finalize & Close Table" button actions `POST /close`.
    *   On success -> Navigate back to Hub or Map.

## 4. Error & Recovery Paths

*   **Payment Rejected**:
    *   Display error message from backend (e.g., "Card Declined").
    *   Stay on Workspace. Do not clear cart/amount (let user retry).
*   **Network Error**:
    *   "Offline / Connectivity Issue".
    *   Disable "Process" button.
*   **Concurrent Modification** (e.g., another waiter adds item):
    *   Backend returns "Bill Updated" error on payment attempt.
    *   UI auto-refreshes ledger. User must confirm new total.
*   **Overpayment (Logic check)**:
    *   Backend rejects payment > remaining (unless it's a tip).
    *   UI displays backend validation error.
