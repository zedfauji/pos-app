# Database Schema Analysis (v3)

## Current Schema Verification
We recently migrated to split Tables and Types. This schema is sufficient for the requested Admin features.

### `public.table_types`
| Column | Type | Notes |
| :--- | :--- | :--- |
| `id` | `uuid` | PK |
| `name` | `text` | Display Name (Bar, Billiard) |
| `has_timer` | `boolean` | Config rule |
| `hourly_rate` | `numeric` | Config rule ($/hr) |
| `requires_server` | `boolean` | Config rule |
| `allow_orders` | `boolean` | Config rule |

### `public.tables`
| Column | Type | Notes |
| :--- | :--- | :--- |
| `table_id` | `uuid` | PK |
| `type_id` | `uuid` | FK to `table_types` |
| `table_name` | `text` | e.g. "Table 1" |
| `is_active` | `boolean` | Used for Soft Delete |
| `capacity` | `int` | Informational |

## Required Changes
**No Schema Changes Required.**

We will rely heavily on:
1.  **Soft Deletes**: Updating `tables.is_active` to `false` instead of `DELETE FROM`.
2.  **Validation**: Application-side checks against `table_sessions` (active sessions) before modifying sensitive data.
