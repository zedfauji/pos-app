# DB SCHEMA TRUTH

**Version**: 2.0 (System-Wide)
**Source**: PostgreSQL Database Inspection (Verified via Live DB & Dumps)
**Date**: 2025-12-23

## Schema: menu

### Table: menu_items
| Column             | Type        | Nullable | Default | Notes |
| ------------------ | ----------- | -------- | ------- | ----- |
| menu_item_id       | uuid        | NO       | -       | PK (Part 1). |
| version            | integer     | NO       | -       | PK (Part 2). |
| name               | varchar     | NO       | -       | |
| description        | text        | YES      | -       | |
| base_price         | numeric     | NO       | -       | **Canonical Price**. |
| category           | varchar     | NO       | -       | |
| sku                | varchar     | YES      | -       | Linked to Inventory. |
| is_available       | boolean     | NO       | true    | |
| is_combo           | boolean     | NO       | false   | |
| is_current_version | boolean     | NO       | true    | Soft-delete/Active flag. |

### Table: combos
| Column   | Type    | Nullable | Default   | Notes |
| -------- | ------- | -------- | --------- | ----- |
| combo_id | bigint  | NO       | nextval   | **ID: BigInt**. |
| price    | numeric | NO       | -         | **Canonical Price**. |

---

## Schema: orders

### Table: orders
| Column      | Type    | Nullable | Default | Notes |
| ----------- | ------- | -------- | ------- | ----- |
| order_id    | uuid    | NO       | random  | PK. |
| billing_id  | uuid    | NO       | -       | Links to `billing.bills`. |
| session_id  | uuid    | NO       | -       | Links to `tables.sessions`. |
| subtotal    | numeric | NO       | 0       | |
| total       | numeric | NO       | 0       | |
| status      | enum    | NO       | open    | Custom type `orders.order_status`. |
| created_at  | timestamptz | NO   | now     | |

### Table: order_items
| Column        | Type    | Nullable | Default | Notes |
| ------------- | ------- | -------- | ------- | ----- |
| order_item_id | uuid    | NO       | random  | PK. |
| order_id      | uuid    | NO       | -       | FK to `orders`. |
| menu_item_id  | uuid    | NO       | -       | |
| base_price    | numeric | NO       | -       | Snapshot of item price. |
| price_delta   | numeric | NO       | 0       | Modifier costs. |
| line_total    | numeric | NO       | -       | |
| quantity      | integer | NO       | -       | |

---

## Schema: pay

### Table: payments
| Column         | Type    | Nullable | Default | Notes |
| -------------- | ------- | -------- | ------- | ----- |
| payment_id     | uuid    | NO       | random  | PK. |
| bill_id        | uuid    | NO       | -       | FK to `billing.bills`. |
| amount_paid    | numeric | NO       | 0       | |
| tip_amount     | numeric | NO       | 0       | |
| method         | enum    | NO       | -       | Custom type? or varchar. DB says `USER-DEFINED`. |
| status         | enum    | NO       | Paid    | Custom type `pay.payment_status`. |
| is_settled     | boolean | NO       | false   | |

---

## Schema: billing

### Table: bills
| Column         | Type    | Nullable | Default | Notes |
| -------------- | ------- | -------- | ------- | ----- |
| bill_id        | uuid    | NO       | random  | PK. |
| session_id     | uuid    | NO       | -       | FK to `tables.sessions`. |
| items_total    | numeric | NO       | 0       | |
| total_amount   | numeric | NO       | 0       | Final amount to pay. |
| status         | enum    | NO       | AwaitingPayment | Custom type `billing.bill_status`. |

---

## Schema: tables

### Table: sessions
| Column           | Type    | Nullable | Default | Notes |
| ---------------- | ------- | -------- | ------- | ----- |
| session_id       | uuid    | NO       | -       | PK. |
| table_id         | uuid    | NO       | -       | |
| is_active        | boolean | NO       | true    | |
| accumulated_cost | numeric | NO       | 0       | |
| guest_count      | integer | NO       | 1       | |

---

## Schema: inventory

### Table: inventory_items
| Column            | Type    | Nullable | Default | Notes |
| ----------------- | ------- | -------- | ------- | ----- |
| item_id           | uuid    | NO       | random  | PK. |
| sku               | text    | NO       | -       | UNIQUE. |
| name              | text    | NO       | -       | |
| quantity_on_hand  | numeric | -        | -       | Stored in `inventory_stock` (joined view). |
| buying_price      | numeric | YES      | -       | |
| selling_price     | numeric | YES      | -       | |
| is_menu_available | boolean | NO       | false   | |

### Table: inventory_stock
| Column           | Type    | Nullable | Default | Notes |
| ---------------- | ------- | -------- | ------- | ----- |
| item_id          | uuid    | NO       | -       | PK, FK to `inventory_items`. |
| quantity_on_hand | numeric | NO       | 0       | |

---

## Schema: users

### Table: users
| Column  | Type    | Nullable | Default | Notes |
| ------- | ------- | -------- | ------- | ----- |
| user_id | varchar | NO       | -       | PK. **Type mismatch: Code likely expects UUID.** |
| username| varchar | NO       | -       | UNIQUE. |
| role    | varchar | NO       | Server  | |

---

## Schema: settings

### Table: hierarchical_settings
| Column        | Type    | Nullable | Default | Notes |
| ------------- | ------- | -------- | ------- | ----- |
| id            | bigint  | NO       | serial  | PK. |
| host_key      | varchar | NO       | default | |
| category      | varchar | NO       | -       | |
| settings_json | jsonb   | NO       | -       | |
