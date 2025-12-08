# Vendor Management

Complete documentation for the vendor management system in MagiDesk POS.

## Overview

The vendor management system provides comprehensive vendor CRUD operations, vendor-item relationships, budget tracking, and integration with inventory and order management.

## Current State

### Status: Functional but Incomplete

The vendor management module provides basic CRUD operations but has critical data persistence issues and lacks enterprise features.

### Key Findings

#### 🔴 Critical Issues (Must Fix Immediately)

1. **Data Loss Bug:** Repository doesn't persist `budget`, `reminder`, `reminder_enabled` fields
2. **Missing Enterprise Features:** No analytics, document management, communication log, or rating system

#### 🟡 Important Issues (Should Fix Soon)

1. Database schema limitations (single contact field, no addresses)
2. Duplicate/unused code (two dialog implementations)
3. No pagination, sorting, or advanced filtering
4. Limited error handling and validation
5. Poor integration with vendor orders and items

## Architecture

### Backend

- **API:** `InventoryApi/Controllers/VendorsController.cs`
- **Repository:** `InventoryApi/Repositories/VendorRepository.cs`
- **Database:** PostgreSQL `inventory.vendors` table
- **Issues:** Missing field persistence, limited validation

### Frontend

- **Page:** `Views/VendorsManagementPage.xaml`
- **ViewModel:** `ViewModels/VendorsManagementViewModel.cs`
- **Dialog:** `Dialogs/VendorCrudDialog.xaml`
- **Service:** `Services/VendorService.cs`
- **Issues:** Duplicate dialogs, limited features, no pagination

### Data Models

- **DTO:** `Shared/DTOs/VendorDto.cs` (basic)
- **Extended DTO:** `Shared/DTOs/ExtendedVendorDto.cs` (unused)
- **Display Model:** `VendorDisplay` in ViewModel
- **Issues:** DTO mismatch with repository, unused extended DTO

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

## API Endpoints

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
```json
{
  "name": "Vendor Name",
  "contactInfo": "contact@vendor.com",
  "status": "active",
  "budget": 10000.00,
  "reminder": "Monthly order due",
  "reminderEnabled": true,
  "notes": "Preferred vendor for beverages"
}
```
- **Response:** `201 Created` with `VendorDto`

**PUT** `/api/inventory/vendors/&#123;id&#125;`
- **Description:** Update vendor
- **Request Body:** `UpdateVendorDto`
- **Response:** `200 OK` | `404 Not Found`

**DELETE** `/api/inventory/vendors/&#123;id&#125;`
- **Description:** Delete vendor (soft delete)
- **Response:** `204 No Content` | `404 Not Found`

### Vendor Items

**GET** `/api/inventory/vendors/&#123;vendorId&#125;/items`
- **Description:** Get items for vendor
- **Response:** `200 OK` with `ItemDto[]`

**POST** `/api/inventory/vendors/&#123;vendorId&#125;/items`
- **Description:** Create item for vendor
- **Request Body:** `CreateItemDto`
- **Response:** `201 Created` with `ItemDto`

## Enhancement Roadmap

### Phase 1: Critical Fixes (Week 1) ⚡

**Priority:** 🔴 **HIGHEST**

**Tasks:**
1. Fix repository to persist all fields (`budget`, `reminder`, `reminder_enabled`)
2. Add `notes` column to SELECT queries
3. Remove or consolidate duplicate dialogs
4. Add basic validation

**Impact:** Prevents data loss, fixes core functionality  
**Risk:** Low - Fixing bugs, not changing architecture  
**Effort:** 1 week

### Phase 2: Core Enhancements (Week 2-3) 📈

**Priority:** 🟡 **HIGH**

**Tasks:**
1. Add pagination and sorting
2. Improve UI/UX (tabs, cards, filters)
3. Add comprehensive validation
4. Integrate vendor orders/items
5. Enhance DTOs

**Impact:** Better user experience, scalability  
**Risk:** Low-Medium - Additive changes  
**Effort:** 2 weeks

### Phase 3: Database & Backend (Week 4-5) 🗄️

**Priority:** 🟡 **MEDIUM**

**Tasks:**
1. Add new database tables (contacts, addresses, documents, etc.)
2. Add new API endpoints (analytics, documents, communications)
3. Implement analytics calculations
4. Implement document storage
5. Implement communication log

**Impact:** Enterprise features foundation  
**Risk:** Medium - Database changes require migration  
**Effort:** 2 weeks

### Phase 4: Enterprise Features (Week 6-7) 🏢

**Priority:** 🟡 **MEDIUM**

**Tasks:**
1. Analytics dashboard UI
2. Document management UI
3. Communication log UI
4. Vendor rating system UI
5. Performance metrics display

**Impact:** Complete enterprise solution  
**Risk:** Low - UI work, backend already done  
**Effort:** 2 weeks

## Proposed UI Enhancements

### Tabbed Vendor Details

- **Overview Tab:** Basic info, KPIs, quick stats
- **Items Tab:** Inventory items from vendor
- **Orders Tab:** Purchase order history
- **Analytics Tab:** Charts and metrics
- **Documents Tab:** Document management
- **Communications Tab:** Notes, tasks, interaction log
- **Settings Tab:** Vendor configuration

### Enhanced Vendor Cards

- Reliability index gauge
- Total spend display
- Order count badges
- Rating stars
- Quick action buttons

### Advanced Filters

- Status, Category, Tags
- Rating range
- Spend range
- Payment terms
- Date ranges

## Integration

### With InventoryApi

- Vendors linked to inventory items
- Vendor items management
- Stock tracking per vendor

### With OrderApi

- Vendor orders tracking
- Purchase order management
- Vendor performance metrics

## Related Documentation

- [InventoryApi Reference](../backend/inventory-api.md)
- [Vendor Management Summary](../../../archive/status-documents/VENDOR_MANAGEMENT_SUMMARY.md)
