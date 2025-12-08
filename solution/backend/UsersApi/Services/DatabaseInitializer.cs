using Npgsql;
using MagiDesk.Shared.DTOs.Users;

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

        // Don't block application startup - run initialization in background
        _ = Task.Run(async () =>
        {
            try
            {
                await InitializeDatabaseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database initialization failed in background task");
            }
        }, cancellationToken);
    }

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Attempting to connect to database...");
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
            _logger.LogInformation("Successfully connected to database");
            
            // Create schema if not exists
            await using var schemaCmd = new NpgsqlCommand("CREATE SCHEMA IF NOT EXISTS users", conn);
            await schemaCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Create roles table
            const string createRolesTableSql = @"
                CREATE TABLE IF NOT EXISTS users.roles (
                    role_id VARCHAR(50) PRIMARY KEY,
                    name VARCHAR(50) NOT NULL UNIQUE,
                    description TEXT,
                    is_system_role BOOLEAN NOT NULL DEFAULT false,
                    is_active BOOLEAN NOT NULL DEFAULT true,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    is_deleted BOOLEAN NOT NULL DEFAULT false
                );
                
                CREATE INDEX IF NOT EXISTS idx_roles_name ON users.roles(name) WHERE is_deleted = false;
                CREATE INDEX IF NOT EXISTS idx_roles_system ON users.roles(is_system_role) WHERE is_deleted = false;
                CREATE INDEX IF NOT EXISTS idx_roles_active ON users.roles(is_active) WHERE is_deleted = false;
            ";
            
            await using var createRolesCmd = new NpgsqlCommand(createRolesTableSql, conn);
            await createRolesCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Create role_permissions table
            const string createRolePermissionsTableSql = @"
                CREATE TABLE IF NOT EXISTS users.role_permissions (
                    role_id VARCHAR(50) NOT NULL,
                    permission VARCHAR(100) NOT NULL,
                    PRIMARY KEY (role_id, permission),
                    FOREIGN KEY (role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE
                );
                
                CREATE INDEX IF NOT EXISTS idx_role_permissions_role ON users.role_permissions(role_id);
                CREATE INDEX IF NOT EXISTS idx_role_permissions_permission ON users.role_permissions(permission);
            ";
            
            await using var createRolePermissionsCmd = new NpgsqlCommand(createRolePermissionsTableSql, conn);
            await createRolePermissionsCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Create role_inheritance table
            const string createRoleInheritanceTableSql = @"
                CREATE TABLE IF NOT EXISTS users.role_inheritance (
                    child_role_id VARCHAR(50) NOT NULL,
                    parent_role_id VARCHAR(50) NOT NULL,
                    PRIMARY KEY (child_role_id, parent_role_id),
                    FOREIGN KEY (child_role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE,
                    FOREIGN KEY (parent_role_id) REFERENCES users.roles(role_id) ON DELETE CASCADE,
                    CHECK (child_role_id != parent_role_id)
                );
                
                CREATE INDEX IF NOT EXISTS idx_role_inheritance_child ON users.role_inheritance(child_role_id);
                CREATE INDEX IF NOT EXISTS idx_role_inheritance_parent ON users.role_inheritance(parent_role_id);
            ";
            
            await using var createRoleInheritanceCmd = new NpgsqlCommand(createRoleInheritanceTableSql, conn);
            await createRoleInheritanceCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Create users table
            const string createUsersTableSql = @"
                CREATE TABLE IF NOT EXISTS users.users (
                    user_id VARCHAR(50) PRIMARY KEY,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password_hash VARCHAR(255) NOT NULL,
                    role VARCHAR(50) NOT NULL DEFAULT 'Server',
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
            
            await using var createUsersCmd = new NpgsqlCommand(createUsersTableSql, conn);
            await createUsersCmd.ExecuteNonQueryAsync(cancellationToken);
            
            // Initialize system roles
            await InitializeSystemRolesAsync(conn, cancellationToken);
            
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
                insertCmd.Parameters.AddWithValue("@role", "Administrator");
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
            // Don't throw - let the application continue running
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeSystemRolesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        try
        {
            // Check if system roles already exist
            await using var countCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users.roles WHERE is_system_role = true", conn);
            var systemRoleCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken));
            
            if (systemRoleCount > 0)
            {
                _logger.LogInformation("System roles already initialized");
                return;
            }

            _logger.LogInformation("Initializing system roles");

            var systemRoles = new[]
            {
                new { Id = "system-owner", Name = "Owner", Description = "Full system access, business owner" },
                new { Id = "system-administrator", Name = "Administrator", Description = "System administration and user management" },
                new { Id = "system-manager", Name = "Manager", Description = "Store operations, staff management, reports" },
                new { Id = "system-server", Name = "Server", Description = "Order taking, table management, customer service" },
                new { Id = "system-cashier", Name = "Cashier", Description = "Payment processing, basic order operations" },
                new { Id = "system-host", Name = "Host", Description = "Customer seating, reservation management" }
            };

            const string insertRoleSql = @"
                INSERT INTO users.roles (role_id, name, description, is_system_role, is_active, created_at, updated_at)
                VALUES (@roleId, @name, @description, @isSystemRole, @isActive, @createdAt, @updatedAt)";

            foreach (var role in systemRoles)
            {
                await using var cmd = new NpgsqlCommand(insertRoleSql, conn);
                cmd.Parameters.AddWithValue("@roleId", role.Id);
                cmd.Parameters.AddWithValue("@name", role.Name);
                cmd.Parameters.AddWithValue("@description", role.Description);
                cmd.Parameters.AddWithValue("@isSystemRole", true);
                cmd.Parameters.AddWithValue("@isActive", true);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Set permissions for each role
            await SetSystemRolePermissionsAsync(conn, cancellationToken);

            _logger.LogInformation("System roles initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize system roles");
            throw;
        }
    }

    private async Task SetSystemRolePermissionsAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string insertPermissionSql = "INSERT INTO users.role_permissions (role_id, permission) VALUES (@roleId, @permission)";

        // Owner gets all permissions
        var ownerPermissions = Permissions.AllPermissions;
        foreach (var permission in ownerPermissions)
        {
            await using var cmd = new NpgsqlCommand(insertPermissionSql, conn);
            cmd.Parameters.AddWithValue("@roleId", "system-owner");
            cmd.Parameters.AddWithValue("@permission", permission);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Administrator permissions
        var adminPermissions = new[]
        {
            Permissions.USER_VIEW, Permissions.USER_CREATE, Permissions.USER_UPDATE, Permissions.USER_DELETE, Permissions.USER_MANAGE_ROLES,
            Permissions.ORDER_VIEW, Permissions.ORDER_CREATE, Permissions.ORDER_UPDATE, Permissions.ORDER_DELETE, Permissions.ORDER_CANCEL, Permissions.ORDER_COMPLETE,
            Permissions.TABLE_VIEW, Permissions.TABLE_MANAGE, Permissions.TABLE_ASSIGN, Permissions.TABLE_CLEAR,
            Permissions.MENU_VIEW, Permissions.MENU_CREATE, Permissions.MENU_UPDATE, Permissions.MENU_DELETE, Permissions.MENU_PRICE_CHANGE,
            Permissions.PAYMENT_VIEW, Permissions.PAYMENT_PROCESS, Permissions.PAYMENT_REFUND, Permissions.PAYMENT_VOID,
            Permissions.INVENTORY_VIEW, Permissions.INVENTORY_UPDATE, Permissions.INVENTORY_ADJUST, Permissions.INVENTORY_RESTOCK,
            Permissions.REPORT_VIEW, Permissions.REPORT_SALES, Permissions.REPORT_INVENTORY, Permissions.REPORT_USER_ACTIVITY, Permissions.REPORT_EXPORT,
            Permissions.SETTINGS_VIEW, Permissions.SETTINGS_UPDATE, Permissions.SETTINGS_SYSTEM, Permissions.SETTINGS_RECEIPT,
            Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE, Permissions.CUSTOMER_DELETE,
            Permissions.RESERVATION_VIEW, Permissions.RESERVATION_CREATE, Permissions.RESERVATION_UPDATE, Permissions.RESERVATION_CANCEL,
            Permissions.VENDOR_VIEW, Permissions.VENDOR_CREATE, Permissions.VENDOR_UPDATE, Permissions.VENDOR_DELETE, Permissions.VENDOR_ORDER,
            Permissions.SESSION_VIEW, Permissions.SESSION_MANAGE, Permissions.SESSION_CLOSE,
            Permissions.CAJA_OPEN, Permissions.CAJA_CLOSE, Permissions.CAJA_VIEW, Permissions.CAJA_VIEW_HISTORY
        };

        foreach (var permission in adminPermissions)
        {
            await using var cmd = new NpgsqlCommand(insertPermissionSql, conn);
            cmd.Parameters.AddWithValue("@roleId", "system-administrator");
            cmd.Parameters.AddWithValue("@permission", permission);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Manager permissions
        var managerPermissions = new[]
        {
            Permissions.ORDER_VIEW, Permissions.ORDER_CREATE, Permissions.ORDER_UPDATE, Permissions.ORDER_COMPLETE,
            Permissions.TABLE_VIEW, Permissions.TABLE_MANAGE, Permissions.TABLE_ASSIGN, Permissions.TABLE_CLEAR,
            Permissions.MENU_VIEW, Permissions.MENU_CREATE, Permissions.MENU_UPDATE, Permissions.MENU_PRICE_CHANGE,
            Permissions.PAYMENT_VIEW, Permissions.PAYMENT_PROCESS, Permissions.PAYMENT_REFUND,
            Permissions.INVENTORY_VIEW, Permissions.INVENTORY_UPDATE, Permissions.INVENTORY_ADJUST, Permissions.INVENTORY_RESTOCK,
            Permissions.REPORT_VIEW, Permissions.REPORT_SALES, Permissions.REPORT_INVENTORY, Permissions.REPORT_EXPORT,
            Permissions.SETTINGS_VIEW, Permissions.SETTINGS_UPDATE,
            Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
            Permissions.RESERVATION_VIEW, Permissions.RESERVATION_CREATE, Permissions.RESERVATION_UPDATE, Permissions.RESERVATION_CANCEL,
            Permissions.VENDOR_VIEW, Permissions.VENDOR_CREATE, Permissions.VENDOR_UPDATE, Permissions.VENDOR_ORDER,
            Permissions.SESSION_VIEW, Permissions.SESSION_MANAGE, Permissions.SESSION_CLOSE,
            Permissions.CAJA_OPEN, Permissions.CAJA_CLOSE, Permissions.CAJA_VIEW, Permissions.CAJA_VIEW_HISTORY
        };

        foreach (var permission in managerPermissions)
        {
            await using var cmd = new NpgsqlCommand(insertPermissionSql, conn);
            cmd.Parameters.AddWithValue("@roleId", "system-manager");
            cmd.Parameters.AddWithValue("@permission", permission);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Server permissions
        var serverPermissions = new[]
        {
            Permissions.ORDER_VIEW, Permissions.ORDER_CREATE, Permissions.ORDER_UPDATE, Permissions.ORDER_COMPLETE,
            Permissions.TABLE_VIEW, Permissions.TABLE_MANAGE, Permissions.TABLE_ASSIGN,
            Permissions.MENU_VIEW,
            Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
            Permissions.RESERVATION_VIEW, Permissions.RESERVATION_CREATE, Permissions.RESERVATION_UPDATE,
            Permissions.SESSION_VIEW
        };

        foreach (var permission in serverPermissions)
        {
            await using var cmd = new NpgsqlCommand(insertPermissionSql, conn);
            cmd.Parameters.AddWithValue("@roleId", "system-server");
            cmd.Parameters.AddWithValue("@permission", permission);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Cashier permissions
        var cashierPermissions = new[]
        {
            Permissions.ORDER_VIEW, Permissions.ORDER_UPDATE, Permissions.ORDER_COMPLETE,
            Permissions.MENU_VIEW,
            Permissions.PAYMENT_VIEW, Permissions.PAYMENT_PROCESS, Permissions.PAYMENT_REFUND,
            Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
            Permissions.SESSION_VIEW,
            Permissions.CAJA_CLOSE, Permissions.CAJA_VIEW
        };

        foreach (var permission in cashierPermissions)
        {
            await using var cmd = new NpgsqlCommand(insertPermissionSql, conn);
            cmd.Parameters.AddWithValue("@roleId", "system-cashier");
            cmd.Parameters.AddWithValue("@permission", permission);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Host permissions
        var hostPermissions = new[]
        {
            Permissions.TABLE_VIEW, Permissions.TABLE_MANAGE, Permissions.TABLE_ASSIGN,
            Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
            Permissions.RESERVATION_VIEW, Permissions.RESERVATION_CREATE, Permissions.RESERVATION_UPDATE, Permissions.RESERVATION_CANCEL,
            Permissions.SESSION_VIEW
        };

        foreach (var permission in hostPermissions)
        {
            await using var cmd = new NpgsqlCommand(insertPermissionSql, conn);
            cmd.Parameters.AddWithValue("@roleId", "system-host");
            cmd.Parameters.AddWithValue("@permission", permission);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
