using Npgsql;
using Dapper;
using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Users;
using BCrypt.Net;

namespace UsersApi.Repositories;

public sealed class UsersRepository : IUsersRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public UsersRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT user_id as UserId, username as Username, password_hash as PasswordHash, 
                                    role as Role, created_at as CreatedAt, updated_at as UpdatedAt, is_active as IsActive 
                            FROM users.users WHERE user_id = @UserId AND is_deleted = false";
        return await conn.QuerySingleOrDefaultAsync<UserDto>(sql, new { UserId = userId });
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT user_id as UserId, username as Username, password_hash as PasswordHash, 
                                    role as Role, created_at as CreatedAt, updated_at as UpdatedAt, is_active as IsActive 
                            FROM users.users WHERE LOWER(username) = LOWER(@Username) AND is_deleted = false";
        return await conn.QuerySingleOrDefaultAsync<UserDto>(sql, new { Username = username });
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserSearchRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        var sqlBuilder = new SqlBuilder();
        var template = sqlBuilder.AddTemplate(@"
            SELECT user_id as UserId, username as Username, password_hash as PasswordHash, 
                   role as Role, created_at as CreatedAt, updated_at as UpdatedAt, is_active as IsActive 
            FROM users.users /**where**/ /**orderby**/ LIMIT @PageSize OFFSET @Offset");
        
        var countTemplate = sqlBuilder.AddTemplate("SELECT COUNT(*) FROM users.users /**where**/");

        sqlBuilder.Where("is_deleted = false");

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            sqlBuilder.Where("LOWER(username) LIKE LOWER(@SearchTerm)", new { SearchTerm = $"%{request.SearchTerm}%" });
        }
        
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            sqlBuilder.Where("role = @Role", new { request.Role });
        }
        
        if (request.IsActive.HasValue)
        {
            sqlBuilder.Where("is_active = @IsActive", new { IsActive = request.IsActive.Value });
        }

        var orderBy = GetOrderByClause(request.SortBy, request.SortDescending);
        sqlBuilder.OrderBy(orderBy);

        // Calculate offset
        var offset = (request.Page - 1) * request.PageSize;

        // Execute queries
        var totalCount = await conn.ExecuteScalarAsync<int>(countTemplate.RawSql, countTemplate.Parameters);
        
        // Fix: AddDynamicParams returns void, so chained call failed.
        var parameters = new DynamicParameters(template.Parameters);
        parameters.AddDynamicParams(new { request.PageSize, Offset = offset });
        
        var users = await conn.QueryAsync<UserDto>(template.RawSql, parameters);

        return new PagedResult<UserDto>
        {
            Items = users.ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<string> CreateUserAsync(UserDto user, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"INSERT INTO users.users (user_id, username, password_hash, role, created_at, updated_at, is_active)
                           VALUES (@UserId, @Username, @PasswordHash, @Role, @CreatedAt, @UpdatedAt, @IsActive)
                           RETURNING user_id";
        
        var userId = user.UserId ?? Guid.NewGuid().ToString();
        var result = await conn.ExecuteScalarAsync<string>(sql, new 
        {
            UserId = userId,
            user.Username,
            user.PasswordHash,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt,
            user.IsActive
        });
        
        return result ?? throw new InvalidOperationException("Failed to create user");
    }

    public async Task<bool> UpdateUserAsync(string userId, MagiDesk.Shared.DTOs.Users.UpdateUserRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        var sqlBuilder = new SqlBuilder();
        var template = sqlBuilder.AddTemplate("UPDATE users.users SET /**set**/ WHERE user_id = @UserId AND is_deleted = false", new { UserId = userId });

        sqlBuilder.Set("updated_at = @UpdatedAt", new { UpdatedAt = DateTime.UtcNow });

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            sqlBuilder.Set("username = @Username", new { request.Username });
        }
        
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            sqlBuilder.Set("password_hash = @PasswordHash", new { PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password) });
        }
        
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            sqlBuilder.Set("role = @Role", new { request.Role });
        }
        
        if (request.IsActive.HasValue)
        {
            sqlBuilder.Set("is_active = @IsActive", new { IsActive = request.IsActive.Value });
        }
        
        var rowsAffected = await conn.ExecuteAsync(template.RawSql, template.Parameters);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "UPDATE users.users SET is_deleted = true, updated_at = @UpdatedAt WHERE user_id = @UserId";
        
        var rowsAffected = await conn.ExecuteAsync(sql, new { UserId = userId, UpdatedAt = DateTime.UtcNow });
        return rowsAffected > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username, string? excludeUserId = null, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        var sql = "SELECT COUNT(*) FROM users.users WHERE LOWER(username) = LOWER(@Username) AND is_deleted = false";
        var param = new DynamicParameters();
        param.Add("Username", username);

        if (!string.IsNullOrWhiteSpace(excludeUserId))
        {
            sql += " AND user_id != @ExcludeUserId";
            param.Add("ExcludeUserId", excludeUserId);
        }
        
        var count = await conn.ExecuteScalarAsync<int>(sql, param);
        return count > 0;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
            COUNT(*) as TotalUsers,
            COUNT(*) FILTER (WHERE is_active = true) as ActiveUsers,
            COUNT(*) FILTER (WHERE role = 'admin') as AdminUsers,
            COUNT(*) FILTER (WHERE role = 'employee') as EmployeeUsers,
            COALESCE(MAX(created_at), '0001-01-01') as LastUserCreated
            FROM users.users WHERE is_deleted = false";
        
        return await conn.QuerySingleOrDefaultAsync<UserStatsDto>(sql) ?? new UserStatsDto();
    }

    public async Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await GetUserByUsernameAsync(username, ct);
        if (user == null || !user.IsActive) return false;
        
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    private static string GetOrderByClause(string sortBy, bool sortDescending)
    {
        var direction = sortDescending ? "DESC" : "ASC";
        return sortBy.ToLower() switch
        {
            "username" => $"username {direction}",
            "role" => $"role {direction}, username ASC",
            "createdat" => $"created_at {direction}",
            "updatedat" => $"updated_at {direction}",
            "isactive" => $"is_active {direction}, username ASC",
            _ => $"username {direction}"
        };
    }
}
