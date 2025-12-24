-- Migration: Create order_logs in orders schema
CREATE TABLE IF NOT EXISTS orders.order_logs (
    log_id BIGSERIAL PRIMARY KEY,
    order_id UUID NOT NULL REFERENCES orders.orders(order_id),
    action TEXT NOT NULL,
    old_value JSONB,
    new_value JSONB,
    server_id TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_orders_logs_order_id ON orders.order_logs(order_id);
