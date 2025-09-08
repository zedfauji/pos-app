using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Grpc.Auth;
using MagiDesk.Backend.Models;
using MagiDesk.Shared.DTOs;
using Microsoft.Extensions.Options;
using System.Globalization;
using CsvHelper;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;

namespace MagiDesk.Backend.Services;

public class FirestoreOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}

public class AllVendorsDump
{
    public int VendorCount { get; set; }
    public List<VendorDump> Vendors { get; set; } = new();
}

public class VendorDump
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object?>>> Subcollections { get; set; } = new();
}

public class VendorDebugInfo
{
    public string VendorId { get; set; } = string.Empty;
    public bool DocumentExists { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
    public List<string> Subcollections { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object?>>> Samples { get; set; } = new();
}

public interface IFirestoreService
{
    // Vendors
    Task<List<VendorDto>> GetVendorsAsync(CancellationToken ct = default);
    Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default);
    Task<VendorDto> CreateVendorAsync(VendorDto dto, CancellationToken ct = default);
    Task<bool> UpdateVendorAsync(string id, VendorDto dto, CancellationToken ct = default);
    Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default);

    // Items (subcollection under vendor)
    Task<List<ItemDto>> GetItemsAsync(string vendorId, CancellationToken ct = default);
    Task<ItemDto?> GetItemAsync(string vendorId, string id, CancellationToken ct = default);
    Task<ItemDto> CreateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default);
    Task<bool> UpdateItemAsync(string vendorId, string id, ItemDto dto, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(string vendorId, string id, CancellationToken ct = default);

    // Debug helpers
    Task<VendorDebugInfo> GetVendorDebugAsync(string vendorId, CancellationToken ct = default);
    Task<AllVendorsDump> GetAllVendorsDumpAsync(CancellationToken ct = default);
    Task<string> ExportVendorItemsAsync(string vendorId, string format, CancellationToken ct = default);
    Task ImportVendorItemsAsync(string vendorId, string format, Stream fileStream, CancellationToken ct = default);
}

public class FirestoreService : IFirestoreService
{
    private readonly FirestoreDb _db;

    private const string VendorsCollection = "vendors";
    private const string ItemsSubcollection = "items";

    public FirestoreService(IOptions<FirestoreOptions> options, ILogger<FirestoreService> logger, IWebHostEnvironment env)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.ProjectId))
        {
            throw new InvalidOperationException("Firestore:ProjectId is not configured");
        }

        // Resolve credentials path; allow overriding by GOOGLE_APPLICATION_CREDENTIALS env var
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

    public async Task<string> ExportVendorItemsAsync(string vendorId, string format, CancellationToken ct = default)
    {
        var items = await GetItemsAsync(vendorId, ct);
        format = format.ToLowerInvariant();
        switch (format)
        {
            case "json":
                return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            case "yaml":
                var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
                return serializer.Serialize(items);
            case "csv":
                using (var sw = new StringWriter())
                using (var csv = new CsvWriter(sw, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(items);
                    return sw.ToString();
                }
            default:
                throw new ArgumentException("Unsupported format. Use csv, json, or yaml");
        }
    }

    public async Task ImportVendorItemsAsync(string vendorId, string format, Stream fileStream, CancellationToken ct = default)
    {
        format = format.ToLowerInvariant();
        List<ItemDto> items;
        if (format == "json")
        {
            // Accept either a bare array of ItemDto or a vendor dump object with items under $.items or $.fields.items
            using var sr = new StreamReader(fileStream);
            var text = await sr.ReadToEndAsync();
            items = new();
            try
            {
                var arr = JsonSerializer.Deserialize<List<ItemDto>>(text);
                if (arr is not null) items = arr;
            }
            catch
            {
                try
                {
                    using var doc = JsonDocument.Parse(text);
                    JsonElement root = doc.RootElement;
                    JsonElement itemsElement;
                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("items", out itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in itemsElement.EnumerateArray())
                        {
                            items.Add(ParseVendorItemElement(el));
                        }
                    }
                    else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("fields", out var fields) && fields.ValueKind == JsonValueKind.Object && fields.TryGetProperty("items", out itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var el in itemsElement.EnumerateArray())
                        {
                            items.Add(ParseVendorItemElement(el));
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("JSON root must be an array of items or contain 'items' at root or under 'fields'.");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to parse JSON items payload. Expected List<ItemDto> or vendor dump with items.", ex);
                }
            }
        }
        else if (format == "yaml")
        {
            using var sr = new StreamReader(fileStream);
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var text = await sr.ReadToEndAsync();
            items = deserializer.Deserialize<List<ItemDto>>(text) ?? new();
        }
        else if (format == "csv")
        {
            using var sr = new StreamReader(fileStream);
            using var csv = new CsvReader(sr, CultureInfo.InvariantCulture);
            items = csv.GetRecords<ItemDto>().ToList();
        }
        else
        {
            throw new ArgumentException("Unsupported format. Use csv, json, or yaml");
        }

        // Insert/update items
        foreach (var item in items)
        {
            // Enforce read-only SKU policy: set SKU to Id always, generate Id if missing
            if (string.IsNullOrWhiteSpace(item.Id)) item.Id = Guid.NewGuid().ToString();
            item.Sku = item.Id;
            var updated = await UpdateItemAsync(vendorId, item.Id, item, ct);
            if (!updated)
            {
                await CreateItemAsync(vendorId, item, ct);
            }
        }
    }

    private static ItemDto ParseVendorItemElement(JsonElement el)
    {
        string id = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();
        string name = el.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? string.Empty : string.Empty;
        decimal price = 0m;
        if (el.TryGetProperty("price", out var pProp))
        {
            if (pProp.ValueKind == JsonValueKind.Number)
            {
                if (pProp.TryGetDecimal(out var dec)) price = dec;
                else if (pProp.TryGetDouble(out var dbl)) price = Convert.ToDecimal(dbl);
                else if (pProp.TryGetInt64(out var lng)) price = lng;
            }
        }
        int stock = 0;
        if (el.TryGetProperty("inStock", out var sProp) && sProp.ValueKind == JsonValueKind.True) stock = 1;
        return new ItemDto
        {
            Id = id,
            Sku = id,
            Name = name,
            Price = price,
            Stock = stock
        };
    }

    // Vendors
    public async Task<List<VendorDto>> GetVendorsAsync(CancellationToken ct = default)
    {
        var snapshot = await _db.Collection(VendorsCollection).GetSnapshotAsync(ct);
        return snapshot.Documents.Select(d => MapVendor(d)).Where(v => v is not null)!.ToList()!;
    }

    public async Task<VendorDto?> GetVendorAsync(string id, CancellationToken ct = default)
    {
        var doc = _db.Collection(VendorsCollection).Document(id);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return null;
        return MapVendor(snap)!;
    }

    public async Task<VendorDto> CreateVendorAsync(VendorDto dto, CancellationToken ct = default)
    {
        var data = new Dictionary<string, object?>
        {
            ["name"] = dto.Name,
            ["contactInfo"] = dto.ContactInfo,
            ["status"] = dto.Status,
            ["budget"] = Convert.ToDouble(dto.Budget),
            ["reminder"] = dto.Reminder,
            ["reminderEnabled"] = dto.ReminderEnabled
        };
        var docRef = await _db.Collection(VendorsCollection).AddAsync(data, ct);
        dto.Id = docRef.Id;
        return dto;
    }

    public async Task<bool> UpdateVendorAsync(string id, VendorDto dto, CancellationToken ct = default)
    {
        var doc = _db.Collection(VendorsCollection).Document(id);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return false;
        var updates = new Dictionary<string, object?>
        {
            ["name"] = dto.Name,
            ["contactInfo"] = dto.ContactInfo,
            ["status"] = dto.Status,
            ["budget"] = Convert.ToDouble(dto.Budget),
            ["reminder"] = dto.Reminder,
            ["reminderEnabled"] = dto.ReminderEnabled
        };
        await doc.SetAsync(updates, SetOptions.MergeAll, cancellationToken: ct);
        return true;
    }

    // Debug helpers
    public async Task<VendorDebugInfo> GetVendorDebugAsync(string vendorId, CancellationToken ct = default)
    {
        var result = new VendorDebugInfo { VendorId = vendorId };
        var docRef = _db.Collection(VendorsCollection).Document(vendorId);
        var snap = await docRef.GetSnapshotAsync(ct);
        result.DocumentExists = snap.Exists;
        if (snap.Exists)
        {
            try { result.Fields = snap.ToDictionary(); } catch { result.Fields = new(); }
        }

        // List subcollections and sample a few documents from each
        var colRefs = docRef.ListCollectionsAsync();
        await foreach (var col in colRefs.WithCancellation(ct))
        {
            result.Subcollections.Add(col.Id);
            var subSnap = await col.Limit(3).GetSnapshotAsync(ct);
            var list = new List<Dictionary<string, object?>>();
            foreach (var d in subSnap.Documents)
            {
                try { list.Add(d.ToDictionary()); } catch { list.Add(new()); }
            }
            result.Samples[col.Id] = list;
        }

        return result;
    }

    public async Task<AllVendorsDump> GetAllVendorsDumpAsync(CancellationToken ct = default)
    {
        var dump = new AllVendorsDump();
        var vendorsSnap = await _db.Collection(VendorsCollection).GetSnapshotAsync(ct);
        dump.VendorCount = vendorsSnap.Count;
        foreach (var vdoc in vendorsSnap.Documents)
        {
            var vd = new VendorDump { Id = vdoc.Id };
            try { vd.Fields = vdoc.ToDictionary(); } catch { vd.Fields = new(); }

            var colRefs = vdoc.Reference.ListCollectionsAsync();
            await foreach (var col in colRefs.WithCancellation(ct))
            {
                var subSnap = await col.GetSnapshotAsync(ct);
                var list = new List<Dictionary<string, object?>>();
                foreach (var d in subSnap.Documents)
                {
                    try { list.Add(d.ToDictionary()); } catch { list.Add(new()); }
                }
                vd.Subcollections[col.Id] = list;
            }
            dump.Vendors.Add(vd);
        }
        return dump;
    }

    public async Task<bool> DeleteVendorAsync(string id, CancellationToken ct = default)
    {
        var doc = _db.Collection(VendorsCollection).Document(id);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return false;
        await doc.DeleteAsync(cancellationToken: ct);
        return true;
    }

    // Items
    public async Task<List<ItemDto>> GetItemsAsync(string vendorId, CancellationToken ct = default)
    {
        // Prefer embedded array "items" when present; otherwise fallback to subcollection
        var vdoc = await _db.Collection(VendorsCollection).Document(vendorId).GetSnapshotAsync(ct);
        if (vdoc.Exists)
        {
            var dict = vdoc.ToDictionary();
            if (dict.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> arr)
            {
                // Optional itemPrices map overrides prices by id
                Dictionary<string, object?> priceMap = new();
                if (dict.TryGetValue("itemPrices", out var ip) && ip is Dictionary<string, object?> pm)
                    priceMap = pm;

                var list = new List<ItemDto>();
                foreach (var el in arr)
                {
                    try
                    {
                        if (el is Dictionary<string, object?> idict)
                        {
                            var id = idict.TryGetValue("id", out var vid) ? vid?.ToString() ?? string.Empty : string.Empty;
                            var name = idict.TryGetValue("name", out var vname) ? vname?.ToString() ?? string.Empty : string.Empty;
                            decimal price = 0m;
                            if (priceMap.TryGetValue(id, out var ovr) && ovr is double dpr)
                                price = Convert.ToDecimal(dpr);
                            else if (idict.TryGetValue("price", out var p) && p is double dp)
                                price = Convert.ToDecimal(dp);
                            int stock = 0;
                            if (idict.TryGetValue("inStock", out var inst) && inst is bool b && b) stock = 1;
                            list.Add(new ItemDto { Id = id, Name = name, Price = price, Stock = stock, Sku = id });
                        }
                    }
                    catch { }
                }
                return list;
            }
        }

        // Fallback: read subcollection
        var itemsCol = _db.Collection(VendorsCollection).Document(vendorId).Collection(ItemsSubcollection);
        var snapshot = await itemsCol.GetSnapshotAsync(ct);
        return snapshot.Documents.Select(MapItem).Where(i => i is not null)!.ToList()!;
    }

    public async Task<ItemDto?> GetItemAsync(string vendorId, string id, CancellationToken ct = default)
    {
        var vdoc = await _db.Collection(VendorsCollection).Document(vendorId).GetSnapshotAsync(ct);
        if (vdoc.Exists)
        {
            var dict = vdoc.ToDictionary();
            if (dict.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> arr)
            {
                Dictionary<string, object?> priceMap = new();
                if (dict.TryGetValue("itemPrices", out var ip) && ip is Dictionary<string, object?> pm)
                    priceMap = pm;
                foreach (var el in arr)
                {
                    if (el is Dictionary<string, object?> idict)
                    {
                        var iid = idict.TryGetValue("id", out var vid) ? vid?.ToString() ?? string.Empty : string.Empty;
                        if (iid == id)
                        {
                            decimal price = 0m;
                            if (priceMap.TryGetValue(iid, out var ovr) && ovr is double dpr)
                                price = Convert.ToDecimal(dpr);
                            else if (idict.TryGetValue("price", out var p) && p is double dp)
                                price = Convert.ToDecimal(dp);
                            int stock = 0;
                            if (idict.TryGetValue("inStock", out var inst) && inst is bool b && b) stock = 1;
                            var name = idict.TryGetValue("name", out var vname) ? vname?.ToString() ?? string.Empty : string.Empty;
                            return new ItemDto { Id = iid, Name = name, Price = price, Stock = stock, Sku = iid };
                        }
                    }
                }
            }
        }
        var doc = _db.Collection(VendorsCollection).Document(vendorId).Collection(ItemsSubcollection).Document(id);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return null;
        return MapItem(snap)!;
    }

    public async Task<ItemDto> CreateItemAsync(string vendorId, ItemDto dto, CancellationToken ct = default)
    {
        var vref = _db.Collection(VendorsCollection).Document(vendorId);
        var vdoc = await vref.GetSnapshotAsync(ct);
        if (vdoc.Exists)
        {
            var dict = vdoc.ToDictionary();
            var arr = new List<Dictionary<string, object?>>();
            if (dict.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> existing)
            {
                foreach (var el in existing)
                    if (el is Dictionary<string, object?> d) arr.Add(new Dictionary<string, object?>(d));
            }
            if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
            var newItem = new Dictionary<string, object?>
            {
                ["id"] = dto.Id,
                ["name"] = dto.Name ?? string.Empty,
                ["price"] = Convert.ToDouble(dto.Price),
                ["category"] = "",
                ["inStock"] = dto.Stock > 0
            };
            arr.Add(newItem);

            // Update itemPrices map
            Dictionary<string, object?> priceMap = new();
            if (dict.TryGetValue("itemPrices", out var ip) && ip is Dictionary<string, object?> pm)
                priceMap = new Dictionary<string, object?>(pm);
            priceMap[dto.Id] = Convert.ToDouble(dto.Price);

            await vref.SetAsync(new Dictionary<string, object?>
            {
                ["items"] = arr,
                ["itemPrices"] = priceMap,
                ["lastUpdated"] = Timestamp.FromDateTime(DateTime.UtcNow)
            }, SetOptions.MergeAll, ct);
            return dto;
        }

        // Fallback: create in subcollection
        var data = new Dictionary<string, object?>
        {
            ["sku"] = dto.Sku,
            ["name"] = dto.Name,
            ["price"] = dto.Price,
            ["stock"] = dto.Stock
        };
        var itemsCol = vref.Collection(ItemsSubcollection);
        var docRef = await itemsCol.AddAsync(data, ct);
        dto.Id = docRef.Id;
        return dto;
    }

    public async Task<bool> UpdateItemAsync(string vendorId, string id, ItemDto dto, CancellationToken ct = default)
    {
        var vref = _db.Collection(VendorsCollection).Document(vendorId);
        var vdoc = await vref.GetSnapshotAsync(ct);
        if (vdoc.Exists)
        {
            var dict = vdoc.ToDictionary();
            var arr = new List<Dictionary<string, object?>>();
            bool found = false;
            if (dict.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> existing)
            {
                foreach (var el in existing)
                {
                    if (el is Dictionary<string, object?> d)
                    {
                        if (d.TryGetValue("id", out var vid) && vid?.ToString() == id)
                        {
                            d["name"] = dto.Name ?? string.Empty;
                            d["price"] = Convert.ToDouble(dto.Price);
                            d["inStock"] = dto.Stock > 0;
                            found = true;
                        }
                        arr.Add(new Dictionary<string, object?>(d));
                    }
                }
            }

            if (found)
            {
                // Update itemPrices map only if item existed
                Dictionary<string, object?> priceMap = new();
                if (dict.TryGetValue("itemPrices", out var ip) && ip is Dictionary<string, object?> pm)
                    priceMap = new Dictionary<string, object?>(pm);
                priceMap[id] = Convert.ToDouble(dto.Price);

                await vref.SetAsync(new Dictionary<string, object?>
                {
                    ["items"] = arr,
                    ["itemPrices"] = priceMap,
                    ["lastUpdated"] = Timestamp.FromDateTime(DateTime.UtcNow)
                }, SetOptions.MergeAll, ct);
                return true;
            }
            else
            {
                // Item not found in embedded array; let caller create it
                return false;
            }
        }

        // Fallback: update in subcollection
        var doc = vref.Collection(ItemsSubcollection).Document(id);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return false;
        var updates = new Dictionary<string, object?>
        {
            ["sku"] = dto.Sku,
            ["name"] = dto.Name,
            ["price"] = dto.Price,
            ["stock"] = dto.Stock
        };
        await doc.SetAsync(updates, SetOptions.MergeAll, cancellationToken: ct);
        return true;
    }

    public async Task<bool> DeleteItemAsync(string vendorId, string id, CancellationToken ct = default)
    {
        var vref = _db.Collection(VendorsCollection).Document(vendorId);
        var vdoc = await vref.GetSnapshotAsync(ct);
        if (vdoc.Exists)
        {
            var dict = vdoc.ToDictionary();
            var arr = new List<Dictionary<string, object?>>();
            bool removed = false;
            if (dict.TryGetValue("items", out var itemsObj) && itemsObj is IEnumerable<object> existing)
            {
                foreach (var el in existing)
                {
                    if (el is Dictionary<string, object?> d)
                    {
                        if (d.TryGetValue("id", out var vid) && vid?.ToString() == id)
                        {
                            removed = true; // skip adding to new array
                            continue;
                        }
                        arr.Add(new Dictionary<string, object?>(d));
                    }
                }
            }

            if (removed)
            {
                // Also update price map
                Dictionary<string, object?> priceMap = new();
                if (dict.TryGetValue("itemPrices", out var ip) && ip is Dictionary<string, object?> pm)
                {
                    priceMap = new Dictionary<string, object?>(pm);
                    priceMap.Remove(id);
                }

                await vref.SetAsync(new Dictionary<string, object?>
                {
                    ["items"] = arr,
                    ["itemPrices"] = priceMap,
                    ["lastUpdated"] = Timestamp.FromDateTime(DateTime.UtcNow)
                }, SetOptions.MergeAll, ct);
                return true;
            }
        }

        // Fallback: delete from subcollection
        var doc = vref.Collection(ItemsSubcollection).Document(id);
        var snap = await doc.GetSnapshotAsync(ct);
        if (!snap.Exists) return false;
        await doc.DeleteAsync(cancellationToken: ct);
        return true;
    }

    private static VendorDto? MapVendor(DocumentSnapshot d)
    {
        try
        {
            var dict = d.ToDictionary();
            return new VendorDto
            {
                Id = d.Id,
                Name = dict.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                ContactInfo = dict.TryGetValue("contactInfo", out var ci) ? ci?.ToString() : null,
                Status = dict.TryGetValue("status", out var st) ? st?.ToString() ?? "active" : "active",
                Budget = dict.TryGetValue("budget", out var bud)
                    ? (bud is double db ? Convert.ToDecimal(db) : bud is long lg ? Convert.ToDecimal(lg) : 0m)
                    : 0m,
                Reminder = dict.TryGetValue("reminder", out var rem) ? rem?.ToString() : null,
                ReminderEnabled = dict.TryGetValue("reminderEnabled", out var re) && re is bool b && b
            };
        }
        catch
        {
            return null;
        }
    }

    private static ItemDto? MapItem(DocumentSnapshot d)
    {
        try
        {
            var dict = d.ToDictionary();
            return new ItemDto
            {
                Id = d.Id,
                Sku = dict.TryGetValue("sku", out var sku) ? sku?.ToString() ?? string.Empty : string.Empty,
                Name = dict.TryGetValue("name", out var name) ? name?.ToString() ?? string.Empty : string.Empty,
                Price = dict.TryGetValue("price", out var price) && price is double dd ? Convert.ToDecimal(dd) : 0m,
                Stock = dict.TryGetValue("stock", out var stock) && stock is long ll ? (int)ll : 0,
            };
        }
        catch
        {
            return null;
        }
    }
}
