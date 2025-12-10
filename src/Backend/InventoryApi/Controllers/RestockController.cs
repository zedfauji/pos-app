using MagiDesk.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using Npgsql;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestockController : ControllerBase
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<RestockController> _logger;

    public RestockController(NpgsqlDataSource dataSource, ILogger<RestockController> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RestockRequestDto>>> GetRestockRequests(
        [FromQuery] string? status = null,
        [FromQuery] string? vendorId = null,
        [FromQuery] int? priority = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT 
                    rr.request_id::text as RequestId,
                    rr.item_id::text as ItemId,
                    i.name as ItemName,
                    rr.requested_quantity as RequestedQuantity,
                    rr.created_by as CreatedBy,
                    rr.created_date as CreatedDate,
                    rr.status as Status,
                    rr.priority as Priority,
                    rr.notes as Notes,
                    rr.approved_by as ApprovedBy,
                    rr.approved_at as ApprovedAt,
                    rr.rejection_reason as RejectionReason,
                    rr.vendor_id::text as VendorId,
                    rr.unit_cost as UnitCost
                FROM inventory.restock_requests rr
                LEFT JOIN inventory.inventory_items i ON i.item_id = rr.item_id
                WHERE 1=1
                    AND (@status IS NULL OR rr.status = @status)
                    AND (@vendorId IS NULL OR rr.vendor_id::text = @vendorId)
                    AND (@priority IS NULL OR rr.priority = @priority)
                    AND (@search IS NULL OR i.name ILIKE @search OR rr.notes ILIKE @search)
                ORDER BY rr.created_date DESC
                LIMIT 1000";

            var requests = await conn.QueryAsync<RestockRequestDto>(sql, new 
            { 
                status, 
                vendorId, 
                priority, 
                search = search != null ? $"%{search}%" : null 
            });

            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get restock requests");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{requestId}")]
    public async Task<ActionResult<RestockRequestDto>> GetRestockRequest(string requestId, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                SELECT 
                    rr.request_id::text as RequestId,
                    rr.item_id::text as ItemId,
                    i.name as ItemName,
                    rr.requested_quantity as RequestedQuantity,
                    rr.created_by as CreatedBy,
                    rr.created_date as CreatedDate,
                    rr.status as Status,
                    rr.priority as Priority,
                    rr.notes as Notes,
                    rr.approved_by as ApprovedBy,
                    rr.approved_at as ApprovedAt,
                    rr.rejection_reason as RejectionReason,
                    rr.vendor_id::text as VendorId,
                    rr.unit_cost as UnitCost
                FROM inventory.restock_requests rr
                LEFT JOIN inventory.inventory_items i ON i.item_id = rr.item_id
                WHERE rr.request_id = @requestId";

            var request = await conn.QueryFirstOrDefaultAsync<RestockRequestDto>(sql, new { requestId });
            
            if (request == null)
                return NotFound();

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get restock request {RequestId}", requestId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<RestockRequestDto>> CreateRestockRequest([FromBody] RestockRequestDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var requestId = Guid.NewGuid().ToString();
            
            var sql = @"
                INSERT INTO inventory.restock_requests 
                (request_id, item_id, requested_quantity, created_by, created_date, status, priority, notes, vendor_id, unit_cost)
                VALUES 
                (@requestId, @itemId, @requestedQuantity, @createdBy, @createdDate, @status, @priority, @notes, @vendorId, @unitCost)
                RETURNING request_id::text as RequestId";

            var result = await conn.QuerySingleAsync<string>(sql, new
            {
                requestId,
                itemId = Guid.Parse(request.ItemId),
                request.RequestedQuantity,
                request.CreatedBy,
                request.CreatedDate,
                request.Status,
                request.Priority,
                request.Notes,
                vendorId = request.VendorId != null ? Guid.Parse(request.VendorId) : (Guid?)null,
                request.UnitCost
            });

            request.RequestId = result;
            return CreatedAtAction(nameof(GetRestockRequest), new { requestId = result }, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create restock request");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("{requestId}")]
    public async Task<IActionResult> UpdateRestockRequest(string requestId, [FromBody] RestockRequestDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.restock_requests 
                SET 
                    requested_quantity = @requestedQuantity,
                    status = @status,
                    priority = @priority,
                    notes = @notes,
                    approved_by = @approvedBy,
                    approved_at = @approvedAt,
                    rejection_reason = @rejectionReason,
                    vendor_id = @vendorId,
                    unit_cost = @unitCost
                WHERE request_id = @requestId";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                requestId,
                request.RequestedQuantity,
                request.Status,
                request.Priority,
                request.Notes,
                request.ApprovedBy,
                request.ApprovedAt,
                request.RejectionReason,
                vendorId = request.VendorId != null ? Guid.Parse(request.VendorId) : (Guid?)null,
                request.UnitCost
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update restock request {RequestId}", requestId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{requestId}")]
    public async Task<IActionResult> DeleteRestockRequest(string requestId, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = "DELETE FROM inventory.restock_requests WHERE request_id = @requestId";
            var rowsAffected = await conn.ExecuteAsync(sql, new { requestId });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete restock request {RequestId}", requestId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{requestId}/approve")]
    public async Task<IActionResult> ApproveRestockRequest(string requestId, [FromBody] ApproveRequestDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.restock_requests 
                SET 
                    status = 'Approved',
                    approved_by = @approvedBy,
                    approved_at = @approvedAt,
                    notes = COALESCE(@notes, notes)
                WHERE request_id = @requestId AND status = 'Pending'";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                requestId,
                request.ApprovedBy,
                request.ApprovedAt,
                request.Notes
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve restock request {RequestId}", requestId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{requestId}/reject")]
    public async Task<IActionResult> RejectRestockRequest(string requestId, [FromBody] RejectRequestDto request, CancellationToken ct)
    {
        try
        {
            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            
            var sql = @"
                UPDATE inventory.restock_requests 
                SET 
                    status = 'Cancelled',
                    rejection_reason = @rejectionReason,
                    notes = COALESCE(@notes, notes)
                WHERE request_id = @requestId AND status = 'Pending'";

            var rowsAffected = await conn.ExecuteAsync(sql, new
            {
                requestId,
                request.RejectionReason,
                request.Notes
            });

            if (rowsAffected == 0)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject restock request {RequestId}", requestId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ApproveRequestDto
{
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
    public string? Notes { get; set; }
}

public class RejectRequestDto
{
    public string RejectionReason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}



