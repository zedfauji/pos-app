using Google.Cloud.Firestore;
using System;
using MagiDesk.Shared.DTOs;
using System.Linq;

namespace MagiDesk.Backend.Services;

public interface IOrdersService
{
    Task<string> FinalizeOrderAsync(OrderDto order, CancellationToken ct = default);
    Task<CartDraftDto> SaveDraftAsync(CartDraftDto draft, CancellationToken ct = default);
    Task<CartDraftDto?> GetDraftAsync(string draftId, CancellationToken ct = default);
    Task<List<CartDraftDto>> GetDraftsAsync(int limit = 20, CancellationToken ct = default);
    Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<List<OrdersJobDto>> GetJobsAsync(int limit = 10, CancellationToken ct = default);
    Task<List<OrderNotificationDto>> GetNotificationsAsync(int limit = 50, CancellationToken ct = default);
}

public class OrdersService : IOrdersService
{
    private readonly FirestoreDb _db;
    private readonly IEmailService _email;
    private readonly ILogger<OrdersService> _logger;

    private const string OrdersCol = "orders";
    private const string DraftsCol = "cart-draft";
    private const string JobsCol = "orders_jobs";
    private const string NotifsCol = "order_notifications";
    private const string VendorsCol = "vendors";

    public OrdersService(Microsoft.Extensions.Options.IOptions<FirestoreOptions> options, IWebHostEnvironment env, IEmailService email, ILogger<OrdersService> logger)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.ProjectId))
            throw new InvalidOperationException("Firestore:ProjectId is not configured");
        string? credsEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        string credentialsPath = credsEnv ?? Path.GetFullPath(Path.Combine(env.ContentRootPath, opt.CredentialsPath));
        if (!File.Exists(credentialsPath))
            throw new FileNotFoundException($"Firestore credentials not found at {credentialsPath}.");
        _db = new FirestoreDbBuilder { ProjectId = opt.ProjectId, CredentialsPath = credentialsPath }.Build();
        _email = email;
        _logger = logger;
    }

    public async Task<string> FinalizeOrderAsync(OrderDto order, CancellationToken ct = default)
    {
        // Scaffold: write order doc and a simple job record
        if (string.IsNullOrWhiteSpace(order.OrderId)) order.OrderId = Guid.NewGuid().ToString("N");
        order.Status = "submitted";
        order.CreatedAt = DateTime.UtcNow;
        // Store with lowercase field names for consistency with reads
        var items = order.Items.Select(i => new Dictionary<string, object?>
        {
            ["itemId"] = i.ItemId,
            ["itemName"] = i.ItemName,
            ["quantity"] = i.Quantity,
            ["price"] = (double)i.Price
        }).ToList();
        await _db.Collection(OrdersCol).Document(order.OrderId).SetAsync(new Dictionary<string, object?>
        {
            ["orderId"] = order.OrderId,
            ["vendorId"] = order.VendorId,
            ["vendorName"] = order.VendorName,
            ["items"] = items,
            ["totalAmount"] = (double)order.TotalAmount,
            ["status"] = order.Status,
            ["createdAt"] = Timestamp.FromDateTime(order.CreatedAt.ToUniversalTime())
        }, cancellationToken: ct);

        var jobId = Guid.NewGuid().ToString("N");
        var jobRef = _db.Collection(JobsCol).Document(jobId);
        await jobRef.SetAsync(new Dictionary<string, object?>
        {
            ["jobId"] = jobId,
            ["vendorName"] = order.VendorName,
            ["status"] = "submitted",
            ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
        }, cancellationToken: ct);
        // Notification (order finalized)
        await _db.Collection(NotifsCol).AddAsync(new Dictionary<string, object?>
        {
            ["eventType"] = "order-finalized",
            ["vendorName"] = order.VendorName,
            ["orderId"] = order.OrderId,
            ["timestamp"] = Timestamp.FromDateTime(DateTime.UtcNow),
            ["statusMessage"] = "Order submitted"
        }, ct);

        // Run post-processing outside of the HTTP request cancellation scope with a short timeout
        _ = Task.Run(async () =>
        {
            using var bgCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var token = bgCts.Token;
            try
            {
                // Build vendor index (itemId -> (vendorId, vendorName))
                var vendorIndex = await BuildVendorItemIndexAsync(token);

                // Group items by vendor
                var groups = new Dictionary<string, (string VendorName, List<MagiDesk.Shared.DTOs.OrderItemDto> Items)>();
                foreach (var it in order.Items)
                {
                    if (!string.IsNullOrWhiteSpace(it.ItemId) && vendorIndex.TryGetValue(it.ItemId, out var vinfo))
                    {
                        if (!groups.TryGetValue(vinfo.VendorId, out var g))
                            g = (vinfo.VendorName, new List<MagiDesk.Shared.DTOs.OrderItemDto>());
                        g.Items.Add(it);
                        groups[vinfo.VendorId] = g;
                    }
                    else
                    {
                        // Fallback to order vendor if index not found
                        var vid = string.IsNullOrWhiteSpace(order.VendorId) ? "unknown" : order.VendorId;
                        var vname = string.IsNullOrWhiteSpace(order.VendorName) ? "Unknown" : order.VendorName;
                        if (!groups.TryGetValue(vid, out var g)) g = (vname, new List<MagiDesk.Shared.DTOs.OrderItemDto>());
                        g.Items.Add(it);
                        groups[vid] = g;
                    }
                }

                // Send one email per vendor group
                foreach (var kvp in groups)
                {
                    var vendorId = kvp.Key;
                    var vendorName = kvp.Value.VendorName;
                    var items = kvp.Value.Items;
                    var to = await ResolveVendorEmailAsync(vendorId, vendorName, token) ?? "girishdudhwal@gmail.com";
                    var subject = $"Order {order.OrderId} for {vendorName}";
                    var itemsHtml = string.Join("", items.Select(i => $"<li>{i.ItemName} x{i.Quantity} @ {i.Price:C}</li>"));
                    var total = items.Aggregate(0m, (acc, i) => acc + (i.Price * i.Quantity));
                    var body = $"<h3>Order {order.OrderId}</h3><p>Vendor: {vendorName}</p><ul>{itemsHtml}</ul><p>Total: {total:C}</p><p>Created at: {order.CreatedAt:O}</p>";
                    var ok = await _email.SendEmailAsync(to, subject, body, token);
                    await _db.Collection(NotifsCol).AddAsync(new Dictionary<string, object?>
                    {
                        ["eventType"] = ok ? "email-sent" : "email-failed",
                        ["vendorName"] = vendorName,
                        ["orderId"] = order.OrderId,
                        ["timestamp"] = Timestamp.FromDateTime(DateTime.UtcNow),
                        ["statusMessage"] = ok ? $"Email sent to {to}" : $"Email failed for {to}"
                    }, token);
                }

                await jobRef.SetAsync(new Dictionary<string, object?>
                {
                    ["status"] = "submitted",
                    ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
                }, SetOptions.MergeAll, token);
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogWarning(oce, "Finalize order post-processing timed out");
                await _db.Collection(NotifsCol).AddAsync(new Dictionary<string, object?>
                {
                    ["eventType"] = "email-timeout",
                    ["vendorName"] = order.VendorName,
                    ["orderId"] = order.OrderId,
                    ["timestamp"] = Timestamp.FromDateTime(DateTime.UtcNow),
                    ["statusMessage"] = "Post-processing timed out"
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Finalize order post-processing failed");
                await _db.Collection(NotifsCol).AddAsync(new Dictionary<string, object?>
                {
                    ["eventType"] = "email-failed",
                    ["vendorName"] = order.VendorName,
                    ["orderId"] = order.OrderId,
                    ["timestamp"] = Timestamp.FromDateTime(DateTime.UtcNow),
                    ["statusMessage"] = ex.Message
                }, CancellationToken.None);
            }
        }, CancellationToken.None);
        return jobId;
    }

    private async Task<string?> ResolveVendorEmailAsync(string vendorId, string vendorName, CancellationToken ct)
    {
        // Try by doc id
        if (!string.IsNullOrWhiteSpace(vendorId))
        {
            var doc = await _db.Collection(VendorsCol).Document(vendorId).GetSnapshotAsync(ct);
            if (doc.Exists)
            {
                var map = doc.ToDictionary();
                if (map.TryGetValue("email", out var em) && em is string es && !string.IsNullOrWhiteSpace(es))
                    return es;
            }
        }
        // Try by name (case-insensitive) if supported
        if (!string.IsNullOrWhiteSpace(vendorName))
        {
            // Firestore doesn't support case-insensitive contains without indices; try exact equality first
            var snap = await _db.Collection(VendorsCol).WhereEqualTo("name", vendorName).Limit(1).GetSnapshotAsync(ct);
            if (snap.Count > 0)
            {
                var map = snap.Documents[0].ToDictionary();
                if (map.TryGetValue("email", out var em) && em is string es && !string.IsNullOrWhiteSpace(es))
                    return es;
            }
        }
        return null;
    }

    private async Task<Dictionary<string, (string VendorId, string VendorName)>> BuildVendorItemIndexAsync(CancellationToken ct)
    {
        var index = new Dictionary<string, (string VendorId, string VendorName)>();
        var vendorsSnap = await _db.Collection(VendorsCol).GetSnapshotAsync(ct);
        foreach (var vdoc in vendorsSnap.Documents)
        {
            string vendorId = vdoc.Id;
            string vendorName = string.Empty;
            try
            {
                var dict = vdoc.ToDictionary();
                vendorName = dict.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
                if (dict.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> arr)
                {
                    foreach (var el in arr)
                    {
                        if (el is Dictionary<string, object?> im)
                        {
                            var id = im.TryGetValue("id", out var iid) ? iid?.ToString() ?? string.Empty : string.Empty;
                            if (!string.IsNullOrWhiteSpace(id))
                                index[id] = (vendorId, vendorName);
                        }
                    }
                }
            }
            catch { }
        }
        return index;
    }

    public async Task<CartDraftDto> SaveDraftAsync(CartDraftDto draft, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draft.CartId)) draft.CartId = Guid.NewGuid().ToString("N");
        draft.Status = "draft";
        draft.CreatedAt = draft.CreatedAt == default ? DateTime.UtcNow : draft.CreatedAt;
        await _db.Collection(DraftsCol).Document(draft.CartId).SetAsync(draft, cancellationToken: ct);
        await _db.Collection(JobsCol).AddAsync(new Dictionary<string, object?>
        {
            ["jobId"] = draft.CartId,
            ["vendorName"] = draft.VendorName,
            ["status"] = "draft",
            ["updatedAt"] = Timestamp.FromDateTime(DateTime.UtcNow)
        }, ct);
        return draft;
    }

    public async Task<CartDraftDto?> GetDraftAsync(string draftId, CancellationToken ct = default)
    {
        var doc = await _db.Collection(DraftsCol).Document(draftId).GetSnapshotAsync(ct);
        if (!doc.Exists) return null;
        return doc.ConvertTo<CartDraftDto>();
    }

    public async Task<List<CartDraftDto>> GetDraftsAsync(int limit = 20, CancellationToken ct = default)
    {
        var snap = await _db.Collection(DraftsCol).OrderByDescending("createdAt").Limit(limit).GetSnapshotAsync(ct);
        return snap.Documents.Select(d => d.ConvertTo<CartDraftDto>()).ToList();
    }

    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct = default)
    {
        var snap = await _db.Collection(OrdersCol).OrderByDescending("createdAt").Limit(100).GetSnapshotAsync(ct);
        var list = new List<OrderDto>();
        foreach (var d in snap.Documents)
        {
            var m = d.ToDictionary();
            var order = new OrderDto
            {
                OrderId = m.TryGetValue("orderId", out var oid) ? oid?.ToString() ?? d.Id : (m.TryGetValue("OrderId", out var oid2) ? oid2?.ToString() ?? d.Id : d.Id),
                VendorId = m.TryGetValue("vendorId", out var vid) ? vid?.ToString() ?? string.Empty : (m.TryGetValue("VendorId", out var vid2) ? vid2?.ToString() ?? string.Empty : string.Empty),
                VendorName = m.TryGetValue("vendorName", out var vnm) ? vnm?.ToString() ?? string.Empty : (m.TryGetValue("VendorName", out var vnm2) ? vnm2?.ToString() ?? string.Empty : string.Empty),
                TotalAmount = m.TryGetValue("totalAmount", out var ta) && ta is double dd ? (decimal)dd : (m.TryGetValue("totalAmount", out var ta2) && ta2 is long ll ? (decimal)ll : (m.TryGetValue("TotalAmount", out var ta3) && ta3 is double dd3 ? (decimal)dd3 : 0m)),
                Status = m.TryGetValue("status", out var st) ? st?.ToString() ?? string.Empty : (m.TryGetValue("Status", out var st2) ? st2?.ToString() ?? string.Empty : string.Empty),
                CreatedAt = m.TryGetValue("createdAt", out var ca) && ca is Timestamp ts ? ts.ToDateTime() : (m.TryGetValue("CreatedAt", out var ca2) && ca2 is Timestamp ts2 ? ts2.ToDateTime() : DateTime.MinValue),
            };
            if ((m.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> arr) || (m.TryGetValue("Items", out var itemsObj2) && (arr = itemsObj2 as IEnumerable<object>) != null))
            {
                foreach (var itObj in arr)
                {
                    if (itObj is Dictionary<string, object> im)
                    {
                        var item = new OrderItemDto
                        {
                            ItemId = im.TryGetValue("itemId", out var iid) ? iid?.ToString() ?? string.Empty : (im.TryGetValue("ItemId", out var iid2) ? iid2?.ToString() ?? string.Empty : string.Empty),
                            ItemName = im.TryGetValue("itemName", out var inm) ? inm?.ToString() ?? string.Empty : (im.TryGetValue("ItemName", out var inm2) ? inm2?.ToString() ?? string.Empty : string.Empty),
                            Quantity = im.TryGetValue("quantity", out var q) && q is long lq ? (int)lq : (im.TryGetValue("Quantity", out var q2) && q2 is long lq2 ? (int)lq2 : 0),
                            Price = im.TryGetValue("price", out var pr) && pr is double dp ? (decimal)dp : (im.TryGetValue("price", out var pr2) && pr2 is long lp ? (decimal)lp : (im.TryGetValue("Price", out var pr3) && pr3 is double dp3 ? (decimal)dp3 : 0m))
                        };
                        order.Items.Add(item);
                    }
                }
            }
            list.Add(order);
        }
        return list;
    }

    public async Task<List<OrdersJobDto>> GetJobsAsync(int limit = 10, CancellationToken ct = default)
    {
        var snap = await _db.Collection(JobsCol).OrderByDescending("updatedAt").Limit(limit).GetSnapshotAsync(ct);
        return snap.Documents.Select(d =>
        {
            var m = d.ToDictionary();
            return new OrdersJobDto
            {
                JobId = m.TryGetValue("jobId", out var j) ? j?.ToString() ?? string.Empty : string.Empty,
                VendorName = m.TryGetValue("vendorName", out var v) ? v?.ToString() ?? string.Empty : string.Empty,
                Status = m.TryGetValue("status", out var s) ? s?.ToString() ?? string.Empty : string.Empty,
                UpdatedAt = m.TryGetValue("updatedAt", out var u) && u is Timestamp ts ? ts.ToDateTime() : DateTime.UtcNow
            };
        }).ToList();
    }

    public async Task<List<OrderNotificationDto>> GetNotificationsAsync(int limit = 50, CancellationToken ct = default)
    {
        var snap = await _db.Collection(NotifsCol).OrderByDescending("timestamp").Limit(limit).GetSnapshotAsync(ct);
        return snap.Documents.Select(d => d.ConvertTo<OrderNotificationDto>()).ToList();
    }
}
