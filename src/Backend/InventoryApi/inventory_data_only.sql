--
-- PostgreSQL database dump
--

\restrict PZYFgOuBTKjG7q1dGMUG6ubdf6wNDdChLVmqo0vutbT9Kpu6JibTGmpHQlQgSHj

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
-- Data for Name: vendors; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.vendors (vendor_id, name, contact_info, status, notes, created_at, updated_at, budget, reminder, reminder_enabled) FROM stdin;
9468fcb9-b035-41ad-a24a-307b1c83c334	CCU Chile	Distribuidora de cervezas	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
4512010e-447e-44d6-b167-638011e97f80	Coca Cola Andina	Bebidas gaseosas	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
92d164ad-eeb1-4d86-b641-87fa5e3b2114	Proveedor Local	Productos locales	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
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
-- PostgreSQL database dump complete
--

\unrestrict PZYFgOuBTKjG7q1dGMUG6ubdf6wNDdChLVmqo0vutbT9Kpu6JibTGmpHQlQgSHj

