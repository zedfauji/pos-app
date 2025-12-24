# SYSTEM WIDE SCHEMA DRIFT MATRIX

**Phase**: B
**Date**: 2025-12-23

| API | Location | Concept | Code Expectation | Database Reality | Impact | Action |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **OrderApi** | `OrderRepository` | **Target Schema** | `ord` | `orders` & `ord` both exist | **High - Split Brain** | Migrate code to `orders` schema. |
| **OrderApi** | `OrderRepository` | Table | `ord.orders` | `orders.orders` | **High** | Redirect to `orders.orders`. |
| **OrderApi** | `OrderDto` | Delivery Status | `DeliveryStatus` | Missing in `orders.orders` | Runtime Error | Add column to `orders.orders`. |
| **OrderApi** | `OrderDto` | Profit | `ProfitTotal` | Missing in `orders.orders` | Data Loss | Add column to `orders.orders`. |
| **OrderApi** | `OrderDto` | Shift | `ShiftId` | Missing in `orders.orders` | Logic Fail | Add column to `orders.orders`. |
| **OrderApi** | `OrderDto` | Discount | `DiscountTotal` | `discount` | Mapping Error | Update Code mapping. |
| **OrderApi** | `OrderDto` | Tax | `TaxTotal` | `tax` | Mapping Error | Update Code mapping. |
| **PaymentApi** | `PaymentRepository` | Payment Method | `payment_method` | `method` | **Runtime Error** | Update Code to `method`. |
| **PaymentApi** | `PaymentRepository` | External Ref | `external_ref` | `external_reference` | **Runtime Error** | Update Code to `external_reference`. |
| **Inventory** | `InventoryRepository` | Table Name | `inventory_items` | `items` (Live DB) | **Critical** | Fixed via Restore. Code uses proper schema now. |
| **Settings** | `SettingsApi` | Table | `hierarchical_settings` | Missing | **Critical** | Fixed via Create. |
| **Users** | `UsersRepository` | ID Type | `Guid` (implied) | `varchar` | Low | Accept Drift (Legacy). |

## Notes
- `ord` schema appears to be legacy. `orders` schema contains the robust financial columns.
- `inventory` schema was corrupted/missing in Live DB; restored from dump.
