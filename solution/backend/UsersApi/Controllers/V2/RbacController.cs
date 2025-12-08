using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;
using UsersApi.Services;
using System.ComponentModel.DataAnnotations;

namespace UsersApi.Controllers.V2;

/// <summary>
/// Version 2 RBAC Controller with permission-based access control
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class RbacController : ControllerBase
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<RbacController> _logger;

    public RbacController(IRbacService rbacService, ILogger<RbacController> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    #region Role Management

    /// <summary>
    /// Get all roles with pagination and filtering (requires user:view permission)
    /// </summary>
    [HttpGet("roles")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetRoles([FromQuery] RoleSearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _rbacService.GetRolesAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles");
            return StatusCode(500, "Failed to retrieve roles");
        }
    }

    /// <summary>
    /// Get role by ID (requires user:view permission)
    /// </summary>
    [HttpGet("roles/{roleId}")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetRole(string roleId, CancellationToken ct = default)
    {
        try
        {
            var role = await _rbacService.GetRoleByIdAsync(roleId, ct);
            if (role == null)
            {
                return NotFound($"Role with ID '{roleId}' not found");
            }
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role {RoleId}", roleId);
            return StatusCode(500, "Failed to retrieve role");
        }
    }

    /// <summary>
    /// Get role by name (requires user:view permission)
    /// </summary>
    [HttpGet("roles/name/{roleName}")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetRoleByName(string roleName, CancellationToken ct = default)
    {
        try
        {
            var role = await _rbacService.GetRoleByNameAsync(roleName, ct);
            if (role == null)
            {
                return NotFound($"Role with name '{roleName}' not found");
            }
            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role by name {RoleName}", roleName);
            return StatusCode(500, "Failed to retrieve role");
        }
    }

    /// <summary>
    /// Create a new custom role (requires user:manage_roles permission)
    /// </summary>
    [HttpPost("roles")]
    [RequiresPermission(Permissions.USER_MANAGE_ROLES)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var role = await _rbacService.CreateRoleAsync(request, ct);
            return CreatedAtAction(nameof(GetRole), new { roleId = role.Id }, role);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create role: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create role");
            return StatusCode(500, "Failed to create role");
        }
    }

    /// <summary>
    /// Update a custom role (requires user:manage_roles permission)
    /// </summary>
    [HttpPut("roles/{roleId}")]
    [RequiresPermission(Permissions.USER_MANAGE_ROLES)]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _rbacService.UpdateRoleAsync(roleId, request, ct);
            if (!success)
            {
                return NotFound($"Role with ID '{roleId}' not found");
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update role: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role {RoleId}", roleId);
            return StatusCode(500, "Failed to update role");
        }
    }

    /// <summary>
    /// Delete a custom role (requires user:manage_roles permission)
    /// </summary>
    [HttpDelete("roles/{roleId}")]
    [RequiresPermission(Permissions.USER_MANAGE_ROLES)]
    public async Task<IActionResult> DeleteRole(string roleId, CancellationToken ct = default)
    {
        try
        {
            var success = await _rbacService.DeleteRoleAsync(roleId, ct);
            if (!success)
            {
                return NotFound($"Role with ID '{roleId}' not found");
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Role not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete role: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete role {RoleId}", roleId);
            return StatusCode(500, "Failed to delete role");
        }
    }

    /// <summary>
    /// Get role statistics (requires user:view permission)
    /// </summary>
    [HttpGet("roles/stats")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetRoleStats(CancellationToken ct = default)
    {
        try
        {
            var stats = await _rbacService.GetRoleStatsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role stats");
            return StatusCode(500, "Failed to retrieve role statistics");
        }
    }

    /// <summary>
    /// Get system roles (requires user:view permission)
    /// </summary>
    [HttpGet("roles/system")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetSystemRoles(CancellationToken ct = default)
    {
        try
        {
            var roles = await _rbacService.GetSystemRolesAsync(ct);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system roles");
            return StatusCode(500, "Failed to retrieve system roles");
        }
    }

    /// <summary>
    /// Get custom roles (requires user:view permission)
    /// </summary>
    [HttpGet("roles/custom")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetCustomRoles(CancellationToken ct = default)
    {
        try
        {
            var roles = await _rbacService.GetCustomRolesAsync(ct);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get custom roles");
            return StatusCode(500, "Failed to retrieve custom roles");
        }
    }

    #endregion

    #region Permission Management

    /// <summary>
    /// Get all available permissions (public endpoint - no permission required)
    /// </summary>
    [HttpGet("permissions")]
    public IActionResult GetPermissions()
    {
        try
        {
            var permissions = Permissions.AllPermissions.Select(p => new PermissionDto
            {
                Name = p,
                Description = Permissions.PermissionDescriptions.TryGetValue(p, out var desc) ? desc : p,
                Category = Permissions.PermissionCategories.FirstOrDefault(kvp => kvp.Value.Contains(p)).Key ?? "Other"
            }).ToArray();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions");
            return StatusCode(500, "Failed to retrieve permissions");
        }
    }

    /// <summary>
    /// Get permissions by category (public endpoint - no permission required)
    /// </summary>
    [HttpGet("permissions/category/{category}")]
    public IActionResult GetPermissionsByCategory(string category, CancellationToken ct = default)
    {
        try
        {
            var permissions = Permissions.GetPermissionsByCategory(category);
            if (!permissions.Any())
            {
                return NotFound($"Category '{category}' not found");
            }

            var permissionDtos = permissions.Select(p => new PermissionDto
            {
                Name = p,
                Description = Permissions.PermissionDescriptions.TryGetValue(p, out var desc) ? desc : p,
                Category = category
            }).ToArray();

            return Ok(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for category {Category}", category);
            return StatusCode(500, "Failed to retrieve permissions");
        }
    }

    /// <summary>
    /// Get all permission categories (public endpoint - no permission required)
    /// </summary>
    [HttpGet("permissions/categories")]
    public IActionResult GetPermissionCategories()
    {
        try
        {
            var categories = Permissions.GetAllCategories();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permission categories");
            return StatusCode(500, "Failed to retrieve permission categories");
        }
    }

    /// <summary>
    /// Get user permissions (public endpoint for internal service-to-service calls)
    /// Note: This endpoint is used by other APIs to check permissions, so it cannot require permissions itself
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
    // No [RequiresPermission] - this endpoint must be accessible for service-to-service calls
    public async Task<IActionResult> GetUserPermissions(string userId, CancellationToken ct = default)
    {
        try
        {
            var permissions = await _rbacService.GetUserPermissionsAsync(userId, ct);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user permissions for {UserId}", userId);
            return StatusCode(500, "Failed to retrieve user permissions");
        }
    }

    /// <summary>
    /// Get role permissions (requires user:view permission)
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetRolePermissions(string roleId, CancellationToken ct = default)
    {
        try
        {
            var permissions = await _rbacService.GetRolePermissionsAsync(roleId, ct);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role permissions for {RoleId}", roleId);
            return StatusCode(500, "Failed to retrieve role permissions");
        }
    }

    /// <summary>
    /// Get effective permissions for a role including inherited (requires user:view permission)
    /// </summary>
    [HttpGet("roles/{roleId}/effective-permissions")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> GetEffectivePermissions(string roleId, CancellationToken ct = default)
    {
        try
        {
            var permissions = await _rbacService.GetEffectivePermissionsAsync(roleId, ct);
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get effective permissions for {RoleId}", roleId);
            return StatusCode(500, "Failed to retrieve effective permissions");
        }
    }

    /// <summary>
    /// Check if user has specific permission (requires user:view permission)
    /// </summary>
    [HttpPost("users/{userId}/permissions/check")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> CheckUserPermission(string userId, [FromBody] CheckPermissionRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hasPermission = await _rbacService.HasPermissionAsync(userId, request.Permission, ct);
            return Ok(new { HasPermission = hasPermission });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permission for user {UserId}", userId);
            return StatusCode(500, "Failed to check permission");
        }
    }

    /// <summary>
    /// Check if user has any of the specified permissions (requires user:view permission)
    /// </summary>
    [HttpPost("users/{userId}/permissions/check-any")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> CheckUserAnyPermission(string userId, [FromBody] CheckPermissionsRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hasPermission = await _rbacService.HasAnyPermissionAsync(userId, request.Permissions, ct);
            return Ok(new { HasPermission = hasPermission });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permissions for user {UserId}", userId);
            return StatusCode(500, "Failed to check permissions");
        }
    }

    /// <summary>
    /// Check if user has all of the specified permissions (requires user:view permission)
    /// </summary>
    [HttpPost("users/{userId}/permissions/check-all")]
    [RequiresPermission(Permissions.USER_VIEW)]
    public async Task<IActionResult> CheckUserAllPermissions(string userId, [FromBody] CheckPermissionsRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var hasPermission = await _rbacService.HasAllPermissionsAsync(userId, request.Permissions, ct);
            return Ok(new { HasPermission = hasPermission });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permissions for user {UserId}", userId);
            return StatusCode(500, "Failed to check permissions");
        }
    }

    #endregion

    #region Role Composition

    /// <summary>
    /// Validate role composition (requires user:manage_roles permission)
    /// </summary>
    [HttpPost("roles/validate-composition")]
    [RequiresPermission(Permissions.USER_MANAGE_ROLES)]
    public async Task<IActionResult> ValidateRoleComposition([FromBody] CreateRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _rbacService.ValidateRoleCompositionAsync(request, ct);
            return Ok(new { IsValid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate role composition");
            return StatusCode(500, "Failed to validate role composition");
        }
    }

    /// <summary>
    /// Check if role can inherit from specified roles (requires user:manage_roles permission)
    /// </summary>
    [HttpPost("roles/{roleId}/can-inherit")]
    [RequiresPermission(Permissions.USER_MANAGE_ROLES)]
    public async Task<IActionResult> CanInheritFromRoles(string roleId, [FromBody] string[] parentRoleIds, CancellationToken ct = default)
    {
        try
        {
            var canInherit = await _rbacService.CanInheritFromRoleAsync(roleId, parentRoleIds, ct);
            return Ok(new { CanInherit = canInherit });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check role inheritance for {RoleId}", roleId);
            return StatusCode(500, "Failed to check role inheritance");
        }
    }

    #endregion
}

public record CheckPermissionRequest
{
    [Required]
    public string Permission { get; init; } = string.Empty;
}

public record CheckPermissionsRequest
{
    [Required]
    public string[] Permissions { get; init; } = Array.Empty<string>();
}

