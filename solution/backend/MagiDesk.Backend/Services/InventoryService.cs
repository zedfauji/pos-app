using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Backend.Services;

public interface IInventoryService
{
    Task<List<InventoryItem>> GetInventoryAsync(CancellationToken ct = default);
    Task<string> LaunchSyncProductNamesAsync(CancellationToken ct = default);
    Task<JobStatusDto?> GetJobAsync(string jobId, CancellationToken ct = default);
    Task<List<JobStatusDto>> GetRecentJobsAsync(int limit = 10, CancellationToken ct = default);
}

public class InventoryService : IInventoryService
{
    private readonly FirestoreDb _db;
    private const string InventoryCollection = "inventario";
    private const string ProductsCollection = "productos";
    private const string JobsCollection = "jobs";

    public InventoryService(Microsoft.Extensions.Options.IOptions<FirestoreOptions> options, IWebHostEnvironment env)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.ProjectId))
            throw new InvalidOperationException("Firestore:ProjectId is not configured");
        string? credsEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        string credentialsPath = credsEnv ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, opt.CredentialsPath));
        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException($"Firestore credentials not found at {credentialsPath}.");
        GoogleCredential credential = GoogleCredential.FromFile(credentialsPath);
        var channelCredentials = credential.ToChannelCredentials();
        _db = new FirestoreDbBuilder { ProjectId = opt.ProjectId, ChannelCredentials = channelCredentials }.Build();
    }

    public async Task<List<InventoryItem>> GetInventoryAsync(CancellationToken ct = default)
    {
        var snap = await _db.Collection(InventoryCollection).GetSnapshotAsync(ct);
        var list = new List<InventoryItem>();
        foreach (var d in snap.Documents)
        {
            try
            {
                var dict = d.ToDictionary();
                var item = new InventoryItem
                {
                    Id = d.Id,
                    clave = dict.TryGetValue("clave", out var v1) ? v1?.ToString() : null,
                    productname = dict.TryGetValue("productname", out var v2) ? v2?.ToString() : null,
                    entradas = dict.TryGetValue("entradas", out var e) && e is double ed ? ed : 0,
                    salidas = dict.TryGetValue("salidas", out var s) && s is double sd ? sd : 0,
                    saldo = dict.TryGetValue("saldo", out var sa) && sa is double sad ? sad : 0,
                    migrated_at = dict.TryGetValue("migrated_at", out var m) && m is Timestamp ts ? (DateTime?)ts.ToDateTime() : null
                };
                list.Add(item);
            }
            catch { }
        }
        return list.OrderByDescending(i => i.migrated_at ?? DateTime.MinValue).ToList();
    }

    public async Task<string> LaunchSyncProductNamesAsync(CancellationToken ct = default)
    {
        var jobId = Guid.NewGuid().ToString("N");
        var jobRef = _db.Collection(JobsCollection).Document(jobId);
        await jobRef.SetAsync(new Dictionary<string, object?>
        {
            ["type"] = "syncProductNames",
            ["status"] = "running",
            ["startedAt"] = Timestamp.FromDateTime(DateTime.UtcNow),
        }, SetOptions.Overwrite, ct);

        _ = Task.Run(async () =>
        {
            int updated = 0;
            try
            {
                var invSnap = await _db.Collection(InventoryCollection).GetSnapshotAsync(ct);
                foreach (var doc in invSnap.Documents)
                {
                    try
                    {
                        var dict = doc.ToDictionary();
                        var clave = dict.TryGetValue("clave", out var c) ? c?.ToString() : null;
                        if (string.IsNullOrWhiteSpace(clave)) continue;
                        var prodQuery = await _db.Collection(ProductsCollection).WhereEqualTo("clave", clave).Limit(1).GetSnapshotAsync(ct);
                        if (prodQuery.Count > 0)
                        {
                            var p = prodQuery.Documents[0].ToDictionary();
                            var name = p.TryGetValue("productname", out var pn) ? pn?.ToString() : null;
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                await doc.Reference.SetAsync(new Dictionary<string, object?>
                                {
                                    ["productname"] = name
                                }, SetOptions.MergeAll, ct);
                                updated++;
                            }
                        }
                    }
                    catch { }
                }
                await jobRef.SetAsync(new Dictionary<string, object?>
                {
                    ["status"] = "completed",
                    ["updatedCount"] = updated,
                    ["finishedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
                }, SetOptions.MergeAll, ct);
            }
            catch (Exception ex)
            {
                await jobRef.SetAsync(new Dictionary<string, object?>
                {
                    ["status"] = "failed",
                    ["error"] = ex.Message,
                    ["finishedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
                }, SetOptions.MergeAll, ct);
            }
        }, ct);

        return jobId;
    }

    public async Task<JobStatusDto?> GetJobAsync(string jobId, CancellationToken ct = default)
    {
        var doc = await _db.Collection(JobsCollection).Document(jobId).GetSnapshotAsync(ct);
        if (!doc.Exists) return null;
        var dict = doc.ToDictionary();
        return new JobStatusDto
        {
            JobId = doc.Id,
            Type = dict.TryGetValue("type", out var t) ? t?.ToString() ?? "" : "",
            Status = dict.TryGetValue("status", out var s) ? s?.ToString() ?? "" : "",
            UpdatedCount = dict.TryGetValue("updatedCount", out var u) && u is long ul ? (int)ul : 0,
            StartedAt = dict.TryGetValue("startedAt", out var st) && st is Timestamp ts1 ? ts1.ToDateTime() : null,
            FinishedAt = dict.TryGetValue("finishedAt", out var ft) && ft is Timestamp ts2 ? ts2.ToDateTime() : null,
            Error = dict.TryGetValue("error", out var er) ? er?.ToString() : null
        };
    }

    public async Task<List<JobStatusDto>> GetRecentJobsAsync(int limit = 10, CancellationToken ct = default)
    {
        // Avoid composite index requirement by ordering only on startedAt and filtering type in-memory.
        // We fetch a bit more than requested to account for filtering.
        int fetch = Math.Max(limit * 3, 20);
        Query query = _db.Collection(JobsCollection).OrderByDescending("startedAt").Limit(fetch);
        QuerySnapshot snap;
        try
        {
            snap = await query.GetSnapshotAsync(ct);
        }
        catch (Grpc.Core.RpcException)
        {
            // Fallback: fetch without order if index configuration still causes issues
            snap = await _db.Collection(JobsCollection).Limit(fetch).GetSnapshotAsync(ct);
        }

        var list = new List<JobStatusDto>();
        foreach (var d in snap.Documents)
        {
            var dict = d.ToDictionary();
            var item = new JobStatusDto
            {
                JobId = d.Id,
                Type = dict.TryGetValue("type", out var t) ? t?.ToString() ?? "" : "",
                Status = dict.TryGetValue("status", out var s) ? s?.ToString() ?? "" : "",
                UpdatedCount = dict.TryGetValue("updatedCount", out var u) && u is long ul ? (int)ul : 0,
                StartedAt = dict.TryGetValue("startedAt", out var st) && st is Timestamp ts1 ? ts1.ToDateTime() : null,
                FinishedAt = dict.TryGetValue("finishedAt", out var ft) && ft is Timestamp ts2 ? ts2.ToDateTime() : null,
                Error = dict.TryGetValue("error", out var er) ? er?.ToString() : null
            };
            list.Add(item);
        }

        // Filter and order in-memory
        return list
            .Where(j => string.Equals(j.Type, "syncProductNames", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(j => j.StartedAt ?? DateTime.MinValue)
            .Take(limit)
            .ToList();
    }
}
