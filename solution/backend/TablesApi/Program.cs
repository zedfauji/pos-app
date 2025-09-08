using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
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

// Prioritize env var to make Cloud Run overrides take effect
string? connString = Environment.GetEnvironmentVariable("TABLESAPI__CONNSTRING")
    ?? builder.Configuration.GetConnectionString("Postgres")
    ?? builder.Configuration["Db:Postgres:ConnectionString"];

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
    var now = DateTime.UtcNow;
    const string insertSql = @"INSERT INTO public.table_sessions(session_id, table_label, server_id, server_name, start_time, status)
                               VALUES(@sid, @label, @srvId, @srvName, @start, 'active')";
    await using (var ins = new NpgsqlCommand(insertSql, conn))
    {
        ins.Parameters.AddWithValue("@sid", sessionId);
        ins.Parameters.AddWithValue("@label", label);
        ins.Parameters.AddWithValue("@srvId", req.ServerId);
        ins.Parameters.AddWithValue("@srvName", req.ServerName);
        ins.Parameters.AddWithValue("@start", now);
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

    return Results.Ok(new { session_id = sessionId, start_time = now });
});

// Stop a table session and generate bill
app.MapPost("/tables/{label}/stop", async (string label) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await EnsureSchemaAsync(conn);

    // Find active session
    const string findSql = @"SELECT session_id, server_id, server_name, start_time FROM public.table_sessions
                             WHERE table_label = @label AND status = 'active' ORDER BY start_time DESC LIMIT 1";
    Guid sessionId;
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
        await using var rdr = await getItems.ExecuteReaderAsync();
        if (await rdr.ReadAsync() && !rdr.IsDBNull(0))
        {
            var json = rdr.GetString(0);
            try { items = System.Text.Json.JsonSerializer.Deserialize<List<ItemLine>>(json) ?? new(); } catch { items = new(); }
        }
    }

    // Compute costs (fetch dynamic rate if exists)
    decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
    decimal itemsCost = items.Sum(i => i.price * i.quantity);
    decimal timeCost = ratePerMinute * minutes;
    decimal total = timeCost + itemsCost;

    // Create bill
    var billId = Guid.NewGuid();
    const string billSql = @"INSERT INTO public.bills(bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount)
                            VALUES(@bid, @label, @srvId, @srvName, @start, @end, @mins, @items, @timeCost, @itemsCost, @total)";
    await using (var bill = new NpgsqlCommand(billSql, conn))
    {
        bill.Parameters.AddWithValue("@bid", billId);
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
    cmd.CommandText = $@"SELECT bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount
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
    const string sql = @"SELECT bill_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount
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
        items jsonb NOT NULL DEFAULT '[]'
    );

    CREATE TABLE IF NOT EXISTS public.bills (
        bill_id uuid PRIMARY KEY,
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
        created_at timestamp NOT NULL DEFAULT now()
    );";
    // Ensure new columns for bills
    const string sql2 = @"ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS items jsonb NOT NULL DEFAULT '[]';
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS time_cost numeric NOT NULL DEFAULT 0;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS items_cost numeric NOT NULL DEFAULT 0;
                         ALTER TABLE public.bills ADD COLUMN IF NOT EXISTS total_amount numeric NOT NULL DEFAULT 0;";
    const string sql3 = @"CREATE TABLE IF NOT EXISTS public.app_settings(
                            key text PRIMARY KEY,
                            value text NOT NULL
                          );";
    await using var cmd = new NpgsqlCommand(sql, conn);
    await cmd.ExecuteNonQueryAsync();
    await using var cmd2 = new NpgsqlCommand(sql2, conn);
    await cmd2.ExecuteNonQueryAsync();
    await using var cmd3 = new NpgsqlCommand(sql3, conn);
    await cmd3.ExecuteNonQueryAsync();
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
