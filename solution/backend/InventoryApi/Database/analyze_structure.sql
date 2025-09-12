-- Database Structure Analysis Script
-- Run this in Cloud SQL Studio to see what's actually in your database

-- Check if inventory schema exists
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name = 'inventory';

-- Check what tables exist in inventory schema
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'inventory';

-- Check categories table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_schema = 'inventory' AND table_name = 'categories';

-- Check vendors table structure  
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_schema = 'inventory' AND table_name = 'vendors';

-- Check inventory_items table structure
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_schema = 'inventory' AND table_name = 'inventory_items';

-- Check what data exists
SELECT 'CATEGORIES:' as info;
SELECT * FROM inventory.categories LIMIT 10;

SELECT 'VENDORS:' as info;
SELECT * FROM inventory.vendors LIMIT 10;

SELECT 'INVENTORY_ITEMS:' as info;
SELECT * FROM inventory.inventory_items LIMIT 10;



