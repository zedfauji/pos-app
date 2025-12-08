using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Linq;
using System.IO;

namespace MagiDesk.SyncService;

public class SyncEngine
{
    private readonly SimpleLogger _log;

    public SyncEngine(SimpleLogger logger)
    {
        _log = logger;
    }

    public async Task RunOnceAsync(SyncConfig cfg, CancellationToken ct = default)
    {
        try
        {
            var fdbPaths = new List<string>(cfg.FirebirdPaths);
            if (fdbPaths.Count == 0)
            {
                try
                {
                    var dir = cfg.DefaultFirebirdDirectory ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                    {
                        var found = Directory.EnumerateFiles(dir, "*.fdb", SearchOption.AllDirectories).ToList();
                        fdbPaths.AddRange(found);
                        _log.Info($"Default scan found {found.Count} .FDB files under '{dir}'.");
                    }
                }
                catch (Exception scanEx)
                {
                    _log.Error("Error scanning default Firebird directory", scanEx);
                }
            }
            if (fdbPaths.Count == 0)
            {
                _log.Info("No Firebird paths found to sync.");
                return;
            }
            if (string.IsNullOrWhiteSpace(cfg.FirestoreProjectId) || string.IsNullOrWhiteSpace(cfg.ServiceAccountJsonPath))
            {
                _log.Error("Firestore configuration missing (projectId or serviceAccountJsonPath).");
                return;
            }

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", cfg.ServiceAccountJsonPath);
            var db = await FirestoreDb.CreateAsync(cfg.FirestoreProjectId);

            foreach (var fdbPath in fdbPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (ct.IsCancellationRequested) break;
                await SyncDatabaseAsync(fdbPath, db, cfg, ct);
            }
        }
        catch (Exception ex)
        {
            _log.Error("RunOnce failed", ex);
        }
    }

    private async Task SyncDatabaseAsync(string dbPath, FirestoreDb db, SyncConfig cfg, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(dbPath)) { _log.Error($"FDB not found: {dbPath}"); return; }

            var cs = new FbConnectionStringBuilder
            {
                Database = dbPath,
                UserID = cfg.FirebirdUser ?? "sysdba",
                Password = cfg.FirebirdPassword ?? "masterkey",
                Dialect = 3,
                Role = null,
                ConnectionLifeTime = 0,
                Charset = "UTF8",
                Pooling = true
            }.ToString();

            using var conn = new FbConnection(cs);
            await conn.OpenAsync(ct);

            var tables = await ListUserTablesAsync(conn, ct);
            foreach (var table in tables)
            {
                if (ct.IsCancellationRequested) break;
                await SyncTableAsync(conn, table, db, cfg, ct);
            }
        }
        catch (Exception ex)
        {
            _log.Error($"SyncDatabase failed for {dbPath}", ex);
        }
    }

    private async Task<List<string>> ListUserTablesAsync(FbConnection conn, CancellationToken ct)
    {
        var list = new List<string>();
        const string sql = @"SELECT TRIM(RDB$RELATION_NAME) FROM RDB$RELATIONS WHERE COALESCE(RDB$SYSTEM_FLAG,0) = 0 AND RDB$VIEW_BLR IS NULL";
        using var cmd = new FbCommand(sql, conn);
        using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(rdr.GetString(0).Trim());
        }
        return list;
    }

    private async Task SyncTableAsync(FbConnection conn, string table, FirestoreDb db, SyncConfig cfg, CancellationToken ct)
    {
        try
        {
            // Read last checkpoint
            var metaDoc = db.Collection("sync_metadata").Document(table);
            Timestamp lastTs = Timestamp.FromDateTime(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            long rowCount = 0;
            var snap = await metaDoc.GetSnapshotAsync(ct);
            if (snap.Exists)
            {
                if (snap.TryGetValue("lastSyncedAt", out Timestamp ts)) lastTs = ts;
                if (snap.TryGetValue("rowCount", out long rc)) rowCount = rc;
            }

            var lastUpdCol = string.IsNullOrWhiteSpace(cfg.LastUpdatedColumnName) ? "last_updated" : cfg.LastUpdatedColumnName;
            var sql = $"SELECT * FROM {table} WHERE {lastUpdCol} > @p0";
            using var cmd = new FbCommand(sql, conn);
            cmd.Parameters.AddWithValue("@p0", lastTs.ToDateTime().ToUniversalTime());

            using var rdr = await cmd.ExecuteReaderAsync(ct);
            var updates = new List<Dictionary<string, object?>>();
            var maxUpdated = lastTs;
            var cols = Enumerable.Range(0, rdr.FieldCount).Select(i => rdr.GetName(i)).ToArray();
            while (await rdr.ReadAsync(ct))
            {
                var row = new Dictionary<string, object?>();
                foreach (var name in cols)
                {
                    var val = rdr[name];
                    if (val is DBNull) val = null;
                    row[name] = val;
                    if (string.Equals(name, lastUpdCol, StringComparison.OrdinalIgnoreCase) && val is DateTime dt)
                    {
                        var ts = Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                        if (ts.CompareTo(maxUpdated) > 0) maxUpdated = ts;
                    }
                }
                updates.Add(row);
            }
            if (updates.Count == 0) { _log.Info($"{table}: no changes."); return; }

            // Determine document id from primary key(s) — fallback to concatenating all columns
            string[] pkCols = await GetPrimaryKeyColumnsAsync(conn, table, ct);
            var batch = db.StartBatch();
            foreach (var row in updates)
            {
                var docId = BuildDocId(row, pkCols);
                var docRef = db.Collection(table).Document(docId);
                batch.Set(docRef, row, SetOptions.MergeAll);
            }
            await batch.CommitAsync(ct);

            // Save checkpoint
            var newCount = rowCount + updates.Count;
            await metaDoc.SetAsync(new { lastSyncedAt = maxUpdated, rowCount = newCount }, SetOptions.MergeAll, ct);
            _log.Info($"{table}: upserted {updates.Count} rows. lastSyncedAt={maxUpdated}");
        }
        catch (FbException fbex) when (fbex.ErrorCode == 335544345 /* lock conflict */)
        {
            _log.Error($"{table}: DB locked, will retry later.", fbex);
        }
        catch (Exception ex)
        {
            _log.Error($"SyncTable failed for {table}", ex);
        }
    }

    private static string BuildDocId(Dictionary<string, object?> row, string[] pkCols)
    {
        var cols = (pkCols.Length > 0 ? pkCols : row.Keys.ToArray());
        var parts = new List<string>();
        foreach (var c in cols)
        {
            row.TryGetValue(c, out var v);
            parts.Add(Convert.ToString(v) ?? string.Empty);
        }
        return string.Join("_", parts);
    }

    private async Task<string[]> GetPrimaryKeyColumnsAsync(FbConnection conn, string table, CancellationToken ct)
    {
        var sql = @"select sg.rdb$field_name
            from rdb$indices i
            join rdb$index_segments sg on sg.rdb$index_name = i.rdb$index_name
            where i.rdb$relation_name = @tbl and i.rdb$unique_flag = 1 and i.rdb$index_type is null";
        using var cmd = new FbCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tbl", table);
        var list = new List<string>();
        using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
        {
            list.Add(rdr.GetString(0).Trim());
        }
        return list.ToArray();
    }
}
