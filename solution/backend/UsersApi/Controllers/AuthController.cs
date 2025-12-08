using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Auth;
using UsersApi.Services;
using System.ComponentModel.DataAnnotations;

namespace UsersApi.Controllers;

[ApiController]
[Route("api/[controller]")] // Default route (v1)
[Route("api/v1/[controller]")] // Explicit v1 route
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsersService usersService, ILogger<AuthController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with username and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate credentials
            var isValid = await _usersService.ValidateUserCredentialsAsync(request.Username, request.Password, ct);
            if (!isValid)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized("Invalid credentials");
            }

            // Get user details
            var user = await _usersService.GetUserByUsernameAsync(request.Username, ct);
            if (user == null)
            {
                _logger.LogError("User not found after successful credential validation: {Username}", request.Username);
                return Unauthorized("User not found");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted login: {Username}", request.Username);
                return Unauthorized("Account is inactive");
            }

            // Create login response
            var response = new LoginResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role, // Role is not nullable in UserDto
                LastLoginAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successful login for user: {Username} with role: {Role}", request.Username, user.Role);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for username: {Username}", request.Username);
            return StatusCode(500, "Login failed due to server error");
        }
    }

    /// <summary>
    /// Validate user credentials without creating a session
    /// </summary>
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
            _logger.LogError(ex, "Credential validation failed for username: {Username}", request.Username);
            return StatusCode(500, "Validation failed due to server error");
        }
    }

    /// <summary>
    /// Get current user information (placeholder for future session management)
    /// </summary>
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        try
        {
            // For now, return a placeholder response
            // In the future, this could use JWT tokens or session management
            return Ok(new { Message = "Session management not implemented yet" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return StatusCode(500, "Failed to get current user");
        }
    }
}

