using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;

namespace TablesApi.Controllers;

/// <summary>
/// Controller for bill-related operations in the Payment Hub.
/// Handles FINANCIAL data only - no operational logic.
/// </summary>
[ApiController]
[Route("[controller]")]
public class BillsController : ControllerBase
{
    private readonly string _connectionString;
    private readonly ILogger<BillsController> _logger;

    public BillsController(IConfiguration configuration, ILogger<BillsController> logger)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing Postgres connection string");
        _logger = logger;
    }

    /// <summary>
    /// Get all unsettled bills (status = 'AwaitingPayment').
    /// Used by Payment Hub to display bills needing settlement.
    /// </summary>
    [HttpGet("unsettled")]
    public async Task<IActionResult> GetUnsettledBills()
    {
        const string sql = @"
            SELECT 
                b.bill_id AS BillId,
                b.billing_id AS BillingId,
                b.session_id AS SessionId,
                b.table_id AS TableId,
                COALESCE(b.table_label, ts.table_label, 'Unknown') AS TableLabel,
                COALESCE(b.server_name, ts.server_name, 'N/A') AS ServerName,
                ts.start_time AS StartTime,
                ts.end_time AS EndTime,
                b.items_total AS ItemsTotal,
                b.time_total AS TimeTotal,
                b.subtotal AS Subtotal,
                b.discounts AS Discounts,
                b.tax AS Tax,
                b.total_amount AS TotalAmount,
                b.time_minutes AS TimeMinutes,
                b.status AS Status,
                b.created_at AS CreatedAt
            FROM billing.bills b
            LEFT JOIN public.""TableSessions"" ts ON ts.session_id = b.session_id
            WHERE b.status = 'AwaitingPayment'
            ORDER BY b.created_at DESC";

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var bills = await conn.QueryAsync<BillDto>(sql);
            _logger.LogInformation("GetUnsettledBills returned {Count} bills", bills.Count());
            return Ok(bills);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unsettled bills");
            return StatusCode(500, "Failed to retrieve unsettled bills");
        }
    }

    /// <summary>
    /// Get a single bill by ID.
    /// </summary>
    [HttpGet("{billId:guid}")]
    public async Task<IActionResult> GetBill(Guid billId)
    {
        const string sql = @"
            SELECT 
                b.bill_id AS BillId,
                b.billing_id AS BillingId,
                b.session_id AS SessionId,
                b.table_id AS TableId,
                COALESCE(b.table_label, ts.table_label, 'Unknown') AS TableLabel,
                COALESCE(b.server_name, ts.server_name, 'N/A') AS ServerName,
                ts.start_time AS StartTime,
                ts.end_time AS EndTime,
                b.items_total AS ItemsTotal,
                b.time_total AS TimeTotal,
                b.subtotal AS Subtotal,
                b.discounts AS Discounts,
                b.tax AS Tax,
                b.total_amount AS TotalAmount,
                b.time_minutes AS TimeMinutes,
                b.status AS Status,
                b.created_at AS CreatedAt
            FROM billing.bills b
            LEFT JOIN public.""TableSessions"" ts ON ts.session_id = b.session_id
            WHERE b.bill_id = @BillId";

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var bill = await conn.QuerySingleOrDefaultAsync<BillDto>(sql, new { BillId = billId });
            
            if (bill == null)
                return NotFound($"Bill {billId} not found");
                
            return Ok(bill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get bill {BillId}", billId);
            return StatusCode(500, "Failed to retrieve bill");
        }
    }

    /// <summary>
    /// Settle an unsettled bill (mark as Paid).
    /// Used by Payment Workspace to complete payment for bills from ended sessions.
    /// </summary>
    [HttpPost("{billId:guid}/settle")]
    public async Task<IActionResult> SettleBill(Guid billId, [FromBody] SettleBillRequest request)
    {
        const string sql = @"
            UPDATE billing.bills 
            SET status = 'Paid', 
                settled_at = @Now,
                updated_at = @Now
            WHERE bill_id = @BillId 
              AND status = 'AwaitingPayment'
            RETURNING bill_id";

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var updated = await conn.QuerySingleOrDefaultAsync<Guid?>(sql, new { 
                BillId = billId, 
                Now = DateTimeOffset.UtcNow 
            });
            
            if (updated == null)
            {
                return BadRequest(new { message = "Bill not found or already settled" });
            }
            
            _logger.LogInformation("Bill {BillId} settled with method {Method}", billId, request.PaymentMethod);
            
            return Ok(new { 
                message = "Bill settled successfully",
                billId = billId,
                paymentMethod = request.PaymentMethod
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to settle bill {BillId}", billId);
            return StatusCode(500, "Failed to settle bill");
        }
    }
}

public record SettleBillRequest
{
    public string PaymentMethod { get; init; } = "cash";
    public decimal AmountTendered { get; init; }
    public decimal TipAmount { get; init; }
    public decimal DiscountAmount { get; init; }
}

/// <summary>
/// DTO for bill data returned to Payment Hub.
/// </summary>
public record BillDto
{
    public Guid BillId { get; init; }
    public Guid BillingId { get; init; }
    public Guid SessionId { get; init; }
    public Guid TableId { get; init; }
    public string? TableLabel { get; init; }
    public string? ServerName { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public decimal ItemsTotal { get; init; }
    public decimal TimeTotal { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discounts { get; init; }
    public decimal Tax { get; init; }
    public decimal TotalAmount { get; init; }
    public int TimeMinutes { get; init; }
    public string Status { get; init; } = "AwaitingPayment";
    public DateTimeOffset CreatedAt { get; init; }
}
