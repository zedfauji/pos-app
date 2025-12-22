# Failure & Edge Case Matrix

| ID | Scenario | User Action | System Response | Recovery |
|----|----------|-------------|-----------------|----------|
| **F01** | **Underpayment** (User Error) | Tries to pay less than due (in Full Mode) or Partial < 0 | Backend handles Partial. UI allows it (switches to Partial logic) or Backend returns "Partial Accepted". | Valid behavior, just updates Ledger. |
| **F02** | **Overpayment** (User Error) | Tries to pay $100 on $50 bill. | Backend returns error "Overpayment not allowed" (unless Tip). | UI displays error. User corrects amount. |
| **F03** | **Network Failure** | Clicks "Process" while offline. | Command execution fails. Busy indicator stops. | Show "Connection Error". Allow retry. |
| **F04** | **Cancel / Back** | Navigates back to Hub mid-payment. | State is preserved in Backend (Session is still open). VM state is lost. | User re-enters, fetches fresh Ledger. Safe. |
| **F05** | **Double Click** (Race) | Clicks "Process" twice rapidly. | `IsProcessing` flag disables button immediately. Backend uses Idempotency Key (if available) or optimistic locking. | UI prevents double submission. |
| **F06** | **Remote Update** | Someone adds item while User is paying. | Backend `RegisterPayment` checks `TotalDue`. If changed, might reject or accept partial. | ideally, Backend rejects with "Bill Changed". UI refreshes. |
| **F07** | **Zero Balance** | Bill is $0 (All discounted). | User tries to pay. | "Pay" button disabled or acts as "Close". |
| **F08** | **Card Declined** | Sim. Card Terminal rejection. | Backend returns "Payment Failed". | UI allows retry with different method. |
| **F09** | **Rounding Errors** | Split 1/3 of $10.00. | Backend `Calculate` handles rounding (e.g. 3.33, 3.33, 3.34). | UI displays what Backend returns. No client math. |
