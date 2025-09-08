namespace MagiDesk.Shared.DTOs.Auth;

public class UserDto
{
    public string? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // never send to clients in real apps; here used for admin CRUD only
    public string Role { get; set; } = "employee";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
