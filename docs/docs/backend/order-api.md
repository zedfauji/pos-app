# OrderApi

The OrderApi handles order processing, pricing, validation, and order lifecycle management.

## Overview

**Location:** `solution/backend/OrderApi/`  
**Port:** 5004 (local)  
**Database Schema:** `ord`  
**Framework:** ASP.NET Core 8

## Key Features

- Order creation and management
- Order items with pricing and validation
- Order logs for audit trail
- Integration with InventoryApi for stock checks
- Order totals calculation
- Kitchen service integration

## Configuration

**appsettings.json:**
```json
{
  "Postgres": {
    "InstanceConnectionName": "bola8pos:northamerica-south1:pos-app-1",
    "Username": "posapp",
    "Password": "Campus_66",
    "Database": "posapp",
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=posapp",
    "CloudRunSocketConnectionString": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Username=posapp;Password=Campus_66;Database=posapp;SslMode=Disable"
  },
  "InventoryApi": {
    "BaseUrl": "https://magidesk-inventory-904541739138.northamerica-south1.run.app"
  }
}
```

## Database Schema

### ord.orders
```sql
CREATE TABLE ord.orders (
    order_id BIGSERIAL PRIMARY KEY,
    session_id TEXT NOT NULL,
    billing_id TEXT,
    table_id TEXT,
    server_id TEXT,
    server_name TEXT,
    status TEXT NOT NULL CHECK (status IN ('open', 'closed', 'cancelled')),
    subtotal NUMERIC(18,2) NOT NULL DEFAULT 0,
    profit_total NUMERIC(18,2) NOT NULL DEFAULT 0,
    total NUMERIC(18,2) NOT NULL DEFAULT 0,
    delivery_status TEXT CHECK (delivery_status IN ('pending', 'partial', 'delivered')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    closed_at TIMESTAMPTZ
);
```

### ord.order_items
```sql
CREATE TABLE ord.order_items (
    order_item_id BIGSERIAL PRIMARY KEY,
    order_id BIGINT NOT NULL REFERENCES ord.orders(order_id),
    menu_item_id BIGINT,
    snapshot_name TEXT NOT NULL,
    snapshot_sku TEXT,
    snapshot_category TEXT,
    quantity NUMERIC(18,3) NOT NULL,
    base_price NUMERIC(18,2) NOT NULL,
    vendor_price NUMERIC(18,2),
    line_total NUMERIC(18,2) NOT NULL,
    profit NUMERIC(18,2),
    delivered_quantity NUMERIC(18,3) DEFAULT 0,
    status TEXT CHECK (status IN ('pending', 'confirmed', 'cancelled')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### ord.order_logs
```sql
CREATE TABLE ord.order_logs (
    log_id BIGSERIAL PRIMARY KEY,
    order_id BIGINT NOT NULL REFERENCES ord.orders(order_id),
    action TEXT NOT NULL,
    old_value JSONB,
    new_value JSONB,
    user_id TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Endpoints

### Health Check

**GET** `/health`
- **Description:** Service health check
- **Response:** `200 OK`

### Database Debug

**GET** `/debug/db`
- **Description:** Database connection diagnostics
- **Response:** 
```json
{
  "configured": true,
  "canConnect": true,
  "serverVersion": "PostgreSQL 17.x",
  "database": "postgres",
  "user": "posapp"
}
```

### Orders

**POST** `/api/orders`
- **Description:** Create a new order
- **Request Body:** `CreateOrderRequestDto`
```json
{
  "sessionId": "uuid",
  "billingId": "uuid",
  "tableId": "string",
  "serverId": "string",
  "serverName": "string",
  "items": [
    {
      "menuItemId": 0,
      "quantity": 1.0,
      "basePrice": 10.00,
      "snapshotName": "Item Name"
    }
  ]
}
```
- **Response:** `201 Created` with `OrderDto`

**GET** `/api/orders/&#123;orderId&#125;`
- **Description:** Get order by ID
- **Response:** `200 OK` with `OrderDto` | `404 Not Found`

**GET** `/api/orders/by-session/&#123;sessionId&#125;?includeHistory={bool}`
- **Description:** Get orders by session ID
- **Query Parameters:**
  - `includeHistory` (bool, optional) - Include closed orders
- **Response:** `200 OK` with `OrderDto[]`

**POST** `/api/orders/&#123;orderId&#125;/items`
- **Description:** Add items to existing order
- **Request Body:** `CreateOrderItemDto[]`
- **Response:** `200 OK` with updated `OrderDto`

**PUT** `/api/orders/&#123;orderId&#125;/items/&#123;orderItemId&#125;`
- **Description:** Update order item
- **Request Body:** `UpdateOrderItemDto`
- **Response:** `200 OK` | `404 Not Found`

**DELETE** `/api/orders/&#123;orderId&#125;/items/&#123;orderItemId&#125;`
- **Description:** Delete order item
- **Response:** `204 No Content` | `404 Not Found`

**POST** `/api/orders/&#123;orderId&#125;/close`
- **Description:** Close an order
- **Response:** `200 OK` | `404 Not Found`

### Order Logs

**GET** `/api/orders/&#123;orderId&#125;/logs?page={int}&pageSize={int}`
- **Description:** Get order logs with pagination
- **Query Parameters:**
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
- **Response:** `200 OK` with `PagedResult<OrderLogDto>`

**POST** `/api/orders/&#123;orderId&#125;/logs/recalculate`
- **Description:** Recalculate order totals
- **Response:** `200 OK` with updated totals

## Integration with InventoryApi

### Stock Availability Check

Before adding items to an order, OrderApi checks availability:

**POST** `/api/inventory/availability/check-by-sku`
- **Body:** `[{ "sku": "string", "quantity": number }]`
- **Response:** Availability status for each SKU

### Stock Deduction

After order confirmation, OrderApi deducts stock:

**POST** `/api/inventory/items/adjust-by-sku`
- **Body:** `{ "sku": "string", "delta": number, "reason": "string", "userId": "string" }`
- **Response:** Updated stock level

### Resilience

- Uses **Polly** for HTTP retries
- Handles InventoryApi unavailability gracefully
- Logs integration failures

## Pricing and Validation

### OrderService Logic

1. **Item Validation:**
   - Checks item availability
   - Snapshots price and vendor info
   - Calculates modifier deltas from `menu.modifier_options`
   - Computes profit margin

2. **Combo Validation:**
   - Validates combo availability
   - Validates all child items
   - Snapshots combo price
   - Calculates vendor sum

3. **Totals Calculation:**
   - `RecalculateTotalsAsync()` recomputes:
     - `subtotal` - Sum of all line totals
     - `profit_total` - Sum of all profit amounts
     - `total` - Final order total

## Order Logs

All order operations are logged in `order_logs`:
- **Create:** Logs new order JSON
- **Add Item:** Logs old/new item lists
- **Update Item:** Logs item changes
- **Delete Item:** Logs removal
- **Close:** Logs final order state

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\deploy-order-cloudrun.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-order" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-order-904541739138.northamerica-south1.run.app`

## Related Documentation

- [Payment Flow](../features/payments.md)
- [Table Management](../features/tables.md)
- [Inventory Integration](../backend/inventory-api.md)
