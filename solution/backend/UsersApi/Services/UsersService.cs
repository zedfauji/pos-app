using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Users;
using static MagiDesk.Shared.DTOs.Users.UserRoles;
using UsersApi.Repositories;
using BCrypt.Net;

namespace UsersApi.Services;

public sealed class UsersService : IUsersService
{
    private readonly IUsersRepository _repository;

    public UsersService(IUsersRepository repository)
    {
        _repository = repository;
    }

    public Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken ct = default)
        => _repository.GetUserByIdAsync(userId, ct);

    public Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken ct = default)
        => _repository.GetUserByUsernameAsync(username, ct);

    public Task<PagedResult<UserDto>> GetUsersAsync(UserSearchRequest request, CancellationToken ct = default)
        => _repository.GetUsersAsync(request, ct);

    public async Task<UserDto> CreateUserAsync(MagiDesk.Shared.DTOs.Users.CreateUserRequest request, CancellationToken ct = default)
    {
        // Validate username uniqueness
        if (await _repository.UsernameExistsAsync(request.Username, null, ct))
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists");
        }

        // Validate role if provided
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!IsValidRole(request.Role))
            {
                throw new ArgumentException($"Invalid role. Must be one of: {string.Join(", ", AllRoles)}");
            }
        }

        var user = new UserDto
        {
            UserId = Guid.NewGuid().ToString(),
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var userId = await _repository.CreateUserAsync(user, ct);
        user.UserId = userId;
        
        return user;
    }

    public async Task<bool> UpdateUserAsync(string userId, MagiDesk.Shared.DTOs.Users.UpdateUserRequest request, CancellationToken ct = default)
    {
        // Check if user exists
        var existingUser = await _repository.GetUserByIdAsync(userId, ct);
        if (existingUser == null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found");
        }

        // Validate username uniqueness if changing username
        if (!string.IsNullOrWhiteSpace(request.Username) && 
            request.Username != existingUser.Username &&
            await _repository.UsernameExistsAsync(request.Username, userId, ct))
        {
            throw new InvalidOperationException($"Username '{request.Username}' already exists");
        }

        // Validate role if changing role
        if (!string.IsNullOrWhiteSpace(request.Role) && !IsValidRole(request.Role))
        {
            throw new ArgumentException($"Invalid role: {request.Role}");
        }

        return await _repository.UpdateUserAsync(userId, request, ct);
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        // Check if user exists
        var existingUser = await _repository.GetUserByIdAsync(userId, ct);
        if (existingUser == null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' not found");
        }

        return await _repository.DeleteUserAsync(userId, ct);
    }

    public Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default)
        => _repository.GetUserStatsAsync(ct);

    public Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken ct = default)
        => _repository.ValidateUserCredentialsAsync(username, password, ct);

    public Task<bool> PingAsync(CancellationToken ct = default)
    {
        // Simple ping - could add more sophisticated health checks
        return Task.FromResult(true);
    }

    private static bool IsValidRole(string role)
    {
        return role.ToLower() is "admin" or "employee";
    }
}
