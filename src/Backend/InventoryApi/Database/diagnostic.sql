-- DIAGNOSTIC SCRIPT - Run this first to see what's in your database
-- ===========================================

-- Check what categories exist
SELECT 'CATEGORIES:' as info;
SELECT category_id, name FROM inventory.categories;

-- Check what vendors exist  
SELECT 'VENDORS:' as info;
SELECT vendor_id, name FROM inventory.vendors;

-- Check if the specific names we're looking for exist
SELECT 'LOOKING FOR CATEGORY "Beers":' as info;
SELECT category_id, name FROM inventory.categories WHERE name = 'Beers';

SELECT 'LOOKING FOR VENDOR "CCU Chile":' as info;
SELECT vendor_id, name FROM inventory.vendors WHERE name = 'CCU Chile';
