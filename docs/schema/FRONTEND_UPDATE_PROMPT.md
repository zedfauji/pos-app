# Frontend Update Prompt: ComboId Type Change

**Date**: 2025-12-23  
**Priority**: HIGH - Breaking Change  
**Scope**: Frontend/Client Code Updates

## Context

The backend database schema migration intentionally changed `combo_id` from `uuid` to `bigint` to match `menu.combos.combo_id`. As part of Phase 5 code updates, all backend DTOs and interfaces have been updated to use `long?` instead of `Guid?` for `ComboId`.

## Breaking Changes

### DTO Type Changes

The following DTOs have been updated in the backend:

1. **`OrderItemDto`** (from `OrderApi/Models/OrderDtos.cs`)
   - **Changed**: `ComboId` property type
   - **From**: `Guid?`
   - **To**: `long?`

2. **`CreateOrderItemDto`** (from `OrderApi/Models/OrderDtos.cs`)
   - **Changed**: `ComboId` property type
   - **From**: `Guid?`
   - **To**: `long?`

## Required Frontend Updates

### 1. Update Shared DTO Definitions

**Location**: `solution/shared/DTOs/OrderItemDto.cs` and related DTO files

**Action**: Update the shared `OrderItemDto` definition to use `long?` for `ComboId` if it exists. The frontend appears to use a simplified `OrderItemDto` from `MagiDesk.Shared.DTOs`, but if there's a `ComboId` property, it must be `long?`:

**Check**: First verify if `OrderItemDto` in `solution/shared/DTOs/` has a `ComboId` property. If it does, update it:

```csharp
// Before
public class OrderItemDto
{
    public string ItemId { get; set; }
    public Guid? ComboId { get; set; }  // ❌ Change this if exists
    // ... other properties
}

// After
public class OrderItemDto
{
    public string ItemId { get; set; }
    public long? ComboId { get; set; }  // ✅ Changed to long?
    // ... other properties
}
```

### 2. Update API Client DTOs (If Using Refit)

**Location**: Refit interface definitions or API client DTOs

**Action**: If the frontend has local DTO definitions that mirror the backend, update them:

```csharp
// Before
public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? ComboId { get; set; }  // ❌ Change this
    // ... other properties
}

// After
public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid? MenuItemId { get; set; }
    public long? ComboId { get; set; }  // ✅ Changed to long?
    // ... other properties
}
```

### 2. Update ViewModels

**Location**: ViewModels that bind to order items or create order items

**Action**: Change any properties or fields that reference `ComboId` from `Guid?` to `long?`:

```csharp
// Before
[ObservableProperty]
private Guid? _selectedComboId;

// After
[ObservableProperty]
private long? _selectedComboId;
```

### 3. Update UI Bindings

**Location**: XAML files that bind to combo-related properties

**Action**: Ensure bindings work with `long?` type. Most bindings should work automatically, but verify:
- ComboBox SelectedValue bindings
- TextBlock/TextBlock displays of combo IDs
- Any converters that handle ComboId

### 4. Update API Client Interfaces

**Location**: Refit interfaces (e.g., `IOrderApi.cs`, `ITableApi.cs`)

**Action**: Verify that API method signatures match the backend. If using Refit, the types should automatically match, but verify:

```csharp
// Should automatically work with Refit, but verify:
Task<OrderItemDto> CreateOrderItemAsync(CreateOrderItemDto item);
// CreateOrderItemDto.ComboId should be long? to match backend
```

### 5. Update Combo Selection Logic

**Location**: Any code that creates or manipulates combo selections

**Action**: Update code that sets or reads `ComboId`:

```csharp
// Before
var orderItem = new CreateOrderItemDto
{
    ComboId = selectedCombo.Id,  // If selectedCombo.Id is Guid
    // ...
};

// After
var orderItem = new CreateOrderItemDto
{
    ComboId = selectedCombo.Id,  // selectedCombo.Id should now be long
    // ...
};
```

### 6. Update Combo Data Models

**Location**: Combo-related models/DTOs (e.g., `ComboDto`, `ComboItemDto`)

**Action**: Verify that combo ID properties are `long` (not `Guid`):

```csharp
// Should be:
public class ComboDto
{
    public long Id { get; set; }  // ✅ long, not Guid
    // ...
}
```

## Files to Search and Update

Search for these patterns across the frontend codebase:

1. **Pattern**: `ComboId.*Guid` or `Guid.*ComboId`
   - Find all references to ComboId with Guid type
   - Update to `long?` or `long`

2. **Pattern**: `OrderItemDto` or `CreateOrderItemDto`
   - Find all usages of these DTOs
   - Verify ComboId property type

3. **Pattern**: Combo selection/creation logic
   - Find where combos are selected and added to orders
   - Verify type compatibility

## Testing Checklist

After making changes, verify:

- [ ] Order creation with combo items works
- [ ] Combo selection in UI displays correctly
- [ ] Order items with combos are saved correctly
- [ ] Order items with combos are retrieved and displayed correctly
- [ ] No type conversion errors in runtime
- [ ] No binding errors in XAML

## Rationale

This change was made to align with the database schema:
- `menu.combos.combo_id` is `bigint` (not `uuid`)
- `orders.order_items.combo_id` was migrated from `uuid` to `bigint` to match
- Backend DTOs now correctly reflect the database schema
- This prevents type mismatch issues between backend and database

## Reference Documents

- Backend changes: `docs/schema/PHASE5_COMPLETION_REPORT.md`
- Migration details: `solution/backend/migrations/schema-fixes/02_fix_order_items_combo_id.sql`
- Schema authority: `docs/schema/CANONICAL_DB_SCHEMA.md`

## Questions?

If you encounter issues:
1. Check the backend DTO definitions in `solution/backend/OrderApi/Models/OrderDtos.cs`
2. Verify the database schema: `menu.combos.combo_id` is `bigint`
3. Check migration script: `02_fix_order_items_combo_id.sql` confirms the intentional change

---

## Current Frontend State

**Good News**: 
- `solution/shared/DTOs/Billing/BillingDtos.cs` already has `ComboId` as `long?` (line 97) ✅
- `solution/shared/DTOs/Orders/OrderItemDto.cs` does NOT have a `ComboId` property (it's a simplified DTO)

**Action Required**:
- If the frontend uses backend DTOs directly via Refit, they should automatically match
- If the frontend has local DTO definitions that mirror backend DTOs, those need updating
- Check combo-related DTOs (ComboDto, etc.) to ensure `Id` is `long`, not `Guid`

## Specific Files to Check

1. **Combo DTOs**: Search for `ComboDto` or combo-related DTOs - verify `Id` is `long`
2. **API Interfaces**: Check Refit interfaces that use `CreateOrderItemDto` or `OrderItemDto` from backend
3. **ViewModels**: Check if any ViewModels store combo IDs as `Guid?` - change to `long?`

---

**Status**: Ready for frontend implementation  
**Estimated Impact**: Medium - Requires type changes but logic should remain the same  
**Note**: The shared `OrderItemDto` doesn't have `ComboId`, so changes may only be needed in combo-specific code

