-- Migration: Align orders schema with OrderApi requirements
-- Target: orders.orders

-- 1. Add delivery_status
ALTER TABLE orders.orders ADD COLUMN IF NOT EXISTS delivery_status VARCHAR(50) NOT NULL DEFAULT 'pending';

-- 2. Add profit_total
ALTER TABLE orders.orders ADD COLUMN IF NOT EXISTS profit_total NUMERIC NOT NULL DEFAULT 0;

-- 3. Add shift_id
ALTER TABLE orders.orders ADD COLUMN IF NOT EXISTS shift_id UUID;

-- 4. Ensure order_items has snapshot columns (Code uses snapshot_name, etc)
-- Checking orders.order_items vs ord.order_items
-- ord.order_items has snapshot_name. orders.order_items has modifiers (jsonb).
-- Let's add snapshot columns to orders.order_items to allow full history preservation if code uses them.
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS snapshot_name VARCHAR(255);
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS snapshot_sku VARCHAR(100);
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS snapshot_category VARCHAR(100);
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS snapshot_group VARCHAR(100);
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS snapshot_version INT;
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS snapshot_picture_url TEXT;

-- 5. Add selected_modifiers (Code uses this name) - DB has 'modifiers'
-- We can alias in code, OR add the column. 
-- 'modifiers' is generic. 'selected_modifiers' is explicit.
-- Let's stick to 'modifiers' in DB (it exists), we will map in Code.
-- But wait, code inserts into 'selected_modifiers'. If I change code to 'modifiers', it works.
-- So no DB change for modifiers.

-- 6. Add Indexes
CREATE INDEX IF NOT EXISTS idx_orders_shift_id ON orders.orders(shift_id);
CREATE INDEX IF NOT EXISTS idx_orders_delivery_status ON orders.orders(delivery_status);

