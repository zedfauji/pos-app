-- Migration: Multi-Floor Layout System
-- Description: Adds Floors table and refactors Tables table to support multi-floor layouts with visual positioning
-- Date: 2024

-- Create Floors table
CREATE TABLE IF NOT EXISTS public.floors (
    floor_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    floor_name text NOT NULL,
    description text NULL,
    is_default boolean NOT NULL DEFAULT false,
    is_active boolean NOT NULL DEFAULT true,
    display_order integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT floors_name_unique UNIQUE (floor_name)
);

-- Create index for default floor lookup
CREATE INDEX IF NOT EXISTS idx_floors_default ON public.floors(is_default) WHERE is_default = true;
CREATE INDEX IF NOT EXISTS idx_floors_active ON public.floors(is_active) WHERE is_active = true;

-- Migrate existing table_status to new tables structure
-- First, create a default floor if none exists
INSERT INTO public.floors (floor_name, description, is_default, is_active, display_order)
SELECT 'Main Floor', 'Default floor for existing tables', true, true, 0
WHERE NOT EXISTS (SELECT 1 FROM public.floors WHERE is_default = true);

-- Get the default floor ID
DO $$
DECLARE
    default_floor_id uuid;
BEGIN
    SELECT floor_id INTO default_floor_id FROM public.floors WHERE is_default = true LIMIT 1;
    
    -- Create new tables table with layout support
    CREATE TABLE IF NOT EXISTS public.tables (
        table_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
        floor_id uuid NOT NULL REFERENCES public.floors(floor_id) ON DELETE CASCADE,
        table_name text NOT NULL,
        table_number text NULL,
        table_type text NOT NULL DEFAULT 'billiard', -- billiard | bar
        x_position numeric(10,2) NOT NULL DEFAULT 0,
        y_position numeric(10,2) NOT NULL DEFAULT 0,
        rotation numeric(5,2) NOT NULL DEFAULT 0, -- degrees 0-360
        size text NOT NULL DEFAULT 'M', -- S | M | L
        width numeric(10,2) NULL, -- pixels/cm
        height numeric(10,2) NULL, -- pixels/cm
        status text NOT NULL DEFAULT 'available', -- available | occupied
        billing_rate numeric(10,2) NOT NULL DEFAULT 0, -- per hour
        auto_start_timer boolean NOT NULL DEFAULT false,
        icon_style text NULL, -- custom icon identifier
        grouping_tags text[] NULL, -- VIP, Special Area, etc.
        is_active boolean NOT NULL DEFAULT true,
        is_locked boolean NOT NULL DEFAULT false, -- lock position in designer
        order_id text NULL,
        start_time timestamptz NULL,
        server text NULL,
        created_at timestamptz NOT NULL DEFAULT now(),
        updated_at timestamptz NOT NULL DEFAULT now(),
        CONSTRAINT tables_floor_name_unique UNIQUE (floor_id, table_name)
    );
    
    -- Migrate data from table_status to tables
    IF default_floor_id IS NOT NULL THEN
        INSERT INTO public.tables (
            floor_id, table_name, table_number, table_type, 
            x_position, y_position, status, order_id, start_time, server, is_active
        )
        SELECT 
            default_floor_id,
            label as table_name,
            label as table_number,
            type as table_type,
            0 as x_position,
            0 as y_position,
            CASE WHEN occupied THEN 'occupied' ELSE 'available' END as status,
            order_id,
            start_time,
            server,
            true as is_active
        FROM public.table_status
        ON CONFLICT (floor_id, table_name) DO NOTHING;
    END IF;
END $$;

-- Create indexes for tables
CREATE INDEX IF NOT EXISTS idx_tables_floor_id ON public.tables(floor_id);
CREATE INDEX IF NOT EXISTS idx_tables_status ON public.tables(status);
CREATE INDEX IF NOT EXISTS idx_tables_type ON public.tables(table_type);
CREATE INDEX IF NOT EXISTS idx_tables_active ON public.tables(is_active) WHERE is_active = true;

-- Create table_layout_history for versioning (optional but recommended)
CREATE TABLE IF NOT EXISTS public.table_layout_history (
    history_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    floor_id uuid NOT NULL REFERENCES public.floors(floor_id) ON DELETE CASCADE,
    version_number integer NOT NULL,
    layout_data jsonb NOT NULL, -- snapshot of all tables on floor
    created_by text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    description text NULL,
    CONSTRAINT layout_history_version_unique UNIQUE (floor_id, version_number)
);

CREATE INDEX IF NOT EXISTS idx_layout_history_floor ON public.table_layout_history(floor_id, created_at DESC);

-- Create app_settings table if it doesn't exist (for layout settings)
CREATE TABLE IF NOT EXISTS public.app_settings (
    key text PRIMARY KEY,
    value text NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT now()
);

-- Insert default layout settings
INSERT INTO public.app_settings (key, value) VALUES
    ('Layout.GridSize', '20'),
    ('Layout.SnapToGrid', 'true'),
    ('Layout.DefaultTableSize', 'M'),
    ('Layout.ShowTableNumbers', 'true'),
    ('Layout.FloorSwitchLock', 'false'),
    ('Layout.ProtectLayoutChanges', 'false')
ON CONFLICT (key) DO NOTHING;

-- Add updated_at trigger for floors
CREATE OR REPLACE FUNCTION update_floors_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_floors_updated_at
    BEFORE UPDATE ON public.floors
    FOR EACH ROW
    EXECUTE FUNCTION update_floors_updated_at();

-- Add updated_at trigger for tables
CREATE OR REPLACE FUNCTION update_tables_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_tables_updated_at
    BEFORE UPDATE ON public.tables
    FOR EACH ROW
    EXECUTE FUNCTION update_tables_updated_at();

-- Ensure only one default floor exists
CREATE OR REPLACE FUNCTION ensure_single_default_floor()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.is_default = true THEN
        UPDATE public.floors SET is_default = false WHERE floor_id != NEW.floor_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_ensure_single_default_floor
    AFTER INSERT OR UPDATE OF is_default ON public.floors
    FOR EACH ROW
    WHEN (NEW.is_default = true)
    EXECUTE FUNCTION ensure_single_default_floor();

