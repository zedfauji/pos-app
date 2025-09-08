using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Services;

public class TableRepository
{
    private readonly string _connString;
    private readonly string _localPath;
    private bool _useDatabase;
    private readonly string? _apiBaseUrl;
    private readonly HttpClient _http;
    public string SourceLabel { get; private set; } = "Local";

    public TableRepository()
    {
        // Read connection string from base appsettings.json layered with user override
        var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
            .Build();
        _connString = cfg["Db:Postgres:ConnectionString"] ?? string.Empty;
        _localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "tables.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_localPath)!);
        _useDatabase = !string.IsNullOrWhiteSpace(_connString);
        _apiBaseUrl = cfg["TablesApi:BaseUrl"];
        // Initialize HTTP client with logging
        var inner = new HttpClientHandler();
        inner.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        var logging = new HttpLoggingHandler(inner);
        _http = new HttpClient(logging);
    }

    public async Task<BillResult?> GetBillByIdAsync(Guid billId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return null;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), $"/bills/{billId}");
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;
            var data = await res.Content.ReadFromJsonAsync<BillResult>(cancellationToken: ct);
            return data;
        }
        catch { return null; }
    }

    public async Task EnsureSchemaAsync()
    {
        // Prefer API if configured and healthy
        if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var health = await _http.GetAsync(new Uri(new Uri(_apiBaseUrl!), "/health"), cts.Token);
                if (health.IsSuccessStatusCode)
                {
                    // seed (idempotent)
                    await _http.PostAsync(new Uri(new Uri(_apiBaseUrl!), "/tables/seed"), content: null);
                    SourceLabel = "API";
                    return;
                }
            }
            catch { /* fall through to DB/local */ }
        }

        if (_useDatabase)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();
                var sql = @"CREATE TABLE IF NOT EXISTS public.table_status (
                    label text PRIMARY KEY,
                    type text NOT NULL,
                    occupied boolean NOT NULL,
                    order_id text NULL,
                    start_time timestamptz NULL,
                    server text NULL,
                    updated_at timestamptz NOT NULL DEFAULT now()
                );
                ALTER TABLE public.table_status ADD COLUMN IF NOT EXISTS order_id text NULL;
                ALTER TABLE public.table_status ADD COLUMN IF NOT EXISTS start_time timestamptz NULL;
                ALTER TABLE public.table_status ADD COLUMN IF NOT EXISTS server text NULL;";
                await using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                SourceLabel = "DB";
            }
            catch
            {
                // fall back to local
                _useDatabase = false;
                SourceLabel = "Local";
            }
        }
        if (!_useDatabase)
        {
            if (!File.Exists(_localPath))
            {
                var seed = System.Text.Json.JsonSerializer.Serialize(new List<TableStatusDto>(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_localPath, seed);
            }
        }
    }

    public async Task<List<TableStatusDto>> GetAllAsync()
    {
        var list = new List<TableStatusDto>();
        // API first
        if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            try
            {
                var url = new Uri(new Uri(_apiBaseUrl!), "/tables");
                var apiList = await _http.GetFromJsonAsync<List<TableStatusDto>>(url);
                if (apiList != null) { SourceLabel = "API"; return apiList; }
            }
            catch { /* fallback below */ }
        }
        if (_useDatabase)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();
                var sql = "SELECT label, type, occupied, order_id, start_time, server FROM public.table_status ORDER BY label";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    list.Add(new TableStatusDto
                    {
                        Label = rdr.GetString(0),
                        Type = rdr.GetString(1),
                        Occupied = rdr.GetBoolean(2),
                        OrderId = rdr.IsDBNull(3) ? null : rdr.GetString(3),
                        StartTime = rdr.IsDBNull(4) ? null : rdr.GetFieldValue<DateTimeOffset>(4),
                        Server = rdr.IsDBNull(5) ? null : rdr.GetString(5)
                    });
                }
                SourceLabel = "DB";
                return list;
            }
            catch
            {
                _useDatabase = false;
                SourceLabel = "Local";
            }
        }
        // Local fallback
        if (File.Exists(_localPath))
        {
            var json = await File.ReadAllTextAsync(_localPath);
            var data = System.Text.Json.JsonSerializer.Deserialize<List<TableStatusDto>>(json) ?? new();
            return data;
        }
        return list;
    }

    public async Task UpsertAsync(TableStatusDto rec)
    {
        // API first
        if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            try
            {
                var url = new Uri(new Uri(_apiBaseUrl!), "/tables/upsert");
                var res = await _http.PostAsJsonAsync(url, rec);
                if (res.IsSuccessStatusCode) { SourceLabel = "API"; return; }
            }
            catch { /* fallback */ }
        }
        if (_useDatabase)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();
                const string sql = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server) VALUES(@l, @t, @o, @oid, @st, @srv)
                    ON CONFLICT (label) DO UPDATE SET type = EXCLUDED.type, occupied = EXCLUDED.occupied, order_id = EXCLUDED.order_id, start_time = EXCLUDED.start_time, server = EXCLUDED.server, updated_at = now();";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@l", rec.Label);
                cmd.Parameters.AddWithValue("@t", rec.Type);
                cmd.Parameters.AddWithValue("@o", rec.Occupied);
                cmd.Parameters.AddWithValue("@oid", (object?)rec.OrderId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@st", (object?)rec.StartTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@srv", (object?)rec.Server ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                return;
            }
            catch
            {
                _useDatabase = false;
            }
        }
        // Local fallback
        var list = await GetAllAsync();
        var idx = list.FindIndex(x => string.Equals(x.Label, rec.Label, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) list[idx] = rec; else list.Add(rec);
        var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_localPath, json);
    }

    public async Task UpsertManyAsync(IEnumerable<TableStatusDto> recs)
    {
        // API first (simple loop)
        if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            try
            {
                foreach (var r in recs)
                {
                    var url = new Uri(new Uri(_apiBaseUrl!), "/tables/upsert");
                    await _http.PostAsJsonAsync(url, r);
                }
                SourceLabel = "API";
                return;
            }
            catch { /* fallback */ }
        }
        if (_useDatabase)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connString);
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();
                const string sql = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server) VALUES(@l, @t, @o, @oid, @st, @srv)
                    ON CONFLICT (label) DO UPDATE SET type = EXCLUDED.type, occupied = EXCLUDED.occupied, order_id = EXCLUDED.order_id, start_time = EXCLUDED.start_time, server = EXCLUDED.server, updated_at = now();";
                foreach (var rec in recs)
                {
                    await using var cmd = new NpgsqlCommand(sql, conn, tx);
                    cmd.Parameters.AddWithValue("@l", rec.Label);
                    cmd.Parameters.AddWithValue("@t", rec.Type);
                    cmd.Parameters.AddWithValue("@o", rec.Occupied);
                    cmd.Parameters.AddWithValue("@oid", (object?)rec.OrderId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@st", (object?)rec.StartTime ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@srv", (object?)rec.Server ?? DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();
                return;
            }
            catch
            {
                _useDatabase = false;
            }
        }
        // Local fallback
        var list = await GetAllAsync();
        foreach (var rec in recs)
        {
            var idx = list.FindIndex(x => string.Equals(x.Label, rec.Label, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) list[idx] = rec; else list.Add(rec);
        }
        var json = System.Text.Json.JsonSerializer.Serialize(list, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_localPath, json);
    }

    public DateTime GetLocalLastWriteUtc()
        => File.Exists(_localPath) ? File.GetLastWriteTimeUtc(_localPath) : DateTime.MinValue;

    // --- Sessions & Billing --- (using shared DTOs)

    public async Task<(bool ok, string message)> StartSessionAsync(string label, string serverId, string serverName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            // Fallback: just mark occupied in local/DB
            await UpsertAsync(new TableStatusDto { Label = label, Type = "billiard", Occupied = true, StartTime = DateTimeOffset.UtcNow, Server = serverName });
            return (true, "offline-mode");
        }
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), $"/tables/{Uri.EscapeDataString(label)}/start");
            var res = await _http.PostAsJsonAsync(url, new StartSessionRequest(serverId, serverName), ct);
            if (res.IsSuccessStatusCode) { SourceLabel = "API"; return (true, ""); }
            var body = await res.Content.ReadAsStringAsync(ct);
            return (false, body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool ok, BillResult? bill, string message)> StopSessionAsync(string label, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            // Fallback: mark available locally
            await UpsertAsync(new TableStatusDto { Label = label, Type = "billiard", Occupied = false, StartTime = null, Server = null });
            return (true, null, "offline-mode");
        }
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), $"/tables/{Uri.EscapeDataString(label)}/stop");
            var res = await _http.PostAsync(url, content: null, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                return (false, null, body);
            }
            var bill = await res.Content.ReadFromJsonAsync<BillResult>(cancellationToken: ct);
            return (true, bill, "");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<List<BillResult>> GetBillsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, string? table = null, string? server = null, CancellationToken ct = default)
    {
        var list = new List<BillResult>();
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return list;
        try
        {
            var baseUri = new Uri(new Uri(_apiBaseUrl!), "/bills");
            var query = new List<string>();
            if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.UtcDateTime.ToString("o"))}");
            if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.UtcDateTime.ToString("o"))}");
            if (!string.IsNullOrWhiteSpace(table)) query.Add($"table={Uri.EscapeDataString(table)}");
            if (!string.IsNullOrWhiteSpace(server)) query.Add($"server={Uri.EscapeDataString(server)}");
            var url = new Uri(baseUri + (query.Count > 0 ? ("?" + string.Join("&", query)) : string.Empty));
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return list;
            var data = await res.Content.ReadFromJsonAsync<List<BillResult>>(cancellationToken: ct) ?? new();
            return data;
        }
        catch (Exception ex)
        {
            Log.Error("GetBillsAsync failed", ex);
            return list;
        }
    }

    public async Task<List<ItemLine>> GetSessionItemsAsync(string label, CancellationToken ct = default)
    {
        var items = new List<ItemLine>();
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return items;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), $"/tables/{Uri.EscapeDataString(label)}/items");
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return items;
            var data = await res.Content.ReadFromJsonAsync<List<ItemLine>>(cancellationToken: ct) ?? new();
            return data;
        }
        catch { return items; }
    }

    public async Task<bool> ReplaceSessionItemsAsync(string label, List<ItemLine> items, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return false;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), $"/tables/{Uri.EscapeDataString(label)}/items");
            var res = await _http.PostAsJsonAsync(url, items ?? new(), ct);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // Recovery: free a table even if there is no active session row
    public async Task<bool> ForceFreeAsync(string label, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return false;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), $"/tables/{Uri.EscapeDataString(label)}/force-free");
            var res = await _http.PostAsync(url, content: null, ct);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // Settings: rate per minute
    public async Task<decimal?> GetRatePerMinuteAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return null;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), "/settings/rate");
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return null;
            var doc = await res.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: ct);
            if (doc != null && doc.TryGetPropertyValue("ratePerMinute", out var val) && val != null)
            {
                if (decimal.TryParse(val.ToString(), out var d)) return d;
            }
            return null;
        }
        catch { return null; }
    }

    public async Task<bool> SetRatePerMinuteAsync(decimal rate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return false;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), "/settings/rate");
            var content = System.Net.Http.Json.JsonContent.Create(rate);
            var res = await _http.PutAsync(url, content, ct);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // --- New: Move session to another table ---
    public async Task<(bool ok, string message)> MoveSessionAsync(string fromLabel, string toLabel, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return (false, "API base URL not configured");
        try
        {
            var baseUri = new Uri(new Uri(_apiBaseUrl!), $"/tables/{Uri.EscapeDataString(fromLabel)}/move");
            var url = new Uri(baseUri + $"?to={Uri.EscapeDataString(toLabel)}");
            var res = await _http.PostAsync(url, content: null, ct);
            if (res.IsSuccessStatusCode) return (true, "");
            var body = await res.Content.ReadAsStringAsync(ct);
            return (false, body);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // --- New: List active sessions overview ---
    public async Task<List<MagiDesk.Shared.DTOs.Tables.SessionOverview>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var list = new List<MagiDesk.Shared.DTOs.Tables.SessionOverview>();
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return list;
        try
        {
            var url = new Uri(new Uri(_apiBaseUrl!), "/sessions/active");
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return list;
            var data = await res.Content.ReadFromJsonAsync<List<MagiDesk.Shared.DTOs.Tables.SessionOverview>>(cancellationToken: ct) ?? new();
            return data;
        }
        catch { return list; }
    }

    public async Task<List<MagiDesk.Shared.DTOs.Tables.SessionOverview>> GetSessionsAsync(int limit = 100, DateTimeOffset? from = null, DateTimeOffset? to = null, string? table = null, string? server = null, CancellationToken ct = default)
    {
        var list = new List<MagiDesk.Shared.DTOs.Tables.SessionOverview>();
        if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return list;
        try
        {
            var baseUri = new Uri(new Uri(_apiBaseUrl!), "/sessions");
            var qp = new List<string>();
            qp.Add($"limit={limit}");
            if (from.HasValue) qp.Add($"from={Uri.EscapeDataString(from.Value.UtcDateTime.ToString("o"))}");
            if (to.HasValue) qp.Add($"to={Uri.EscapeDataString(to.Value.UtcDateTime.ToString("o"))}");
            if (!string.IsNullOrWhiteSpace(table)) qp.Add($"table={Uri.EscapeDataString(table)}");
            if (!string.IsNullOrWhiteSpace(server)) qp.Add($"server={Uri.EscapeDataString(server)}");
            var url = new Uri(baseUri + (qp.Count > 0 ? ("?" + string.Join("&", qp)) : string.Empty));
            var res = await _http.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return list;
            var data = await res.Content.ReadFromJsonAsync<List<MagiDesk.Shared.DTOs.Tables.SessionOverview>>(cancellationToken: ct) ?? new();
            return data;
        }
        catch { return list; }
    }

    // --- Helper: get available tables (not occupied) ---
    public async Task<List<string>> GetAvailableTableLabelsAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync();
        var labels = new List<string>();
        foreach (var t in all)
        {
            if (!t.Occupied) labels.Add(t.Label);
        }
        return labels;
    }
}
