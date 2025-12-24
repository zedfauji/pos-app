-- Migration: Add is_deleted to orders schema
ALTER TABLE orders.orders ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE orders.order_items ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS idx_orders_is_deleted ON orders.orders(is_deleted);
CREATE INDEX IF NOT EXISTS idx_order_items_is_deleted ON orders.order_items(is_deleted);
