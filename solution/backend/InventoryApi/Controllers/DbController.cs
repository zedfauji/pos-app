using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/db")] 
public class DbController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IWebHostEnvironment _env;

    public DbController(NpgsqlDataSource dataSource, IWebHostEnvironment env)
    {
        _dataSource = dataSource;
        _env = env;
    }

    [HttpPost("apply-schema")] 
    public async Task<IActionResult> ApplySchema(CancellationToken ct)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Database", "schema.sql");
            if (!System.IO.File.Exists(path)) return NotFound(new { error = "schema.sql not found" });
            var sql = await System.IO.File.ReadAllTextAsync(path, ct);
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);
            await conn.ExecuteAsync(sql, transaction: tx);
            await tx.CommitAsync(ct);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("probe")]
    public async Task<IActionResult> Probe(CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            const string sql = @"create schema if not exists inventory;
                                   create table if not exists inventory._probe (x int);";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync(ct);
            return Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

