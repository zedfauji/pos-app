using Npgsql;
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
        const string sql = @"SELECT user_id, username, password_hash, role, created_at, updated_at, is_active 
                           FROM users.users WHERE user_id = @userId AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        
        return new UserDto
        {
            UserId = rdr.GetString(0),
            Username = rdr.GetString(1),
            PasswordHash = rdr.GetString(2),
            Role = rdr.GetString(3),
            CreatedAt = rdr.GetDateTime(4),
            UpdatedAt = rdr.GetDateTime(5),
            IsActive = rdr.GetBoolean(6)
        };
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT user_id, username, password_hash, role, created_at, updated_at, is_active 
                           FROM users.users WHERE LOWER(username) = LOWER(@username) AND is_deleted = false";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@username", username);
        
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        if (!await rdr.ReadAsync(ct)) return null;
        
        return new UserDto
        {
            UserId = rdr.GetString(0),
            Username = rdr.GetString(1),
            PasswordHash = rdr.GetString(2),
            Role = rdr.GetString(3),
            CreatedAt = rdr.GetDateTime(4),
            UpdatedAt = rdr.GetDateTime(5),
            IsActive = rdr.GetBoolean(6)
        };
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(UserSearchRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        var whereConditions = new List<string> { "is_deleted = false" };
        var parameters = new List<NpgsqlParameter>();
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            whereConditions.Add("LOWER(username) LIKE LOWER(@searchTerm)");
            parameters.Add(new NpgsqlParameter("@searchTerm", $"%{request.SearchTerm}%"));
        }
        
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            whereConditions.Add("role = @role");
            parameters.Add(new NpgsqlParameter("@role", request.Role));
        }
        
        if (request.IsActive.HasValue)
        {
            whereConditions.Add("is_active = @isActive");
            parameters.Add(new NpgsqlParameter("@isActive", request.IsActive.Value));
        }
        
        var whereClause = string.Join(" AND ", whereConditions);
        var orderBy = GetOrderByClause(request.SortBy, request.SortDescending);
        
        // Get total count
        var countSql = $"SELECT COUNT(*) FROM users.users WHERE {whereClause}";
        await using var countCmd = new NpgsqlCommand(countSql, conn);
        foreach (var param in parameters)
        {
            countCmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
        }
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));
        
        // Get paged results
        var offset = (request.Page - 1) * request.PageSize;
        var sql = $@"SELECT user_id, username, password_hash, role, created_at, updated_at, is_active 
                   FROM users.users WHERE {whereClause} 
                   ORDER BY {orderBy} 
                   LIMIT @pageSize OFFSET @offset";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(new NpgsqlParameter(param.ParameterName, param.Value));
        }
        cmd.Parameters.AddWithValue("@pageSize", request.PageSize);
        cmd.Parameters.AddWithValue("@offset", offset);
        
        var users = new List<UserDto>();
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            users.Add(new UserDto
            {
                UserId = rdr.GetString(0),
                Username = rdr.GetString(1),
                PasswordHash = rdr.GetString(2),
                Role = rdr.GetString(3),
                CreatedAt = rdr.GetDateTime(4),
                UpdatedAt = rdr.GetDateTime(5),
                IsActive = rdr.GetBoolean(6)
            });
        }
        
        return new PagedResult<UserDto>
        {
            Items = users,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<string> CreateUserAsync(UserDto user, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"INSERT INTO users.users (user_id, username, password_hash, role, created_at, updated_at, is_active)
                           VALUES (@userId, @username, @passwordHash, @role, @createdAt, @updatedAt, @isActive)
                           RETURNING user_id";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", user.UserId ?? Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@role", user.Role);
        cmd.Parameters.AddWithValue("@createdAt", user.CreatedAt);
        cmd.Parameters.AddWithValue("@updatedAt", user.UpdatedAt);
        cmd.Parameters.AddWithValue("@isActive", user.IsActive);
        
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString() ?? throw new InvalidOperationException("Failed to create user");
    }

    public async Task<bool> UpdateUserAsync(string userId, MagiDesk.Shared.DTOs.Users.UpdateUserRequest request, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        
        var setClause = new List<string> { "updated_at = @updatedAt" };
        var parameters = new List<NpgsqlParameter>
        {
            new("@userId", userId),
            new("@updatedAt", DateTime.UtcNow)
        };
        
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            setClause.Add("username = @username");
            parameters.Add(new NpgsqlParameter("@username", request.Username));
        }
        
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            setClause.Add("password_hash = @passwordHash");
            parameters.Add(new NpgsqlParameter("@passwordHash", BCrypt.Net.BCrypt.HashPassword(request.Password)));
        }
        
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            setClause.Add("role = @role");
            parameters.Add(new NpgsqlParameter("@role", request.Role));
        }
        
        if (request.IsActive.HasValue)
        {
            setClause.Add("is_active = @isActive");
            parameters.Add(new NpgsqlParameter("@isActive", request.IsActive.Value));
        }
        
        var sql = $"UPDATE users.users SET {string.Join(", ", setClause)} WHERE user_id = @userId AND is_deleted = false";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }
        
        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = "UPDATE users.users SET is_deleted = true, updated_at = @updatedAt WHERE user_id = @userId";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        
        var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
        return rowsAffected > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username, string? excludeUserId = null, CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var sql = "SELECT COUNT(*) FROM users.users WHERE LOWER(username) = LOWER(@username) AND is_deleted = false";
        var parameters = new List<NpgsqlParameter> { new("@username", username) };
        
        if (!string.IsNullOrWhiteSpace(excludeUserId))
        {
            sql += " AND user_id != @excludeUserId";
            parameters.Add(new NpgsqlParameter("@excludeUserId", excludeUserId));
        }
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var param in parameters)
        {
            cmd.Parameters.Add(param);
        }
        
        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        return count > 0;
    }

    public async Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        const string sql = @"SELECT 
            COUNT(*) as total_users,
            COUNT(*) FILTER (WHERE is_active = true) as active_users,
            COUNT(*) FILTER (WHERE role = 'admin') as admin_users,
            COUNT(*) FILTER (WHERE role = 'employee') as employee_users,
            MAX(created_at) as last_user_created
            FROM users.users WHERE is_deleted = false";
        
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var rdr = await cmd.ExecuteReaderAsync(ct);
        
        if (!await rdr.ReadAsync(ct))
        {
            return new UserStatsDto();
        }
        
        return new UserStatsDto
        {
            TotalUsers = rdr.GetInt32(0),
            ActiveUsers = rdr.GetInt32(1),
            AdminUsers = rdr.GetInt32(2),
            EmployeeUsers = rdr.GetInt32(3),
            LastUserCreated = rdr.IsDBNull(4) ? DateTime.MinValue : rdr.GetDateTime(4)
        };
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
