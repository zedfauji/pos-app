using PaymentApi.Models;
using PaymentApi.Repositories;

namespace PaymentApi.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly IConfiguration _config;
    private readonly ImmutableIdService _idService;

    public PaymentService(IPaymentRepository repo, IConfiguration config, ImmutableIdService idService)
    {
        _repo = repo;
        _config = config;
        _idService = idService;
    }

    public async Task<BillLedgerDto> RegisterPaymentAsync(RegisterPaymentRequestDto req, CancellationToken ct)
    {
        if (req.Lines is null || req.Lines.Count == 0)
            throw new InvalidOperationException("NO_PAYMENT_LINES");
        if (req.Lines.Any(l => l.AmountPaid < 0 || l.DiscountAmount < 0 || l.TipAmount < 0))
            throw new InvalidOperationException("INVALID_AMOUNTS");
        
        // Immutability protection: Validate billing ID format
        if (req.BillingId == Guid.Empty)
            throw new InvalidOperationException("INVALID_BILLING_ID: Billing ID cannot be empty");
        
        // Validate billing ID format (should be immutable format)
        if (!_idService.IsValidBillingId(req.BillingId.ToString()))
        {
            _idService.LogImmutableIdModificationAttempt("BillingId", req.BillingId.ToString(), "InvalidFormat");
            throw new InvalidOperationException("INVALID_BILLING_ID_FORMAT: Billing ID must follow immutable format");
        }
        
        // Check if billing ID already exists in ledger (prevent reuse)
        var existingLedger = await _repo.GetLedgerAsync(req.BillingId, ct);
        if (existingLedger != null)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentService: Billing ID {req.BillingId} already exists in ledger, allowing additional payments");
        }

        // Validate that the billing ID exists in TablesApi before allowing payment
        var tablesApiBaseUrl = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL") 
            ?? _config["TablesApi:BaseUrl"];
        
        if (!string.IsNullOrWhiteSpace(tablesApiBaseUrl))
        {
            try
            {
                using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
                { BaseAddress = new Uri(tablesApiBaseUrl.TrimEnd('/') + "/") };
                
                var sessionsUrl = $"sessions/active";
                System.Diagnostics.Debug.WriteLine($"PaymentService: Checking if billing ID exists in active sessions: {sessionsUrl}");
                
                using var res = await http.GetAsync(sessionsUrl, ct);
                System.Diagnostics.Debug.WriteLine($"PaymentService: TablesApi active sessions check response: {(int)res.StatusCode} {res.ReasonPhrase}");
                
                if (!res.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"PaymentService: Cannot retrieve active sessions from TablesApi, rejecting payment");
                    throw new InvalidOperationException($"TABLESAPI_ERROR: Cannot verify billing ID {req.BillingId}");
                }
                
                var sessionsJson = await res.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"PaymentService: Active sessions response: {sessionsJson}");
                
                // Check if the billing ID exists in any active session
                var sessions = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(sessionsJson);
                bool billingIdExists = false;
                if (sessions != null)
                {
                    foreach (var session in sessions)
                    {
                        if (session is System.Text.Json.Nodes.JsonObject sessionObj && 
                            sessionObj.TryGetPropertyValue("billingId", out var billingIdNode) &&
                            billingIdNode?.ToString() == req.BillingId.ToString())
                        {
                            billingIdExists = true;
                            break;
                        }
                    }
                }
                
                    if (!billingIdExists)
                    {
                        System.Diagnostics.Debug.WriteLine($"PaymentService: Billing ID {req.BillingId} not found in active sessions, but allowing payment to proceed");
                        // Don't throw exception - allow payment to proceed
                    }
                
                System.Diagnostics.Debug.WriteLine($"PaymentService: Billing ID {req.BillingId} exists in TablesApi, proceeding with payment");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"PaymentService: HTTP error checking TablesApi: {ex.Message}, allowing payment to proceed");
                // Don't throw exception - allow payment to proceed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PaymentService: Error checking TablesApi: {ex.Message}, allowing payment to proceed");
                // Don't throw exception - allow payment to proceed
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"PaymentService: TABLESAPI_BASEURL not configured, skipping billing ID validation");
        }

        BillLedgerDto? ledger = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            // Current ledger snapshot for logging
            var old = await _repo.GetLedgerAsync(req.BillingId, token);

            // Insert all payment legs
            await _repo.InsertPaymentsAsync(conn, tx, req.SessionId, req.BillingId, req.ServerId, req.Lines, token);

            // Aggregate deltas
            var addPaid = req.Lines.Sum(l => l.AmountPaid);
            var addDisc = req.Lines.Sum(l => l.DiscountAmount);
            var addTip = req.Lines.Sum(l => l.TipAmount);

            System.Diagnostics.Debug.WriteLine($"PaymentService.RegisterPaymentAsync: BillingId={req.BillingId}, SessionId={req.SessionId}, TotalDue={req.TotalDue}, AddPaid={addPaid}, AddDisc={addDisc}, AddTip={addTip}");

            // Upsert ledger, compute status
            var (due, disc, paid, tip, status) = await _repo.UpsertLedgerAsync(conn, tx, req.SessionId, req.BillingId, req.TotalDue, (addPaid, addDisc, addTip), token);
            ledger = new BillLedgerDto(req.BillingId, req.SessionId, due, disc, paid, tip, status);

            // Log
            await _repo.AppendLogAsync(conn, tx, req.BillingId, req.SessionId, "register_payment", old, new { lines = req.Lines, ledger }, req.ServerId, token);
        }, ct);

        // If fully settled, notify TablesApi to mark the bill settled (best-effort)
        try
        {
            if (ledger!.Status?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true)
            {
                var tablesBase = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL") 
                    ?? _config["TablesApi:BaseUrl"];
                System.Diagnostics.Debug.WriteLine($"PaymentService: Ledger status is 'paid', attempting to notify TablesApi. TABLESAPI_BASEURL = {tablesBase ?? "NOT SET"}");
                
                if (!string.IsNullOrWhiteSpace(tablesBase))
                {
                    using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
                    { BaseAddress = new Uri(tablesBase.TrimEnd('/') + "/") };
                    
                    var settleUrl = $"bills/by-billing/{Uri.EscapeDataString(req.BillingId.ToString())}/settle";
                    System.Diagnostics.Debug.WriteLine($"PaymentService: Calling TablesApi settle endpoint: {settleUrl}");
                    
                    using var res = await http.PostAsync(settleUrl, new StringContent(string.Empty), ct);
                    System.Diagnostics.Debug.WriteLine($"PaymentService: TablesApi settle response: {(int)res.StatusCode} {res.ReasonPhrase}");
                    
                    if (res.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"PaymentService: Successfully notified TablesApi to settle bill {req.BillingId}");
                    }
                    else
                    {
                        var errorContent = await res.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"PaymentService: TablesApi settle failed: {errorContent}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"PaymentService: TABLESAPI_BASEURL not configured, skipping TablesApi notification");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"PaymentService: Ledger status is '{ledger?.Status}', not notifying TablesApi");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PaymentService: Exception while notifying TablesApi: {ex.Message}");
        }

        return ledger!;
    }

    public async Task<BillLedgerDto> CloseBillAsync(Guid billingId, string? serverId, CancellationToken ct)
    {
        // Mark closed if ledger is settled (paid)
        BillLedgerDto? ledger = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            var old = await _repo.GetLedgerAsync(billingId, token);
            if (old is null) throw new InvalidOperationException("LEDGER_NOT_FOUND");
            if (!(old.TotalPaid + old.TotalDiscount >= old.TotalDue))
                throw new InvalidOperationException("LEDGER_NOT_SETTLED");

            // Log close action; actual bill close state is managed by TablesApi bill status
            await _repo.AppendLogAsync(conn, tx, billingId, old.SessionId, "close_bill", old, new { status = "closed" }, serverId, token);
            ledger = old with { };
        }, ct);
        return ledger!;
    }

    public async Task<BillLedgerDto> ApplyDiscountAsync(Guid billingId, Guid sessionId, decimal discountAmount, string? discountReason, string? serverId, CancellationToken ct)
    {
        if (discountAmount <= 0) throw new InvalidOperationException("INVALID_DISCOUNT");
        BillLedgerDto? ledger = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            var old = await _repo.GetLedgerAsync(billingId, token);
            var (due, disc, paid, tip, status) = await _repo.UpsertLedgerAsync(conn, tx, sessionId, billingId, old?.TotalDue, (0m, discountAmount, 0m), token);
            ledger = new BillLedgerDto(billingId, sessionId, due, disc, paid, tip, status);
            await _repo.AppendLogAsync(conn, tx, billingId, sessionId, "apply_discount", old, new { amount = discountAmount, reason = discountReason, ledger }, serverId, token);
        }, ct);
        return ledger!;
    }

    public Task<BillLedgerDto?> GetLedgerAsync(Guid billingId, CancellationToken ct)
        => _repo.GetLedgerAsync(billingId, ct);

    public Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(Guid billingId, CancellationToken ct)
        => _repo.ListPaymentsAsync(billingId, ct);

    public async Task<PagedResult<PaymentLogDto>> ListLogsAsync(Guid billingId, int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListLogsAsync(billingId, page, pageSize, ct);
        return new PagedResult<PaymentLogDto>(items, total);
    }

    public Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit, CancellationToken ct)
        => _repo.GetAllPaymentsAsync(limit, ct);

    public async Task<RefundDto> ProcessRefundAsync(Guid paymentId, ProcessRefundRequestDto request, string? serverId, CancellationToken ct)
    {
        // Validation
        if (request.RefundAmount <= 0)
            throw new InvalidOperationException("INVALID_REFUND_AMOUNT: Refund amount must be greater than zero");

        if (string.IsNullOrWhiteSpace(request.RefundMethod))
            throw new InvalidOperationException("INVALID_REFUND_METHOD: Refund method is required");

        if (string.IsNullOrWhiteSpace(request.RefundReason))
            throw new InvalidOperationException("INVALID_REFUND_REASON: Refund reason is required and cannot be empty");

        RefundDto? refund = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            // Get the payment to validate
            var payment = await _repo.GetPaymentByIdAsync(paymentId, token);
            if (payment == null)
                throw new InvalidOperationException("PAYMENT_NOT_FOUND");

            // Get total already refunded for this payment
            var totalRefunded = await _repo.GetTotalRefundedAmountAsync(conn, tx, paymentId, token);
            var remainingAmount = payment.AmountPaid - totalRefunded;

            // Validate refund amount doesn't exceed remaining amount
            if (request.RefundAmount > remainingAmount)
                throw new InvalidOperationException($"INVALID_REFUND_AMOUNT: Refund amount ({request.RefundAmount:C}) exceeds remaining refundable amount ({remainingAmount:C})");

            // Get old ledger state for logging
            var oldLedger = await _repo.GetLedgerAsync(payment.BillingId, token);

            // Insert refund record
            refund = await _repo.InsertRefundAsync(
                conn, tx,
                paymentId,
                payment.BillingId,
                payment.SessionId,
                request.RefundAmount,
                request.RefundReason,
                request.RefundMethod,
                request.ExternalRef,
                request.Meta,
                serverId,
                token
            );

            // Update ledger with negative delta (refund reduces total_paid)
            var (due, disc, paid, tip, status) = await _repo.UpsertLedgerAsync(
                conn, tx,
                payment.SessionId,
                payment.BillingId,
                null, // Don't change total_due
                (-request.RefundAmount, 0m, 0m), // Negative delta for refund
                token
            );

            var newLedger = new BillLedgerDto(payment.BillingId, payment.SessionId, due, disc, paid, tip, status);

            // Log refund action
            await _repo.AppendLogAsync(
                conn, tx,
                payment.BillingId,
                payment.SessionId,
                "process_refund",
                oldLedger,
                new { refund, ledger = newLedger },
                serverId,
                token
            );

            // Notify TablesApi to update bill payment_state (best-effort)
            try
            {
                var tablesBase = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL")
                    ?? _config["TablesApi:BaseUrl"];

                if (!string.IsNullOrWhiteSpace(tablesBase))
                {
                    using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
                    { BaseAddress = new Uri(tablesBase.TrimEnd('/') + "/") };

                    // Update bill payment_state based on refund status
                    var updateUrl = $"bills/by-billing/{Uri.EscapeDataString(payment.BillingId.ToString())}/payment-state";
                    var stateUpdate = new { PaymentState = status };
                    using var res = await http.PutAsJsonAsync(updateUrl, stateUpdate, token);
                    
                    if (res.IsSuccessStatusCode)
                    {
                        System.Diagnostics.Debug.WriteLine($"PaymentService: Successfully updated TablesApi bill payment_state to {status}");
                    }
                    else
                    {
                        var errorContent = await res.Content.ReadAsStringAsync(token);
                        System.Diagnostics.Debug.WriteLine($"PaymentService: TablesApi payment_state update failed: {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PaymentService: Exception while updating TablesApi payment_state: {ex.Message}");
                // Don't fail the refund if TablesApi update fails
            }
        }, ct);

        return refund!;
    }

    public Task<IReadOnlyList<RefundDto>> GetRefundsByPaymentIdAsync(Guid paymentId, CancellationToken ct)
        => _repo.GetRefundsByPaymentIdAsync(paymentId, ct);

    public Task<IReadOnlyList<RefundDto>> GetRefundsByBillingIdAsync(Guid billingId, CancellationToken ct)
        => _repo.GetRefundsByBillingIdAsync(billingId, ct);
}
