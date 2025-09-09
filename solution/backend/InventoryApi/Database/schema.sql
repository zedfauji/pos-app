-- Inventory schema (CloudSQL PostgreSQL 17)
do $$
begin
  if not exists (select 1 from information_schema.schemata where schema_name = 'inventory') then
    execute 'create schema inventory';
  end if;
end $$;

-- Vendors
create table if not exists inventory.vendors (
    vendor_id uuid primary key default gen_random_uuid(),
    name text not null,
    contact_info text null,
    status text not null default 'active',
    notes text null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

-- Categories
create table if not exists inventory.inventory_categories (
    category_id uuid primary key default gen_random_uuid(),
    name text not null,
    parent_id uuid null references inventory.inventory_categories(category_id) on delete set null,
    path text null,
    created_at timestamptz not null default now()
);

-- Items
create table if not exists inventory.inventory_items (
    item_id uuid primary key default gen_random_uuid(),
    vendor_id uuid null references inventory.vendors(vendor_id) on delete set null,
    category_id uuid null references inventory.inventory_categories(category_id) on delete set null,
    sku text not null unique,
    name text not null,
    description text null,
    unit text null,
    barcode text null,
    reorder_threshold numeric(18,3) null,
    buying_price numeric(18,2) null,
    selling_price numeric(18,2) null,
    tax_rate numeric(5,2) null,
    is_menu_available boolean not null default false,
    is_active boolean not null default true,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now()
);

-- Current stock per item
create table if not exists inventory.inventory_stock (
    item_id uuid primary key references inventory.inventory_items(item_id) on delete cascade,
    quantity_on_hand numeric(18,3) not null default 0,
    last_counted_at timestamptz null
);

-- Transactions (stock movements)
do $$
begin
  if not exists (
    select 1 from pg_type t
    join pg_namespace n on n.oid = t.typnamespace
    where t.typname = 'transaction_source' and n.nspname = 'inventory') then
    execute $$create type inventory.transaction_source as enum ('purchase','customer_order','adjustment','wastage','transfer')$$;
  end if;
end $$;

create table if not exists inventory.inventory_transactions (
    transaction_id uuid primary key default gen_random_uuid(),
    item_id uuid not null references inventory.inventory_items(item_id) on delete cascade,
    delta numeric(18,3) not null,
    quantity_before numeric(18,3) not null,
    quantity_after numeric(18,3) not null,
    unit_cost numeric(18,2) null,
    source inventory.transaction_source not null,
    source_ref text null,
    user_id text null,
    notes text null,
    occurred_at timestamptz not null default now(),
    created_at timestamptz not null default now()
);

-- Convenience view for current items with stock
create or replace view inventory.v_items_current as
select i.item_id,
       i.vendor_id,
       i.category_id,
       i.sku,
       i.name,
       i.description,
       i.selling_price,
       coalesce(s.quantity_on_hand, 0) as quantity_on_hand,
       i.is_menu_available
from inventory.inventory_items i
left join inventory.inventory_stock s on s.item_id = i.item_id
where i.is_active = true;

-- Indexes
create index if not exists idx_inventory_items_vendor on inventory.inventory_items(vendor_id);
create index if not exists idx_inventory_items_category on inventory.inventory_items(category_id);
create index if not exists idx_inventory_items_menu on inventory.inventory_items(is_menu_available);
create index if not exists idx_inventory_tx_item_time on inventory.inventory_transactions(item_id, occurred_at);

