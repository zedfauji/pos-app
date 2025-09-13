using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Users;

namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    /// <summary>
    /// Get all available user roles for POS system
    /// </summary>
    [HttpGet]
    public ActionResult<object> GetRoles()
    {
        var roles = UserRoles.AllRoles.Select(role => new
        {
            Name = role,
            Description = UserRoles.RoleDescriptions[role],
            Level = UserRoles.GetRoleLevel(role),
            Permissions = UserRoles.RolePermissions[role]
        }).ToList();

        return Ok(roles);
    }

    /// <summary>
    /// Get role hierarchy information
    /// </summary>
    [HttpGet("hierarchy")]
    public ActionResult<object> GetRoleHierarchy()
    {
        return Ok(new
        {
            Roles = UserRoles.AllRoles,
            Descriptions = UserRoles.RoleDescriptions,
            Permissions = UserRoles.RolePermissions
        });
    }

    /// <summary>
    /// Validate if a role is valid
    /// </summary>
    [HttpGet("validate/{role}")]
    public ActionResult<object> ValidateRole(string role)
    {
        var isValid = UserRoles.IsValidRole(role);
        return Ok(new
        {
            Role = role,
            IsValid = isValid,
            Level = isValid ? UserRoles.GetRoleLevel(role) : -1,
            Description = isValid ? UserRoles.RoleDescriptions[role] : "Invalid role"
        });
    }
}
