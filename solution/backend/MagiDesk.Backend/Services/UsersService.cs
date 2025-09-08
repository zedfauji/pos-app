using Google.Cloud.Firestore;
using Microsoft.Extensions.Options;
using MagiDesk.Shared.DTOs.Auth;
using Grpc.Auth;

namespace MagiDesk.Backend.Services;

public interface IUsersService
{
    Task<List<UserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDto?> GetUserAsync(string userId, CancellationToken ct = default);
    Task<UserDto?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest req, CancellationToken ct = default);
    Task<bool> UpdateUserAsync(string userId, UpdateUserRequest req, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default);
    Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default);
    string HashPassword(string plain);
}

public class UsersService : IUsersService
{
    private readonly FirestoreDb _db;
    private const string UsersCollection = "users";

    public UsersService(IOptions<FirestoreOptions> options, IWebHostEnvironment env)
    {
        var opt = options.Value;
        string? credsEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        string credentialsPath = credsEnv ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, opt.CredentialsPath));
        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException($"Firestore credentials not found at {credentialsPath}");
        var credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialsPath);
        var channelCredentials = credential.ToChannelCredentials();
        _db = new FirestoreDbBuilder { ProjectId = opt.ProjectId, ChannelCredentials = channelCredentials }.Build();
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var snap = await _db.Collection(UsersCollection).GetSnapshotAsync(ct);
        return snap.Documents.Select(MapUser).Where(u => u != null)!.ToList()!;
    }

    public async Task<UserDto?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        var doc = _db.Collection(UsersCollection).Document(userId);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return null;
        return MapUser(snap);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var q = _db.Collection(UsersCollection).WhereEqualTo("username", username).Limit(1);
        var snap = await q.GetSnapshotAsync(ct);
        var d = snap.Documents.FirstOrDefault();
        return d != null ? MapUser(d) : null;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest req, CancellationToken ct = default)
    {
        // uniqueness check
        var existing = await GetByUsernameAsync(req.Username, ct);
        if (existing != null) throw new InvalidOperationException("Username already exists");
        var now = Timestamp.FromDateTime(DateTime.UtcNow);
        var data = new Dictionary<string, object?>
        {
            ["username"] = req.Username,
            ["passwordHash"] = HashPassword(req.Password),
            ["role"] = string.IsNullOrWhiteSpace(req.Role) ? "employee" : req.Role,
            ["createdAt"] = now,
            ["updatedAt"] = now,
            ["isActive"] = true
        };
        var docRef = await _db.Collection(UsersCollection).AddAsync(data, ct);
        return new UserDto
        {
            UserId = docRef.Id,
            Username = req.Username,
            PasswordHash = data["passwordHash"]!.ToString()!,
            Role = data["role"]!.ToString()!,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public async Task<bool> UpdateUserAsync(string userId, UpdateUserRequest req, CancellationToken ct = default)
    {
        var doc = _db.Collection(UsersCollection).Document(userId);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return false;
        var updates = new Dictionary<string, object?>
        {
            ["username"] = req.Username,
            ["role"] = string.IsNullOrWhiteSpace(req.Role) ? "employee" : req.Role,
            ["isActive"] = req.IsActive,
            ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
        };
        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            updates["passwordHash"] = HashPassword(req.Password!);
        }
        await doc.SetAsync(updates, SetOptions.MergeAll, ct);
        return true;
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        var doc = _db.Collection(UsersCollection).Document(userId);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return false;
        await doc.DeleteAsync(cancellationToken: ct);
        return true;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var user = await GetByUsernameAsync(req.Username, ct);
        if (user == null) return null;
        if (!user.IsActive) return null;
        var hash = HashPassword(req.Password);
        if (!string.Equals(hash, user.PasswordHash, StringComparison.Ordinal)) return null;
        // update last login
        await _db.Collection(UsersCollection).Document(user.UserId!).SetAsync(
            new Dictionary<string, object?> { ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow) }, SetOptions.MergeAll, ct);
        return new LoginResponse
        {
            UserId = user.UserId!,
            Username = user.Username,
            Role = user.Role,
            LastLoginAt = DateTime.UtcNow
        };
    }

    public string HashPassword(string plain)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plain);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static UserDto? MapUser(DocumentSnapshot d)
    {
        try
        {
            var dict = d.ToDictionary();
            return new UserDto
            {
                UserId = d.Id,
                Username = dict.TryGetValue("username", out var u) ? u?.ToString() ?? string.Empty : string.Empty,
                PasswordHash = dict.TryGetValue("passwordHash", out var p) ? p?.ToString() ?? string.Empty : string.Empty,
                Role = dict.TryGetValue("role", out var r) ? r?.ToString() ?? "employee" : "employee",
                CreatedAt = dict.TryGetValue("createdAt", out var ca) && ca is Timestamp tc ? tc.ToDateTime() : DateTime.UtcNow,
                UpdatedAt = dict.TryGetValue("updatedAt", out var ua) && ua is Timestamp tu ? tu.ToDateTime() : DateTime.UtcNow,
                IsActive = dict.TryGetValue("isActive", out var ia) && ia is bool b && b
            };
        }
        catch
        {
            return null;
        }
    }
}
