# Parity Gap Matrix
**Status**: DRAFT
**Purpose**: Tracks gap between Legacy Behavior Spec and Configured System.

| ID | Behavior | Legacy Status | Parity Status | Gap Type | Resolution Plan |
|----|----------|---------------|---------------|----------|-----------------|
| **1.0** | **Table/Session** | | | | |
| 1.1 | Start Session | Works (with quirks) | ✅ Matches | None | - |
| 1.2 | Stop Session (Bill) | Works | ✅ Matches | None | Backend handles calculation correctly. |
| 1.3 | Reopen Bill | Works (creates new session) | ⚠️ Partial | UI Gap | Backend supports it. Frontend needs "Reopen" button in History. |
| 1.4 | Move Table | Works | ✅ Matches | None | Implemented in `TablesPage`. |
| 1.5 | Stale Cleanup | Manual | ❌ Missing | Ops Gap | No UI for "Cleanup Stale Sessions" in Admin. |
| **2.0** | **Orders** | | | | |
| 2.1 | Create Order | Works | ✅ Matches | None | `OrdersPage` functioning. |
| 2.2 | Modify Order | Works (30% profit bug) | ✅ Matches | None | Logic preserved (including the bug). |
| 2.3 | Kitchen Print | Works | ❓ Unknown | I/O Gap | Does WinUI 3 print to physical printer? |
| **3.0** | **Payments** | | | | |
| 3.1 | Process Payment | Works | ✅ Matches | None | `PaymentPage` functioning. |
| 3.2 | Split Payment | Works | ⚠️ Partial | UI Gap | `SplitPaymentCalculator` ported, but UI flow usually simpler in current state. |
| 3.3 | Partial Cash | Allowed ("Store Credit") | ❌ Missing | Logic Gap | Current system prevents closing if `Paid < Due`. Need "Write-off" button. |
| 3.4 | Refund / Void | Works | ❌ Missing | UI/Backend Gap | No "Void" button in Payment History. backend supports status update? |
| **4.0** | **Shifts** | | | | |
| 4.1 | Open/Close Shift | Works | ⚠️ Partial | Frontend Gap | Shift Controller Backend exists. Frontend integration "In Progress". |
| 4.2 | End of Day Report | Works | ❌ Missing | Reporting Gap | No "EOD Report" view in WinUI. |
| 4.3 | Cash Drawer (Caja) | Works | ❌ Missing | Hardware Gap | No command sent to open drawer (Generic Printer command). |
| **5.0** | **Inventory** | | | | |
| 5.1 | Auto-Deduct Stock | Works (Drinks only) | ✅ Matches | None | Logic exists in `OrderService`. |
| 5.2 | Restock | Works | ✅ Matches | None | `RestockPage` exists. |
| **6.0** | **Financials** | | | | |
| 6.1 | Rounding | Banker's | ✅ Matches | None | .NET `decimal` default. |
| 6.2 | Tax Calculation | Standard | ✅ Matches | None | Logic preserved. |
| 6.3 | Immutability | Loose | ✅ Improved | - | Immutability enforced via DB triggers (Stronger than legacy). |

## Summary of Critical Gaps
1.  **Shift UI**: Operators cannot open/close shifts.
2.  **Reports**: No End-of-Day visibility.
3.  **Refunds/Voids**: No way to correct mistakes.
4.  **Partial Cash**: Cannot close "short" bills (Store Credit).
5.  **Hardware**: Printing/Cash Drawer likely not wired up.
