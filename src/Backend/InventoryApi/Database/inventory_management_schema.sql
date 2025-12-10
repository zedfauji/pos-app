-- Inventory Management System Database Schema
-- This script creates the necessary tables for the Inventory Management System

-- Create restock_requests table
CREATE TABLE IF NOT EXISTS inventory.restock_requests (
    request_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    item_id UUID NOT NULL REFERENCES inventory.inventory_items(item_id),
    requested_quantity DECIMAL(10,2) NOT NULL CHECK (requested_quantity > 0),
    created_by VARCHAR(255) NOT NULL,
    created_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    status VARCHAR(50) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending', 'Approved', 'Ordered', 'Received', 'Cancelled')),
    priority INTEGER NOT NULL DEFAULT 1 CHECK (priority BETWEEN 1 AND 4),
    notes TEXT,
    approved_by VARCHAR(255),
    approved_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    vendor_id UUID REFERENCES inventory.vendors(vendor_id),
    unit_cost DECIMAL(10,2),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create vendor_orders table
CREATE TABLE IF NOT EXISTS inventory.vendor_orders (
    order_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_id UUID NOT NULL REFERENCES inventory.vendors(vendor_id),
    order_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    expected_delivery_date TIMESTAMP WITH TIME ZONE,
    actual_delivery_date TIMESTAMP WITH TIME ZONE,
    status VARCHAR(50) NOT NULL DEFAULT 'Draft' CHECK (status IN ('Draft', 'Sent', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled')),
    priority INTEGER NOT NULL DEFAULT 1 CHECK (priority BETWEEN 1 AND 4),
    total_value DECIMAL(12,2) NOT NULL DEFAULT 0,
    item_count INTEGER NOT NULL DEFAULT 0,
    notes TEXT,
    created_by VARCHAR(255) NOT NULL,
    sent_by VARCHAR(255),
    sent_date TIMESTAMP WITH TIME ZONE,
    confirmed_by VARCHAR(255),
    confirmed_date TIMESTAMP WITH TIME ZONE,
    tracking_number VARCHAR(255),
    delivery_days INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create audit_logs table
CREATE TABLE IF NOT EXISTS inventory.audit_logs (
    audit_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    entity_type VARCHAR(100) NOT NULL,
    entity_id UUID NOT NULL,
    action VARCHAR(100) NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    user_name VARCHAR(255),
    details TEXT,
    old_values TEXT,
    new_values TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create users table for audit purposes
CREATE TABLE IF NOT EXISTS inventory.users (
    user_id VARCHAR(255) PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    role VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_restock_requests_item_id ON inventory.restock_requests(item_id);
CREATE INDEX IF NOT EXISTS idx_restock_requests_status ON inventory.restock_requests(status);
CREATE INDEX IF NOT EXISTS idx_restock_requests_vendor_id ON inventory.restock_requests(vendor_id);
CREATE INDEX IF NOT EXISTS idx_restock_requests_created_date ON inventory.restock_requests(created_date);

CREATE INDEX IF NOT EXISTS idx_vendor_orders_vendor_id ON inventory.vendor_orders(vendor_id);
CREATE INDEX IF NOT EXISTS idx_vendor_orders_status ON inventory.vendor_orders(status);
CREATE INDEX IF NOT EXISTS idx_vendor_orders_order_date ON inventory.vendor_orders(order_date);
CREATE INDEX IF NOT EXISTS idx_vendor_orders_expected_delivery ON inventory.vendor_orders(expected_delivery_date);

CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON inventory.audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_timestamp ON inventory.audit_logs(timestamp);
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON inventory.audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON inventory.audit_logs(action);

-- Create triggers for updated_at timestamps
CREATE OR REPLACE FUNCTION inventory.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_restock_requests_updated_at 
    BEFORE UPDATE ON inventory.restock_requests 
    FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at_column();

CREATE TRIGGER update_vendor_orders_updated_at 
    BEFORE UPDATE ON inventory.vendor_orders 
    FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at_column();

CREATE TRIGGER update_users_updated_at 
    BEFORE UPDATE ON inventory.users 
    FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at_column();

-- Insert sample data for testing
INSERT INTO inventory.users (user_id, name, email, role) VALUES 
('admin', 'System Administrator', 'admin@magidesk.com', 'admin'),
('manager', 'Inventory Manager', 'manager@magidesk.com', 'manager'),
('staff', 'Staff Member', 'staff@magidesk.com', 'staff')
ON CONFLICT (user_id) DO NOTHING;

-- Insert sample audit logs
INSERT INTO inventory.audit_logs (entity_type, entity_id, action, user_id, user_name, details) VALUES 
('System', gen_random_uuid(), 'Startup', 'admin', 'System Administrator', 'Inventory Management System initialized'),
('System', gen_random_uuid(), 'Schema', 'admin', 'System Administrator', 'Database schema updated for Inventory Management System')
ON CONFLICT DO NOTHING;



