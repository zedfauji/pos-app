-- ===========================================
-- COMPLETE INVENTORY DATABASE RESTORE SCRIPT
-- Includes: Schema + Current Data + Bola 8 Menu Items
-- Run this script to restore the entire inventory database
-- ===========================================

-- Drop existing schema and recreate (WARNING: This will delete all existing data)
DROP SCHEMA IF EXISTS inventory CASCADE;
CREATE SCHEMA inventory;

-- Set search path
SET search_path TO inventory, public;

-- Create transaction source enum
CREATE TYPE inventory.transaction_source AS ENUM (
    'purchase',
    'customer_order',
    'adjustment',
    'wastage',
    'transfer'
);

-- Create update function
CREATE FUNCTION inventory.update_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$    
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
    $$;

-- Create tables
CREATE TABLE inventory.categories (
    category_id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    is_active boolean DEFAULT true,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now()
);

CREATE TABLE inventory.inventory_categories (
    category_id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    parent_id uuid,
    path text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT valid_name CHECK ((name <> ''::text))
);

CREATE TABLE inventory.vendors (
    vendor_id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    contact_info text,
    status text DEFAULT 'active'::text NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    budget numeric(12,2) DEFAULT 0,
    reminder character varying(50),
    reminder_enabled boolean DEFAULT false,
    CONSTRAINT valid_name CHECK ((name <> ''::text)),
    CONSTRAINT vendors_status_check CHECK ((status = ANY (ARRAY['active'::text, 'inactive'::text])))
);

CREATE TABLE inventory.inventory_items (
    item_id uuid DEFAULT gen_random_uuid() NOT NULL,
    vendor_id uuid,
    category_id uuid,
    sku text NOT NULL,
    name text NOT NULL,
    description text,
    unit text,
    barcode text,
    reorder_threshold numeric(18,3),
    buying_price numeric(18,2),
    selling_price numeric(18,2),
    tax_rate numeric(5,2),
    is_menu_available boolean DEFAULT false NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT valid_name CHECK ((name <> ''::text)),
    CONSTRAINT valid_prices CHECK (((buying_price >= (0)::numeric) AND (selling_price >= (0)::numeric))),
    CONSTRAINT valid_sku CHECK ((sku <> ''::text)),
    CONSTRAINT valid_tax_rate CHECK (((tax_rate >= (0)::numeric) AND (tax_rate <= (100)::numeric)))
);

CREATE TABLE inventory.inventory_stock (
    item_id uuid NOT NULL,
    quantity_on_hand numeric(18,3) DEFAULT 0 NOT NULL,
    last_counted_at timestamp with time zone,
    CONSTRAINT inventory_stock_quantity_on_hand_check CHECK ((quantity_on_hand >= (0)::numeric))
);

CREATE TABLE inventory.inventory_transactions (
    transaction_id uuid DEFAULT gen_random_uuid() NOT NULL,
    item_id uuid NOT NULL,
    delta numeric(18,3) NOT NULL,
    quantity_before numeric(18,3) NOT NULL,
    quantity_after numeric(18,3) NOT NULL,
    unit_cost numeric(18,2),
    source inventory.transaction_source NOT NULL,
    source_ref text,
    user_id text,
    notes text,
    occurred_at timestamp with time zone DEFAULT now() NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT inventory_transactions_quantity_after_check CHECK ((quantity_after >= (0)::numeric)),
    CONSTRAINT inventory_transactions_quantity_before_check CHECK ((quantity_before >= (0)::numeric)),
    CONSTRAINT inventory_transactions_unit_cost_check CHECK ((unit_cost >= (0)::numeric))
);

-- Create view
CREATE VIEW inventory.v_items_current AS
 SELECT i.item_id,
    i.vendor_id,
    i.category_id,
    i.sku,
    i.name,
    i.description,
    i.selling_price,
    COALESCE(s.quantity_on_hand, (0)::numeric) AS quantity_on_hand,
    i.is_menu_available
   FROM (inventory.inventory_items i
     LEFT JOIN inventory.inventory_stock s ON ((s.item_id = i.item_id)))
  WHERE (i.is_active = true);

-- Add constraints
ALTER TABLE ONLY inventory.categories
    ADD CONSTRAINT categories_name_key UNIQUE (name);

ALTER TABLE ONLY inventory.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (category_id);

ALTER TABLE ONLY inventory.inventory_categories
    ADD CONSTRAINT inventory_categories_pkey PRIMARY KEY (category_id);

ALTER TABLE ONLY inventory.vendors
    ADD CONSTRAINT vendors_pkey PRIMARY KEY (vendor_id);

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_pkey PRIMARY KEY (item_id);

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_sku_key UNIQUE (sku);

ALTER TABLE ONLY inventory.inventory_stock
    ADD CONSTRAINT inventory_stock_pkey PRIMARY KEY (item_id);

ALTER TABLE ONLY inventory.inventory_transactions
    ADD CONSTRAINT inventory_transactions_pkey PRIMARY KEY (transaction_id);

-- Add foreign key constraints
ALTER TABLE ONLY inventory.inventory_categories
    ADD CONSTRAINT inventory_categories_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES inventory.inventory_categories(category_id) ON DELETE SET NULL;

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_category_id_fkey FOREIGN KEY (category_id) REFERENCES inventory.categories(category_id) ON DELETE SET NULL;

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_vendor_id_fkey FOREIGN KEY (vendor_id) REFERENCES inventory.vendors(vendor_id) ON DELETE SET NULL;

ALTER TABLE ONLY inventory.inventory_stock
    ADD CONSTRAINT inventory_stock_item_id_fkey FOREIGN KEY (item_id) REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE;

ALTER TABLE ONLY inventory.inventory_transactions
    ADD CONSTRAINT inventory_transactions_item_id_fkey FOREIGN KEY (item_id) REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE;

-- Create indexes
CREATE INDEX idx_inventory_items_category ON inventory.inventory_items USING btree (category_id);
CREATE INDEX idx_inventory_items_menu ON inventory.inventory_items USING btree (is_menu_available);
CREATE INDEX idx_inventory_items_vendor ON inventory.inventory_items USING btree (vendor_id);
CREATE INDEX idx_inventory_tx_item_time ON inventory.inventory_transactions USING btree (item_id, occurred_at);

-- Create triggers
CREATE TRIGGER update_items_updated_at BEFORE UPDATE ON inventory.inventory_items FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at();
CREATE TRIGGER update_vendors_updated_at BEFORE UPDATE ON inventory.vendors FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at();

-- ===========================================
-- INSERT CATEGORIES
-- ===========================================
INSERT INTO inventory.categories (category_id, name, description, is_active, created_at, updated_at) VALUES
('79369c6f-4f88-48df-8f95-ceae0121aab6', 'Beers', 'Cervezas nacionales e importadas', true, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00'),
('a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'Soft Drinks', 'Bebidas gaseosas', true, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00'),
('cea8a622-e8ae-4734-aec2-527d97d4f794', 'Mixed Drinks', 'Bebidas mixtas y cócteles', true, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00'),
('4178e834-b41b-4092-9fc4-0d938ca79525', 'Fresh Drinks', 'Bebidas frescas naturales', true, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00'),
('fdf1fef8-9731-442a-894f-97f845f1928d', 'Appetizers', 'Entradas y aperitivos', true, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00'),
('5e66a062-7a02-4026-85e7-b200c44f2ba0', 'Craft Beers', 'Cervezas artesanales', true, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00');

-- ===========================================
-- INSERT VENDORS
-- ===========================================
INSERT INTO inventory.vendors (vendor_id, name, contact_info, status, notes, created_at, updated_at, budget, reminder, reminder_enabled) VALUES
('9468fcb9-b035-41ad-a24a-307b1c83c334', 'CCU Chile', 'Distribuidora de cervezas', 'active', NULL, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00', 0.00, NULL, false),
('4512010e-447e-44d6-b167-638011e97f80', 'Coca Cola Andina', 'Bebidas gaseosas', 'active', NULL, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00', 0.00, NULL, false),
('92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'Proveedor Local', 'Productos locales', 'active', NULL, '2025-09-12 02:37:37.902267+00', '2025-09-12 02:37:37.902267+00', 0.00, NULL, false);

-- ===========================================
-- INSERT BOLA 8 MENU ITEMS
-- ===========================================

-- Beers (12 items)
INSERT INTO inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) VALUES
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-CORONA', 'Corona', 'Cerveza Lager Mexicana', 'unit', NULL, 20, 2500, 3500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-PACIFICO', 'Pacifico', 'Cerveza Lager Clara', 'unit', NULL, 20, 2500, 3500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-VICTORIA', 'Victoria', 'Cerveza Lager Oscura', 'unit', NULL, 20, 2500, 3500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-INDIO', 'Indio', 'Cerveza Lager Oscura', 'unit', NULL, 20, 2500, 3500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-TKT-LIGHT', 'Tecate Light', 'Cerveza Lager Ligera', 'unit', NULL, 20, 2200, 3000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-TKT-ROJA', 'Tecate Roja', 'Cerveza Lager Roja', 'unit', NULL, 20, 2200, 3000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-AMSTEL', 'Amstel', 'Cerveza Lager Holandesa', 'unit', NULL, 20, 2800, 3800, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-HEINEKEN', 'Heineken', 'Cerveza Lager Holandesa', 'unit', NULL, 20, 2800, 3800, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-HEINEKEN0', 'Heineken 0.0', 'Cerveza sin alcohol', 'unit', NULL, 15, 2500, 3500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-CRISTAL', 'Cristal', 'Cerveza Lager Chilena', 'unit', NULL, 25, 2000, 3000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-ESCUDO', 'Escudo', 'Cerveza Lager Chilena', 'unit', NULL, 25, 2000, 3000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '9468fcb9-b035-41ad-a24a-307b1c83c334', '79369c6f-4f88-48df-8f95-ceae0121aab6', 'BEER-AUSTRAL', 'Austral', 'Cerveza Lager Chilena', 'unit', NULL, 20, 2200, 3200, 19.0, true, true, NOW(), NOW());

-- Soft Drinks (8 items)
INSERT INTO inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) VALUES
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-COCA-COLA', 'Coca Cola', 'Bebida gaseosa cola', 'unit', NULL, 30, 800, 1500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-SPRITE', 'Sprite', 'Bebida gaseosa limón-lima', 'unit', NULL, 30, 800, 1500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-FANTA', 'Fanta', 'Bebida gaseosa naranja', 'unit', NULL, 30, 800, 1500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-COCA-ZERO', 'Coca Cola Zero', 'Bebida gaseosa cola sin azúcar', 'unit', NULL, 25, 900, 1500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-COCA-LIGHT', 'Coca Cola Light', 'Bebida gaseosa cola light', 'unit', NULL, 25, 900, 1500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-SCHWEPPES', 'Schweppes', 'Bebida gaseosa tónica', 'unit', NULL, 20, 1000, 1800, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-GINGER-ALE', 'Ginger Ale', 'Bebida gaseosa jengibre', 'unit', NULL, 20, 1000, 1800, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '4512010e-447e-44d6-b167-638011e97f80', 'a11af6bd-c42f-43fe-8bb2-ace1b740616d', 'SODA-AGUA-TONICA', 'Agua Tónica', 'Bebida gaseosa tónica premium', 'unit', NULL, 15, 1200, 2000, 19.0, true, true, NOW(), NOW());

-- Mixed Drinks (6 items)
INSERT INTO inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) VALUES
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'cea8a622-e8ae-4734-aec2-527d97d4f794', 'COCKTAIL-MOJITO', 'Mojito', 'Ron blanco, menta, lima, soda', 'unit', NULL, 10, 2000, 4500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'cea8a622-e8ae-4734-aec2-527d97d4f794', 'COCKTAIL-CUBA-LIBRE', 'Cuba Libre', 'Ron blanco, Coca Cola, lima', 'unit', NULL, 10, 1800, 4000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'cea8a622-e8ae-4734-aec2-527d97d4f794', 'COCKTAIL-PINA-COLADA', 'Piña Colada', 'Ron blanco, crema de coco, piña', 'unit', NULL, 8, 2500, 5000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'cea8a622-e8ae-4734-aec2-527d97d4f794', 'COCKTAIL-MARGARITA', 'Margarita', 'Tequila, triple sec, lima', 'unit', NULL, 8, 2200, 4800, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'cea8a622-e8ae-4734-aec2-527d97d4f794', 'COCKTAIL-BLOODY-MARY', 'Bloody Mary', 'Vodka, jugo de tomate, especias', 'unit', NULL, 6, 2000, 4500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'cea8a622-e8ae-4734-aec2-527d97d4f794', 'COCKTAIL-COSMOPOLITAN', 'Cosmopolitan', 'Vodka, triple sec, arándanos', 'unit', NULL, 6, 2500, 5200, 19.0, true, true, NOW(), NOW());

-- Fresh Drinks (4 items)
INSERT INTO inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) VALUES
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '4178e834-b41b-4092-9fc4-0d938ca79525', 'FRESH-LIMONADA', 'Limonada Natural', 'Limones frescos, agua, azúcar', 'unit', NULL, 15, 500, 2000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '4178e834-b41b-4092-9fc4-0d938ca79525', 'FRESH-NARANJADA', 'Naranjada Natural', 'Naranjas frescas, agua, azúcar', 'unit', NULL, 15, 500, 2000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '4178e834-b41b-4092-9fc4-0d938ca79525', 'FRESH-JUGO-MANZANA', 'Jugo de Manzana', 'Manzanas frescas, agua', 'unit', NULL, 12, 600, 2200, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '4178e834-b41b-4092-9fc4-0d938ca79525', 'FRESH-AGUA-COCO', 'Agua de Coco', 'Coco fresco, agua', 'unit', NULL, 10, 800, 2500, 19.0, true, true, NOW(), NOW());

-- Appetizers (8 items)
INSERT INTO inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) VALUES
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-NACHOS', 'Nachos con Queso', 'Tortillas de maíz, queso fundido', 'portion', NULL, 20, 1500, 3500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-WINGS', 'Alitas de Pollo', 'Alitas marinadas, salsa picante', 'portion', NULL, 15, 2000, 4500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-CHEESE-STICKS', 'Palitos de Queso', 'Queso empanizado, salsa marinara', 'portion', NULL, 12, 1200, 3000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-BUFFALO-WINGS', 'Alitas Buffalo', 'Alitas con salsa buffalo', 'portion', NULL, 15, 2000, 4500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-SHRIMP-COCKTAIL', 'Cóctel de Camarones', 'Camarones cocidos, salsa cóctel', 'portion', NULL, 8, 3000, 6000, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-GUACAMOLE', 'Guacamole', 'Aguacate, tomate, cebolla, cilantro', 'portion', NULL, 10, 800, 2500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-SALSA-CHIPS', 'Chips y Salsa', 'Tortillas fritas, salsa picante', 'portion', NULL, 20, 500, 1800, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', 'fdf1fef8-9731-442a-894f-97f845f1928d', 'APPETIZER-BEER-NUTS', 'Maní Cervecero', 'Maní tostado con especias', 'portion', NULL, 25, 300, 1200, 19.0, true, true, NOW(), NOW());

-- Craft Beers (4 items)
INSERT INTO inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) VALUES
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '5e66a062-7a02-4026-85e7-b200c44f2ba0', 'CRAFT-IPA-LOCAL', 'IPA Local', 'Cerveza artesanal IPA', 'unit', NULL, 8, 3000, 5500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '5e66a062-7a02-4026-85e7-b200c44f2ba0', 'CRAFT-STOUT-LOCAL', 'Stout Local', 'Cerveza artesanal Stout', 'unit', NULL, 8, 3000, 5500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '5e66a062-7a02-4026-85e7-b200c44f2ba0', 'CRAFT-PALE-ALE', 'Pale Ale', 'Cerveza artesanal Pale Ale', 'unit', NULL, 8, 3000, 5500, 19.0, true, true, NOW(), NOW()),
(gen_random_uuid(), '92d164ad-eeb1-4d86-b641-87fa5e3b2114', '5e66a062-7a02-4026-85e7-b200c44f2ba0', 'CRAFT-WHEAT-BEER', 'Wheat Beer', 'Cerveza artesanal de trigo', 'unit', NULL, 8, 3000, 5500, 19.0, true, true, NOW(), NOW());

-- ===========================================
-- INITIALIZE STOCK LEVELS
-- ===========================================

-- Initialize stock for all items (100 units each)
INSERT INTO inventory.inventory_stock (item_id, quantity_on_hand, last_counted_at)
SELECT item_id, 100, CURRENT_TIMESTAMP
FROM inventory.inventory_items
WHERE is_active = true;

-- ===========================================
-- CREATE INITIAL TRANSACTIONS
-- ===========================================

-- Create initial stock transactions for all items
INSERT INTO inventory.inventory_transactions (transaction_id, item_id, delta, quantity_before, quantity_after, unit_cost, source, source_ref, user_id, notes, occurred_at, created_at)
SELECT 
    gen_random_uuid(),
    item_id,
    100,
    0,
    100,
    buying_price,
    'purchase',
    'INITIAL_STOCK',
    'system',
    'Initial stock setup',
    NOW(),
    NOW()
FROM inventory.inventory_items
WHERE is_active = true;

-- ===========================================
-- RESTORE COMPLETE
-- ===========================================

-- Verify data
SELECT 'Categories: ' || COUNT(*) FROM inventory.categories;
SELECT 'Vendors: ' || COUNT(*) FROM inventory.vendors;
SELECT 'Items: ' || COUNT(*) FROM inventory.inventory_items;
SELECT 'Stock Records: ' || COUNT(*) FROM inventory.inventory_stock;
SELECT 'Transactions: ' || COUNT(*) FROM inventory.inventory_transactions;

-- Show sample data
SELECT 'Sample Items:' as info;
SELECT sku, name, selling_price, is_menu_available FROM inventory.inventory_items LIMIT 10;



