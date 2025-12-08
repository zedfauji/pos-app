# Database Structure for Tables and Floors

## Overview

The system uses a **multi-floor layout system** where tables belong to floors. There are two table-related tables that need to stay synchronized.

## Database Schema

### 1. Floors Table (`public.floors`)

```sql
CREATE TABLE public.floors (
    floor_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    floor_name text NOT NULL UNIQUE,
    description text NULL,
    is_default boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
```

**Key Points:**
- One floor should have `is_default = true`
- Floors can be active/inactive
- Display order controls sorting

### 2. Tables Table (`public.tables`) - **PRIMARY TABLE**

```sql
CREATE TABLE public.tables (
    table_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    floor_id uuid NOT NULL REFERENCES public.floors(floor_id) ON DELETE CASCADE,
    table_name text NOT NULL,
    table_number text NULL,
    table_type text NOT NULL DEFAULT 'billiard', -- 'billiard' | 'bar'
    x_position numeric(10,2) NOT NULL DEFAULT 0,
    y_position numeric(10,2) NOT NULL DEFAULT 0,
    rotation numeric(5,2) NOT NULL DEFAULT 0,
    size text NOT NULL DEFAULT 'M', -- 'S' | 'M' | 'L'
    width numeric(10,2) NULL,
    height numeric(10,2) NULL,
    status text NOT NULL DEFAULT 'available', -- 'available' | 'occupied'
    billing_rate numeric(10,2) NOT NULL DEFAULT 0,
    auto_start_timer boolean NOT NULL DEFAULT false,
    icon_style text NULL,
    grouping_tags text[] NULL,
    is_active boolean NOT NULL DEFAULT true,
    is_locked boolean NOT NULL DEFAULT false,
    order_id text NULL,
    start_time timestamptz NULL,
    server text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT tables_floor_name_unique UNIQUE (floor_id, table_name)
);
```

**Key Points:**
- **Every table MUST have a `floor_id`** - cannot be NULL
- Table name must be unique within a floor
- `status` field tracks availability ('available' or 'occupied')
- `order_id`, `start_time`, and `server` are synced from sessions

### 3. Table Status Table (`public.table_status`) - **LEGACY/SYNC TABLE**

```sql
CREATE TABLE public.table_status (
    label text PRIMARY KEY,
    type text NOT NULL,
    occupied boolean NOT NULL,
    order_id text NULL,
    start_time timestamptz NULL,
    server text NULL,
    updated_at timestamptz NOT NULL DEFAULT now()
);
```

**Key Points:**
- This is a **legacy table** kept for backward compatibility
- Used by session management (`table_sessions` references `table_label`)
- **MUST be kept in sync** with `public.tables.status`
- `label` should match `table_name` from `public.tables`

### 4. Table Sessions Table (`public.table_sessions`)

```sql
CREATE TABLE public.table_sessions (
    session_id uuid PRIMARY KEY,
    table_label text NOT NULL,  -- References table_status.label
    server_id text NOT NULL,
    server_name text NOT NULL,
    start_time timestamptz NOT NULL,
    end_time timestamptz NULL,
    status text NOT NULL CHECK (status IN ('active','closed')) DEFAULT 'active',
    items jsonb NOT NULL DEFAULT '[]',
    billing_id uuid NULL,
    last_heartbeat timestamptz NULL
);
```

**Key Points:**
- `table_label` references `table_status.label` (legacy)
- When session starts, both `table_status` and `public.tables` should be updated
- Session state checking compares `table_status.occupied` with active sessions

## Data Consistency Requirements

### Critical Rules:

1. **Every table in `public.tables` MUST have a `floor_id`**
   ```sql
   -- Check for tables without floor_id
   SELECT table_id, table_name FROM public.tables WHERE floor_id IS NULL;
   ```

2. **`table_status.label` should match `tables.table_name`**
   ```sql
   -- Check for mismatches
   SELECT t.table_name, ts.label 
   FROM public.tables t
   LEFT JOIN public.table_status ts ON t.table_name = ts.label
   WHERE ts.label IS NULL;
   ```

3. **`table_status.occupied` should match `tables.status`**
   ```sql
   -- Check for status mismatches
   SELECT t.table_name, t.status, ts.occupied
   FROM public.tables t
   JOIN public.table_status ts ON t.table_name = ts.label
   WHERE (t.status = 'occupied' AND ts.occupied = false)
      OR (t.status = 'available' AND ts.occupied = true);
   ```

4. **Active sessions should match table status**
   ```sql
   -- Check for session/status mismatches
   SELECT ts.table_label, ts.status as session_status, tst.occupied
   FROM public.table_sessions ts
   JOIN public.table_status tst ON ts.table_label = tst.label
   WHERE ts.status = 'active'
     AND tst.occupied = false;
   ```

## Migration Status

The system includes automatic migration that:
1. Creates a default floor if none exists
2. Migrates data from `table_status` to `tables` (one-time)
3. Keeps both tables in sync during operations

## Common Issues

### Issue 1: Tables without floor_id
**Symptom**: Tables don't appear in floor layouts
**Fix**: 
```sql
-- Assign all tables without floor_id to default floor
UPDATE public.tables t
SET floor_id = (SELECT floor_id FROM public.floors WHERE is_default = true LIMIT 1)
WHERE t.floor_id IS NULL;
```

### Issue 2: Missing table_status entries
**Symptom**: Session state inconsistencies
**Fix**:
```sql
-- Create table_status entries for all tables
INSERT INTO public.table_status (label, type, occupied, order_id, start_time, server)
SELECT 
    t.table_name as label,
    t.table_type as type,
    (t.status = 'occupied') as occupied,
    t.order_id,
    t.start_time,
    t.server
FROM public.tables t
WHERE NOT EXISTS (
    SELECT 1 FROM public.table_status ts WHERE ts.label = t.table_name
);
```

### Issue 3: Status mismatch between tables and table_status
**Symptom**: Tables show wrong status
**Fix**:
```sql
-- Sync status from tables to table_status
UPDATE public.table_status ts
SET 
    occupied = (t.status = 'occupied'),
    order_id = t.order_id,
    start_time = t.start_time,
    server = t.server,
    updated_at = now()
FROM public.tables t
WHERE ts.label = t.table_name
  AND (
    ts.occupied != (t.status = 'occupied')
    OR ts.order_id IS DISTINCT FROM t.order_id
    OR ts.start_time IS DISTINCT FROM t.start_time
    OR ts.server IS DISTINCT FROM t.server
  );
```

## Recommended Database State

For a healthy database:

1. ✅ All tables have `floor_id` set
2. ✅ Every table in `public.tables` has a corresponding entry in `table_status`
3. ✅ `table_status.occupied` matches `tables.status`
4. ✅ Active sessions in `table_sessions` match `table_status.occupied = true`
5. ✅ No orphaned sessions (active sessions for available tables)

## Verification Queries

Run these to check database health:

```sql
-- 1. Check for tables without floor_id
SELECT COUNT(*) as tables_without_floor 
FROM public.tables 
WHERE floor_id IS NULL;

-- 2. Check for missing table_status entries
SELECT COUNT(*) as missing_status_entries
FROM public.tables t
LEFT JOIN public.table_status ts ON t.table_name = ts.label
WHERE ts.label IS NULL;

-- 3. Check for status mismatches
SELECT COUNT(*) as status_mismatches
FROM public.tables t
JOIN public.table_status ts ON t.table_name = ts.label
WHERE (t.status = 'occupied' AND ts.occupied = false)
   OR (t.status = 'available' AND ts.occupied = true);

-- 4. Check for orphaned active sessions
SELECT COUNT(*) as orphaned_sessions
FROM public.table_sessions ts
JOIN public.table_status tst ON ts.table_label = tst.label
WHERE ts.status = 'active' AND tst.occupied = false;
```

All counts should be **0** for a healthy database.

