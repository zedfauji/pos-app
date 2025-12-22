# 03 - Backend Contract Verification

## Overview

This document verifies all required backend APIs exist and identifies any gaps.

---

## Required APIs

### 1. Table Status Endpoint ✅ EXISTS

| Property | Value |
|----------|-------|
| Endpoint | `GET /tables` |
| Interface | `ITableApi.GetTablesAsync()` |
| Returns | `List<TableStatusDto>` |

**Verification**: Found in [ITableApi.cs](file:///c:/Users/giris/Documents/Code/Order-Tracking-By-GPT/solution/MagiDesk.Client/Services/ITableApi.cs#L13-14)

**DTO Fields** (`TableStatusDto`):
- `Label` - Table name ✅
- `Type` - billiard/bar ✅
- `Occupied` - Status flag ✅
- `CurrentSessionId` - Session identifier ✅
- `StartTime` - Session start timestamp ✅
- `Server` - Staff name ✅

---

### 2. Session Info Endpoint  ✅ EXISTS (via Table Status)

Session information is embedded in `TableStatusDto`. No separate endpoint needed.

---

### 3. Ordered Items Endpoint ✅ EXISTS

| Property | Value |
|----------|-------|
| Endpoint | `GET /tables/{label}/items` |
| Interface | `ITableApi.GetItemsAsync(string label)` |
| Returns | `List<ItemLine>` |

**Verification**: Found in [ITableApi.cs](file:///c:/Users/giris/Documents/Code/Order-Tracking-By-GPT/solution/MagiDesk.Client/Services/ITableApi.cs#L29-30)

**DTO Fields** (`ItemLine`):
- `itemId` - Item identifier ✅
- `name` - Item name ✅
- `quantity` - Quantity ordered ✅
- `price` - Unit price ✅

---

### 4. Submit Order Endpoint ✅ EXISTS

| Property | Value |
|----------|-------|
| Endpoint | `POST /tables/{label}/order` |
| Interface | `ITableApi.PostOrderAsync(string label, OrderRequest request)` |
| Request Body | `OrderRequest { Items: List<OrderItemDto> }` |
| Returns | `IApiResponse<object>` |

**Verification**: Found in [ITableApi.cs](file:///c:/Users/giris/Documents/Code/Order-Tracking-By-GPT/solution/MagiDesk.Client/Services/ITableApi.cs#L32-33)

---

### 5. Menu Items Endpoint ✅ EXISTS

| Property | Value |
|----------|-------|
| Endpoint | `GET /api/menu/items` |
| Interface | `IMenuApi.ListItemsAsync(MenuItemQueryDto query)` |
| Returns | `PagedResult<MenuItemDto>` |

**Verification**: Found in [IMenuApi.cs](file:///c:/Users/giris/Documents/Code/Order-Tracking-By-GPT/solution/MagiDesk.Client/Services/IMenuApi.cs#L12-13)

---

## Identified Gaps

### Gap 1: Menu Categories ⚠️ NEEDS VERIFICATION

**Current State**: `MenuItemQueryDto` accepts category filter, but we need:
- List of available categories for tab filtering
- OR categories embedded in menu item response

**Options**:
1. Extract distinct categories from `MenuItemDto.Category` client-side (simple)
2. Add `GET /api/menu/categories` endpoint (proper)

**Recommendation**: Option 1 is acceptable since backend remains authoritative for items.

---

### Gap 2: Order Batch/Time Grouping ⚠️ ENHANCEMENT

**Current State**: `GetItemsAsync` returns flat `List<ItemLine>` without batch info.

**Desired**: Items grouped by send batch with timestamps.

**Options**:
1. Add `sentAt` timestamp to `ItemLine` - allows UI grouping
2. Add new `GET /tables/{label}/order-batches` endpoint

**Recommendation**: Option 1 is minimal change. Backend should add `sentAt` field.

---

### Gap 3: Elapsed Time Calculation ⚠️ CLARIFICATION NEEDED

**Question**: Should elapsed time be:
- A) Calculated in UI from `StartTime` (display-only timer)
- B) Provided by backend as `ElapsedSeconds` field

**Current Design**: Backend provides `StartTime`. UI displays elapsed via timer tick.

**Recommendation**: Option A is acceptable since it's pure display arithmetic.

---

## Contract Summary

| Feature | Endpoint | Status |
|---------|----------|--------|
| Table Status | `GET /tables` | ✅ Ready |
| Ordered Items | `GET /tables/{label}/items` | ✅ Ready (needs `sentAt` for grouping) |
| Submit Order | `POST /tables/{label}/order` | ✅ Ready |
| Menu Items | `GET /api/menu/items` | ✅ Ready |
| Menu Categories | N/A | ⚠️ Extract from items |
| Order Batches | N/A | ⚠️ Enhancement suggested |

---

## Backend Changes Required

### Priority 1: None (Can proceed now)
All essential endpoints exist.

### Priority 2: Enhancement (Optional)
1. Add `sentAt: DateTime` to `ItemLine` for proper batch grouping
2. Add `category: string` to `MenuItemDto` if not present

> [!NOTE]
> No backend changes are strictly required. The workspace can be built with current contracts.
