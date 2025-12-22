# API Contracts V2: Flexible Tables

## 1. Table Configuration Endpoints (New)

### GET `/api/table-types`
Returns available types for configuration or frontend logic.
**Response**:
```json
[
  {
    "id": 1,
    "name": "Billiard",
    "hasTimer": true,
    "hourlyRate": 15.00,
    "allowOrders": true
  },
  {
    "id": 2,
    "name": "Bar",
    "hasTimer": false,
    "allowOrders": true
  }
]
```

## 2. Table Status Updates

### GET `/tables` (Updated DTO)
The `TableStatusDto` must now include the Type rules so the frontend knows whether to show a Timer or not.

**Updated DTO**:
```csharp
public class TableStatusDto
{
    public string Label { get; set; }
    public string TypeName { get; set; } // "Billiard"
    public int TypeId { get; set; }
    public TableConfigDto Config { get; set; } // Nested rules
    public bool Occupied { get; set; }
    // ... existing fields
}

public class TableConfigDto 
{
    public bool HasTimer { get; set; }
    public bool AllowOrders { get; set; }
}
```

## 3. Session Management

### POST `/tables/{label}/start`
No change to signature, but internal logic must validate `RequiresServer` if configured.

### POST `/tables/{label}/move` (Breaking Change / Major Update)
Current: `?to={targetLabel}`
New: Requires body to handle confirmations if pricing changes.

**Request**:
```json
{
  "targetTableLabel": "Bar-1",
  "acceptBillingChange": true // User acknowledges timer might stop
}
```

**Response**:
```json
{
  "success": true,
  "actionTaken": "TimerStopped", // Enum: TimerStopped, TimerStarted, OrdersMovedOnly
  "previousSessionCostFinalized": 12.50, // If timer stopped
  "message": "Moved to Bar-1. Timer stopped. $12.50 added to bill."
}
```

## 4. Workflows

### Pre-Session Validation
The Frontend calls `GET /tables` to know if it requires a Server Name. Use `TableConfigDto.RequiresServer` to toggle the UI validation.
