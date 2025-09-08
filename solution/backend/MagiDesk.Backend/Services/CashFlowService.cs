using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Backend.Services;

public interface ICashFlowService
{
    Task AddCashFlowAsync(CashFlow entry, CancellationToken ct = default);
    Task<List<CashFlow>> GetCashFlowHistoryAsync(CancellationToken ct = default);
    Task UpdateCashFlowAsync(CashFlow entry, CancellationToken ct = default);
}

public class CashFlowService : ICashFlowService
{
    private readonly FirestoreDb _db;
    private const string CollectionName = "cashflow";

    public CashFlowService(Microsoft.Extensions.Options.IOptions<FirestoreOptions> options, IWebHostEnvironment env)
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

    public async Task AddCashFlowAsync(CashFlow entry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Id)) entry.Id = Guid.NewGuid().ToString();
        var doc = _db.Collection(CollectionName).Document(entry.Id);
        var data = new Dictionary<string, object?>
        {
            ["Id"] = entry.Id,
            ["EmployeeName"] = entry.EmployeeName,
            ["Date"] = Timestamp.FromDateTime(entry.Date.ToUniversalTime()),
            ["CashAmount"] = Convert.ToDouble(entry.CashAmount),
            ["Notes"] = entry.Notes
        };
        await doc.SetAsync(data, SetOptions.MergeAll, ct);
    }

    public async Task UpdateCashFlowAsync(CashFlow entry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Id)) throw new ArgumentException("Id is required for update");
        await AddCashFlowAsync(entry, ct);
    }

    public async Task<List<CashFlow>> GetCashFlowHistoryAsync(CancellationToken ct = default)
    {
        var col = _db.Collection(CollectionName);
        var snap = await col.OrderByDescending("Date").GetSnapshotAsync(ct);
        var list = new List<CashFlow>();
        foreach (var d in snap.Documents)
        {
            try
            {
                var dict = d.ToDictionary();
                var model = new CashFlow
                {
                    Id = dict.TryGetValue("Id", out var id) ? id?.ToString() : d.Id,
                    EmployeeName = dict.TryGetValue("EmployeeName", out var en) ? en?.ToString() ?? string.Empty : string.Empty,
                    Date = dict.TryGetValue("Date", out var dt) && dt is Timestamp ts ? ts.ToDateTime() : DateTime.UtcNow,
                    CashAmount = dict.TryGetValue("CashAmount", out var ca) && ca is double dd ? Convert.ToDecimal(dd) : 0m,
                    Notes = dict.TryGetValue("Notes", out var notes) ? notes?.ToString() : null
                };
                list.Add(model);
            }
            catch { }
        }
        return list;
    }
}
