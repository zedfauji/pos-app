# Database Migration Strategy: Table Types

## 1. Schema Changes

### New Table: `table_types`
This table defines the templates for table behavior.

```sql
CREATE TABLE table_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE, -- 'Billiard', 'Bar', 'Patio'
    has_timer BOOLEAN DEFAULT FALSE,
    hourly_rate DECIMAL(10,2) DEFAULT 0.00,
    allow_orders BOOLEAN DEFAULT TRUE,
    requires_server BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### Table Modification: `tables`
We need to link existing tables to these types.

```sql
ALTER TABLE tables 
ADD COLUMN type_id INT REFERENCES table_types(id);
-- Note: We will eventually drop the string 'type' column, but keep it for rollback safety initially.
```

## 2. Migration Plan (Safe Mode)

We will not drop the old column immediately. The migration will:
1.  Create `table_types`.
2.  Insert Default Types based on current logic:
    *   **Billiard**: `has_timer=true`, `hourly_rate` (Default logic?), `allow_orders=true`
    *   **Bar**: `has_timer=false`, `allow_orders=true`
    *   **To Go**: `has_timer=false`, `allow_orders=true`
3.  Update `tables` rows:
    *   `UPDATE tables SET type_id = (SELECT id FROM table_types WHERE name = 'Billiard') WHERE type = 'billiard';`
    *   `UPDATE tables SET type_id = (SELECT id FROM table_types WHERE name = 'Bar') WHERE type = 'bar';`
    *   Handle any unknowns (Default to 'Bar' safe mode).

## 3. Rollback Strategy
Since we are keeping the `type` column initially:
*   Down-migration involves doing nothing (frontend ignores `type_id`).
*   If we need to revert fully, we drop the `type_id` column FK.

## 4. Verification Queries
*   Select all tables where `type_id` is NULL (Should be 0).
*   Select count of tables by joined Type Name vs. old string Type (Should match).
