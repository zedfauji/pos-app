# Phase 5: Code Update Issues

**Date**: 2025-12-23  
**Status**: IN PROGRESS  
**Issue**: DTO Type Mismatch for ComboId

## Known Issue: ComboId Type Mismatch

### Problem
- **Database Schema**: `orders.order_items.combo_id` is `bigint` (matches `menu.combos.combo_id`)
- **DTOs**: `OrderItemDto.ComboId` and `CreateOrderItemDto.ComboId` use `Guid?`
- **Migration Decision**: Intentional - Changed from `uuid` to `bigint` to match `menu.combos.combo_id` (see `02_fix_order_items_combo_id.sql`)

### Current Workaround (INCORRECT)
The current code attempts to convert `Guid?` to `long?` using `GetHashCode()`, which is **WRONG** and will cause data corruption:
```csharp
long? comboIdLong = it.ComboId.HasValue ? (long?)it.ComboId.Value.GetHashCode() : null;
```

**Why this is wrong:**
- `GetHashCode()` does not preserve the actual combo_id value
- Hash collisions are possible
- This will create orphaned references

### Required Fix

**Option 1: Update DTOs (Recommended)**
Change DTOs to use `long?` instead of `Guid?`:
```csharp
// OrderApi/Models/OrderDtos.cs
public sealed record OrderItemDto(Guid Id, Guid? MenuItemId, long? ComboId, ...);
public sealed record CreateOrderItemDto(Guid? MenuItemId, long? ComboId, ...);
```

**Impact:**
- Breaking change for API consumers
- Requires updating all code that uses these DTOs
- Service layer methods that accept `Guid comboId` need to change to `long comboId`

**Option 2: Keep Conversion Layer (Not Recommended)**
If DTOs must remain `Guid?`, we need a proper mapping table or conversion logic, but this adds complexity and potential for errors.

### Affected Files
1. `solution/backend/OrderApi/Models/OrderDtos.cs` - DTO definitions
2. `solution/backend/OrderApi/Repositories/OrderRepository.cs` - Repository methods
3. `solution/backend/OrderApi/Services/OrderService.cs` - Service layer
4. `solution/backend/OrderApi/Repositories/IOrderRepository.cs` - Interface definitions

### Next Steps
1. **Decision Required**: Choose Option 1 (update DTOs) or Option 2 (keep conversion)
2. **If Option 1**: Update all DTOs, interfaces, and service methods
3. **If Option 2**: Implement proper conversion logic (mapping table or conversion service)

---

## Other Schema Reference Updates

### Completed
- ✅ `TableRepository.cs` - Updated `ord.*` → `orders.*`
- ✅ `ShiftRepository.cs` - Updated `public.bills` → `billing.bills`, `public.payments` → `pay.payments`
- ✅ `BillingRepository.cs` - Updated `public.bills` → `billing.bills`
- ✅ `OrderIntegrationService.cs` - Updated `ord.*` → `orders.*`

### Pending
- ⚠️ `OrderRepository.cs` - ComboId type conversion issue (see above)
- ⚠️ `OrderService.cs` - May need updates if DTOs change

---

**Status**: Blocked on ComboId type decision

