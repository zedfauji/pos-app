namespace MagiDesk.Shared.DTOs.Auth;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "employee";
}

public class UpdateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = "employee";
    public bool IsActive { get; set; } = true;
}

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
    public DateTime LastLoginAt { get; set; }
}

public class SessionDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = "employee";
    public DateTime LastLoginAt { get; set; }
}
