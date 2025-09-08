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
        if (_dataSource is null)
        {
            _logger.LogWarning("No NpgsqlDataSource configured; skipping Menu schema initialization.");
            return;
        }

        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await EnsureMenuSchemaAsync(conn, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureMenuSchemaAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        const string sql = @"
create schema if not exists menu;

create table if not exists menu.menu_items (
  menu_item_id        bigserial primary key,
  sku_id              text not null unique,
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
}
