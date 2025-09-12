using Microsoft.AspNetCore.Mvc;
using PaymentApi.Models;
using PaymentApi.Services;

namespace PaymentApi.Controllers;

[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<BillLedgerDto>> RegisterAsync([FromBody] RegisterPaymentRequestDto req, CancellationToken ct)
    {
        try
        {
            var ledger = await _service.RegisterPaymentAsync(req, ct);
            // Avoid route link-generation issues across hosting environments
            return Created($"/api/payments/{req.BillingId}/ledger", ledger);
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

    [HttpPost("{billingId}/discounts")]
    public async Task<ActionResult<BillLedgerDto>> ApplyDiscountAsync([FromRoute] Guid billingId, [FromBody] decimal discountAmount, [FromQuery] Guid sessionId, [FromQuery] string? reason, [FromQuery] string? serverId, CancellationToken ct = default)
    {
        var ledger = await _service.ApplyDiscountAsync(billingId, sessionId, discountAmount, reason, serverId, ct);
        return Ok(ledger);
    }

    [HttpGet("{billingId}")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> ListPaymentsAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var list = await _service.ListPaymentsAsync(billingId, ct);
        return Ok(list);
    }

    [HttpGet("{billingId}/ledger")]
    public async Task<ActionResult<BillLedgerDto>> GetLedgerAsync([FromRoute] Guid billingId, CancellationToken ct)
    {
        var ledger = await _service.GetLedgerAsync(billingId, ct);
        if (ledger is null) return NotFound();
        return Ok(ledger);
        }

    [HttpGet("{billingId}/logs")]
    public async Task<ActionResult<PagedResult<PaymentLogDto>>> ListLogsAsync([FromRoute] Guid billingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _service.ListLogsAsync(billingId, page, pageSize, ct);
        return Ok(res);
    }

    [HttpPost("{billingId}/close")]
    public async Task<ActionResult<BillLedgerDto>> CloseAsync([FromRoute] Guid billingId, [FromQuery] string? serverId, CancellationToken ct)
    {
        var ledger = await _service.CloseBillAsync(billingId, serverId, ct);
        return Ok(ledger);
    }

    [HttpGet("{billingId}/receipt")]
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
}
