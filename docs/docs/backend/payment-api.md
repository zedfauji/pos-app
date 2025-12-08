# PaymentApi

The PaymentApi handles payment processing, split payments, discounts, and payment history.

## Overview

**Location:** `solution/backend/PaymentApi/`  
**Port:** 5005 (local)  
**Database Schema:** `pay`  
**Framework:** ASP.NET Core 8

## Key Features

- Payment registration (single and split payments)
- Bill-level discounts
- Payment history and audit logs
- Bill ledger tracking
- Multiple payment methods
- Tips and discounts

## Database Schema

### pay.payments
```sql
CREATE TABLE pay.payments (
    payment_id BIGSERIAL PRIMARY KEY,
    session_id TEXT NOT NULL,
    billing_id TEXT NOT NULL,
    amount_paid NUMERIC(18,2) NOT NULL,
    currency TEXT NOT NULL DEFAULT 'USD',
    payment_method TEXT NOT NULL,
    discount_amount NUMERIC(18,2) NOT NULL DEFAULT 0,
    discount_reason TEXT,
    tip_amount NUMERIC(18,2) NOT NULL DEFAULT 0,
    external_ref TEXT,
    meta JSONB,
    created_by TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### pay.bill_ledger
```sql
CREATE TABLE pay.bill_ledger (
    billing_id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    total_due NUMERIC(18,2) NOT NULL,
    total_discount NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_paid NUMERIC(18,2) NOT NULL DEFAULT 0,
    total_tip NUMERIC(18,2) NOT NULL DEFAULT 0,
    status TEXT NOT NULL CHECK (status IN ('unpaid', 'partial', 'paid')),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### pay.payment_logs
```sql
CREATE TABLE pay.payment_logs (
    log_id BIGSERIAL PRIMARY KEY,
    billing_id TEXT NOT NULL,
    session_id TEXT,
    action TEXT NOT NULL,
    old_value JSONB,
    new_value JSONB,
    server_id TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Status Calculation

Bill status is calculated as:
- **`paid`**: `total_paid + total_discount >= total_due`
- **`partial`**: `total_paid > 0` but not fully paid
- **`unpaid`**: `total_paid = 0`

## Endpoints

### Register Payment (Split Supported)

**POST** `/api/payments`
- **Description:** Register payment(s) for a bill (supports split payments)
- **Request Body:**
```json
{
  "sessionId": "uuid",
  "billingId": "uuid",
  "totalDue": 27.99,
  "lines": [
    {
      "amountPaid": 20.00,
      "paymentMethod": "card",
      "discountAmount": 0.00,
      "discountReason": null,
      "tipAmount": 2.00,
      "externalRef": "AUTH1234",
      "meta": {
        "cardLast4": "1234",
        "transactionId": "TXN-123"
      }
    },
    {
      "amountPaid": 5.99,
      "paymentMethod": "cash",
      "discountAmount": 0.00,
      "tipAmount": 0.00
    }
  ],
  "serverId": "user-id"
}
```
- **Response:** `201 Created` with `BillLedgerDto`

### Apply Discount

**POST** `/api/payments/&#123;billingId&#125;/discounts?sessionId={id}&reason={text}&serverId={id}`
- **Description:** Apply bill-level discount
- **Query Parameters:**
  - `sessionId` (string, required)
  - `reason` (string, optional)
  - `serverId` (string, required)
- **Request Body:** Discount amount (decimal)
- **Response:** `200 OK` with `BillLedgerDto`

### Get Bill Payments

**GET** `/api/payments/&#123;billingId&#125;`
- **Description:** List all payments for a bill
- **Response:** `200 OK` with `PaymentDto[]`

### Get Ledger Summary

**GET** `/api/payments/&#123;billingId&#125;/ledger`
- **Description:** Get bill ledger summary
- **Response:** `200 OK` with `BillLedgerDto` | `404 Not Found`

### Payment Logs

**GET** `/api/payments/&#123;billingId&#125;/logs?page={int}&pageSize={int}`
- **Description:** Get payment logs with pagination
- **Query Parameters:**
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
- **Response:** `200 OK` with `PagedResult<PaymentLogDto>`

## Payment Methods

Supported payment methods:
- `cash` - Cash payment
- `card` - Credit/Debit card
- `mobile` - Mobile payment (Apple Pay, Google Pay, etc.)
- `digital` - Digital wallet
- Custom methods can be added

## Split Payment Flow

1. **Calculate Split:**
   - User enters amounts for each payment method
   - System validates total equals `totalDue`
   - Distributes tip and discount proportionally

2. **Process Payments:**
   - Creates payment record for each split line
   - Updates bill ledger
   - Logs all payment actions

3. **Update Status:**
   - Calculates new bill status
   - Updates `bill_ledger` totals
   - Returns updated ledger

## Integration

### Frontend Configuration

**appsettings.json:**
```json
{
  "PaymentApi": {
    "BaseUrl": "https://magidesk-payment-904541739138.northamerica-south1.run.app"
  }
}
```

### PaymentApiService Usage

```csharp
// Register split payment
var paymentRequest = new RegisterPaymentRequest
{
    SessionId = sessionId,
    BillingId = billingId,
    TotalDue = 100.00m,
    Lines = new[]
    {
        new PaymentLine { AmountPaid = 60.00m, PaymentMethod = "card" },
        new PaymentLine { AmountPaid = 40.00m, PaymentMethod = "cash" }
    },
    ServerId = userId
};

var ledger = await _paymentService.RegisterPaymentAsync(paymentRequest);
```

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\deploy-payment-cloudrun.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-payment" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-payment-904541739138.northamerica-south1.run.app`

## Notes

- `totalDue` can be provided by caller (from OrderApi/Billing)
- Ledger caches `totalDue` on first payment
- For refunds/voids, extend API with negative payment legs or dedicated void endpoint
- All financial amounts use `NUMERIC(18,2)` for precision

## Related Documentation

- [Payment Flow](../features/payments.md)
- [Split Payments](../features/split-payments.md)
- [Order Processing](../backend/order-api.md)
