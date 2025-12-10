using System.ComponentModel.DataAnnotations;

namespace MagiDesk.Shared.DTOs.Users;

public class UserDto
{
    public string? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Server;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public record CreateUserRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    [StringLength(20, MinimumLength = 6)]
    [RegularExpression(@"^\d+$", ErrorMessage = "Password must contain only digits")]
    public string Password { get; init; } = string.Empty;
    
    [Required]
    public string Role { get; init; } = UserRoles.Server;
}

public record UpdateUserRequest
{
    [StringLength(50, MinimumLength = 3)]
    public string? Username { get; init; }
    
    [StringLength(20, MinimumLength = 6)]
    [RegularExpression(@"^\d+$", ErrorMessage = "Password must contain only digits")]
    public string? Password { get; init; }
    
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
}

public record UserSearchRequest
{
    public string? SearchTerm { get; init; }
    public string? Role { get; init; }
    public bool? IsActive { get; init; }
    public string SortBy { get; init; } = "Username";
    public bool SortDescending { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record UserStatsDto
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int AdminUsers { get; init; }
    public int EmployeeUsers { get; init; }
    public DateTime LastUserCreated { get; init; }
}
