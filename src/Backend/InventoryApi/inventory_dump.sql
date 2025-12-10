--
-- PostgreSQL database dump
--

\restrict kZczddwVC7khNaQocbVaZLRLMN7D6WLNLG0jZ4mAFE1HkC8NWheQIMYOOFCoyld

-- Dumped from database version 17.5
-- Dumped by pg_dump version 17.6

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: inventory; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA inventory;


--
-- Name: SCHEMA inventory; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA inventory IS 'Schema for inventory management';


--
-- Name: transaction_source; Type: TYPE; Schema: inventory; Owner: -
--

CREATE TYPE inventory.transaction_source AS ENUM (
    'purchase',
    'customer_order',
    'adjustment',
    'wastage',
    'transfer'
);


--
-- Name: TYPE transaction_source; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON TYPE inventory.transaction_source IS 'Enum for sources of inventory transactions';


--
-- Name: update_updated_at(); Type: FUNCTION; Schema: inventory; Owner: -
--

CREATE FUNCTION inventory.update_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$    
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
    $$;


--
-- Name: FUNCTION update_updated_at(); Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON FUNCTION inventory.update_updated_at() IS 'Updates the updated_at column on row updates';


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: _probe; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory._probe (
    x integer
);


--
-- Name: categories; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.categories (
    category_id uuid DEFAULT gen_random_uuid() NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    is_active boolean DEFAULT true,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now()
);


--
-- Name: inventory_categories; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.inventory_categories (
    category_id uuid DEFAULT gen_random_uuid() NOT NULL,
    name text NOT NULL,
    parent_id uuid,
    path text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT valid_name CHECK ((name <> ''::text))
);


--
-- Name: TABLE inventory_categories; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON TABLE inventory.inventory_categories IS 'Hierarchical categories for inventory items';


--
-- Name: COLUMN inventory_categories.path; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON COLUMN inventory.inventory_categories.path IS 'Stores category hierarchy path for efficient querying';


--
-- Name: inventory_items; Type: TABLE; Schema: inventory; Owner: -
--

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


--
-- Name: TABLE inventory_items; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON TABLE inventory.inventory_items IS 'Stores details of inventory items';


--
-- Name: inventory_stock; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.inventory_stock (
    item_id uuid NOT NULL,
    quantity_on_hand numeric(18,3) DEFAULT 0 NOT NULL,
    last_counted_at timestamp with time zone,
    CONSTRAINT inventory_stock_quantity_on_hand_check CHECK ((quantity_on_hand >= (0)::numeric))
);


--
-- Name: TABLE inventory_stock; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON TABLE inventory.inventory_stock IS 'Tracks current stock levels for each item';


--
-- Name: inventory_transactions; Type: TABLE; Schema: inventory; Owner: -
--

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


--
-- Name: TABLE inventory_transactions; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON TABLE inventory.inventory_transactions IS 'Records all inventory transactions';


--
-- Name: v_items_current; Type: VIEW; Schema: inventory; Owner: -
--

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


--
-- Name: VIEW v_items_current; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON VIEW inventory.v_items_current IS 'View of active inventory items with current stock levels';


--
-- Name: vendors; Type: TABLE; Schema: inventory; Owner: -
--

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


--
-- Name: TABLE vendors; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON TABLE inventory.vendors IS 'Stores information about vendors supplying inventory items';


--
-- Name: COLUMN vendors.status; Type: COMMENT; Schema: inventory; Owner: -
--

COMMENT ON COLUMN inventory.vendors.status IS 'Vendor status: active or inactive';


--
-- Data for Name: _probe; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory._probe (x) FROM stdin;
\.


--
-- Data for Name: categories; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.categories (category_id, name, description, is_active, created_at, updated_at) FROM stdin;
79369c6f-4f88-48df-8f95-ceae0121aab6	Beers	Cervezas nacionales e importadas	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
a11af6bd-c42f-43fe-8bb2-ace1b740616d	Soft Drinks	Bebidas gaseosas	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
cea8a622-e8ae-4734-aec2-527d97d4f794	Mixed Drinks	Bebidas mixtas y cócteles	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
4178e834-b41b-4092-9fc4-0d938ca79525	Fresh Drinks	Bebidas frescas naturales	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
fdf1fef8-9731-442a-894f-97f845f1928d	Appetizers	Entradas y aperitivos	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
5e66a062-7a02-4026-85e7-b200c44f2ba0	Craft Beers	Cervezas artesanales	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
\.


--
-- Data for Name: inventory_categories; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_categories (category_id, name, parent_id, path, created_at) FROM stdin;
\.


--
-- Data for Name: inventory_items; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: inventory_stock; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_stock (item_id, quantity_on_hand, last_counted_at) FROM stdin;
\.


--
-- Data for Name: inventory_transactions; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_transactions (transaction_id, item_id, delta, quantity_before, quantity_after, unit_cost, source, source_ref, user_id, notes, occurred_at, created_at) FROM stdin;
\.


--
-- Data for Name: vendors; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.vendors (vendor_id, name, contact_info, status, notes, created_at, updated_at, budget, reminder, reminder_enabled) FROM stdin;
9468fcb9-b035-41ad-a24a-307b1c83c334	CCU Chile	Distribuidora de cervezas	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
4512010e-447e-44d6-b167-638011e97f80	Coca Cola Andina	Bebidas gaseosas	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
92d164ad-eeb1-4d86-b641-87fa5e3b2114	Proveedor Local	Productos locales	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
\.


--
-- Name: categories categories_name_key; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.categories
    ADD CONSTRAINT categories_name_key UNIQUE (name);


--
-- Name: categories categories_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (category_id);


--
-- Name: inventory_categories inventory_categories_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_categories
    ADD CONSTRAINT inventory_categories_pkey PRIMARY KEY (category_id);


--
-- Name: inventory_items inventory_items_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_pkey PRIMARY KEY (item_id);


--
-- Name: inventory_items inventory_items_sku_key; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_sku_key UNIQUE (sku);


--
-- Name: inventory_stock inventory_stock_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_stock
    ADD CONSTRAINT inventory_stock_pkey PRIMARY KEY (item_id);


--
-- Name: inventory_transactions inventory_transactions_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_transactions
    ADD CONSTRAINT inventory_transactions_pkey PRIMARY KEY (transaction_id);


--
-- Name: vendors vendors_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.vendors
    ADD CONSTRAINT vendors_pkey PRIMARY KEY (vendor_id);


--
-- Name: idx_inventory_items_category; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_items_category ON inventory.inventory_items USING btree (category_id);


--
-- Name: idx_inventory_items_menu; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_items_menu ON inventory.inventory_items USING btree (is_menu_available);


--
-- Name: idx_inventory_items_vendor; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_items_vendor ON inventory.inventory_items USING btree (vendor_id);


--
-- Name: idx_inventory_tx_item_time; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_tx_item_time ON inventory.inventory_transactions USING btree (item_id, occurred_at);


--
-- Name: inventory_items update_items_updated_at; Type: TRIGGER; Schema: inventory; Owner: -
--

CREATE TRIGGER update_items_updated_at BEFORE UPDATE ON inventory.inventory_items FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at();


--
-- Name: vendors update_vendors_updated_at; Type: TRIGGER; Schema: inventory; Owner: -
--

CREATE TRIGGER update_vendors_updated_at BEFORE UPDATE ON inventory.vendors FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at();


--
-- Name: inventory_categories inventory_categories_parent_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_categories
    ADD CONSTRAINT inventory_categories_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES inventory.inventory_categories(category_id) ON DELETE SET NULL;


--
-- Name: inventory_items inventory_items_category_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_category_id_fkey FOREIGN KEY (category_id) REFERENCES inventory.inventory_categories(category_id) ON DELETE SET NULL;


--
-- Name: inventory_items inventory_items_vendor_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_vendor_id_fkey FOREIGN KEY (vendor_id) REFERENCES inventory.vendors(vendor_id) ON DELETE SET NULL;


--
-- Name: inventory_stock inventory_stock_item_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_stock
    ADD CONSTRAINT inventory_stock_item_id_fkey FOREIGN KEY (item_id) REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE;


--
-- Name: inventory_transactions inventory_transactions_item_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_transactions
    ADD CONSTRAINT inventory_transactions_item_id_fkey FOREIGN KEY (item_id) REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

\unrestrict kZczddwVC7khNaQocbVaZLRLMN7D6WLNLG0jZ4mAFE1HkC8NWheQIMYOOFCoyld

