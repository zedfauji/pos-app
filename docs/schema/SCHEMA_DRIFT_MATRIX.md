# SCHEMA DRIFT MATRIX

**Domain**: MenuApi
**Date**: 2025-12-23

| Entity | Location | Concept | Expected (Code/Assumption) | Actual (Database) | Impact | Correction Required |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **MenuItem** | `MenuRepository.ComputeComboPriceAsync` | Price Column | `price` OR `selling_price` | `base_price` | **Runtime Error** | Change SQL to use `base_price`. |
| **MenuItem** | `MenuRepository.RollbackItemAsync` | Restore Logic | JSON keys `Price`, `SellingPrice` | `base_price` | **Data Loss / Error** | Update JSON parsing to look for `BasePrice`. |
| **MenuItem** | `MenuRepository.UpdateItemAsync` | Price Parameter | `@price` mapping to `base_price` | `base_price` | Minor naming | Ensure consistency. |
| **Combos** | `MenuRepository` | ID Type | `long` | `bigint` | None | Aligned. |
| **Combos** | `MenuRepository` | Price Column | `price` | `price` | None | Aligned. |
| **Modifiers** | `MenuRepository` | ID Type | `long` | `bigint` | None | Aligned. |
| **MenuItem** | `DB` | Category | Foreign Key ID | `varchar` (String) | Weak Referential Integrity | Accept as legacy for now. |

## Notes
- `menu_items` table **ONLY** has `base_price`.
- Code historically seemingly supported `selling_price` or `price`. These are dead concepts.
- `combos` table has `price`. This is inconsistent naming (`base_price` vs `price`) but valid schema.
