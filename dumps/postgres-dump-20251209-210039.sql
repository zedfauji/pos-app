--
-- PostgreSQL database dump
--

\restrict IDefbRJmnGrMbFfC1yEOLStVffh0oaAiVLeRei2SahWhHBtdvy9BOE7SOuq9P69

-- Dumped from database version 17.7
-- Dumped by pg_dump version 17.6

-- Started on 2025-12-09 21:00:39

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
-- TOC entry 11 (class 2615 OID 127649)
-- Name: customers; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA customers;


--
-- TOC entry 9 (class 2615 OID 17302)
-- Name: inventory; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA inventory;


--
-- TOC entry 6 (class 2615 OID 16601)
-- Name: menu; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA menu;


--
-- TOC entry 7 (class 2615 OID 16718)
-- Name: ord; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA ord;


--
-- TOC entry 8 (class 2615 OID 17016)
-- Name: pay; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA pay;


--
-- TOC entry 12 (class 2615 OID 179291)
-- Name: settings; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA settings;


--
-- TOC entry 10 (class 2615 OID 84915)
-- Name: users; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA users;


--
-- TOC entry 1001 (class 1247 OID 17304)
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
-- TOC entry 279 (class 1255 OID 17315)
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
-- TOC entry 280 (class 1255 OID 17064)
-- Name: prevent_bill_ledger_updates(); Type: FUNCTION; Schema: pay; Owner: -
--

CREATE FUNCTION pay.prevent_bill_ledger_updates() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  RETURN NEW;
END;
$$;


--
-- TOC entry 298 (class 1255 OID 17066)
-- Name: prevent_payment_logs_updates(); Type: FUNCTION; Schema: pay; Owner: -
--

CREATE FUNCTION pay.prevent_payment_logs_updates() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  -- Prevent updates to log_id
  IF OLD.log_id != NEW.log_id THEN
    RAISE EXCEPTION 'Log ID is immutable and cannot be changed. Log ID: %', OLD.log_id;
  END IF;
  
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  -- Prevent updates to created_at
  IF OLD.created_at != NEW.created_at THEN
    RAISE EXCEPTION 'Created timestamp is immutable and cannot be changed. Created at: %', OLD.created_at;
  END IF;
  
  RETURN NEW;
END;
$$;


--
-- TOC entry 295 (class 1255 OID 17062)
-- Name: prevent_payment_updates(); Type: FUNCTION; Schema: pay; Owner: -
--

CREATE FUNCTION pay.prevent_payment_updates() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  -- Prevent updates to payment_id
  IF OLD.payment_id != NEW.payment_id THEN
    RAISE EXCEPTION 'Payment ID is immutable and cannot be changed. Payment ID: %', OLD.payment_id;
  END IF;
  
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  -- Prevent updates to created_at
  IF OLD.created_at != NEW.created_at THEN
    RAISE EXCEPTION 'Created timestamp is immutable and cannot be changed. Created at: %', OLD.created_at;
  END IF;
  
  RETURN NEW;
END;
$$;


--
-- TOC entry 297 (class 1255 OID 354503)
-- Name: prevent_refund_updates(); Type: FUNCTION; Schema: pay; Owner: -
--

CREATE FUNCTION pay.prevent_refund_updates() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
  -- Prevent updates to refund_id
  IF OLD.refund_id != NEW.refund_id THEN
    RAISE EXCEPTION 'Refund ID is immutable and cannot be changed. Refund ID: %', OLD.refund_id;
  END IF;
  
  -- Prevent updates to billing_id
  IF OLD.billing_id != NEW.billing_id THEN
    RAISE EXCEPTION 'Billing ID is immutable and cannot be changed. Billing ID: %', OLD.billing_id;
  END IF;
  
  -- Prevent updates to payment_id
  IF OLD.payment_id != NEW.payment_id THEN
    RAISE EXCEPTION 'Payment ID is immutable and cannot be changed. Payment ID: %', OLD.payment_id;
  END IF;
  
  -- Prevent updates to created_at
  IF OLD.created_at != NEW.created_at THEN
    RAISE EXCEPTION 'Created timestamp is immutable and cannot be changed. Created at: %', OLD.created_at;
  END IF;
  
  RETURN NEW;
END;
$$;


--
-- TOC entry 296 (class 1255 OID 277165)
-- Name: ensure_single_default_floor(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.ensure_single_default_floor() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
        BEGIN
            IF NEW.is_default = true THEN
                UPDATE public.floors SET is_default = false WHERE floor_id != NEW.floor_id;
            END IF;
            RETURN NEW;
        END;
        $$;


--
-- TOC entry 293 (class 1255 OID 277161)
-- Name: update_floors_updated_at(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_floors_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
        BEGIN
            NEW.updated_at = now();
            RETURN NEW;
        END;
        $$;


--
-- TOC entry 294 (class 1255 OID 277163)
-- Name: update_tables_updated_at(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.update_tables_updated_at() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
        BEGIN
            NEW.updated_at = now();
            RETURN NEW;
        END;
        $$;


--
-- TOC entry 281 (class 1255 OID 179328)
-- Name: update_updated_at_column(); Type: FUNCTION; Schema: settings; Owner: -
--

CREATE FUNCTION settings.update_updated_at_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 265 (class 1259 OID 127669)
-- Name: customers; Type: TABLE; Schema: customers; Owner: -
--

CREATE TABLE customers.customers (
    customer_id uuid NOT NULL,
    first_name character varying(100) NOT NULL,
    last_name character varying(100) NOT NULL,
    phone character varying(20),
    email character varying(255),
    date_of_birth date,
    photo_url text,
    membership_level_id uuid,
    membership_start_date timestamp with time zone,
    membership_expiry_date timestamp with time zone,
    total_spent numeric(10,2) DEFAULT 0,
    total_visits integer DEFAULT 0,
    loyalty_points integer DEFAULT 0,
    is_active boolean DEFAULT true,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    notes text
);


--
-- TOC entry 268 (class 1259 OID 127718)
-- Name: loyalty_transactions; Type: TABLE; Schema: customers; Owner: -
--

CREATE TABLE customers.loyalty_transactions (
    transaction_id uuid NOT NULL,
    customer_id uuid,
    transaction_type integer NOT NULL,
    points integer NOT NULL,
    description text NOT NULL,
    order_id uuid,
    order_amount numeric(10,2),
    expiry_date timestamp with time zone,
    related_transaction_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    created_by character varying(100)
);


--
-- TOC entry 264 (class 1259 OID 127650)
-- Name: membership_levels; Type: TABLE; Schema: customers; Owner: -
--

CREATE TABLE customers.membership_levels (
    membership_level_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    description text,
    discount_percentage numeric(5,2) DEFAULT 0,
    loyalty_multiplier numeric(5,2) DEFAULT 1.0,
    minimum_spend_requirement numeric(10,2),
    validity_months integer,
    color_hex character varying(7),
    icon character varying(50),
    sort_order integer DEFAULT 0,
    is_active boolean DEFAULT true,
    is_default boolean DEFAULT false,
    max_wallet_balance numeric(10,2),
    free_delivery boolean DEFAULT false,
    priority_support boolean DEFAULT false,
    birthday_bonus_points integer DEFAULT 0,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- TOC entry 267 (class 1259 OID 127705)
-- Name: wallet_transactions; Type: TABLE; Schema: customers; Owner: -
--

CREATE TABLE customers.wallet_transactions (
    transaction_id uuid NOT NULL,
    wallet_id uuid,
    transaction_type integer NOT NULL,
    amount numeric(10,2) NOT NULL,
    balance_after numeric(10,2) NOT NULL,
    description text NOT NULL,
    reference_id character varying(100),
    order_id uuid,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    created_by character varying(100)
);


--
-- TOC entry 266 (class 1259 OID 127687)
-- Name: wallets; Type: TABLE; Schema: customers; Owner: -
--

CREATE TABLE customers.wallets (
    wallet_id uuid NOT NULL,
    customer_id uuid,
    balance numeric(10,2) DEFAULT 0,
    total_loaded numeric(10,2) DEFAULT 0,
    total_spent numeric(10,2) DEFAULT 0,
    last_transaction_date timestamp with time zone,
    is_active boolean DEFAULT true,
    created_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


--
-- TOC entry 263 (class 1259 OID 98361)
-- Name: cash_flow; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.cash_flow (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    employee_name text NOT NULL,
    date timestamp with time zone DEFAULT now() NOT NULL,
    cash_amount numeric(18,2) NOT NULL,
    notes text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 252 (class 1259 OID 17316)
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
-- TOC entry 253 (class 1259 OID 17325)
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
-- TOC entry 255 (class 1259 OID 17346)
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
-- TOC entry 256 (class 1259 OID 17360)
-- Name: inventory_stock; Type: TABLE; Schema: inventory; Owner: -
--

CREATE TABLE inventory.inventory_stock (
    item_id uuid NOT NULL,
    quantity_on_hand numeric(18,3) DEFAULT 0 NOT NULL,
    last_counted_at timestamp with time zone,
    CONSTRAINT inventory_stock_quantity_on_hand_check CHECK ((quantity_on_hand >= (0)::numeric))
);


--
-- TOC entry 257 (class 1259 OID 17365)
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
-- TOC entry 258 (class 1259 OID 17376)
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
-- TOC entry 254 (class 1259 OID 17333)
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
-- TOC entry 238 (class 1259 OID 16689)
-- Name: combo_items; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.combo_items (
    combo_id bigint NOT NULL,
    menu_item_id bigint NOT NULL,
    quantity integer DEFAULT 1 NOT NULL,
    is_required boolean DEFAULT true NOT NULL
);


--
-- TOC entry 237 (class 1259 OID 16675)
-- Name: combos; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.combos (
    combo_id bigint NOT NULL,
    name text NOT NULL,
    description text,
    price numeric(12,2) NOT NULL,
    is_discountable boolean DEFAULT true NOT NULL,
    is_available boolean DEFAULT true NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    picture_url text,
    created_by text,
    updated_by text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 236 (class 1259 OID 16674)
-- Name: combos_combo_id_seq; Type: SEQUENCE; Schema: menu; Owner: -
--

CREATE SEQUENCE menu.combos_combo_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4592 (class 0 OID 0)
-- Dependencies: 236
-- Name: combos_combo_id_seq; Type: SEQUENCE OWNED BY; Schema: menu; Owner: -
--

ALTER SEQUENCE menu.combos_combo_id_seq OWNED BY menu.combos.combo_id;


--
-- TOC entry 240 (class 1259 OID 16707)
-- Name: menu_history; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.menu_history (
    history_id bigint NOT NULL,
    entity_type text NOT NULL,
    entity_id bigint NOT NULL,
    action text NOT NULL,
    old_value jsonb,
    new_value jsonb,
    version integer,
    changed_by text,
    changed_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 239 (class 1259 OID 16706)
-- Name: menu_history_history_id_seq; Type: SEQUENCE; Schema: menu; Owner: -
--

CREATE SEQUENCE menu.menu_history_history_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4593 (class 0 OID 0)
-- Dependencies: 239
-- Name: menu_history_history_id_seq; Type: SEQUENCE OWNED BY; Schema: menu; Owner: -
--

ALTER SEQUENCE menu.menu_history_history_id_seq OWNED BY menu.menu_history.history_id;


--
-- TOC entry 235 (class 1259 OID 16657)
-- Name: menu_item_modifiers; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.menu_item_modifiers (
    menu_item_id bigint NOT NULL,
    modifier_id bigint NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    is_optional boolean DEFAULT true NOT NULL
);


--
-- TOC entry 230 (class 1259 OID 16603)
-- Name: menu_items; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.menu_items (
    menu_item_id bigint NOT NULL,
    sku_id text NOT NULL,
    name text NOT NULL,
    description text,
    category text NOT NULL,
    group_name text,
    vendor_price numeric(12,2) DEFAULT 0.00 NOT NULL,
    selling_price numeric(12,2) NOT NULL,
    price numeric(12,2),
    picture_url text,
    is_discountable boolean DEFAULT true NOT NULL,
    is_part_of_combo boolean DEFAULT false NOT NULL,
    is_available boolean DEFAULT true NOT NULL,
    version integer DEFAULT 1 NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    created_by text,
    updated_by text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 229 (class 1259 OID 16602)
-- Name: menu_items_menu_item_id_seq; Type: SEQUENCE; Schema: menu; Owner: -
--

CREATE SEQUENCE menu.menu_items_menu_item_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4594 (class 0 OID 0)
-- Dependencies: 229
-- Name: menu_items_menu_item_id_seq; Type: SEQUENCE OWNED BY; Schema: menu; Owner: -
--

ALTER SEQUENCE menu.menu_items_menu_item_id_seq OWNED BY menu.menu_items.menu_item_id;


--
-- TOC entry 234 (class 1259 OID 16638)
-- Name: modifier_options; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.modifier_options (
    option_id bigint NOT NULL,
    modifier_id bigint NOT NULL,
    name text NOT NULL,
    price_delta numeric(12,2) DEFAULT 0.00 NOT NULL,
    is_available boolean DEFAULT true NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 233 (class 1259 OID 16637)
-- Name: modifier_options_option_id_seq; Type: SEQUENCE; Schema: menu; Owner: -
--

CREATE SEQUENCE menu.modifier_options_option_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4595 (class 0 OID 0)
-- Dependencies: 233
-- Name: modifier_options_option_id_seq; Type: SEQUENCE OWNED BY; Schema: menu; Owner: -
--

ALTER SEQUENCE menu.modifier_options_option_id_seq OWNED BY menu.modifier_options.option_id;


--
-- TOC entry 232 (class 1259 OID 16624)
-- Name: modifiers; Type: TABLE; Schema: menu; Owner: -
--

CREATE TABLE menu.modifiers (
    modifier_id bigint NOT NULL,
    name text NOT NULL,
    description text,
    is_required boolean DEFAULT false NOT NULL,
    allow_multiple boolean DEFAULT false NOT NULL,
    min_selections integer DEFAULT 0 NOT NULL,
    max_selections integer,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 231 (class 1259 OID 16623)
-- Name: modifiers_modifier_id_seq; Type: SEQUENCE; Schema: menu; Owner: -
--

CREATE SEQUENCE menu.modifiers_modifier_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4596 (class 0 OID 0)
-- Dependencies: 231
-- Name: modifiers_modifier_id_seq; Type: SEQUENCE OWNED BY; Schema: menu; Owner: -
--

ALTER SEQUENCE menu.modifiers_modifier_id_seq OWNED BY menu.modifiers.modifier_id;


--
-- TOC entry 244 (class 1259 OID 16741)
-- Name: order_items; Type: TABLE; Schema: ord; Owner: -
--

CREATE TABLE ord.order_items (
    order_item_id bigint NOT NULL,
    order_id bigint NOT NULL,
    menu_item_id bigint,
    combo_id bigint,
    quantity integer DEFAULT 1 NOT NULL,
    base_price numeric(12,2) NOT NULL,
    vendor_price numeric(12,2) DEFAULT 0.00 NOT NULL,
    price_delta numeric(12,2) DEFAULT 0.00 NOT NULL,
    line_discount numeric(12,2) DEFAULT 0.00 NOT NULL,
    line_total numeric(12,2) DEFAULT 0.00 NOT NULL,
    profit numeric(12,2) DEFAULT 0.00 NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    notes text,
    snapshot_name text,
    snapshot_sku text,
    snapshot_category text,
    snapshot_group text,
    snapshot_version integer,
    snapshot_picture_url text,
    selected_modifiers jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    delivered_quantity integer DEFAULT 0 NOT NULL
);


--
-- TOC entry 243 (class 1259 OID 16740)
-- Name: order_items_order_item_id_seq; Type: SEQUENCE; Schema: ord; Owner: -
--

CREATE SEQUENCE ord.order_items_order_item_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4597 (class 0 OID 0)
-- Dependencies: 243
-- Name: order_items_order_item_id_seq; Type: SEQUENCE OWNED BY; Schema: ord; Owner: -
--

ALTER SEQUENCE ord.order_items_order_item_id_seq OWNED BY ord.order_items.order_item_id;


--
-- TOC entry 246 (class 1259 OID 16765)
-- Name: order_logs; Type: TABLE; Schema: ord; Owner: -
--

CREATE TABLE ord.order_logs (
    log_id bigint NOT NULL,
    order_id bigint NOT NULL,
    action text NOT NULL,
    old_value jsonb,
    new_value jsonb,
    server_id text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 245 (class 1259 OID 16764)
-- Name: order_logs_log_id_seq; Type: SEQUENCE; Schema: ord; Owner: -
--

CREATE SEQUENCE ord.order_logs_log_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4598 (class 0 OID 0)
-- Dependencies: 245
-- Name: order_logs_log_id_seq; Type: SEQUENCE OWNED BY; Schema: ord; Owner: -
--

ALTER SEQUENCE ord.order_logs_log_id_seq OWNED BY ord.order_logs.log_id;


--
-- TOC entry 242 (class 1259 OID 16720)
-- Name: orders; Type: TABLE; Schema: ord; Owner: -
--

CREATE TABLE ord.orders (
    order_id bigint NOT NULL,
    session_id uuid NOT NULL,
    billing_id uuid,
    table_id text NOT NULL,
    server_id text NOT NULL,
    server_name text,
    status text DEFAULT 'open'::text NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL,
    subtotal numeric(12,2) DEFAULT 0.00 NOT NULL,
    discount_total numeric(12,2) DEFAULT 0.00 NOT NULL,
    tax_total numeric(12,2) DEFAULT 0.00 NOT NULL,
    total numeric(12,2) DEFAULT 0.00 NOT NULL,
    profit_total numeric(12,2) DEFAULT 0.00 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    closed_at timestamp with time zone,
    delivery_status text DEFAULT 'pending'::text NOT NULL,
    CONSTRAINT orders_delivery_status_check CHECK ((delivery_status = ANY (ARRAY['pending'::text, 'partial'::text, 'delivered'::text]))),
    CONSTRAINT orders_status_check CHECK ((status = ANY (ARRAY['open'::text, 'waiting'::text, 'delivered'::text, 'closed'::text])))
);


--
-- TOC entry 241 (class 1259 OID 16719)
-- Name: orders_order_id_seq; Type: SEQUENCE; Schema: ord; Owner: -
--

CREATE SEQUENCE ord.orders_order_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4599 (class 0 OID 0)
-- Dependencies: 241
-- Name: orders_order_id_seq; Type: SEQUENCE OWNED BY; Schema: ord; Owner: -
--

ALTER SEQUENCE ord.orders_order_id_seq OWNED BY ord.orders.order_id;


--
-- TOC entry 249 (class 1259 OID 17034)
-- Name: bill_ledger; Type: TABLE; Schema: pay; Owner: -
--

CREATE TABLE pay.bill_ledger (
    billing_id uuid NOT NULL,
    session_id uuid NOT NULL,
    total_due numeric(12,2) DEFAULT 0.00 NOT NULL,
    total_discount numeric(12,2) DEFAULT 0.00 NOT NULL,
    total_paid numeric(12,2) DEFAULT 0.00 NOT NULL,
    total_tip numeric(12,2) DEFAULT 0.00 NOT NULL,
    status text DEFAULT 'unpaid'::text NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 251 (class 1259 OID 17049)
-- Name: payment_logs; Type: TABLE; Schema: pay; Owner: -
--

CREATE TABLE pay.payment_logs (
    log_id bigint NOT NULL,
    billing_id text NOT NULL,
    session_id text NOT NULL,
    action text NOT NULL,
    old_value jsonb,
    new_value jsonb,
    server_id text,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 250 (class 1259 OID 17048)
-- Name: payment_logs_log_id_seq; Type: SEQUENCE; Schema: pay; Owner: -
--

CREATE SEQUENCE pay.payment_logs_log_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4600 (class 0 OID 0)
-- Dependencies: 250
-- Name: payment_logs_log_id_seq; Type: SEQUENCE OWNED BY; Schema: pay; Owner: -
--

ALTER SEQUENCE pay.payment_logs_log_id_seq OWNED BY pay.payment_logs.log_id;


--
-- TOC entry 248 (class 1259 OID 17018)
-- Name: payments; Type: TABLE; Schema: pay; Owner: -
--

CREATE TABLE pay.payments (
    payment_id bigint NOT NULL,
    session_id uuid NOT NULL,
    billing_id uuid NOT NULL,
    amount_paid numeric(12,2) NOT NULL,
    currency text DEFAULT 'USD'::text NOT NULL,
    payment_method text NOT NULL,
    discount_amount numeric(12,2) DEFAULT 0.00 NOT NULL,
    discount_reason text,
    tip_amount numeric(12,2) DEFAULT 0.00 NOT NULL,
    external_ref text,
    meta jsonb,
    created_by text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT payments_amount_paid_check CHECK ((amount_paid >= (0)::numeric)),
    CONSTRAINT payments_discount_amount_check CHECK ((discount_amount >= (0)::numeric)),
    CONSTRAINT payments_tip_amount_check CHECK ((tip_amount >= (0)::numeric))
);


--
-- TOC entry 247 (class 1259 OID 17017)
-- Name: payments_payment_id_seq; Type: SEQUENCE; Schema: pay; Owner: -
--

CREATE SEQUENCE pay.payments_payment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4601 (class 0 OID 0)
-- Dependencies: 247
-- Name: payments_payment_id_seq; Type: SEQUENCE OWNED BY; Schema: pay; Owner: -
--

ALTER SEQUENCE pay.payments_payment_id_seq OWNED BY pay.payments.payment_id;


--
-- TOC entry 278 (class 1259 OID 354486)
-- Name: refunds; Type: TABLE; Schema: pay; Owner: -
--

CREATE TABLE pay.refunds (
    refund_id uuid DEFAULT gen_random_uuid() NOT NULL,
    payment_id uuid NOT NULL,
    billing_id uuid NOT NULL,
    session_id uuid NOT NULL,
    refund_amount numeric(12,2) NOT NULL,
    refund_reason text,
    refund_method text DEFAULT 'original'::text NOT NULL,
    external_ref text,
    meta jsonb,
    created_by text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT refunds_immutable_billing_id CHECK (((billing_id IS NOT NULL) AND (billing_id <> '00000000-0000-0000-0000-000000000000'::uuid))),
    CONSTRAINT refunds_immutable_created_at CHECK ((created_at IS NOT NULL)),
    CONSTRAINT refunds_immutable_refund_id CHECK ((refund_id IS NOT NULL)),
    CONSTRAINT refunds_refund_amount_check CHECK ((refund_amount > (0)::numeric))
);


--
-- TOC entry 227 (class 1259 OID 16588)
-- Name: app_settings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.app_settings (
    key text NOT NULL,
    value text NOT NULL
);


--
-- TOC entry 270 (class 1259 OID 165232)
-- Name: billing_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.billing_sessions (
    billing_id uuid NOT NULL,
    session_id uuid NOT NULL,
    created_at timestamp with time zone DEFAULT now()
);


--
-- TOC entry 269 (class 1259 OID 165216)
-- Name: billings; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.billings (
    billing_id uuid DEFAULT gen_random_uuid() NOT NULL,
    customer_name text,
    customer_contact text,
    total_amount numeric(10,2) DEFAULT 0.00,
    subtotal numeric(10,2) DEFAULT 0.00,
    tax_amount numeric(10,2) DEFAULT 0.00,
    discount_amount numeric(10,2) DEFAULT 0.00,
    status text DEFAULT 'open'::text,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now(),
    closed_at timestamp with time zone,
    paid_at timestamp with time zone,
    CONSTRAINT billings_status_check CHECK ((status = ANY (ARRAY['open'::text, 'closed'::text, 'paid'::text, 'cancelled'::text])))
);


--
-- TOC entry 226 (class 1259 OID 16575)
-- Name: bills; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.bills (
    bill_id uuid NOT NULL,
    table_label text NOT NULL,
    server_id text NOT NULL,
    server_name text NOT NULL,
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone NOT NULL,
    total_time_minutes integer NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    items jsonb DEFAULT '[]'::jsonb NOT NULL,
    time_cost numeric DEFAULT 0 NOT NULL,
    items_cost numeric DEFAULT 0 NOT NULL,
    total_amount numeric DEFAULT 0 NOT NULL,
    billing_id uuid,
    session_id uuid,
    status text DEFAULT 'awaiting_payment'::text NOT NULL,
    closed_at timestamp with time zone,
    is_settled boolean DEFAULT false NOT NULL,
    payment_state text DEFAULT 'not-paid'::text NOT NULL
);


--
-- TOC entry 275 (class 1259 OID 277096)
-- Name: floors; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.floors (
    floor_id uuid DEFAULT gen_random_uuid() NOT NULL,
    floor_name text NOT NULL,
    description text,
    is_default boolean DEFAULT false NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    display_order integer DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 277 (class 1259 OID 277144)
-- Name: table_layout_history; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.table_layout_history (
    history_id uuid DEFAULT gen_random_uuid() NOT NULL,
    floor_id uuid NOT NULL,
    version_number integer NOT NULL,
    layout_data jsonb NOT NULL,
    created_by text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    description text
);


--
-- TOC entry 228 (class 1259 OID 16595)
-- Name: table_session_moves; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.table_session_moves (
    session_id uuid NOT NULL,
    from_label text NOT NULL,
    to_label text NOT NULL,
    moved_at timestamp without time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 225 (class 1259 OID 16566)
-- Name: table_sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.table_sessions (
    session_id uuid NOT NULL,
    table_label text NOT NULL,
    server_id text NOT NULL,
    server_name text NOT NULL,
    start_time timestamp with time zone NOT NULL,
    end_time timestamp with time zone,
    status text DEFAULT 'active'::text NOT NULL,
    items jsonb DEFAULT '[]'::jsonb NOT NULL,
    billing_id uuid,
    last_heartbeat timestamp with time zone,
    destination_table_id text,
    moved_at timestamp with time zone,
    original_table_id text,
    CONSTRAINT table_sessions_status_check CHECK ((status = ANY (ARRAY['active'::text, 'closed'::text, 'moved'::text, 'transferred'::text])))
);


--
-- TOC entry 224 (class 1259 OID 16558)
-- Name: table_status; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.table_status (
    label text NOT NULL,
    type text NOT NULL,
    occupied boolean NOT NULL,
    order_id text,
    start_time timestamp with time zone,
    server text,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 276 (class 1259 OID 277113)
-- Name: tables; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.tables (
    table_id uuid DEFAULT gen_random_uuid() NOT NULL,
    floor_id uuid NOT NULL,
    table_name text NOT NULL,
    table_number text,
    table_type text DEFAULT 'billiard'::text NOT NULL,
    x_position numeric(10,2) DEFAULT 0 NOT NULL,
    y_position numeric(10,2) DEFAULT 0 NOT NULL,
    rotation numeric(5,2) DEFAULT 0 NOT NULL,
    size text DEFAULT 'M'::text NOT NULL,
    width numeric(10,2),
    height numeric(10,2),
    status text DEFAULT 'available'::text NOT NULL,
    billing_rate numeric(10,2) DEFAULT 0 NOT NULL,
    auto_start_timer boolean DEFAULT false NOT NULL,
    icon_style text,
    grouping_tags text[],
    is_active boolean DEFAULT true NOT NULL,
    is_locked boolean DEFAULT false NOT NULL,
    order_id text,
    start_time timestamp with time zone,
    server text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- TOC entry 272 (class 1259 OID 179293)
-- Name: hierarchical_settings; Type: TABLE; Schema: settings; Owner: -
--

CREATE TABLE settings.hierarchical_settings (
    id bigint NOT NULL,
    host_key character varying(100) DEFAULT 'default'::character varying NOT NULL,
    category character varying(50) NOT NULL,
    settings_json jsonb NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    created_by character varying(100),
    updated_by character varying(100)
);


--
-- TOC entry 271 (class 1259 OID 179292)
-- Name: hierarchical_settings_id_seq; Type: SEQUENCE; Schema: settings; Owner: -
--

CREATE SEQUENCE settings.hierarchical_settings_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4602 (class 0 OID 0)
-- Dependencies: 271
-- Name: hierarchical_settings_id_seq; Type: SEQUENCE OWNED BY; Schema: settings; Owner: -
--

ALTER SEQUENCE settings.hierarchical_settings_id_seq OWNED BY settings.hierarchical_settings.id;


--
-- TOC entry 274 (class 1259 OID 179308)
-- Name: settings_audit; Type: TABLE; Schema: settings; Owner: -
--

CREATE TABLE settings.settings_audit (
    id bigint NOT NULL,
    host_key character varying(100) DEFAULT 'default'::character varying NOT NULL,
    action character varying(50) NOT NULL,
    description text,
    category character varying(50),
    changes_json jsonb,
    changed_by character varying(100),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    ip_address inet,
    user_agent text
);


--
-- TOC entry 273 (class 1259 OID 179307)
-- Name: settings_audit_id_seq; Type: SEQUENCE; Schema: settings; Owner: -
--

CREATE SEQUENCE settings.settings_audit_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- TOC entry 4603 (class 0 OID 0)
-- Dependencies: 273
-- Name: settings_audit_id_seq; Type: SEQUENCE OWNED BY; Schema: settings; Owner: -
--

ALTER SEQUENCE settings.settings_audit_id_seq OWNED BY settings.settings_audit.id;


--
-- TOC entry 262 (class 1259 OID 89838)
-- Name: role_inheritance; Type: TABLE; Schema: users; Owner: -
--

CREATE TABLE users.role_inheritance (
    child_role_id character varying(50) NOT NULL,
    parent_role_id character varying(50) NOT NULL,
    CONSTRAINT role_inheritance_check CHECK (((child_role_id)::text <> (parent_role_id)::text))
);


--
-- TOC entry 261 (class 1259 OID 89826)
-- Name: role_permissions; Type: TABLE; Schema: users; Owner: -
--

CREATE TABLE users.role_permissions (
    role_id character varying(50) NOT NULL,
    permission character varying(100) NOT NULL
);


--
-- TOC entry 260 (class 1259 OID 89809)
-- Name: roles; Type: TABLE; Schema: users; Owner: -
--

CREATE TABLE users.roles (
    role_id character varying(50) NOT NULL,
    name character varying(50) NOT NULL,
    description text,
    is_system_role boolean DEFAULT false NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL
);


--
-- TOC entry 259 (class 1259 OID 84916)
-- Name: users; Type: TABLE; Schema: users; Owner: -
--

CREATE TABLE users.users (
    user_id character varying(50) NOT NULL,
    username character varying(50) NOT NULL,
    password_hash character varying(255) NOT NULL,
    role character varying(20) DEFAULT 'employee'::character varying NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    is_deleted boolean DEFAULT false NOT NULL
);


--
-- TOC entry 4020 (class 2604 OID 16678)
-- Name: combos combo_id; Type: DEFAULT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.combos ALTER COLUMN combo_id SET DEFAULT nextval('menu.combos_combo_id_seq'::regclass);


--
-- TOC entry 4029 (class 2604 OID 16710)
-- Name: menu_history history_id; Type: DEFAULT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_history ALTER COLUMN history_id SET DEFAULT nextval('menu.menu_history_history_id_seq'::regclass);


--
-- TOC entry 3997 (class 2604 OID 16606)
-- Name: menu_items menu_item_id; Type: DEFAULT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_items ALTER COLUMN menu_item_id SET DEFAULT nextval('menu.menu_items_menu_item_id_seq'::regclass);


--
-- TOC entry 4012 (class 2604 OID 16641)
-- Name: modifier_options option_id; Type: DEFAULT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.modifier_options ALTER COLUMN option_id SET DEFAULT nextval('menu.modifier_options_option_id_seq'::regclass);


--
-- TOC entry 4006 (class 2604 OID 16627)
-- Name: modifiers modifier_id; Type: DEFAULT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.modifiers ALTER COLUMN modifier_id SET DEFAULT nextval('menu.modifiers_modifier_id_seq'::regclass);


--
-- TOC entry 4042 (class 2604 OID 16744)
-- Name: order_items order_item_id; Type: DEFAULT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.order_items ALTER COLUMN order_item_id SET DEFAULT nextval('ord.order_items_order_item_id_seq'::regclass);


--
-- TOC entry 4053 (class 2604 OID 16768)
-- Name: order_logs log_id; Type: DEFAULT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.order_logs ALTER COLUMN log_id SET DEFAULT nextval('ord.order_logs_log_id_seq'::regclass);


--
-- TOC entry 4031 (class 2604 OID 16723)
-- Name: orders order_id; Type: DEFAULT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.orders ALTER COLUMN order_id SET DEFAULT nextval('ord.orders_order_id_seq'::regclass);


--
-- TOC entry 4066 (class 2604 OID 17052)
-- Name: payment_logs log_id; Type: DEFAULT; Schema: pay; Owner: -
--

ALTER TABLE ONLY pay.payment_logs ALTER COLUMN log_id SET DEFAULT nextval('pay.payment_logs_log_id_seq'::regclass);


--
-- TOC entry 4055 (class 2604 OID 17021)
-- Name: payments payment_id; Type: DEFAULT; Schema: pay; Owner: -
--

ALTER TABLE ONLY pay.payments ALTER COLUMN payment_id SET DEFAULT nextval('pay.payments_payment_id_seq'::regclass);


--
-- TOC entry 4137 (class 2604 OID 179296)
-- Name: hierarchical_settings id; Type: DEFAULT; Schema: settings; Owner: -
--

ALTER TABLE ONLY settings.hierarchical_settings ALTER COLUMN id SET DEFAULT nextval('settings.hierarchical_settings_id_seq'::regclass);


--
-- TOC entry 4142 (class 2604 OID 179311)
-- Name: settings_audit id; Type: DEFAULT; Schema: settings; Owner: -
--

ALTER TABLE ONLY settings.settings_audit ALTER COLUMN id SET DEFAULT nextval('settings.settings_audit_id_seq'::regclass);


--
-- TOC entry 4573 (class 0 OID 127669)
-- Dependencies: 265
-- Data for Name: customers; Type: TABLE DATA; Schema: customers; Owner: -
--

COPY customers.customers (customer_id, first_name, last_name, phone, email, date_of_birth, photo_url, membership_level_id, membership_start_date, membership_expiry_date, total_spent, total_visits, loyalty_points, is_active, created_at, updated_at, notes) FROM stdin;
0eb8655c-8b9c-4976-93bb-12b48a268e2d	Preeti	Gupta	3324940009	sawdustpaper@gmail.com	\N	\N	11111111-1111-1111-1111-111111111111	2025-09-15 15:02:57.197057+00	\N	0.00	0	0	t	2025-09-15 15:02:57.197057+00	2025-09-15 15:02:57.197057+00	123123123123123
\.


--
-- TOC entry 4576 (class 0 OID 127718)
-- Dependencies: 268
-- Data for Name: loyalty_transactions; Type: TABLE DATA; Schema: customers; Owner: -
--

COPY customers.loyalty_transactions (transaction_id, customer_id, transaction_type, points, description, order_id, order_amount, expiry_date, related_transaction_id, created_at, updated_at, created_by) FROM stdin;
\.


--
-- TOC entry 4572 (class 0 OID 127650)
-- Dependencies: 264
-- Data for Name: membership_levels; Type: TABLE DATA; Schema: customers; Owner: -
--

COPY customers.membership_levels (membership_level_id, name, description, discount_percentage, loyalty_multiplier, minimum_spend_requirement, validity_months, color_hex, icon, sort_order, is_active, is_default, max_wallet_balance, free_delivery, priority_support, birthday_bonus_points, created_at, updated_at) FROM stdin;
11111111-1111-1111-1111-111111111111	Regular	Standard membership with basic benefits	0.00	1.00	\N	\N	#808080	Person	1	t	t	1000.00	f	f	0	2025-09-15 14:43:03.064227+00	2025-09-15 14:43:03.064227+00
22222222-2222-2222-2222-222222222222	Silver	Silver membership with 5% discount and enhanced benefits	5.00	1.25	500.00	12	#C0C0C0	Star	2	t	f	2500.00	f	f	100	2025-09-15 14:43:03.064227+00	2025-09-15 14:43:03.064227+00
33333333-3333-3333-3333-333333333333	Gold	Gold membership with 10% discount and premium benefits	10.00	1.50	1500.00	12	#FFD700	Crown	3	t	f	5000.00	t	f	250	2025-09-15 14:43:03.064227+00	2025-09-15 14:43:03.064227+00
44444444-4444-4444-4444-444444444444	VIP	VIP membership with 15% discount and exclusive benefits	15.00	2.00	5000.00	24	#8B008B	Diamond	4	t	f	10000.00	t	t	500	2025-09-15 14:43:03.064227+00	2025-09-15 14:43:03.064227+00
\.


--
-- TOC entry 4575 (class 0 OID 127705)
-- Dependencies: 267
-- Data for Name: wallet_transactions; Type: TABLE DATA; Schema: customers; Owner: -
--

COPY customers.wallet_transactions (transaction_id, wallet_id, transaction_type, amount, balance_after, description, reference_id, order_id, created_at, created_by) FROM stdin;
\.


--
-- TOC entry 4574 (class 0 OID 127687)
-- Dependencies: 266
-- Data for Name: wallets; Type: TABLE DATA; Schema: customers; Owner: -
--

COPY customers.wallets (wallet_id, customer_id, balance, total_loaded, total_spent, last_transaction_date, is_active, created_at, updated_at) FROM stdin;
e68b2f9c-8a65-4488-822f-63104a1e44d9	0eb8655c-8b9c-4976-93bb-12b48a268e2d	0.00	0.00	0.00	\N	t	2025-09-15 15:02:57.418371+00	2025-09-15 15:02:57.418371+00
\.


--
-- TOC entry 4571 (class 0 OID 98361)
-- Dependencies: 263
-- Data for Name: cash_flow; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.cash_flow (id, employee_name, date, cash_amount, notes, created_at, updated_at) FROM stdin;
\.


--
-- TOC entry 4561 (class 0 OID 17316)
-- Dependencies: 252
-- Data for Name: categories; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.categories (category_id, name, description, is_active, created_at, updated_at) FROM stdin;
79369c6f-4f88-48df-8f95-ceae0121aab6	Beers	Cervezas nacionales e importadas	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
a11af6bd-c42f-43fe-8bb2-ace1b740616d	Soft Drinks	Bebidas gaseosas	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
cea8a622-e8ae-4734-aec2-527d97d4f794	Mixed Drinks	Bebidas mixtas y cÃ³cteles	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
4178e834-b41b-4092-9fc4-0d938ca79525	Fresh Drinks	Bebidas frescas naturales	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
fdf1fef8-9731-442a-894f-97f845f1928d	Appetizers	Entradas y aperitivos	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
5e66a062-7a02-4026-85e7-b200c44f2ba0	Craft Beers	Cervezas artesanales	t	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00
\.


--
-- TOC entry 4562 (class 0 OID 17325)
-- Dependencies: 253
-- Data for Name: inventory_categories; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_categories (category_id, name, parent_id, path, created_at) FROM stdin;
\.


--
-- TOC entry 4564 (class 0 OID 17346)
-- Dependencies: 255
-- Data for Name: inventory_items; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_items (item_id, vendor_id, category_id, sku, name, description, unit, barcode, reorder_threshold, buying_price, selling_price, tax_rate, is_menu_available, is_active, created_at, updated_at) FROM stdin;
3b949796-b8ee-40ec-91e8-009102fdeef3	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-CORONA	Corona	Cerveza Lager Mexicana	unit	\N	20.000	2500.00	3500.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
ace4b34a-2cbc-4123-b5b4-01b7c3c4cec4	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-PACIFICO	Pacifico	Cerveza Lager Clara	unit	\N	20.000	2500.00	3500.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
1702878e-18c9-4789-a57e-c20882f17447	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-VICTORIA	Victoria	Cerveza Lager Oscura	unit	\N	20.000	2500.00	3500.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
2de5201c-151a-4151-b5ef-4e6add5ae5e4	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-INDIO	Indio	Cerveza Lager Oscura	unit	\N	20.000	2500.00	3500.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
ad9a83e6-eb39-46a2-a15a-addf0bbe1b3e	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-TKT-LIGHT	Tecate Light	Cerveza Lager Ligera	unit	\N	20.000	2200.00	3000.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
1fb37002-441a-4aae-acae-3e82e73ec85d	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-TKT-ROJA	Tecate Roja	Cerveza Lager Roja	unit	\N	20.000	2200.00	3000.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
1f76e86a-0d9d-49b1-8625-216213ebcbc4	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-AMSTEL	Amstel	Cerveza Lager Holandesa	unit	\N	20.000	2800.00	3800.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
909647c3-8ec9-4c0c-b94a-f8d7407122eb	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-HEINEKEN	Heineken	Cerveza Lager Holandesa	unit	\N	20.000	2800.00	3800.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
dd101cab-d0b3-4c45-938b-559a7d12e199	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-HEINEKEN0	Heineken 0.0	Cerveza sin alcohol	unit	\N	15.000	2500.00	3500.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
946e5322-b8bd-4c52-a6db-3ceda4b9b760	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-CRISTAL	Cristal	Cerveza Lager Chilena	unit	\N	25.000	2000.00	3000.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
ca8b2b1c-ae85-4ca1-8d13-163b5b23279d	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-ESCUDO	Escudo	Cerveza Lager Chilena	unit	\N	25.000	2000.00	3000.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
7ceae0e0-b7f1-4373-a5d4-b8bbbc3b2ca2	9468fcb9-b035-41ad-a24a-307b1c83c334	79369c6f-4f88-48df-8f95-ceae0121aab6	BEER-AUSTRAL	Austral	Cerveza Lager Chilena	unit	\N	20.000	2200.00	3200.00	19.00	t	t	2025-09-12 03:09:26.244008+00	2025-09-12 03:09:26.244008+00
483d068b-5cea-4e15-b69a-aa48787a5648	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-COCA-COLA	Coca Cola	Bebida gaseosa cola	unit	\N	30.000	800.00	1500.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
51035350-86be-4868-9d46-0e818f3d5712	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-SPRITE	Sprite	Bebida gaseosa limÃ³n-lima	unit	\N	30.000	800.00	1500.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
e8b010ac-3983-4bc0-bf05-ad5b0d36ec6a	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-FANTA	Fanta	Bebida gaseosa naranja	unit	\N	30.000	800.00	1500.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
81c3f913-4aa3-416f-ae86-4a3f12a4f78d	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-COCA-ZERO	Coca Cola Zero	Bebida gaseosa cola sin azÃºcar	unit	\N	25.000	900.00	1500.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
4eb223df-50fd-43f2-879d-c3f4ad995788	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-COCA-LIGHT	Coca Cola Light	Bebida gaseosa cola light	unit	\N	25.000	900.00	1500.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
c3dd9218-0007-40b3-9221-84e808be5441	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-SCHWEPPES	Schweppes	Bebida gaseosa tÃ³nica	unit	\N	20.000	1000.00	1800.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
9bfa652d-f1a5-4828-b0db-5e175a9d7385	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-GINGER-ALE	Ginger Ale	Bebida gaseosa jengibre	unit	\N	20.000	1000.00	1800.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
4a289ae3-9bd8-46de-b92f-e4ec940e5dda	4512010e-447e-44d6-b167-638011e97f80	a11af6bd-c42f-43fe-8bb2-ace1b740616d	SODA-AGUA-TONICA	Agua TÃ³nica	Bebida gaseosa tÃ³nica premium	unit	\N	15.000	1200.00	2000.00	19.00	t	t	2025-09-12 03:09:26.296467+00	2025-09-12 03:09:26.296467+00
339a962f-a55a-4945-b31f-d6e478feb9d8	92d164ad-eeb1-4d86-b641-87fa5e3b2114	cea8a622-e8ae-4734-aec2-527d97d4f794	COCKTAIL-MOJITO	Mojito	Ron blanco, menta, lima, soda	unit	\N	10.000	2000.00	4500.00	19.00	t	t	2025-09-12 03:09:26.351511+00	2025-09-12 03:09:26.351511+00
66d3c8e9-7359-49e9-a17f-9b14c597520a	92d164ad-eeb1-4d86-b641-87fa5e3b2114	cea8a622-e8ae-4734-aec2-527d97d4f794	COCKTAIL-CUBA-LIBRE	Cuba Libre	Ron blanco, Coca Cola, lima	unit	\N	10.000	1800.00	4000.00	19.00	t	t	2025-09-12 03:09:26.351511+00	2025-09-12 03:09:26.351511+00
a0b446f7-35d9-497e-842b-750b3b15e240	92d164ad-eeb1-4d86-b641-87fa5e3b2114	cea8a622-e8ae-4734-aec2-527d97d4f794	COCKTAIL-PINA-COLADA	PiÃ±a Colada	Ron blanco, crema de coco, piÃ±a	unit	\N	8.000	2500.00	5000.00	19.00	t	t	2025-09-12 03:09:26.351511+00	2025-09-12 03:09:26.351511+00
73324a84-83c2-4c6a-af0a-f2c1cdeef139	92d164ad-eeb1-4d86-b641-87fa5e3b2114	cea8a622-e8ae-4734-aec2-527d97d4f794	COCKTAIL-MARGARITA	Margarita	Tequila, triple sec, lima	unit	\N	8.000	2200.00	4800.00	19.00	t	t	2025-09-12 03:09:26.351511+00	2025-09-12 03:09:26.351511+00
6b7ff79a-0545-4060-ad03-f05a9cd3967d	92d164ad-eeb1-4d86-b641-87fa5e3b2114	cea8a622-e8ae-4734-aec2-527d97d4f794	COCKTAIL-BLOODY-MARY	Bloody Mary	Vodka, jugo de tomate, especias	unit	\N	6.000	2000.00	4500.00	19.00	t	t	2025-09-12 03:09:26.351511+00	2025-09-12 03:09:26.351511+00
6ea10f93-fed7-42c8-98bb-15d27cd019f5	92d164ad-eeb1-4d86-b641-87fa5e3b2114	cea8a622-e8ae-4734-aec2-527d97d4f794	COCKTAIL-COSMOPOLITAN	Cosmopolitan	Vodka, triple sec, arÃ¡ndanos	unit	\N	6.000	2500.00	5200.00	19.00	t	t	2025-09-12 03:09:26.351511+00	2025-09-12 03:09:26.351511+00
a9bcd41a-0d34-4cf6-b3a7-728b3fa9a2cf	92d164ad-eeb1-4d86-b641-87fa5e3b2114	4178e834-b41b-4092-9fc4-0d938ca79525	FRESH-LIMONADA	Limonada Natural	Limones frescos, agua, azÃºcar	unit	\N	15.000	500.00	2000.00	19.00	t	t	2025-09-12 03:09:26.407312+00	2025-09-12 03:09:26.407312+00
1f96a349-f006-429f-8265-aa5f71817657	92d164ad-eeb1-4d86-b641-87fa5e3b2114	4178e834-b41b-4092-9fc4-0d938ca79525	FRESH-NARANJADA	Naranjada Natural	Naranjas frescas, agua, azÃºcar	unit	\N	15.000	500.00	2000.00	19.00	t	t	2025-09-12 03:09:26.407312+00	2025-09-12 03:09:26.407312+00
3a4d47af-077c-478c-8453-37a93dbeeecc	92d164ad-eeb1-4d86-b641-87fa5e3b2114	4178e834-b41b-4092-9fc4-0d938ca79525	FRESH-JUGO-MANZANA	Jugo de Manzana	Manzanas frescas, agua	unit	\N	12.000	600.00	2200.00	19.00	t	t	2025-09-12 03:09:26.407312+00	2025-09-12 03:09:26.407312+00
4a6754f9-175f-4f78-bd86-c8d82aed3d92	92d164ad-eeb1-4d86-b641-87fa5e3b2114	4178e834-b41b-4092-9fc4-0d938ca79525	FRESH-AGUA-COCO	Agua de Coco	Coco fresco, agua	unit	\N	10.000	800.00	2500.00	19.00	t	t	2025-09-12 03:09:26.407312+00	2025-09-12 03:09:26.407312+00
dd61c72d-4559-4b9d-8e78-4936943b734e	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-NACHOS	Nachos con Queso	Tortillas de maÃ­z, queso fundido	portion	\N	20.000	1500.00	3500.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
d02827c5-bb93-44bc-9ec9-75b9154faa06	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-WINGS	Alitas de Pollo	Alitas marinadas, salsa picante	portion	\N	15.000	2000.00	4500.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
efb772b3-b786-4e4b-be27-ef04d5643df0	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-CHEESE-STICKS	Palitos de Queso	Queso empanizado, salsa marinara	portion	\N	12.000	1200.00	3000.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
51f6a7f7-1ef1-4bd5-8184-53d6e716e785	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-BUFFALO-WINGS	Alitas Buffalo	Alitas con salsa buffalo	portion	\N	15.000	2000.00	4500.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
cdd7bd83-438c-48eb-88a2-8219c7655ea0	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-SHRIMP-COCKTAIL	CÃ³ctel de Camarones	Camarones cocidos, salsa cÃ³ctel	portion	\N	8.000	3000.00	6000.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
ed86e379-ae3f-47c2-8f35-ef876aa7a461	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-GUACAMOLE	Guacamole	Aguacate, tomate, cebolla, cilantro	portion	\N	10.000	800.00	2500.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
e477c92d-794a-4922-a21b-78eaeedfdba6	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-SALSA-CHIPS	Chips y Salsa	Tortillas fritas, salsa picante	portion	\N	20.000	500.00	1800.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
640c01a7-3122-44a1-9d9e-8d62f5cd70f0	92d164ad-eeb1-4d86-b641-87fa5e3b2114	fdf1fef8-9731-442a-894f-97f845f1928d	APPETIZER-BEER-NUTS	ManÃ­ Cervecero	ManÃ­ tostado con especias	portion	\N	25.000	300.00	1200.00	19.00	t	t	2025-09-12 03:09:26.45926+00	2025-09-12 03:09:26.45926+00
a8bed029-50ef-49ad-bb3d-bf59d377c7bc	92d164ad-eeb1-4d86-b641-87fa5e3b2114	5e66a062-7a02-4026-85e7-b200c44f2ba0	CRAFT-IPA-LOCAL	IPA Local	Cerveza artesanal IPA	unit	\N	8.000	3000.00	5500.00	19.00	t	t	2025-09-12 03:09:26.511235+00	2025-09-12 03:09:26.511235+00
2e5f4091-78d6-4d84-ae0d-1997783ae6f3	92d164ad-eeb1-4d86-b641-87fa5e3b2114	5e66a062-7a02-4026-85e7-b200c44f2ba0	CRAFT-STOUT-LOCAL	Stout Local	Cerveza artesanal Stout	unit	\N	8.000	3000.00	5500.00	19.00	t	t	2025-09-12 03:09:26.511235+00	2025-09-12 03:09:26.511235+00
bd04e24b-a73c-411d-aa7d-2b834263d9b1	92d164ad-eeb1-4d86-b641-87fa5e3b2114	5e66a062-7a02-4026-85e7-b200c44f2ba0	CRAFT-PALE-ALE	Pale Ale	Cerveza artesanal Pale Ale	unit	\N	8.000	3000.00	5500.00	19.00	t	t	2025-09-12 03:09:26.511235+00	2025-09-12 03:09:26.511235+00
e36d02a3-4c9c-4839-83c4-41e2a0ee04ec	92d164ad-eeb1-4d86-b641-87fa5e3b2114	5e66a062-7a02-4026-85e7-b200c44f2ba0	CRAFT-WHEAT-BEER	Wheat Beer	Cerveza artesanal de trigo	unit	\N	8.000	3000.00	5500.00	19.00	t	t	2025-09-12 03:09:26.511235+00	2025-09-12 03:09:26.511235+00
\.


--
-- TOC entry 4565 (class 0 OID 17360)
-- Dependencies: 256
-- Data for Name: inventory_stock; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_stock (item_id, quantity_on_hand, last_counted_at) FROM stdin;
3b949796-b8ee-40ec-91e8-009102fdeef3	100.000	2025-09-12 03:09:26.559722+00
ace4b34a-2cbc-4123-b5b4-01b7c3c4cec4	100.000	2025-09-12 03:09:26.559722+00
1702878e-18c9-4789-a57e-c20882f17447	100.000	2025-09-12 03:09:26.559722+00
2de5201c-151a-4151-b5ef-4e6add5ae5e4	100.000	2025-09-12 03:09:26.559722+00
ad9a83e6-eb39-46a2-a15a-addf0bbe1b3e	100.000	2025-09-12 03:09:26.559722+00
1fb37002-441a-4aae-acae-3e82e73ec85d	100.000	2025-09-12 03:09:26.559722+00
1f76e86a-0d9d-49b1-8625-216213ebcbc4	100.000	2025-09-12 03:09:26.559722+00
909647c3-8ec9-4c0c-b94a-f8d7407122eb	100.000	2025-09-12 03:09:26.559722+00
dd101cab-d0b3-4c45-938b-559a7d12e199	100.000	2025-09-12 03:09:26.559722+00
946e5322-b8bd-4c52-a6db-3ceda4b9b760	100.000	2025-09-12 03:09:26.559722+00
ca8b2b1c-ae85-4ca1-8d13-163b5b23279d	100.000	2025-09-12 03:09:26.559722+00
7ceae0e0-b7f1-4373-a5d4-b8bbbc3b2ca2	100.000	2025-09-12 03:09:26.559722+00
483d068b-5cea-4e15-b69a-aa48787a5648	100.000	2025-09-12 03:09:26.559722+00
51035350-86be-4868-9d46-0e818f3d5712	100.000	2025-09-12 03:09:26.559722+00
e8b010ac-3983-4bc0-bf05-ad5b0d36ec6a	100.000	2025-09-12 03:09:26.559722+00
81c3f913-4aa3-416f-ae86-4a3f12a4f78d	100.000	2025-09-12 03:09:26.559722+00
4eb223df-50fd-43f2-879d-c3f4ad995788	100.000	2025-09-12 03:09:26.559722+00
c3dd9218-0007-40b3-9221-84e808be5441	100.000	2025-09-12 03:09:26.559722+00
9bfa652d-f1a5-4828-b0db-5e175a9d7385	100.000	2025-09-12 03:09:26.559722+00
4a289ae3-9bd8-46de-b92f-e4ec940e5dda	100.000	2025-09-12 03:09:26.559722+00
339a962f-a55a-4945-b31f-d6e478feb9d8	100.000	2025-09-12 03:09:26.559722+00
66d3c8e9-7359-49e9-a17f-9b14c597520a	100.000	2025-09-12 03:09:26.559722+00
a0b446f7-35d9-497e-842b-750b3b15e240	100.000	2025-09-12 03:09:26.559722+00
73324a84-83c2-4c6a-af0a-f2c1cdeef139	100.000	2025-09-12 03:09:26.559722+00
6b7ff79a-0545-4060-ad03-f05a9cd3967d	100.000	2025-09-12 03:09:26.559722+00
6ea10f93-fed7-42c8-98bb-15d27cd019f5	100.000	2025-09-12 03:09:26.559722+00
a9bcd41a-0d34-4cf6-b3a7-728b3fa9a2cf	100.000	2025-09-12 03:09:26.559722+00
1f96a349-f006-429f-8265-aa5f71817657	100.000	2025-09-12 03:09:26.559722+00
3a4d47af-077c-478c-8453-37a93dbeeecc	100.000	2025-09-12 03:09:26.559722+00
4a6754f9-175f-4f78-bd86-c8d82aed3d92	100.000	2025-09-12 03:09:26.559722+00
dd61c72d-4559-4b9d-8e78-4936943b734e	100.000	2025-09-12 03:09:26.559722+00
d02827c5-bb93-44bc-9ec9-75b9154faa06	100.000	2025-09-12 03:09:26.559722+00
efb772b3-b786-4e4b-be27-ef04d5643df0	100.000	2025-09-12 03:09:26.559722+00
51f6a7f7-1ef1-4bd5-8184-53d6e716e785	100.000	2025-09-12 03:09:26.559722+00
cdd7bd83-438c-48eb-88a2-8219c7655ea0	100.000	2025-09-12 03:09:26.559722+00
ed86e379-ae3f-47c2-8f35-ef876aa7a461	100.000	2025-09-12 03:09:26.559722+00
e477c92d-794a-4922-a21b-78eaeedfdba6	100.000	2025-09-12 03:09:26.559722+00
640c01a7-3122-44a1-9d9e-8d62f5cd70f0	100.000	2025-09-12 03:09:26.559722+00
a8bed029-50ef-49ad-bb3d-bf59d377c7bc	100.000	2025-09-12 03:09:26.559722+00
2e5f4091-78d6-4d84-ae0d-1997783ae6f3	100.000	2025-09-12 03:09:26.559722+00
bd04e24b-a73c-411d-aa7d-2b834263d9b1	100.000	2025-09-12 03:09:26.559722+00
e36d02a3-4c9c-4839-83c4-41e2a0ee04ec	100.000	2025-09-12 03:09:26.559722+00
\.


--
-- TOC entry 4566 (class 0 OID 17365)
-- Dependencies: 257
-- Data for Name: inventory_transactions; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.inventory_transactions (transaction_id, item_id, delta, quantity_before, quantity_after, unit_cost, source, source_ref, user_id, notes, occurred_at, created_at) FROM stdin;
36cca81c-9807-43de-8ef0-7d4871d8c287	3b949796-b8ee-40ec-91e8-009102fdeef3	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
0edc88b0-e10c-4d34-8b85-39b22da85d52	ace4b34a-2cbc-4123-b5b4-01b7c3c4cec4	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
45e90084-6f51-4f93-8184-8acdabbbbee4	1702878e-18c9-4789-a57e-c20882f17447	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
0b155a4e-0ab0-48a8-b2bd-6af63b3c7b8d	2de5201c-151a-4151-b5ef-4e6add5ae5e4	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
a29999a8-1476-48b5-95c7-9d14f9947806	ad9a83e6-eb39-46a2-a15a-addf0bbe1b3e	100.000	0.000	100.000	2200.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
174cf6e4-14fc-44f6-b750-313c5beec0a7	1fb37002-441a-4aae-acae-3e82e73ec85d	100.000	0.000	100.000	2200.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
a55ca269-96f3-42c3-b821-d21b621fe46f	1f76e86a-0d9d-49b1-8625-216213ebcbc4	100.000	0.000	100.000	2800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
74130b3d-b8ca-49c6-9603-fdb185de71ed	909647c3-8ec9-4c0c-b94a-f8d7407122eb	100.000	0.000	100.000	2800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
2b44766c-7831-4314-8d68-13735b172b5a	dd101cab-d0b3-4c45-938b-559a7d12e199	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
f96fbee4-efb7-4c3b-89a1-87d26b33c12a	946e5322-b8bd-4c52-a6db-3ceda4b9b760	100.000	0.000	100.000	2000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
2378d034-98ee-4d71-8c7c-e645f278ffd5	ca8b2b1c-ae85-4ca1-8d13-163b5b23279d	100.000	0.000	100.000	2000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
3fd6d4ed-53e5-4f62-8fe4-6bb0af779607	7ceae0e0-b7f1-4373-a5d4-b8bbbc3b2ca2	100.000	0.000	100.000	2200.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
54aade49-fb8d-47be-a112-2192d7e7fd04	483d068b-5cea-4e15-b69a-aa48787a5648	100.000	0.000	100.000	800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
d7474823-74bb-4459-8e8c-321f4c41efd5	51035350-86be-4868-9d46-0e818f3d5712	100.000	0.000	100.000	800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
139c2367-46dc-43f9-81d3-f604cb7c4baf	e8b010ac-3983-4bc0-bf05-ad5b0d36ec6a	100.000	0.000	100.000	800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
e1f1971e-15c5-4c9b-9a86-cefb9f4e4bd6	81c3f913-4aa3-416f-ae86-4a3f12a4f78d	100.000	0.000	100.000	900.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
0941b192-3bff-4bbf-93ac-5a1d5781122d	4eb223df-50fd-43f2-879d-c3f4ad995788	100.000	0.000	100.000	900.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
80d04d07-5fde-49f7-b3b6-8065eb938585	c3dd9218-0007-40b3-9221-84e808be5441	100.000	0.000	100.000	1000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
9c6d67a5-94cf-475d-8e3f-8c35a78d5f40	9bfa652d-f1a5-4828-b0db-5e175a9d7385	100.000	0.000	100.000	1000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
186ab503-ad14-4965-810f-0fe961b53347	4a289ae3-9bd8-46de-b92f-e4ec940e5dda	100.000	0.000	100.000	1200.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
bd9bb89e-3b04-4eec-a582-0a7c201c4aa2	339a962f-a55a-4945-b31f-d6e478feb9d8	100.000	0.000	100.000	2000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
4ba374f2-da08-476b-94b9-777546a7fd6e	66d3c8e9-7359-49e9-a17f-9b14c597520a	100.000	0.000	100.000	1800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
8251b86d-5def-42c1-a40d-c5a8032d128a	a0b446f7-35d9-497e-842b-750b3b15e240	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
2d92527b-8c42-4d74-9d9e-a8d03b313d4d	73324a84-83c2-4c6a-af0a-f2c1cdeef139	100.000	0.000	100.000	2200.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
f388b4dd-ef0b-4c7b-af68-f1da548d88f7	6b7ff79a-0545-4060-ad03-f05a9cd3967d	100.000	0.000	100.000	2000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
17dd879b-4459-44a3-b82f-f208ff154d5b	6ea10f93-fed7-42c8-98bb-15d27cd019f5	100.000	0.000	100.000	2500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
756651f5-ed8d-43f3-b21a-8e4721a6b5d2	a9bcd41a-0d34-4cf6-b3a7-728b3fa9a2cf	100.000	0.000	100.000	500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
069ccc12-bed9-414c-8dd0-a17b109a5c85	1f96a349-f006-429f-8265-aa5f71817657	100.000	0.000	100.000	500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
d7cd4b7f-2294-4c7f-9667-0ec211bc58a8	3a4d47af-077c-478c-8453-37a93dbeeecc	100.000	0.000	100.000	600.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
35ff3b2d-e729-4d8c-85ae-3c897705fe41	4a6754f9-175f-4f78-bd86-c8d82aed3d92	100.000	0.000	100.000	800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
f94cd166-352c-4cff-bead-7dee2a7b1460	dd61c72d-4559-4b9d-8e78-4936943b734e	100.000	0.000	100.000	1500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
8999b2ff-3cb6-4c4b-9cf1-a7e0cdee9285	d02827c5-bb93-44bc-9ec9-75b9154faa06	100.000	0.000	100.000	2000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
74f1d1bd-850e-4492-8083-d80874119be1	efb772b3-b786-4e4b-be27-ef04d5643df0	100.000	0.000	100.000	1200.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
8fda83c5-6138-495d-8dab-f73d1f7653c9	51f6a7f7-1ef1-4bd5-8184-53d6e716e785	100.000	0.000	100.000	2000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
d4898111-4f08-404e-8a1f-bf28fbdcdae2	cdd7bd83-438c-48eb-88a2-8219c7655ea0	100.000	0.000	100.000	3000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
8c5e761d-fe90-4a29-accc-80245094fd6a	ed86e379-ae3f-47c2-8f35-ef876aa7a461	100.000	0.000	100.000	800.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
13bacaa9-e6fa-46d5-a0b3-4fa2b32e33ee	e477c92d-794a-4922-a21b-78eaeedfdba6	100.000	0.000	100.000	500.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
052536ce-32f3-4459-80dd-a8d76a940306	640c01a7-3122-44a1-9d9e-8d62f5cd70f0	100.000	0.000	100.000	300.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
fec690d9-4279-4959-bdbd-bc65e16c83ed	a8bed029-50ef-49ad-bb3d-bf59d377c7bc	100.000	0.000	100.000	3000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
cc1249f9-6019-455a-b4e0-57bba0acb8a9	2e5f4091-78d6-4d84-ae0d-1997783ae6f3	100.000	0.000	100.000	3000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
347aa440-641f-456d-b661-71914bf140dc	bd04e24b-a73c-411d-aa7d-2b834263d9b1	100.000	0.000	100.000	3000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
bed4cedd-b6f0-4273-b735-412e3b334bc2	e36d02a3-4c9c-4839-83c4-41e2a0ee04ec	100.000	0.000	100.000	3000.00	purchase	INITIAL_STOCK	system	Initial stock setup	2025-09-12 03:09:26.615478+00	2025-09-12 03:09:26.615478+00
\.


--
-- TOC entry 4563 (class 0 OID 17333)
-- Dependencies: 254
-- Data for Name: vendors; Type: TABLE DATA; Schema: inventory; Owner: -
--

COPY inventory.vendors (vendor_id, name, contact_info, status, notes, created_at, updated_at, budget, reminder, reminder_enabled) FROM stdin;
9468fcb9-b035-41ad-a24a-307b1c83c334	CCU Chile	Distribuidora de cervezas	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
4512010e-447e-44d6-b167-638011e97f80	Coca Cola Andina	Bebidas gaseosas	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
92d164ad-eeb1-4d86-b641-87fa5e3b2114	Proveedor Local	Productos locales	active	\N	2025-09-12 02:37:37.902267+00	2025-09-12 02:37:37.902267+00	0.00	\N	f
\.


--
-- TOC entry 4547 (class 0 OID 16689)
-- Dependencies: 238
-- Data for Name: combo_items; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.combo_items (combo_id, menu_item_id, quantity, is_required) FROM stdin;
1	2	1	t
1	3	1	t
1	1	1	t
\.


--
-- TOC entry 4546 (class 0 OID 16675)
-- Dependencies: 237
-- Data for Name: combos; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.combos (combo_id, name, description, price, is_discountable, is_available, version, is_deleted, picture_url, created_by, updated_by, created_at, updated_at) FROM stdin;
1	Burger Combo	Burger + Fries + Coffee	11.99	t	t	1	f		\N	\N	2025-09-09 00:02:44.934792+00	2025-09-09 00:02:44.934792+00
\.


--
-- TOC entry 4549 (class 0 OID 16707)
-- Dependencies: 240
-- Data for Name: menu_history; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.menu_history (history_id, entity_type, entity_id, action, old_value, new_value, version, changed_by, changed_at) FROM stdin;
1	menu_item	9	update	{"Item": {"Id": 9, "Sku": "APPETIZER-BUFFALO-WINGS", "Name": "Alitas Buffalo", "Price": null, "Version": 1, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "SellingPrice": 4500.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": "Alitas Buffalo", "Price": 4500, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "VendorPrice": null, "SellingPrice": 4500, "IsPartOfCombo": false, "IsDiscountable": true}	2	system	2025-09-15 00:18:32.571851+00
2	menu_item	9	update	{"Item": {"Id": 9, "Sku": "APPETIZER-BUFFALO-WINGS", "Name": "Alitas Buffalo", "Price": 4500.00, "Version": 2, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "SellingPrice": 4500.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": "Alitas Buffalo", "Price": 4500, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "VendorPrice": null, "SellingPrice": 4500, "IsPartOfCombo": false, "IsDiscountable": true}	3	system	2025-09-15 00:19:06.580563+00
3	menu_item	9	update	{"Item": {"Id": 9, "Sku": "APPETIZER-BUFFALO-WINGS", "Name": "Alitas Buffalo", "Price": 4500.00, "Version": 3, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "SellingPrice": 4500.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": "Alitas Buffalo", "Price": 4500, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "VendorPrice": null, "SellingPrice": 4500, "IsPartOfCombo": false, "IsDiscountable": true}	4	system	2025-09-15 00:19:34.950785+00
4	menu_item	9	update	{"Item": {"Id": 9, "Sku": "APPETIZER-BUFFALO-WINGS", "Name": "Alitas Buffalo", "Price": 15000.00, "Version": 4, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "SellingPrice": 15000.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": "Alitas Buffalo", "Price": 15000, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "VendorPrice": null, "SellingPrice": 15000, "IsPartOfCombo": false, "IsDiscountable": true}	5	system	2025-09-15 00:28:23.62382+00
5	menu_item	9	update	{"Item": {"Id": 9, "Sku": "APPETIZER-BUFFALO-WINGS", "Name": "Alitas Buffalo", "Price": 15000.00, "Version": 5, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "SellingPrice": 15000.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": "Alitas Buffalo", "Price": 15000, "Category": "Appetizers", "GroupName": "Wings", "PictureUrl": "https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center", "Description": "Alitas con salsa buffalo", "IsAvailable": true, "VendorPrice": null, "SellingPrice": 15000, "IsPartOfCombo": false, "IsDiscountable": true}	6	system	2025-09-15 00:28:34.675783+00
6	menu_item	8	update	{"Item": {"Id": 8, "Sku": "APPETIZER-SHRIMP-COCKTAIL", "Name": "Cóctel de Camarones", "Price": null, "Version": 1, "Category": "Appetizers", "GroupName": "Seafood", "PictureUrl": "https://images.unsplash.com/photo-1559847844-5315695dadae?w=400&h=300&fit=crop&crop=center", "Description": "Camarones cocidos, salsa cóctel", "IsAvailable": true, "SellingPrice": 6000.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": null, "Price": null, "Category": null, "GroupName": null, "PictureUrl": null, "Description": null, "IsAvailable": false, "VendorPrice": null, "SellingPrice": null, "IsPartOfCombo": null, "IsDiscountable": null}	2	system	2025-09-15 15:06:28.179515+00
7	menu_item	8	update	{"Item": {"Id": 8, "Sku": "APPETIZER-SHRIMP-COCKTAIL", "Name": "Cóctel de Camarones", "Price": null, "Version": 2, "Category": "Appetizers", "GroupName": "Seafood", "PictureUrl": "https://images.unsplash.com/photo-1559847844-5315695dadae?w=400&h=300&fit=crop&crop=center", "Description": "Camarones cocidos, salsa cóctel", "IsAvailable": false, "SellingPrice": 6000.00, "IsPartOfCombo": false, "IsDiscountable": true}, "Modifiers": []}	{"Name": null, "Price": null, "Category": null, "GroupName": null, "PictureUrl": null, "Description": null, "IsAvailable": true, "VendorPrice": null, "SellingPrice": null, "IsPartOfCombo": null, "IsDiscountable": null}	3	system	2025-09-15 15:06:31.220965+00
\.


--
-- TOC entry 4544 (class 0 OID 16657)
-- Dependencies: 235
-- Data for Name: menu_item_modifiers; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.menu_item_modifiers (menu_item_id, modifier_id, sort_order, is_optional) FROM stdin;
1	1	1	f
\.


--
-- TOC entry 4539 (class 0 OID 16603)
-- Dependencies: 230
-- Data for Name: menu_items; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.menu_items (menu_item_id, sku_id, name, description, category, group_name, vendor_price, selling_price, price, picture_url, is_discountable, is_part_of_combo, is_available, version, is_deleted, created_by, updated_by, created_at, updated_at) FROM stdin;
12	APPETIZER-WINGS	Alitas de Pollo	Alitas marinadas, salsa picante	Appetizers	Wings	0.00	4500.00	\N	https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
14	APPETIZER-SALSA-CHIPS	Chips y Salsa	Tortillas fritas, salsa picante	Appetizers	Snacks	0.00	1800.00	\N	https://images.unsplash.com/photo-1551024506-0bccd828d307?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
8	APPETIZER-SHRIMP-COCKTAIL	Cóctel de Camarones	Camarones cocidos, salsa cóctel	Appetizers	Seafood	0.00	6000.00	\N	https://images.unsplash.com/photo-1559847844-5315695dadae?w=400&h=300&fit=crop&crop=center	t	f	t	3	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-15 15:06:31.220965+00
15	APPETIZER-GUACAMOLE	Guacamole	Aguacate, tomate, cebolla, cilantro	Appetizers	Dips	0.00	2500.00	\N	https://images.unsplash.com/photo-1551782450-17144efb9c50?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
11	APPETIZER-NACHOS	Nachos con Queso	Tortillas de maíz, queso fundido	Appetizers	Nachos	0.00	3500.00	\N	https://images.unsplash.com/photo-1571091718767-18b5b1457add?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
17	FRESH-AGUA-COCO	Agua de Coco	Coco fresco, agua	Beverages	Fresh Juices	0.00	2500.00	\N	https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
31	SODA-AGUA-TONICA	Agua Tónica	Bebida gaseosa tónica premium	Beverages	Soft Drinks	0.00	2000.00	\N	https://images.unsplash.com/photo-1544145945-f90425340c7e?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
35	BEER-AMSTEL	Amstel	Cerveza Lager Holandesa	Beverages	Beer	0.00	3800.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
24	COCKTAIL-BLOODY-MARY	Bloody Mary	Vodka, jugo de tomate, especias	Beverages	Cocktails	0.00	4500.00	\N	https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
28	SODA-COCA-LIGHT	Coca Cola Light	Bebida gaseosa cola light	Beverages	Soft Drinks	0.00	1500.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
32	SODA-COCA-ZERO	Coca Cola Zero	Bebida gaseosa cola sin azúcar	Beverages	Soft Drinks	0.00	1500.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
38	BEER-CORONA	Corona	Cerveza Lager Mexicana	Beverages	Beer	0.00	3500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
44	BEER-CRISTAL	Cristal	Cerveza Lager Chilena	Beverages	Beer	0.00	3000.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
20	COCKTAIL-CUBA-LIBRE	Cuba Libre	Ron blanco, Coca Cola, lima	Beverages	Cocktails	0.00	4000.00	\N	https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
33	SODA-FANTA	Fanta	Bebida gaseosa naranja	Beverages	Soft Drinks	0.00	1500.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
34	BEER-HEINEKEN	Heineken	Cerveza Lager Holandesa	Beverages	Beer	0.00	3800.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
45	BEER-HEINEKEN0	Heineken 0.0	Cerveza sin alcohol	Beverages	Beer	0.00	3500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
4	CRAFT-IPA-LOCAL	IPA Local	Cerveza artesanal IPA	Beverages	Craft Beer	0.00	5500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
19	FRESH-LIMONADA	Limonada Natural	Limones frescos, agua, azúcar	Beverages	Fresh Juices	0.00	2000.00	\N	https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
23	COCKTAIL-MARGARITA	Margarita	Tequila, triple sec, lima	Beverages	Cocktails	0.00	4800.00	\N	https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
16	FRESH-NARANJADA	Naranjada Natural	Naranjas frescas, agua, azúcar	Beverages	Fresh Juices	0.00	2000.00	\N	https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
41	BEER-PACIFICO	Pacifico	Cerveza Lager Clara	Beverages	Beer	0.00	3500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
22	COCKTAIL-PINA-COLADA	Piña Colada	Ron blanco, crema de coco, piña	Beverages	Cocktails	0.00	5000.00	\N	https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
29	SODA-SCHWEPPES	Schweppes	Bebida gaseosa tónica	Beverages	Soft Drinks	0.00	1800.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
7	CRAFT-STOUT-LOCAL	Stout Local	Cerveza artesanal Stout	Beverages	Craft Beer	0.00	5500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
36	BEER-TKT-ROJA	Tecate Roja	Cerveza Lager Roja	Beverages	Beer	0.00	3000.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
40	BEER-VICTORIA	Victoria	Cerveza Lager Oscura	Beverages	Beer	0.00	3500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
2	ENT-BUR-001	Classic Burger	Beef patty with lettuce & tomato	Entrees	Burgers	2.00	7.99	\N	https://images.unsplash.com/photo-1568901346375-23c9450c58cd?w=400&h=300&fit=crop&crop=center	t	t	t	1	f	\N	\N	2025-09-09 00:02:44.904385+00	2025-09-09 00:02:44.904385+00
3	SID-FRY-001	French Fries	Crispy fries	Sides	Fries	0.40	2.99	\N	https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400&h=300&fit=crop&crop=center	t	t	t	1	f	\N	\N	2025-09-09 00:02:44.906572+00	2025-09-09 00:02:44.906572+00
13	APPETIZER-BEER-NUTS	Maní Cervecero	Maní tostado con especias	Appetizers	Snacks	0.00	1200.00	\N	https://images.unsplash.com/photo-1578662996442-48f60103fc96?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
10	APPETIZER-CHEESE-STICKS	Palitos de Queso	Queso empanizado, salsa marinara	Appetizers	Cheese	0.00	3000.00	\N	https://images.unsplash.com/photo-1563379091339-03246963d4d1?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
42	BEER-AUSTRAL	Austral	Cerveza Lager Chilena	Beverages	Beer	0.00	3200.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
27	SODA-COCA-COLA	Coca Cola	Bebida gaseosa cola	Beverages	Soft Drinks	0.00	1500.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
1	BEV-COF-001	Coffee	Hot brewed coffee	Beverages	Hot Drinks	0.50	2.50	\N	https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=400&h=300&fit=crop&crop=center	t	t	t	1	f	\N	\N	2025-09-09 00:02:44.900891+00	2025-09-09 00:02:44.900891+00
25	COCKTAIL-COSMOPOLITAN	Cosmopolitan	Vodka, triple sec, arándanos	Beverages	Cocktails	0.00	5200.00	\N	https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
43	BEER-ESCUDO	Escudo	Cerveza Lager Chilena	Beverages	Beer	0.00	3000.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
30	SODA-GINGER-ALE	Ginger Ale	Bebida gaseosa jengibre	Beverages	Soft Drinks	0.00	1800.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
39	BEER-INDIO	Indio	Cerveza Lager Oscura	Beverages	Beer	0.00	3500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
18	FRESH-JUGO-MANZANA	Jugo de Manzana	Manzanas frescas, agua	Beverages	Fresh Juices	0.00	2200.00	\N	https://images.unsplash.com/photo-1553530666-ba11a7da3888?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
21	COCKTAIL-MOJITO	Mojito	Ron blanco, menta, lima, soda	Beverages	Cocktails	0.00	4500.00	\N	https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
6	CRAFT-PALE-ALE	Pale Ale	Cerveza artesanal Pale Ale	Beverages	Craft Beer	0.00	5500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
26	SODA-SPRITE	Sprite	Bebida gaseosa limón-lima	Beverages	Soft Drinks	0.00	1500.00	\N	https://images.unsplash.com/photo-1581636625402-29b2a704ef13?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
37	BEER-TKT-LIGHT	Tecate Light	Cerveza Lager Ligera	Beverages	Beer	0.00	3000.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
5	CRAFT-WHEAT-BEER	Wheat Beer	Cerveza artesanal de trigo	Beverages	Craft Beer	0.00	5500.00	\N	https://images.unsplash.com/photo-1608270586620-248524c67de9?w=400&h=300&fit=crop&crop=center	t	f	t	1	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-12 16:07:32.493572+00
9	APPETIZER-BUFFALO-WINGS	Alitas Buffalo	Alitas con salsa buffalo	Appetizers	Wings	0.00	15000.00	15000.00	https://images.unsplash.com/photo-1567620832904-9fe5cf23db13?w=400&h=300&fit=crop&crop=center	t	f	t	6	f	system	system	2025-09-12 16:07:32.493572+00	2025-09-15 00:28:34.675783+00
\.


--
-- TOC entry 4543 (class 0 OID 16638)
-- Dependencies: 234
-- Data for Name: modifier_options; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.modifier_options (option_id, modifier_id, name, price_delta, is_available, sort_order, created_at, updated_at) FROM stdin;
1	1	Small	0.00	t	1	2025-09-09 00:02:44.926402+00	2025-09-09 00:02:44.926402+00
2	1	Medium	0.50	t	2	2025-09-09 00:02:44.929353+00	2025-09-09 00:02:44.929353+00
3	1	Large	1.00	t	3	2025-09-09 00:02:44.931294+00	2025-09-09 00:02:44.931294+00
\.


--
-- TOC entry 4541 (class 0 OID 16624)
-- Dependencies: 232
-- Data for Name: modifiers; Type: TABLE DATA; Schema: menu; Owner: -
--

COPY menu.modifiers (modifier_id, name, description, is_required, allow_multiple, min_selections, max_selections, created_at, updated_at) FROM stdin;
1	Size	Choose a size	t	f	1	1	2025-09-09 00:02:44.908498+00	2025-09-09 00:02:44.908498+00
\.


--
-- TOC entry 4553 (class 0 OID 16741)
-- Dependencies: 244
-- Data for Name: order_items; Type: TABLE DATA; Schema: ord; Owner: -
--

COPY ord.order_items (order_item_id, order_id, menu_item_id, combo_id, quantity, base_price, vendor_price, price_delta, line_discount, line_total, profit, is_deleted, notes, snapshot_name, snapshot_sku, snapshot_category, snapshot_group, snapshot_version, snapshot_picture_url, selected_modifiers, created_at, updated_at, delivered_quantity) FROM stdin;
51	29	\N	\N	1	12.99	8.99	0.00	0.00	12.99	4.00	f	\N	Margherita Pizza	PIZZA001	Food	\N	\N	\N	\N	2025-09-12 19:20:37.156551+00	2025-09-12 19:20:47.227599+00	1
52	29	\N	\N	1	8.50	5.50	0.00	0.00	8.50	3.00	f	\N	Nachos	SNACK001	Snacks	\N	\N	\N	\N	2025-09-12 19:20:37.156551+00	2025-09-12 19:20:47.227599+00	1
32	12	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:21:47.460537+00	2025-09-11 16:21:47.460537+00	0
33	12	1	\N	1	2.50	1.75	0.00	0.00	2.50	2.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:21:47.460537+00	2025-09-11 16:21:47.460537+00	0
34	12	3	\N	1	2.99	2.09	0.00	0.00	2.99	2.59	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:21:47.460537+00	2025-09-11 16:21:47.460537+00	0
35	14	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:31:30.517415+00	2025-09-11 16:31:30.517415+00	0
36	14	1	\N	1	2.50	1.75	0.00	0.00	2.50	2.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:31:30.517415+00	2025-09-11 16:31:30.517415+00	0
37	14	3	\N	1	2.99	2.09	0.00	0.00	2.99	2.59	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:31:30.517415+00	2025-09-11 16:31:30.517415+00	0
39	15	1	\N	1	2.50	1.75	0.00	0.00	2.50	2.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:43:54.76657+00	2025-09-11 16:43:54.76657+00	0
40	15	3	\N	1	2.99	2.09	0.00	0.00	2.99	2.59	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:43:54.76657+00	2025-09-11 16:43:54.76657+00	0
38	15	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	t	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-11 16:43:54.76657+00	2025-09-11 16:43:54.76657+00	0
41	17	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 00:18:25.667207+00	2025-09-12 00:18:25.667207+00	0
42	18	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 00:19:32.685913+00	2025-09-12 00:19:32.685913+00	0
43	19	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 00:19:35.708464+00	2025-09-12 00:19:35.708464+00	0
44	20	2	\N	1	7.99	5.59	0.00	0.00	7.99	5.99	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 00:35:44.113477+00	2025-09-12 00:35:44.113477+00	0
45	20	1	\N	1	2.50	1.75	0.00	0.00	2.50	2.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 00:35:44.113477+00	2025-09-12 00:35:44.113477+00	0
46	20	3	\N	1	2.99	2.09	0.00	0.00	2.99	2.59	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 00:35:44.113477+00	2025-09-12 00:35:44.113477+00	0
48	23	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 18:21:40.892954+00	2025-09-12 18:21:46.341016+00	1
49	23	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 18:21:40.892954+00	2025-09-12 18:22:50.0992+00	1
47	23	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 18:21:40.892954+00	2025-09-12 18:22:51.258481+00	1
50	29	\N	\N	2	5.50	3.50	0.00	0.00	11.00	4.00	f	\N	Premium Beer	BEER001	Beverages	\N	\N	\N	\N	2025-09-12 19:20:37.156551+00	2025-09-12 19:20:47.227599+00	1
59	32	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:13:06.890032+00	2025-09-12 22:00:42.363572+00	1
60	32	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:13:06.890032+00	2025-09-12 22:00:42.363572+00	1
61	32	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:13:06.890032+00	2025-09-12 22:00:42.363572+00	1
62	32	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:13:06.890032+00	2025-09-12 22:00:42.363572+00	1
68	34	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:40.862999+00	2025-09-12 22:00:43.76203+00	1
56	31	2	\N	2	7.99	5.59	0.00	0.00	15.98	11.98	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:04:46.386792+00	2025-09-12 21:05:13.007307+00	2
54	31	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:04:46.386792+00	2025-09-12 21:05:14.277202+00	1
55	31	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:04:46.386792+00	2025-09-12 21:05:42.215052+00	1
57	31	27	\N	1	1500.00	1050.00	0.00	0.00	1500.00	1500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:04:46.386792+00	2025-09-12 21:05:43.23782+00	1
53	31	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	t	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:04:46.386792+00	2025-09-12 21:05:07.28377+00	1
69	34	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:40.862999+00	2025-09-12 22:00:43.76203+00	1
70	34	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:40.862999+00	2025-09-12 22:00:43.76203+00	1
67	34	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:40.862999+00	2025-09-12 22:00:43.76203+00	1
75	37	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:12:35.21838+00	2025-09-12 22:12:39.061711+00	1
76	37	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:12:35.21838+00	2025-09-12 22:12:39.061711+00	1
77	37	42	\N	1	3200.00	2240.00	0.00	0.00	3200.00	3200.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:12:35.21838+00	2025-09-12 22:12:39.061711+00	1
78	37	24	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:12:35.21838+00	2025-09-12 22:12:39.061711+00	1
79	38	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:19:20.590124+00	2025-09-14 01:19:24.738031+00	1
80	38	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:19:20.590124+00	2025-09-14 01:19:24.738031+00	1
81	38	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:19:20.590124+00	2025-09-14 01:19:24.738031+00	1
72	35	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:03:44.27183+00	2025-09-12 22:04:09.149449+00	1
73	35	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:03:44.27183+00	2025-09-12 22:04:09.149449+00	1
82	38	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:19:20.590124+00	2025-09-14 01:19:24.738031+00	1
83	40	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:34:15.06682+00	2025-09-14 01:34:15.06682+00	0
84	40	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:34:15.06682+00	2025-09-14 01:34:15.06682+00	0
85	40	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:34:15.06682+00	2025-09-14 01:34:15.06682+00	0
86	41	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:36:52.800861+00	2025-09-14 01:36:52.800861+00	0
63	33	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:08.137188+00	2025-09-12 22:00:40.417325+00	1
64	33	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:08.137188+00	2025-09-12 22:00:40.417325+00	1
65	33	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:08.137188+00	2025-09-12 22:00:40.417325+00	1
66	33	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:26:08.137188+00	2025-09-12 22:00:40.417325+00	1
58	32	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 21:13:06.890032+00	2025-09-12 22:00:42.363572+00	1
74	35	42	\N	1	3200.00	2240.00	0.00	0.00	3200.00	3200.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:03:44.27183+00	2025-09-12 22:04:09.149449+00	1
71	35	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-12 22:03:44.27183+00	2025-09-12 22:04:09.149449+00	1
87	41	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:36:52.800861+00	2025-09-14 01:36:52.800861+00	0
88	41	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 01:36:52.800861+00	2025-09-14 01:36:52.800861+00	0
89	42	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 02:15:14.180971+00	2025-09-14 02:15:19.41984+00	1
90	42	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 02:15:14.180971+00	2025-09-14 02:15:19.41984+00	1
91	42	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 02:15:14.180971+00	2025-09-14 02:15:19.41984+00	1
92	43	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 02:23:20.997419+00	2025-09-14 02:28:27.588958+00	1
93	43	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 02:23:20.997419+00	2025-09-14 02:28:27.588958+00	1
94	43	9	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-14 02:23:20.997419+00	2025-09-14 02:28:27.588958+00	1
95	50	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-15 15:42:58.41595+00	2025-09-15 15:43:02.703074+00	1
96	50	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-15 15:42:58.41595+00	2025-09-15 15:43:02.703074+00	1
97	50	9	\N	1	15000.00	10500.00	0.00	0.00	15000.00	15000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-15 15:42:58.41595+00	2025-09-15 15:43:02.703074+00	1
98	50	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-15 15:42:58.41595+00	2025-09-15 15:43:02.703074+00	1
99	51	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:37:46.558513+00	2025-09-16 02:37:50.945964+00	1
100	51	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:37:46.558513+00	2025-09-16 02:37:50.945964+00	1
101	51	9	\N	1	15000.00	10500.00	0.00	0.00	15000.00	15000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:37:46.558513+00	2025-09-16 02:37:50.945964+00	1
102	51	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:37:46.558513+00	2025-09-16 02:37:50.945964+00	1
103	51	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:37:46.558513+00	2025-09-16 02:37:50.945964+00	1
104	51	42	\N	1	3200.00	2240.00	0.00	0.00	3200.00	3200.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:37:46.558513+00	2025-09-16 02:37:50.945964+00	1
105	52	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:55:24.796583+00	2025-09-16 03:00:41.424279+00	1
106	52	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:55:24.796583+00	2025-09-16 03:00:41.424279+00	1
107	52	9	\N	1	15000.00	10500.00	0.00	0.00	15000.00	15000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:55:24.796583+00	2025-09-16 03:00:41.424279+00	1
108	52	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:55:24.796583+00	2025-09-16 03:00:41.424279+00	1
109	52	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:55:24.796583+00	2025-09-16 03:00:41.424279+00	1
110	52	42	\N	1	3200.00	2240.00	0.00	0.00	3200.00	3200.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 02:55:24.796583+00	2025-09-16 03:00:41.424279+00	1
111	53	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:44:50.062091+00	2025-09-16 07:45:16.965763+00	1
112	53	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:44:50.062091+00	2025-09-16 07:45:16.965763+00	1
113	53	9	\N	1	15000.00	10500.00	0.00	0.00	15000.00	15000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:44:50.062091+00	2025-09-16 07:45:16.965763+00	1
114	53	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:44:50.062091+00	2025-09-16 07:45:16.965763+00	1
115	53	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:44:50.062091+00	2025-09-16 07:45:16.965763+00	1
116	53	42	\N	1	3200.00	2240.00	0.00	0.00	3200.00	3200.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:44:50.062091+00	2025-09-16 07:45:16.965763+00	1
117	53	17	\N	1	2500.00	1750.00	0.00	0.00	2500.00	2500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:45:02.306285+00	2025-09-16 07:45:16.965763+00	1
118	53	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:45:02.306285+00	2025-09-16 07:45:16.965763+00	1
119	53	11	\N	1	3500.00	2450.00	0.00	0.00	3500.00	3500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:45:02.306285+00	2025-09-16 07:45:16.965763+00	1
120	53	16	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-09-16 07:45:02.306285+00	2025-09-16 07:45:16.965763+00	1
121	61	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:19:54.087507+00	2025-12-01 22:20:26.345865+00	1
122	61	24	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:19:54.087507+00	2025-12-01 22:20:26.345865+00	1
123	61	14	\N	1	1800.00	1260.00	0.00	0.00	1800.00	1800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:19:54.087507+00	2025-12-01 22:20:26.345865+00	1
124	61	42	\N	1	3200.00	2240.00	0.00	0.00	3200.00	3200.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:19:54.087507+00	2025-12-01 22:20:26.345865+00	1
125	61	35	\N	1	3800.00	2660.00	0.00	0.00	3800.00	3800.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:19:54.087507+00	2025-12-01 22:20:26.345865+00	1
126	62	31	\N	6	2000.00	1400.00	0.00	0.00	12000.00	12000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:05.936855+00	2025-12-01 22:20:46.202399+00	6
127	62	24	\N	3	4500.00	3150.00	0.00	0.00	13500.00	13500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:05.936855+00	2025-12-01 22:20:46.202399+00	3
128	62	28	\N	3	1500.00	1050.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:05.936855+00	2025-12-01 22:20:46.202399+00	3
129	62	1	\N	4	2.50	1.75	0.00	0.00	10.00	8.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:05.936855+00	2025-12-01 22:20:46.202399+00	4
130	63	31	\N	5	2000.00	1400.00	0.00	0.00	10000.00	10000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:14.949076+00	2025-12-01 22:20:49.272769+00	5
131	63	9	\N	4	15000.00	10500.00	0.00	0.00	60000.00	60000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:14.949076+00	2025-12-01 22:20:49.272769+00	4
132	63	12	\N	5	4500.00	3150.00	0.00	0.00	22500.00	22500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:20:14.949076+00	2025-12-01 22:20:49.272769+00	5
133	64	31	\N	4	2000.00	1400.00	0.00	0.00	8000.00	8000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:34:20.304291+00	2025-12-01 22:34:29.940805+00	4
134	64	17	\N	4	2500.00	1750.00	0.00	0.00	10000.00	10000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:34:20.304291+00	2025-12-01 22:34:29.940805+00	4
135	64	9	\N	3	15000.00	10500.00	0.00	0.00	45000.00	45000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:34:20.304291+00	2025-12-01 22:34:29.940805+00	3
136	64	12	\N	5	4500.00	3150.00	0.00	0.00	22500.00	22500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:34:20.304291+00	2025-12-01 22:34:29.940805+00	5
137	65	17	\N	4	2500.00	1750.00	0.00	0.00	10000.00	10000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:47:10.258534+00	2025-12-01 22:47:18.947141+00	4
138	65	31	\N	3	2000.00	1400.00	0.00	0.00	6000.00	6000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:47:10.258534+00	2025-12-01 22:47:18.947141+00	3
139	66	31	\N	3	2000.00	1400.00	0.00	0.00	6000.00	6000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:56:34.937601+00	2025-12-01 22:56:34.937601+00	0
140	66	17	\N	3	2500.00	1750.00	0.00	0.00	7500.00	7500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:56:34.937601+00	2025-12-01 22:56:34.937601+00	0
141	67	17	\N	3	2500.00	1750.00	0.00	0.00	7500.00	7500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:57:18.914293+00	2025-12-01 22:57:22.333682+00	3
142	67	31	\N	3	2000.00	1400.00	0.00	0.00	6000.00	6000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:57:18.914293+00	2025-12-01 22:57:22.333682+00	3
143	67	9	\N	3	15000.00	10500.00	0.00	0.00	45000.00	45000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:57:18.914293+00	2025-12-01 22:57:22.333682+00	3
144	67	12	\N	3	4500.00	3150.00	0.00	0.00	13500.00	13500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 22:57:18.914293+00	2025-12-01 22:57:22.333682+00	3
145	68	31	\N	3	2000.00	1400.00	0.00	0.00	6000.00	6000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:01:59.304934+00	2025-12-01 23:02:03.712359+00	3
146	68	42	\N	3	3200.00	2240.00	0.00	0.00	9600.00	9600.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:01:59.304934+00	2025-12-01 23:02:03.712359+00	3
147	68	24	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:01:59.304934+00	2025-12-01 23:02:03.712359+00	1
148	70	17	\N	2	2500.00	1750.00	0.00	0.00	5000.00	5000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:15:20.411291+00	2025-12-01 23:15:20.411291+00	0
149	70	31	\N	1	2000.00	1400.00	0.00	0.00	2000.00	2000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:15:20.411291+00	2025-12-01 23:15:20.411291+00	0
150	70	9	\N	1	15000.00	10500.00	0.00	0.00	15000.00	15000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:15:20.411291+00	2025-12-01 23:15:20.411291+00	0
151	70	12	\N	1	4500.00	3150.00	0.00	0.00	4500.00	4500.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-01 23:15:20.411291+00	2025-12-01 23:15:20.411291+00	0
152	75	8	\N	1	6000.00	4200.00	0.00	0.00	6000.00	6000.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-03 01:23:04.839457+00	2025-12-03 01:23:30.048132+00	1
153	75	1	\N	1	2.50	1.75	0.00	0.00	2.50	2.00	f	\N	\N	\N	\N	\N	\N	\N	\N	2025-12-03 01:23:04.839457+00	2025-12-03 01:23:30.048132+00	1
\.


--
-- TOC entry 4555 (class 0 OID 16765)
-- Dependencies: 246
-- Data for Name: order_logs; Type: TABLE DATA; Schema: ord; Owner: -
--

COPY ord.order_logs (log_id, order_id, action, old_value, new_value, server_id, created_at) FROM stdin;
92	34	mark_delivered	\N	[{"OrderItemId": 67, "DeliveredQuantity": 1}]	\N	2025-09-12 21:27:05.34949+00
24	11	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "fa4dac24-58f5-4905-9e02-a14ea6ae3228", "ProfitTotal": 0, "DiscountTotal": 0}		2025-09-11 15:52:24.419622+00
25	12	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "c4aa67b1-5edb-462a-b2aa-bf76ecb792b8", "ProfitTotal": 0, "DiscountTotal": 0}	Preeti	2025-09-11 16:21:41.079903+00
26	12	add_items	\N	[{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0}, {"Id": 0, "Profit": 2.00, "ComboId": null, "Quantity": 1, "BasePrice": 2.50, "LineTotal": 2.50, "MenuItemId": 1, "PriceDelta": 0}, {"Id": 0, "Profit": 2.59, "ComboId": null, "Quantity": 1, "BasePrice": 2.99, "LineTotal": 2.99, "MenuItemId": 3, "PriceDelta": 0}]	\N	2025-09-11 16:21:47.475072+00
27	13	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "d9cfdee8-ab41-4afb-843d-030cf97fd210", "ProfitTotal": 0, "DiscountTotal": 0}	Preeti	2025-09-11 16:22:18.220337+00
28	14	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "ddbd2da8-4e07-4e6e-9e1a-59ebefaa633a", "ProfitTotal": 0, "DiscountTotal": 0}	Preeti	2025-09-11 16:31:25.080395+00
29	14	add_items	\N	[{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0}, {"Id": 0, "Profit": 2.00, "ComboId": null, "Quantity": 1, "BasePrice": 2.50, "LineTotal": 2.50, "MenuItemId": 1, "PriceDelta": 0}, {"Id": 0, "Profit": 2.59, "ComboId": null, "Quantity": 1, "BasePrice": 2.99, "LineTotal": 2.99, "MenuItemId": 3, "PriceDelta": 0}]	\N	2025-09-11 16:31:30.522779+00
30	14	close	\N	{"Status": "closed"}	\N	2025-09-11 16:31:34.130862+00
31	15	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "321db6d8-290b-47cd-ae79-72cf3ab060b0", "ProfitTotal": 0, "DiscountTotal": 0}	Preeti	2025-09-11 16:43:47.579059+00
32	15	add_items	\N	[{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0}, {"Id": 0, "Profit": 2.00, "ComboId": null, "Quantity": 1, "BasePrice": 2.50, "LineTotal": 2.50, "MenuItemId": 1, "PriceDelta": 0}, {"Id": 0, "Profit": 2.59, "ComboId": null, "Quantity": 1, "BasePrice": 2.99, "LineTotal": 2.99, "MenuItemId": 3, "PriceDelta": 0}]	\N	2025-09-11 16:43:54.781838+00
33	15	close	\N	{"Status": "closed"}	\N	2025-09-11 16:44:31.945247+00
34	15	delete_item	{"OrderItemId": 38}	\N	\N	2025-09-11 16:45:01.073627+00
35	16	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "1d699cd6-2056-4756-84a5-fa056fbcb04d", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-11 20:45:05.820348+00
36	16	close	\N	{"Status": "closed"}	\N	2025-09-11 20:46:02.015099+00
67	28	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "c0697c7c-c38c-4a94-ae38-245e7c35a1bc", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 19:08:28.869717+00
37	17	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 7.99, "Status": "open", "TableId": "Billiard 2", "Subtotal": 7.99, "TaxTotal": 0, "SessionId": "1d699cd6-2056-4756-84a5-fa056fbcb04d", "ProfitTotal": 5.99, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 00:18:25.713983+00
38	18	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 7.99, "Status": "open", "TableId": "Billiard 2", "Subtotal": 7.99, "TaxTotal": 0, "SessionId": "1d699cd6-2056-4756-84a5-fa056fbcb04d", "ProfitTotal": 5.99, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 00:19:32.691248+00
39	19	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 7.99, "Status": "open", "TableId": "Billiard 2", "Subtotal": 7.99, "TaxTotal": 0, "SessionId": "1d699cd6-2056-4756-84a5-fa056fbcb04d", "ProfitTotal": 5.99, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 00:19:35.713014+00
40	20	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 5.99, "ComboId": null, "Quantity": 1, "BasePrice": 7.99, "LineTotal": 7.99, "MenuItemId": 2, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2.00, "ComboId": null, "Quantity": 1, "BasePrice": 2.50, "LineTotal": 2.50, "MenuItemId": 1, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2.59, "ComboId": null, "Quantity": 1, "BasePrice": 2.99, "LineTotal": 2.99, "MenuItemId": 3, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 13.48, "Status": "open", "TableId": "Billiard 2", "Subtotal": 13.48, "TaxTotal": 0, "SessionId": "1d699cd6-2056-4756-84a5-fa056fbcb04d", "ProfitTotal": 10.58, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 00:35:44.162805+00
41	20	close	\N	{"Status": "closed"}	\N	2025-09-12 18:01:53.984045+00
42	19	close	\N	{"Status": "closed"}	\N	2025-09-12 18:01:54.046171+00
43	18	close	\N	{"Status": "closed"}	\N	2025-09-12 18:01:54.081119+00
44	17	close	\N	{"Status": "closed"}	\N	2025-09-12 18:01:54.112347+00
45	21	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "41a13af8-7b2d-4cf8-85cc-e60a60a6708a", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:02:18.359486+00
46	21	close	\N	{"Status": "closed"}	\N	2025-09-12 18:02:33.372032+00
47	22	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "960f44f6-6680-47ac-bca8-103aff4ddab4", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:21:14.52897+00
48	23	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "2dbd064f-2bba-4e26-979a-6b563f4e8006", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:21:18.281371+00
49	23	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-12 18:21:40.907182+00
50	23	mark_delivered	\N	[{"OrderItemId": 47, "DeliveredQuantity": 1}]	\N	2025-09-12 18:21:45.578745+00
51	23	mark_delivered	\N	[{"OrderItemId": 48, "DeliveredQuantity": 1}]	\N	2025-09-12 18:21:46.344254+00
52	23	mark_delivered	\N	[{"OrderItemId": 47, "DeliveredQuantity": 1}]	\N	2025-09-12 18:21:47.211885+00
53	23	mark_delivered	\N	[{"OrderItemId": 47, "DeliveredQuantity": 1}]	\N	2025-09-12 18:21:47.84955+00
54	22	close	\N	{"Status": "closed"}	\N	2025-09-12 18:22:01.826963+00
55	23	close	\N	{"Status": "closed"}	\N	2025-09-12 18:22:05.163122+00
56	23	mark_delivered	\N	[{"OrderItemId": 49, "DeliveredQuantity": 1}]	\N	2025-09-12 18:22:50.103045+00
57	23	mark_delivered	\N	[{"OrderItemId": 47, "DeliveredQuantity": 1}]	\N	2025-09-12 18:22:50.732346+00
58	23	mark_delivered	\N	[{"OrderItemId": 47, "DeliveredQuantity": 1}]	\N	2025-09-12 18:22:51.261788+00
59	24	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "d9c22572-31e8-4c5a-af78-66468b5505b3", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:25:26.795122+00
60	24	close	\N	{"Status": "closed"}	\N	2025-09-12 18:25:28.676684+00
61	25	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "575071d5-b6c3-4ebf-bd7c-2da9495f1bd7", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:34:43.834363+00
62	25	close	\N	{"Status": "closed"}	\N	2025-09-12 18:34:47.538537+00
63	26	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "156afeca-8116-4674-86da-e3a1fee72f3c", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:49:57.543744+00
64	26	close	\N	{"Status": "closed"}	\N	2025-09-12 18:50:03.49382+00
65	27	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "38e77108-33e6-4476-8e71-3f8d76e5c933", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 18:53:45.792304+00
66	27	close	\N	{"Status": "closed"}	\N	2025-09-12 18:53:49.243618+00
68	28	close	\N	{"Status": "closed"}	\N	2025-09-12 19:08:31.187696+00
69	30	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "551546cd-3afc-492b-8a31-aa189cd70d8d", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 19:21:25.147888+00
70	30	close	\N	{"Status": "closed"}	\N	2025-09-12 19:21:27.640502+00
71	31	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "b40af7ae-a081-474b-9e66-29cf43a75a89", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 21:04:27.736483+00
72	31	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 11.98, "ComboId": null, "Quantity": 2, "BasePrice": 7.99, "LineTotal": 15.98, "MenuItemId": 2, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 1500.00, "ComboId": null, "Quantity": 1, "BasePrice": 1500.00, "LineTotal": 1500.00, "MenuItemId": 27, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-12 21:04:46.403491+00
73	31	mark_delivered	\N	[{"OrderItemId": 53, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:01.825883+00
74	31	mark_delivered	\N	[{"OrderItemId": 54, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:02.856972+00
75	31	mark_delivered	\N	[{"OrderItemId": 55, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:03.633+00
76	31	mark_delivered	\N	[{"OrderItemId": 57, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:06.459028+00
77	31	mark_delivered	\N	[{"OrderItemId": 53, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:07.295692+00
78	31	mark_delivered	\N	[{"OrderItemId": 56, "DeliveredQuantity": 2}]	\N	2025-09-12 21:05:13.011015+00
79	31	mark_delivered	\N	[{"OrderItemId": 54, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:14.293825+00
80	31	mark_delivered	\N	[{"OrderItemId": 55, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:42.21846+00
81	31	mark_delivered	\N	[{"OrderItemId": 57, "DeliveredQuantity": 1}]	\N	2025-09-12 21:05:43.258047+00
82	31	close	\N	{"Status": "closed"}	\N	2025-09-12 21:07:04.268089+00
83	31	delete_item	{"OrderItemId": 53}	\N	\N	2025-09-12 21:07:06.05527+00
84	32	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 17300.00, "Status": "open", "TableId": "Billiard 1", "Subtotal": 17300.00, "TaxTotal": 0, "SessionId": "b40af7ae-a081-474b-9e66-29cf43a75a89", "ProfitTotal": 17300.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 21:13:06.900289+00
85	33	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 13500.00, "Status": "open", "TableId": "Billiard 1", "Subtotal": 13500.00, "TaxTotal": 0, "SessionId": "b40af7ae-a081-474b-9e66-29cf43a75a89", "ProfitTotal": 13500.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 21:26:08.146022+00
86	34	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "1922dc18-b808-46c3-9d00-74c29e6c8a74", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 21:26:24.333396+00
87	34	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-12 21:26:40.869022+00
88	34	mark_delivered	\N	[{"OrderItemId": 67, "DeliveredQuantity": 1}]	\N	2025-09-12 21:26:46.763654+00
89	34	mark_delivered	\N	[{"OrderItemId": 68, "DeliveredQuantity": 1}]	\N	2025-09-12 21:26:52.078499+00
90	34	mark_delivered	\N	[{"OrderItemId": 69, "DeliveredQuantity": 1}]	\N	2025-09-12 21:26:54.950541+00
91	34	mark_delivered	\N	[{"OrderItemId": 70, "DeliveredQuantity": 1}]	\N	2025-09-12 21:27:01.230496+00
93	33	mark_delivered	\N	[{"OrderItemId": 63, "DeliveredQuantity": 1}, {"OrderItemId": 64, "DeliveredQuantity": 1}, {"OrderItemId": 65, "DeliveredQuantity": 1}, {"OrderItemId": 66, "DeliveredQuantity": 1}]	\N	2025-09-12 21:58:52.745191+00
94	32	mark_delivered	\N	[{"OrderItemId": 58, "DeliveredQuantity": 1}, {"OrderItemId": 59, "DeliveredQuantity": 1}, {"OrderItemId": 60, "DeliveredQuantity": 1}, {"OrderItemId": 61, "DeliveredQuantity": 1}, {"OrderItemId": 62, "DeliveredQuantity": 1}]	\N	2025-09-12 21:58:55.229407+00
95	34	mark_delivered	\N	[{"OrderItemId": 68, "DeliveredQuantity": 1}, {"OrderItemId": 69, "DeliveredQuantity": 1}, {"OrderItemId": 70, "DeliveredQuantity": 1}, {"OrderItemId": 67, "DeliveredQuantity": 1}]	\N	2025-09-12 21:58:56.781533+00
96	33	mark_delivered	\N	[{"OrderItemId": 63, "DeliveredQuantity": 1}, {"OrderItemId": 64, "DeliveredQuantity": 1}, {"OrderItemId": 65, "DeliveredQuantity": 1}, {"OrderItemId": 66, "DeliveredQuantity": 1}]	\N	2025-09-12 21:59:00.179924+00
97	33	mark_delivered	\N	[{"OrderItemId": 63, "DeliveredQuantity": 1}, {"OrderItemId": 64, "DeliveredQuantity": 1}, {"OrderItemId": 65, "DeliveredQuantity": 1}, {"OrderItemId": 66, "DeliveredQuantity": 1}]	\N	2025-09-12 21:59:11.445554+00
98	32	mark_delivered	\N	[{"OrderItemId": 58, "DeliveredQuantity": 1}, {"OrderItemId": 59, "DeliveredQuantity": 1}, {"OrderItemId": 60, "DeliveredQuantity": 1}, {"OrderItemId": 61, "DeliveredQuantity": 1}, {"OrderItemId": 62, "DeliveredQuantity": 1}]	\N	2025-09-12 21:59:13.05825+00
99	34	mark_delivered	\N	[{"OrderItemId": 68, "DeliveredQuantity": 1}, {"OrderItemId": 69, "DeliveredQuantity": 1}, {"OrderItemId": 70, "DeliveredQuantity": 1}, {"OrderItemId": 67, "DeliveredQuantity": 1}]	\N	2025-09-12 21:59:14.470736+00
100	33	mark_delivered	\N	[{"OrderItemId": 63, "DeliveredQuantity": 1}, {"OrderItemId": 64, "DeliveredQuantity": 1}, {"OrderItemId": 65, "DeliveredQuantity": 1}, {"OrderItemId": 66, "DeliveredQuantity": 1}]	\N	2025-09-12 22:00:19.857796+00
101	32	mark_delivered	\N	[{"OrderItemId": 58, "DeliveredQuantity": 1}, {"OrderItemId": 59, "DeliveredQuantity": 1}, {"OrderItemId": 60, "DeliveredQuantity": 1}, {"OrderItemId": 61, "DeliveredQuantity": 1}, {"OrderItemId": 62, "DeliveredQuantity": 1}]	\N	2025-09-12 22:00:21.501011+00
102	34	mark_delivered	\N	[{"OrderItemId": 68, "DeliveredQuantity": 1}, {"OrderItemId": 69, "DeliveredQuantity": 1}, {"OrderItemId": 70, "DeliveredQuantity": 1}, {"OrderItemId": 67, "DeliveredQuantity": 1}]	\N	2025-09-12 22:00:23.583076+00
103	33	mark_delivered	\N	[{"OrderItemId": 63, "DeliveredQuantity": 1}, {"OrderItemId": 64, "DeliveredQuantity": 1}, {"OrderItemId": 65, "DeliveredQuantity": 1}, {"OrderItemId": 66, "DeliveredQuantity": 1}]	\N	2025-09-12 22:00:40.430044+00
104	32	mark_delivered	\N	[{"OrderItemId": 58, "DeliveredQuantity": 1}, {"OrderItemId": 59, "DeliveredQuantity": 1}, {"OrderItemId": 60, "DeliveredQuantity": 1}, {"OrderItemId": 61, "DeliveredQuantity": 1}, {"OrderItemId": 62, "DeliveredQuantity": 1}]	\N	2025-09-12 22:00:42.37147+00
105	34	mark_delivered	\N	[{"OrderItemId": 68, "DeliveredQuantity": 1}, {"OrderItemId": 69, "DeliveredQuantity": 1}, {"OrderItemId": 70, "DeliveredQuantity": 1}, {"OrderItemId": 67, "DeliveredQuantity": 1}]	\N	2025-09-12 22:00:43.768906+00
106	35	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3200.00, "ComboId": null, "Quantity": 1, "BasePrice": 3200.00, "LineTotal": 3200.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 12200.00, "Status": "open", "TableId": "Billiard 2", "Subtotal": 12200.00, "TaxTotal": 0, "SessionId": "1922dc18-b808-46c3-9d00-74c29e6c8a74", "ProfitTotal": 12200.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-12 22:03:44.298579+00
107	35	mark_delivered	\N	[{"OrderItemId": 71, "DeliveredQuantity": 1}, {"OrderItemId": 72, "DeliveredQuantity": 1}, {"OrderItemId": 73, "DeliveredQuantity": 1}, {"OrderItemId": 74, "DeliveredQuantity": 1}]	\N	2025-09-12 22:03:47.700497+00
108	35	mark_delivered	\N	[{"OrderItemId": 71, "DeliveredQuantity": 1}, {"OrderItemId": 72, "DeliveredQuantity": 1}, {"OrderItemId": 73, "DeliveredQuantity": 1}, {"OrderItemId": 74, "DeliveredQuantity": 1}]	\N	2025-09-12 22:03:51.153853+00
109	35	mark_delivered	\N	[{"OrderItemId": 71, "DeliveredQuantity": 1}, {"OrderItemId": 72, "DeliveredQuantity": 1}, {"OrderItemId": 73, "DeliveredQuantity": 1}, {"OrderItemId": 74, "DeliveredQuantity": 1}]	\N	2025-09-12 22:03:54.825257+00
110	35	mark_delivered	\N	[{"OrderItemId": 72, "DeliveredQuantity": 1}, {"OrderItemId": 73, "DeliveredQuantity": 1}, {"OrderItemId": 74, "DeliveredQuantity": 1}, {"OrderItemId": 71, "DeliveredQuantity": 1}]	\N	2025-09-12 22:04:02.266595+00
111	35	mark_delivered	\N	[{"OrderItemId": 72, "DeliveredQuantity": 1}, {"OrderItemId": 73, "DeliveredQuantity": 1}, {"OrderItemId": 74, "DeliveredQuantity": 1}, {"OrderItemId": 71, "DeliveredQuantity": 1}]	\N	2025-09-12 22:04:05.668528+00
112	35	mark_delivered	\N	[{"OrderItemId": 72, "DeliveredQuantity": 1}, {"OrderItemId": 73, "DeliveredQuantity": 1}, {"OrderItemId": 74, "DeliveredQuantity": 1}, {"OrderItemId": 71, "DeliveredQuantity": 1}]	\N	2025-09-12 22:04:09.155624+00
113	36	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "7d8fbab1-a573-4f04-b405-67214ac2d0a4", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 22:12:21.067974+00
114	36	close	\N	{"Status": "closed"}	\N	2025-09-12 22:12:22.782157+00
115	37	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "782045f7-b733-40e2-b44a-44ac373aae48", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	Preeti	2025-09-12 22:12:27.193228+00
116	37	add_items	\N	[{"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3200.00, "ComboId": null, "Quantity": 1, "BasePrice": 3200.00, "LineTotal": 3200.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 24, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-12 22:12:35.229575+00
117	37	mark_delivered	\N	[{"OrderItemId": 75, "DeliveredQuantity": 1}, {"OrderItemId": 76, "DeliveredQuantity": 1}, {"OrderItemId": 77, "DeliveredQuantity": 1}, {"OrderItemId": 78, "DeliveredQuantity": 1}]	\N	2025-09-12 22:12:39.07038+00
118	38	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "d08c98b8-416d-48dd-bf3e-d2470400534c", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	a040f705-2252-4218-a15f-e55255416067	2025-09-14 01:19:09.715224+00
119	38	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-14 01:19:20.609747+00
120	38	mark_delivered	\N	[{"OrderItemId": 79, "DeliveredQuantity": 1}, {"OrderItemId": 80, "DeliveredQuantity": 1}, {"OrderItemId": 81, "DeliveredQuantity": 1}, {"OrderItemId": 82, "DeliveredQuantity": 1}]	\N	2025-09-14 01:19:24.750246+00
121	39	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "f03a977d-db67-4ba9-b6ed-27d5075475f9", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	a040f705-2252-4218-a15f-e55255416067	2025-09-14 01:31:28.210643+00
122	40	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 9000.00, "Status": "open", "TableId": "Billiard 1", "Subtotal": 9000.00, "TaxTotal": 0, "SessionId": "f03a977d-db67-4ba9-b6ed-27d5075475f9", "ProfitTotal": 9000.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-09-14 01:34:15.076971+00
123	40	close	\N	{"Status": "closed"}	\N	2025-09-14 01:34:46.890453+00
124	39	close	\N	{"Status": "closed"}	\N	2025-09-14 01:34:46.925764+00
125	41	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "73431e13-879e-4b8a-aa58-f146486f87a1", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	c85b2501-68cb-44ea-bb3b-f7a254352471	2025-09-14 01:36:43.161635+00
126	41	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-14 01:36:52.810999+00
127	41	close	\N	{"Status": "closed"}	\N	2025-09-14 01:37:00.936778+00
128	42	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "e18396e6-4787-46c6-aef9-e3af01d7c731", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:15:00.560638+00
129	42	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-14 02:15:14.196882+00
130	42	mark_delivered	\N	[{"OrderItemId": 89, "DeliveredQuantity": 1}, {"OrderItemId": 90, "DeliveredQuantity": 1}, {"OrderItemId": 91, "DeliveredQuantity": 1}]	\N	2025-09-14 02:15:19.428779+00
131	43	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "1ba86713-fa77-4c6c-b81e-5b7ff14db2ce", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:23:10.090896+00
132	43	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-14 02:23:21.010992+00
133	43	mark_delivered	\N	[{"OrderItemId": 92, "DeliveredQuantity": 1}, {"OrderItemId": 93, "DeliveredQuantity": 1}, {"OrderItemId": 94, "DeliveredQuantity": 1}]	\N	2025-09-14 02:28:27.617847+00
134	44	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "d38dbcdc-cac9-4179-b61c-92ff6982993b", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:34:00.365916+00
135	45	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "34f35a28-2145-4d66-8181-46e03f80ab85", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:34:02.508431+00
136	46	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "1918b113-fff0-4e1d-b2ff-99ffcdc2e114", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:34:05.024818+00
137	47	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "88c50846-2c42-4537-ae89-d8b0b42bc448", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:34:07.758904+00
138	48	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 5", "Subtotal": 0, "TaxTotal": 0, "SessionId": "f191267d-2aac-48df-9a13-2b4a2ebf7f9f", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 02:34:10.567528+00
139	44	close	\N	{"Status": "closed"}	\N	2025-09-14 17:05:07.547482+00
140	45	close	\N	{"Status": "closed"}	\N	2025-09-14 17:05:10.849871+00
141	46	close	\N	{"Status": "closed"}	\N	2025-09-14 17:05:12.951944+00
142	47	close	\N	{"Status": "closed"}	\N	2025-09-14 17:05:15.893866+00
143	48	close	\N	{"Status": "closed"}	\N	2025-09-14 17:05:18.303939+00
144	49	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "16f1957c-26bf-4602-9b4b-bf418eda6213", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-14 18:12:02.250894+00
145	49	close	\N	{"Status": "closed"}	\N	2025-09-14 18:12:10.709051+00
146	50	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "5edeefed-6143-4d2c-8f60-aa62b464a64a", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-15 15:42:47.256962+00
147	50	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 15000.00, "ComboId": null, "Quantity": 1, "BasePrice": 15000.00, "LineTotal": 15000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-15 15:42:58.428246+00
148	50	mark_delivered	\N	[{"OrderItemId": 95, "DeliveredQuantity": 1}, {"OrderItemId": 96, "DeliveredQuantity": 1}, {"OrderItemId": 97, "DeliveredQuantity": 1}, {"OrderItemId": 98, "DeliveredQuantity": 1}]	\N	2025-09-15 15:43:02.712264+00
149	51	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "bc2c8990-38f6-4ce0-ab0c-4d06c2602c66", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-16 02:37:33.08563+00
150	51	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 15000.00, "ComboId": null, "Quantity": 1, "BasePrice": 15000.00, "LineTotal": 15000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3200.00, "ComboId": null, "Quantity": 1, "BasePrice": 3200.00, "LineTotal": 3200.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-16 02:37:46.577562+00
151	51	mark_delivered	\N	[{"OrderItemId": 99, "DeliveredQuantity": 1}, {"OrderItemId": 100, "DeliveredQuantity": 1}, {"OrderItemId": 101, "DeliveredQuantity": 1}, {"OrderItemId": 102, "DeliveredQuantity": 1}, {"OrderItemId": 103, "DeliveredQuantity": 1}, {"OrderItemId": 104, "DeliveredQuantity": 1}]	\N	2025-09-16 02:37:50.962006+00
152	52	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "45781653-67c0-43e2-b886-7013d58823b6", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-09-16 02:55:02.195945+00
153	52	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 15000.00, "ComboId": null, "Quantity": 1, "BasePrice": 15000.00, "LineTotal": 15000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3200.00, "ComboId": null, "Quantity": 1, "BasePrice": 3200.00, "LineTotal": 3200.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-16 02:55:24.819254+00
154	52	mark_delivered	\N	[{"OrderItemId": 105, "DeliveredQuantity": 1}, {"OrderItemId": 106, "DeliveredQuantity": 1}, {"OrderItemId": 107, "DeliveredQuantity": 1}, {"OrderItemId": 108, "DeliveredQuantity": 1}, {"OrderItemId": 109, "DeliveredQuantity": 1}, {"OrderItemId": 110, "DeliveredQuantity": 1}]	\N	2025-09-16 03:00:41.437797+00
155	53	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "a52c412b-6c0b-4cf5-8499-064559f25fce", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	a040f705-2252-4218-a15f-e55255416067	2025-09-16 07:44:29.263711+00
156	53	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 15000.00, "ComboId": null, "Quantity": 1, "BasePrice": 15000.00, "LineTotal": 15000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3200.00, "ComboId": null, "Quantity": 1, "BasePrice": 3200.00, "LineTotal": 3200.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-16 07:44:50.080511+00
157	53	add_items	\N	[{"Id": 0, "Profit": 2500.00, "ComboId": null, "Quantity": 1, "BasePrice": 2500.00, "LineTotal": 2500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3500.00, "ComboId": null, "Quantity": 1, "BasePrice": 3500.00, "LineTotal": 3500.00, "MenuItemId": 11, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 16, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-09-16 07:45:02.312785+00
158	53	mark_delivered	\N	[{"OrderItemId": 111, "DeliveredQuantity": 1}, {"OrderItemId": 112, "DeliveredQuantity": 1}, {"OrderItemId": 113, "DeliveredQuantity": 1}, {"OrderItemId": 114, "DeliveredQuantity": 1}, {"OrderItemId": 115, "DeliveredQuantity": 1}, {"OrderItemId": 116, "DeliveredQuantity": 1}, {"OrderItemId": 117, "DeliveredQuantity": 1}, {"OrderItemId": 118, "DeliveredQuantity": 1}, {"OrderItemId": 119, "DeliveredQuantity": 1}, {"OrderItemId": 120, "DeliveredQuantity": 1}]	\N	2025-09-16 07:45:16.980618+00
159	54	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "58850a49-b68d-4430-85fb-ab448a301ea8", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	0f3b1931-7f0f-4e16-8d88-9f7d0a1d7f8f	2025-09-18 05:43:32.101155+00
160	54	mark_delivered	\N	[]	\N	2025-09-18 18:36:54.351925+00
161	55	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 18:29:41.096543+00
162	55	mark_waiting	\N	{"Status": "waiting"}	\N	2025-12-01 18:29:58.962619+00
163	56	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "7f1557eb-788c-415d-9042-77b02730a7a8", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:14:58.21181+00
164	57	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "d8624a3d-8ded-4299-b704-4e1eb98acd31", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:15:02.806191+00
165	58	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "1b5446fb-2537-4007-be4c-e4d669f1673e", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:15:05.109827+00
166	59	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "6fdcf12e-d92b-431d-a57a-0e1f18fb8857", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:15:07.357498+00
167	60	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 5", "Subtotal": 0, "TaxTotal": 0, "SessionId": "2a6a3248-0cc4-489f-acfa-d916f3250514", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:15:10.231863+00
168	61	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 24, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 1800.00, "ComboId": null, "Quantity": 1, "BasePrice": 1800.00, "LineTotal": 1800.00, "MenuItemId": 14, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3200.00, "ComboId": null, "Quantity": 1, "BasePrice": 3200.00, "LineTotal": 3200.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 3800.00, "ComboId": null, "Quantity": 1, "BasePrice": 3800.00, "LineTotal": 3800.00, "MenuItemId": 35, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 15300.00, "Status": "open", "TableId": "Billiard 2", "Subtotal": 15300.00, "TaxTotal": 0, "SessionId": "d8624a3d-8ded-4299-b704-4e1eb98acd31", "ProfitTotal": 15300.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-12-01 22:19:54.103975+00
169	62	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 12000.00, "ComboId": null, "Quantity": 6, "BasePrice": 2000.00, "LineTotal": 12000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 13500.00, "ComboId": null, "Quantity": 3, "BasePrice": 4500.00, "LineTotal": 13500.00, "MenuItemId": 24, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 3, "BasePrice": 1500.00, "LineTotal": 4500.00, "MenuItemId": 28, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 8.00, "ComboId": null, "Quantity": 4, "BasePrice": 2.50, "LineTotal": 10.00, "MenuItemId": 1, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 30010.00, "Status": "open", "TableId": "Billiard 3", "Subtotal": 30010.00, "TaxTotal": 0, "SessionId": "6fdcf12e-d92b-431d-a57a-0e1f18fb8857", "ProfitTotal": 30008.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-12-01 22:20:05.947593+00
170	63	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 10000.00, "ComboId": null, "Quantity": 5, "BasePrice": 2000.00, "LineTotal": 10000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 60000.00, "ComboId": null, "Quantity": 4, "BasePrice": 15000.00, "LineTotal": 60000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 22500.00, "ComboId": null, "Quantity": 5, "BasePrice": 4500.00, "LineTotal": 22500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 92500.00, "Status": "open", "TableId": "Bar 5", "Subtotal": 92500.00, "TaxTotal": 0, "SessionId": "2a6a3248-0cc4-489f-acfa-d916f3250514", "ProfitTotal": 92500.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-12-01 22:20:14.956239+00
171	56	mark_delivered	\N	[]	\N	2025-12-01 22:20:22.930533+00
172	61	mark_delivered	\N	[{"OrderItemId": 121, "DeliveredQuantity": 1}, {"OrderItemId": 122, "DeliveredQuantity": 1}, {"OrderItemId": 123, "DeliveredQuantity": 1}, {"OrderItemId": 124, "DeliveredQuantity": 1}, {"OrderItemId": 125, "DeliveredQuantity": 1}]	\N	2025-12-01 22:20:26.354669+00
173	57	mark_delivered	\N	[]	\N	2025-12-01 22:20:28.206173+00
174	58	mark_delivered	\N	[]	\N	2025-12-01 22:20:44.043404+00
175	62	mark_delivered	\N	[{"OrderItemId": 126, "DeliveredQuantity": 6}, {"OrderItemId": 127, "DeliveredQuantity": 3}, {"OrderItemId": 128, "DeliveredQuantity": 3}, {"OrderItemId": 129, "DeliveredQuantity": 4}]	\N	2025-12-01 22:20:46.223298+00
176	59	mark_delivered	\N	[]	\N	2025-12-01 22:20:47.729566+00
177	63	mark_delivered	\N	[{"OrderItemId": 130, "DeliveredQuantity": 5}, {"OrderItemId": 131, "DeliveredQuantity": 4}, {"OrderItemId": 132, "DeliveredQuantity": 5}]	\N	2025-12-01 22:20:49.28066+00
178	60	mark_delivered	\N	[]	\N	2025-12-01 22:20:50.978895+00
179	64	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "df6bf801-7846-46cf-9e4d-d535c39c5d6c", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:34:07.940035+00
180	64	add_items	\N	[{"Id": 0, "Profit": 8000.00, "ComboId": null, "Quantity": 4, "BasePrice": 2000.00, "LineTotal": 8000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 10000.00, "ComboId": null, "Quantity": 4, "BasePrice": 2500.00, "LineTotal": 10000.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 45000.00, "ComboId": null, "Quantity": 3, "BasePrice": 15000.00, "LineTotal": 45000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 22500.00, "ComboId": null, "Quantity": 5, "BasePrice": 4500.00, "LineTotal": 22500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-12-01 22:34:20.313065+00
181	64	mark_delivered	\N	[{"OrderItemId": 133, "DeliveredQuantity": 4}, {"OrderItemId": 134, "DeliveredQuantity": 4}, {"OrderItemId": 135, "DeliveredQuantity": 3}, {"OrderItemId": 136, "DeliveredQuantity": 5}]	\N	2025-12-01 22:34:29.949312+00
182	65	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "518e229b-d581-4ce0-87b8-b1c929173542", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:47:00.221005+00
183	65	add_items	\N	[{"Id": 0, "Profit": 10000.00, "ComboId": null, "Quantity": 4, "BasePrice": 2500.00, "LineTotal": 10000.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 6000.00, "ComboId": null, "Quantity": 3, "BasePrice": 2000.00, "LineTotal": 6000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-12-01 22:47:10.264991+00
184	65	mark_delivered	\N	[{"OrderItemId": 137, "DeliveredQuantity": 4}, {"OrderItemId": 138, "DeliveredQuantity": 3}]	\N	2025-12-01 22:47:18.952424+00
185	66	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "fa5a5cbb-9e17-4467-ac58-0d4b72a7af7d", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:56:28.239429+00
186	66	add_items	\N	[{"Id": 0, "Profit": 6000.00, "ComboId": null, "Quantity": 3, "BasePrice": 2000.00, "LineTotal": 6000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 7500.00, "ComboId": null, "Quantity": 3, "BasePrice": 2500.00, "LineTotal": 7500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-12-01 22:56:34.942393+00
187	66	close	\N	{"Status": "closed"}	\N	2025-12-01 22:56:52.811219+00
188	67	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "a42b20eb-6088-4e25-9f7b-b026c718195f", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 22:57:09.618014+00
189	67	add_items	\N	[{"Id": 0, "Profit": 7500.00, "ComboId": null, "Quantity": 3, "BasePrice": 2500.00, "LineTotal": 7500.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 6000.00, "ComboId": null, "Quantity": 3, "BasePrice": 2000.00, "LineTotal": 6000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 45000.00, "ComboId": null, "Quantity": 3, "BasePrice": 15000.00, "LineTotal": 45000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 13500.00, "ComboId": null, "Quantity": 3, "BasePrice": 4500.00, "LineTotal": 13500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-12-01 22:57:18.923299+00
190	67	mark_delivered	\N	[{"OrderItemId": 141, "DeliveredQuantity": 3}, {"OrderItemId": 142, "DeliveredQuantity": 3}, {"OrderItemId": 143, "DeliveredQuantity": 3}, {"OrderItemId": 144, "DeliveredQuantity": 3}]	\N	2025-12-01 22:57:22.341281+00
191	68	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 4", "Subtotal": 0, "TaxTotal": 0, "SessionId": "eaaa4e7c-f743-48f0-ad0d-b317a71c80e1", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 23:01:42.008299+00
192	68	add_items	\N	[{"Id": 0, "Profit": 6000.00, "ComboId": null, "Quantity": 3, "BasePrice": 2000.00, "LineTotal": 6000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 9600.00, "ComboId": null, "Quantity": 3, "BasePrice": 3200.00, "LineTotal": 9600.00, "MenuItemId": 42, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 24, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-12-01 23:01:59.312003+00
193	68	mark_delivered	\N	[{"OrderItemId": 145, "DeliveredQuantity": 3}, {"OrderItemId": 146, "DeliveredQuantity": 3}, {"OrderItemId": 147, "DeliveredQuantity": 1}]	\N	2025-12-01 23:02:03.71805+00
194	69	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "d6454d71-f6de-4cd9-bd7f-2567bcb85f3b", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 23:06:25.652135+00
195	69	close	\N	{"Status": "closed"}	\N	2025-12-01 23:06:50.923137+00
196	70	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 2", "Subtotal": 0, "TaxTotal": 0, "SessionId": "e5f9e22f-ffe4-4685-93ad-b1a95b919ac8", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-01 23:14:46.093752+00
197	70	add_items	\N	[{"Id": 0, "Profit": 5000.00, "ComboId": null, "Quantity": 2, "BasePrice": 2500.00, "LineTotal": 5000.00, "MenuItemId": 17, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2000.00, "ComboId": null, "Quantity": 1, "BasePrice": 2000.00, "LineTotal": 2000.00, "MenuItemId": 31, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 15000.00, "ComboId": null, "Quantity": 1, "BasePrice": 15000.00, "LineTotal": 15000.00, "MenuItemId": 9, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 4500.00, "ComboId": null, "Quantity": 1, "BasePrice": 4500.00, "LineTotal": 4500.00, "MenuItemId": 12, "PriceDelta": 0, "DeliveredQuantity": 0}]	\N	2025-12-01 23:15:20.420648+00
198	70	close	\N	{"Status": "closed"}	\N	2025-12-01 23:15:27.265357+00
199	71	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 1", "Subtotal": 0, "TaxTotal": 0, "SessionId": "4dc6a927-bf18-45b5-b38e-106ecd69fd6c", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-02 04:59:59.105252+00
200	71	close	\N	{"Status": "closed"}	\N	2025-12-02 05:40:56.058216+00
201	72	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 5", "Subtotal": 0, "TaxTotal": 0, "SessionId": "278e7d89-6de1-4c5a-b9e7-6b482f3caa11", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	a040f705-2252-4218-a15f-e55255416067	2025-12-03 01:13:54.265495+00
202	73	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Billiard 3", "Subtotal": 0, "TaxTotal": 0, "SessionId": "5254dc87-4642-4457-a980-608248e5adc9", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	a040f705-2252-4218-a15f-e55255416067	2025-12-03 01:13:59.23888+00
203	74	create	\N	{"Id": 0, "Items": [], "Total": 0, "Status": "open", "TableId": "Bar 6", "Subtotal": 0, "TaxTotal": 0, "SessionId": "a5025ba4-94bc-4758-bf5c-55ded5ad2292", "ProfitTotal": 0, "DiscountTotal": 0, "DeliveryStatus": "pending"}	63dcc276-e917-4c2d-9c21-29135d86b009	2025-12-03 01:14:03.590085+00
204	75	create	\N	{"Id": 0, "Items": [{"Id": 0, "Profit": 6000.00, "ComboId": null, "Quantity": 1, "BasePrice": 6000.00, "LineTotal": 6000.00, "MenuItemId": 8, "PriceDelta": 0, "DeliveredQuantity": 0}, {"Id": 0, "Profit": 2.00, "ComboId": null, "Quantity": 1, "BasePrice": 2.50, "LineTotal": 2.50, "MenuItemId": 1, "PriceDelta": 0, "DeliveredQuantity": 0}], "Total": 6002.50, "Status": "open", "TableId": "Billiard 3", "Subtotal": 6002.50, "TaxTotal": 0, "SessionId": "5254dc87-4642-4457-a980-608248e5adc9", "ProfitTotal": 6002.00, "DiscountTotal": 0, "DeliveryStatus": "pending"}	1	2025-12-03 01:23:04.849211+00
205	72	mark_delivered	\N	[]	\N	2025-12-03 01:23:27.93096+00
206	75	mark_delivered	\N	[{"OrderItemId": 152, "DeliveredQuantity": 1}, {"OrderItemId": 153, "DeliveredQuantity": 1}]	\N	2025-12-03 01:23:30.053875+00
207	73	mark_delivered	\N	[]	\N	2025-12-03 01:23:31.625503+00
208	74	mark_delivered	\N	[]	\N	2025-12-03 01:23:33.118858+00
\.


--
-- TOC entry 4551 (class 0 OID 16720)
-- Dependencies: 242
-- Data for Name: orders; Type: TABLE DATA; Schema: ord; Owner: -
--

COPY ord.orders (order_id, session_id, billing_id, table_id, server_id, server_name, status, is_deleted, subtotal, discount_total, tax_total, total, profit_total, created_at, updated_at, closed_at, delivery_status) FROM stdin;
11	fa4dac24-58f5-4905-9e02-a14ea6ae3228	c22cbefc-c7d2-473e-8ee5-cf383b34bcfc	Billiard 3			open	f	0.00	0.00	0.00	0.00	0.00	2025-09-11 15:52:24.357843+00	2025-09-11 15:52:24.357843+00	\N	pending
12	c4aa67b1-5edb-462a-b2aa-bf76ecb792b8	26c8cc4d-f4a7-4e9d-bf6d-662beb512597	Billiard 3			open	f	13.48	0.00	0.00	13.48	10.58	2025-09-11 16:21:41.036153+00	2025-09-11 16:21:47.480393+00	\N	pending
13	d9cfdee8-ab41-4afb-843d-030cf97fd210	9bcf8350-cc7d-42d8-bfcb-f042a8b4be28	Billiard 4			open	f	0.00	0.00	0.00	0.00	0.00	2025-09-11 16:22:18.216122+00	2025-09-11 16:22:18.216122+00	\N	pending
39	f03a977d-db67-4ba9-b6ed-27d5075475f9	f24ffa00-5542-4b85-8a08-148c4975d186	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 01:31:28.205814+00	2025-09-14 01:34:46.922518+00	2025-09-14 01:34:46.922518+00	pending
14	ddbd2da8-4e07-4e6e-9e1a-59ebefaa633a	9ea58b3f-a599-49d2-96b0-a1c910008cc3	Billiard 3			closed	f	13.48	0.00	0.00	13.48	10.58	2025-09-11 16:31:25.076819+00	2025-09-11 16:31:34.12065+00	2025-09-11 16:31:34.12065+00	pending
15	321db6d8-290b-47cd-ae79-72cf3ab060b0	a6f79f76-78dd-4aca-a4de-350c5500ab4c	Billiard 1			closed	f	5.49	0.00	0.00	5.49	4.59	2025-09-11 16:43:47.574495+00	2025-09-11 16:45:01.075746+00	2025-09-11 16:44:31.940707+00	pending
16	1d699cd6-2056-4756-84a5-fa056fbcb04d	f5788bce-7ba4-4eac-b415-defdb257d88d	Billiard 2			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-11 20:45:05.777619+00	2025-09-11 20:46:02.008407+00	2025-09-11 20:46:02.008407+00	pending
20	1d699cd6-2056-4756-84a5-fa056fbcb04d	\N	Billiard 2			closed	f	13.48	0.00	0.00	13.48	10.58	2025-09-12 00:35:44.113477+00	2025-09-12 18:01:53.964308+00	2025-09-12 18:01:53.964308+00	pending
19	1d699cd6-2056-4756-84a5-fa056fbcb04d	\N	Billiard 2			closed	f	7.99	0.00	0.00	7.99	5.99	2025-09-12 00:19:35.708464+00	2025-09-12 18:01:54.043542+00	2025-09-12 18:01:54.043542+00	pending
18	1d699cd6-2056-4756-84a5-fa056fbcb04d	\N	Billiard 2			closed	f	7.99	0.00	0.00	7.99	5.99	2025-09-12 00:19:32.685913+00	2025-09-12 18:01:54.078466+00	2025-09-12 18:01:54.078466+00	pending
17	1d699cd6-2056-4756-84a5-fa056fbcb04d	\N	Billiard 2			closed	f	7.99	0.00	0.00	7.99	5.99	2025-09-12 00:18:25.667207+00	2025-09-12 18:01:54.109952+00	2025-09-12 18:01:54.109952+00	pending
21	41a13af8-7b2d-4cf8-85cc-e60a60a6708a	1e2ea83a-e676-49f6-9ca5-07c656bd2acc	Billiard 3			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 18:02:18.343391+00	2025-09-12 18:02:33.36932+00	2025-09-12 18:02:33.36932+00	pending
48	f191267d-2aac-48df-9a13-2b4a2ebf7f9f	a7709d25-3af4-4db8-ae17-297e13153340	Billiard 5			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 02:34:10.56421+00	2025-09-14 17:05:18.300032+00	2025-09-14 17:05:18.300032+00	pending
32	b40af7ae-a081-474b-9e66-29cf43a75a89	\N	Billiard 1			delivered	f	17300.00	0.00	0.00	17300.00	17300.00	2025-09-12 21:13:06.890032+00	2025-09-12 22:02:39.601937+00	\N	delivered
33	b40af7ae-a081-474b-9e66-29cf43a75a89	\N	Billiard 1			delivered	f	13500.00	0.00	0.00	13500.00	13500.00	2025-09-12 21:26:08.137188+00	2025-09-12 22:02:46.221942+00	\N	delivered
34	1922dc18-b808-46c3-9d00-74c29e6c8a74	0018a7b1-f832-434d-83c7-1bba960db532	Billiard 2			delivered	f	13500.00	0.00	0.00	13500.00	13500.00	2025-09-12 21:26:24.325377+00	2025-09-12 22:02:46.221942+00	\N	delivered
22	960f44f6-6680-47ac-bca8-103aff4ddab4	a075601c-cd28-42a2-a234-47bf70cb76e9	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 18:21:14.484691+00	2025-09-12 18:22:01.822327+00	2025-09-12 18:22:01.822327+00	pending
41	73431e13-879e-4b8a-aa58-f146486f87a1	b595a49e-22cc-4709-9c2c-7a8370bc849f	Billiard 1			closed	f	9000.00	0.00	0.00	9000.00	9000.00	2025-09-14 01:36:43.156533+00	2025-09-14 01:37:00.93224+00	2025-09-14 01:37:00.93224+00	pending
23	2dbd064f-2bba-4e26-979a-6b563f4e8006	988646ea-63ec-461a-aac1-ed8478e6d816	Billiard 4			closed	f	10800.00	0.00	0.00	10800.00	10800.00	2025-09-12 18:21:18.275835+00	2025-09-12 18:22:51.265646+00	2025-09-12 18:22:05.159272+00	partial
24	d9c22572-31e8-4c5a-af78-66468b5505b3	e4e83e38-1d36-4621-9ef7-5d6000733c7a	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 18:25:26.791398+00	2025-09-12 18:25:28.67427+00	2025-09-12 18:25:28.67427+00	pending
25	575071d5-b6c3-4ebf-bd7c-2da9495f1bd7	fce0d9b4-3543-433f-b7b0-f0f7fec9f0b4	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 18:34:43.829768+00	2025-09-12 18:34:47.534088+00	2025-09-12 18:34:47.534088+00	pending
26	156afeca-8116-4674-86da-e3a1fee72f3c	52953c22-5d7c-4485-ac54-7ef102343a09	Billiard 2			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 18:49:57.540035+00	2025-09-12 18:50:03.491234+00	2025-09-12 18:50:03.491234+00	pending
27	38e77108-33e6-4476-8e71-3f8d76e5c933	109fab76-7457-4fbd-acff-7e13cb23df71	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 18:53:45.788821+00	2025-09-12 18:53:49.240834+00	2025-09-12 18:53:49.240834+00	pending
28	c0697c7c-c38c-4a94-ae38-245e7c35a1bc	1f670fdc-361b-43ff-83ae-eeee654aa1ca	Billiard 2			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 19:08:28.866171+00	2025-09-12 19:08:31.184177+00	2025-09-12 19:08:31.184177+00	pending
31	b40af7ae-a081-474b-9e66-29cf43a75a89	577f4776-7f4d-48fd-aa16-338c3821cbec	Billiard 1			closed	f	7315.98	0.00	0.00	7315.98	7311.98	2025-09-12 21:04:27.690633+00	2025-09-12 21:07:06.057846+00	2025-09-12 21:07:04.263889+00	partial
29	a4ada3a6-8dc3-4661-8cf2-836ce1342805	ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d	Billiard 3	PREETI	PREETI	closed	f	32.49	0.00	0.00	32.49	0.00	2025-09-12 19:20:29.647635+00	2025-09-12 19:21:11.092924+00	2025-09-12 19:21:11.092924+00	partial
30	551546cd-3afc-492b-8a31-aa189cd70d8d	42922bf4-2fd8-4e36-940a-83655b1d9364	Billiard 2			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 19:21:25.144224+00	2025-09-12 19:21:27.638053+00	2025-09-12 19:21:27.638053+00	pending
49	16f1957c-26bf-4602-9b4b-bf418eda6213	0b25d958-125a-4050-9353-8aedeead2efb	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 18:12:02.207385+00	2025-09-14 18:12:10.700619+00	2025-09-14 18:12:10.700619+00	pending
42	e18396e6-4787-46c6-aef9-e3af01d7c731	b535503c-089c-40b1-a972-0ef33d0e20f5	Billiard 1			delivered	f	9000.00	0.00	0.00	9000.00	9000.00	2025-09-14 02:15:00.516422+00	2025-09-14 02:15:19.438804+00	\N	delivered
35	1922dc18-b808-46c3-9d00-74c29e6c8a74	\N	Billiard 2			delivered	f	12200.00	0.00	0.00	12200.00	12200.00	2025-09-12 22:03:44.27183+00	2025-09-12 22:10:08.815777+00	\N	delivered
36	7d8fbab1-a573-4f04-b405-67214ac2d0a4	284db248-2200-4ae8-89bc-97ab9dabc217	Billiard 2			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-12 22:12:21.026896+00	2025-09-12 22:12:22.777268+00	2025-09-12 22:12:22.777268+00	pending
37	782045f7-b733-40e2-b44a-44ac373aae48	8ac82625-9dd8-4d5c-8a4e-93583b98653c	Billiard 2			delivered	f	14200.00	0.00	0.00	14200.00	14200.00	2025-09-12 22:12:27.189811+00	2025-09-12 22:12:39.080786+00	\N	delivered
43	1ba86713-fa77-4c6c-b81e-5b7ff14db2ce	c9fd8f2c-edb0-4fed-a1fc-4d052a8d2b4d	Billiard 1			delivered	f	9000.00	0.00	0.00	9000.00	9000.00	2025-09-14 02:23:10.047297+00	2025-09-14 02:28:27.629635+00	\N	delivered
38	d08c98b8-416d-48dd-bf3e-d2470400534c	79d540d5-4e36-4b63-b35e-f5820ed14bf9	Billiard 1			delivered	f	13500.00	0.00	0.00	13500.00	13500.00	2025-09-14 01:19:09.652649+00	2025-09-14 01:19:24.763361+00	\N	delivered
40	f03a977d-db67-4ba9-b6ed-27d5075475f9	\N	Billiard 1			closed	f	9000.00	0.00	0.00	9000.00	9000.00	2025-09-14 01:34:15.06682+00	2025-09-14 01:34:46.882144+00	2025-09-14 01:34:46.882144+00	pending
44	d38dbcdc-cac9-4179-b61c-92ff6982993b	13600b72-da13-4c79-a9ce-6e8e01281da9	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 02:34:00.336858+00	2025-09-14 17:05:07.52234+00	2025-09-14 17:05:07.52234+00	pending
45	34f35a28-2145-4d66-8181-46e03f80ab85	f672d7e9-9439-4700-b27f-6b95dc64cffa	Billiard 2			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 02:34:02.504854+00	2025-09-14 17:05:10.846788+00	2025-09-14 17:05:10.846788+00	pending
46	1918b113-fff0-4e1d-b2ff-99ffcdc2e114	29e474fb-07ca-4b6d-81f0-19de4e2a1b2a	Billiard 3			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 02:34:05.017283+00	2025-09-14 17:05:12.948926+00	2025-09-14 17:05:12.948926+00	pending
47	88c50846-2c42-4537-ae89-d8b0b42bc448	72aaf7c9-c187-45c0-baf9-344280c134d2	Billiard 4			closed	f	0.00	0.00	0.00	0.00	0.00	2025-09-14 02:34:07.755495+00	2025-09-14 17:05:15.891391+00	2025-09-14 17:05:15.891391+00	pending
50	5edeefed-6143-4d2c-8f60-aa62b464a64a	59cbec0b-bb1a-46b1-a469-98fae5487110	Billiard 1			delivered	f	24000.00	0.00	0.00	24000.00	24000.00	2025-09-15 15:42:47.236077+00	2025-09-15 15:43:02.724195+00	\N	delivered
52	45781653-67c0-43e2-b886-7013d58823b6	d3992167-e95b-457b-b441-e9f25e515b3e	Billiard 1			delivered	f	31000.00	0.00	0.00	31000.00	31000.00	2025-09-16 02:55:02.15256+00	2025-09-16 03:00:41.449777+00	\N	delivered
51	bc2c8990-38f6-4ce0-ab0c-4d06c2602c66	469b6915-7087-4dab-a269-918c6b1c12b2	Billiard 1			delivered	f	31000.00	0.00	0.00	31000.00	31000.00	2025-09-16 02:37:33.04152+00	2025-09-16 02:37:50.973259+00	\N	delivered
54	58850a49-b68d-4430-85fb-ab448a301ea8	c0e44df4-8e3c-49e2-9125-062a8924f738	Billiard 1			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-09-18 05:43:32.056842+00	2025-09-18 18:36:54.364411+00	\N	delivered
55	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	153a2d71-36ce-4005-b7e9-7ab692758d67	Billiard 1			waiting	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 18:29:41.052971+00	2025-12-01 18:29:58.953604+00	\N	pending
53	a52c412b-6c0b-4cf5-8499-064559f25fce	d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	Billiard 1			delivered	f	41000.00	0.00	0.00	41000.00	41000.00	2025-09-16 07:44:29.217961+00	2025-09-16 07:45:17.008215+00	\N	delivered
56	7f1557eb-788c-415d-9042-77b02730a7a8	f7764c5b-6ead-4016-bddb-980810bbd83d	Billiard 1			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 22:14:58.183024+00	2025-12-01 22:20:22.942746+00	\N	delivered
61	d8624a3d-8ded-4299-b704-4e1eb98acd31	\N	Billiard 2			delivered	f	15300.00	0.00	0.00	15300.00	15300.00	2025-12-01 22:19:54.087507+00	2025-12-01 22:20:26.361535+00	\N	delivered
57	d8624a3d-8ded-4299-b704-4e1eb98acd31	d0753ff9-9ab3-46d0-b815-a01401af0522	Billiard 2			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 22:15:02.801332+00	2025-12-01 22:20:28.213201+00	\N	delivered
58	1b5446fb-2537-4007-be4c-e4d669f1673e	ba79f68c-66ae-4c09-9d9c-72056ce71b7a	Billiard 4			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 22:15:05.105581+00	2025-12-01 22:20:44.051464+00	\N	delivered
62	6fdcf12e-d92b-431d-a57a-0e1f18fb8857	\N	Billiard 3			delivered	f	30010.00	0.00	0.00	30010.00	30008.00	2025-12-01 22:20:05.936855+00	2025-12-01 22:20:46.23084+00	\N	delivered
59	6fdcf12e-d92b-431d-a57a-0e1f18fb8857	64e3a542-40b3-417d-b4f5-f51515388bf5	Billiard 3			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 22:15:07.353232+00	2025-12-01 22:20:47.736795+00	\N	delivered
63	2a6a3248-0cc4-489f-acfa-d916f3250514	\N	Bar 5			delivered	f	92500.00	0.00	0.00	92500.00	92500.00	2025-12-01 22:20:14.949076+00	2025-12-01 22:20:49.305996+00	\N	delivered
60	2a6a3248-0cc4-489f-acfa-d916f3250514	47cefd76-de17-4861-855f-34571980cf1c	Bar 5			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 22:15:10.227452+00	2025-12-01 22:20:50.986714+00	\N	delivered
64	df6bf801-7846-46cf-9e4d-d535c39c5d6c	fcb295b7-c308-49ae-9c9c-5fe71fcb62e8	Bar 4			delivered	f	85500.00	0.00	0.00	85500.00	85500.00	2025-12-01 22:34:07.935287+00	2025-12-01 22:34:29.962895+00	\N	delivered
68	eaaa4e7c-f743-48f0-ad0d-b317a71c80e1	2ada545d-90aa-4ac9-9a7f-5c669297cf95	Bar 4			delivered	f	20100.00	0.00	0.00	20100.00	20100.00	2025-12-01 23:01:42.003688+00	2025-12-01 23:02:03.725256+00	\N	delivered
69	d6454d71-f6de-4cd9-bd7f-2567bcb85f3b	b766b9a5-86b5-436f-be7d-356480b1d5b6	Bar 3			closed	f	0.00	0.00	0.00	0.00	0.00	2025-12-01 23:06:25.647149+00	2025-12-01 23:06:50.919625+00	2025-12-01 23:06:50.919625+00	pending
65	518e229b-d581-4ce0-87b8-b1c929173542	37337bc6-6bc2-43c2-bd99-4f8d03145b26	Bar 3			delivered	f	16000.00	0.00	0.00	16000.00	16000.00	2025-12-01 22:47:00.215601+00	2025-12-01 22:47:18.960833+00	\N	delivered
66	fa5a5cbb-9e17-4467-ac58-0d4b72a7af7d	a7cb9baf-666c-459e-9ace-8465ee6e2f39	Bar 4			closed	f	13500.00	0.00	0.00	13500.00	13500.00	2025-12-01 22:56:28.234871+00	2025-12-01 22:56:52.807127+00	2025-12-01 22:56:52.807127+00	pending
70	2220669d-830b-4d2e-b5bc-70b9c5185e03	e0296ff2-2401-4e39-8979-fa7f00f04b6c	Bar 2			closed	f	26500.00	0.00	0.00	26500.00	26500.00	2025-12-01 23:14:46.088266+00	2025-12-01 23:15:27.261658+00	2025-12-01 23:15:27.261658+00	pending
67	a42b20eb-6088-4e25-9f7b-b026c718195f	c5fc85ef-f76e-458a-8e25-6ea0a3657d70	Bar 1			delivered	f	72000.00	0.00	0.00	72000.00	72000.00	2025-12-01 22:57:09.61403+00	2025-12-01 22:57:22.349278+00	\N	delivered
71	4dc6a927-bf18-45b5-b38e-106ecd69fd6c	dcadcaee-87dc-46ef-b24b-a6c7f223ee35	Billiard 1			closed	f	0.00	0.00	0.00	0.00	0.00	2025-12-02 04:59:59.086324+00	2025-12-02 05:40:56.035503+00	2025-12-02 05:40:56.035503+00	pending
72	278e7d89-6de1-4c5a-b9e7-6b482f3caa11	acbd2559-b71b-41d7-a633-c9cc0564a114	Billiard 5			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-03 01:13:54.23964+00	2025-12-03 01:23:27.942252+00	\N	delivered
75	5254dc87-4642-4457-a980-608248e5adc9	\N	Billiard 3			delivered	f	6002.50	0.00	0.00	6002.50	6002.00	2025-12-03 01:23:04.839457+00	2025-12-03 01:23:30.061874+00	\N	delivered
73	5254dc87-4642-4457-a980-608248e5adc9	32c39ba8-76be-4698-acbf-1b61597844c1	Billiard 3			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-03 01:13:59.234504+00	2025-12-03 01:23:31.63281+00	\N	delivered
74	a5025ba4-94bc-4758-bf5c-55ded5ad2292	29df8f2f-926d-40ce-8f45-a46afbc32500	Bar 6			delivered	f	0.00	0.00	0.00	0.00	0.00	2025-12-03 01:14:03.58055+00	2025-12-03 01:23:33.126811+00	\N	delivered
\.


--
-- TOC entry 4558 (class 0 OID 17034)
-- Dependencies: 249
-- Data for Name: bill_ledger; Type: TABLE DATA; Schema: pay; Owner: -
--

COPY pay.bill_ledger (billing_id, session_id, total_due, total_discount, total_paid, total_tip, status, updated_at) FROM stdin;
550e8400-e29b-41d4-a716-446655440001	550e8400-e29b-41d4-a716-446655440000	25.00	0.00	50.00	0.00	paid	2025-09-12 04:24:25.256019+00
ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d	a4ada3a6-8dc3-4661-8cf2-836ce1342805	33.76	0.00	33.76	5.00	paid	2025-09-12 19:21:41.258945+00
153a2d71-36ce-4005-b7e9-7ab692758d67	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	10.00	0.00	40.00	0.00	paid	2025-12-01 22:07:15.961546+00
d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	a52c412b-6c0b-4cf5-8499-064559f25fce	5.00	0.00	5.00	0.00	paid	2025-12-01 22:10:00.974694+00
\.


--
-- TOC entry 4560 (class 0 OID 17049)
-- Dependencies: 251
-- Data for Name: payment_logs; Type: TABLE DATA; Schema: pay; Owner: -
--

COPY pay.payment_logs (log_id, billing_id, session_id, action, old_value, new_value, server_id, created_at) FROM stdin;
40	ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d	a4ada3a6-8dc3-4661-8cf2-836ce1342805	payment_completed	{"status": "awaiting_payment", "payment_state": "not-paid"}	{"tip": 5.00, "status": "closed", "amount_paid": 33.76, "payment_state": "paid", "payment_method": "cash"}	MANAGER001	2025-09-12 19:21:33.935472+00
38	550e8400-e29b-41d4-a716-446655440001	550e8400-e29b-41d4-a716-446655440000	register_payment	\N	{"lines": [{"Meta": null, "TipAmount": 0.0, "AmountPaid": 25.0, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0.0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 25.0, "TotalTip": 0.0, "BillingId": "550e8400-e29b-41d4-a716-446655440001", "SessionId": "550e8400-e29b-41d4-a716-446655440000", "TotalPaid": 25.0, "TotalDiscount": 0.0}}	\N	2025-09-12 04:24:20.353686+00
39	550e8400-e29b-41d4-a716-446655440001	550e8400-e29b-41d4-a716-446655440000	register_payment	{"Status": "paid", "TotalDue": 25.00, "TotalTip": 0.00, "BillingId": "550e8400-e29b-41d4-a716-446655440001", "SessionId": "550e8400-e29b-41d4-a716-446655440000", "TotalPaid": 25.00, "TotalDiscount": 0.00}	{"lines": [{"Meta": null, "TipAmount": 0.0, "AmountPaid": 25.0, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0.0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 25.00, "TotalTip": 0.00, "BillingId": "550e8400-e29b-41d4-a716-446655440001", "SessionId": "550e8400-e29b-41d4-a716-446655440000", "TotalPaid": 50.00, "TotalDiscount": 0.00}}	\N	2025-09-12 04:24:25.256019+00
41	153a2d71-36ce-4005-b7e9-7ab692758d67	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	register_payment	\N	{"lines": [{"Meta": null, "TipAmount": 0, "AmountPaid": 10.00, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 10.00, "TotalTip": 0, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 10.00, "TotalDiscount": 0}}	\N	2025-12-01 22:03:12.982258+00
42	153a2d71-36ce-4005-b7e9-7ab692758d67	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	register_payment	{"Status": "paid", "TotalDue": 10.00, "TotalTip": 0.00, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 10.00, "TotalDiscount": 0.00}	{"lines": [{"Meta": null, "TipAmount": 0, "AmountPaid": 10.00, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 10.00, "TotalTip": 0.00, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 20.00, "TotalDiscount": 0.00}}	\N	2025-12-01 22:03:38.205477+00
43	153a2d71-36ce-4005-b7e9-7ab692758d67	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	register_payment	{"Status": "paid", "TotalDue": 10.00, "TotalTip": 0.00, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 20.00, "TotalDiscount": 0.00}	{"lines": [{"Meta": null, "TipAmount": 0, "AmountPaid": 10.00, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 10.00, "TotalTip": 0.00, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 30.00, "TotalDiscount": 0.00}}	\N	2025-12-01 22:06:48.41932+00
44	153a2d71-36ce-4005-b7e9-7ab692758d67	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	register_payment	{"Status": "paid", "TotalDue": 10.00, "TotalTip": 0.00, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 30.00, "TotalDiscount": 0.00}	{"lines": [{"Meta": null, "TipAmount": 0, "AmountPaid": 10.00, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 10.00, "TotalTip": 0.00, "BillingId": "153a2d71-36ce-4005-b7e9-7ab692758d67", "SessionId": "e5bf3a57-863e-40e2-b7b7-aa21f6830e9e", "TotalPaid": 40.00, "TotalDiscount": 0.00}}	\N	2025-12-01 22:07:15.961546+00
45	d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	a52c412b-6c0b-4cf5-8499-064559f25fce	register_payment	\N	{"lines": [{"Meta": null, "TipAmount": 0, "AmountPaid": 5.00, "ExternalRef": null, "PaymentMethod": "Cash", "DiscountAmount": 0, "DiscountReason": null}], "ledger": {"Status": "paid", "TotalDue": 5.00, "TotalTip": 0, "BillingId": "d6b51d97-b72a-49db-a9ee-ea7bfc7fc519", "SessionId": "a52c412b-6c0b-4cf5-8499-064559f25fce", "TotalPaid": 5.00, "TotalDiscount": 0}}	\N	2025-12-01 22:10:00.974694+00
\.


--
-- TOC entry 4557 (class 0 OID 17018)
-- Dependencies: 248
-- Data for Name: payments; Type: TABLE DATA; Schema: pay; Owner: -
--

COPY pay.payments (payment_id, session_id, billing_id, amount_paid, currency, payment_method, discount_amount, discount_reason, tip_amount, external_ref, meta, created_by, created_at) FROM stdin;
37	550e8400-e29b-41d4-a716-446655440000	550e8400-e29b-41d4-a716-446655440001	25.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-09-12 04:24:20.353686+00
38	550e8400-e29b-41d4-a716-446655440000	550e8400-e29b-41d4-a716-446655440001	25.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-09-12 04:24:25.256019+00
39	a4ada3a6-8dc3-4661-8cf2-836ce1342805	ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d	33.76	USD	cash	0.00	\N	5.00	CASH_PAYMENT_1757704885	{"manager": "MANAGER001", "location": "main_bar", "receipt_printed": true}	MANAGER001	2025-09-12 19:21:25.005933+00
40	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	153a2d71-36ce-4005-b7e9-7ab692758d67	10.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-12-01 22:03:12.982258+00
41	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	153a2d71-36ce-4005-b7e9-7ab692758d67	10.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-12-01 22:03:38.205477+00
42	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	153a2d71-36ce-4005-b7e9-7ab692758d67	10.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-12-01 22:06:48.41932+00
43	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	153a2d71-36ce-4005-b7e9-7ab692758d67	10.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-12-01 22:07:15.961546+00
44	a52c412b-6c0b-4cf5-8499-064559f25fce	d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	5.00	USD	Cash	0.00	\N	0.00	\N	\N	\N	2025-12-01 22:10:00.974694+00
\.


--
-- TOC entry 4586 (class 0 OID 354486)
-- Dependencies: 278
-- Data for Name: refunds; Type: TABLE DATA; Schema: pay; Owner: -
--

COPY pay.refunds (refund_id, payment_id, billing_id, session_id, refund_amount, refund_reason, refund_method, external_ref, meta, created_by, created_at) FROM stdin;
\.


--
-- TOC entry 4536 (class 0 OID 16588)
-- Dependencies: 227
-- Data for Name: app_settings; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.app_settings (key, value) FROM stdin;
Layout.GridSize	20
Layout.SnapToGrid	true
Layout.DefaultTableSize	M
Layout.ShowTableNumbers	true
Layout.FloorSwitchLock	false
Layout.ProtectLayoutChanges	false
\.


--
-- TOC entry 4578 (class 0 OID 165232)
-- Dependencies: 270
-- Data for Name: billing_sessions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.billing_sessions (billing_id, session_id, created_at) FROM stdin;
\.


--
-- TOC entry 4577 (class 0 OID 165216)
-- Dependencies: 269
-- Data for Name: billings; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.billings (billing_id, customer_name, customer_contact, total_amount, subtotal, tax_amount, discount_amount, status, created_at, updated_at, closed_at, paid_at) FROM stdin;
d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	\N	\N	41000.00	41000.00	0.00	0.00	closed	2025-09-16 07:44:29+00	2025-09-16 07:45:30+00	\N	\N
\.


--
-- TOC entry 4535 (class 0 OID 16575)
-- Dependencies: 226
-- Data for Name: bills; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.bills (bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, created_at, items, time_cost, items_cost, total_amount, billing_id, session_id, status, closed_at, is_settled, payment_state) FROM stdin;
b4bd4ae3-8f69-455b-bbe2-82472788d654	Billiard 3	PREETI	PREETI	2025-09-12 19:20:20.405814+00	2025-09-12 19:21:02.144026+00	1	2025-09-12 19:21:02.144026+00	[{"name": "Premium Beer", "price": 5.50, "total": 11.00, "quantity": 2}, {"name": "Margherita Pizza", "price": 12.99, "total": 12.99, "quantity": 1}, {"name": "Nachos", "price": 8.50, "total": 8.50, "quantity": 1}]	1.27	32.49	33.76	ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d	a4ada3a6-8dc3-4661-8cf2-836ce1342805	closed	2025-09-12 19:21:26.770838+00	t	paid
138ec241-f626-48e8-9563-416c5ba0b2eb	Billiard 1	PREETI	PREETI	2025-09-12 19:08:09.747731+00	2025-09-12 19:24:26.972002+00	1	2025-09-12 19:24:26.972002+00	[]	2.00	0.00	2.00	1377d3a6-03b6-438b-b37a-281f22ae3fa9	14c22d97-198f-4089-a746-8277aaf62ccf	awaiting_payment	\N	f	not-paid
e5b8773e-ecc8-4293-9d11-3b4b8346b17b	Billiard 2	SYSTEM	PREETI	2025-09-12 19:39:18.221082+00	2025-09-12 19:39:18.312495+00	0	2025-09-12 19:39:18.326737+00	[]	0.00	0	0.00	5f2cd089-6c3d-4b3a-b9c2-efb1bde1a74a	7fab8f0d-f78d-45f9-b2f4-8c7264d6b0b1	awaiting_payment	\N	f	not-paid
bc14579a-7896-4827-95d8-e2d85a0794c7	Billiard 1	SYSTEM	PREETI	2025-09-12 19:08:09.747731+00	2025-09-12 19:39:31.964507+00	31	2025-09-12 19:39:31.969972+00	[]	155.00	0	155.00	1377d3a6-03b6-438b-b37a-281f22ae3fa9	14c22d97-198f-4089-a746-8277aaf62ccf	awaiting_payment	\N	f	not-paid
22c7ecd5-300f-4841-b884-93795563305c	Billiard 2	Preeti	PREETI	2025-09-12 21:26:24.170764+00	2025-09-12 22:12:12.284489+00	45	2025-09-12 22:12:12.300499+00	[]	225.00	0	225.00	0018a7b1-f832-434d-83c7-1bba960db532	1922dc18-b808-46c3-9d00-74c29e6c8a74	awaiting_payment	\N	f	not-paid
e6917e4d-c284-4269-a107-cddfb3595226	Billiard 1	Preeti	PREETI	2025-09-12 21:04:25.826846+00	2025-09-12 22:12:16.568172+00	67	2025-09-12 22:12:16.571202+00	[]	335.00	0	335.00	577f4776-7f4d-48fd-aa16-338c3821cbec	b40af7ae-a081-474b-9e66-29cf43a75a89	awaiting_payment	\N	f	not-paid
512456b9-f6d0-453d-ba7d-f8b70139dd37	Billiard 2	Preeti	PREETI	2025-09-12 22:12:20.839404+00	2025-09-12 22:12:23.018761+00	0	2025-09-12 22:12:23.021406+00	[]	0.00	0	0.00	284db248-2200-4ae8-89bc-97ab9dabc217	7d8fbab1-a573-4f04-b405-67214ac2d0a4	awaiting_payment	\N	f	not-paid
42c5cac0-200c-40b0-964e-fb4a9f1213ce	Billiard 2	Preeti	PREETI	2025-09-12 22:12:27.047936+00	2025-09-12 22:37:45.685653+00	25	2025-09-12 22:37:45.689091+00	[]	125.00	0	125.00	8ac82625-9dd8-4d5c-8a4e-93583b98653c	782045f7-b733-40e2-b44a-44ac373aae48	awaiting_payment	\N	f	not-paid
f80298b8-2b52-4138-95cb-621cb5f94491	Billiard 1	a040f705-2252-4218-a15f-e55255416067	server2	2025-09-14 01:19:07.861589+00	2025-09-14 01:19:32.237143+00	0	2025-09-14 01:19:32.247819+00	[]	0.00	0	0.00	79d540d5-4e36-4b63-b35e-f5820ed14bf9	d08c98b8-416d-48dd-bf3e-d2470400534c	awaiting_payment	\N	f	not-paid
61b1d7d5-d23a-4965-ab9f-d4389d591287	Billiard 1	a040f705-2252-4218-a15f-e55255416067	server2	2025-09-14 01:31:27.854495+00	2025-09-14 01:34:47.159863+00	3	2025-09-14 01:34:47.163767+00	[]	15.00	0	15.00	f24ffa00-5542-4b85-8a08-148c4975d186	f03a977d-db67-4ba9-b6ed-27d5075475f9	awaiting_payment	\N	f	not-paid
dd626edd-a920-439a-a009-aa72cbe2d430	Billiard 1	c85b2501-68cb-44ea-bb3b-f7a254352471	owner1	2025-09-14 01:36:42.85329+00	2025-09-14 01:37:01.17343+00	0	2025-09-14 01:37:01.177542+00	[]	0.00	0	0.00	b595a49e-22cc-4709-9c2c-7a8370bc849f	73431e13-879e-4b8a-aa58-f146486f87a1	awaiting_payment	\N	f	not-paid
66705d6c-3c94-4db6-ab22-a4f68c1bbf96	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:14:58.756403+00	2025-09-14 02:15:34.487856+00	0	2025-09-14 02:15:34.500539+00	[]	0.00	0	0.00	b535503c-089c-40b1-a972-0ef33d0e20f5	e18396e6-4787-46c6-aef9-e3af01d7c731	awaiting_payment	\N	f	not-paid
aa89c68f-c995-4fed-9161-dd81a4d4bffe	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:23:09.513391+00	2025-09-14 02:28:34.233564+00	5	2025-09-14 02:28:34.237249+00	[]	25.00	0	25.00	c9fd8f2c-edb0-4fed-a1fc-4d052a8d2b4d	1ba86713-fa77-4c6c-b81e-5b7ff14db2ce	awaiting_payment	\N	f	not-paid
9c5e70c3-2f2f-4868-ac07-ddd2a19913f4	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:33:59.991325+00	2025-09-14 17:05:07.851754+00	871	2025-09-14 17:05:07.875492+00	[]	4355.00	0	4355.00	13600b72-da13-4c79-a9ce-6e8e01281da9	d38dbcdc-cac9-4179-b61c-92ff6982993b	awaiting_payment	\N	f	not-paid
dba9449e-eb59-4569-a014-a75b20c06f63	Billiard 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:02.348554+00	2025-09-14 17:05:11.106447+00	871	2025-09-14 17:05:11.110041+00	[]	4355.00	0	4355.00	f672d7e9-9439-4700-b27f-6b95dc64cffa	34f35a28-2145-4d66-8181-46e03f80ab85	awaiting_payment	\N	f	not-paid
1fca1e2c-aea0-4cb5-a586-7374e78da9f6	Billiard 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:04.865737+00	2025-09-14 17:05:13.19174+00	871	2025-09-14 17:05:13.196745+00	[]	4355.00	0	4355.00	29e474fb-07ca-4b6d-81f0-19de4e2a1b2a	1918b113-fff0-4e1d-b2ff-99ffcdc2e114	awaiting_payment	\N	f	not-paid
f0a35464-9526-43c8-a330-0557a17e1660	Billiard 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:07.617965+00	2025-09-14 17:05:16.183599+00	871	2025-09-14 17:05:16.187368+00	[]	4355.00	0	4355.00	72aaf7c9-c187-45c0-baf9-344280c134d2	88c50846-2c42-4537-ae89-d8b0b42bc448	awaiting_payment	\N	f	not-paid
dde31038-0d5c-401f-9793-55df75189e65	Billiard 5	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:10.418047+00	2025-09-14 17:05:18.546821+00	871	2025-09-14 17:05:18.552327+00	[]	4355.00	0	4355.00	a7709d25-3af4-4db8-ae17-297e13153340	f191267d-2aac-48df-9a13-2b4a2ebf7f9f	awaiting_payment	\N	f	not-paid
0b26b2cf-b91a-4d00-9561-f88bd2fd98b7	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 18:12:00.37302+00	2025-09-14 18:12:10.954726+00	0	2025-09-14 18:12:10.965377+00	[]	0.00	0	0.00	0b25d958-125a-4050-9353-8aedeead2efb	16f1957c-26bf-4602-9b4b-bf418eda6213	awaiting_payment	\N	f	not-paid
7d90de3b-9082-49e1-86fb-7aee9679c608	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-15 15:42:46.888707+00	2025-09-15 15:43:22.334235+00	0	2025-09-15 15:43:22.343606+00	[]	0.00	0	0.00	59cbec0b-bb1a-46b1-a469-98fae5487110	5edeefed-6143-4d2c-8f60-aa62b464a64a	awaiting_payment	\N	f	not-paid
99fe8bbd-2a98-46a5-a6b1-c75da33a9a7d	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-16 02:37:30.464113+00	2025-09-16 02:37:55.81924+00	0	2025-09-16 02:37:55.834+00	[]	0.00	0	0.00	469b6915-7087-4dab-a269-918c6b1c12b2	bc2c8990-38f6-4ce0-ab0c-4d06c2602c66	awaiting_payment	\N	f	not-paid
98b1eb6f-a325-474d-aa72-d5bb800ae52c	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-16 02:55:00.245166+00	2025-09-16 03:00:47.858536+00	5	2025-09-16 03:00:47.86904+00	[]	25.00	0	25.00	d3992167-e95b-457b-b441-e9f25e515b3e	45781653-67c0-43e2-b886-7013d58823b6	awaiting_payment	\N	f	not-paid
2b62e015-b772-4a36-9f8e-0bf793410b48	Bar 1	0f3b1931-7f0f-4e16-8d88-9f7d0a1d7f8f	cashier1	2025-09-18 05:43:30.028128+00	2025-09-20 00:13:28.02741+00	2549	2025-09-20 00:13:28.042494+00	[]	12745.00	0	12745.00	c0e44df4-8e3c-49e2-9125-062a8924f738	58850a49-b68d-4430-85fb-ab448a301ea8	awaiting_payment	\N	f	not-paid
b8b3bf5b-8553-4242-8e0f-2c5948695046	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 18:29:38.785775+00	2025-12-01 18:31:49.787584+00	2	2025-12-01 18:31:49.796862+00	[]	10.00	0	10.00	153a2d71-36ce-4005-b7e9-7ab692758d67	e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	awaiting_payment	\N	t	paid
00ea09c5-9d5d-4abe-8d39-25282ee43ab1	Billiard 1	a040f705-2252-4218-a15f-e55255416067	server2	2025-09-16 07:44:27.132671+00	2025-09-16 07:45:30.927244+00	1	2025-09-16 07:45:30.938928+00	[]	5.00	0	5.00	d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	a52c412b-6c0b-4cf5-8499-064559f25fce	awaiting_payment	\N	t	paid
1f88455a-3fee-4334-9dbf-d96d5347f736	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:14:57.817616+00	2025-12-01 22:20:57.984283+00	6	2025-12-01 22:20:57.995934+00	[]	30.00	0	30.00	f7764c5b-6ead-4016-bddb-980810bbd83d	7f1557eb-788c-415d-9042-77b02730a7a8	awaiting_payment	\N	f	not-paid
0ce67aec-d4ef-4c87-9fcd-f892a242dd75	Bar 5	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:10.078873+00	2025-12-01 22:21:19.28739+00	6	2025-12-01 22:21:19.296504+00	[]	30.00	0	30.00	47cefd76-de17-4861-855f-34571980cf1c	2a6a3248-0cc4-489f-acfa-d916f3250514	awaiting_payment	\N	f	not-paid
c42f11a5-feb2-42ac-a542-fd9344d31ee2	Billiard 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:02.669756+00	2025-12-01 22:21:34.910203+00	6	2025-12-01 22:21:34.914494+00	[]	30.00	0	30.00	d0753ff9-9ab3-46d0-b815-a01401af0522	d8624a3d-8ded-4299-b704-4e1eb98acd31	awaiting_payment	\N	f	not-paid
eb3f20d5-10fa-42e3-82c4-02c868bf80a8	Billiard 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:07.032114+00	2025-12-01 22:21:37.793619+00	6	2025-12-01 22:21:37.798634+00	[]	30.00	0	30.00	64e3a542-40b3-417d-b4f5-f51515388bf5	6fdcf12e-d92b-431d-a57a-0e1f18fb8857	awaiting_payment	\N	f	not-paid
8de1c56a-a382-4348-9319-8a7875a56ce6	Billiard 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:04.979252+00	2025-12-01 22:21:40.042628+00	6	2025-12-01 22:21:40.046619+00	[]	30.00	0	30.00	ba79f68c-66ae-4c09-9d9c-72056ce71b7a	1b5446fb-2537-4007-be4c-e4d669f1673e	awaiting_payment	\N	f	not-paid
05bb69b9-a16c-4e50-a3ed-c5c3e18b0798	Bar 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:34:07.715779+00	2025-12-01 22:34:42.15152+00	0	2025-12-01 22:34:42.155796+00	[]	0.00	0	0.00	fcb295b7-c308-49ae-9c9c-5fe71fcb62e8	df6bf801-7846-46cf-9e4d-d535c39c5d6c	awaiting_payment	\N	f	not-paid
2b6f7c40-07cb-4621-99a9-14a52cf48ef2	Bar 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:46:59.897775+00	2025-12-01 22:47:36.599173+00	0	2025-12-01 22:47:36.630065+00	[{"name": "Agua de Coco", "price": 2500.00, "itemId": "17", "quantity": 4}, {"name": "Agua Tónica", "price": 2000.00, "itemId": "31", "quantity": 3}]	0	16000.00	16000.00	37337bc6-6bc2-43c2-bd99-4f8d03145b26	518e229b-d581-4ce0-87b8-b1c929173542	awaiting_payment	\N	f	not-paid
9c3c5132-9c3b-4f19-b018-68178d42c67e	Bar 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:56:27.818009+00	2025-12-01 22:56:53.052024+00	0	2025-12-01 22:56:53.059018+00	[]	0	0	0	a7cb9baf-666c-459e-9ace-8465ee6e2f39	fa5a5cbb-9e17-4467-ac58-0d4b72a7af7d	awaiting_payment	\N	f	not-paid
023ebb05-19c1-402b-b293-792e72e62c33	Bar 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:57:09.48482+00	2025-12-01 22:57:31.325609+00	0	2025-12-01 22:57:31.33422+00	[{"name": "Agua de Coco", "price": 2500.00, "itemId": "17", "quantity": 3}, {"name": "Agua Tónica", "price": 2000.00, "itemId": "31", "quantity": 3}, {"name": "Alitas Buffalo", "price": 15000.00, "itemId": "9", "quantity": 3}, {"name": "Alitas de Pollo", "price": 4500.00, "itemId": "12", "quantity": 3}]	0	72000.00	72000.00	c5fc85ef-f76e-458a-8e25-6ea0a3657d70	a42b20eb-6088-4e25-9f7b-b026c718195f	awaiting_payment	\N	f	not-paid
636f84fd-93e6-4119-af87-2c3e29d3ae0c	Bar 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:01:41.728794+00	2025-12-01 23:02:09.075537+00	0	2025-12-01 23:02:09.082268+00	[{"name": "Agua Tónica", "price": 2000.00, "itemId": "31", "quantity": 3}, {"name": "Austral", "price": 3200.00, "itemId": "42", "quantity": 3}, {"name": "Bloody Mary", "price": 4500.00, "itemId": "24", "quantity": 1}]	0	20100.00	20100.00	2ada545d-90aa-4ac9-9a7f-5c669297cf95	eaaa4e7c-f743-48f0-ad0d-b317a71c80e1	awaiting_payment	\N	f	not-paid
9e65005d-969e-4b4c-8a2d-2ea18164fb44	Bar 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:06:25.395285+00	2025-12-01 23:06:51.153383+00	0	2025-12-01 23:06:51.160469+00	[]	0	0	0	b766b9a5-86b5-436f-be7d-356480b1d5b6	d6454d71-f6de-4cd9-bd7f-2567bcb85f3b	awaiting_payment	\N	f	not-paid
c271531d-e065-40bc-ba02-48631e35e0a1	Bar 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:14:45.796998+00	2025-12-01 23:15:27.475451+00	0	2025-12-01 23:15:27.481332+00	[]	0	0	0	e0296ff2-2401-4e39-8979-fa7f00f04b6c	e5f9e22f-ffe4-4685-93ad-b1a95b919ac8	awaiting_payment	\N	f	not-paid
6b964a15-bd64-40f1-af73-f436a3a0073e	Bar 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-02 00:05:51.246161+00	2025-12-02 03:14:51.318583+00	189	2025-12-02 03:14:51.353429+00	[]	0	0	0	e0296ff2-2401-4e39-8979-fa7f00f04b6c	2220669d-830b-4d2e-b5bc-70b9c5185e03	awaiting_payment	\N	f	not-paid
396e7fe3-9ed8-4c83-a89f-1d64e87e9f6d	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-02 04:59:58.81594+00	2025-12-02 05:40:56.321573+00	40	2025-12-02 05:40:56.338053+00	[]	200.00	0	200.00	dcadcaee-87dc-46ef-b24b-a6c7f223ee35	4dc6a927-bf18-45b5-b38e-106ecd69fd6c	awaiting_payment	\N	f	not-paid
55841a1f-a297-4bcb-8d48-f1153c80b861	Bar 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:48:55.875426+00	2025-12-02 21:02:39.257382+00	1273	2025-12-02 21:02:39.271396+00	[]	0	0	0	e0296ff2-2401-4e39-8979-fa7f00f04b6c	fc86e740-d4c4-4323-9b7c-b25ebeac1c59	awaiting_payment	\N	f	not-paid
29487146-76b4-4f69-941d-af6b8b36e9ce	Billiard 3	a040f705-2252-4218-a15f-e55255416067	server2	2025-12-03 01:13:59.093743+00	2025-12-03 01:23:40.18642+00	9	2025-12-03 01:23:40.215153+00	[{"name": "Cóctel de Camarones", "price": 6000.00, "itemId": "8", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}]	45.00	6002.50	6047.50	32c39ba8-76be-4698-acbf-1b61597844c1	5254dc87-4642-4457-a980-608248e5adc9	awaiting_payment	\N	f	not-paid
0f5e3c0a-1cbe-4a8a-a634-7b6d9d2a5846	Billiard 5	a040f705-2252-4218-a15f-e55255416067	server2	2025-12-03 01:13:54.013734+00	2025-12-03 01:23:43.644686+00	9	2025-12-03 01:23:43.651256+00	[]	45.00	0	45.00	acbd2559-b71b-41d7-a633-c9cc0564a114	278e7d89-6de1-4c5a-b9e7-6b482f3caa11	awaiting_payment	\N	f	not-paid
5ed5dd50-28d3-466f-bdf0-fc6bc8304403	Bar 6	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-03 01:14:03.440279+00	2025-12-03 01:23:46.080955+00	9	2025-12-03 01:23:46.086156+00	[]	0	0	0	29df8f2f-926d-40ce-8f45-a46afbc32500	a5025ba4-94bc-4758-bf5c-55ded5ad2292	awaiting_payment	\N	f	not-paid
\.


--
-- TOC entry 4583 (class 0 OID 277096)
-- Dependencies: 275
-- Data for Name: floors; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.floors (floor_id, floor_name, description, is_default, is_active, display_order, created_at, updated_at) FROM stdin;
92d6cced-c67b-4337-aca1-0c78e3d6c848	Main Floor	Default floor for existing tables	t	t	0	2025-12-02 22:47:31.073598+00	2025-12-02 22:47:31.073598+00
5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza	\N	f	t	1	2025-12-02 23:06:52.837847+00	2025-12-02 23:06:52.837847+00
\.


--
-- TOC entry 4585 (class 0 OID 277144)
-- Dependencies: 277
-- Data for Name: table_layout_history; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.table_layout_history (history_id, floor_id, version_number, layout_data, created_by, created_at, description) FROM stdin;
\.


--
-- TOC entry 4537 (class 0 OID 16595)
-- Dependencies: 228
-- Data for Name: table_session_moves; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.table_session_moves (session_id, from_label, to_label, moved_at) FROM stdin;
b017f0ec-a064-40e2-a65b-b11d5a6a9545	Billiard 1	Bar 10	2025-09-08 16:57:02.598956
372e902c-71ed-406d-8919-2c2cf2444a23	Billiard 2	Bar 2	2025-09-08 16:57:05.662348
2b0ee688-6a81-4750-8e2f-061a60db7f77	Billiard 3	Billiard 1	2025-09-08 16:57:09.71255
58850a49-b68d-4430-85fb-ab448a301ea8	Billiard 1	Bar 1	2025-09-18 05:44:51.08848
\.


--
-- TOC entry 4534 (class 0 OID 16566)
-- Dependencies: 225
-- Data for Name: table_sessions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.table_sessions (session_id, table_label, server_id, server_name, start_time, end_time, status, items, billing_id, last_heartbeat, destination_table_id, moved_at, original_table_id) FROM stdin;
d931258c-4b8a-407d-bbd1-f227b0930e20	Billiard 2	Preeti	PREETI	2025-09-11 15:36:18.161652+00	2025-09-11 15:37:10.77571+00	closed	[{"name": "Classic Burger", "price": 7.99, "itemId": "2", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}, {"name": "French Fries", "price": 2.99, "itemId": "3", "quantity": 1}]	a6887ccf-f358-4534-828e-8ea0c6f5899f	\N	\N	\N	\N
156afeca-8116-4674-86da-e3a1fee72f3c	Billiard 2	Preeti	PREETI	2025-09-12 18:49:57.403318+00	2025-09-12 18:50:03.539502+00	closed	[]	52953c22-5d7c-4485-ac54-7ef102343a09	\N	\N	\N	\N
6bad54c8-7c1e-474e-a4ec-9283cc0c129e	Billiard 3	Preeti	PREETI	2025-09-11 15:40:14.347337+00	2025-09-11 15:40:25.817312+00	closed	[{"name": "Classic Burger", "price": 7.99, "itemId": "2", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}, {"name": "French Fries", "price": 2.99, "itemId": "3", "quantity": 1}]	07d0fa0c-2f8a-479c-bf02-7de4406a5b43	\N	\N	\N	\N
fa4dac24-58f5-4905-9e02-a14ea6ae3228	Billiard 3	Preeti	PREETI	2025-09-11 15:46:16.221157+00	2025-09-11 16:21:33.566506+00	closed	[{"name": "Classic Burger", "price": 7.99, "itemId": "2", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}, {"name": "French Fries", "price": 2.99, "itemId": "3", "quantity": 1}]	c22cbefc-c7d2-473e-8ee5-cf383b34bcfc	\N	\N	\N	\N
38e77108-33e6-4476-8e71-3f8d76e5c933	Billiard 1	Preeti	PREETI	2025-09-12 18:53:45.609253+00	2025-09-12 18:53:49.369255+00	closed	[]	109fab76-7457-4fbd-acff-7e13cb23df71	\N	\N	\N	\N
c4aa67b1-5edb-462a-b2aa-bf76ecb792b8	Billiard 3	Preeti	PREETI	2025-09-11 16:21:39.40444+00	2025-09-11 16:21:54.706024+00	closed	[{"name": "Classic Burger", "price": 7.99, "itemId": "2", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}, {"name": "French Fries", "price": 2.99, "itemId": "3", "quantity": 1}]	26c8cc4d-f4a7-4e9d-bf6d-662beb512597	\N	\N	\N	\N
d9cfdee8-ab41-4afb-843d-030cf97fd210	Billiard 4	Preeti	PREETI	2025-09-11 16:22:18.151188+00	2025-09-11 16:22:20.041065+00	closed	[]	9bcf8350-cc7d-42d8-bfcb-f042a8b4be28	\N	\N	\N	\N
ddbd2da8-4e07-4e6e-9e1a-59ebefaa633a	Billiard 3	Preeti	PREETI	2025-09-11 16:31:24.841713+00	2025-09-11 16:31:34.177251+00	closed	[{"name": "Classic Burger", "price": 7.99, "itemId": "2", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}, {"name": "French Fries", "price": 2.99, "itemId": "3", "quantity": 1}]	9ea58b3f-a599-49d2-96b0-a1c910008cc3	\N	\N	\N	\N
4ad302e5-f054-450f-b1a8-f0e6a7f5db48	Billiard 1	SYSTEM	PREETI	2025-09-12 18:55:02.311298+00	2025-09-12 18:55:02.360379+00	closed	[]	bf7e3164-f07e-41c7-a213-6110f7fb5fb7	\N	\N	\N	\N
321db6d8-290b-47cd-ae79-72cf3ab060b0	Billiard 1	Preeti	PREETI	2025-09-11 16:43:47.339412+00	2025-09-11 16:44:31.994439+00	closed	[{"name": "Classic Burger", "price": 7.99, "itemId": "2", "quantity": 1}, {"name": "Coffee", "price": 2.50, "itemId": "1", "quantity": 1}, {"name": "French Fries", "price": 2.99, "itemId": "3", "quantity": 1}]	a6f79f76-78dd-4aca-a4de-350c5500ab4c	\N	\N	\N	\N
1d699cd6-2056-4756-84a5-fa056fbcb04d	Billiard 2	Preeti	PREETI	2025-09-11 20:45:03.849068+00	2025-09-12 18:01:54.164225+00	closed	[]	f5788bce-7ba4-4eac-b415-defdb257d88d	\N	\N	\N	\N
41a13af8-7b2d-4cf8-85cc-e60a60a6708a	Billiard 3	Preeti	PREETI	2025-09-12 18:02:18.208621+00	2025-09-12 18:02:33.41385+00	closed	[]	1e2ea83a-e676-49f6-9ca5-07c656bd2acc	\N	\N	\N	\N
960f44f6-6680-47ac-bca8-103aff4ddab4	Billiard 1	Preeti	PREETI	2025-09-12 18:21:12.731584+00	2025-09-12 18:22:01.88158+00	closed	[]	a075601c-cd28-42a2-a234-47bf70cb76e9	\N	\N	\N	\N
2dbd064f-2bba-4e26-979a-6b563f4e8006	Billiard 4	Preeti	PREETI	2025-09-12 18:21:18.213353+00	2025-09-12 18:22:05.213866+00	closed	[]	988646ea-63ec-461a-aac1-ed8478e6d816	\N	\N	\N	\N
d9c22572-31e8-4c5a-af78-66468b5505b3	Billiard 1	Preeti	PREETI	2025-09-12 18:25:26.625067+00	2025-09-12 18:25:28.714547+00	closed	[]	e4e83e38-1d36-4621-9ef7-5d6000733c7a	\N	\N	\N	\N
575071d5-b6c3-4ebf-bd7c-2da9495f1bd7	Billiard 1	Preeti	PREETI	2025-09-12 18:34:43.610019+00	2025-09-12 18:34:47.58595+00	closed	[]	fce0d9b4-3543-433f-b7b0-f0f7fec9f0b4	\N	\N	\N	\N
c0697c7c-c38c-4a94-ae38-245e7c35a1bc	Billiard 2	Preeti	PREETI	2025-09-12 19:08:28.752599+00	2025-09-12 19:14:51.440523+00	closed	[]	1f670fdc-361b-43ff-83ae-eeee654aa1ca	\N	\N	\N	\N
a4ada3a6-8dc3-4661-8cf2-836ce1342805	Billiard 3	PREETI	PREETI	2025-09-12 19:20:20.405814+00	2025-09-12 19:21:07.502348+00	closed	[{"name": "Premium Beer", "price": 5.50, "total": 11.00, "quantity": 2}, {"name": "Margherita Pizza", "price": 12.99, "total": 12.99, "quantity": 1}, {"name": "Nachos", "price": 8.50, "total": 8.50, "quantity": 1}]	ae4f5f20-3c7b-4ab2-9d10-3f3b02ff0b2d	2025-09-12 19:21:07.502348+00	\N	\N	\N
551546cd-3afc-492b-8a31-aa189cd70d8d	Billiard 2	Preeti	PREETI	2025-09-12 19:21:24.734184+00	2025-09-12 19:21:27.885246+00	closed	[]	42922bf4-2fd8-4e36-940a-83655b1d9364	\N	\N	\N	\N
9e125d16-4046-4c51-9d10-b00ba92625b6	Billiard 2	SYSTEM	PREETI	2025-09-12 19:22:07.593326+00	2025-09-12 19:22:07.67523+00	closed	[]	7341dc5a-f83c-46b7-a1c9-a9666f23b054	\N	\N	\N	\N
4c614f7c-cc8c-43cd-967a-2ecb7cf5ec21	Billiard 2	SYSTEM	PREETI	2025-09-12 19:22:12.356307+00	2025-09-12 19:22:12.447541+00	closed	[]	230f2a05-1a1c-488c-9c59-86e20b8fec94	\N	\N	\N	\N
a9d18540-b12f-49c8-965b-1c9c1aa61459	Billiard 2	SYSTEM	PREETI	2025-09-12 19:22:15.10812+00	2025-09-12 19:22:15.184618+00	closed	[]	86386226-7c14-4502-9f6a-a9278df7b22a	\N	\N	\N	\N
04c7415b-ed7b-4991-97c3-a0e3aae23fc5	Billiard 2	SYSTEM	PREETI	2025-09-12 19:22:50.634022+00	2025-09-12 19:22:50.717306+00	closed	[]	527780a4-9497-45ea-8a1f-69e3e2668a5c	\N	\N	\N	\N
7fab8f0d-f78d-45f9-b2f4-8c7264d6b0b1	Billiard 2	SYSTEM	PREETI	2025-09-12 19:39:18.221082+00	2025-09-12 19:39:18.312495+00	closed	[]	5f2cd089-6c3d-4b3a-b9c2-efb1bde1a74a	\N	\N	\N	\N
14c22d97-198f-4089-a746-8277aaf62ccf	Billiard 1	SYSTEM	PREETI	2025-09-12 19:08:09.747731+00	2025-09-12 19:39:31.964507+00	closed	[]	1377d3a6-03b6-438b-b37a-281f22ae3fa9	\N	\N	\N	\N
1922dc18-b808-46c3-9d00-74c29e6c8a74	Billiard 2	Preeti	PREETI	2025-09-12 21:26:24.170764+00	2025-09-12 22:12:12.284489+00	closed	[]	0018a7b1-f832-434d-83c7-1bba960db532	\N	\N	\N	\N
b40af7ae-a081-474b-9e66-29cf43a75a89	Billiard 1	Preeti	PREETI	2025-09-12 21:04:25.826846+00	2025-09-12 22:12:16.568172+00	closed	[]	577f4776-7f4d-48fd-aa16-338c3821cbec	\N	\N	\N	\N
7d8fbab1-a573-4f04-b405-67214ac2d0a4	Billiard 2	Preeti	PREETI	2025-09-12 22:12:20.839404+00	2025-09-12 22:12:23.018761+00	closed	[]	284db248-2200-4ae8-89bc-97ab9dabc217	\N	\N	\N	\N
782045f7-b733-40e2-b44a-44ac373aae48	Billiard 2	Preeti	PREETI	2025-09-12 22:12:27.047936+00	2025-09-12 22:37:45.685653+00	closed	[]	8ac82625-9dd8-4d5c-8a4e-93583b98653c	\N	\N	\N	\N
d08c98b8-416d-48dd-bf3e-d2470400534c	Billiard 1	a040f705-2252-4218-a15f-e55255416067	server2	2025-09-14 01:19:07.861589+00	2025-09-14 01:19:32.237143+00	closed	[]	79d540d5-4e36-4b63-b35e-f5820ed14bf9	\N	\N	\N	\N
f03a977d-db67-4ba9-b6ed-27d5075475f9	Billiard 1	a040f705-2252-4218-a15f-e55255416067	server2	2025-09-14 01:31:27.854495+00	2025-09-14 01:34:47.159863+00	closed	[]	f24ffa00-5542-4b85-8a08-148c4975d186	\N	\N	\N	\N
73431e13-879e-4b8a-aa58-f146486f87a1	Billiard 1	c85b2501-68cb-44ea-bb3b-f7a254352471	owner1	2025-09-14 01:36:42.85329+00	2025-09-14 01:37:01.17343+00	closed	[]	b595a49e-22cc-4709-9c2c-7a8370bc849f	\N	\N	\N	\N
e18396e6-4787-46c6-aef9-e3af01d7c731	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:14:58.756403+00	2025-09-14 02:15:34.487856+00	closed	[]	b535503c-089c-40b1-a972-0ef33d0e20f5	\N	\N	\N	\N
1ba86713-fa77-4c6c-b81e-5b7ff14db2ce	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:23:09.513391+00	2025-09-14 02:28:34.233564+00	closed	[]	c9fd8f2c-edb0-4fed-a1fc-4d052a8d2b4d	\N	\N	\N	\N
d38dbcdc-cac9-4179-b61c-92ff6982993b	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:33:59.991325+00	2025-09-14 17:05:07.851754+00	closed	[]	13600b72-da13-4c79-a9ce-6e8e01281da9	\N	\N	\N	\N
34f35a28-2145-4d66-8181-46e03f80ab85	Billiard 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:02.348554+00	2025-09-14 17:05:11.106447+00	closed	[]	f672d7e9-9439-4700-b27f-6b95dc64cffa	\N	\N	\N	\N
1918b113-fff0-4e1d-b2ff-99ffcdc2e114	Billiard 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:04.865737+00	2025-09-14 17:05:13.19174+00	closed	[]	29e474fb-07ca-4b6d-81f0-19de4e2a1b2a	\N	\N	\N	\N
88c50846-2c42-4537-ae89-d8b0b42bc448	Billiard 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:07.617965+00	2025-09-14 17:05:16.183599+00	closed	[]	72aaf7c9-c187-45c0-baf9-344280c134d2	\N	\N	\N	\N
f191267d-2aac-48df-9a13-2b4a2ebf7f9f	Billiard 5	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 02:34:10.418047+00	2025-09-14 17:05:18.546821+00	closed	[]	a7709d25-3af4-4db8-ae17-297e13153340	\N	\N	\N	\N
16f1957c-26bf-4602-9b4b-bf418eda6213	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-14 18:12:00.37302+00	2025-09-14 18:12:10.954726+00	closed	[]	0b25d958-125a-4050-9353-8aedeead2efb	\N	\N	\N	\N
5edeefed-6143-4d2c-8f60-aa62b464a64a	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-15 15:42:46.888707+00	2025-09-15 15:43:22.334235+00	closed	[]	59cbec0b-bb1a-46b1-a469-98fae5487110	\N	\N	\N	\N
bc2c8990-38f6-4ce0-ab0c-4d06c2602c66	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-16 02:37:30.464113+00	2025-09-16 02:37:55.81924+00	closed	[]	469b6915-7087-4dab-a269-918c6b1c12b2	\N	\N	\N	\N
45781653-67c0-43e2-b886-7013d58823b6	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-09-16 02:55:00.245166+00	2025-09-16 03:00:47.858536+00	closed	[]	d3992167-e95b-457b-b441-e9f25e515b3e	\N	\N	\N	\N
a52c412b-6c0b-4cf5-8499-064559f25fce	Billiard 1	a040f705-2252-4218-a15f-e55255416067	server2	2025-09-16 07:44:27.132671+00	2025-09-16 07:45:30.927244+00	closed	[]	d6b51d97-b72a-49db-a9ee-ea7bfc7fc519	\N	\N	\N	\N
58850a49-b68d-4430-85fb-ab448a301ea8	Bar 1	0f3b1931-7f0f-4e16-8d88-9f7d0a1d7f8f	cashier1	2025-09-18 05:43:30.028128+00	2025-09-20 00:13:28.02741+00	closed	[]	c0e44df4-8e3c-49e2-9125-062a8924f738	\N	\N	\N	\N
e5bf3a57-863e-40e2-b7b7-aa21f6830e9e	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 18:29:38.785775+00	2025-12-01 18:31:49.787584+00	closed	[]	153a2d71-36ce-4005-b7e9-7ab692758d67	\N	\N	\N	\N
7f1557eb-788c-415d-9042-77b02730a7a8	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:14:57.817616+00	2025-12-01 22:20:57.984283+00	closed	[]	f7764c5b-6ead-4016-bddb-980810bbd83d	\N	\N	\N	\N
2a6a3248-0cc4-489f-acfa-d916f3250514	Bar 5	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:10.078873+00	2025-12-01 22:21:19.28739+00	closed	[]	47cefd76-de17-4861-855f-34571980cf1c	\N	\N	\N	\N
d8624a3d-8ded-4299-b704-4e1eb98acd31	Billiard 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:02.669756+00	2025-12-01 22:21:34.910203+00	closed	[]	d0753ff9-9ab3-46d0-b815-a01401af0522	\N	\N	\N	\N
6fdcf12e-d92b-431d-a57a-0e1f18fb8857	Billiard 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:07.032114+00	2025-12-01 22:21:37.793619+00	closed	[]	64e3a542-40b3-417d-b4f5-f51515388bf5	\N	\N	\N	\N
1b5446fb-2537-4007-be4c-e4d669f1673e	Billiard 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:15:04.979252+00	2025-12-01 22:21:40.042628+00	closed	[]	ba79f68c-66ae-4c09-9d9c-72056ce71b7a	\N	\N	\N	\N
df6bf801-7846-46cf-9e4d-d535c39c5d6c	Bar 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:34:07.715779+00	2025-12-01 22:34:42.15152+00	closed	[]	fcb295b7-c308-49ae-9c9c-5fe71fcb62e8	\N	\N	\N	\N
518e229b-d581-4ce0-87b8-b1c929173542	Bar 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:46:59.897775+00	2025-12-01 22:47:36.599173+00	closed	[]	37337bc6-6bc2-43c2-bd99-4f8d03145b26	\N	\N	\N	\N
fa5a5cbb-9e17-4467-ac58-0d4b72a7af7d	Bar 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:56:27.818009+00	2025-12-01 22:56:53.052024+00	closed	[]	a7cb9baf-666c-459e-9ace-8465ee6e2f39	\N	\N	\N	\N
a42b20eb-6088-4e25-9f7b-b026c718195f	Bar 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 22:57:09.48482+00	2025-12-01 22:57:31.325609+00	closed	[]	c5fc85ef-f76e-458a-8e25-6ea0a3657d70	\N	\N	\N	\N
eaaa4e7c-f743-48f0-ad0d-b317a71c80e1	Bar 4	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:01:41.728794+00	2025-12-01 23:02:09.075537+00	closed	[]	2ada545d-90aa-4ac9-9a7f-5c669297cf95	\N	\N	\N	\N
d6454d71-f6de-4cd9-bd7f-2567bcb85f3b	Bar 3	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:06:25.395285+00	2025-12-01 23:06:51.153383+00	closed	[]	b766b9a5-86b5-436f-be7d-356480b1d5b6	\N	\N	\N	\N
e5f9e22f-ffe4-4685-93ad-b1a95b919ac8	Bar 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:14:45.796998+00	2025-12-01 23:15:27.475451+00	closed	[]	e0296ff2-2401-4e39-8979-fa7f00f04b6c	\N	\N	\N	\N
2220669d-830b-4d2e-b5bc-70b9c5185e03	Bar 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-02 00:05:51.246161+00	2025-12-02 03:14:51.318583+00	closed	[]	e0296ff2-2401-4e39-8979-fa7f00f04b6c	\N	\N	\N	\N
4dc6a927-bf18-45b5-b38e-106ecd69fd6c	Billiard 1	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-02 04:59:58.81594+00	2025-12-02 05:40:56.321573+00	closed	[]	dcadcaee-87dc-46ef-b24b-a6c7f223ee35	\N	\N	\N	\N
fc86e740-d4c4-4323-9b7c-b25ebeac1c59	Bar 2	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-01 23:48:55.875426+00	2025-12-02 21:02:39.257382+00	closed	[]	e0296ff2-2401-4e39-8979-fa7f00f04b6c	\N	\N	\N	\N
5254dc87-4642-4457-a980-608248e5adc9	Billiard 3	a040f705-2252-4218-a15f-e55255416067	server2	2025-12-03 01:13:59.093743+00	2025-12-03 01:23:40.18642+00	closed	[]	32c39ba8-76be-4698-acbf-1b61597844c1	\N	\N	\N	\N
278e7d89-6de1-4c5a-b9e7-6b482f3caa11	Billiard 5	a040f705-2252-4218-a15f-e55255416067	server2	2025-12-03 01:13:54.013734+00	2025-12-03 01:23:43.644686+00	closed	[]	acbd2559-b71b-41d7-a633-c9cc0564a114	\N	\N	\N	\N
a5025ba4-94bc-4758-bf5c-55ded5ad2292	Bar 6	63dcc276-e917-4c2d-9c21-29135d86b009	admin	2025-12-03 01:14:03.440279+00	2025-12-03 01:23:46.080955+00	closed	[]	29df8f2f-926d-40ce-8f45-a46afbc32500	\N	\N	\N	\N
\.


--
-- TOC entry 4533 (class 0 OID 16558)
-- Dependencies: 224
-- Data for Name: table_status; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.table_status (label, type, occupied, order_id, start_time, server, updated_at) FROM stdin;
Bar 7	bar	f	\N	\N	\N	2025-09-07 21:28:50.634233+00
Bar 8	bar	f	\N	\N	\N	2025-09-07 21:28:50.634233+00
Bar 9	bar	f	\N	\N	\N	2025-09-07 21:28:50.634233+00
Bar 4	bar	f	\N	\N	\N	2025-12-01 23:02:09.08511+00
Bar 3	bar	f	\N	\N	\N	2025-12-01 23:06:51.169184+00
Billiard 1	billiard	f	\N	\N	\N	2025-12-02 05:40:56.341069+00
Bar 2	bar	f	\N	\N	\N	2025-12-02 21:02:39.273744+00
Terraza 1	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Terraza 2	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Bar 10	bar	f	\N	\N	\N	2025-09-08 17:27:46.045953+00
Terraza 3	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Terraza 4	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Terraza 6	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Terraza 7	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Terraza 8	bar	f	\N	\N	\N	2025-12-03 00:57:16.334362+00
Billiard 3	billiard	f	\N	\N	\N	2025-12-03 01:23:40.218083+00
Billiard 5	billiard	f	\N	\N	\N	2025-12-03 01:23:43.653505+00
Bar 6	bar	f	\N	\N	\N	2025-12-03 01:23:46.088232+00
Terraza 5	bar	f	\N	\N	\N	2025-12-02 23:08:43.974527+00
Bar 5	bar	f	\N	\N	\N	2025-12-01 22:21:19.299806+00
Billiard 2	billiard	f	\N	\N	\N	2025-12-01 22:21:34.916518+00
Billiard 4	billiard	f	\N	\N	\N	2025-12-01 22:21:40.04858+00
Bar 1	bar	f	\N	\N	\N	2025-12-01 22:57:31.337156+00
\.


--
-- TOC entry 4584 (class 0 OID 277113)
-- Dependencies: 276
-- Data for Name: tables; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.tables (table_id, floor_id, table_name, table_number, table_type, x_position, y_position, rotation, size, width, height, status, billing_rate, auto_start_timer, icon_style, grouping_tags, is_active, is_locked, order_id, start_time, server, created_at, updated_at) FROM stdin;
55017140-90d6-4a66-afea-27148bbca06b	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 7	Bar 7	bar	240.00	140.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.795828+00
1b093c8e-841e-42f7-a026-13273229cb92	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 8	Bar 8	bar	240.00	20.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.873469+00
faa76b4a-b317-4422-a4ad-9d00b0ed0e06	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 9	Bar 9	bar	1280.00	760.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.950267+00
c4f9f906-ac16-4128-abc8-24fa499e86c5	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 2	Bar 2	bar	240.00	560.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 01:24:56.874265+00
83c9cb06-2c25-45d9-93ea-c830651097fb	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 10	Bar 10	bar	1160.00	760.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:12:51.42507+00
a3996612-a527-43f2-bb3e-b8c6fd8960b6	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 1	\N	bar	320.00	720.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:07:08.200877+00	2025-12-02 23:08:43.649318+00
6d07ee3f-78b0-4d2b-8ee2-832a8edb3b69	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 2	\N	bar	320.00	580.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:07:21.53822+00	2025-12-02 23:08:43.732207+00
2bc9c9dd-c6e5-49d4-b98a-93535b955885	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 3	\N	bar	200.00	440.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:07:36.094333+00	2025-12-02 23:08:43.815646+00
a172320f-d13d-4a1a-a857-92847b160f97	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 4	\N	bar	400.00	440.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:07:56.734381+00	2025-12-02 23:08:43.896036+00
9f27bdfb-95d2-4bfa-b79a-72bf73ab6ff6	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 5	\N	bar	200.00	300.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:08:05.240899+00	2025-12-02 23:08:43.974527+00
39ed7ed3-910a-4703-b2c3-6da135a5ff13	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 6	\N	bar	400.00	300.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:08:11.226138+00	2025-12-02 23:08:44.056552+00
60ac3b96-c667-4e4e-910b-c2d1749c29c8	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 7	\N	bar	300.00	180.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:08:20.329832+00	2025-12-02 23:08:44.135696+00
92461ec5-59dd-4a30-bd06-9a560da12fa1	5f81e4e8-45ff-442d-abfb-e2494b8cd87f	Terraza 8	\N	bar	300.00	60.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 23:08:40.214302+00	2025-12-02 23:08:44.215765+00
caff2076-2bdb-45bd-9652-ab5096d9ba67	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 1	Bar 1	bar	1020.00	760.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.204478+00
b2da379d-7b9b-45cf-8244-e5274360c5bf	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 1	Terraza 1	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
f170143c-775a-4208-8564-b7a83799f4da	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 2	Terraza 2	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
20ebc921-3890-4642-8825-1ac8afde42ff	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 3	Terraza 3	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
73105b09-3498-478f-8c5e-6864900c6eea	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 4	Terraza 4	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
45c3a2da-c6c7-40fc-a499-7043ec74fc10	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 6	Bar 6	bar	320.00	280.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 20:50:59.768972+00
ba4d1e39-48fe-458c-b8dc-c82d1281754b	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 3	Bar 3	bar	140.00	420.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.452789+00
dd1e5909-7e53-433e-8579-37145b4f9f23	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 4	Bar 4	bar	320.00	420.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.532374+00
0ec6d139-fe5e-4769-985b-2f7cff691380	92d6cced-c67b-4337-aca1-0c78e3d6c848	Bar 5	Bar 5	bar	140.00	280.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-02 23:09:29.616979+00
2a2a398d-4c1a-4adf-a673-f3a3ca2d6917	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 6	Terraza 6	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
66afd4a5-ea2f-43a2-b1f3-6b3be6d76b2b	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 7	Terraza 7	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
1f959cee-858a-4462-85a9-67efea72729c	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 8	Terraza 8	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
fb60a7a9-77d2-4a42-808d-c2ce3c11c239	92d6cced-c67b-4337-aca1-0c78e3d6c848	Terraza 5	Terraza 5	bar	0.00	0.00	0.00	M	\N	\N	available	0.00	f	\N	\N	t	f	\N	\N	\N	2025-12-03 02:22:17.07696+00	2025-12-03 02:22:17.07696+00
9630ba63-9370-4516-8cad-b737a022feb6	92d6cced-c67b-4337-aca1-0c78e3d6c848	Billiard 3	Billiard 3	billiard	720.00	80.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 20:51:14.346226+00
521b1f3b-2237-4b7a-9a19-76155400c1c0	92d6cced-c67b-4337-aca1-0c78e3d6c848	Billiard 2	Billiard 2	billiard	720.00	200.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 20:51:15.761274+00
f3ab4b46-d2ed-489d-9910-780b1e03208d	92d6cced-c67b-4337-aca1-0c78e3d6c848	Billiard 1	Billiard 1	billiard	720.00	320.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 20:51:17.203544+00
ef328b10-33ee-4f4d-ab4d-6049d402e9da	92d6cced-c67b-4337-aca1-0c78e3d6c848	Billiard 4	Billiard 4	billiard	960.00	80.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 20:51:18.940491+00
9690928f-d036-44b7-8c41-ac25629ff67f	92d6cced-c67b-4337-aca1-0c78e3d6c848	Billiard 5	Billiard 5	billiard	960.00	320.00	0.00	M	\N	\N	available	0.00	f	\N	{}	t	f	\N	\N	\N	2025-12-02 22:47:31.084492+00	2025-12-03 20:51:20.515937+00
\.


--
-- TOC entry 4580 (class 0 OID 179293)
-- Dependencies: 272
-- Data for Name: hierarchical_settings; Type: TABLE DATA; Schema: settings; Owner: -
--

COPY settings.hierarchical_settings (id, host_key, category, settings_json, is_active, created_at, updated_at, created_by, updated_by) FROM stdin;
1	default	general	{"theme": "System", "hostKey": null, "language": "en-US", "timezone": "America/New_York", "businessName": "Test Business", "businessEmail": null, "businessPhone": "+1-555-0123", "businessAddress": "123 Test Street", "businessWebsite": null}	t	2025-09-16 23:04:12.250763+00	2025-09-17 19:34:15.760254+00	\N	\N
2	default	pos	{"tax": {"defaultTaxRate": 8.5, "taxDisplayName": "Tax", "additionalTaxRates": [], "taxInclusivePricing": false}, "shifts": {"shiftEndTime": "21:00:00", "autoCloseShift": false, "shiftStartTime": "09:00:00", "requireShiftReports": true}, "cashDrawer": {"comPort": null, "baudRate": 9600, "autoOpenOnSale": true, "autoOpenOnRefund": true, "requireManagerOverride": false}, "tableLayout": {"ratePerMinute": 0.50, "enableAutoStop": false, "warnAfterMinutes": 30, "showTimerOnTables": true, "autoStopAfterMinutes": 120}}	t	2025-09-16 23:04:12.250763+00	2025-09-17 19:34:15.760254+00	\N	\N
6	default	inventory	{"stock": {"autoDeductStock": true, "enableStockAlerts": true, "lowStockThreshold": 10, "allowNegativeStock": false, "criticalStockThreshold": 5}, "reorder": {"enableAutoReorder": false, "reorderLeadTimeDays": 7, "safetyStockMultiplier": 1.5}, "vendors": {"defaultCurrency": "USD", "defaultPaymentTerms": 30, "requireApprovalForOrders": true}}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
7	default	customers	{"wallet": {"minTopUpAmount": 10, "maxWalletBalance": 1000, "enableWalletSystem": true, "allowNegativeBalance": false, "requireIdForWalletUse": false}, "loyalty": {"pointValue": 0.01, "pointsPerDollar": 1.0, "enableLoyaltyProgram": true, "minPointsForRedemption": 100}, "membership": {"tiers": [], "defaultExpiryDays": 365, "autoRenewMemberships": false, "requireEmailForMembership": true}}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
8	default	payments	{"discounts": {"presetDiscounts": ["5%", "10%", "15%", "20%"], "maxDiscountPercentage": 50, "allowStackingDiscounts": false, "requireManagerApproval": true}, "surcharges": {"enableSurcharges": false, "showSurchargeOnReceipt": true, "cardSurchargePercentage": 2.5}, "splitPayments": {"maxSplitCount": 4, "allowUnevenSplits": true, "enableSplitPayments": true}, "enabledMethods": []}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
3	default	printers	{"jobs": {"timeoutMs": 5000, "maxRetries": 3, "logPrintJobs": true, "maxQueueSize": 50, "queueFailedJobs": true}, "devices": {"defaultComPort": "COM1", "defaultBaudRate": 9600, "availablePrinters": [], "autoDetectPrinters": true}, "kitchen": {"printers": [], "copiesPerOrder": 1, "printTimestamps": true, "printOrderNumbers": true, "printSpecialInstructions": true}, "receipt": {"template": {"showLogo": true, "topMargin": 5, "leftMargin": 2, "rightMargin": 2, "bottomMargin": 5, "footerMessage": "Thank you for your business!", "headerMessage": "", "showItemDetails": true, "showBusinessInfo": true, "showTaxBreakdown": true, "showPaymentMethod": true}, "paperSize": "Microsoft.UI.Xaml.Controls.ComboBoxItem", "printProForma": true, "defaultPrinter": "Brother DCP-1610NW series Printer", "fallbackPrinter": "Brother DCP-1610NW series", "printCustomerCopy": true, "printFinalReceipt": true, "printMerchantCopy": true, "autoPrintOnPayment": true, "previewBeforePrint": true, "copiesForFinalReceipt": 1}}	t	2025-09-16 23:04:12.250763+00	2025-09-17 19:34:15.760254+00	\N	\N
10	default	notifications	{"sms": {"apiKey": "", "provider": "", "apiSecret": "", "enableSms": false, "fromNumber": ""}, "push": {"enablePush": true, "showOrderAlerts": true, "showStockAlerts": true, "showSystemAlerts": true, "showPaymentAlerts": true}, "email": {"useSsl": true, "fromName": "", "password": "", "smtpPort": 587, "username": "", "smtpServer": "", "enableEmail": false, "fromAddress": ""}, "alerts": {"alertVolume": 50, "alertSoundPath": "", "thresholdAlerts": [], "enableSoundAlerts": true}}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
11	default	security	{"rbac": {"allowRoleInheritance": true, "restrictedOperations": [], "enforceRolePermissions": true, "requireManagerOverride": true}, "audit": {"retentionDays": 365, "logDataChanges": true, "logUserActions": true, "logSystemEvents": true, "enableAuditLogging": true}, "login": {"enableTwoFactor": false, "maxLoginAttempts": 5, "passwordExpiryDays": 90, "lockoutDurationMinutes": 15, "requireStrongPasswords": true}, "sessions": {"enableAutoLogout": true, "maxConcurrentSessions": 3, "sessionTimeoutMinutes": 60, "requireReauthForSensitive": true}}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
12	default	integrations	{"api": {"endpoints": [], "defaultRetries": 3, "defaultTimeoutMs": 10000, "enableApiLogging": true}, "crm": {"apiKey": "", "syncOrders": true, "apiEndpoint": "", "crmProvider": "", "enableCrmSync": false, "syncCustomers": true, "syncIntervalMinutes": 60}, "webhooks": {"endpoints": [], "timeoutMs": 10000, "maxRetries": 3, "enableWebhooks": false}, "paymentGateways": {"gateways": [], "defaultGateway": "", "enableTestMode": false}}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
13	default	system	{"logging": {"logLevel": "Information", "logFilePath": "logs/magidesk.log", "maxLogFiles": 10, "logRetentionDays": 30, "maxLogFileSizeMB": 10, "enableFileLogging": true, "enableConsoleLogging": true}, "tracing": {"enableTracing": false, "traceApiCalls": true, "traceDbQueries": false, "tracingEndpoint": "", "traceUserActions": true}, "performance": {"enableResponseCaching": true, "maxConcurrentRequests": 1000, "cacheExpirationMinutes": 15, "enableResponseCompression": true, "databaseConnectionPoolSize": 50}, "backgroundJobs": {"enableBackgroundJobs": true, "backupIntervalMinutes": 240, "cleanupIntervalMinutes": 60, "heartbeatIntervalMinutes": 5}}	t	2025-09-17 03:00:43.264435+00	2025-09-17 19:34:15.760254+00	\N	\N
\.


--
-- TOC entry 4582 (class 0 OID 179308)
-- Dependencies: 274
-- Data for Name: settings_audit; Type: TABLE DATA; Schema: settings; Owner: -
--

COPY settings.settings_audit (id, host_key, action, description, category, changes_json, changed_by, created_at, ip_address, user_agent) FROM stdin;
1	default	schema_created	Hierarchical settings schema created with default values	\N	\N	\N	2025-09-16 23:04:18.139278+00	\N	\N
2	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:00:43.335223+00	\N	\N
3	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:01:44.330926+00	\N	\N
4	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:02:01.627945+00	\N	\N
5	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:02:05.012939+00	\N	\N
6	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:02:05.474346+00	\N	\N
7	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:07:36.790582+00	\N	\N
8	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:13:40.543042+00	\N	\N
9	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 03:13:48.008314+00	\N	\N
10	default	category_updated	Category 'printers' updated	\N	\N	\N	2025-09-17 03:13:50.045445+00	\N	\N
11	default	settings_updated	All settings updated	\N	\N	\N	2025-09-17 19:34:15.781776+00	\N	\N
\.


--
-- TOC entry 4570 (class 0 OID 89838)
-- Dependencies: 262
-- Data for Name: role_inheritance; Type: TABLE DATA; Schema: users; Owner: -
--

COPY users.role_inheritance (child_role_id, parent_role_id) FROM stdin;
\.


--
-- TOC entry 4569 (class 0 OID 89826)
-- Dependencies: 261
-- Data for Name: role_permissions; Type: TABLE DATA; Schema: users; Owner: -
--

COPY users.role_permissions (role_id, permission) FROM stdin;
system-owner	user:view
system-owner	user:create
system-owner	user:update
system-owner	user:delete
system-owner	user:manage_roles
system-owner	order:view
system-owner	order:create
system-owner	order:update
system-owner	order:delete
system-owner	order:cancel
system-owner	order:complete
system-owner	table:view
system-owner	table:manage
system-owner	table:assign
system-owner	table:clear
system-owner	menu:view
system-owner	menu:create
system-owner	menu:update
system-owner	menu:delete
system-owner	menu:price_change
system-owner	payment:view
system-owner	payment:process
system-owner	payment:refund
system-owner	payment:void
system-owner	inventory:view
system-owner	inventory:update
system-owner	inventory:adjust
system-owner	inventory:restock
system-owner	report:view
system-owner	report:sales
system-owner	report:inventory
system-owner	report:user_activity
system-owner	report:export
system-owner	settings:view
system-owner	settings:update
system-owner	settings:system
system-owner	settings:receipt
system-owner	customer:view
system-owner	customer:create
system-owner	customer:update
system-owner	customer:delete
system-owner	reservation:view
system-owner	reservation:create
system-owner	reservation:update
system-owner	reservation:cancel
system-owner	vendor:view
system-owner	vendor:create
system-owner	vendor:update
system-owner	vendor:delete
system-owner	vendor:order
system-owner	session:view
system-owner	session:manage
system-owner	session:close
system-administrator	user:view
system-administrator	user:create
system-administrator	user:update
system-administrator	user:delete
system-administrator	user:manage_roles
system-administrator	order:view
system-administrator	order:create
system-administrator	order:update
system-administrator	order:delete
system-administrator	order:cancel
system-administrator	order:complete
system-administrator	table:view
system-administrator	table:manage
system-administrator	table:assign
system-administrator	table:clear
system-administrator	menu:view
system-administrator	menu:create
system-administrator	menu:update
system-administrator	menu:delete
system-administrator	menu:price_change
system-administrator	payment:view
system-administrator	payment:process
system-administrator	payment:refund
system-administrator	payment:void
system-administrator	inventory:view
system-administrator	inventory:update
system-administrator	inventory:adjust
system-administrator	inventory:restock
system-administrator	report:view
system-administrator	report:sales
system-administrator	report:inventory
system-administrator	report:user_activity
system-administrator	report:export
system-administrator	settings:view
system-administrator	settings:update
system-administrator	settings:system
system-administrator	settings:receipt
system-administrator	customer:view
system-administrator	customer:create
system-administrator	customer:update
system-administrator	customer:delete
system-administrator	reservation:view
system-administrator	reservation:create
system-administrator	reservation:update
system-administrator	reservation:cancel
system-administrator	vendor:view
system-administrator	vendor:create
system-administrator	vendor:update
system-administrator	vendor:delete
system-administrator	vendor:order
system-administrator	session:view
system-administrator	session:manage
system-administrator	session:close
system-manager	order:view
system-manager	order:create
system-manager	order:update
system-manager	order:complete
system-manager	table:view
system-manager	table:manage
system-manager	table:assign
system-manager	table:clear
system-manager	menu:view
system-manager	menu:create
system-manager	menu:update
system-manager	menu:price_change
system-manager	payment:view
system-manager	payment:process
system-manager	payment:refund
system-manager	inventory:view
system-manager	inventory:update
system-manager	inventory:adjust
system-manager	inventory:restock
system-manager	report:view
system-manager	report:sales
system-manager	report:inventory
system-manager	report:export
system-manager	settings:view
system-manager	settings:update
system-manager	customer:view
system-manager	customer:create
system-manager	customer:update
system-manager	reservation:view
system-manager	reservation:create
system-manager	reservation:update
system-manager	reservation:cancel
system-manager	vendor:view
system-manager	vendor:create
system-manager	vendor:update
system-manager	vendor:order
system-manager	session:view
system-manager	session:manage
system-manager	session:close
system-server	order:view
system-server	order:create
system-server	order:update
system-server	order:complete
system-server	table:view
system-server	table:manage
system-server	table:assign
system-server	menu:view
system-server	customer:view
system-server	customer:create
system-server	customer:update
system-server	reservation:view
system-server	reservation:create
system-server	reservation:update
system-server	session:view
system-cashier	order:view
system-cashier	order:update
system-cashier	order:complete
system-cashier	menu:view
system-cashier	payment:view
system-cashier	payment:process
system-cashier	payment:refund
system-cashier	customer:view
system-cashier	customer:create
system-cashier	customer:update
system-cashier	session:view
system-host	table:view
system-host	table:manage
system-host	table:assign
system-host	customer:view
system-host	customer:create
system-host	customer:update
system-host	reservation:view
system-host	reservation:create
system-host	reservation:update
system-host	reservation:cancel
system-host	session:view
\.


--
-- TOC entry 4568 (class 0 OID 89809)
-- Dependencies: 260
-- Data for Name: roles; Type: TABLE DATA; Schema: users; Owner: -
--

COPY users.roles (role_id, name, description, is_system_role, is_active, created_at, updated_at, is_deleted) FROM stdin;
system-owner	Owner	Full system access, business owner	t	t	2025-09-13 17:27:02.976959	2025-09-13 17:27:02.976962	f
system-administrator	Administrator	System administration and user management	t	t	2025-09-13 17:27:02.994604	2025-09-13 17:27:02.994606	f
system-manager	Manager	Store operations, staff management, reports	t	t	2025-09-13 17:27:02.996647	2025-09-13 17:27:02.996649	f
system-server	Server	Order taking, table management, customer service	t	t	2025-09-13 17:27:02.998511	2025-09-13 17:27:02.998513	f
system-cashier	Cashier	Payment processing, basic order operations	t	t	2025-09-13 17:27:03.00051	2025-09-13 17:27:03.000511	f
system-host	Host	Customer seating, reservation management	t	t	2025-09-13 17:27:03.002634	2025-09-13 17:27:03.002635	f
ac5120ba-bfee-4786-8202-b2b7293e2e33	TestRole	Test role for RBAC	f	t	2025-09-13 17:27:26.047041	2025-09-13 17:27:26.04706	f
a8aa24d3-118c-4f48-a589-d02354dff9e4	TestRole2	Test role for RBAC testing	f	t	2025-09-13 17:31:42.411823	2025-09-13 17:31:42.411842	f
\.


--
-- TOC entry 4567 (class 0 OID 84916)
-- Dependencies: 259
-- Data for Name: users; Type: TABLE DATA; Schema: users; Owner: -
--

COPY users.users (user_id, username, password_hash, role, created_at, updated_at, is_active, is_deleted) FROM stdin;
63dcc276-e917-4c2d-9c21-29135d86b009	admin	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Administrator	2025-09-13 16:20:39.07388	2025-09-13 16:24:00.729272	t	f
c85b2501-68cb-44ea-bb3b-f7a254352471	owner1	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Owner	2025-09-13 16:29:32.588482	2025-09-13 16:29:32.588482	t	f
026faf10-542d-45c8-8c38-8accc279575b	manager1	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Manager	2025-09-13 16:29:32.588482	2025-09-13 16:29:32.588482	t	f
a040f705-2252-4218-a15f-e55255416067	server2	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Server	2025-09-13 16:29:32.588482	2025-09-13 16:29:32.588482	t	f
0f3b1931-7f0f-4e16-8d88-9f7d0a1d7f8f	cashier1	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Cashier	2025-09-13 16:29:32.588482	2025-09-13 16:29:32.588482	t	f
71820a91-90fd-48a4-8c7b-979225f7b4ce	host1	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Host	2025-09-13 16:29:32.588482	2025-09-13 16:29:32.588482	t	f
be843758-be20-4b05-8864-da4c398a9b0b	server1	$2a$11$eDyTq4zVC5qpFdCZy2b9NuH9neiw0G.8wUR9DoARHW5WlURcgUqE2	Server	2025-09-13 16:29:32.588482	2025-09-14 00:13:40.658317	f	f
\.


--
-- TOC entry 4604 (class 0 OID 0)
-- Dependencies: 236
-- Name: combos_combo_id_seq; Type: SEQUENCE SET; Schema: menu; Owner: -
--

SELECT pg_catalog.setval('menu.combos_combo_id_seq', 1, true);


--
-- TOC entry 4605 (class 0 OID 0)
-- Dependencies: 239
-- Name: menu_history_history_id_seq; Type: SEQUENCE SET; Schema: menu; Owner: -
--

SELECT pg_catalog.setval('menu.menu_history_history_id_seq', 7, true);


--
-- TOC entry 4606 (class 0 OID 0)
-- Dependencies: 229
-- Name: menu_items_menu_item_id_seq; Type: SEQUENCE SET; Schema: menu; Owner: -
--

SELECT pg_catalog.setval('menu.menu_items_menu_item_id_seq', 45, true);


--
-- TOC entry 4607 (class 0 OID 0)
-- Dependencies: 233
-- Name: modifier_options_option_id_seq; Type: SEQUENCE SET; Schema: menu; Owner: -
--

SELECT pg_catalog.setval('menu.modifier_options_option_id_seq', 3, true);


--
-- TOC entry 4608 (class 0 OID 0)
-- Dependencies: 231
-- Name: modifiers_modifier_id_seq; Type: SEQUENCE SET; Schema: menu; Owner: -
--

SELECT pg_catalog.setval('menu.modifiers_modifier_id_seq', 1, true);


--
-- TOC entry 4609 (class 0 OID 0)
-- Dependencies: 243
-- Name: order_items_order_item_id_seq; Type: SEQUENCE SET; Schema: ord; Owner: -
--

SELECT pg_catalog.setval('ord.order_items_order_item_id_seq', 153, true);


--
-- TOC entry 4610 (class 0 OID 0)
-- Dependencies: 245
-- Name: order_logs_log_id_seq; Type: SEQUENCE SET; Schema: ord; Owner: -
--

SELECT pg_catalog.setval('ord.order_logs_log_id_seq', 208, true);


--
-- TOC entry 4611 (class 0 OID 0)
-- Dependencies: 241
-- Name: orders_order_id_seq; Type: SEQUENCE SET; Schema: ord; Owner: -
--

SELECT pg_catalog.setval('ord.orders_order_id_seq', 75, true);


--
-- TOC entry 4612 (class 0 OID 0)
-- Dependencies: 250
-- Name: payment_logs_log_id_seq; Type: SEQUENCE SET; Schema: pay; Owner: -
--

SELECT pg_catalog.setval('pay.payment_logs_log_id_seq', 45, true);


--
-- TOC entry 4613 (class 0 OID 0)
-- Dependencies: 247
-- Name: payments_payment_id_seq; Type: SEQUENCE SET; Schema: pay; Owner: -
--

SELECT pg_catalog.setval('pay.payments_payment_id_seq', 44, true);


--
-- TOC entry 4614 (class 0 OID 0)
-- Dependencies: 271
-- Name: hierarchical_settings_id_seq; Type: SEQUENCE SET; Schema: settings; Owner: -
--

SELECT pg_catalog.setval('settings.hierarchical_settings_id_seq', 94, true);


--
-- TOC entry 4615 (class 0 OID 0)
-- Dependencies: 273
-- Name: settings_audit_id_seq; Type: SEQUENCE SET; Schema: settings; Owner: -
--

SELECT pg_catalog.setval('settings.settings_audit_id_seq', 11, true);


--
-- TOC entry 4295 (class 2606 OID 127681)
-- Name: customers customers_pkey; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.customers
    ADD CONSTRAINT customers_pkey PRIMARY KEY (customer_id);


--
-- TOC entry 4306 (class 2606 OID 127726)
-- Name: loyalty_transactions loyalty_transactions_pkey; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.loyalty_transactions
    ADD CONSTRAINT loyalty_transactions_pkey PRIMARY KEY (transaction_id);


--
-- TOC entry 4291 (class 2606 OID 127668)
-- Name: membership_levels membership_levels_name_key; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.membership_levels
    ADD CONSTRAINT membership_levels_name_key UNIQUE (name);


--
-- TOC entry 4293 (class 2606 OID 127666)
-- Name: membership_levels membership_levels_pkey; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.membership_levels
    ADD CONSTRAINT membership_levels_pkey PRIMARY KEY (membership_level_id);


--
-- TOC entry 4304 (class 2606 OID 127712)
-- Name: wallet_transactions wallet_transactions_pkey; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.wallet_transactions
    ADD CONSTRAINT wallet_transactions_pkey PRIMARY KEY (transaction_id);


--
-- TOC entry 4300 (class 2606 OID 127699)
-- Name: wallets wallets_customer_id_key; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.wallets
    ADD CONSTRAINT wallets_customer_id_key UNIQUE (customer_id);


--
-- TOC entry 4302 (class 2606 OID 127697)
-- Name: wallets wallets_pkey; Type: CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.wallets
    ADD CONSTRAINT wallets_pkey PRIMARY KEY (wallet_id);


--
-- TOC entry 4287 (class 2606 OID 98371)
-- Name: cash_flow cash_flow_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.cash_flow
    ADD CONSTRAINT cash_flow_pkey PRIMARY KEY (id);


--
-- TOC entry 4244 (class 2606 OID 17382)
-- Name: categories categories_name_key; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.categories
    ADD CONSTRAINT categories_name_key UNIQUE (name);


--
-- TOC entry 4246 (class 2606 OID 17384)
-- Name: categories categories_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (category_id);


--
-- TOC entry 4248 (class 2606 OID 17386)
-- Name: inventory_categories inventory_categories_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_categories
    ADD CONSTRAINT inventory_categories_pkey PRIMARY KEY (category_id);


--
-- TOC entry 4255 (class 2606 OID 17390)
-- Name: inventory_items inventory_items_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_pkey PRIMARY KEY (item_id);


--
-- TOC entry 4257 (class 2606 OID 17392)
-- Name: inventory_items inventory_items_sku_key; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_sku_key UNIQUE (sku);


--
-- TOC entry 4259 (class 2606 OID 17394)
-- Name: inventory_stock inventory_stock_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_stock
    ADD CONSTRAINT inventory_stock_pkey PRIMARY KEY (item_id);


--
-- TOC entry 4262 (class 2606 OID 17396)
-- Name: inventory_transactions inventory_transactions_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_transactions
    ADD CONSTRAINT inventory_transactions_pkey PRIMARY KEY (transaction_id);


--
-- TOC entry 4250 (class 2606 OID 17388)
-- Name: vendors vendors_pkey; Type: CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.vendors
    ADD CONSTRAINT vendors_pkey PRIMARY KEY (vendor_id);


--
-- TOC entry 4215 (class 2606 OID 16688)
-- Name: combos combos_pkey; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.combos
    ADD CONSTRAINT combos_pkey PRIMARY KEY (combo_id);


--
-- TOC entry 4220 (class 2606 OID 16715)
-- Name: menu_history menu_history_pkey; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_history
    ADD CONSTRAINT menu_history_pkey PRIMARY KEY (history_id);


--
-- TOC entry 4205 (class 2606 OID 16618)
-- Name: menu_items menu_items_pkey; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_items
    ADD CONSTRAINT menu_items_pkey PRIMARY KEY (menu_item_id);


--
-- TOC entry 4211 (class 2606 OID 16650)
-- Name: modifier_options modifier_options_pkey; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.modifier_options
    ADD CONSTRAINT modifier_options_pkey PRIMARY KEY (option_id);


--
-- TOC entry 4208 (class 2606 OID 16636)
-- Name: modifiers modifiers_pkey; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.modifiers
    ADD CONSTRAINT modifiers_pkey PRIMARY KEY (modifier_id);


--
-- TOC entry 4217 (class 2606 OID 16695)
-- Name: combo_items pk_combo_items; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.combo_items
    ADD CONSTRAINT pk_combo_items PRIMARY KEY (combo_id, menu_item_id);


--
-- TOC entry 4213 (class 2606 OID 16663)
-- Name: menu_item_modifiers pk_menu_item_modifiers; Type: CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_item_modifiers
    ADD CONSTRAINT pk_menu_item_modifiers PRIMARY KEY (menu_item_id, modifier_id);


--
-- TOC entry 4230 (class 2606 OID 16757)
-- Name: order_items order_items_pkey; Type: CONSTRAINT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.order_items
    ADD CONSTRAINT order_items_pkey PRIMARY KEY (order_item_id);


--
-- TOC entry 4233 (class 2606 OID 16773)
-- Name: order_logs order_logs_pkey; Type: CONSTRAINT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.order_logs
    ADD CONSTRAINT order_logs_pkey PRIMARY KEY (log_id);


--
-- TOC entry 4227 (class 2606 OID 16736)
-- Name: orders orders_pkey; Type: CONSTRAINT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.orders
    ADD CONSTRAINT orders_pkey PRIMARY KEY (order_id);


--
-- TOC entry 4238 (class 2606 OID 17261)
-- Name: bill_ledger bill_ledger_pkey; Type: CONSTRAINT; Schema: pay; Owner: -
--

ALTER TABLE ONLY pay.bill_ledger
    ADD CONSTRAINT bill_ledger_pkey PRIMARY KEY (billing_id);


--
-- TOC entry 4242 (class 2606 OID 17057)
-- Name: payment_logs payment_logs_pkey; Type: CONSTRAINT; Schema: pay; Owner: -
--

ALTER TABLE ONLY pay.payment_logs
    ADD CONSTRAINT payment_logs_pkey PRIMARY KEY (log_id);


--
-- TOC entry 4236 (class 2606 OID 17032)
-- Name: payments payments_pkey; Type: CONSTRAINT; Schema: pay; Owner: -
--

ALTER TABLE ONLY pay.payments
    ADD CONSTRAINT payments_pkey PRIMARY KEY (payment_id);


--
-- TOC entry 4352 (class 2606 OID 354499)
-- Name: refunds refunds_pkey; Type: CONSTRAINT; Schema: pay; Owner: -
--

ALTER TABLE ONLY pay.refunds
    ADD CONSTRAINT refunds_pkey PRIMARY KEY (refund_id);


--
-- TOC entry 4200 (class 2606 OID 16594)
-- Name: app_settings app_settings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.app_settings
    ADD CONSTRAINT app_settings_pkey PRIMARY KEY (key);


--
-- TOC entry 4310 (class 2606 OID 165237)
-- Name: billing_sessions billing_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.billing_sessions
    ADD CONSTRAINT billing_sessions_pkey PRIMARY KEY (billing_id, session_id);


--
-- TOC entry 4308 (class 2606 OID 165231)
-- Name: billings billings_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.billings
    ADD CONSTRAINT billings_pkey PRIMARY KEY (billing_id);


--
-- TOC entry 4198 (class 2606 OID 16582)
-- Name: bills bills_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.bills
    ADD CONSTRAINT bills_pkey PRIMARY KEY (bill_id);


--
-- TOC entry 4330 (class 2606 OID 277110)
-- Name: floors floors_name_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.floors
    ADD CONSTRAINT floors_name_unique UNIQUE (floor_name);


--
-- TOC entry 4332 (class 2606 OID 277108)
-- Name: floors floors_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.floors
    ADD CONSTRAINT floors_pkey PRIMARY KEY (floor_id);


--
-- TOC entry 4345 (class 2606 OID 277154)
-- Name: table_layout_history layout_history_version_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.table_layout_history
    ADD CONSTRAINT layout_history_version_unique UNIQUE (floor_id, version_number);


--
-- TOC entry 4347 (class 2606 OID 277152)
-- Name: table_layout_history table_layout_history_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.table_layout_history
    ADD CONSTRAINT table_layout_history_pkey PRIMARY KEY (history_id);


--
-- TOC entry 4196 (class 2606 OID 16574)
-- Name: table_sessions table_sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.table_sessions
    ADD CONSTRAINT table_sessions_pkey PRIMARY KEY (session_id);


--
-- TOC entry 4193 (class 2606 OID 16565)
-- Name: table_status table_status_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.table_status
    ADD CONSTRAINT table_status_pkey PRIMARY KEY (label);


--
-- TOC entry 4340 (class 2606 OID 277134)
-- Name: tables tables_floor_name_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tables
    ADD CONSTRAINT tables_floor_name_unique UNIQUE (floor_id, table_name);


--
-- TOC entry 4342 (class 2606 OID 277132)
-- Name: tables tables_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tables
    ADD CONSTRAINT tables_pkey PRIMARY KEY (table_id);


--
-- TOC entry 4314 (class 2606 OID 179304)
-- Name: hierarchical_settings hierarchical_settings_pkey; Type: CONSTRAINT; Schema: settings; Owner: -
--

ALTER TABLE ONLY settings.hierarchical_settings
    ADD CONSTRAINT hierarchical_settings_pkey PRIMARY KEY (id);


--
-- TOC entry 4328 (class 2606 OID 179317)
-- Name: settings_audit settings_audit_pkey; Type: CONSTRAINT; Schema: settings; Owner: -
--

ALTER TABLE ONLY settings.settings_audit
    ADD CONSTRAINT settings_audit_pkey PRIMARY KEY (id);


--
-- TOC entry 4321 (class 2606 OID 179306)
-- Name: hierarchical_settings uk_hierarchical_settings_host_category; Type: CONSTRAINT; Schema: settings; Owner: -
--

ALTER TABLE ONLY settings.hierarchical_settings
    ADD CONSTRAINT uk_hierarchical_settings_host_category UNIQUE (host_key, category);


--
-- TOC entry 4285 (class 2606 OID 89843)
-- Name: role_inheritance role_inheritance_pkey; Type: CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.role_inheritance
    ADD CONSTRAINT role_inheritance_pkey PRIMARY KEY (child_role_id, parent_role_id);


--
-- TOC entry 4281 (class 2606 OID 89830)
-- Name: role_permissions role_permissions_pkey; Type: CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.role_permissions
    ADD CONSTRAINT role_permissions_pkey PRIMARY KEY (role_id, permission);


--
-- TOC entry 4275 (class 2606 OID 89822)
-- Name: roles roles_name_key; Type: CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.roles
    ADD CONSTRAINT roles_name_key UNIQUE (name);


--
-- TOC entry 4277 (class 2606 OID 89820)
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (role_id);


--
-- TOC entry 4268 (class 2606 OID 84925)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (user_id);


--
-- TOC entry 4270 (class 2606 OID 84927)
-- Name: users users_username_key; Type: CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.users
    ADD CONSTRAINT users_username_key UNIQUE (username);


--
-- TOC entry 4296 (class 1259 OID 127733)
-- Name: idx_customers_email; Type: INDEX; Schema: customers; Owner: -
--

CREATE INDEX idx_customers_email ON customers.customers USING btree (email) WHERE (email IS NOT NULL);


--
-- TOC entry 4297 (class 1259 OID 127734)
-- Name: idx_customers_name; Type: INDEX; Schema: customers; Owner: -
--

CREATE INDEX idx_customers_name ON customers.customers USING btree (first_name, last_name);


--
-- TOC entry 4298 (class 1259 OID 127732)
-- Name: idx_customers_phone; Type: INDEX; Schema: customers; Owner: -
--

CREATE INDEX idx_customers_phone ON customers.customers USING btree (phone) WHERE (phone IS NOT NULL);


--
-- TOC entry 4288 (class 1259 OID 98372)
-- Name: idx_cash_flow_date; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_cash_flow_date ON inventory.cash_flow USING btree (date);


--
-- TOC entry 4289 (class 1259 OID 98373)
-- Name: idx_cash_flow_employee; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_cash_flow_employee ON inventory.cash_flow USING btree (employee_name);


--
-- TOC entry 4251 (class 1259 OID 17422)
-- Name: idx_inventory_items_category; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_items_category ON inventory.inventory_items USING btree (category_id);


--
-- TOC entry 4252 (class 1259 OID 17423)
-- Name: idx_inventory_items_menu; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_items_menu ON inventory.inventory_items USING btree (is_menu_available);


--
-- TOC entry 4253 (class 1259 OID 17424)
-- Name: idx_inventory_items_vendor; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_items_vendor ON inventory.inventory_items USING btree (vendor_id);


--
-- TOC entry 4260 (class 1259 OID 17425)
-- Name: idx_inventory_tx_item_time; Type: INDEX; Schema: inventory; Owner: -
--

CREATE INDEX idx_inventory_tx_item_time ON inventory.inventory_transactions USING btree (item_id, occurred_at);


--
-- TOC entry 4218 (class 1259 OID 16716)
-- Name: ix_menu_history_entity; Type: INDEX; Schema: menu; Owner: -
--

CREATE INDEX ix_menu_history_entity ON menu.menu_history USING btree (entity_type, entity_id);


--
-- TOC entry 4201 (class 1259 OID 16622)
-- Name: ix_menu_items_avail; Type: INDEX; Schema: menu; Owner: -
--

CREATE INDEX ix_menu_items_avail ON menu.menu_items USING btree (is_available) WHERE (is_deleted = false);


--
-- TOC entry 4202 (class 1259 OID 16620)
-- Name: ix_menu_items_category; Type: INDEX; Schema: menu; Owner: -
--

CREATE INDEX ix_menu_items_category ON menu.menu_items USING btree (category);


--
-- TOC entry 4203 (class 1259 OID 16621)
-- Name: ix_menu_items_group; Type: INDEX; Schema: menu; Owner: -
--

CREATE INDEX ix_menu_items_group ON menu.menu_items USING btree (group_name);


--
-- TOC entry 4209 (class 1259 OID 16656)
-- Name: ix_modifier_options_modifier; Type: INDEX; Schema: menu; Owner: -
--

CREATE INDEX ix_modifier_options_modifier ON menu.modifier_options USING btree (modifier_id);


--
-- TOC entry 4206 (class 1259 OID 16619)
-- Name: ux_menu_items_sku_active; Type: INDEX; Schema: menu; Owner: -
--

CREATE UNIQUE INDEX ux_menu_items_sku_active ON menu.menu_items USING btree (lower(sku_id)) WHERE (is_deleted = false);


--
-- TOC entry 4221 (class 1259 OID 165252)
-- Name: idx_orders_billing_id; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX idx_orders_billing_id ON ord.orders USING btree (billing_id);


--
-- TOC entry 4222 (class 1259 OID 165251)
-- Name: idx_orders_session_id; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX idx_orders_session_id ON ord.orders USING btree (session_id);


--
-- TOC entry 4228 (class 1259 OID 16763)
-- Name: ix_order_items_order; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX ix_order_items_order ON ord.order_items USING btree (order_id) WHERE (is_deleted = false);


--
-- TOC entry 4231 (class 1259 OID 17282)
-- Name: ix_order_logs_order; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX ix_order_logs_order ON ord.order_logs USING btree (order_id);


--
-- TOC entry 4223 (class 1259 OID 17219)
-- Name: ix_orders_session; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX ix_orders_session ON ord.orders USING btree (session_id) WHERE (is_deleted = false);


--
-- TOC entry 4224 (class 1259 OID 16739)
-- Name: ix_orders_status; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX ix_orders_status ON ord.orders USING btree (status);


--
-- TOC entry 4225 (class 1259 OID 16738)
-- Name: ix_orders_table; Type: INDEX; Schema: ord; Owner: -
--

CREATE INDEX ix_orders_table ON ord.orders USING btree (table_id) WHERE (is_deleted = false);


--
-- TOC entry 4239 (class 1259 OID 17047)
-- Name: ix_bill_ledger_status; Type: INDEX; Schema: pay; Owner: -
--

CREATE INDEX ix_bill_ledger_status ON pay.bill_ledger USING btree (status);


--
-- TOC entry 4240 (class 1259 OID 17058)
-- Name: ix_payment_logs_billing; Type: INDEX; Schema: pay; Owner: -
--

CREATE INDEX ix_payment_logs_billing ON pay.payment_logs USING btree (billing_id);


--
-- TOC entry 4234 (class 1259 OID 17245)
-- Name: ix_payments_billing; Type: INDEX; Schema: pay; Owner: -
--

CREATE INDEX ix_payments_billing ON pay.payments USING btree (billing_id);


--
-- TOC entry 4348 (class 1259 OID 354501)
-- Name: ix_refunds_billing; Type: INDEX; Schema: pay; Owner: -
--

CREATE INDEX ix_refunds_billing ON pay.refunds USING btree (billing_id);


--
-- TOC entry 4349 (class 1259 OID 354502)
-- Name: ix_refunds_created_at; Type: INDEX; Schema: pay; Owner: -
--

CREATE INDEX ix_refunds_created_at ON pay.refunds USING btree (created_at);


--
-- TOC entry 4350 (class 1259 OID 354500)
-- Name: ix_refunds_payment; Type: INDEX; Schema: pay; Owner: -
--

CREATE INDEX ix_refunds_payment ON pay.refunds USING btree (payment_id);


--
-- TOC entry 4311 (class 1259 OID 165248)
-- Name: idx_billing_sessions_billing_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_billing_sessions_billing_id ON public.billing_sessions USING btree (billing_id);


--
-- TOC entry 4312 (class 1259 OID 165249)
-- Name: idx_billing_sessions_session_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_billing_sessions_session_id ON public.billing_sessions USING btree (session_id);


--
-- TOC entry 4333 (class 1259 OID 277112)
-- Name: idx_floors_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_floors_active ON public.floors USING btree (is_active) WHERE (is_active = true);


--
-- TOC entry 4334 (class 1259 OID 277111)
-- Name: idx_floors_default; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_floors_default ON public.floors USING btree (is_default) WHERE (is_default = true);


--
-- TOC entry 4343 (class 1259 OID 277160)
-- Name: idx_layout_history_floor; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_layout_history_floor ON public.table_layout_history USING btree (floor_id, created_at DESC);


--
-- TOC entry 4194 (class 1259 OID 165250)
-- Name: idx_table_sessions_billing_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_table_sessions_billing_id ON public.table_sessions USING btree (billing_id);


--
-- TOC entry 4335 (class 1259 OID 277143)
-- Name: idx_tables_active; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_tables_active ON public.tables USING btree (is_active) WHERE (is_active = true);


--
-- TOC entry 4336 (class 1259 OID 277140)
-- Name: idx_tables_floor_id; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_tables_floor_id ON public.tables USING btree (floor_id);


--
-- TOC entry 4337 (class 1259 OID 277141)
-- Name: idx_tables_status; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_tables_status ON public.tables USING btree (status);


--
-- TOC entry 4338 (class 1259 OID 277142)
-- Name: idx_tables_type; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX idx_tables_type ON public.tables USING btree (table_type);


--
-- TOC entry 4315 (class 1259 OID 179320)
-- Name: idx_hierarchical_settings_active; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_hierarchical_settings_active ON settings.hierarchical_settings USING btree (is_active) WHERE (is_active = true);


--
-- TOC entry 4316 (class 1259 OID 179319)
-- Name: idx_hierarchical_settings_category; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_hierarchical_settings_category ON settings.hierarchical_settings USING btree (category);


--
-- TOC entry 4317 (class 1259 OID 179318)
-- Name: idx_hierarchical_settings_host_key; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_hierarchical_settings_host_key ON settings.hierarchical_settings USING btree (host_key);


--
-- TOC entry 4318 (class 1259 OID 179326)
-- Name: idx_hierarchical_settings_json; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_hierarchical_settings_json ON settings.hierarchical_settings USING gin (settings_json);


--
-- TOC entry 4319 (class 1259 OID 179321)
-- Name: idx_hierarchical_settings_updated; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_hierarchical_settings_updated ON settings.hierarchical_settings USING btree (updated_at DESC);


--
-- TOC entry 4322 (class 1259 OID 179324)
-- Name: idx_settings_audit_action; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_settings_audit_action ON settings.settings_audit USING btree (action);


--
-- TOC entry 4323 (class 1259 OID 179325)
-- Name: idx_settings_audit_category; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_settings_audit_category ON settings.settings_audit USING btree (category);


--
-- TOC entry 4324 (class 1259 OID 179327)
-- Name: idx_settings_audit_changes_json; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_settings_audit_changes_json ON settings.settings_audit USING gin (changes_json);


--
-- TOC entry 4325 (class 1259 OID 179323)
-- Name: idx_settings_audit_created_at; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_settings_audit_created_at ON settings.settings_audit USING btree (created_at DESC);


--
-- TOC entry 4326 (class 1259 OID 179322)
-- Name: idx_settings_audit_host_key; Type: INDEX; Schema: settings; Owner: -
--

CREATE INDEX idx_settings_audit_host_key ON settings.settings_audit USING btree (host_key);


--
-- TOC entry 4282 (class 1259 OID 89854)
-- Name: idx_role_inheritance_child; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_role_inheritance_child ON users.role_inheritance USING btree (child_role_id);


--
-- TOC entry 4283 (class 1259 OID 89855)
-- Name: idx_role_inheritance_parent; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_role_inheritance_parent ON users.role_inheritance USING btree (parent_role_id);


--
-- TOC entry 4278 (class 1259 OID 89837)
-- Name: idx_role_permissions_permission; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_role_permissions_permission ON users.role_permissions USING btree (permission);


--
-- TOC entry 4279 (class 1259 OID 89836)
-- Name: idx_role_permissions_role; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_role_permissions_role ON users.role_permissions USING btree (role_id);


--
-- TOC entry 4271 (class 1259 OID 89825)
-- Name: idx_roles_active; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_roles_active ON users.roles USING btree (is_active) WHERE (is_deleted = false);


--
-- TOC entry 4272 (class 1259 OID 89823)
-- Name: idx_roles_name; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_roles_name ON users.roles USING btree (name) WHERE (is_deleted = false);


--
-- TOC entry 4273 (class 1259 OID 89824)
-- Name: idx_roles_system; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_roles_system ON users.roles USING btree (is_system_role) WHERE (is_deleted = false);


--
-- TOC entry 4263 (class 1259 OID 84930)
-- Name: idx_users_active; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_users_active ON users.users USING btree (is_active) WHERE (is_deleted = false);


--
-- TOC entry 4264 (class 1259 OID 84931)
-- Name: idx_users_created_at; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_users_created_at ON users.users USING btree (created_at) WHERE (is_deleted = false);


--
-- TOC entry 4265 (class 1259 OID 84929)
-- Name: idx_users_role; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_users_role ON users.users USING btree (role) WHERE (is_deleted = false);


--
-- TOC entry 4266 (class 1259 OID 84928)
-- Name: idx_users_username; Type: INDEX; Schema: users; Owner: -
--

CREATE INDEX idx_users_username ON users.users USING btree (username) WHERE (is_deleted = false);


--
-- TOC entry 4381 (class 2620 OID 17426)
-- Name: inventory_items update_items_updated_at; Type: TRIGGER; Schema: inventory; Owner: -
--

CREATE TRIGGER update_items_updated_at BEFORE UPDATE ON inventory.inventory_items FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at();


--
-- TOC entry 4380 (class 2620 OID 17427)
-- Name: vendors update_vendors_updated_at; Type: TRIGGER; Schema: inventory; Owner: -
--

CREATE TRIGGER update_vendors_updated_at BEFORE UPDATE ON inventory.vendors FOR EACH ROW EXECUTE FUNCTION inventory.update_updated_at();


--
-- TOC entry 4378 (class 2620 OID 360772)
-- Name: bill_ledger trg_prevent_bill_ledger_updates; Type: TRIGGER; Schema: pay; Owner: -
--

CREATE TRIGGER trg_prevent_bill_ledger_updates BEFORE UPDATE ON pay.bill_ledger FOR EACH ROW EXECUTE FUNCTION pay.prevent_bill_ledger_updates();


--
-- TOC entry 4379 (class 2620 OID 360774)
-- Name: payment_logs trg_prevent_payment_logs_updates; Type: TRIGGER; Schema: pay; Owner: -
--

CREATE TRIGGER trg_prevent_payment_logs_updates BEFORE UPDATE ON pay.payment_logs FOR EACH ROW EXECUTE FUNCTION pay.prevent_payment_logs_updates();


--
-- TOC entry 4377 (class 2620 OID 360771)
-- Name: payments trg_prevent_payment_updates; Type: TRIGGER; Schema: pay; Owner: -
--

CREATE TRIGGER trg_prevent_payment_updates BEFORE UPDATE ON pay.payments FOR EACH ROW EXECUTE FUNCTION pay.prevent_payment_updates();


--
-- TOC entry 4386 (class 2620 OID 360773)
-- Name: refunds trg_prevent_refund_updates; Type: TRIGGER; Schema: pay; Owner: -
--

CREATE TRIGGER trg_prevent_refund_updates BEFORE UPDATE ON pay.refunds FOR EACH ROW EXECUTE FUNCTION pay.prevent_refund_updates();


--
-- TOC entry 4383 (class 2620 OID 353691)
-- Name: floors trigger_ensure_single_default_floor; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trigger_ensure_single_default_floor AFTER INSERT OR UPDATE OF is_default ON public.floors FOR EACH ROW WHEN ((new.is_default = true)) EXECUTE FUNCTION public.ensure_single_default_floor();


--
-- TOC entry 4384 (class 2620 OID 353689)
-- Name: floors trigger_floors_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trigger_floors_updated_at BEFORE UPDATE ON public.floors FOR EACH ROW EXECUTE FUNCTION public.update_floors_updated_at();


--
-- TOC entry 4385 (class 2620 OID 353690)
-- Name: tables trigger_tables_updated_at; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER trigger_tables_updated_at BEFORE UPDATE ON public.tables FOR EACH ROW EXECUTE FUNCTION public.update_tables_updated_at();


--
-- TOC entry 4382 (class 2620 OID 179329)
-- Name: hierarchical_settings trigger_update_hierarchical_settings_updated_at; Type: TRIGGER; Schema: settings; Owner: -
--

CREATE TRIGGER trigger_update_hierarchical_settings_updated_at BEFORE UPDATE ON settings.hierarchical_settings FOR EACH ROW EXECUTE FUNCTION settings.update_updated_at_column();


--
-- TOC entry 4369 (class 2606 OID 127682)
-- Name: customers customers_membership_level_id_fkey; Type: FK CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.customers
    ADD CONSTRAINT customers_membership_level_id_fkey FOREIGN KEY (membership_level_id) REFERENCES customers.membership_levels(membership_level_id) ON DELETE RESTRICT;


--
-- TOC entry 4372 (class 2606 OID 127727)
-- Name: loyalty_transactions loyalty_transactions_customer_id_fkey; Type: FK CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.loyalty_transactions
    ADD CONSTRAINT loyalty_transactions_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES customers.customers(customer_id) ON DELETE CASCADE;


--
-- TOC entry 4371 (class 2606 OID 127713)
-- Name: wallet_transactions wallet_transactions_wallet_id_fkey; Type: FK CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.wallet_transactions
    ADD CONSTRAINT wallet_transactions_wallet_id_fkey FOREIGN KEY (wallet_id) REFERENCES customers.wallets(wallet_id) ON DELETE CASCADE;


--
-- TOC entry 4370 (class 2606 OID 127700)
-- Name: wallets wallets_customer_id_fkey; Type: FK CONSTRAINT; Schema: customers; Owner: -
--

ALTER TABLE ONLY customers.wallets
    ADD CONSTRAINT wallets_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES customers.customers(customer_id) ON DELETE CASCADE;


--
-- TOC entry 4361 (class 2606 OID 17397)
-- Name: inventory_categories inventory_categories_parent_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_categories
    ADD CONSTRAINT inventory_categories_parent_id_fkey FOREIGN KEY (parent_id) REFERENCES inventory.inventory_categories(category_id) ON DELETE SET NULL;


--
-- TOC entry 4362 (class 2606 OID 17402)
-- Name: inventory_items inventory_items_category_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_category_id_fkey FOREIGN KEY (category_id) REFERENCES inventory.categories(category_id) ON DELETE SET NULL;


--
-- TOC entry 4363 (class 2606 OID 17407)
-- Name: inventory_items inventory_items_vendor_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_items
    ADD CONSTRAINT inventory_items_vendor_id_fkey FOREIGN KEY (vendor_id) REFERENCES inventory.vendors(vendor_id) ON DELETE SET NULL;


--
-- TOC entry 4364 (class 2606 OID 17412)
-- Name: inventory_stock inventory_stock_item_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_stock
    ADD CONSTRAINT inventory_stock_item_id_fkey FOREIGN KEY (item_id) REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE;


--
-- TOC entry 4365 (class 2606 OID 17417)
-- Name: inventory_transactions inventory_transactions_item_id_fkey; Type: FK CONSTRAINT; Schema: inventory; Owner: -
--

ALTER TABLE ONLY inventory.inventory_transactions
    ADD CONSTRAINT inventory_transactions_item_id_fkey FOREIGN KEY (item_id) REFERENCES inventory.inventory_items(item_id) ON DELETE CASCADE;


--
-- TOC entry 4356 (class 2606 OID 16696)
-- Name: combo_items combo_items_combo_id_fkey; Type: FK CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.combo_items
    ADD CONSTRAINT combo_items_combo_id_fkey FOREIGN KEY (combo_id) REFERENCES menu.combos(combo_id) ON DELETE CASCADE;


--
-- TOC entry 4357 (class 2606 OID 16701)
-- Name: combo_items combo_items_menu_item_id_fkey; Type: FK CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.combo_items
    ADD CONSTRAINT combo_items_menu_item_id_fkey FOREIGN KEY (menu_item_id) REFERENCES menu.menu_items(menu_item_id) ON DELETE RESTRICT;


--
-- TOC entry 4354 (class 2606 OID 16664)
-- Name: menu_item_modifiers menu_item_modifiers_menu_item_id_fkey; Type: FK CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_item_modifiers
    ADD CONSTRAINT menu_item_modifiers_menu_item_id_fkey FOREIGN KEY (menu_item_id) REFERENCES menu.menu_items(menu_item_id) ON DELETE CASCADE;


--
-- TOC entry 4355 (class 2606 OID 16669)
-- Name: menu_item_modifiers menu_item_modifiers_modifier_id_fkey; Type: FK CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.menu_item_modifiers
    ADD CONSTRAINT menu_item_modifiers_modifier_id_fkey FOREIGN KEY (modifier_id) REFERENCES menu.modifiers(modifier_id) ON DELETE RESTRICT;


--
-- TOC entry 4353 (class 2606 OID 16651)
-- Name: modifier_options modifier_options_modifier_id_fkey; Type: FK CONSTRAINT; Schema: menu; Owner: -
--

ALTER TABLE ONLY menu.modifier_options
    ADD CONSTRAINT modifier_options_modifier_id_fkey FOREIGN KEY (modifier_id) REFERENCES menu.modifiers(modifier_id) ON DELETE CASCADE;


--
-- TOC entry 4359 (class 2606 OID 16758)
-- Name: order_items order_items_order_id_fkey; Type: FK CONSTRAINT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.order_items
    ADD CONSTRAINT order_items_order_id_fkey FOREIGN KEY (order_id) REFERENCES ord.orders(order_id) ON DELETE CASCADE;


--
-- TOC entry 4360 (class 2606 OID 16774)
-- Name: order_logs order_logs_order_id_fkey; Type: FK CONSTRAINT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.order_logs
    ADD CONSTRAINT order_logs_order_id_fkey FOREIGN KEY (order_id) REFERENCES ord.orders(order_id) ON DELETE CASCADE;


--
-- TOC entry 4358 (class 2606 OID 165211)
-- Name: orders orders_session_id_fkey; Type: FK CONSTRAINT; Schema: ord; Owner: -
--

ALTER TABLE ONLY ord.orders
    ADD CONSTRAINT orders_session_id_fkey FOREIGN KEY (session_id) REFERENCES public.table_sessions(session_id) ON DELETE CASCADE;


--
-- TOC entry 4373 (class 2606 OID 165238)
-- Name: billing_sessions billing_sessions_billing_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.billing_sessions
    ADD CONSTRAINT billing_sessions_billing_id_fkey FOREIGN KEY (billing_id) REFERENCES public.billings(billing_id) ON DELETE CASCADE;


--
-- TOC entry 4374 (class 2606 OID 165243)
-- Name: billing_sessions billing_sessions_session_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.billing_sessions
    ADD CONSTRAINT billing_sessions_session_id_fkey FOREIGN KEY (session_id) REFERENCES public.table_sessions(session_id) ON DELETE CASCADE;


--
-- TOC entry 4376 (class 2606 OID 277155)
-- Name: table_layout_history table_layout_history_floor_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.table_layout_history
    ADD CONSTRAINT table_layout_history_floor_id_fkey FOREIGN KEY (floor_id) REFERENCES public.floors(floor_id) ON DELETE CASCADE;


--
-- TOC entry 4375 (class 2606 OID 277135)
-- Name: tables tables_floor_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.tables
    ADD CONSTRAINT tables_floor_id_fkey FOREIGN KEY (floor_id) REFERENCES public.floors(floor_id) ON DELETE CASCADE;


--
-- TOC entry 4367 (class 2606 OID 89844)
-- Name: role_inheritance role_inheritance_child_role_id_fkey; Type: FK CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.role_inheritance
    ADD CONSTRAINT role_inheritance_child_role_id_fkey FOREIGN KEY (child_role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE;


--
-- TOC entry 4368 (class 2606 OID 89849)
-- Name: role_inheritance role_inheritance_parent_role_id_fkey; Type: FK CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.role_inheritance
    ADD CONSTRAINT role_inheritance_parent_role_id_fkey FOREIGN KEY (parent_role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE;


--
-- TOC entry 4366 (class 2606 OID 89831)
-- Name: role_permissions role_permissions_role_id_fkey; Type: FK CONSTRAINT; Schema: users; Owner: -
--

ALTER TABLE ONLY users.role_permissions
    ADD CONSTRAINT role_permissions_role_id_fkey FOREIGN KEY (role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE;


-- Completed on 2025-12-09 21:00:47

--
-- PostgreSQL database dump complete
--

\unrestrict IDefbRJmnGrMbFfC1yEOLStVffh0oaAiVLeRei2SahWhHBtdvy9BOE7SOuq9P69

