using Dapper;
using InventoryApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/inventory")] 
public class StockController : ControllerBase
{
    private readonly IInventoryRepository _repo;
    private readonly NpgsqlDataSource _dataSource;
    public StockController(IInventoryRepository repo, NpgsqlDataSource dataSource)
    {
        _repo = repo;
        _dataSource = dataSource;
    }

    public record AdjustRequest(decimal delta, decimal? unitCost, string source, string? sourceRef, string? notes, string? userId);
    public record AvailabilityRequest(string id, decimal quantity);
    public record AvailabilityBySkuRequest(string sku, decimal quantity);

    [HttpPost("items/{id}/adjust")]
    public async Task<IActionResult> Adjust(string id, [FromBody] AdjustRequest req, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            // Lock row
            var before = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(
                "select quantity_on_hand from inventory.inventory_stock where item_id=@Id for update",
                new { Id = Guid.Parse(id) }, cancellationToken: ct));
            if (before is null)
            {
                // initialize
                await conn.ExecuteAsync(new CommandDefinition(
                    "insert into inventory.inventory_stock(item_id, quantity_on_hand) values(@Id, 0) on conflict (item_id) do nothing",
                    new { Id = Guid.Parse(id) }, cancellationToken: ct));
                before = 0m;
            }
            var after = before!.Value + req.delta;
            if (after < 0)
            {
                return BadRequest(new { error = "Insufficient stock" });
            }
            await conn.ExecuteAsync(new CommandDefinition(
                "update inventory.inventory_stock set quantity_on_hand=@After, last_counted_at=now() where item_id=@Id",
                new { Id = Guid.Parse(id), After = after }, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(
                "insert into inventory.inventory_transactions(item_id, delta, quantity_before, quantity_after, unit_cost, source, source_ref, user_id, notes) values (@Id, @Delta, @Before, @After, @UnitCost, cast(@Source as inventory.transaction_source), @SourceRef, @UserId, @Notes)",
                new { Id = Guid.Parse(id), Delta = req.delta, Before = before, After = after, UnitCost = req.unitCost, Source = req.source, SourceRef = req.sourceRef, UserId = req.userId, Notes = req.notes }, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return NoContent();
        }
        catch (PostgresException pe) when (pe.SqlState == "22P02")
        {
            await tx.RollbackAsync(ct);
            return BadRequest(new { error = "Invalid id format" });
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpPost("availability/check")]
    public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityRequest[] req, CancellationToken ct)
    {
        var ids = req.Select(r => Guid.Parse(r.id)).ToArray();
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(Guid item_id, decimal quantity_on_hand)>(
            new CommandDefinition(
                "select item_id, coalesce(quantity_on_hand,0) as quantity_on_hand from inventory.inventory_stock where item_id = any(@ids)",
                new { ids }, cancellationToken: ct));
        var map = rows.ToDictionary(x => x.item_id, x => x.quantity_on_hand);
        var insufficient = new List<object>();
        foreach (var r in req)
        {
            var gid = Guid.Parse(r.id);
            var have = map.TryGetValue(gid, out var q) ? q : 0m;
            if (have < r.quantity)
                insufficient.Add(new { id = r.id, have, need = r.quantity });
        }
        return Ok(new { ok = insufficient.Count == 0, insufficient });
    }

    [HttpPost("availability/check-by-sku")]
    public async Task<IActionResult> CheckAvailabilityBySku([FromBody] AvailabilityBySkuRequest[] req, CancellationToken ct)
    {
        var skus = req.Select(r => r.sku).ToArray();
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<(string sku, decimal quantity_on_hand)>(
            new CommandDefinition(
                "select i.sku, coalesce(s.quantity_on_hand,0) as quantity_on_hand from inventory.inventory_items i left join inventory.inventory_stock s on s.item_id=i.item_id where i.sku = any(@skus) and i.is_active = true",
                new { skus }, cancellationToken: ct));
        var map = rows.ToDictionary(x => x.sku, x => x.quantity_on_hand, StringComparer.OrdinalIgnoreCase);
        var insufficient = new List<object>();
        foreach (var r in req)
        {
            var have = map.TryGetValue(r.sku, out var q) ? q : 0m;
            if (have < r.quantity)
                insufficient.Add(new { sku = r.sku, have, need = r.quantity });
        }
        return Ok(new { ok = insufficient.Count == 0, insufficient });
    }

    public record AdjustBySkuRequest(string sku, decimal delta, decimal? unitCost, string source, string? sourceRef, string? notes, string? userId);

    [HttpPost("items/adjust-by-sku")]
    public async Task<IActionResult> AdjustBySku([FromBody] AdjustBySkuRequest req, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var id = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "select item_id from inventory.inventory_items where lower(sku)=lower(@sku) and is_active=true",
                new { req.sku }, cancellationToken: ct));
            if (id is null) return NotFound(new { error = "SKU not found" });
            // Lock row
            var before = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(
                "select quantity_on_hand from inventory.inventory_stock where item_id=@Id for update",
                new { Id = id.Value }, cancellationToken: ct));
            if (before is null)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "insert into inventory.inventory_stock(item_id, quantity_on_hand) values(@Id, 0) on conflict (item_id) do nothing",
                    new { Id = id.Value }, cancellationToken: ct));
                before = 0m;
            }
            var after = before!.Value + req.delta;
            if (after < 0) return BadRequest(new { error = "Insufficient stock" });
            await conn.ExecuteAsync(new CommandDefinition(
                "update inventory.inventory_stock set quantity_on_hand=@After, last_counted_at=now() where item_id=@Id",
                new { Id = id.Value, After = after }, cancellationToken: ct));
            await conn.ExecuteAsync(new CommandDefinition(
                "insert into inventory.inventory_transactions(item_id, delta, quantity_before, quantity_after, unit_cost, source, source_ref, user_id, notes) values (@Id, @Delta, @Before, @After, @UnitCost, cast(@Source as inventory.transaction_source), @SourceRef, @UserId, @Notes)",
                new { Id = id.Value, Delta = req.delta, Before = before, After = after, UnitCost = req.unitCost, Source = req.source, SourceRef = req.sourceRef, UserId = req.userId, Notes = req.notes }, cancellationToken: ct));
            await tx.CommitAsync(ct);
            return NoContent();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}

