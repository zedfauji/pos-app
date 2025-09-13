using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Users;

namespace UsersApi.Repositories;

public interface IUsersRepository
{
    Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetUsersAsync(UserSearchRequest request, CancellationToken ct = default);
    Task<string> CreateUserAsync(UserDto user, CancellationToken ct = default);
    Task<bool> UpdateUserAsync(string userId, MagiDesk.Shared.DTOs.Users.UpdateUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, string? excludeUserId = null, CancellationToken ct = default);
    Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default);
    Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken ct = default);
}
