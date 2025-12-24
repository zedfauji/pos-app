using MagiDesk.Shared.DTOs.Users;
using Npgsql;
using Dapper;
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
                VALUES (@Id, @Name, @Description, @IsSystemRole, @IsActive, @CreatedAt, @UpdatedAt)
                RETURNING role_id";

            var roleId = await conn.ExecuteScalarAsync<string>(insertRoleSql, new
            {
                role.Id,
                role.Name,
                Description = role.Description ?? (object)DBNull.Value,
                role.IsSystemRole,
                role.IsActive,
                role.CreatedAt,
                role.UpdatedAt
            }, transaction);

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
            SELECT r.role_id as Id, r.name as Name, r.description as Description, 
                   r.is_system_role as IsSystemRole, r.is_active as IsActive, 
                   r.created_at as CreatedAt, r.updated_at as UpdatedAt,
                   COALESCE(array_agg(DISTINCT rp.permission) FILTER (WHERE rp.permission IS NOT NULL), '{}') as Permissions,
                   COALESCE(array_agg(DISTINCT ri.parent_role_id) FILTER (WHERE ri.parent_role_id IS NOT NULL), '{}') as InheritFromRoles
            FROM users.roles r
            LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
            LEFT JOIN users.role_inheritance ri ON r.role_id = ri.child_role_id
            WHERE r.role_id = @RoleId AND r.is_deleted = false
            GROUP BY r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at";

        return await conn.QuerySingleOrDefaultAsync<RoleDto>(sql, new { RoleId = roleId });
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            SELECT r.role_id as Id, r.name as Name, r.description as Description, 
                   r.is_system_role as IsSystemRole, r.is_active as IsActive, 
                   r.created_at as CreatedAt, r.updated_at as UpdatedAt,
                   COALESCE(array_agg(DISTINCT rp.permission) FILTER (WHERE rp.permission IS NOT NULL), '{}') as Permissions,
                   COALESCE(array_agg(DISTINCT ri.parent_role_id) FILTER (WHERE ri.parent_role_id IS NOT NULL), '{}') as InheritFromRoles
            FROM users.roles r
            LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
            LEFT JOIN users.role_inheritance ri ON r.role_id = ri.child_role_id
            WHERE r.name = @RoleName AND r.is_deleted = false
            GROUP BY r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at";

        return await conn.QuerySingleOrDefaultAsync<RoleDto>(sql, new { RoleName = roleName });
    }

    public async Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        var sqlBuilder = new SqlBuilder();
        var template = sqlBuilder.AddTemplate(@"
             SELECT r.role_id as Id, r.name as Name, r.description as Description, 
                   r.is_system_role as IsSystemRole, r.is_active as IsActive, 
                   r.created_at as CreatedAt, r.updated_at as UpdatedAt,
                   COALESCE(array_agg(DISTINCT rp.permission) FILTER (WHERE rp.permission IS NOT NULL), '{}') as Permissions,
                   COALESCE(array_agg(DISTINCT ri.parent_role_id) FILTER (WHERE ri.parent_role_id IS NOT NULL), '{}') as InheritFromRoles
            FROM users.roles r
            LEFT JOIN users.role_permissions rp ON r.role_id = rp.role_id
            LEFT JOIN users.role_inheritance ri ON r.role_id = ri.child_role_id
            /**where**/
            GROUP BY r.role_id, r.name, r.description, r.is_system_role, r.is_active, r.created_at, r.updated_at
            /**orderby**/
            LIMIT @PageSize OFFSET @Offset");
        
        var countTemplate = sqlBuilder.AddTemplate("SELECT COUNT(*) FROM users.roles r /**where**/");

        sqlBuilder.Where("r.is_deleted = false");

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            sqlBuilder.Where("(r.name ILIKE @SearchTerm OR r.description ILIKE @SearchTerm)", new { SearchTerm = $"%{request.SearchTerm}%" });
        }

        if (request.IsSystemRole.HasValue)
        {
            sqlBuilder.Where("r.is_system_role = @IsSystemRole", new { IsSystemRole = request.IsSystemRole.Value });
        }

        if (request.IsActive.HasValue)
        {
            sqlBuilder.Where("r.is_active = @IsActive", new { IsActive = request.IsActive.Value });
        }

        var orderByDir = request.SortDescending ? "DESC" : "ASC";
        var sortBy = request.SortBy.ToLower() switch 
        {
            "name" => "r.name",
            "createdat" => "r.created_at",
            "updatedat" => "r.updated_at",
            _ => "r.name"
        };
        sqlBuilder.OrderBy($"{sortBy} {orderByDir}");

        var offset = (request.Page - 1) * request.PageSize;

        var totalCount = await conn.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);
        
        // Fix: AddDynamicParams returns void, so separate calls needed
        var parameters = new DynamicParameters(template.Parameters);
        parameters.AddDynamicParams(new { request.PageSize, Offset = offset });
        var roles = await conn.QueryAsync<RoleDto>(template.RawSql, parameters);

        return new PagedResult<RoleDto>
        {
            Items = roles.ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var sqlBuilder = new SqlBuilder();
        var template = sqlBuilder.AddTemplate("UPDATE users.roles SET /**set**/ WHERE role_id = @RoleId AND is_deleted = false", new { RoleId = roleId });

        bool hasUpdate = false;
        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            sqlBuilder.Set("description = @Description", new { request.Description });
            hasUpdate = true;
        }

        if (request.IsActive.HasValue)
        {
            sqlBuilder.Set("is_active = @IsActive", new { IsActive = request.IsActive.Value });
            hasUpdate = true;
        }

        if (!hasUpdate)
        {
            return true;
        }

        sqlBuilder.Set("updated_at = @UpdatedAt", new { UpdatedAt = DateTime.UtcNow });

        var rowsAffected = await conn.ExecuteAsync(template.RawSql, template.Parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            UPDATE users.roles 
            SET is_deleted = true, updated_at = @UpdatedAt 
            WHERE role_id = @RoleId AND is_deleted = false";

        var rowsAffected = await conn.ExecuteAsync(sql, new { RoleId = roleId, UpdatedAt = DateTime.UtcNow });
        return rowsAffected > 0;
    }

    public async Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                COUNT(*) as TotalRoles,
                COUNT(*) FILTER (WHERE is_system_role = true) as SystemRoles,
                COUNT(*) FILTER (WHERE is_system_role = false) as CustomRoles,
                COUNT(*) FILTER (WHERE is_active = true) as ActiveRoles,
                COUNT(*) FILTER (WHERE is_active = false) as InactiveRoles,
                COALESCE(MAX(created_at), '0001-01-01') as LastRoleCreated
            FROM users.roles 
            WHERE is_deleted = false";

        return await conn.QuerySingleOrDefaultAsync<RoleStatsDto>(sql) ?? new RoleStatsDto();
    }

    #endregion

    #region Permission Management

    public async Task<bool> SetRolePermissionsAsync(string roleId, string[] permissions, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var transaction = await conn.BeginTransactionAsync(ct);

        try
        {
            await conn.ExecuteAsync("DELETE FROM users.role_permissions WHERE role_id = @RoleId", new { RoleId = roleId }, transaction);

            if (permissions.Any())
            {
                const string insertSql = "INSERT INTO users.role_permissions (role_id, permission) VALUES (@RoleId, @Permission)";
                foreach (var permission in permissions)
                {
                    await conn.ExecuteAsync(insertSql, new { RoleId = roleId, Permission = permission }, transaction);
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
            await conn.ExecuteAsync("DELETE FROM users.role_inheritance WHERE child_role_id = @RoleId", new { RoleId = roleId }, transaction);

            if (parentRoleIds.Any())
            {
                const string insertSql = "INSERT INTO users.role_inheritance (child_role_id, parent_role_id) VALUES (@RoleId, @ParentRoleId)";
                foreach (var parentRoleId in parentRoleIds)
                {
                    await conn.ExecuteAsync(insertSql, new { RoleId = roleId, ParentRoleId = parentRoleId }, transaction);
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
        const string sql = "SELECT permission FROM users.role_permissions WHERE role_id = @RoleId";
        var result = await conn.QueryAsync<string>(sql, new { RoleId = roleId });
        return result.ToArray();
    }

    public async Task<string[]> GetRoleInheritanceAsync(string roleId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "SELECT parent_role_id FROM users.role_inheritance WHERE child_role_id = @RoleId";
        var result = await conn.QueryAsync<string>(sql, new { RoleId = roleId });
        return result.ToArray();
    }

    public async Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        const string sql = @"
            WITH RECURSIVE role_permissions AS (
                -- Direct role permissions
                SELECT rp.permission
                FROM users.role_permissions rp
                JOIN users.roles r ON rp.role_id = r.role_id
                JOIN users.users u ON u.role = r.name
                WHERE u.user_id = @UserId AND u.is_active = true AND u.is_deleted = false
                
                UNION
                
                -- Inherited permissions
                SELECT rp.permission
                FROM users.role_permissions rp
                JOIN users.role_inheritance ri ON rp.role_id = ri.parent_role_id
                JOIN users.roles r ON ri.child_role_id = r.role_id
                JOIN users.users u ON u.role = r.name
                WHERE u.user_id = @UserId AND u.is_active = true AND u.is_deleted = false
            )
            SELECT DISTINCT permission FROM role_permissions";

        var result = await conn.QueryAsync<string>(sql, new { UserId = userId });
        return result.ToArray();
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
                WHERE role_id = @RoleId
                
                UNION
                
                -- Inherited permissions
                SELECT rp.permission
                FROM users.role_permissions rp
                JOIN users.role_inheritance ri ON rp.role_id = ri.parent_role_id
                WHERE ri.child_role_id = @RoleId
            )
            SELECT DISTINCT permission FROM role_permissions";

        var result = await conn.QueryAsync<string>(sql, new { RoleId = roleId });
        return result.ToArray();
    }

    public async Task<bool> RoleExistsAsync(string roleName, string? excludeRoleId = null, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);

        var sql = "SELECT COUNT(*) FROM users.roles WHERE name = @RoleName AND is_deleted = false";
        var param = new DynamicParameters();
        param.Add("RoleName", roleName);

        if (!string.IsNullOrEmpty(excludeRoleId))
        {
            sql += " AND role_id != @ExcludeRoleId";
            param.Add("ExcludeRoleId", excludeRoleId);
        }

        var count = await conn.ExecuteScalarAsync<int>(sql, param);
        return count > 0;
    }

    public async Task<bool> ValidateRoleInheritanceAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default)
    {
        foreach (var parentRoleId in parentRoleIds)
        {
            if (parentRoleId == roleId) return false;
        }
        return true;
    }

    #endregion
}
