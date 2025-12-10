using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Users;
using UsersApi.Services;
using System.ComponentModel.DataAnnotations;

namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Get all roles with pagination and filtering
    /// </summary>
    [HttpGet("roles")]
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
    /// Get role by ID
    /// </summary>
    [HttpGet("roles/{roleId}")]
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
    /// Get role by name
    /// </summary>
    [HttpGet("roles/name/{roleName}")]
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
    /// Create a new custom role
    /// </summary>
    [HttpPost("roles")]
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
    /// Update a custom role
    /// </summary>
    [HttpPut("roles/{roleId}")]
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
    /// Delete a custom role
    /// </summary>
    [HttpDelete("roles/{roleId}")]
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
    /// Get role statistics
    /// </summary>
    [HttpGet("roles/stats")]
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
    /// Get system roles
    /// </summary>
    [HttpGet("roles/system")]
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
    /// Get custom roles
    /// </summary>
    [HttpGet("roles/custom")]
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
    /// Get all available permissions
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
    /// Get permissions by category
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
    /// Get all permission categories
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
    /// Get user permissions
    /// </summary>
    [HttpGet("users/{userId}/permissions")]
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
    /// Get role permissions
    /// </summary>
    [HttpGet("roles/{roleId}/permissions")]
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
    /// Get effective permissions for a role (including inherited)
    /// </summary>
    [HttpGet("roles/{roleId}/effective-permissions")]
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
    /// Check if user has specific permission
    /// </summary>
    [HttpPost("users/{userId}/permissions/check")]
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
    /// Check if user has any of the specified permissions
    /// </summary>
    [HttpPost("users/{userId}/permissions/check-any")]
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
    /// Check if user has all of the specified permissions
    /// </summary>
    [HttpPost("users/{userId}/permissions/check-all")]
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
    /// Validate role composition
    /// </summary>
    [HttpPost("roles/validate-composition")]
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
    /// Check if role can inherit from specified roles
    /// </summary>
    [HttpPost("roles/{roleId}/can-inherit")]
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
