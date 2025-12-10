using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/reports")] 
public class ReportsController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    public ReportsController(NpgsqlDataSource dataSource) { _dataSource = dataSource; }

    public record LowStockItem(string item_id, string sku, string name, decimal quantity_on_hand, decimal? reorder_threshold);

    [HttpGet("low-stock")] 
    public async Task<ActionResult<IEnumerable<LowStockItem>>> LowStock([FromQuery] decimal? threshold, CancellationToken ct)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var sql = @"select i.item_id::text, i.sku, i.name,
                           coalesce(s.quantity_on_hand,0) as quantity_on_hand,
                           i.reorder_threshold
                    from inventory.inventory_items i
                    left join inventory.inventory_stock s on s.item_id = i.item_id
                    where i.is_active = true and coalesce(s.quantity_on_hand,0) <= coalesce(@th, i.reorder_threshold, 0)
                    order by quantity_on_hand asc, i.name asc
                    limit 500";
        var items = await conn.QueryAsync<LowStockItem>(sql, new { th = threshold });
        return Ok(items);
    }

    public record MarginSummary(decimal total_selling, decimal total_buying, decimal gross_margin);

    [HttpGet("margins")] 
    public async Task<ActionResult<MarginSummary>> Margins([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        // For now, approximate based on transactions where source='customer_order'
        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var sql = @"select coalesce(sum(case when t.delta < 0 then (abs(t.delta) * i.selling_price) end),0) as total_selling,
                           coalesce(sum(case when t.delta < 0 then (abs(t.delta) * coalesce(i.buying_price,0)) end),0) as total_buying
                    from inventory.inventory_transactions t
                    join inventory.inventory_items i on i.item_id = t.item_id
                    where t.source = 'customer_order'
                      and (@from is null or t.occurred_at >= @from)
                      and (@to is null or t.occurred_at < @to)";
        var row = await conn.QuerySingleAsync(sql, new { from, to });
        decimal selling = row.total_selling ?? 0m;
        decimal buying = row.total_buying ?? 0m;
        return Ok(new MarginSummary(selling, buying, selling - buying));
    }
}

