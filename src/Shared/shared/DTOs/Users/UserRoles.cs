namespace MagiDesk.Shared.DTOs.Users;

/// <summary>
/// Predefined roles for POS system with hierarchical permissions
/// </summary>
public static class UserRoles
{
    public const string Owner = "Owner";
    public const string Administrator = "Administrator"; 
    public const string Manager = "Manager";
    public const string Server = "Server";
    public const string Cashier = "Cashier";
    public const string Host = "Host";

    /// <summary>
    /// All available roles in order of hierarchy (highest to lowest permissions)
    /// </summary>
    public static readonly string[] AllRoles = 
    {
        Owner,
        Administrator,
        Manager,
        Server,
        Cashier,
        Host
    };

    /// <summary>
    /// Role descriptions for UI display
    /// </summary>
    public static readonly Dictionary<string, string> RoleDescriptions = new()
    {
        { Owner, "Full system access, business owner" },
        { Administrator, "System administration and user management" },
        { Manager, "Store operations, staff management, reports" },
        { Server, "Order taking, table management, customer service" },
        { Cashier, "Payment processing, basic order operations" },
        { Host, "Customer seating, reservation management" }
    };

    /// <summary>
    /// Role permissions mapping
    /// </summary>
    public static readonly Dictionary<string, string[]> RolePermissions = new()
    {
        { Owner, new[] { "ALL_PERMISSIONS" } },
        { Administrator, new[] { "USER_MANAGEMENT", "SYSTEM_SETTINGS", "REPORTS", "INVENTORY", "MENU", "ORDERS", "PAYMENTS" } },
        { Manager, new[] { "REPORTS", "INVENTORY", "MENU", "ORDERS", "STAFF_MANAGEMENT", "PAYMENTS" } },
        { Server, new[] { "ORDERS", "TABLES", "MENU_VIEW", "CUSTOMER_SERVICE" } },
        { Cashier, new[] { "PAYMENTS", "ORDERS", "MENU_VIEW" } },
        { Host, new[] { "TABLES", "RESERVATIONS", "CUSTOMER_SERVICE" } }
    };

    /// <summary>
    /// Check if a role is valid
    /// </summary>
    public static bool IsValidRole(string role)
    {
        return AllRoles.Contains(role);
    }

    /// <summary>
    /// Get role hierarchy level (0 = highest, 5 = lowest)
    /// </summary>
    public static int GetRoleLevel(string role)
    {
        return Array.IndexOf(AllRoles, role);
    }

    /// <summary>
    /// Check if role1 has higher or equal permissions than role2
    /// </summary>
    public static bool HasHigherOrEqualPermissions(string role1, string role2)
    {
        return GetRoleLevel(role1) <= GetRoleLevel(role2);
    }
}
