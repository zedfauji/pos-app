-- CORRECTED Bola 8 Pool Club Menu Items Database Seeding Script
-- Based on actual database structure analysis
-- Run these blocks one by one in Cloud SQL Studio

-- ===========================================
-- BLOCK 1: Create Categories (already exists, skip)
-- ===========================================
-- Categories already exist from previous runs

-- ===========================================
-- BLOCK 2: Create Vendors (already exists, skip)  
-- ===========================================
-- Vendors already exist from previous runs

-- ===========================================
-- BLOCK 3: Beers (12 items) - CORRECTED
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-CORONA', 'Corona', 'Cerveza Lager Mexicana', 3500, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-CORONA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-PACIFICO', 'Pacifico', 'Cerveza Lager Clara', 3500, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-PACIFICO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-VICTORIA', 'Victoria', 'Cerveza Lager Oscura', 3500, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-VICTORIA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-INDIO', 'Indio', 'Cerveza Lager Oscura', 3500, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-INDIO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-TKT-LIGHT', 'Tecate Light', 'Cerveza Lager Ligera', 3000, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-TKT-LIGHT');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-TKT-ROJA', 'Tecate Roja', 'Cerveza Lager Roja', 3000, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-TKT-ROJA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-AMSTEL', 'Amstel', 'Cerveza Lager Holandesa', 3800, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-AMSTEL');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-HEINEKEN', 'Heineken', 'Cerveza Lager Holandesa', 3800, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-HEINEKEN');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-HEINEKEN0', 'Heineken 0.0', 'Cerveza sin alcohol', 3500, c.category_id, v.vendor_id, 15, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-HEINEKEN0');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-CRISTAL', 'Cristal', 'Cerveza Lager Chilena', 3000, c.category_id, v.vendor_id, 25, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-CRISTAL');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-ESCUDO', 'Escudo', 'Cerveza Lager Chilena', 3000, c.category_id, v.vendor_id, 25, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-ESCUDO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, is_menu_available, unit) 
SELECT 'BEER-AUSTRAL', 'Austral', 'Cerveza Lager Chilena', 3200, c.category_id, v.vendor_id, 20, true, true, 'unit'
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-AUSTRAL');

-- ===========================================
-- BLOCK 4: Initialize Stock for Beers
-- ===========================================
INSERT INTO inventory.inventory_stock (item_id, quantity_on_hand, last_counted_at)
SELECT item_id, 100, CURRENT_TIMESTAMP
FROM inventory.inventory_items
WHERE sku LIKE 'BEER-%'
AND item_id NOT IN (SELECT item_id FROM inventory.inventory_stock);



