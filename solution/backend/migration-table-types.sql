-- 1. Create table_types table
CREATE TABLE IF NOT EXISTS public.table_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    has_timer BOOLEAN DEFAULT FALSE,
    hourly_rate DECIMAL(10,2) DEFAULT 0.00,
    allow_orders BOOLEAN DEFAULT TRUE,
    requires_server BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 2. Insert Default Types
-- Type 1: Billiard (Timer = True, $15/hr default)
INSERT INTO public.table_types (name, has_timer, hourly_rate, allow_orders, requires_server)
VALUES ('Billiard', true, 15.00, true, true)
ON CONFLICT (name) DO NOTHING;

-- Type 2: Bar (Timer = False)
INSERT INTO public.table_types (name, has_timer, hourly_rate, allow_orders, requires_server)
VALUES ('Bar', false, 0.00, true, true)
ON CONFLICT (name) DO NOTHING;

-- Type 3: To Go (Timer = False, Orders Only)
INSERT INTO public.table_types (name, has_timer, hourly_rate, allow_orders, requires_server)
VALUES ('To Go', false, 0.00, true, true)
ON CONFLICT (name) DO NOTHING;

-- Type 4: Extra (Generic)
INSERT INTO public.table_types (name, has_timer, hourly_rate, allow_orders, requires_server)
VALUES ('Extra', false, 0.00, true, true)
ON CONFLICT (name) DO NOTHING;

-- 3. Add type_id to tables
ALTER TABLE public.tables 
ADD COLUMN IF NOT EXISTS type_id INT REFERENCES public.table_types(id);

-- 4. Migrate Data
-- Map existing table_type (0=Standard/Bar, 1=Billiard) to new IDs
-- Billiard
UPDATE public.tables 
SET type_id = (SELECT id FROM public.table_types WHERE name = 'Billiard')
WHERE table_type = 1 AND type_id IS NULL;

-- Bar (Default 0)
UPDATE public.tables 
SET type_id = (SELECT id FROM public.table_types WHERE name = 'Bar')
WHERE table_type = 0 AND type_id IS NULL;

-- Safety Default for any others
UPDATE public.tables 
SET type_id = (SELECT id FROM public.table_types WHERE name = 'Bar')
WHERE type_id IS NULL;
