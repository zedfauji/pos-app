| Entity / Table | Classification | Reason |
|:---|:---|:---|
| **ord.orders** | IMMUTABLE | Financial record of customer intent. |
| **ord.order_items** | IMMUTABLE | Line items define the contract. |
| **pay.payments** | IMMUTABLE | Money received is a historical fact. |
| **pay.payment_ledger** | IMMUTABLE | Ledger must be append-only. |
| **billing.bills** | IMMUTABLE | Issued bill is a snapshot in time. |
| **billing.bill_items** | IMMUTABLE | Cannot change what was billed. |
| **public.shifts** | IMMUTABLE | Shift boundaries are legal records. |
| **audit.events** | IMMUTABLE | The log itself must be tamper-proof. |
| **tables.sessions** | MUTABLE | State machine (Active -> Closed). |
| **menu.items** | MUTABLE | Prices/Names change over time (versioned). |
| **settings.app_settings** | MUTABLE | Configuration changes. |
| **users.users** | MUTABLE | Profile updates allowed. |
| **customers.profiles** | MUTABLE | Customer details update. |
