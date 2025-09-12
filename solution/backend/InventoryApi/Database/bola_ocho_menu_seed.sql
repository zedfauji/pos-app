-- Bola 8 Pool Club Menu Items Database Seeding Script
-- Run these blocks one by one in Cloud SQL Studio

-- ===========================================
-- BLOCK 1: Create Categories
-- ===========================================
INSERT INTO inventory.categories (name, description, is_active, created_at) VALUES 
('Beers', 'Cervezas nacionales e importadas', true, NOW()),
('Soft Drinks', 'Bebidas gaseosas', true, NOW()),
('Mixed Drinks', 'Bebidas mixtas y cócteles', true, NOW()),
('Fresh Drinks', 'Bebidas frescas naturales', true, NOW()),
('Appetizers', 'Entradas y aperitivos', true, NOW()),
('Craft Beers', 'Cervezas artesanales', true, NOW())
ON CONFLICT (name) DO NOTHING;

-- ===========================================
-- BLOCK 2: Create Vendors
-- ===========================================
INSERT INTO inventory.vendors (name, contact_info, status, budget, reminder, reminder_enabled, created_at) 
SELECT 'CCU Chile', 'Distribuidora de cervezas', 'active', 1000000, 'Monday', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM inventory.vendors WHERE name = 'CCU Chile');

INSERT INTO inventory.vendors (name, contact_info, status, budget, reminder, reminder_enabled, created_at) 
SELECT 'Coca Cola Andina', 'Bebidas gaseosas', 'active', 500000, 'Tuesday', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM inventory.vendors WHERE name = 'Coca Cola Andina');

INSERT INTO inventory.vendors (name, contact_info, status, budget, reminder, reminder_enabled, created_at) 
SELECT 'Proveedor Local', 'Productos locales', 'active', 300000, 'Wednesday', true, NOW()
WHERE NOT EXISTS (SELECT 1 FROM inventory.vendors WHERE name = 'Proveedor Local');

-- ===========================================
-- BLOCK 3: Beers (12 items)
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-CORONA', 'Corona', 'Cerveza Lager Mexicana', 3500, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-CORONA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-PACIFICO', 'Pacifico', 'Cerveza Lager Clara', 3500, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-PACIFICO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-VICTORIA', 'Victoria', 'Cerveza Lager Oscura', 3500, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-VICTORIA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-INDIO', 'Indio', 'Cerveza Lager Oscura', 3500, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-INDIO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-TKT-LIGHT', 'Tecate Light', 'Cerveza Lager Ligera', 3000, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-TKT-LIGHT');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-TKT-ROJA', 'Tecate Roja', 'Cerveza Lager Roja', 3000, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-TKT-ROJA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-AMSTEL', 'Amstel', 'Cerveza Lager Holandesa', 3800, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-AMSTEL');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-HEINEKEN', 'Heineken', 'Cerveza Lager Holandesa', 3800, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-HEINEKEN');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-HEINEKEN0', 'Heineken 0.0', 'Cerveza sin alcohol', 3500, c.category_id, v.vendor_id, 15, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-HEINEKEN0');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-CRISTAL', 'Cristal', 'Cerveza Lager Chilena', 3000, c.category_id, v.vendor_id, 25, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-CRISTAL');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-ESCUDO', 'Escudo', 'Cerveza Lager Chilena', 3000, c.category_id, v.vendor_id, 25, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-ESCUDO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'BEER-AUSTRAL', 'Austral', 'Cerveza Lager Chilena', 3200, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'BEER-AUSTRAL');

-- ===========================================
-- BLOCK 4: Craft Beers (16 items)
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-LAGER', 'Kunstmann Lager', 'Cerveza artesanal Lager', 5000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-LAGER');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-WEIZEN', 'Kunstmann Weizen', 'Cerveza artesanal de Trigo', 5500, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-WEIZEN');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-BOCK', 'Kunstmann Bock', 'Cerveza artesanal Bock', 6000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-BOCK');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-IPA', 'Kunstmann IPA', 'Cerveza artesanal India Pale Ale', 6000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-IPA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-STOUT', 'Kunstmann Stout', 'Cerveza artesanal Stout', 6500, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-STOUT');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-AMBAR', 'Kunstmann Ámbar', 'Cerveza artesanal Ámbar', 5500, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-AMBAR');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-HONEY', 'Kunstmann Honey', 'Cerveza artesanal con Miel', 6000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-HONEY');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-TOROBAYO', 'Kunstmann Torobayo', 'Cerveza artesanal Torobayo', 5800, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-TOROBAYO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-SESSION', 'Kunstmann Session IPA', 'Cerveza artesanal Session IPA', 5800, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-SESSION');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-KOLSCH', 'Kunstmann Kolsch', 'Cerveza artesanal Kolsch', 5500, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-KOLSCH');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-PALEALE', 'Kunstmann Pale Ale', 'Cerveza artesanal Pale Ale', 5500, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-PALEALE');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-SCOTCH', 'Kunstmann Scotch Ale', 'Cerveza artesanal Scotch Ale', 6200, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-SCOTCH');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-BELGIAN', 'Kunstmann Belgian Ale', 'Cerveza artesanal Belgian Ale', 6200, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-BELGIAN');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-PORTER', 'Kunstmann Porter', 'Cerveza artesanal Porter', 6000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-PORTER');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-KUNSTMANN-DOPPELBOCK', 'Kunstmann Doppelbock', 'Cerveza artesanal Doppelbock', 7000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-KUNSTMANN-DOPPELBOCK');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'CRAFT-JOHNNIE-WALKER', 'Johnnie Walker', 'Whisky Escocés', 8000, c.category_id, v.vendor_id, 5, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Craft Beers' AND v.name = 'CCU Chile'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'CRAFT-JOHNNIE-WALKER');

-- ===========================================
-- BLOCK 5: Soft Drinks (6 items)
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'SODA-COCA-COLA', 'Coca Cola', 'Bebida gaseosa original', 2000, c.category_id, v.vendor_id, 30, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Soft Drinks' AND v.name = 'Coca Cola Andina'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'SODA-COCA-COLA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'SODA-COCA-LIGHT', 'Coca Cola Light', 'Bebida gaseosa dietética', 2000, c.category_id, v.vendor_id, 30, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Soft Drinks' AND v.name = 'Coca Cola Andina'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'SODA-COCA-LIGHT');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'SODA-SPRITE', 'Sprite', 'Bebida gaseosa sabor lima-limón', 2000, c.category_id, v.vendor_id, 30, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Soft Drinks' AND v.name = 'Coca Cola Andina'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'SODA-SPRITE');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'SODA-FANTA', 'Fanta', 'Bebida gaseosa sabor naranja', 2000, c.category_id, v.vendor_id, 30, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Soft Drinks' AND v.name = 'Coca Cola Andina'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'SODA-FANTA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'SODA-FRESCA', 'Fresca', 'Bebida gaseosa sabor pomelo', 2000, c.category_id, v.vendor_id, 30, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Soft Drinks' AND v.name = 'Coca Cola Andina'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'SODA-FRESCA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'SODA-MANZANITA', 'Manzanita', 'Bebida gaseosa sabor manzana', 2000, c.category_id, v.vendor_id, 30, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Soft Drinks' AND v.name = 'Coca Cola Andina'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'SODA-MANZANITA');

-- ===========================================
-- BLOCK 6: Mixed Drinks (2 items)
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'MIX-MICHELADA', 'Michelada', 'Cerveza preparada con limón, sal y especias', 4500, c.category_id, v.vendor_id, 15, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Mixed Drinks' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'MIX-MICHELADA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'MIX-NEW-MIX', 'New Mix', 'Bebida alcohólica premezclada', 4000, c.category_id, v.vendor_id, 15, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Mixed Drinks' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'MIX-NEW-MIX');

-- ===========================================
-- BLOCK 7: Fresh Drinks (3 items)
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'FRESH-AGUA', 'Agua', 'Botella de agua purificada', 1500, c.category_id, v.vendor_id, 40, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Fresh Drinks' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'FRESH-AGUA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'FRESH-LIMONADA', 'Limonada', 'Jugo natural de limón', 3000, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Fresh Drinks' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'FRESH-LIMONADA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'FRESH-NARANJADA', 'Naranjada', 'Jugo natural de naranja', 3000, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Fresh Drinks' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'FRESH-NARANJADA');

-- ===========================================
-- BLOCK 8: Appetizers (14 items)
-- ===========================================
INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-PAPAS-FRANCESAS', 'Papas Francesas', 'Clásicas papas fritas', 3500, c.category_id, v.vendor_id, 25, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-PAPAS-FRANCESAS');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-PAPAS-DORADAS', 'Papas Doradas', 'Papas fritas con un toque especial', 3800, c.category_id, v.vendor_id, 25, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-PAPAS-DORADAS');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-PAPAS-GAJO', 'Papas Gajo', 'Papas en gajos sazonadas', 4000, c.category_id, v.vendor_id, 25, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-PAPAS-GAJO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-PAPAS-BOTANERA', 'Papas Botanera', 'Papas con aderezo especial', 4200, c.category_id, v.vendor_id, 25, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-PAPAS-BOTANERA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-ALITAS', 'Alitas', 'Alitas de pollo con salsa a elección', 6000, c.category_id, v.vendor_id, 15, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-ALITAS');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-BONELESS', 'Boneless', 'Trozos de pollo sin hueso con salsa', 5800, c.category_id, v.vendor_id, 15, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-BONELESS');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-TENDERS', 'Tenders', 'Tiras de pollo apanadas', 5500, c.category_id, v.vendor_id, 15, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-TENDERS');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-AROS-CEBOLLA', 'Aros de Cebolla', 'Crujientes aros de cebolla', 3800, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-AROS-CEBOLLA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-DEDOS-QUESO', 'Dedos de Queso', 'Dedos de queso mozzarella apanados', 4500, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-DEDOS-QUESO');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-SALCHIPULPOS', 'Salchipulpos', 'Salchichas cortadas en forma de pulpo con papas', 4000, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-SALCHIPULPOS');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-TIRAS-CAMOTE', 'Tiras de Camote', 'Tiras de camote fritas', 4200, c.category_id, v.vendor_id, 20, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-TIRAS-CAMOTE');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-CARNE-SECA', 'Carne Seca', 'Carne seca sazonada', 6500, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-CARNE-SECA');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-JAMON-RES', 'Jamón Res', 'Porción de jamón de res', 5000, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-JAMON-RES');

INSERT INTO inventory.inventory_items (sku, name, description, selling_price, category_id, vendor_id, reorder_threshold, is_active, created_at) 
SELECT 'APP-JAMON-POLLO', 'Jamón Pollo', 'Porción de jamón de pollo', 4800, c.category_id, v.vendor_id, 10, true, NOW()
FROM inventory.categories c, inventory.vendors v 
WHERE c.name = 'Appetizers' AND v.name = 'Proveedor Local'
AND NOT EXISTS (SELECT 1 FROM inventory.inventory_items WHERE sku = 'APP-JAMON-POLLO');

-- ===========================================
-- BLOCK 9: Initialize Stock
-- ===========================================
INSERT INTO inventory.inventory_stock (item_id, quantity_on_hand, last_counted_at)
SELECT item_id, 100, CURRENT_TIMESTAMP
FROM inventory.inventory_items
WHERE item_id NOT IN (SELECT item_id FROM inventory.inventory_stock);