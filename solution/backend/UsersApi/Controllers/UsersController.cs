using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Shared.DTOs.Users;
using UsersApi.Services;
using System.ComponentModel.DataAnnotations;

namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")] // Default route (v1)
[Route("api/v1/[controller]")] // Explicit v1 route
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUsersService usersService, ILogger<UsersController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping(CancellationToken ct = default)
    {
        try
        {
            var result = await _usersService.PingAsync(ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping failed");
            return StatusCode(500, false);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _usersService.GetUsersAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return StatusCode(500, "Failed to retrieve users");
        }
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _usersService.GetUserByIdAsync(userId, ct);
            if (user == null)
            {
                return NotFound($"User with ID '{userId}' not found");
            }
            
            // Don't return password hash to clients
            var safeUser = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                PasswordHash = string.Empty,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsActive = user.IsActive
            };
            return Ok(safeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", userId);
            return StatusCode(500, "Failed to retrieve user");
        }
    }

    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetUserByUsername(string username, CancellationToken ct = default)
    {
        try
        {
            var user = await _usersService.GetUserByUsernameAsync(username, ct);
            if (user == null)
            {
                return NotFound($"User with username '{username}' not found");
            }
            
            // Don't return password hash to clients
            var safeUser = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                PasswordHash = string.Empty,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsActive = user.IsActive
            };
            return Ok(safeUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by username {Username}", username);
            return StatusCode(500, "Failed to retrieve user");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] MagiDesk.Shared.DTOs.Users.CreateUserRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _usersService.CreateUserAsync(request, ct);
            
            // Don't return password hash to clients
            var safeUser = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                PasswordHash = string.Empty,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsActive = user.IsActive
            };
            return CreatedAtAction(nameof(GetUser), new { userId = user.UserId }, safeUser);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user");
            return StatusCode(500, "Failed to create user");
        }
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] MagiDesk.Shared.DTOs.Users.UpdateUserRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _usersService.UpdateUserAsync(userId, request, ct);
            if (!success)
            {
                return NotFound($"User with ID '{userId}' not found");
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update user: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", userId);
            return StatusCode(500, "Failed to update user");
        }
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId, CancellationToken ct = default)
    {
        try
        {
            var success = await _usersService.DeleteUserAsync(userId, ct);
            if (!success)
            {
                return NotFound($"User with ID '{userId}' not found");
            }

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found: {Message}", ex.Message);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return StatusCode(500, "Failed to delete user");
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats(CancellationToken ct = default)
    {
        try
        {
            var stats = await _usersService.GetUserStatsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user stats");
            return StatusCode(500, "Failed to retrieve user statistics");
        }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCredentials([FromBody] ValidateCredentialsRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _usersService.ValidateUserCredentialsAsync(request.Username, request.Password, ct);
            return Ok(new { IsValid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate credentials");
            return StatusCode(500, "Failed to validate credentials");
        }
    }
}

public record ValidateCredentialsRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;
    
    [Required]
    public string Password { get; init; } = string.Empty;
}
