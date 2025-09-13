using Npgsql;

namespace UsersApi.Services;

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
        if (_dataSource == null)
        {
            _logger.LogWarning("No database connection configured, skipping initialization");
            return;
        }

        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            
            // Create schema if not exists
            await using var schemaCmd = new NpgsqlCommand("CREATE SCHEMA IF NOT EXISTS users", conn);
            await schemaCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Create users table
            const string createTableSql = @"
                CREATE TABLE IF NOT EXISTS users.users (
                    user_id VARCHAR(50) PRIMARY KEY,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    role VARCHAR(20) NOT NULL DEFAULT 'employee',
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    is_active BOOLEAN NOT NULL DEFAULT true,
                    is_deleted BOOLEAN NOT NULL DEFAULT false
                );
                
                CREATE INDEX IF NOT EXISTS idx_users_username ON users.users(username) WHERE is_deleted = false;
                CREATE INDEX IF NOT EXISTS idx_users_role ON users.users(role) WHERE is_deleted = false;
                CREATE INDEX IF NOT EXISTS idx_users_active ON users.users(is_active) WHERE is_deleted = false;
                CREATE INDEX IF NOT EXISTS idx_users_created_at ON users.users(created_at) WHERE is_deleted = false;
            ";
            
            await using var createCmd = new NpgsqlCommand(createTableSql, conn);
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Create default admin user if no users exist
            await using var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users.users WHERE is_deleted = false", conn);
            var userCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
            
            if (userCount == 0)
            {
                _logger.LogInformation("Creating default admin user");
                const string insertAdminSql = @"
                    INSERT INTO users.users (user_id, username, password_hash, role, created_at, updated_at, is_active, is_deleted)
                    VALUES (@userId, @username, @passwordHash, @role, @createdAt, @updatedAt, @isActive, @isDeleted)
                ";
                
                await using var insertCmd = new NpgsqlCommand(insertAdminSql, conn);
                insertCmd.Parameters.AddWithValue("@userId", Guid.NewGuid().ToString());
                insertCmd.Parameters.AddWithValue("@username", "admin");
                insertCmd.Parameters.AddWithValue("@passwordHash", BCrypt.Net.BCrypt.HashPassword("123456"));
                insertCmd.Parameters.AddWithValue("@role", "admin");
                insertCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
                insertCmd.Parameters.AddWithValue("@isActive", true);
                insertCmd.Parameters.AddWithValue("@isDeleted", false);
                
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Default admin user created (username: admin, password: 123456)");
            }
            
            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
