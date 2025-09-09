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
        var ledger = await _service.RegisterPaymentAsync(req, ct);
        return CreatedAtAction(nameof(GetLedgerAsync), new { billingId = req.BillingId }, ledger);
    }

    [HttpPost("{billingId}/discounts")]
    public async Task<ActionResult<BillLedgerDto>> ApplyDiscountAsync([FromRoute] string billingId, [FromBody] decimal discountAmount, [FromQuery] string sessionId, [FromQuery] string? reason, [FromQuery] string? serverId, CancellationToken ct = default)
    {
        var ledger = await _service.ApplyDiscountAsync(billingId, sessionId, discountAmount, reason, serverId, ct);
        return Ok(ledger);
    }

    [HttpGet("{billingId}")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> ListPaymentsAsync([FromRoute] string billingId, CancellationToken ct)
    {
        var list = await _service.ListPaymentsAsync(billingId, ct);
        return Ok(list);
    }

    [HttpGet("{billingId}/ledger")]
    public async Task<ActionResult<BillLedgerDto>> GetLedgerAsync([FromRoute] string billingId, CancellationToken ct)
    {
        var ledger = await _service.GetLedgerAsync(billingId, ct);
        if (ledger is null) return NotFound();
        return Ok(ledger);
        }

    [HttpGet("{billingId}/logs")]
    public async Task<ActionResult<PagedResult<PaymentLogDto>>> ListLogsAsync([FromRoute] string billingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var res = await _service.ListLogsAsync(billingId, page, pageSize, ct);
        return Ok(res);
    }

    [HttpPost("{billingId}/close")]
    public async Task<ActionResult<BillLedgerDto>> CloseAsync([FromRoute] string billingId, [FromQuery] string? serverId, CancellationToken ct)
    {
        var ledger = await _service.CloseBillAsync(billingId, serverId, ct);
        return Ok(ledger);
    }
}
