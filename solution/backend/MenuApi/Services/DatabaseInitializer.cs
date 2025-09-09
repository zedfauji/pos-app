using Npgsql;

namespace MenuApi.Services;

public sealed class DatabaseInitializer : IHostedService
{
    private readonly NpgsqlDataSource? _dataSource;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(NpgsqlDataSource? dataSource, ILogger<DatabaseInitializer> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_dataSource is null)
            {
                _logger.LogWarning("No NpgsqlDataSource configured; skipping Menu schema initialization.");
                return;
            }

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            await EnsureMenuSchemaAsync(conn, cancellationToken);
            await SeedAsync(conn, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Menu schema initialization skipped due to error. Service will continue to start.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureMenuSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = @"
create schema if not exists menu;

create table if not exists menu.menu_items (
  menu_item_id        bigserial primary key,
  sku_id              text not null,
  name                text not null,
  description         text null,
  category            text not null,
  group_name          text null,
  vendor_price        numeric(12,2) not null default 0.00,
  selling_price       numeric(12,2) not null,
  price               numeric(12,2) null,
  picture_url         text null,
  is_discountable     boolean not null default true,
  is_part_of_combo    boolean not null default false,
  is_available        boolean not null default true,
  version             int not null default 1,
  is_deleted          boolean not null default false,
  created_by          text null,
  updated_by          text null,
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);
-- Drop any legacy unique constraint or index on sku_id to allow partial unique index
do $$ begin
  if exists (
    select 1 from information_schema.table_constraints tc
    where tc.constraint_type = 'UNIQUE'
      and tc.table_schema = 'menu'
      and tc.table_name = 'menu_items'
      and tc.constraint_name = 'menu_items_sku_id_key') then
    alter table menu.menu_items drop constraint menu_items_sku_id_key;
  end if;
exception when undefined_table then
  -- ignore
  null;
end $$;

-- Create case-insensitive partial unique index for active (not deleted) rows
create unique index if not exists ux_menu_items_sku_active
  on menu.menu_items (lower(sku_id))
  where is_deleted = false;
create index if not exists ix_menu_items_category on menu.menu_items(category);
create index if not exists ix_menu_items_group on menu.menu_items(group_name);
create index if not exists ix_menu_items_avail on menu.menu_items(is_available) where is_deleted = false;

create table if not exists menu.modifiers (
  modifier_id         bigserial primary key,
  name                text not null,
  description         text null,
  is_required         boolean not null default false,
  allow_multiple      boolean not null default false,
  min_selections      int not null default 0,
  max_selections      int null,
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);

create table if not exists menu.modifier_options (
  option_id           bigserial primary key,
  modifier_id         bigint not null references menu.modifiers(modifier_id) on delete cascade,
  name                text not null,
  price_delta         numeric(12,2) not null default 0.00,
  is_available        boolean not null default true,
  sort_order          int not null default 0,
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);
create index if not exists ix_modifier_options_modifier on menu.modifier_options(modifier_id);

create table if not exists menu.menu_item_modifiers (
  menu_item_id        bigint not null references menu.menu_items(menu_item_id) on delete cascade,
  modifier_id         bigint not null references menu.modifiers(modifier_id) on delete restrict,
  sort_order          int not null default 0,
  is_optional         boolean not null default true,
  constraint pk_menu_item_modifiers primary key(menu_item_id, modifier_id)
);

create table if not exists menu.combos (
  combo_id            bigserial primary key,
  name                text not null,
  description         text null,
  price               numeric(12,2) not null,
  is_discountable     boolean not null default true,
  is_available        boolean not null default true,
  version             int not null default 1,
  is_deleted          boolean not null default false,
  picture_url         text null,
  created_by          text null,
  updated_by          text null,
  created_at          timestamptz not null default now(),
  updated_at          timestamptz not null default now()
);

create table if not exists menu.combo_items (
  combo_id            bigint not null references menu.combos(combo_id) on delete cascade,
  menu_item_id        bigint not null references menu.menu_items(menu_item_id) on delete restrict,
  quantity            int not null default 1,
  is_required         boolean not null default true,
  constraint pk_combo_items primary key(combo_id, menu_item_id)
);

create table if not exists menu.menu_history (
  history_id          bigserial primary key,
  entity_type         text not null,
  entity_id           bigint not null,
  action              text not null,
  old_value           jsonb null,
  new_value           jsonb null,
  version             int null,
  changed_by          text null,
  changed_at          timestamptz not null default now()
);
create index if not exists ix_menu_history_entity on menu.menu_history(entity_type, entity_id);
";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task SeedAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        // Seed minimal data if empty
        const string hasAny = "SELECT COUNT(1) FROM menu.menu_items";
        await using (var check = new NpgsqlCommand(hasAny, conn))
        {
            var count = Convert.ToInt32(await check.ExecuteScalarAsync(ct));
            if (count > 0) return;
        }

        // Insert items
        long coffeeId, burgerId, friesId;
        await using (var cmd = new NpgsqlCommand(@"INSERT INTO menu.menu_items(sku_id, name, description, category, group_name, vendor_price, selling_price, picture_url, is_discountable, is_part_of_combo, is_available)
                                                 VALUES('BEV-COF-001','Coffee','Hot brewed coffee','Beverages','Hot Drinks',0.50,2.50,'',true,true,true)
                                                 RETURNING menu_item_id", conn))
        {
            coffeeId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }
        await using (var cmd = new NpgsqlCommand(@"INSERT INTO menu.menu_items(sku_id, name, description, category, group_name, vendor_price, selling_price, picture_url, is_discountable, is_part_of_combo, is_available)
                                                 VALUES('ENT-BUR-001','Classic Burger','Beef patty with lettuce & tomato','Entrees','Burgers',2.00,7.99,'',true,true,true)
                                                 RETURNING menu_item_id", conn))
        {
            burgerId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }
        await using (var cmd = new NpgsqlCommand(@"INSERT INTO menu.menu_items(sku_id, name, description, category, group_name, vendor_price, selling_price, picture_url, is_discountable, is_part_of_combo, is_available)
                                                 VALUES('SID-FRY-001','French Fries','Crispy fries','Sides','Fries',0.40,2.99,'',true,true,true)
                                                 RETURNING menu_item_id", conn))
        {
            friesId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }

        // Size modifier for coffee
        long sizeModId;
        await using (var cmd = new NpgsqlCommand(@"INSERT INTO menu.modifiers(name, description, is_required, allow_multiple, min_selections, max_selections)
                                                 VALUES('Size','Choose a size', true, false, 1, 1)
                                                 RETURNING modifier_id", conn))
        {
            sizeModId = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        }
        // Options
        foreach (var tuple in new[] { ("Small",0m,1), ("Medium",0.50m,2), ("Large",1.00m,3) })
        {
            await using var op = new NpgsqlCommand(@"INSERT INTO menu.modifier_options(modifier_id, name, price_delta, is_available, sort_order)
                                                    VALUES(@m, @n, @d, true, @s)", conn);
            op.Parameters.AddWithValue("@m", sizeModId);
            op.Parameters.AddWithValue("@n", tuple.Item1);
            op.Parameters.AddWithValue("@d", tuple.Item2);
            op.Parameters.AddWithValue("@s", tuple.Item3);
            await op.ExecuteNonQueryAsync(ct);
        }
        // Link Coffee -> Size
        await using (var link = new NpgsqlCommand(@"INSERT INTO menu.menu_item_modifiers(menu_item_id, modifier_id, sort_order, is_optional) VALUES(@mi, @mo, 1, false)", conn))
        {
            link.Parameters.AddWithValue("@mi", coffeeId);
            link.Parameters.AddWithValue("@mo", sizeModId);
            await link.ExecuteNonQueryAsync(ct);
        }

        // Combo
        long comboId;
        await using (var cc = new NpgsqlCommand(@"INSERT INTO menu.combos(name, description, price, is_discountable, is_available, picture_url)
                                                VALUES('Burger Combo','Burger + Fries + Coffee', 11.99, true, true, '')
                                                RETURNING combo_id", conn))
        {
            comboId = Convert.ToInt64(await cc.ExecuteScalarAsync(ct));
        }
        foreach (var mid in new[] { burgerId, friesId, coffeeId })
        {
            await using var ci = new NpgsqlCommand(@"INSERT INTO menu.combo_items(combo_id, menu_item_id, quantity, is_required) VALUES(@c, @m, 1, true)", conn);
            ci.Parameters.AddWithValue("@c", comboId);
            ci.Parameters.AddWithValue("@m", mid);
            await ci.ExecuteNonQueryAsync(ct);
        }
    }
}
