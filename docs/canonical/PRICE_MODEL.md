# PRICE MODEL

**System**: POS Backend
**Currency**: System Base Currency (Implicit)

## Rules

1. **Storage Type**: All monetary values MUST be stored as `numeric` (PostgreSQL) / `decimal` (C#).
2. **Precision**: Database handles precision. Code should not truncate arbitrarily.
3. **Nullability**: Price columns (`base_price`, `price`, `price_delta`) MUST be `NOT NULL`.

## Entity Pricing

### Menu Items
- **Column**: `base_price`
- **Semantics**: The base selling price of the item before modifiers or discounts.

### Modifiers
- **Column**: `price_delta`
- **Semantics**: The specific amount to ADD to the item's price. Can be positive, zero, or negative.

### Combos
- **Column**: `price`
- **Semantics**: The fixed price for the entire combo bundle. Overrides individual item prices.

## Calculation Logic
- **Item Total** = `menu_items.base_price` + SUM(`modifier_options.price_delta`)
- **Combo Total** = `combos.price` (Fixed) OR Computed from items (if dynamic - currently fixed `price` column implies static bundle price).
