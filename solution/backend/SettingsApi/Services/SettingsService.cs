using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using Microsoft.Extensions.Options;

namespace SettingsApi.Services;

public class FirestoreOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}

public class FrontendSettings
{
    public string? ApiBaseUrl { get; set; }
    public string? Theme { get; set; }
    public decimal? RatePerMinute { get; set; }
}

public class BackendSettings
{
    public string? ConnectionString { get; set; }
    public string? Notes { get; set; }
}

public class AppSettings
{
    public string? Locale { get; set; }
    public bool? EnableNotifications { get; set; }
    public Dictionary<string, object?>? Extras { get; set; }
}

public interface ISettingsService
{
    Task<FrontendSettings?> GetFrontendAsync(string? hostOverride = null, CancellationToken ct = default);
    Task<bool> SaveFrontendAsync(FrontendSettings settings, string? hostOverride = null, CancellationToken ct = default);
    Task<BackendSettings?> GetBackendAsync(string? hostOverride = null, CancellationToken ct = default);
    Task<bool> SaveBackendAsync(BackendSettings settings, string? hostOverride = null, CancellationToken ct = default);
    Task<AppSettings?> GetAppAsync(string? hostOverride = null, CancellationToken ct = default);
    Task<bool> SaveAppAsync(AppSettings settings, string? hostOverride = null, CancellationToken ct = default);
}

public class SettingsService : ISettingsService
{
    private readonly FirestoreDb _db;
    private const string RootCollection = "pos-app-settings";

    public SettingsService(IOptions<FirestoreOptions> options, IWebHostEnvironment env)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.ProjectId))
        {
            throw new InvalidOperationException("Firestore:ProjectId is not configured");
        }
        string? credsEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        string credentialsPath = credsEnv ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, opt.CredentialsPath));
        if (!File.Exists(credentialsPath))
        {
            throw new FileNotFoundException($"Firestore credentials not found at {credentialsPath}. Ensure serviceAccount.json is present or GOOGLE_APPLICATION_CREDENTIALS is set.");
        }
        GoogleCredential credential = GoogleCredential.FromFile(credentialsPath);
        var channelCredentials = credential.ToChannelCredentials();
        _db = new FirestoreDbBuilder
        {
            ProjectId = opt.ProjectId,
            ChannelCredentials = channelCredentials
        }.Build();
    }

    private static string HostKey(string? hostOverride)
    {
        if (!string.IsNullOrWhiteSpace(hostOverride)) return hostOverride!;
        try { return System.Net.Dns.GetHostName(); }
        catch { return Environment.MachineName; }
    }

    public async Task<FrontendSettings?> GetFrontendAsync(string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("frontend").Document("settings");
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return null;
        var dict = snap.ToDictionary();
        var res = new FrontendSettings();
        if (dict.TryGetValue("apiBaseUrl", out var a)) res.ApiBaseUrl = a?.ToString();
        if (dict.TryGetValue("theme", out var t)) res.Theme = t?.ToString();
        if (dict.TryGetValue("ratePerMinute", out var rpm) && rpm is double dd) res.RatePerMinute = Convert.ToDecimal(dd);
        return res;
    }

    public async Task<bool> SaveFrontendAsync(FrontendSettings settings, string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("frontend").Document("settings");
        var data = new Dictionary<string, object?>
        {
            ["apiBaseUrl"] = settings.ApiBaseUrl,
            ["theme"] = settings.Theme,
            ["ratePerMinute"] = settings.RatePerMinute.HasValue ? Convert.ToDouble(settings.RatePerMinute.Value) : null
        };
        await doc.SetAsync(data, SetOptions.MergeAll, ct);
        return true;
    }

    public async Task<BackendSettings?> GetBackendAsync(string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("backend").Document("settings");
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return null;
        var dict = snap.ToDictionary();
        var res = new BackendSettings();
        if (dict.TryGetValue("connectionString", out var c)) res.ConnectionString = c?.ToString();
        if (dict.TryGetValue("notes", out var n)) res.Notes = n?.ToString();
        return res;
    }

    public async Task<bool> SaveBackendAsync(BackendSettings settings, string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("backend").Document("settings");
        var data = new Dictionary<string, object?>
        {
            ["connectionString"] = settings.ConnectionString,
            ["notes"] = settings.Notes
        };
        await doc.SetAsync(data, SetOptions.MergeAll, ct);
        return true;
    }

    public async Task<AppSettings?> GetAppAsync(string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("app").Document("settings");
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return null;
        var dict = snap.ToDictionary();
        var res = new AppSettings();
        if (dict.TryGetValue("locale", out var l)) res.Locale = l?.ToString();
        if (dict.TryGetValue("enableNotifications", out var n) && n is bool b) res.EnableNotifications = b;
        // store any remaining as extras
        res.Extras = dict;
        return res;
    }

    public async Task<bool> SaveAppAsync(AppSettings settings, string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("app").Document("settings");
        var data = new Dictionary<string, object?>
        {
            ["locale"] = settings.Locale,
            ["enableNotifications"] = settings.EnableNotifications
        };
        if (settings.Extras != null)
        {
            foreach (var kv in settings.Extras)
            {
                if (!data.ContainsKey(kv.Key)) data[kv.Key] = kv.Value;
            }
        }
        await doc.SetAsync(data, SetOptions.MergeAll, ct);
        return true;
    }
}
