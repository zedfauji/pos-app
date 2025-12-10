using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<AuditController> _logger;

    public AuditController(NpgsqlDataSource dataSource, ILogger<AuditController> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 1000,
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT 
                    al.audit_id::text as Id,
                    al.timestamp as Timestamp,
                    al.entity_type as EntityType,
                    al.entity_id::text as EntityId,
                    al.action as Action,
                    al.user_id as UserId,
                    al.user_name as UserName,
                    al.details as Details,
                    al.old_values as OldValues,
                    al.new_values as NewValues
                FROM inventory.audit_logs al
                WHERE 1=1
                    AND (@entityType IS NULL OR al.entity_type = @entityType)
                    AND (@entityId IS NULL OR al.entity_id::text = @entityId)
                    AND (@action IS NULL OR al.action = @action)
                    AND (@userId IS NULL OR al.user_id = @userId)
                    AND (@fromDate IS NULL OR al.timestamp >= @fromDate)
                    AND (@toDate IS NULL OR al.timestamp <= @toDate)
                    AND (@search IS NULL OR al.details ILIKE @search OR al.user_name ILIKE @search)
                ORDER BY al.timestamp DESC
                LIMIT @limit";

            var logs = await conn.QueryAsync<AuditLogDto>(sql, new 
            { 
                entityType, 
                entityId, 
                action, 
                userId, 
                fromDate, 
                toDate, 
                search = search != null ? $"%{search}%" : null,
                limit
            });

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<InventoryTransactionAuditDto>>> GetInventoryTransactions(
        [FromQuery] string? itemId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? userId = null,
        [FromQuery] int limit = 1000,
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT 
                    it.transaction_id::text as Id,
                    it.item_id::text as ItemId,
                    i.name as ItemName,
                    it.source as Type,
                    it.delta as Quantity,
                    it.unit_cost as UnitCost,
                    it.source as Source,
                    it.source_ref as SourceRef,
                    it.user_id as UserId,
                    COALESCE(u.name, it.user_id) as UserName,
                    it.occurred_at as Timestamp,
                    it.notes as Notes
                FROM inventory.inventory_transactions it
                LEFT JOIN inventory.inventory_items i ON i.item_id = it.item_id
                LEFT JOIN inventory.users u ON u.user_id = it.user_id
                WHERE 1=1
                    AND (@itemId IS NULL OR it.item_id::text = @itemId)
                    AND (@type IS NULL OR it.source = @type)
                    AND (@source IS NULL OR it.source = @source)
                    AND (@fromDate IS NULL OR it.occurred_at >= @fromDate)
                    AND (@toDate IS NULL OR it.occurred_at <= @toDate)
                    AND (@userId IS NULL OR it.user_id = @userId)
                ORDER BY it.occurred_at DESC
                LIMIT @limit";

            var transactions = await conn.QueryAsync<InventoryTransactionAuditDto>(sql, new 
            { 
                itemId, 
                type, 
                source, 
                fromDate, 
                toDate, 
                userId,
                limit
            });

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory transactions");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("reports/inventory")]
    public async Task<ActionResult<InventoryReportDto>> GenerateInventoryReport(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var reportDate = DateTime.Now;
            var periodFrom = fromDate ?? reportDate.AddDays(-30);
            var periodTo = toDate ?? reportDate;

            // Get basic statistics
            var statsSql = @"
                SELECT 
                    COUNT(*) as TotalItems,
                    COALESCE(SUM(s.quantity_on_hand * i.selling_price), 0) as TotalValue,
                    COUNT(CASE WHEN COALESCE(s.quantity_on_hand, 0) <= COALESCE(i.reorder_threshold, 0) THEN 1 END) as LowStockItems,
                    COUNT(CASE WHEN COALESCE(s.quantity_on_hand, 0) > COALESCE(i.reorder_threshold, 0) * 2 THEN 1 END) as FastMovingItems,
                    COUNT(CASE WHEN COALESCE(s.quantity_on_hand, 0) <= COALESCE(i.reorder_threshold, 0) * 0.5 THEN 1 END) as SlowMovingItems
                FROM inventory.inventory_items i
                LEFT JOIN inventory.inventory_stock s ON s.item_id = i.item_id
                WHERE i.is_active = true";

            var stats = await conn.QuerySingleAsync(statsSql);

            // Get top selling items
            var topSellingSql = @"
                SELECT 
                    i.name as ItemName,
                    ABS(SUM(it.delta)) as QuantitySold,
                    ABS(SUM(it.delta * COALESCE(it.unit_cost, i.selling_price))) as Revenue
                FROM inventory.inventory_transactions it
                JOIN inventory.inventory_items i ON i.item_id = it.item_id
                WHERE it.delta < 0 
                    AND it.source = 'customer_order'
                    AND (@fromDate IS NULL OR it.occurred_at >= @fromDate)
                    AND (@toDate IS NULL OR it.occurred_at <= @toDate)
                GROUP BY i.item_id, i.name
                ORDER BY QuantitySold DESC
                LIMIT 10";

            var topSellingItems = await conn.QueryAsync<TopSellingItemDto>(topSellingSql, new { fromDate = periodFrom, toDate = periodTo });

            var report = new InventoryReportDto
            {
                ReportDate = reportDate,
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                TotalItems = stats.TotalItems,
                TotalValue = stats.TotalValue,
                LowStockItems = stats.LowStockItems,
                FastMovingItems = stats.FastMovingItems,
                SlowMovingItems = stats.SlowMovingItems,
                TopSellingItems = topSellingItems.ToList(),
                Summary = $"Inventory report covering {periodFrom:yyyy-MM-dd} to {periodTo:yyyy-MM-dd}. " +
                         $"Total value: ${stats.TotalValue:C}, Low stock items: {stats.LowStockItems}"
            };

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate inventory report");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("logs")]
    public async Task<IActionResult> CreateAuditLog([FromBody] AuditLogDto log, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var auditId = Guid.NewGuid();
            
            var sql = @"
                INSERT INTO inventory.audit_logs 
                (audit_id, timestamp, entity_type, entity_id, action, user_id, user_name, details, old_values, new_values)
                VALUES 
                (@auditId, @timestamp, @entityType, @entityId, @action, @userId, @userName, @details, @oldValues, @newValues)";

            await conn.ExecuteAsync(sql, new
            {
                auditId,
                log.Timestamp,
                log.EntityType,
                entityId = Guid.Parse(log.EntityId),
                log.Action,
                log.UserId,
                log.UserName,
                log.Details,
                log.OldValues,
                log.NewValues
            });

            return CreatedAtAction(nameof(GetAuditLogs), new { }, new { id = auditId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}



