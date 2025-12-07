# MenuApi

The MenuApi handles menu items, modifiers, combos, and menu analytics.

## Overview

**Location:** `solution/backend/MenuApi/`  
**Port:** 5003 (local)  
**Database Schema:** `menu`  
**Framework:** ASP.NET Core 8

## Key Features

- Menu item management
- Modifiers and modifier options
- Combo offers
- Menu versioning
- Menu analytics
- Bulk operations

## Controllers

### MenuItemsController

**Base Route:** `/api/menu/items`

#### Endpoints

**GET** `/api/menu/items`
- **Description:** List menu items with pagination and filtering
- **Query Parameters:**
  - `SearchTerm` (string, optional)
  - `Category` (string, optional)
  - `IsAvailable` (bool?, optional)
  - `Page` (int, default: 1)
  - `PageSize` (int, default: 20)

**GET** `/api/menu/items/&#123;id&#125;`
- **Description:** Get menu item by ID

**GET** `/api/menu/items/sku/&#123;sku&#125;`
- **Description:** Get menu item by SKU

**GET** `/api/menu/items/check-duplicate-sku/&#123;sku&#125;`
- **Description:** Check if SKU already exists
- **Query Parameters:**
  - `excludeId` (long?, optional) - Exclude this ID from check

**PUT** `/api/menu/items/&#123;id&#125;/availability`
- **Description:** Set item availability
- **Request Body:** `AvailabilityUpdateDto`

**POST** `/api/menu/items/&#123;id&#125;/rollback`
- **Description:** Rollback item to a previous version
- **Query Parameters:**
  - `toVersion` (int) - Version number to rollback to

**GET** `/api/menu/items/&#123;id&#125;/picture`
- **Description:** Get item picture (redirects to URL)

**POST** `/api/menu/items`
- **Description:** Create a new menu item
- **Request Body:** `CreateMenuItemDto`

**PUT** `/api/menu/items/&#123;id&#125;`
- **Description:** Update a menu item
- **Request Body:** `UpdateMenuItemDto`

**DELETE** `/api/menu/items/&#123;id&#125;`
- **Description:** Delete a menu item

### ModifiersController

**Base Route:** `/api/menu/modifiers`

Manages modifiers and modifier options.

### CombosController

**Base Route:** `/api/menu/combos`

Manages combo offers.

### MenuAnalyticsController

**Base Route:** `/api/menu/analytics`

Provides menu analytics and insights.

### MenuVersioningController

**Base Route:** `/api/menu/versioning`

Handles menu versioning operations.

### MenuBulkOperationController

**Base Route:** `/api/menu/bulk`

Handles bulk operations on menu items.

## Services

### MenuService

**Interface:** `IMenuService`

**Key Methods:**
- `ListItemsAsync(MenuItemQueryDto, CancellationToken)` - List items
- `GetItemAsync(long, CancellationToken)` - Get item by ID
- `GetItemBySkuAsync(string, CancellationToken)` - Get item by SKU
- `CreateItemAsync(CreateMenuItemDto, CancellationToken)` - Create item
- `UpdateItemAsync(long, UpdateMenuItemDto, CancellationToken)` - Update item
- `DeleteItemAsync(long, CancellationToken)` - Delete item
- `SetItemAvailabilityAsync(long, bool, string, CancellationToken)` - Set availability
- `RollbackItemAsync(long, int, string, CancellationToken)` - Rollback version

## Database Schema

### menu.menu_items

```sql
CREATE TABLE menu.menu_items (
    item_id BIGSERIAL PRIMARY KEY,
    sku TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    description TEXT,
    price NUMERIC(10,2) NOT NULL,
    category TEXT,
    is_available BOOLEAN NOT NULL DEFAULT TRUE,
    picture_url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

## Related Documentation

- [Menu Management Feature](../../features/menu.md)
- [API Reference - Menu](../../api/v1/menu.md)
