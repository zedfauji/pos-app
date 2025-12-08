using Microsoft.AspNetCore.Mvc;
using MagiDesk.Shared.DTOs.Auth;
using UsersApi.Services;
using System.ComponentModel.DataAnnotations;

namespace UsersApi.Controllers.V2;

/// <summary>
/// Version 2 Auth Controller with RBAC support
/// Returns permissions in login response
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly IRbacService _rbacService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsersService usersService,
        IRbacService rbacService,
        ILogger<AuthController> logger)
    {
        _usersService = usersService;
        _rbacService = rbacService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with username and password (v2 - includes permissions)
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

            // Get user permissions (v2 enhancement)
            string[] permissions = Array.Empty<string>();
            try
            {
                permissions = await _rbacService.GetUserPermissionsAsync(user.UserId, ct);
                _logger.LogDebug("Retrieved {Count} permissions for user {UserId}", permissions.Length, user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve permissions for user {UserId}, continuing with empty permissions", user.UserId);
                // Continue without permissions - don't fail login
            }

            // Create login response with permissions
            var response = new LoginResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                Permissions = permissions, // v2: Include permissions
                LastLoginAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Successful login for user: {Username} with role: {Role} and {PermissionCount} permissions",
                request.Username,
                user.Role,
                permissions.Length);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for username: {Username}", request.Username);
            return StatusCode(500, "Login failed due to server error");
        }
    }

    /// <summary>
    /// Validate user credentials without creating a session (v2)
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
    /// Get current user information with permissions (v2)
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct = default)
    {
        try
        {
            // Extract user ID from request (set by middleware)
            if (!HttpContext.Items.TryGetValue("UserId", out var userIdObj) || userIdObj is not string userId)
            {
                return Unauthorized("User ID not found in request");
            }

            var user = await _usersService.GetUserByIdAsync(userId, ct);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Get permissions
            var permissions = await _rbacService.GetUserPermissionsAsync(userId, ct);

            return Ok(new
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                Permissions = permissions,
                IsActive = user.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return StatusCode(500, "Failed to get current user");
        }
    }
}

