using MagiDesk.Shared.DTOs.Users;

namespace MagiDesk.Shared.Authorization.Services;

/// <summary>
/// Interface for Role-Based Access Control operations
/// This interface is shared across all APIs to enable centralized permission checking
/// </summary>
public interface IRbacService
{
    // Permission Management (Core methods needed by authorization handlers)
    Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default);
    Task<bool> HasAnyPermissionAsync(string userId, string[] permissions, CancellationToken ct = default);
    Task<bool> HasAllPermissionsAsync(string userId, string[] permissions, CancellationToken ct = default);

    // Role Management (Optional - for role management endpoints)
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByIdAsync(string roleId, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default);
    Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default);
    Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default);
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default);
    Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default);

    // Permission Management (Extended)
    Task<string[]> GetRolePermissionsAsync(string roleId, CancellationToken ct = default);

    // Role Composition
    Task<string[]> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default);
    Task<bool> CanInheritFromRoleAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default);
    Task<bool> ValidateRoleCompositionAsync(CreateRoleRequest request, CancellationToken ct = default);

    // System Role Management
    Task InitializeSystemRolesAsync(CancellationToken ct = default);
    Task<RoleDto[]> GetSystemRolesAsync(CancellationToken ct = default);
    Task<RoleDto[]> GetCustomRolesAsync(CancellationToken ct = default);
}

