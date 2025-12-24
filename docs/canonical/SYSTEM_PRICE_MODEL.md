# SYSTEM PRICE MODEL

**Scope**: All Backend APIs

## 1. Data Types
- **Database**: `numeric` (or `numeric(18,2)` / `numeric(18,4)`). NEVER `float` or `double`.
- **Code**: `decimal`.

## 2. Nullability
- **Prices**: MUST be `NOT NULL`.
- **Totals**: MUST be `NOT NULL` (default 0).
- **Discounts/Tax**: MUST be `NOT NULL` (default 0).

## 3. Calculation Authority
- **Source**: `orders.orders` and `orders.order_items` stores the **Final Snapshot** of prices at the time of transaction.
- **Reference**: `menu.menu_items.base_price` is for *display*. Order prices are frozen upon creation.
