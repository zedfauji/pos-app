-- Create Categories Table for Inventory System
-- This script creates the basic categories table needed for menu items

-- Create categories table
CREATE TABLE IF NOT EXISTS inventory.categories (
    category_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create vendors table if it doesn't exist
CREATE TABLE IF NOT EXISTS inventory.vendors (
    vendor_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    contact_info TEXT,
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Add missing columns to existing vendors table
DO $$
BEGIN
    -- Add budget column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'inventory' 
                   AND table_name = 'vendors' 
                   AND column_name = 'budget') THEN
        ALTER TABLE inventory.vendors ADD COLUMN budget DECIMAL(12,2) DEFAULT 0;
    END IF;
    
    -- Add reminder column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'inventory' 
                   AND table_name = 'vendors' 
                   AND column_name = 'reminder') THEN
        ALTER TABLE inventory.vendors ADD COLUMN reminder VARCHAR(50);
    END IF;
    
    -- Add reminder_enabled column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'inventory' 
                   AND table_name = 'vendors' 
                   AND column_name = 'reminder_enabled') THEN
        ALTER TABLE inventory.vendors ADD COLUMN reminder_enabled BOOLEAN DEFAULT false;
    END IF;
END $$;

-- Create inventory_items table if it doesn't exist
CREATE TABLE IF NOT EXISTS inventory.inventory_items (
    item_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    category_id UUID REFERENCES inventory.categories(category_id),
    vendor_id UUID REFERENCES inventory.vendors(vendor_id),
    selling_price DECIMAL(10,2) NOT NULL DEFAULT 0,
    buying_price DECIMAL(10,2) DEFAULT 0,
    reorder_threshold DECIMAL(10,2) DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    is_menu_available BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create inventory_stock table if it doesn't exist
CREATE TABLE IF NOT EXISTS inventory.inventory_stock (
    item_id UUID PRIMARY KEY REFERENCES inventory.inventory_items(item_id),
    quantity_on_hand DECIMAL(10,2) DEFAULT 0,
    last_counted_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Insert basic categories
INSERT INTO inventory.categories (name, description) VALUES 
('Beers', 'Cervezas nacionales e importadas'),
('Soft Drinks', 'Bebidas gaseosas'),
('Mixed Drinks', 'Bebidas mixtas y cócteles'),
('Fresh Drinks', 'Bebidas frescas naturales'),
('Appetizers', 'Entradas y aperitivos'),
('Craft Beers', 'Cervezas artesanales')
ON CONFLICT (name) DO NOTHING;

-- Insert basic vendors (only if they don't exist)
INSERT INTO inventory.vendors (name, contact_info, status, budget, reminder, reminder_enabled) 
SELECT 'CCU Chile', 'Distribuidora de cervezas', 'active', 1000000, 'Monday', true
WHERE NOT EXISTS (SELECT 1 FROM inventory.vendors WHERE name = 'CCU Chile');

INSERT INTO inventory.vendors (name, contact_info, status, budget, reminder, reminder_enabled) 
SELECT 'Coca Cola Andina', 'Bebidas gaseosas', 'active', 500000, 'Tuesday', true
WHERE NOT EXISTS (SELECT 1 FROM inventory.vendors WHERE name = 'Coca Cola Andina');

INSERT INTO inventory.vendors (name, contact_info, status, budget, reminder, reminder_enabled) 
SELECT 'Proveedor Local', 'Productos locales', 'active', 300000, 'Wednesday', true
WHERE NOT EXISTS (SELECT 1 FROM inventory.vendors WHERE name = 'Proveedor Local');
