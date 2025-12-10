using MagiDesk.Shared.DTOs.Users;
using Npgsql;
using UsersApi.Services;

namespace UsersApi.Repositories;

/// <summary>
/// Repository for Role-Based Access Control operations
/// </summary>
public sealed class RbacRepository : IRbacRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<RbacRepository> _logger;

    public RbacRepository(NpgsqlDataSource dataSource, ILogger<RbacRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    #region Role Management

    public async Task<string> CreateRoleAsync(RoleDto role, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            const string insertRoleSql = @"
                INSERT INTO users.roles (role_id, name, description, is_system_role, is_active, created_at, updated_at)
                VALUES (@roleId, @name, @description, @isSystemRole, @isActive, @createdAt, @updatedAt)
                RETURNING role_id";

            await using var cmd = new NpgsqlCommand(insertRoleSql, conn, transaction);
            cmd.Parameters.AddWithValue("@roleId", role.Id);
            cmd.Parameters.AddWithValue("@name", role.Name);
            cmd.Parameters.AddWithValue("@description", role.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@isSystemRole", role.IsSystemRole);
            cmd.Parameters.AddWithValue("@isActive", role.IsActive);
            cmd.Parameters.AddWithValue("@createdAt", role.CreatedAt);
            cmd.Parameters.AddWithValue("@updatedAt", role.UpdatedAt);

            var roleId = await cmd.ExecuteScalarAsync(ct) as string;
            if (string.IsNullOrEmpty(roleId))
            {
                throw new InvalidOperationException("Failed to create role");
            }

            await transaction.CommitAsync(ct);
            return roleId;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            SELECT r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at,
                   COALESCE(array_agg(DISTINCT rp.permission) FILTER (WHERE rp.permission IS NOT NULL), '{}') as permissions,
                   COALESCE(array_agg(DISTINCT ri.parent_role_id) FILTER (WHERE ri.parent_role_id IS NOT NULL), '{}') as inherit_from_roles
            FROM users.roles r
            LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
            LEFT JOIN users.role_inheritance ri ON r.role_id = ri.child_role_id
            WHERE r.role_id = @roleId AND r.is_deleted = false
            GROUP BY r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@roleId", roleId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return MapRoleFromReader(reader);
        }

        return null;
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            SELECT r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at,
                   COALESCE(array_agg(DISTINCT rp.permission) FILTER (WHERE rp.permission IS NOT NULL), '{}') as permissions,
                   COALESCE(array_agg(DISTINCT ri.parent_role_id) FILTER (WHERE ri.parent_role_id IS NOT NULL), '{}') as inherit_from_roles
            FROM users.roles r
            LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
            LEFT JOIN users.role_inheritance ri ON r.role_id = ri.child_role_id
            WHERE r.name = @roleName AND r.is_deleted = false
            GROUP BY r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@roleName", roleName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return MapRoleFromReader(reader);
        }

        return null;
    }

    public async Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var whereConditions = new List<string> { "r.is_deleted = false" };
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            whereConditions.Add("(r.name ILIKE @searchTerm OR r.description ILIKE @searchTerm)");
            parameters.Add(new NpgsqlParameter("@searchTerm", $"%{request.SearchTerm}%"));
        }

        if (request.IsSystemRole.HasValue)
        {
            whereConditions.Add("r.is_system_role = @isSystemRole");
            parameters.Add(new NpgsqlParameter("@isSystemRole", request.IsSystemRole.Value));
        }

        if (request.IsActive.HasValue)
        {
            whereConditions.Add("r.is_active = @isActive");
            parameters.Add(new NpgsqlParameter("@isActive", request.IsActive.Value));
        }

        var whereClause = whereConditions.Any() ? "WHERE " + string.Join(" AND ", whereConditions) : "";

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM users.roles r {whereClause}";
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        foreach (var param in parameters)
        {
            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
        }

        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        // Get paginated results
        var orderBy = request.SortDescending ? "DESC" : "ASC";
        var offset = (request.Page - 1) * request.PageSize;

        var sql = $@"
            SELECT r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at,
                   COALESCE(array_agg(DISTINCT rp.permission) FILTER (WHERE rp.permission IS NOT NULL), '{{}}') as permissions,
                   COALESCE(array_agg(DISTINCT ri.parent_role_id) FILTER (WHERE ri.parent_role_id IS NOT NULL), '{{}}') as inherit_from_roles
            FROM users.roles r
            LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
            LEFT JOIN users.role_inheritance ri ON r.role_id = ri.child_role_id
            {whereClause}
            GROUP BY r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at
            ORDER BY r.{request.SortBy.ToLower()} {orderBy}
            LIMIT @pageSize OFFSET @offset";

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
        }
        cmd.Parameters.Add(new NpgsqlParameter("@pageSize", request.PageSize));
        cmd.Parameters.Add(new NpgsqlParameter("@offset", offset));

        var roles = new List<RoleDto>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            roles.Add(MapRoleFromReader(reader));
        }

        return new PagedResult<RoleDto>
        {
            Items = roles,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var updateFields = new List<string>();
        var parameters = new List<NpgsqlParameter> { new("@roleId", roleId) };

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            updateFields.Add("description = @description");
            parameters.Add(new NpgsqlParameter("@description", request.Description));
        }

        if (request.IsActive.HasValue)
        {
            updateFields.Add("is_active = @isActive");
            parameters.Add(new NpgsqlParameter("@isActive", request.IsActive.Value));
        }

        if (!updateFields.Any())
        {
            return true; // Nothing to update
        }

        updateFields.Add("updated_at = @updatedAt");
        parameters.Add(new NpgsqlParameter("@updatedAt", DateTime.UtcNow));

        var sql = $"UPDATE users.roles SET {string.Join(", ", updateFields)} WHERE role_id = @roleId AND is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            UPDATE users.roles 
            SET is_deleted = true, updated_at = @updatedAt 
            WHERE role_id = @roleId AND is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@roleId", roleId);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);

        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                COUNT(*) as total_roles,
                COUNT(*) FILTER (WHERE is_system_role = true) as system_roles,
                COUNT(*) FILTER (WHERE is_system_role = false) as custom_roles,
                COUNT(*) FILTER (WHERE is_active = true) as active_roles,
                COUNT(*) FILTER (WHERE is_active = false) as inactive_roles,
                MAX(created_at) as last_role_created
            FROM users.roles 
            WHERE is_deleted = false";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (await reader.ReadAsync(ct))
        {
            return new RoleStatsDto
            {
                TotalRoles = reader.GetInt32(0),
                SystemRoles = reader.GetInt32(1),
                CustomRoles = reader.GetInt32(2),
                ActiveRoles = reader.GetInt32(3),
                InactiveRoles = reader.GetInt32(4),
                LastRoleCreated = reader.GetDateTime(5)
            };
        }

        return new RoleStatsDto();
    }

    #endregion

    #region Permission Management

    public async Task<bool> SetRolePermissionsAsync(string roleId, string[] permissions, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            // Delete existing permissions
            const string deleteSql = "DELETE FROM users.role_permissions WHERE role_id = @roleId";
            await using var deleteCmd = new NpgsqlCommand(deleteSql, conn, transaction);
            deleteCmd.Parameters.AddWithValue("@roleId", roleId);
            await deleteCmd.ExecuteNonQueryAsync(ct);

            // Insert new permissions
            if (permissions.Any())
            {
                const string insertSql = "INSERT INTO users.role_permissions (role_id, permission) VALUES (@roleId, @permission)";
                await using var insertCmd = new NpgsqlCommand(insertSql, conn, transaction);
                insertCmd.Parameters.AddWithValue("@roleId", roleId);

                foreach (var permission in permissions)
                {
                    insertCmd.Parameters["@permission"].Value = permission;
                    await insertCmd.ExecuteNonQueryAsync(ct);
                }
            }

            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> SetRoleInheritanceAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            // Delete existing inheritance
            const string deleteSql = "DELETE FROM users.role_inheritance WHERE child_role_id = @roleId";
            await using var deleteCmd = new NpgsqlCommand(deleteSql, conn, transaction);
            deleteCmd.Parameters.AddWithValue("@roleId", roleId);
            await deleteCmd.ExecuteNonQueryAsync(ct);

            // Insert new inheritance
            if (parentRoleIds.Any())
            {
                const string insertSql = "INSERT INTO users.role_inheritance (child_role_id, parent_role_id) VALUES (@roleId, @parentRoleId)";
                await using var insertCmd = new NpgsqlCommand(insertSql, conn, transaction);
                insertCmd.Parameters.AddWithValue("@roleId", roleId);

                foreach (var parentRoleId in parentRoleIds)
                {
                    insertCmd.Parameters["@parentRoleId"].Value = parentRoleId;
                    await insertCmd.ExecuteNonQueryAsync(ct);
                }
            }

            await transaction.CommitAsync(ct);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<string[]> GetRolePermissionsAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = "SELECT permission FROM users.role_permissions WHERE role_id = @roleId";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@roleId", roleId);

        var permissions = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            permissions.Add(reader.GetString(0));
        }

        return permissions.ToArray();
    }

    public async Task<string[]> GetRoleInheritanceAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = "SELECT parent_role_id FROM users.role_inheritance WHERE child_role_id = @roleId";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@roleId", roleId);

        var parentRoles = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            parentRoles.Add(reader.GetString(0));
        }

        return parentRoles.ToArray();
    }

    public async Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            WITH RECURSIVE role_permissions AS (
                -- Direct role permissions
                SELECT rp.permission
                FROM users.role_permissions rp
                JOIN users.users u ON u.role = r.name
                WHERE u.user_id = @userId AND u.is_active = true AND u.is_deleted = false
                
                UNION
                
                -- Inherited permissions
                SELECT rp.permission
                FROM users.role_permissions rp
                JOIN users.role_inheritance ri ON rp.role_id = ri.parent_role_id
                JOIN users.users u ON u.role = r.name
                WHERE u.user_id = @userId AND u.is_active = true AND u.is_deleted = false
            )
            SELECT DISTINCT permission FROM role_permissions";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);

        var permissions = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            permissions.Add(reader.GetString(0));
        }

        return permissions.ToArray();
    }

    #endregion

    #region Role Composition

    public async Task<string[]> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            WITH RECURSIVE role_permissions AS (
                -- Direct permissions
                SELECT permission
                FROM users.role_permissions
                WHERE role_id = @roleId
                
                UNION
                
                -- Inherited permissions
                SELECT rp.permission
                FROM users.role_permissions rp
                JOIN users.role_inheritance ri ON rp.role_id = ri.parent_role_id
                WHERE ri.child_role_id = @roleId
            )
            SELECT DISTINCT permission FROM role_permissions";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@roleId", roleId);

        var permissions = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            permissions.Add(reader.GetString(0));
        }

        return permissions.ToArray();
    }

    public async Task<bool> RoleExistsAsync(string roleName, string? excludeRoleId = null, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var sql = "SELECT COUNT(*) FROM users.roles WHERE name = @roleName AND is_deleted = false";
        var parameters = new List<NpgsqlParameter> { new("@roleName", roleName) };

        if (!string.IsNullOrEmpty(excludeRoleId))
        {
            sql += " AND role_id != @excludeRoleId";
            parameters.Add(new NpgsqlParameter("@excludeRoleId", excludeRoleId));
        }

        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        return count > 0;
    }

    public async Task<bool> ValidateRoleInheritanceAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default)
    {
        // Check for circular inheritance
        foreach (var parentRoleId in parentRoleIds)
        {
            if (parentRoleId == roleId)
            {
                return false; // Can't inherit from self
            }

            // Check if parent role would inherit from this role (circular dependency)
            var parentEffectivePermissions = await GetEffectivePermissionsAsync(parentRoleId, ct);
            // This is a simplified check - in a real implementation, you'd need to traverse the inheritance tree
        }

        return true;
    }

    #endregion

    #region Helper Methods

    private static RoleDto MapRoleFromReader(NpgsqlDataReader reader)
    {
        // Handle array fields safely - PostgreSQL arrays need special handling
        var permissions = new List<string>();
        var inheritFromRoles = new List<string>();
        
        try
        {
            // Get the column index for permissions array
            var permissionsIndex = reader.GetOrdinal("permissions");
            if (!reader.IsDBNull(permissionsIndex))
            {
                // Use GetValue and cast to string array
                var permValue = reader.GetValue(permissionsIndex);
                if (permValue is string[] permArray)
                {
                    permissions.AddRange(permArray);
                }
                else if (permValue is Array array)
                {
                    foreach (var item in array)
                    {
                        if (item != null)
                            permissions.Add(item.ToString()!);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the entire operation
            Console.WriteLine($"Error reading permissions array: {ex.Message}");
        }
        
        try
        {
            // Get the column index for inherit_from_roles array
            var inheritIndex = reader.GetOrdinal("inherit_from_roles");
            if (!reader.IsDBNull(inheritIndex))
            {
                // Use GetValue and cast to string array
                var inheritValue = reader.GetValue(inheritIndex);
                if (inheritValue is string[] inheritArray)
                {
                    inheritFromRoles.AddRange(inheritArray);
                }
                else if (inheritValue is Array array)
                {
                    foreach (var item in array)
                    {
                        if (item != null)
                            inheritFromRoles.Add(item.ToString()!);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the entire operation
            Console.WriteLine($"Error reading inherit_from_roles array: {ex.Message}");
        }

        return new RoleDto
        {
            Id = reader.GetString(reader.GetOrdinal("role_id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            Permissions = permissions.ToArray(),
            InheritFromRoles = inheritFromRoles.ToArray(),
            IsSystemRole = reader.GetBoolean(reader.GetOrdinal("is_system_role")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    #endregion
}
