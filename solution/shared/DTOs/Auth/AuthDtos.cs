namespace MagiDesk.Shared.DTOs.Auth;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "employee";
    public string[] Permissions { get; set; } = Array.Empty<string>(); // Added for RBAC v2
    public DateTime LastLoginAt { get; set; }
}

public class SessionDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "employee";
    public string[] Permissions { get; set; } = Array.Empty<string>(); // Added for RBAC v2
    public DateTime LastLoginAt { get; set; }
}
