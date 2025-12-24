# SCHEMA ALIGNMENT DECISIONS

**Phase**: B
**Date**: 2025-12-23

## Principles
1. **Database is Truth**: Code must adapt to DB.
2. **No Phantom Columns**: Code must not reference columns that don't exist.
3. **Explicit Mappings**: Dapper queries must use explicit aliases/mappings matching DTOs.

## Decisions

### 1. Menu Item Price
- **Decision**: The authoritative price column for `menu_items` is `base_price`.
- **Constraint**: Code MUST use `base_price`.
- **Ban**: Usage of `price` or `selling_price` aliases for `menu_items` is forbidden.
- **DTO**: `MenuItemDto` property `BasePrice` references `base_price`.

### 2. Combo Price
- **Decision**: The authoritative price column for `combos` is `price`.
- **Constraint**: Code MUST use `price`.
- **DTO**: `ComboDto` property `Price` references `price`.
- **Note**: Inconsistency with `menu_items.base_price` is accepted to avoid schema migration risks during this phase.

### 3. ID Types
- **Menu Items**: `UUID` (C# `Guid`).
- **Modifiers**: `BigInt` (C# `long`).
- **Combos**: `BigInt` (C# `long`).
- **Categories**: `UUID` (C# `Guid`).

### 4. Versioning
- **Mechanism**: `version` (int) + `is_current_version` (bool).
- **Currentness**: Only rows with `is_current_version = true` are active.
- **Rollback**: Must handle JSON snapshots carefully. Snapshot keys must match current DTO property names (`BasePrice` not `SellingPrice`).
