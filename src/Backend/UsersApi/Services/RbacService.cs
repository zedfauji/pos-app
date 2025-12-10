using MagiDesk.Shared.DTOs.Users;
using UsersApi.Repositories;

namespace UsersApi.Services;

/// <summary>
/// Service for Role-Based Access Control operations
/// </summary>
public sealed class RbacService : IRbacService
{
    private readonly IRbacRepository _repository;
    private readonly ILogger<RbacService> _logger;

    public RbacService(IRbacRepository repository, ILogger<RbacService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    #region Role Management

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        // Validate role name uniqueness
        if (await _repository.RoleExistsAsync(request.Name, null, ct))
        {
            throw new InvalidOperationException($"Role '{request.Name}' already exists");
        }

        // Validate permissions
        var invalidPermissions = request.Permissions.Where(p => !Permissions.IsValidPermission(p)).ToArray();
        if (invalidPermissions.Any())
        {
            throw new ArgumentException($"Invalid permissions: {string.Join(", ", invalidPermissions)}");
        }

        // Validate role inheritance
        if (request.InheritFromRoles?.Any() == true)
        {
            var invalidRoles = new List<string>();
            foreach (var roleName in request.InheritFromRoles)
            {
                var parentRole = await _repository.GetRoleByNameAsync(roleName, ct);
                if (parentRole == null)
                {
                    invalidRoles.Add(roleName);
                }
            }

            if (invalidRoles.Any())
            {
                throw new ArgumentException($"Invalid parent roles: {string.Join(", ", invalidRoles)}");
            }
        }

        // Create role
        var role = new RoleDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Permissions = request.Permissions,
            InheritFromRoles = request.InheritFromRoles ?? Array.Empty<string>(),
            IsSystemRole = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var roleId = await _repository.CreateRoleAsync(role, ct);
        role = role with { Id = roleId };

        // Set permissions and inheritance
        await _repository.SetRolePermissionsAsync(roleId, request.Permissions, ct);
        if (request.InheritFromRoles?.Any() == true)
        {
            await _repository.SetRoleInheritanceAsync(roleId, request.InheritFromRoles, ct);
        }

        _logger.LogInformation("Created custom role: {RoleName} with {PermissionCount} permissions", 
            request.Name, request.Permissions.Length);

        return role;
    }

    public Task<RoleDto?> GetRoleByIdAsync(string roleId, CancellationToken ct = default)
        => _repository.GetRoleByIdAsync(roleId, ct);

    public Task<RoleDto?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        => _repository.GetRoleByNameAsync(roleName, ct);

    public Task<PagedResult<RoleDto>> GetRolesAsync(RoleSearchRequest request, CancellationToken ct = default)
        => _repository.GetRolesAsync(request, ct);

    public async Task<bool> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var existingRole = await _repository.GetRoleByIdAsync(roleId, ct);
        if (existingRole == null)
        {
            throw new KeyNotFoundException($"Role with ID '{roleId}' not found");
        }

        if (existingRole.IsSystemRole)
        {
            throw new InvalidOperationException("Cannot modify system roles");
        }

        // Validate permissions if provided
        if (request.Permissions?.Any() == true)
        {
            var invalidPermissions = request.Permissions.Where(p => !Permissions.IsValidPermission(p)).ToArray();
            if (invalidPermissions.Any())
            {
                throw new ArgumentException($"Invalid permissions: {string.Join(", ", invalidPermissions)}");
            }
        }

        // Validate role inheritance if provided
        if (request.InheritFromRoles?.Any() == true)
        {
            var invalidRoles = new List<string>();
            foreach (var roleName in request.InheritFromRoles)
            {
                var parentRole = await _repository.GetRoleByNameAsync(roleName, ct);
                if (parentRole == null || parentRole.Id == roleId) // Can't inherit from self
                {
                    invalidRoles.Add(roleName);
                }
            }

            if (invalidRoles.Any())
            {
                throw new ArgumentException($"Invalid parent roles: {string.Join(", ", invalidRoles)}");
            }
        }

        var success = await _repository.UpdateRoleAsync(roleId, request, ct);

        // Update permissions and inheritance if provided
        if (success && request.Permissions?.Any() == true)
        {
            await _repository.SetRolePermissionsAsync(roleId, request.Permissions, ct);
        }

        if (success && request.InheritFromRoles?.Any() == true)
        {
            await _repository.SetRoleInheritanceAsync(roleId, request.InheritFromRoles, ct);
        }

        if (success)
        {
            _logger.LogInformation("Updated role: {RoleId}", roleId);
        }

        return success;
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        var existingRole = await _repository.GetRoleByIdAsync(roleId, ct);
        if (existingRole == null)
        {
            throw new KeyNotFoundException($"Role with ID '{roleId}' not found");
        }

        if (existingRole.IsSystemRole)
        {
            throw new InvalidOperationException("Cannot delete system roles");
        }

        // Check if role is in use
        // TODO: Add check for users using this role

        var success = await _repository.DeleteRoleAsync(roleId, ct);
        if (success)
        {
            _logger.LogInformation("Deleted role: {RoleName}", existingRole.Name);
        }

        return success;
    }

    public Task<RoleStatsDto> GetRoleStatsAsync(CancellationToken ct = default)
        => _repository.GetRoleStatsAsync(ct);

    #endregion

    #region Permission Management

    public Task<string[]> GetUserPermissionsAsync(string userId, CancellationToken ct = default)
        => _repository.GetUserPermissionsAsync(userId, ct);

    public Task<string[]> GetRolePermissionsAsync(string roleId, CancellationToken ct = default)
        => _repository.GetRolePermissionsAsync(roleId, ct);

    public async Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken ct = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, ct);
        return permissions.Contains(permission);
    }

    public async Task<bool> HasAnyPermissionAsync(string userId, string[] permissions, CancellationToken ct = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, ct);
        return permissions.Any(p => userPermissions.Contains(p));
    }

    public async Task<bool> HasAllPermissionsAsync(string userId, string[] permissions, CancellationToken ct = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, ct);
        return permissions.All(p => userPermissions.Contains(p));
    }

    #endregion

    #region Role Composition

    public Task<string[]> GetEffectivePermissionsAsync(string roleId, CancellationToken ct = default)
        => _repository.GetEffectivePermissionsAsync(roleId, ct);

    public async Task<bool> CanInheritFromRoleAsync(string roleId, string[] parentRoleIds, CancellationToken ct = default)
    {
        // Check for circular inheritance
        foreach (var parentRoleId in parentRoleIds)
        {
            if (parentRoleId == roleId)
            {
                return false; // Can't inherit from self
            }

            // Check if parent role would inherit from this role (circular dependency)
            var parentEffectivePermissions = await _repository.GetEffectivePermissionsAsync(parentRoleId, ct);
            // This is a simplified check - in a real implementation, you'd need to traverse the inheritance tree
        }

        return await _repository.ValidateRoleInheritanceAsync(roleId, parentRoleIds, ct);
    }

    public async Task<bool> ValidateRoleCompositionAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        // Validate permissions
        var invalidPermissions = request.Permissions.Where(p => !Permissions.IsValidPermission(p)).ToArray();
        if (invalidPermissions.Any())
        {
            return false;
        }

        // Validate role inheritance
        if (request.InheritFromRoles?.Any() == true)
        {
            foreach (var roleName in request.InheritFromRoles)
            {
                var parentRole = await _repository.GetRoleByNameAsync(roleName, ct);
                if (parentRole == null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region System Role Management

    public async Task InitializeSystemRolesAsync(CancellationToken ct = default)
    {
        var systemRoles = new[]
        {
            new RoleDto
            {
                Id = "system-owner",
                Name = "Owner",
                Description = "Full system access, business owner",
                Permissions = Permissions.AllPermissions,
                InheritFromRoles = Array.Empty<string>(),
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RoleDto
            {
                Id = "system-administrator",
                Name = "Administrator",
                Description = "System administration and user management",
                Permissions = new[]
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
                    Permissions.SESSION_VIEW, Permissions.SESSION_MANAGE, Permissions.SESSION_CLOSE
                },
                InheritFromRoles = Array.Empty<string>(),
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RoleDto
            {
                Id = "system-manager",
                Name = "Manager",
                Description = "Store operations, staff management, reports",
                Permissions = new[]
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
                    Permissions.SESSION_VIEW, Permissions.SESSION_MANAGE, Permissions.SESSION_CLOSE
                },
                InheritFromRoles = Array.Empty<string>(),
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RoleDto
            {
                Id = "system-server",
                Name = "Server",
                Description = "Order taking, table management, customer service",
                Permissions = new[]
                {
                    Permissions.ORDER_VIEW, Permissions.ORDER_CREATE, Permissions.ORDER_UPDATE, Permissions.ORDER_COMPLETE,
                    Permissions.TABLE_VIEW, Permissions.TABLE_MANAGE, Permissions.TABLE_ASSIGN,
                    Permissions.MENU_VIEW,
                    Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
                    Permissions.RESERVATION_VIEW, Permissions.RESERVATION_CREATE, Permissions.RESERVATION_UPDATE,
                    Permissions.SESSION_VIEW
                },
                InheritFromRoles = Array.Empty<string>(),
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RoleDto
            {
                Id = "system-cashier",
                Name = "Cashier",
                Description = "Payment processing, basic order operations",
                Permissions = new[]
                {
                    Permissions.ORDER_VIEW, Permissions.ORDER_UPDATE, Permissions.ORDER_COMPLETE,
                    Permissions.MENU_VIEW,
                    Permissions.PAYMENT_VIEW, Permissions.PAYMENT_PROCESS, Permissions.PAYMENT_REFUND,
                    Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
                    Permissions.SESSION_VIEW
                },
                InheritFromRoles = Array.Empty<string>(),
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new RoleDto
            {
                Id = "system-host",
                Name = "Host",
                Description = "Customer seating, reservation management",
                Permissions = new[]
                {
                    Permissions.TABLE_VIEW, Permissions.TABLE_MANAGE, Permissions.TABLE_ASSIGN,
                    Permissions.CUSTOMER_VIEW, Permissions.CUSTOMER_CREATE, Permissions.CUSTOMER_UPDATE,
                    Permissions.RESERVATION_VIEW, Permissions.RESERVATION_CREATE, Permissions.RESERVATION_UPDATE, Permissions.RESERVATION_CANCEL,
                    Permissions.SESSION_VIEW
                },
                InheritFromRoles = Array.Empty<string>(),
                IsSystemRole = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in systemRoles)
        {
            var existingRole = await _repository.GetRoleByNameAsync(role.Name, ct);
            if (existingRole == null)
            {
                var roleId = await _repository.CreateRoleAsync(role, ct);
                await _repository.SetRolePermissionsAsync(roleId, role.Permissions, ct);
                _logger.LogInformation("Created system role: {RoleName}", role.Name);
            }
        }
    }

    public async Task<RoleDto[]> GetSystemRolesAsync(CancellationToken ct = default)
    {
        var request = new RoleSearchRequest { IsSystemRole = true, PageSize = 100 };
        var result = await _repository.GetRolesAsync(request, ct);
        return result.Items.ToArray();
    }

    public async Task<RoleDto[]> GetCustomRolesAsync(CancellationToken ct = default)
    {
        var request = new RoleSearchRequest { IsSystemRole = false, PageSize = 100 };
        var result = await _repository.GetRolesAsync(request, ct);
        return result.Items.ToArray();
    }

    #endregion
}
