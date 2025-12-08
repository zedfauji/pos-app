# InventoryApi

The InventoryApi manages inventory items, stock levels, vendors, and inventory transactions.

## Overview

**Location:** `solution/backend/InventoryApi/`  
**Port:** 5006 (local)  
**Database Schema:** `inventory`  
**Framework:** ASP.NET Core 8

## Key Features

- Inventory item management
- Stock tracking and adjustments
- Vendor management
- Inventory transactions (audit trail)
- Low stock alerts
- Margin reports
- Integration with MenuApi

## Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "InventoryDb": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Username=posapp;Password=Campus_66;Database=posapp;SslMode=Disable"
  },
  "Postgres": {
    "LocalConnectionString": "Host=localhost;Port=5432;Username=posapp;Password=Campus_66;Database=posapp",
    "CloudRunSocketConnectionString": "Host=/cloudsql/bola8pos:northamerica-south1:pos-app-1;Username=posapp;Password=Campus_66;Database=posapp;SslMode=Disable"
  }
}
```

## Database Schema

### inventory.vendors
```sql
CREATE TABLE inventory.vendors (
    vendor_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT NOT NULL,
    contact_info TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    budget NUMERIC(18,2),
    reminder TEXT,
    reminder_enabled BOOLEAN DEFAULT false,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### inventory.inventory_items
```sql
CREATE TABLE inventory.inventory_items (
    item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID REFERENCES inventory.vendors(vendor_id),
    category_id UUID REFERENCES inventory.inventory_categories(category_id),
    sku TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    description TEXT,
    unit TEXT,
    barcode TEXT,
    reorder_threshold NUMERIC(18,3),
    buying_price NUMERIC(18,2),
    selling_price NUMERIC(18,2),
    tax_rate NUMERIC(5,2),
    is_menu_available BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### inventory.inventory_stock
```sql
CREATE TABLE inventory.inventory_stock (
    item_id UUID PRIMARY KEY REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE,
    quantity_on_hand NUMERIC(18,3) NOT NULL DEFAULT 0,
    last_counted_at TIMESTAMPTZ
);
```

### inventory.inventory_transactions
```sql
CREATE TABLE inventory.inventory_transactions (
    transaction_id BIGSERIAL PRIMARY KEY,
    item_id UUID NOT NULL REFERENCES inventory.inventory_items(item_id),
    transaction_type TEXT NOT NULL CHECK (transaction_type IN ('purchase', 'sale', 'adjustment', 'return', 'waste')),
    quantity_delta NUMERIC(18,3) NOT NULL,
    previous_quantity NUMERIC(18,3),
    new_quantity NUMERIC(18,3),
    transaction_source TEXT,
    reference_id TEXT,
    notes TEXT,
    created_by TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Endpoints

### Health Check

**GET** `/health`
- **Description:** Service health check
- **Response:** `200 OK`

### Database Operations

**GET** `/api/db/probe`
- **Description:** Database connection probe
- **Response:** `200 OK` with connection status

**POST** `/api/db/apply-schema`
- **Description:** Apply database schema
- **Response:** `200 OK` | `500 Internal Server Error`

### Items

**GET** `/api/inventory/items`
- **Description:** List inventory items with filtering
- **Query Parameters:**
  - `vendorId` (UUID, optional)
  - `categoryId` (UUID, optional)
  - `isMenuAvailable` (bool, optional)
  - `search` (string, optional) - Search by name/SKU
- **Response:** `200 OK` with `ItemDto[]`

**GET** `/api/inventory/items/&#123;id&#125;`
- **Description:** Get item by ID
- **Response:** `200 OK` with `ItemDto` | `404 Not Found`

**POST** `/api/inventory/items`
- **Description:** Create new inventory item
- **Request Body:** `CreateItemDto`
- **Response:** `201 Created` with `ItemDto`

**PUT** `/api/inventory/items/&#123;id&#125;`
- **Description:** Update inventory item
- **Request Body:** `UpdateItemDto`
- **Response:** `200 OK` | `404 Not Found`

**DELETE** `/api/inventory/items/&#123;id&#125;`
- **Description:** Delete inventory item (soft delete)
- **Response:** `204 No Content` | `404 Not Found`

### Stock Management

**POST** `/api/inventory/items/&#123;id&#125;/adjust`
- **Description:** Adjust stock for specific item
- **Request Body:**
```json
{
  "delta": -2.0,
  "reason": "Sale",
  "userId": "user-id"
}
```
- **Response:** `200 OK` with updated stock

**POST** `/api/inventory/items/adjust-by-sku`
- **Description:** Adjust stock by SKU
- **Request Body:**
```json
{
  "sku": "ITEM001",
  "delta": -1.0,
  "reason": "Order fulfillment",
  "userId": "user-id"
}
```
- **Response:** `200 OK` with updated stock

**POST** `/api/inventory/availability/check`
- **Description:** Check availability for multiple items by ID
- **Request Body:**
```json
[
  { "id": "uuid", "quantity": 2.0 },
  { "id": "uuid", "quantity": 1.0 }
]
```
- **Response:** `200 OK` with availability status

**POST** `/api/inventory/availability/check-by-sku`
- **Description:** Check availability by SKU
- **Request Body:**
```json
[
  { "sku": "ITEM001", "quantity": 2.0 },
  { "sku": "ITEM002", "quantity": 1.0 }
]
```
- **Response:** `200 OK` with availability status

### Reports

**GET** `/api/reports/low-stock?threshold={number}`
- **Description:** Get items below reorder threshold
- **Query Parameters:**
  - `threshold` (decimal, optional) - Override default threshold
- **Response:** `200 OK` with `LowStockItemDto[]`

**GET** `/api/reports/margins?from={date}&to={date}`
- **Description:** Get margin analysis report
- **Query Parameters:**
  - `from` (date, required) - Start date (YYYY-MM-DD)
  - `to` (date, required) - End date (YYYY-MM-DD)
- **Response:** `200 OK` with `MarginReportDto`

### Vendors

**GET** `/api/inventory/vendors`
- **Description:** List all vendors
- **Response:** `200 OK` with `VendorDto[]`

**GET** `/api/inventory/vendors/&#123;id&#125;`
- **Description:** Get vendor by ID
- **Response:** `200 OK` with `VendorDto` | `404 Not Found`

**POST** `/api/inventory/vendors`
- **Description:** Create new vendor
- **Request Body:** `CreateVendorDto`
- **Response:** `201 Created` with `VendorDto`

**PUT** `/api/inventory/vendors/&#123;id&#125;`
- **Description:** Update vendor
- **Request Body:** `UpdateVendorDto`
- **Response:** `200 OK` | `404 Not Found`

**DELETE** `/api/inventory/vendors/&#123;id&#125;`
- **Description:** Delete vendor
- **Response:** `204 No Content` | `404 Not Found`

## Stock Management Behavior

### Row-Level Locking

All stock adjustments use `FOR UPDATE` locking:
```sql
SELECT quantity_on_hand 
FROM inventory.inventory_stock 
WHERE item_id = @id 
FOR UPDATE;
```

This prevents:
- Negative stock
- Race conditions
- Concurrent modification issues

### Transaction Recording

All stock movements are recorded in `inventory_transactions`:
- **Source tracking:** Identifies where transaction came from
- **Audit trail:** Complete history of all stock changes
- **Reference tracking:** Links to orders, purchases, etc.

## Integration

### MenuApi Integration

- MenuApi stores `inventory_item_id` (nullable FK)
- MenuApi can look up items by SKU
- Menu items can reference inventory items

### OrderApi Integration

- OrderApi checks availability before order creation
- OrderApi deducts stock after order confirmation
- Uses HTTP calls with Polly retries for resilience

## Deployment

**Cloud Run Deployment:**
```powershell
.\solution\backend\deploy-inventory-cloudrun.ps1 `
    -ProjectId "bola8pos" `
    -Region "northamerica-south1" `
    -ServiceName "magidesk-inventory" `
    -CloudSqlInstance "bola8pos:northamerica-south1:pos-app-1"
```

**Service URL:** `https://magidesk-inventory-904541739138.northamerica-south1.run.app`

## Related Documentation

- [Vendor Management](../features/vendor-management.md)
- [Order Processing](../backend/order-api.md)
- [Menu Management](../backend/menu-api.md)
