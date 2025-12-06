using Microsoft.AspNetCore.Authorization;

namespace UsersApi.Attributes;

/// <summary>
/// Attribute to mark endpoints that require specific permissions
/// Used with ASP.NET Core authorization policies
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequiresPermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"Permission:{Permission}";
    }
}

