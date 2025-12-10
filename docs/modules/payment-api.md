# Payment API Module

This module exposes endpoints to register payments (including splits), apply bill-level discounts, retrieve bill ledger summaries, and list payment logs. It integrates with sessions and billing via `sessionId` and `billingId`.

- Project: `solution/backend/PaymentApi/`
- Entry point: `Program.cs`
- Schema: `pay.payments`, `pay.bill_ledger`, `pay.payment_logs`
- Deployment: Cloud Run (see `solution/backend/deploy-payment-cloudrun.ps1`)

## Data Model (PostgreSQL)

- `pay.payments` (one row per payment leg)
  - `session_id`, `billing_id`, `amount_paid`, `payment_method`, `discount_amount`, `tip_amount`
  - `external_ref`, `meta` (JSON) for acquirer/terminal details
  - `created_by` (server_id), `created_at`
- `pay.bill_ledger` (aggregated state per billing_id)
  - `total_due`, `total_discount`, `total_paid`, `total_tip`, `status` (unpaid|partial|paid)
- `pay.payment_logs` (audit)
  - action, old/new JSON, server_id, created_at

Status rule: `paid` if `total_paid + total_discount >= total_due`; `partial` if > 0; else `unpaid`.

## Endpoints

Base path: `/api/payments`

- Register payments (split supported)
  - `POST /api/payments`
  - Body:
    ```json
    {
      "sessionId": "sess-123",
      "billingId": "bill-789",
      "totalDue": 27.99,
      "lines": [
        { "amountPaid": 20.00, "paymentMethod": "card", "discountAmount": 0.00, "discountReason": null, "tipAmount": 2.00, "externalRef": "AUTH1234", "meta": { "cardLast4": "1234" } },
        { "amountPaid": 5.00,  "paymentMethod": "cash", "discountAmount": 0.00, "discountReason": null, "tipAmount": 0.00 }
      ],
      "serverId": "u456"
    }
    ```
  - 201 → `BillLedgerDto` summary

- Apply discount (bill-level)
  - `POST /api/payments/{billingId}/discounts?sessionId={id}&reason={text}&serverId={id}`
  - Body (decimal): `3.00`
  - 200 → `BillLedgerDto`

- List bill payments
  - `GET /api/payments/{billingId}` → 200 `[PaymentDto]`

- Get ledger summary
  - `GET /api/payments/{billingId}/ledger` → 200 `BillLedgerDto` | 404

- Payment logs
  - `GET /api/payments/{billingId}/logs?page=&pageSize=` → 200 `PagedResult<PaymentLogDto>`

## Frontend Integration

- Configure `frontend/appsettings.json`:
  ```json
  {
    "PaymentApi": { "BaseUrl": "https://magidesk-payment-904541739138.northamerica-south1.run.app" }
  }
  ```
- Use a `PaymentApiService` to:
  - Register payments (single or split)
  - Apply discounts
  - Fetch ledger and payments
  - Retrieve logs for receipt/audit panels

## Deployment

- PowerShell: `solution/backend/deploy-payment-cloudrun.ps1`
  - Example:
    ```powershell
    .\backend\deploy-payment-cloudrun.ps1 -ProjectId bola8pos -Region northamerica-south1 -ServiceName magidesk-payment -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
    ```

## Notes

- `totalDue` can be provided by the caller (e.g., from OrderApi/Billing). The ledger caches it on first payment and uses it for status calculations.
- For refunds/voids, extend the API to write negative legs or a dedicated void/refund endpoint and reverse ledger deltas.
