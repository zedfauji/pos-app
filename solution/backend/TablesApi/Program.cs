using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using MagiDesk.Shared.DTOs.Tables;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (open for now; tighten later)
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Resolve Postgres connection string with Cloud Run socket preference
string? connString = null;
string? socketConnFromConfig = builder.Configuration.GetConnectionString("PostgresSocket")
    ?? builder.Configuration["Db:Postgres:SocketConnectionString"];
string? ipConnFromConfig = builder.Configuration.GetConnectionString("Postgres")
    ?? builder.Configuration["Db:Postgres:ConnectionString"];

// If running on Cloud Run, prefer Unix domain socket if provided
bool isCloudRun = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("K_SERVICE"));
if (isCloudRun && !string.IsNullOrWhiteSpace(socketConnFromConfig))
{
    connString = socketConnFromConfig;
}
else
{
    connString = Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING")
        ?? ipConnFromConfig
        ?? socketConnFromConfig; // fallback to socket if explicitly configured even outside Cloud Run
}

// If still missing but Cloud SQL instance name is provided, synthesize a socket connection string
if (string.IsNullOrWhiteSpace(connString))
{
    var instance = Environment.GetEnvironmentVariable("CLOUDSQL_INSTANCE")
        ?? builder.Configuration["Db:Postgres:InstanceConnectionName"];
    if (!string.IsNullOrWhiteSpace(instance))
    {
        // NOTE: SSL is not used for Unix sockets
        connString = $"Host=/cloudsql/{instance};Port=5432;Username=posapp;Password=Campus_66;Database=postgres;SSL Mode=Disable";
    }
}

// Optional: per-minute rate for table time cost
decimal defaultRatePerMinute = 0m;
var rateStr = builder.Configuration["Tables:RatePerMinute"] ?? Environment.GetEnvironmentVariable("TABLESAPI__RATE_PER_MINUTE");
if (decimal.TryParse(rateStr, out var parsed)) defaultRatePerMinute = parsed;

if (string.IsNullOrWhiteSpace(connString))
{
    app.Logger.LogWarning("No Postgres connection string provided. Set Db:Postgres:ConnectionString or TABLESAPI__CONNSTRING.");
}

app.MapGet("/health", async () =>
{
    if (string.IsNullOrWhiteSpace(connString)) return Results.Problem("No connection string", statusCode: 500);
    try
    {
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await EnsureSchemaAsync(conn);
        return Results.Ok(new { status = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Settings: rate per minute for table sessions
app.MapGet("/settings/rate", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = "SELECT value FROM public.app_settings WHERE key = 'Tables.RatePerMinute'";
    await using var cmd = new NpgsqlCommand(sql, conn);
    var obj = await cmd.ExecuteScalarAsync();
    decimal rate = 0m;
    if (obj is string s && decimal.TryParse(s, out var r)) rate = r;
    return Results.Ok(new { ratePerMinute = rate });
});

app.MapPut("/settings/rate", async (HttpContext ctx) =>
{
    try
    {
        decimal? bodyRate = await ctx.Request.ReadFromJsonAsync<decimal?>();
        if (!bodyRate.HasValue) return Results.BadRequest(new { message = "Invalid body; expected number" });
        var rate = bodyRate.Value;
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await EnsureSchemaAsync(conn);
        const string up = @"INSERT INTO public.app_settings(key, value) VALUES('Tables.RatePerMinute', @v)
                           ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value";
        await using var cmd = new NpgsqlCommand(up, conn);
        cmd.Parameters.AddWithValue("@v", rate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync();
        return Results.Ok(new { ratePerMinute = rate });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// Enforce session timers (auto-stop) based on SettingsApi app settings
// Env var SETTINGSAPI_BASEURL must point to SettingsApi base URL
app.MapPost("/sessions/enforce", async ([FromQuery] string? host) =>
{
    string? settingsUrl = Environment.GetEnvironmentVariable("SETTINGSAPI_BASEURL")
        ?? builder.Configuration["SettingsApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(settingsUrl))
        return Results.Problem("SETTINGSAPI_BASEURL not configured", statusCode: 500);

    using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
    { BaseAddress = new Uri(settingsUrl.TrimEnd('/') + "/") };
    var appEndpoint = string.IsNullOrWhiteSpace(host) ? "api/settings/app" : $"api/settings/app?host={Uri.EscapeDataString(host)}";
    var appSettings = await http.GetFromJsonAsync<System.Text.Json.Nodes.JsonObject>(appEndpoint);
    int autoStop = 0;
    try
    {
        if (appSettings != null && appSettings.TryGetPropertyValue("extras", out var extras) && extras is System.Text.Json.Nodes.JsonObject obj)
        {
            if (obj.TryGetPropertyValue("tables.session.autoStopMinutes", out var val) && val != null)
            {
                int.TryParse(val.ToString(), out autoStop);
            }
        }
    }
    catch { }
    if (autoStop <= 0) return Results.Ok(new { enforced = 0, autoStop });

    int enforced = 0;
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    // Get active sessions
    const string findActives = @"SELECT session_id, table_label, server_id, server_name, start_time FROM public.table_sessions WHERE status='active'";
    var sessions = new List<(Guid sid, string table, string serverId, string serverName, DateTime start)>();
    await using (var cmd = new NpgsqlCommand(findActives, conn))
    await using (var rdr = await cmd.ExecuteReaderAsync())
    {
        while (await rdr.ReadAsync())
        {
            sessions.Add((rdr.GetFieldValue<Guid>(0), rdr.GetString(1), rdr.GetString(2), rdr.GetString(3), rdr.GetFieldValue<DateTime>(4)));
        }
    }
    foreach (var s in sessions)
    {
        var minutes = (int)Math.Max(0, (DateTime.UtcNow - s.start).TotalMinutes);
        if (minutes < autoStop) continue;
        // close session and create bill (reuse logic similar to /tables/{label}/stop)
        const string closeSql = "UPDATE public.table_sessions SET end_time = @end, status = 'closed' WHERE session_id = @sid";
        await using (var close = new NpgsqlCommand(closeSql, conn))
        {
            close.Parameters.AddWithValue("@end", DateTime.UtcNow);
            close.Parameters.AddWithValue("@sid", s.sid);
            await close.ExecuteNonQueryAsync();
        }
        // items
        var items = new List<ItemLine>();
        const string itemsSql = @"SELECT items FROM public.table_sessions WHERE session_id = @sid";
        await using (var gi = new NpgsqlCommand(itemsSql, conn))
        {
            gi.Parameters.AddWithValue("@sid", s.sid);
            await using var rdr2 = await gi.ExecuteReaderAsync();
            if (await rdr2.ReadAsync() && !rdr2.IsDBNull(0))
            {
                var json = rdr2.GetString(0);
                try { items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); } catch { items = new(); }
            }
        }
        decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
        decimal itemsCost = items.Sum(i => i.price * i.quantity);
        decimal timeCost = ratePerMinute * minutes;
        decimal total = timeCost + itemsCost;
        var billId = Guid.NewGuid();
        const string billSql = @"INSERT INTO public.bills(bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount)
                                VALUES(@bid, @label, @srvId, @srvName, @start, @end, @mins, @items, @timeCost, @itemsCost, @total)";
        await using (var bill = new NpgsqlCommand(billSql, conn))
        {
            bill.Parameters.AddWithValue("@bid", billId);
            bill.Parameters.AddWithValue("@label", s.table);
            bill.Parameters.AddWithValue("@srvId", s.serverId);
            bill.Parameters.AddWithValue("@srvName", s.serverName);
            bill.Parameters.AddWithValue("@start", s.start);
            bill.Parameters.AddWithValue("@end", DateTime.UtcNow);
            bill.Parameters.AddWithValue("@mins", minutes);
            bill.Parameters.Add("@items", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(items);
            bill.Parameters.AddWithValue("@timeCost", timeCost);
            bill.Parameters.AddWithValue("@itemsCost", itemsCost);
            bill.Parameters.AddWithValue("@total", total);
            await bill.ExecuteNonQueryAsync();
        }
        // free table
        const string freeSql = @"UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL, updated_at = now() WHERE label = @l";
        await using (var free = new NpgsqlCommand(freeSql, conn))
        {
            free.Parameters.AddWithValue("@l", s.table);
            await free.ExecuteNonQueryAsync();
        }
        enforced++;
    }
    return Results.Ok(new { enforced, autoStop });
});

// Move an active session from one table to another
app.MapPost("/tables/{fromLabel}/move", async (string fromLabel, string to, HttpContext ctx) =>
{
    if (string.IsNullOrWhiteSpace(to)) return Results.BadRequest(new { message = "Target table is required" });
    var toLabel = to;
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);

    // Find active session on source
    const string findSql = @"SELECT session_id, server_id, server_name, start_time, billing_id FROM public.table_sessions
                             WHERE table_label = @label AND status = 'active' ORDER BY start_time DESC LIMIT 1";
    Guid sessionId; string serverId; string serverName; DateTime startTime; Guid? billingId = null;
    await using (var find = new NpgsqlCommand(findSql, conn))
    {
        find.Parameters.AddWithValue("@label", fromLabel);
        await using var rdr = await find.ExecuteReaderAsync();
        if (!await rdr.ReadAsync()) return Results.NotFound(new { message = "No active session on source table." });
        sessionId = rdr.GetFieldValue<Guid>(0);
        serverId = rdr.GetString(1);
        serverName = rdr.GetString(2);
        startTime = rdr.GetFieldValue<DateTime>(3);
        if (!rdr.IsDBNull(4)) billingId = rdr.GetFieldValue<Guid>(4);
    }

    // Ensure destination is not occupied
    const string checkDest = @"SELECT occupied FROM public.table_status WHERE label = @l";
    await using (var chk = new NpgsqlCommand(checkDest, conn))
    {
        chk.Parameters.AddWithValue("@l", toLabel);
        var obj = await chk.ExecuteScalarAsync();
        if (obj is bool occ && occ) return Results.Conflict(new { message = "Destination table is occupied." });
    }

    await using var tx = await conn.BeginTransactionAsync();
    // Audit record
    const string auditSql = "INSERT INTO public.table_session_moves(session_id, from_label, to_label, moved_at) VALUES(@sid, @from, @to, now())";
    await using (var aud = new NpgsqlCommand(auditSql, conn, tx))
    {
        aud.Parameters.AddWithValue("@sid", sessionId);
        aud.Parameters.AddWithValue("@from", fromLabel);
        aud.Parameters.AddWithValue("@to", toLabel);
        await aud.ExecuteNonQueryAsync();
    }
    // Update session to new table label
    const string updSession = "UPDATE public.table_sessions SET table_label = @to, status = 'active' WHERE session_id = @sid";
    await using (var us = new NpgsqlCommand(updSession, conn, tx))
    {
        us.Parameters.AddWithValue("@to", toLabel);
        us.Parameters.AddWithValue("@sid", sessionId);
        await us.ExecuteNonQueryAsync();
    }
    // Free source table
    const string freeSrc = @"UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL, updated_at = now() WHERE label = @l";
    await using (var fs = new NpgsqlCommand(freeSrc, conn, tx))
    {
        fs.Parameters.AddWithValue("@l", fromLabel);
        await fs.ExecuteNonQueryAsync();
    }
    // Occupy destination table with original start and server
    const string occDest = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server)
                             VALUES(@l, 'billiard', true, NULL, @st, @srv)
                             ON CONFLICT (label) DO UPDATE SET occupied = EXCLUDED.occupied, start_time = EXCLUDED.start_time, server = EXCLUDED.server, updated_at = now();";
    await using (var od = new NpgsqlCommand(occDest, conn, tx))
    {
        od.Parameters.AddWithValue("@l", toLabel);
        od.Parameters.AddWithValue("@st", startTime);
        od.Parameters.AddWithValue("@srv", (object?)serverName ?? DBNull.Value);
        await od.ExecuteNonQueryAsync();
    }
    await tx.CommitAsync();

    return Results.Ok(new { session_id = sessionId, billing_id = billingId, from = fromLabel, to = toLabel });
});

// List active sessions with computed fields
app.MapGet("/sessions/active", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT s.session_id, s.billing_id, s.table_label, s.server_name, s.start_time, s.status, s.items
                         FROM public.table_sessions s
                         WHERE (s.status ILIKE 'active' OR (s.status IS NULL AND s.end_time IS NULL))
                         ORDER BY s.start_time";
    var list = new List<object>();
    var sessions = new List<(Guid sid, Guid? bid, string table, string server, DateTime start, string status, string itemsJson)>();
    await using (var cmd = new NpgsqlCommand(sql, conn))
    await using (var rdr = await cmd.ExecuteReaderAsync())
    {
        while (await rdr.ReadAsync())
        {
            sessions.Add((rdr.GetFieldValue<Guid>(0), rdr.IsDBNull(1) ? null : rdr.GetFieldValue<Guid>(1), rdr.GetString(2), rdr.GetString(3), rdr.GetFieldValue<DateTime>(4), rdr.GetString(5), rdr.IsDBNull(6) ? "[]" : rdr.GetString(6)));
        }
    }
    decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
    foreach (var s in sessions)
    {
        List<ItemLine> items;
        try { items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(s.itemsJson) ?? new(); } catch { items = new(); }
        var minutes = (int)Math.Max(0, (DateTime.UtcNow - s.start).TotalMinutes);
        var itemsCost = items.Sum(i => i.price * i.quantity);
        var timeCost = ratePerMinute * minutes;
        var total = timeCost + itemsCost;
        list.Add(new
        {
            sessionId = s.sid,
            billingId = s.bid,
            tableId = s.table,
            serverName = s.server,
            startTime = s.start,
            status = s.status,
            itemsCount = items.Count,
            total
        });
    }
    return Results.Ok(list);
});

// List recent sessions (active and closed) with computed totals
app.MapGet("/sessions", async ([FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? table, [FromQuery] string? server, [FromQuery] int limit) =>
{
    if (limit <= 0 || limit > 500) limit = 100;
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    var where = new List<string>();
    var cmd = new NpgsqlCommand();
    cmd.Connection = conn;
    if (!string.IsNullOrWhiteSpace(table)) { where.Add("s.table_label = @table"); cmd.Parameters.AddWithValue("@table", table); }
    if (!string.IsNullOrWhiteSpace(server)) { where.Add("s.server_name ILIKE @server"); cmd.Parameters.AddWithValue("@server", "%" + server + "%"); }
    if (DateTime.TryParse(from, out var fromDt)) { where.Add("(s.end_time IS NULL OR s.end_time >= @from)"); cmd.Parameters.AddWithValue("@from", fromDt.ToUniversalTime()); }
    if (DateTime.TryParse(to, out var toDt)) { where.Add("s.start_time <= @to"); cmd.Parameters.AddWithValue("@to", toDt.ToUniversalTime()); }
    var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
    cmd.CommandText = $@"SELECT s.session_id, s.billing_id, s.table_label, s.server_name, s.start_time, s.end_time, s.status, s.items
                         FROM public.table_sessions s{whereSql}
                         ORDER BY s.start_time DESC
                         LIMIT @limit";
    cmd.Parameters.AddWithValue("@limit", limit);
    var list = new List<object>();
    var rows = new List<(Guid sid, Guid? bid, string table, string server, DateTime start, DateTime? end, string? status, string itemsJson)>();
    await using (var rdr = await cmd.ExecuteReaderAsync())
    {
        while (await rdr.ReadAsync())
        {
            rows.Add((
                rdr.GetFieldValue<Guid>(0),
                rdr.IsDBNull(1) ? null : rdr.GetFieldValue<Guid>(1),
                rdr.GetString(2),
                rdr.IsDBNull(3) ? string.Empty : rdr.GetString(3),
                rdr.GetFieldValue<DateTime>(4),
                rdr.IsDBNull(5) ? (DateTime?)null : rdr.GetFieldValue<DateTime>(5),
                rdr.IsDBNull(6) ? null : rdr.GetString(6),
                rdr.IsDBNull(7) ? "[]" : rdr.GetString(7)
            ));
        }
    }
    decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
    foreach (var s in rows)
    {
        List<ItemLine> items;
        try { items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(s.itemsJson) ?? new(); } catch { items = new(); }
        var end = s.end ?? DateTime.UtcNow;
        var minutes = (int)Math.Max(0, (end - s.start).TotalMinutes);
        var itemsCost = items.Sum(i => i.price * i.quantity);
        var timeCost = ratePerMinute * minutes;
        var total = timeCost + itemsCost;
        list.Add(new
        {
            sessionId = s.sid,
            billingId = s.bid,
            tableId = s.table,
            serverName = s.server,
            startTime = s.start,
            status = string.IsNullOrWhiteSpace(s.status) ? (s.end.HasValue ? "closed" : "active") : s.status,
            itemsCount = items.Count,
            total
        });
    }
    return Results.Ok(list);
});

// Debug: inspect most recent raw session rows
app.MapGet("/debug/sessions", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT session_id, table_label, server_name, start_time, end_time, status, billing_id
                         FROM public.table_sessions ORDER BY start_time DESC LIMIT 20";
    var outList = new List<object>();
    await using (var cmd = new NpgsqlCommand(sql, conn))
    await using (var rdr = await cmd.ExecuteReaderAsync())
    {
        while (await rdr.ReadAsync())
        {
            outList.Add(new
            {
                sessionId = rdr.GetFieldValue<Guid>(0),
                table = rdr.GetString(1),
                server = rdr.IsDBNull(2) ? null : rdr.GetString(2),
                start = rdr.GetFieldValue<DateTime>(3),
                end = rdr.IsDBNull(4) ? (DateTime?)null : rdr.GetFieldValue<DateTime>(4),
                status = rdr.IsDBNull(5) ? null : rdr.GetString(5),
                billingId = rdr.IsDBNull(6) ? (Guid?)null : rdr.GetFieldValue<Guid>(6)
            });
        }
    }
    return Results.Ok(outList);
});

// Force free a table even if there is no active session (recovery path)
app.MapPost("/tables/{label}/force-free", async (string label) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    // Close any lingering 'active' sessions just in case
    const string closeAll = "UPDATE public.table_sessions SET end_time = now(), status = 'closed' WHERE table_label = @label AND status = 'active'";
    await using (var cmd = new NpgsqlCommand(closeAll, conn))
    {
        cmd.Parameters.AddWithValue("@label", label);
        await cmd.ExecuteNonQueryAsync();
    }
    // Free the table
    const string freeSql2 = @"UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL, updated_at = now() WHERE label = @l";
    await using (var free = new NpgsqlCommand(freeSql2, conn))
    {
        free.Parameters.AddWithValue("@l", label);
        await free.ExecuteNonQueryAsync();
    }
    return Results.Ok(new { freed = true });
});

// Detailed DB diagnostics endpoint
app.MapGet("/debug/db", async () =>
{
    var diag = new Dictionary<string, object?>
    {
        ["ok"] = false,
        ["connStringProvided"] = !string.IsNullOrWhiteSpace(connString),
        ["connStringPreview"] = string.IsNullOrWhiteSpace(connString) ? null : (connString!.Length > 64 ? connString.Substring(0, 64) + "..." : connString),
        ["env_TABLESAPI__CONNSTRING"] = Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING") != null,
        ["region"] = builder.Configuration["K_REVISION"] ?? Environment.GetEnvironmentVariable("K_SERVICE") ?? "unknown",
    };
    try
    {
        await using var conn = new NpgsqlConnection(connString);
        var started = DateTimeOffset.UtcNow;
        await conn.OpenAsync();
        var opened = DateTimeOffset.UtcNow;
        await EnsureSchemaAsync(conn);
        await using (var cmd = new NpgsqlCommand("SELECT now()", conn))
        {
            var now = await cmd.ExecuteScalarAsync();
            diag["selectNow"] = now?.ToString();
        }
        await using (var cmd2 = new NpgsqlCommand("SELECT COUNT(1) FROM information_schema.tables", conn))
        {
            var cnt = await cmd2.ExecuteScalarAsync();
            diag["tableCount"] = cnt;
        }
        diag["openMs"] = (opened - started).TotalMilliseconds;
        diag["ok"] = true;
        return Results.Ok(diag);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "DB diagnostics failed");
        diag["errorType"] = ex.GetType().FullName;
        diag["error"] = ex.Message;
        if (ex.InnerException != null)
        {
            diag["innerType"] = ex.InnerException.GetType().FullName;
            diag["innerError"] = ex.InnerException.Message;
        }
        return Results.Problem(title: "DB diagnostics failed", detail: System.Text.Json.JsonSerializer.Serialize(diag), statusCode: 500);
    }
});

// Start a table session
app.MapPost("/tables/{label}/start", async (string label, StartSessionRequest req) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    // Ensure no active session exists for this table
    const string hasActiveSql = "SELECT COUNT(1) FROM public.table_sessions WHERE table_label = @label AND status = 'active'";
    await using (var chk = new NpgsqlCommand(hasActiveSql, conn))
    {
        chk.Parameters.AddWithValue("@label", label);
        var count = (long)(await chk.ExecuteScalarAsync() ?? 0L);
        if (count > 0) return Results.Conflict(new { message = "Active session already exists for this table." });
    }

    var sessionId = Guid.NewGuid();
    var billingId = Guid.NewGuid();
    var now = DateTime.UtcNow;
    const string insertSql = @"INSERT INTO public.table_sessions(session_id, table_label, server_id, server_name, start_time, status, billing_id)
                               VALUES(@sid, @label, @srvId, @srvName, @start, 'active', @bid)";
    await using (var ins = new NpgsqlCommand(insertSql, conn))
    {
        ins.Parameters.AddWithValue("@sid", sessionId);
        ins.Parameters.AddWithValue("@label", label);
        ins.Parameters.AddWithValue("@srvId", req.ServerId);
        ins.Parameters.AddWithValue("@srvName", req.ServerName);
        ins.Parameters.AddWithValue("@start", now);
        ins.Parameters.AddWithValue("@bid", billingId);
        await ins.ExecuteNonQueryAsync();
    }

    // Update table_status to occupied
    const string upsertTable = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server)
                                 VALUES(@l, 'billiard', true, NULL, @st, @srv)
                                 ON CONFLICT (label) DO UPDATE SET occupied = EXCLUDED.occupied, start_time = EXCLUDED.start_time, server = EXCLUDED.server, updated_at = now();";
    await using (var upd = new NpgsqlCommand(upsertTable, conn))
    {
        upd.Parameters.AddWithValue("@l", label);
        upd.Parameters.AddWithValue("@st", now);
        upd.Parameters.AddWithValue("@srv", (object?)req.ServerName ?? DBNull.Value);
        await upd.ExecuteNonQueryAsync();
    }

    return Results.Ok(new { session_id = sessionId, billing_id = billingId, start_time = now });
});

// Stop a table session and generate bill
app.MapPost("/tables/{label}/stop", async (string label) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);

    // Find active session
    const string findSql = @"SELECT session_id, server_id, server_name, start_time, billing_id FROM public.table_sessions
                             WHERE table_label = @label AND status = 'active' ORDER BY start_time DESC LIMIT 1";
    Guid sessionId; Guid? billingGuid = null;
    string serverId;
    string serverName;
    DateTime startTime;
    await using (var find = new NpgsqlCommand(findSql, conn))
    {
        find.Parameters.AddWithValue("@label", label);
        await using var rdr = await find.ExecuteReaderAsync();
        if (!await rdr.ReadAsync()) return Results.NotFound(new { message = "No active session for this table." });
        sessionId = rdr.GetFieldValue<Guid>(0);
        serverId = rdr.GetString(1);
        serverName = rdr.GetString(2);
        startTime = rdr.GetFieldValue<DateTime>(3);
        if (!rdr.IsDBNull(4)) billingGuid = rdr.GetFieldValue<Guid>(4);
    }

    var end = DateTime.UtcNow;
    var minutes = (int)Math.Max(0, (end - startTime).TotalMinutes);

    // Close session
    const string closeSql = "UPDATE public.table_sessions SET end_time = @end, status = 'closed' WHERE session_id = @sid";
    await using (var close = new NpgsqlCommand(closeSql, conn))
    {
        close.Parameters.AddWithValue("@end", end);
        close.Parameters.AddWithValue("@sid", sessionId);
        await close.ExecuteNonQueryAsync();
    }

    // Read items from active session
    var items = new List<ItemLine>();
    const string itemsSql = @"SELECT items FROM public.table_sessions WHERE session_id = @sid";
    await using (var getItems = new NpgsqlCommand(itemsSql, conn))
    {
        getItems.Parameters.AddWithValue("@sid", sessionId);
        await using (var rdr = await getItems.ExecuteReaderAsync())
        {
            if (await rdr.ReadAsync() && !rdr.IsDBNull(0))
            {
                var json = rdr.GetString(0);
                try { items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); } catch { items = new(); }
            }
        }
    }

    // Compute costs (fetch dynamic rate if exists)
    decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
    decimal itemsCost = items.Sum(i => i.price * i.quantity);
    decimal timeCost = ratePerMinute * minutes;
    decimal total = timeCost + itemsCost;

    // Create bill
    var billId = Guid.NewGuid();
    const string billSql = @"INSERT INTO public.bills(bill_id, billing_id, session_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount, status, is_settled, payment_state)
                            VALUES(@bid, @billingId, @sid, @label, @srvId, @srvName, @start, @end, @mins, @items, @timeCost, @itemsCost, @total, 'awaiting_payment', false, 'not-paid')";
    await using (var bill = new NpgsqlCommand(billSql, conn))
    {
        bill.Parameters.AddWithValue("@bid", billId);
        bill.Parameters.AddWithValue("@billingId", (object?)(billingGuid?.ToString()) ?? DBNull.Value);
        bill.Parameters.AddWithValue("@sid", sessionId.ToString());
        bill.Parameters.AddWithValue("@label", label);
        bill.Parameters.AddWithValue("@srvId", serverId);
        bill.Parameters.AddWithValue("@srvName", serverName);
        bill.Parameters.AddWithValue("@start", startTime);
        bill.Parameters.AddWithValue("@end", end);
        bill.Parameters.AddWithValue("@mins", minutes);
        bill.Parameters.Add("@items", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(items);
        bill.Parameters.AddWithValue("@timeCost", timeCost);
        bill.Parameters.AddWithValue("@itemsCost", itemsCost);
        bill.Parameters.AddWithValue("@total", total);
        await bill.ExecuteNonQueryAsync();
    }

    // Mark table available
    const string freeSql = @"UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL, updated_at = now() WHERE label = @l";
    await using (var free = new NpgsqlCommand(freeSql, conn))
    {
        free.Parameters.AddWithValue("@l", label);
        await free.ExecuteNonQueryAsync();
    }

    return Results.Ok(new BillResult
    {
        BillId = billId,
        // Note: client can correlate using returned BillId; billing_id is retrievable via GET /bills/{billId}
        TableLabel = label,
        ServerId = serverId,
        ServerName = serverName,
        StartTime = startTime,
        EndTime = end,
        TotalTimeMinutes = minutes,
        Items = items,
        TimeCost = timeCost,
        ItemsCost = itemsCost,
        TotalAmount = total
    });
});

app.MapPost("/tables/reset", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    await using (var del = new NpgsqlCommand("DELETE FROM public.table_status", conn))
    {
        await del.ExecuteNonQueryAsync();
    }
    var toInsert = new List<TableStatusDto>();
    for (int i = 1; i <= 5; i++) toInsert.Add(new TableStatusDto { Label = $"Billiard {i}", Type = "billiard" });
    for (int i = 1; i <= 10; i++) toInsert.Add(new TableStatusDto { Label = $"Bar {i}", Type = "bar" });
    await using var tx = await conn.BeginTransactionAsync();
    const string sql = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server) VALUES(@l, @t, false, NULL, NULL, NULL)";
    foreach (var rec in toInsert)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@l", rec.Label);
        cmd.Parameters.AddWithValue("@t", rec.Type);
        await cmd.ExecuteNonQueryAsync();
    }
    await tx.CommitAsync();
    return Results.Ok();
});

// List bills with optional filters
app.MapGet("/bills", async (string? from, string? to, string? table, string? server) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);

    var where = new List<string>();
    var cmd = new NpgsqlCommand();
    cmd.Connection = conn;
    if (!string.IsNullOrWhiteSpace(table)) { where.Add("table_label = @table"); cmd.Parameters.AddWithValue("@table", table); }
    if (!string.IsNullOrWhiteSpace(server)) { where.Add("server_name ILIKE @server"); cmd.Parameters.AddWithValue("@server", "%" + server + "%"); }
    if (DateTime.TryParse(from, out var fromDt)) { where.Add("end_time >= @from"); cmd.Parameters.AddWithValue("@from", fromDt.ToUniversalTime()); }
    if (DateTime.TryParse(to, out var toDt)) { where.Add("end_time <= @to"); cmd.Parameters.AddWithValue("@to", toDt.ToUniversalTime()); }

    var whereSql = where.Count > 0 ? (" WHERE " + string.Join(" AND ", where)) : string.Empty;
    cmd.CommandText = $@"SELECT bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount, is_settled, payment_state
                         FROM public.bills{whereSql}
                         ORDER BY end_time DESC
                         LIMIT 500";
    var list = new List<BillResult>();
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        var br = new BillResult
        {
            BillId = rdr.GetFieldValue<Guid>(0),
            TableLabel = rdr.GetString(1),
            ServerId = rdr.GetString(2),
            ServerName = rdr.GetString(3),
            StartTime = rdr.GetFieldValue<DateTime>(4),
            EndTime = rdr.GetFieldValue<DateTime>(5),
            TotalTimeMinutes = rdr.GetInt32(6)
        };
        if (!rdr.IsDBNull(7))
        {
            var json = rdr.GetString(7);
            try { br.Items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); } catch { br.Items = new(); }
        }
        br.TimeCost = rdr.IsDBNull(8) ? 0m : rdr.GetDecimal(8);
        br.ItemsCost = rdr.IsDBNull(9) ? 0m : rdr.GetDecimal(9);
        br.TotalAmount = rdr.IsDBNull(10) ? 0m : rdr.GetDecimal(10);
        // extra fields are available to clients that need them
        list.Add(br);
    }
    return Results.Ok(list);
});

// Get single bill by id
app.MapGet("/bills/{billId}", async (Guid billId) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount, session_id, billing_id, is_settled, payment_state
                         FROM public.bills WHERE bill_id = @id";
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@id", billId);
    await using var rdr = await cmd.ExecuteReaderAsync();
    if (!await rdr.ReadAsync()) return Results.NotFound();
    var br = new BillResult
    {
        BillId = rdr.GetFieldValue<Guid>(0),
        TableLabel = rdr.GetString(1),
        ServerId = rdr.GetString(2),
        ServerName = rdr.GetString(3),
        StartTime = rdr.GetFieldValue<DateTime>(4),
        EndTime = rdr.GetFieldValue<DateTime>(5),
        TotalTimeMinutes = rdr.GetInt32(6)
    };
    if (!rdr.IsDBNull(7))
    {
        var json = rdr.GetString(7);
        try { br.Items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); } catch { br.Items = new(); }
    }
    br.TimeCost = rdr.IsDBNull(8) ? 0m : rdr.GetDecimal(8);
    br.ItemsCost = rdr.IsDBNull(9) ? 0m : rdr.GetDecimal(9);
    br.TotalAmount = rdr.IsDBNull(10) ? 0m : rdr.GetDecimal(10);
    br.SessionId = rdr.IsDBNull(11) ? null : rdr.GetString(11);
    br.BillingId = rdr.IsDBNull(12) ? null : rdr.GetString(12);
    return Results.Ok(br);
});

// Close bill: verify PaymentApi ledger settled then mark bill closed
app.MapPost("/bills/{billId}/close", async (Guid billId) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    // Lookup billing_id
    const string getSql = @"SELECT billing_id FROM public.bills WHERE bill_id = @id";
    string? billingId = null;
    await using (var cmd = new NpgsqlCommand(getSql, conn))
    {
        cmd.Parameters.AddWithValue("@id", billId);
        var obj = await cmd.ExecuteScalarAsync();
        billingId = obj as string;
    }
    if (string.IsNullOrWhiteSpace(billingId)) return Results.Problem("Missing billing_id for bill", statusCode: 400);

    // Call PaymentApi to close (idempotent)
    var payBase = Environment.GetEnvironmentVariable("PAYMENTAPI_BASEURL")
        ?? builder.Configuration["PaymentApi:BaseUrl"];
    if (string.IsNullOrWhiteSpace(payBase)) return Results.Problem("PAYMENTAPI_BASEURL not configured", statusCode: 500);
    using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
    { BaseAddress = new Uri(payBase!.TrimEnd('/') + "/") };
    using var res = await http.PostAsync($"api/payments/{billingId}/close", new StringContent(string.Empty));
    if (!res.IsSuccessStatusCode)
    {
        var body = await res.Content.ReadAsStringAsync();
        return Results.Problem($"Payment close failed: {(int)res.StatusCode} {body}", statusCode: 502);
    }

    // Mark bill closed
    const string upd = @"UPDATE public.bills SET status = 'closed', closed_at = now() WHERE bill_id = @id";
    await using (var u = new NpgsqlCommand(upd, conn))
    {
        u.Parameters.AddWithValue("@id", billId);
        await u.ExecuteNonQueryAsync();
    }
    return Results.Ok(new { closed = true, billId });
});

// List unsettled bills only
app.MapGet("/bills/unsettled", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount
                         FROM public.bills WHERE COALESCE(is_settled, false) = false AND status = 'awaiting_payment'
                         ORDER BY end_time DESC LIMIT 500";
    var list = new List<BillResult>();
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        var br = new BillResult
        {
            BillId = rdr.GetFieldValue<Guid>(0),
            TableLabel = rdr.GetString(1),
            ServerId = rdr.GetString(2),
            ServerName = rdr.GetString(3),
            StartTime = rdr.GetFieldValue<DateTime>(4),
            EndTime = rdr.GetFieldValue<DateTime>(5),
            TotalTimeMinutes = rdr.GetInt32(6)
        };
        if (!rdr.IsDBNull(7))
        {
            var json = rdr.GetString(7);
            try { br.Items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); } catch { br.Items = new(); }
        }
        br.TimeCost = rdr.IsDBNull(8) ? 0m : rdr.GetDecimal(8);
        br.ItemsCost = rdr.IsDBNull(9) ? 0m : rdr.GetDecimal(9);
        br.TotalAmount = rdr.IsDBNull(10) ? 0m : rdr.GetDecimal(10);
        list.Add(br);
    }
    return Results.Ok(list);
});

// Mark bill settled (by bill id)
app.MapPost("/bills/{billId}/settle", async (Guid billId) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string upd = @"UPDATE public.bills SET is_settled = true, payment_state = 'paid' WHERE bill_id = @id";
    await using var cmd = new NpgsqlCommand(upd, conn);
    cmd.Parameters.AddWithValue("@id", billId);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? Results.Ok(new { billId, settled = true }) : Results.NotFound();
});

// Mark bill settled by billingId (used by PaymentApi when ledger reaches paid)
app.MapPost("/bills/by-billing/{billingId}/settle", async (string billingId) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string upd = @"UPDATE public.bills SET is_settled = true, payment_state = 'paid' WHERE billing_id = @bid";
    await using var cmd = new NpgsqlCommand(upd, conn);
    cmd.Parameters.AddWithValue("@bid", billingId);
    var rows = await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { billingId, updated = rows });
});

// Check if billing ID exists (used by PaymentApi for validation)
app.MapGet("/bills/by-billing/{billingId}", async (string billingId) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount, session_id, billing_id, is_settled, payment_state
                         FROM public.bills WHERE billing_id = @bid";
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@bid", billingId);
    await using var rdr = await cmd.ExecuteReaderAsync();
    if (!await rdr.ReadAsync()) return Results.NotFound();
    var br = new BillResult
    {
        BillId = rdr.GetFieldValue<Guid>(0),
        TableLabel = rdr.GetString(1),
        ServerId = rdr.GetString(2),
        ServerName = rdr.GetString(3),
        StartTime = rdr.GetFieldValue<DateTimeOffset>(4).DateTime,
        EndTime = rdr.GetFieldValue<DateTimeOffset>(5).DateTime,
        TotalTimeMinutes = rdr.GetInt32(6),
        Items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(rdr.GetString(7)) ?? new(),
        TimeCost = rdr.GetDecimal(8),
        ItemsCost = rdr.GetDecimal(9),
        TotalAmount = rdr.GetDecimal(10)
    };
    br.SessionId = rdr.IsDBNull(11) ? null : rdr.GetString(11);
    br.BillingId = rdr.IsDBNull(12) ? null : rdr.GetString(12);
    return Results.Ok(br);
});

// Get items for active session
app.MapGet("/tables/{label}/items", async (string label) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    const string sql = @"SELECT items FROM public.table_sessions WHERE table_label = @label AND status = 'active' ORDER BY start_time DESC LIMIT 1";
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@label", label);
    await using var rdr = await cmd.ExecuteReaderAsync();
    if (!await rdr.ReadAsync()) return Results.NotFound(new { message = "No active session." });
    if (rdr.IsDBNull(0)) return Results.Ok(new List<ItemLine>());
    var json = rdr.GetString(0);
    try { var items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); return Results.Ok(items); }
    catch { return Results.Ok(new List<ItemLine>()); }
});

// Replace items for active session
app.MapPost("/tables/{label}/items", async (string label, List<ItemLine> itemsBody) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    const string findSql = @"SELECT session_id FROM public.table_sessions WHERE table_label = @label AND status='active' ORDER BY start_time DESC LIMIT 1";
    Guid sessionId;
    await using (var find = new NpgsqlCommand(findSql, conn))
    {
        find.Parameters.AddWithValue("@label", label);
        var result = await find.ExecuteScalarAsync();
        if (result is Guid g) sessionId = g; else return Results.NotFound(new { message = "No active session." });
    }
    const string updSql = @"UPDATE public.table_sessions SET items = @items WHERE session_id = @sid";
    await using (var upd = new NpgsqlCommand(updSql, conn))
    {
        upd.Parameters.AddWithValue("@sid", sessionId);
        upd.Parameters.Add("@items", NpgsqlDbType.Jsonb).Value = JsonSerializer.Serialize(itemsBody ?? new());
        await upd.ExecuteNonQueryAsync();
    }
    return Results.Ok();
});

app.MapPost("/tables/bulkUpsert", async (IEnumerable<TableStatusDto> recs) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
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
    return Results.Ok();
});

app.MapGet("/tables", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    var list = new List<TableStatusDto>();
    const string sql = "SELECT label, type, occupied, order_id, start_time, server FROM public.table_status ORDER BY label";
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
    return Results.Ok(list);
});

app.MapGet("/tables/counts", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT type, COUNT(*) FROM public.table_status GROUP BY type";
    var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    await using var cmd = new NpgsqlCommand(sql, conn);
    await using var rdr = await cmd.ExecuteReaderAsync();
    while (await rdr.ReadAsync())
    {
        dict[rdr.GetString(0)] = rdr.GetInt32(1);
    }
    return Results.Ok(dict);
});

app.MapGet("/tables/{label}", async (string label) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"SELECT label, type, occupied, order_id, start_time, server FROM public.table_status WHERE label = @l";
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@l", label);
    await using var rdr = await cmd.ExecuteReaderAsync();
    if (await rdr.ReadAsync())
    {
        return Results.Ok(new TableStatusDto
        {
            Label = rdr.GetString(0),
            Type = rdr.GetString(1),
            Occupied = rdr.GetBoolean(2),
            OrderId = rdr.IsDBNull(3) ? null : rdr.GetString(3),
            StartTime = rdr.IsDBNull(4) ? null : rdr.GetFieldValue<DateTimeOffset>(4),
            Server = rdr.IsDBNull(5) ? null : rdr.GetString(5)
        });
    }
    return Results.NotFound();
});

app.MapPut("/tables/{label}", async (string label, TableStatusDto rec) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    const string sql = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server) VALUES(@l, @t, @o, @oid, @st, @srv)
        ON CONFLICT (label) DO UPDATE SET type = EXCLUDED.type, occupied = EXCLUDED.occupied, order_id = EXCLUDED.order_id, start_time = EXCLUDED.start_time, server = EXCLUDED.server, updated_at = now();";
    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@l", label);
    cmd.Parameters.AddWithValue("@t", rec.Type);
    cmd.Parameters.AddWithValue("@o", rec.Occupied);
    cmd.Parameters.AddWithValue("@oid", (object?)rec.OrderId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@st", (object?)rec.StartTime ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@srv", (object?)rec.Server ?? DBNull.Value);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok();
});

app.MapPost("/tables/upsert", async (TableStatusDto rec) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
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
    return Results.Ok();
});

// Convenience: allow GET /tables/seed (returns 200) to avoid 405 when called from browser
app.MapGet("/tables/seed", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    var toInsert = new List<TableStatusDto>();
    for (int i = 1; i <= 5; i++) toInsert.Add(new TableStatusDto { Label = $"Billiard {i}", Type = "billiard" });
    for (int i = 1; i <= 10; i++) toInsert.Add(new TableStatusDto { Label = $"Bar {i}", Type = "bar" });
    await using var tx = await conn.BeginTransactionAsync();
    const string sql = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server) VALUES(@l, @t, false, NULL, NULL, NULL)
        ON CONFLICT (label) DO NOTHING;";
    foreach (var rec in toInsert)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@l", rec.Label);
        cmd.Parameters.AddWithValue("@t", rec.Type);
        await cmd.ExecuteNonQueryAsync();
    }
    await tx.CommitAsync();
    return Results.Ok();
});

app.MapPost("/tables/seed", async () =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);
    var toInsert = new List<TableStatusDto>();
    for (int i = 1; i <= 5; i++) toInsert.Add(new TableStatusDto { Label = $"Billiard {i}", Type = "billiard" });
    for (int i = 1; i <= 10; i++) toInsert.Add(new TableStatusDto { Label = $"Bar {i}", Type = "bar" });

    await using var tx = await conn.BeginTransactionAsync();
    const string sql = @"INSERT INTO public.table_status(label, type, occupied, order_id, start_time, server) VALUES(@l, @t, false, NULL, NULL, NULL)
        ON CONFLICT (label) DO NOTHING;";
    foreach (var rec in toInsert)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("@l", rec.Label);
        cmd.Parameters.AddWithValue("@t", rec.Type);
        await cmd.ExecuteNonQueryAsync();
    }
    await tx.CommitAsync();
    return Results.Ok();
});

app.Run();

static async Task EnsureSchemaAsync(NpgsqlConnection conn)
{
    const string sql = @"CREATE TABLE IF NOT EXISTS public.table_status (
        label text PRIMARY KEY,
        type text NOT NULL,
        occupied boolean NOT NULL,
        order_id text NULL,
        start_time timestamptz NULL,
        server text NULL,
        updated_at timestamptz NOT NULL DEFAULT now()
    );
    ALTER TABLE public.table_sessions ADD COLUMN IF NOT EXISTS items jsonb NOT NULL DEFAULT '[]';
    ALTER TABLE public.table_status ADD COLUMN IF NOT EXISTS order_id text NULL;
    ALTER TABLE public.table_status ADD COLUMN IF NOT EXISTS start_time timestamptz NULL;
    ALTER TABLE public.table_status ADD COLUMN IF NOT EXISTS server text NULL;

    CREATE TABLE IF NOT EXISTS public.table_sessions (
        session_id uuid PRIMARY KEY,
        table_label text NOT NULL,
        server_id text NOT NULL,
        server_name text NOT NULL,
        start_time timestamp NOT NULL,
        end_time timestamp NULL,
        status text NOT NULL CHECK (status IN ('active','closed')) DEFAULT 'active',
        items jsonb NOT NULL DEFAULT '[]',
        billing_id uuid NULL
    );

    CREATE TABLE IF NOT EXISTS public.bills (
        bill_id uuid PRIMARY KEY,
        billing_id text NULL UNIQUE,
        session_id text NULL,
        table_label text NOT NULL,
        server_id text NOT NULL,
        server_name text NOT NULL,
        start_time timestamp NOT NULL,
        end_time timestamp NOT NULL,
        total_time_minutes int NOT NULL,
        items jsonb NOT NULL DEFAULT '[]',
        time_cost numeric NOT NULL DEFAULT 0,
        items_cost numeric NOT NULL DEFAULT 0,
        total_amount numeric NOT NULL DEFAULT 0,
        status text NOT NULL DEFAULT 'awaiting_payment' CHECK (status IN ('awaiting_payment','closed')),
        is_settled boolean NOT NULL DEFAULT false,
        payment_state text NOT NULL DEFAULT 'not-paid' CHECK (payment_state IN ('not-paid','partial-paid','paid','partial-refunded','refunded','cancelled')),
        closed_at timestamp NULL,
        created_at timestamp NOT NULL DEFAULT now()
    );";
    // Ensure new columns for bills
    const string sql2 = @"ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS items jsonb NOT NULL DEFAULT '[]';
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS time_cost numeric NOT NULL DEFAULT 0;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS items_cost numeric NOT NULL DEFAULT 0;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS total_amount numeric NOT NULL DEFAULT 0;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS billing_id text NULL;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS session_id text NULL;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'awaiting_payment';
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS is_settled boolean NOT NULL DEFAULT false;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS payment_state text NOT NULL DEFAULT 'not-paid';
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS closed_at timestamp NULL;";
    const string sql3 = @"CREATE TABLE IF NOT EXISTS public.app_settings(
                            key text PRIMARY KEY,
                            value text NOT NULL
                          );";
    // Audit table for moves
    const string sql4 = @"CREATE TABLE IF NOT EXISTS public.table_session_moves(
                            session_id uuid NOT NULL,
                            from_label text NOT NULL,
                            to_label text NOT NULL,
                            moved_at timestamp NOT NULL DEFAULT now()
                          );";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
    await using var cmd2 = new NpgsqlCommand(sql2, conn);
    await cmd2.ExecuteNonQueryAsync();
    await using var cmd3 = new NpgsqlCommand(sql3, conn);
    await cmd3.ExecuteNonQueryAsync();
    await using var cmd4 = new NpgsqlCommand(sql4, conn);
    await cmd4.ExecuteNonQueryAsync();
}

static async Task<decimal> GetRatePerMinuteAsync(NpgsqlConnection conn, decimal fallback)
{
    const string sql = "SELECT value FROM public.app_settings WHERE key = 'Tables.RatePerMinute'";
    await using var cmd = new NpgsqlCommand(sql, conn);
    var obj = await cmd.ExecuteScalarAsync();
    if (obj is string s && decimal.TryParse(s, out var rate)) return rate;
    return fallback;
}

// Using shared TableStatusDto instead of local TableRecord

// DTOs now live in MagiDesk.Shared.DTOs.Tables
