using Microsoft.AspNetCore.Mvc;
using MagiDesk.Backend.Services;
using MagiDesk.Shared.DTOs.Auth;

namespace MagiDesk.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _users;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUsersService users, ILogger<UsersController> logger)
    {
        _users = users; _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers(CancellationToken ct)
    {
        var list = await _users.GetUsersAsync(ct);
        // for safety, do not leak password hashes in list results
        foreach (var u in list) u.PasswordHash = string.Empty;
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id, CancellationToken ct)
    {
        var u = await _users.GetUserAsync(id, ct);
        if (u == null) return NotFound();
        u.PasswordHash = string.Empty;
        return Ok(u);
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username and password are required");
        if (req.Password.Length < 6) return BadRequest("Password must be at least 6 characters");
        try
        {
            var created = await _users.CreateUserAsync(req, ct);
            created.PasswordHash = string.Empty;
            return Created($"api/users/{created.UserId}", created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username is required");
        if (req.Password != null && req.Password.Length < 6) return BadRequest("Password must be at least 6 characters");
        var ok = await _users.UpdateUserAsync(id, req, ct);
        if (!ok) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteUser(string id, CancellationToken ct)
    {
        var ok = await _users.DeleteUserAsync(id, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUsersService _users;
    public AuthController(IUsersService users) { _users = users; }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username and password are required");
        var res = await _users.LoginAsync(req, ct);
        if (res == null) return Unauthorized();
        return Ok(res);
    }
}
