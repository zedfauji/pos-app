using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Users;

namespace UsersApi.Services;

public interface IUsersService
{
    Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<UserDto?> GetUserByUsernameAsync(string username, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetUsersAsync(UserSearchRequest request, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(MagiDesk.Shared.DTOs.Users.CreateUserRequest request, CancellationToken ct = default);
    Task<bool> UpdateUserAsync(string userId, MagiDesk.Shared.DTOs.Users.UpdateUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default);
    Task<UserStatsDto> GetUserStatsAsync(CancellationToken ct = default);
    Task<bool> ValidateUserCredentialsAsync(string username, string password, CancellationToken ct = default);
    Task<bool> PingAsync(CancellationToken ct = default);
}
