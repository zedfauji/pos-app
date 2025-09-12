using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorOrdersController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<VendorOrdersController> _logger;

    public VendorOrdersController(NpgsqlDataSource dataSource, ILogger<VendorOrdersController> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorOrderDto>>> GetVendorOrders(
        [FromQuery] string? vendorId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? priority = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT 
                    vo.order_id::text as OrderId,
                    vo.vendor_id::text as VendorId,
                    v.name as VendorName,
                    vo.order_date as OrderDate,
                    vo.expected_delivery_date as ExpectedDeliveryDate,
                    vo.actual_delivery_date as ActualDeliveryDate,
                    vo.status as Status,
                    vo.priority as Priority,
                    vo.total_value as TotalValue,
                    vo.item_count as ItemCount,
                    vo.notes as Notes,
                    vo.created_by as CreatedBy,
                    vo.sent_by as SentBy,
                    vo.sent_date as SentDate,
                    vo.confirmed_by as ConfirmedBy,
                    vo.confirmed_date as ConfirmedDate,
                    vo.tracking_number as TrackingNumber,
                    vo.delivery_days as DeliveryDays
                FROM inventory.vendor_orders vo
                LEFT JOIN inventory.vendors v ON v.vendor_id = vo.vendor_id
                WHERE 1=1
                    AND (@vendorId IS NULL OR vo.vendor_id::text = @vendorId)
                    AND (@status IS NULL OR vo.status = @status)
                    AND (@priority IS NULL OR vo.priority = @priority)
                    AND (@search IS NULL OR v.name ILIKE @search OR vo.notes ILIKE @search)
                ORDER BY vo.order_date DESC
                LIMIT 1000";

            var orders = await conn.QueryAsync<VendorOrderDto>(sql, new 
            { 
                vendorId, 
                status, 
                priority, 
                search = search != null ? $"%{search}%" : null 
            });

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor orders");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<VendorOrderDto>> GetVendorOrder(string orderId, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT 
                    vo.order_id::text as OrderId,
                    vo.vendor_id::text as VendorId,
                    v.name as VendorName,
                    vo.order_date as OrderDate,
                    vo.expected_delivery_date as ExpectedDeliveryDate,
                    vo.actual_delivery_date as ActualDeliveryDate,
                    vo.status as Status,
                    vo.priority as Priority,
                    vo.total_value as TotalValue,
                    vo.item_count as ItemCount,
                    vo.notes as Notes,
                    vo.created_by as CreatedBy,
                    vo.sent_by as SentBy,
                    vo.sent_date as SentDate,
                    vo.confirmed_by as ConfirmedBy,
                    vo.confirmed_date as ConfirmedDate,
                    vo.tracking_number as TrackingNumber,
                    vo.delivery_days as DeliveryDays
                FROM inventory.vendor_orders vo
                LEFT JOIN inventory.vendors v ON v.vendor_id = vo.vendor_id
                WHERE vo.order_id = @orderId";

            var order = await conn.QueryFirstOrDefaultAsync<VendorOrderDto>(sql, new { orderId });
            
            if (order == null)
                return NotFound();

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vendor order {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<VendorOrderDto>> CreateVendorOrder([FromBody] VendorOrderDto order, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var orderId = Guid.NewGuid().ToString();
            
            var sql = @"
                INSERT INTO inventory.vendor_orders 
                (order_id, vendor_id, order_date, expected_delivery_date, status, priority, total_value, item_count, notes, created_by)
                VALUES 
                (@orderId, @vendorId, @orderDate, @expectedDeliveryDate, @status, @priority, @totalValue, @itemCount, @notes, @createdBy)
                RETURNING order_id::text as OrderId";

            var result = await conn.QuerySingleAsync<string>(sql, new
            {
                orderId,
                vendorId = Guid.Parse(order.VendorId),
                order.OrderDate,
                order.ExpectedDeliveryDate,
                order.Status,
                order.Priority,
                order.TotalValue,
                order.ItemCount,
                order.Notes,
                order.CreatedBy
            });

            order.OrderId = result;
            return CreatedAtAction(nameof(GetVendorOrder), new { orderId = result }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vendor order");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateVendorOrder(string orderId, [FromBody] VendorOrderDto order, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.vendor_orders 
                SET 
                    expected_delivery_date = @expectedDeliveryDate,
                    actual_delivery_date = @actualDeliveryDate,
                    status = @status,
                    priority = @priority,
                    total_value = @totalValue,
                    item_count = @itemCount,
                    notes = @notes,
                    sent_by = @sentBy,
                    sent_date = @sentDate,
                    confirmed_by = @confirmedBy,
                    confirmed_date = @confirmedDate,
                    tracking_number = @trackingNumber,
                    delivery_days = @deliveryDays
                WHERE order_id = @orderId";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                orderId,
                order.ExpectedDeliveryDate,
                order.ActualDeliveryDate,
                order.Status,
                order.Priority,
                order.TotalValue,
                order.ItemCount,
                order.Notes,
                order.SentBy,
                order.SentDate,
                order.ConfirmedBy,
                order.ConfirmedDate,
                order.TrackingNumber,
                order.DeliveryDays
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vendor order {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{orderId}")]
    public async Task<IActionResult> DeleteVendorOrder(string orderId, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = "DELETE FROM inventory.vendor_orders WHERE order_id = @orderId";
            var rowsAffected = await conn.ExecuteAsync(sql, new { orderId });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vendor order {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{orderId}/send")]
    public async Task<IActionResult> SendVendorOrder(string orderId, [FromBody] SendOrderDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.vendor_orders 
                SET 
                    status = 'Sent',
                    sent_by = @sentBy,
                    sent_date = @sentDate,
                    notes = COALESCE(@notes, notes)
                WHERE order_id = @orderId AND status = 'Draft'";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                orderId,
                request.SentBy,
                request.SentDate,
                request.Notes
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send vendor order {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{orderId}/confirm")]
    public async Task<IActionResult> ConfirmVendorOrder(string orderId, [FromBody] ConfirmOrderDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.vendor_orders 
                SET 
                    status = 'Confirmed',
                    confirmed_by = @confirmedBy,
                    confirmed_date = @confirmedDate,
                    tracking_number = @trackingNumber,
                    notes = COALESCE(@notes, notes)
                WHERE order_id = @orderId AND status = 'Sent'";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                orderId,
                request.ConfirmedBy,
                request.ConfirmedDate,
                request.TrackingNumber,
                request.Notes
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm vendor order {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{orderId}/deliver")]
    public async Task<IActionResult> DeliverVendorOrder(string orderId, [FromBody] DeliverOrderDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.vendor_orders 
                SET 
                    status = 'Delivered',
                    actual_delivery_date = @actualDeliveryDate,
                    delivery_days = @deliveryDays,
                    notes = COALESCE(@notes, notes)
                WHERE order_id = @orderId AND status IN ('Sent', 'Confirmed', 'Shipped')";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                orderId,
                request.ActualDeliveryDate,
                request.DeliveryDays,
                request.Notes
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver vendor order {OrderId}", orderId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class SendOrderDto
{
    public string SentBy { get; set; } = string.Empty;
    public DateTime SentDate { get; set; }
    public string? Notes { get; set; }
}

public class ConfirmOrderDto
{
    public string ConfirmedBy { get; set; } = string.Empty;
    public DateTime ConfirmedDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Notes { get; set; }
}

public class DeliverOrderDto
{
    public DateTime ActualDeliveryDate { get; set; }
    public int DeliveryDays { get; set; }
    public string? Notes { get; set; }
}



