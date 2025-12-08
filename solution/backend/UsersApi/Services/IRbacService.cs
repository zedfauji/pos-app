using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Services;

namespace UsersApi.Services;

/// <summary>
/// Interface for Role-Based Access Control operations
/// Extends the shared IRbacService interface
/// </summary>
public interface IRbacService : MagiDesk.Shared.Authorization.Services.IRbacService
{
    // Role Management
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByIdAsync(string roleId, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
    Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default);
    Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default);
    Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default);

    // Permission Management
    Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default);
    Task<string[]> GetRolePermissionsAsync(string roleId, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default);
    Task<bool> HasAnyPermissionAsync(string userId, string[] permissions, CancellationToken ct = default);
    Task<bool> HasAllPermissionsAsync(string userId, string[] permissions, CancellationToken ct = default);

    // Role Composition
    Task<string[]> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default);
    Task<bool> CanInheritFromRoleAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default);
    Task<bool> ValidateRoleCompositionAsync(CreateRoleRequest request, CancellationToken ct = default);

    // System Role Management
    Task InitializeSystemRolesAsync(CancellationToken ct = default);
    Task<RoleDto[]> GetSystemRolesAsync(CancellationToken ct = default);
    Task<RoleDto[]> GetCustomRolesAsync(CancellationToken ct = default);
}

/// <summary>
/// Interface for RBAC repository operations
/// </summary>
public interface IRbacRepository
{
    // Role Management
    Task<string> CreateRoleAsync(RoleDto role, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByIdAsync(string roleId, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
    Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default);
    Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default);
    Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default);

    // Permission Management
    Task<bool> SetRolePermissionsAsync(string roleId, string[] permissions, CancellationToken ct = default);
    Task<bool> SetRoleInheritanceAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default);
    Task<string[]> GetRolePermissionsAsync(string roleId, CancellationToken ct = default);
    Task<string[]> GetRoleInheritanceAsync(string roleId, CancellationToken ct = default);
    Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default);

    // Role Composition
    Task<string[]> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default);
    Task<bool> RoleExistsAsync(string roleName, string? excludeRoleId = null, CancellationToken ct = default);
    Task<bool> ValidateRoleInheritanceAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default);
}
