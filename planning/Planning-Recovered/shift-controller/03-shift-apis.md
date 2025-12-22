# Shift Controller - API Design

> All endpoints in new `ShiftController` under `/shifts` route.

---

## API Endpoints Overview

| Endpoint | Method | Purpose | Auth Required |
|----------|--------|---------|---------------|
| `/shifts/current` | GET | Get current open shift (or null) | ✅ |
| `/shifts/open` | POST | Open a new shift | ✅ Manager+ |
| `/shifts/{id}/close` | POST | Close shift with cash declaration | ✅ Manager+ |
| `/shifts/{id}/report` | GET | Get detailed shift report | ✅ |
| `/shifts/{id}/z-report/print` | POST | Print Z-report to receipt printer | ✅ |
| `/shifts/history` | GET | Get paginated shift history | ✅ |
| `/shifts/validate` | GET | Validate shift status for UI gating | ✅ |

---

## 1. GET /shifts/current

**Purpose:** Check if a shift is currently open.

**Response 200:**
```json
{
  "isOpen": true,
  "shift": {
    "shiftId": "uuid",
    "shiftNumber": 275,
    "openedBy": "Juan",
    "openedAt": "2025-12-22T04:50:00Z",
    "startingCash": 500.00,
    "status": "open",
    "timeElapsed": "00:54:02",
    "stats": {
      "cashSales": 2150.00,
      "cardSales": 2800.00,
      "totalSales": 4950.00,
      "activeTables": 3,
      "paidSessions": 10
    }
  }
}
```

**Response 200 (no shift):**
```json
{
  "isOpen": false,
  "shift": null
}
```

---

## 2. POST /shifts/open

**Purpose:** Open a new shift. Fails if shift already open.

**Request:**
```json
{
  "startingCash": 500.00,
  "openedByUserId": "uuid",
  "note": "Opening shift for evening"
}
```

**Response 201:**
```json
{
  "shiftId": "uuid",
  "shiftNumber": 276,
  "openedBy": "Juan",
  "openedAt": "2025-12-22T16:00:00Z",
  "startingCash": 500.00,
  "status": "open"
}
```

**Response 409 (shift already open):**
```json
{
  "error": "SHIFT_ALREADY_OPEN",
  "message": "Shift #275 is already open. Close it before opening a new one.",
  "currentShiftId": "uuid"
}
```

---

## 3. POST /shifts/{id}/close

**Purpose:** Close shift with cash declaration and reconciliation.

**Request:**
```json
{
  "declaredCash": 2100.00,
  "closedByUserId": "uuid",
  "reasonForDifference": "Counting error",
  "categoryForDifference": "counting_error" // or "theft", "float_adjustment", etc.
}
```

**Response 200:**
```json
{
  "shiftId": "uuid",
  "shiftNumber": 275,
  "status": "closed",
  "closedAt": "2025-12-22T23:00:00Z",
  "closedBy": "Juan",
  "startingCash": 500.00,
  "expectedCash": 2150.00,
  "declaredCash": 2100.00,
  "difference": -50.00,
  "differenceCategory": "counting_error",
  "differenceNote": "Counting error"
}
```

**Response 409 (blocking conditions):**
```json
{
  "error": "CLOSE_BLOCKED",
  "blockers": [
    { "type": "ACTIVE_TABLES", "count": 2, "labels": ["Billiard 1", "Bar 3"] },
    { "type": "UNSETTLED_BILLS", "count": 3, "totalAmount": 450.00 }
  ],
  "message": "Cannot close shift: 2 active tables, 3 unsettled bills"
}
```

---

## 4. GET /shifts/{id}/report

**Purpose:** Get comprehensive shift report for display/audit.

**Response 200:**
```json
{
  "shift": {
    "shiftId": "uuid",
    "shiftNumber": 275,
    "openedBy": "Juan",
    "openedAt": "2025-12-22T04:50:00Z",
    "closedAt": null,
    "status": "open"
  },
  "sales": {
    "cashSales": 6300.00,
    "cardSales": 2237.00,
    "totalGross": 8537.00,
    "discounts": 150.00,
    "tips": 530.00,
    "refunds": 0.00,
    "netSales": 8387.00
  },
  "cash": {
    "startingCash": 500.00,
    "cashIn": 6300.00,
    "cashOut": 0.00,
    "expectedCash": 6800.00
  },
  "sessions": {
    "total": 13,
    "paid": 10,
    "unsettled": 3
  },
  "breakdown": {
    "cashSalesCount": 45,
    "cardSalesCount": 23,
    "refundCount": 0,
    "discountCount": 5
  }
}
```

---

## 5. POST /shifts/{id}/z-report/print

**Purpose:** Print end-of-day Z-report to receipt printer.

**Request:**
```json
{
  "printerId": "default" // or specific printer ID
}
```

**Response 200:**
```json
{
  "printed": true,
  "printJobId": "uuid"
}
```

**Response 503 (printer unavailable):**
```json
{
  "error": "PRINTER_UNAVAILABLE",
  "message": "Receipt printer not responding"
}
```

---

## 6. GET /shifts/history

**Purpose:** Get paginated history of closed shifts.

**Query Params:**
- `page` (default: 1)
- `pageSize` (default: 10)
- `fromDate` (ISO date)
- `toDate` (ISO date)
- `userId` (filter by user)

**Response 200:**
```json
{
  "page": 1,
  "pageSize": 10,
  "totalCount": 275,
  "totalPages": 28,
  "items": [
    {
      "shiftId": "uuid",
      "shiftNumber": 275,
      "openedBy": "Juan",
      "openedAt": "2025-12-22T04:50:00Z",
      "closedAt": "2025-12-22T14:55:00Z",
      "duration": "10:05:00",
      "startingCash": 500.00,
      "closingCash": 390.00,
      "difference": 0.00,
      "differenceStatus": "OK" // "OK", "OVER", "SHORT"
    }
  ]
}
```

---

## 7. GET /shifts/validate

**Purpose:** Quick validation for UI to determine if operations are allowed.

**Response 200:**
```json
{
  "canOperate": true,
  "currentShiftId": "uuid",
  "shiftNumber": 275,
  "message": null
}
```

**Response 200 (no shift):**
```json
{
  "canOperate": false,
  "currentShiftId": null,
  "shiftNumber": null,
  "message": "No shift is open. Open a shift to begin operations."
}
```

---

## DTOs

```csharp
// Request DTOs
public record OpenShiftRequest(decimal StartingCash, Guid OpenedByUserId, string? Note);
public record CloseShiftRequest(decimal DeclaredCash, Guid ClosedByUserId, string? ReasonForDifference, string? CategoryForDifference);

// Response DTOs
public record ShiftDto(Guid ShiftId, int ShiftNumber, string OpenedBy, DateTime OpenedAt, decimal StartingCash, string Status, ShiftStatsDto? Stats);
public record ShiftStatsDto(decimal CashSales, decimal CardSales, decimal TotalSales, int ActiveTables, int PaidSessions);
public record ShiftCloseResultDto(Guid ShiftId, int ShiftNumber, string Status, DateTime ClosedAt, decimal ExpectedCash, decimal DeclaredCash, decimal Difference);
public record ShiftBlockerDto(string Type, int Count, decimal? TotalAmount, string[]? Labels);
public record ShiftHistoryItemDto(Guid ShiftId, int ShiftNumber, string OpenedBy, DateTime OpenedAt, DateTime? ClosedAt, TimeSpan? Duration, decimal StartingCash, decimal? ClosingCash, decimal? Difference, string DifferenceStatus);
```

---

## Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `NO_SHIFT_OPEN` | 423 | Operation blocked - no shift |
| `SHIFT_ALREADY_OPEN` | 409 | Cannot open - shift exists |
| `CLOSE_BLOCKED` | 409 | Cannot close - blockers exist |
| `SHIFT_NOT_FOUND` | 404 | Shift ID invalid |
| `SHIFT_ALREADY_CLOSED` | 409 | Cannot close twice |
| `UNAUTHORIZED_ROLE` | 403 | Role cannot perform action |
