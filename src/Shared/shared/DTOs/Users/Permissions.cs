using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs.Users;

/// <summary>
/// Comprehensive permission system for POS operations
/// </summary>
public static class Permissions
{
    // User Management Permissions
    public const string USER_VIEW = "user:view";
    public const string USER_CREATE = "user:create";
    public const string USER_UPDATE = "user:update";
    public const string USER_DELETE = "user:delete";
    public const string USER_MANAGE_ROLES = "user:manage_roles";

    // Order Management Permissions
    public const string ORDER_VIEW = "order:view";
    public const string ORDER_CREATE = "order:create";
    public const string ORDER_UPDATE = "order:update";
    public const string ORDER_DELETE = "order:delete";
    public const string ORDER_CANCEL = "order:cancel";
    public const string ORDER_COMPLETE = "order:complete";

    // Table Management Permissions
    public const string TABLE_VIEW = "table:view";
    public const string TABLE_MANAGE = "table:manage";
    public const string TABLE_ASSIGN = "table:assign";
    public const string TABLE_CLEAR = "table:clear";

    // Menu Management Permissions
    public const string MENU_VIEW = "menu:view";
    public const string MENU_CREATE = "menu:create";
    public const string MENU_UPDATE = "menu:update";
    public const string MENU_DELETE = "menu:delete";
    public const string MENU_PRICE_CHANGE = "menu:price_change";

    // Payment Management Permissions
    public const string PAYMENT_VIEW = "payment:view";
    public const string PAYMENT_PROCESS = "payment:process";
    public const string PAYMENT_REFUND = "payment:refund";
    public const string PAYMENT_VOID = "payment:void";

    // Inventory Management Permissions
    public const string INVENTORY_VIEW = "inventory:view";
    public const string INVENTORY_UPDATE = "inventory:update";
    public const string INVENTORY_ADJUST = "inventory:adjust";
    public const string INVENTORY_RESTOCK = "inventory:restock";

    // Reports and Analytics Permissions
    public const string REPORT_VIEW = "report:view";
    public const string REPORT_SALES = "report:sales";
    public const string REPORT_INVENTORY = "report:inventory";
    public const string REPORT_USER_ACTIVITY = "report:user_activity";
    public const string REPORT_EXPORT = "report:export";

    // System Settings Permissions
    public const string SETTINGS_VIEW = "settings:view";
    public const string SETTINGS_UPDATE = "settings:update";
    public const string SETTINGS_SYSTEM = "settings:system";
    public const string SETTINGS_RECEIPT = "settings:receipt";

    // Customer Management Permissions
    public const string CUSTOMER_VIEW = "customer:view";
    public const string CUSTOMER_CREATE = "customer:create";
    public const string CUSTOMER_UPDATE = "customer:update";
    public const string CUSTOMER_DELETE = "customer:delete";

    // Reservation Management Permissions
    public const string RESERVATION_VIEW = "reservation:view";
    public const string RESERVATION_CREATE = "reservation:create";
    public const string RESERVATION_UPDATE = "reservation:update";
    public const string RESERVATION_CANCEL = "reservation:cancel";

    // Vendor Management Permissions
    public const string VENDOR_VIEW = "vendor:view";
    public const string VENDOR_CREATE = "vendor:create";
    public const string VENDOR_UPDATE = "vendor:update";
    public const string VENDOR_DELETE = "vendor:delete";
    public const string VENDOR_ORDER = "vendor:order";

    // Session Management Permissions
    public const string SESSION_VIEW = "session:view";
    public const string SESSION_MANAGE = "session:manage";
    public const string SESSION_CLOSE = "session:close";

    /// <summary>
    /// All available permissions
    /// </summary>
    public static readonly string[] AllPermissions = 
    {
        // User Management
        USER_VIEW, USER_CREATE, USER_UPDATE, USER_DELETE, USER_MANAGE_ROLES,
        
        // Order Management
        ORDER_VIEW, ORDER_CREATE, ORDER_UPDATE, ORDER_DELETE, ORDER_CANCEL, ORDER_COMPLETE,
        
        // Table Management
        TABLE_VIEW, TABLE_MANAGE, TABLE_ASSIGN, TABLE_CLEAR,
        
        // Menu Management
        MENU_VIEW, MENU_CREATE, MENU_UPDATE, MENU_DELETE, MENU_PRICE_CHANGE,
        
        // Payment Management
        PAYMENT_VIEW, PAYMENT_PROCESS, PAYMENT_REFUND, PAYMENT_VOID,
        
        // Inventory Management
        INVENTORY_VIEW, INVENTORY_UPDATE, INVENTORY_ADJUST, INVENTORY_RESTOCK,
        
        // Reports
        REPORT_VIEW, REPORT_SALES, REPORT_INVENTORY, REPORT_USER_ACTIVITY, REPORT_EXPORT,
        
        // Settings
        SETTINGS_VIEW, SETTINGS_UPDATE, SETTINGS_SYSTEM, SETTINGS_RECEIPT,
        
        // Customer Management
        CUSTOMER_VIEW, CUSTOMER_CREATE, CUSTOMER_UPDATE, CUSTOMER_DELETE,
        
        // Reservation Management
        RESERVATION_VIEW, RESERVATION_CREATE, RESERVATION_UPDATE, RESERVATION_CANCEL,
        
        // Vendor Management
        VENDOR_VIEW, VENDOR_CREATE, VENDOR_UPDATE, VENDOR_DELETE, VENDOR_ORDER,
        
        // Session Management
        SESSION_VIEW, SESSION_MANAGE, SESSION_CLOSE
    };

    /// <summary>
    /// Permission descriptions for UI display
    /// </summary>
    public static readonly Dictionary<string, string> PermissionDescriptions = new()
    {
        // User Management
        { USER_VIEW, "View user information" },
        { USER_CREATE, "Create new users" },
        { USER_UPDATE, "Update user information" },
        { USER_DELETE, "Delete users" },
        { USER_MANAGE_ROLES, "Assign and manage user roles" },
        
        // Order Management
        { ORDER_VIEW, "View orders" },
        { ORDER_CREATE, "Create new orders" },
        { ORDER_UPDATE, "Modify existing orders" },
        { ORDER_DELETE, "Delete orders" },
        { ORDER_CANCEL, "Cancel orders" },
        { ORDER_COMPLETE, "Mark orders as complete" },
        
        // Table Management
        { TABLE_VIEW, "View table status" },
        { TABLE_MANAGE, "Manage table assignments" },
        { TABLE_ASSIGN, "Assign tables to servers" },
        { TABLE_CLEAR, "Clear tables" },
        
        // Menu Management
        { MENU_VIEW, "View menu items" },
        { MENU_CREATE, "Add new menu items" },
        { MENU_UPDATE, "Modify menu items" },
        { MENU_DELETE, "Remove menu items" },
        { MENU_PRICE_CHANGE, "Change menu item prices" },
        
        // Payment Management
        { PAYMENT_VIEW, "View payment information" },
        { PAYMENT_PROCESS, "Process payments" },
        { PAYMENT_REFUND, "Process refunds" },
        { PAYMENT_VOID, "Void transactions" },
        
        // Inventory Management
        { INVENTORY_VIEW, "View inventory levels" },
        { INVENTORY_UPDATE, "Update inventory quantities" },
        { INVENTORY_ADJUST, "Adjust inventory for discrepancies" },
        { INVENTORY_RESTOCK, "Process restock orders" },
        
        // Reports
        { REPORT_VIEW, "View reports" },
        { REPORT_SALES, "View sales reports" },
        { REPORT_INVENTORY, "View inventory reports" },
        { REPORT_USER_ACTIVITY, "View user activity reports" },
        { REPORT_EXPORT, "Export reports" },
        
        // Settings
        { SETTINGS_VIEW, "View system settings" },
        { SETTINGS_UPDATE, "Update system settings" },
        { SETTINGS_SYSTEM, "Modify system configuration" },
        { SETTINGS_RECEIPT, "Configure receipt settings" },
        
        // Customer Management
        { CUSTOMER_VIEW, "View customer information" },
        { CUSTOMER_CREATE, "Add new customers" },
        { CUSTOMER_UPDATE, "Update customer information" },
        { CUSTOMER_DELETE, "Remove customers" },
        
        // Reservation Management
        { RESERVATION_VIEW, "View reservations" },
        { RESERVATION_CREATE, "Create reservations" },
        { RESERVATION_UPDATE, "Modify reservations" },
        { RESERVATION_CANCEL, "Cancel reservations" },
        
        // Vendor Management
        { VENDOR_VIEW, "View vendor information" },
        { VENDOR_CREATE, "Add new vendors" },
        { VENDOR_UPDATE, "Update vendor information" },
        { VENDOR_DELETE, "Remove vendors" },
        { VENDOR_ORDER, "Place vendor orders" },
        
        // Session Management
        { SESSION_VIEW, "View active sessions" },
        { SESSION_MANAGE, "Manage sessions" },
        { SESSION_CLOSE, "Close sessions" }
    };

    /// <summary>
    /// Permission categories for grouping
    /// </summary>
    public static readonly Dictionary<string, string[]> PermissionCategories = new()
    {
        { "User Management", new[] { USER_VIEW, USER_CREATE, USER_UPDATE, USER_DELETE, USER_MANAGE_ROLES } },
        { "Order Management", new[] { ORDER_VIEW, ORDER_CREATE, ORDER_UPDATE, ORDER_DELETE, ORDER_CANCEL, ORDER_COMPLETE } },
        { "Table Management", new[] { TABLE_VIEW, TABLE_MANAGE, TABLE_ASSIGN, TABLE_CLEAR } },
        { "Menu Management", new[] { MENU_VIEW, MENU_CREATE, MENU_UPDATE, MENU_DELETE, MENU_PRICE_CHANGE } },
        { "Payment Management", new[] { PAYMENT_VIEW, PAYMENT_PROCESS, PAYMENT_REFUND, PAYMENT_VOID } },
        { "Inventory Management", new[] { INVENTORY_VIEW, INVENTORY_UPDATE, INVENTORY_ADJUST, INVENTORY_RESTOCK } },
        { "Reports & Analytics", new[] { REPORT_VIEW, REPORT_SALES, REPORT_INVENTORY, REPORT_USER_ACTIVITY, REPORT_EXPORT } },
        { "System Settings", new[] { SETTINGS_VIEW, SETTINGS_UPDATE, SETTINGS_SYSTEM, SETTINGS_RECEIPT } },
        { "Customer Management", new[] { CUSTOMER_VIEW, CUSTOMER_CREATE, CUSTOMER_UPDATE, CUSTOMER_DELETE } },
        { "Reservation Management", new[] { RESERVATION_VIEW, RESERVATION_CREATE, RESERVATION_UPDATE, RESERVATION_CANCEL } },
        { "Vendor Management", new[] { VENDOR_VIEW, VENDOR_CREATE, VENDOR_UPDATE, VENDOR_DELETE, VENDOR_ORDER } },
        { "Session Management", new[] { SESSION_VIEW, SESSION_MANAGE, SESSION_CLOSE } }
    };

    /// <summary>
    /// Check if a permission is valid
    /// </summary>
    public static bool IsValidPermission(string permission)
    {
        return AllPermissions.Contains(permission);
    }

    /// <summary>
    /// Get permissions by category
    /// </summary>
    public static string[] GetPermissionsByCategory(string category)
    {
        return PermissionCategories.TryGetValue(category, out var permissions) ? permissions : Array.Empty<string>();
    }

    /// <summary>
    /// Get all permission categories
    /// </summary>
    public static string[] GetAllCategories()
    {
        return PermissionCategories.Keys.ToArray();
    }
}

/// <summary>
/// Permission DTO for API responses
/// </summary>
public record PermissionDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a custom role
/// </summary>
public record CreateRoleRequest
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; init; }
    
    [Required]
    public string[] Permissions { get; init; } = Array.Empty<string>();
    
    public string[]? InheritFromRoles { get; init; }
}

/// <summary>
/// Request to update a role
/// </summary>
public record UpdateRoleRequest
{
    [StringLength(200)]
    public string? Description { get; init; }
    
    public string[]? Permissions { get; init; }
    
    public string[]? InheritFromRoles { get; init; }
    
    public bool? IsActive { get; init; }
}

/// <summary>
/// Role DTO with permissions
/// </summary>
public record RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public string[] InheritFromRoles { get; set; } = Array.Empty<string>();
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Role search request
/// </summary>
public record RoleSearchRequest
{
    public string? SearchTerm { get; init; }
    public bool? IsSystemRole { get; init; }
    public bool? IsActive { get; init; }
    public string SortBy { get; init; } = "Name";
    public bool SortDescending { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Role statistics DTO
/// </summary>
public record RoleStatsDto
{
    public int TotalRoles { get; init; }
    public int SystemRoles { get; init; }
    public int CustomRoles { get; init; }
    public int ActiveRoles { get; init; }
    public int InactiveRoles { get; init; }
    public DateTime LastRoleCreated { get; init; }
}
