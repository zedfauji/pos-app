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
    // Optional URL fields to centralize client configuration
    public string? BackendApiUrl { get; set; }
    public string? SettingsApiUrl { get; set; }
    public string? TablesApiUrl { get; set; }
}

public class AppSettings
{
    public string? Locale { get; set; }
    public bool? EnableNotifications { get; set; }
    public ReceiptSettings? ReceiptSettings { get; set; }
    public Dictionary<string, object?>? Extras { get; set; }
}

public class ReceiptSettings
{
    public string? DefaultPrinter { get; set; }
    public string? ReceiptSize { get; set; } = "80mm";
    public bool? AutoPrintOnPayment { get; set; } = true;
    public bool? PreviewBeforePrint { get; set; } = true;
    public bool? PrintProForma { get; set; } = true;
    public bool? PrintFinalReceipt { get; set; } = true;
    public int? CopiesForFinalReceipt { get; set; } = 2;
    public string? BusinessName { get; set; } = "MagiDesk Billiard Club";
    public string? BusinessAddress { get; set; } = "123 Main Street, City, State 12345";
    public string? BusinessPhone { get; set; } = "(555) 123-4567";
}

public interface ISettingsService
{
    Task<FrontendSettings?> GetFrontendAsync(string? hostOverride = null, CancellationToken ct = default);
    Task<bool> SaveFrontendAsync(FrontendSettings settings, string? hostOverride = null, CancellationToken ct = default);
    Task<BackendSettings?> GetBackendAsync(string? hostOverride = null, CancellationToken ct = default);
    Task<bool> SaveBackendAsync(BackendSettings settings, string? hostOverride = null, CancellationToken ct = default);
    Task<AppSettings?> GetAppAsync(string? hostOverride = null, CancellationToken ct = default);
    Task<bool> SaveAppAsync(AppSettings settings, string? hostOverride = null, CancellationToken ct = default);
    Task<List<Dictionary<string, object?>>> GetAuditAsync(string? hostOverride = null, int limit = 50, CancellationToken ct = default);
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
        await WriteAuditAsync(host, "frontend", data, ct);
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
        if (dict.TryGetValue("backendApiUrl", out var b)) res.BackendApiUrl = b?.ToString();
        if (dict.TryGetValue("settingsApiUrl", out var s)) res.SettingsApiUrl = s?.ToString();
        if (dict.TryGetValue("tablesApiUrl", out var t)) res.TablesApiUrl = t?.ToString();
        return res;
    }

    public async Task<bool> SaveBackendAsync(BackendSettings settings, string? hostOverride = null, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var doc = _db.Collection(RootCollection).Document(host).Collection("backend").Document("settings");
        var data = new Dictionary<string, object?>
        {
            ["connectionString"] = settings.ConnectionString,
            ["notes"] = settings.Notes,
            ["backendApiUrl"] = settings.BackendApiUrl,
            ["settingsApiUrl"] = settings.SettingsApiUrl,
            ["tablesApiUrl"] = settings.TablesApiUrl
        };
        await doc.SetAsync(data, SetOptions.MergeAll, ct);
        await WriteAuditAsync(host, "backend", data, ct);
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
        
        // Parse receipt settings
        if (dict.TryGetValue("receiptSettings", out var rs) && rs is Dictionary<string, object> receiptDict)
        {
            res.ReceiptSettings = new ReceiptSettings();
            if (receiptDict.TryGetValue("defaultPrinter", out var dp)) res.ReceiptSettings.DefaultPrinter = dp?.ToString();
            if (receiptDict.TryGetValue("receiptSize", out var rsz)) res.ReceiptSettings.ReceiptSize = rsz?.ToString();
            if (receiptDict.TryGetValue("autoPrintOnPayment", out var ap) && ap is bool apb) res.ReceiptSettings.AutoPrintOnPayment = apb;
            if (receiptDict.TryGetValue("previewBeforePrint", out var pbp) && pbp is bool pbpb) res.ReceiptSettings.PreviewBeforePrint = pbpb;
            if (receiptDict.TryGetValue("printProForma", out var ppf) && ppf is bool ppfb) res.ReceiptSettings.PrintProForma = ppfb;
            if (receiptDict.TryGetValue("printFinalReceipt", out var pfr) && pfr is bool pfrb) res.ReceiptSettings.PrintFinalReceipt = pfrb;
            if (receiptDict.TryGetValue("copiesForFinalReceipt", out var cfr) && cfr is long cfri) res.ReceiptSettings.CopiesForFinalReceipt = (int)cfri;
            if (receiptDict.TryGetValue("businessName", out var bn)) res.ReceiptSettings.BusinessName = bn?.ToString();
            if (receiptDict.TryGetValue("businessAddress", out var ba)) res.ReceiptSettings.BusinessAddress = ba?.ToString();
            if (receiptDict.TryGetValue("businessPhone", out var bp)) res.ReceiptSettings.BusinessPhone = bp?.ToString();
        }
        
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
        
        // Save receipt settings
        if (settings.ReceiptSettings != null)
        {
            var receiptData = new Dictionary<string, object?>
            {
                ["defaultPrinter"] = settings.ReceiptSettings.DefaultPrinter,
                ["receiptSize"] = settings.ReceiptSettings.ReceiptSize,
                ["autoPrintOnPayment"] = settings.ReceiptSettings.AutoPrintOnPayment,
                ["previewBeforePrint"] = settings.ReceiptSettings.PreviewBeforePrint,
                ["printProForma"] = settings.ReceiptSettings.PrintProForma,
                ["printFinalReceipt"] = settings.ReceiptSettings.PrintFinalReceipt,
                ["copiesForFinalReceipt"] = settings.ReceiptSettings.CopiesForFinalReceipt,
                ["businessName"] = settings.ReceiptSettings.BusinessName,
                ["businessAddress"] = settings.ReceiptSettings.BusinessAddress,
                ["businessPhone"] = settings.ReceiptSettings.BusinessPhone
            };
            data["receiptSettings"] = receiptData;
        }
        
        if (settings.Extras != null)
        {
            foreach (var kv in settings.Extras)
            {
                if (!data.ContainsKey(kv.Key)) data[kv.Key] = kv.Value;
            }
        }
        await doc.SetAsync(data, SetOptions.MergeAll, ct);
        await WriteAuditAsync(host, "app", data, ct);
        return true;
    }

    private async Task WriteAuditAsync(string host, string section, Dictionary<string, object?> data, CancellationToken ct)
    {
        try
        {
            var audit = _db.Collection(RootCollection).Document(host).Collection("audit").Document();
            var payload = new Dictionary<string, object?>
            {
                ["section"] = section,
                ["timestamp"] = Timestamp.FromDateTime(DateTime.UtcNow),
                ["changes"] = data
            };
            await audit.SetAsync(payload, cancellationToken: ct);
        }
        catch { }
    }

    public async Task<List<Dictionary<string, object?>>> GetAuditAsync(string? hostOverride = null, int limit = 50, CancellationToken ct = default)
    {
        var host = HostKey(hostOverride);
        var col = _db.Collection(RootCollection).Document(host).Collection("audit");
        var query = col.OrderByDescending("timestamp").Limit(limit);
        var snaps = await query.GetSnapshotAsync(ct);
        var list = new List<Dictionary<string, object?>>();
        foreach (var doc in snaps.Documents)
        {
            list.Add(doc.ToDictionary());
        }
        return list;
    }

    // Defaults providers
    public static FrontendSettings DefaultFrontend() => new FrontendSettings
    {
        ApiBaseUrl = "https://localhost:5001",
        Theme = "System",
        RatePerMinute = 0m
    };

    public static BackendSettings DefaultBackend() => new BackendSettings
    {
        BackendApiUrl = "https://localhost:5001",
        SettingsApiUrl = "https://localhost:5003",
        TablesApiUrl = "https://localhost:5005"
    };

    public static AppSettings DefaultApp() => new AppSettings
    {
        Locale = "en-US",
        EnableNotifications = false,
        ReceiptSettings = new ReceiptSettings
        {
            DefaultPrinter = "",
            ReceiptSize = "80mm",
            AutoPrintOnPayment = true,
            PreviewBeforePrint = true,
            PrintProForma = true,
            PrintFinalReceipt = true,
            CopiesForFinalReceipt = 2,
            BusinessName = "MagiDesk Billiard Club",
            BusinessAddress = "123 Main Street, City, State 12345",
            BusinessPhone = "(555) 123-4567"
        },
        Extras = new Dictionary<string, object?>
        {
            ["tables.session.warnMinutes"] = 0,
            ["tables.session.autoStopMinutes"] = 0
        }
    };
}
