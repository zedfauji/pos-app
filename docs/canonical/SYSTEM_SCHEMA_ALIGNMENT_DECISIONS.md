# SYSTEM SCHEMA ALIGNMENT DECISIONS

**Phase**: B
**Date**: 2025-12-23

## 1. Order Schema Consolidation
- **Decision**: The `orders` schema (containing `orders`, `order_items`) is the **Source of Truth** for OrderApi.
- **Legacy**: The `ord` schema is deprecated and should not be used by active code.
- **Migration**: We will add `delivery_status`, `profit_total`, `shift_id` to `orders.orders`.

## 2. Payment Column Naming
- **Decision**: `PaymentRepository` must align with DB columns `method` and `external_reference`.
- **Constraint**: No renaming of DB columns (Immutability preference). Code MUST change.

## 3. Financial Precision
- **Decision**: All monetary values use `numeric` type in Postgres and `decimal` in C#.
- **Constraint**: `orders` schema tables (`subtotal`, `total`, `tax`, `discount`) are the authoritative financial record.

## 4. ID Standardization
- **New Tables**: Use `UUID` (`gen_random_uuid()`).
- **Legacy Tables**: `Users` (varchar) and `Modifiers`/`Combos` (bigint) are retained to avoid data migration risk.
- **Foreign Keys**: Must match the type of the referenced PK.

## 5. Inventory Truth
- **Decision**: The Schema defined in `complete_inventory_dump.sql` is the **Source of Truth**, replacing the corrupted live schema.
