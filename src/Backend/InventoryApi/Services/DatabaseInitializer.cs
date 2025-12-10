using Dapper;
using Npgsql;

namespace InventoryApi.Services;

public class DatabaseInitializer
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(NpgsqlDataSource dataSource, ILogger<DatabaseInitializer> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            // First, create basic tables (categories, vendors, inventory_items)
            var categoriesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "create_categories.sql");
            
            if (File.Exists(categoriesPath))
            {
                var categoriesSql = await File.ReadAllTextAsync(categoriesPath, ct);
                await conn.ExecuteAsync(categoriesSql);
                _logger.LogInformation("Basic inventory tables created successfully");
            }
            else
            {
                _logger.LogWarning("Categories file not found at {CategoriesPath}", categoriesPath);
            }
            
            // Read and execute the schema migration
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "inventory_management_schema.sql");
            
            if (File.Exists(schemaPath))
            {
                var schemaSql = await File.ReadAllTextAsync(schemaPath, ct);
                await conn.ExecuteAsync(schemaSql);
                _logger.LogInformation("Inventory Management System schema initialized successfully");
            }
            else
            {
                _logger.LogWarning("Schema file not found at {SchemaPath}", schemaPath);
            }

            // Read and execute the menu seeding
            var menuSeedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "bola_ocho_menu_seed.sql");
            
            if (File.Exists(menuSeedPath))
            {
                var menuSeedSql = await File.ReadAllTextAsync(menuSeedPath, ct);
                await conn.ExecuteAsync(menuSeedSql);
                _logger.LogInformation("Bola 8 Pool Club menu items seeded successfully");
            }
            else
            {
                _logger.LogWarning("Menu seed file not found at {MenuSeedPath}", menuSeedPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Inventory Management System schema");
            throw;
        }
    }
}
