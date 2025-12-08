using Microsoft.AspNetCore.Mvc;
using PaymentApi.Models;
using PaymentApi.Services;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.Authorization.Attributes;

namespace PaymentApi.Controllers.V2;

/// <summary>
/// Version 2 Payments Controller with RBAC enforcement
/// All endpoints require specific permissions
/// </summary>
[ApiController]
[Route("api/v2/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService service, ILogger<PaymentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Register payment (requires payment:process permission)
    /// </summary>
    [HttpPost]
    [RequiresPermission(Permissions.PAYMENT_PROCESS)]
    public async Task<ActionResult<BillLedgerDto>> RegisterAsync([FromBody] RegisterPaymentRequestDto req, CancellationToken ct)
    {
        try
        {
            var ledger = await _service.RegisterPaymentAsync(req, ct);
            return Created($"/api/v2/payments/{req.BillingId}/ledger", ledger);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("INVALID_BILLING_ID_FORMAT"))
        {
            return BadRequest(new { error = "INVALID_BILLING_ID_FORMAT", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("INVALID_BILLING_ID"))
        {
            return BadRequest(new { error = "INVALID_BILLING_ID", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("TABLESAPI_UNAVAILABLE") || ex.Message.StartsWith("TABLESAPI_ERROR"))
        {
            return BadRequest(new { error = "VALIDATION_ERROR", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("NO_PAYMENT_LINES") || ex.Message.StartsWith("INVALID_AMOUNTS"))
        {
            return BadRequest(new { error = "INVALID_REQUEST", message = ex.Message });
        }
    }

    /// <summary>
    /// Apply discount (requires payment:process permission)
    /// </summary>
    [HttpPost("{billingId}/discounts")]
    [RequiresPermission(Permissions.PAYMENT_PROCESS)]
    public async Task<ActionResult<BillLedgerDto>> ApplyDiscountAsync([FromRoute] Guid billingId, [FromBody] decimal discountAmount, [FromQuery] Guid sessionId, [FromQuery] string? reason, [FromQuery] string? serverId, CancellationToken ct = default)
    {
        var ledger = await _service.ApplyDiscountAsync(billingId, sessionId, discountAmount, reason, serverId, ct);
        return Ok(ledger);
    }

    /// <summary>
    /// Get all payments (requires payment:view permission)
    /// </summary>
    [HttpGet("all")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> GetAllPaymentsAsync([FromQuery] int limit = 1000, CancellationToken ct = default)
    {
        try
        {
            var payments = await _service.GetAllPaymentsAsync(limit, ct);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// List payments by billing ID (requires payment:view permission)
    /// </summary>
    [HttpGet("{billingId}")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> ListPaymentsAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var list = await _service.ListPaymentsAsync(billingId, ct);
        return Ok(list);
    }

    /// <summary>
    /// Get bill ledger (requires payment:view permission)
    /// </summary>
    [HttpGet("{billingId}/ledger")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<BillLedgerDto>> GetLedgerAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var ledger = await _service.GetLedgerAsync(billingId, ct);
        if (ledger is null) return NotFound();
        return Ok(ledger);
    }

    /// <summary>
    /// Get payment logs (requires payment:view permission)
    /// </summary>
    [HttpGet("{billingId}/logs")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<PaymentApi.Models.PagedResult<PaymentLogDto>>> ListLogsAsync([FromRoute] Guid billingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _service.ListLogsAsync(billingId, page, pageSize, ct);
        return Ok(res);
    }

    /// <summary>
    /// Close bill (requires payment:process permission)
    /// </summary>
    [HttpPost("{billingId}/close")]
    [RequiresPermission(Permissions.PAYMENT_PROCESS)]
    public async Task<ActionResult<BillLedgerDto>> CloseAsync([FromRoute] Guid billingId, [FromQuery] string? serverId, CancellationToken ct)
    {
        var ledger = await _service.CloseBillAsync(billingId, serverId, ct);
        return Ok(ledger);
    }

    /// <summary>
    /// Get receipt data (requires payment:view permission)
    /// </summary>
    [HttpGet("{billingId}/receipt")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<object>> GetReceiptDataAsync(Guid billingId, CancellationToken ct)
    {
        try
        {
            var ledger = await _service.GetLedgerAsync(billingId, ct);
            if (ledger == null)
            {
                return NotFound(new { error = "LEDGER_NOT_FOUND", message = $"Ledger not found for billing ID: {billingId}" });
            }

            // Get business settings (in a real implementation, this would come from SettingsApi)
            var businessName = Environment.GetEnvironmentVariable("BUSINESS_NAME") ?? "MagiDesk Billiard Club";
            var businessAddress = Environment.GetEnvironmentVariable("BUSINESS_ADDRESS") ?? "123 Main Street, City, State 12345";
            var businessPhone = Environment.GetEnvironmentVariable("BUSINESS_PHONE") ?? "(555) 123-4567";

            var receiptData = new
            {
                BusinessName = businessName,
                BusinessAddress = businessAddress,
                BusinessPhone = businessPhone,
                TableNumber = ledger.SessionId, // Using session ID as table identifier
                ServerName = "Server", // This should come from session data
                StartTime = DateTime.Now.AddHours(-2), // This should come from session data
                EndTime = DateTime.Now,
                BillId = billingId,
                SessionId = ledger.SessionId,
                Subtotal = ledger.TotalDue,
                DiscountAmount = ledger.TotalDiscount,
                TaxAmount = ledger.TotalDue * 0.08m, // 8% tax
                TotalAmount = ledger.TotalDue,
                TotalPaid = ledger.TotalPaid,
                TotalTip = ledger.TotalTip,
                Status = ledger.Status,
                Items = new[] // This would come from order data in a real implementation
                {
                    new { Name = "Billiard Session", Quantity = 1, UnitPrice = ledger.TotalDue, Subtotal = ledger.TotalDue }
                },
                Timestamp = DateTimeOffset.UtcNow
            };

            return Ok(receiptData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Process refund (requires payment:refund permission)
    /// </summary>
    [HttpPost("payment/{paymentId}/refund")]
    [RequiresPermission(Permissions.PAYMENT_REFUND)]
    public async Task<ActionResult<RefundDto>> ProcessRefundAsync([FromRoute] Guid paymentId, [FromBody] ProcessRefundRequestDto request, [FromQuery] string? serverId, CancellationToken ct)
    {
        try
        {
            var refund = await _service.ProcessRefundAsync(paymentId, request, serverId, ct);
            return Ok(refund);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("PAYMENT_NOT_FOUND"))
        {
            return NotFound(new { error = "PAYMENT_NOT_FOUND", message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("INVALID_REFUND_AMOUNT") || ex.Message.StartsWith("INVALID_REFUND_METHOD") || ex.Message.StartsWith("INVALID_REFUND_REASON"))
        {
            return BadRequest(new { error = "INVALID_REQUEST", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Get refunds by payment ID (requires payment:view permission)
    /// </summary>
    [HttpGet("payment/{paymentId}/refunds")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<IReadOnlyList<RefundDto>>> GetRefundsByPaymentIdAsync([FromRoute] Guid paymentId, CancellationToken ct)
    {
        try
        {
            var refunds = await _service.GetRefundsByPaymentIdAsync(paymentId, ct);
            return Ok(refunds);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Get refunds by billing ID (requires payment:view permission)
    /// </summary>
    [HttpGet("{billingId}/refunds")]
    [RequiresPermission(Permissions.PAYMENT_VIEW)]
    public async Task<ActionResult<IReadOnlyList<RefundDto>>> GetRefundsByBillingIdAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        try
        {
            var refunds = await _service.GetRefundsByBillingIdAsync(billingId, ct);
            return Ok(refunds);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INTERNAL_ERROR", message = ex.Message });
        }
    }
}

