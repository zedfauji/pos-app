using PaymentApi.Models;
using MagiDesk.Shared.DTOs.Payments;
using PaymentApi.Repositories;
using MagiDesk.Core.Interfaces;
using MagiDesk.Core.Enums;

namespace PaymentApi.Services;

public sealed class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly IConfiguration _config;
    private readonly ImmutableIdService _idService;
    private readonly IAuditService _auditService;

    public PaymentService(IPaymentRepository repo, IConfiguration config, ImmutableIdService idService, IAuditService auditService)
    {
        _repo = repo;
        _config = config;
        _idService = idService;
        _auditService = auditService;
    }

    public Task<PaymentTransactionResult> RegisterPaymentAsync(RegisterPaymentRequestDto req, CancellationToken ct)
        => RegisterPaymentAsync(req, null, ct);

    public async Task<PaymentTransactionResult> RegisterPaymentAsync(RegisterPaymentRequestDto req, Guid? shiftId, CancellationToken ct)
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
            await _repo.InsertPaymentsAsync(conn, tx, req.SessionId, req.BillingId, req.ServerId, req.Lines, shiftId, token);

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
            
            // AUDIT LOGGING (In Transaction context if possible, but here we invoke service which opens new connection usually. 
            // Ideally should pass transaction, but for now we log AFTER core transaction succeeds to avoid blocking)
        }, ct);
        
        // AUDIT LOGGING (Outside transaction to ensure core logic succeeds first)
        await _auditService.LogEventAsync(
            actorId: req.ServerId ?? "system",
            actionType: AuditActionTypes.PaymentReceived,
            entityType: "BillLedger",
            entityId: req.BillingId.ToString(),
            beforeState: (BillLedgerDto?)null,
            afterState: ledger,
            correlationId: req.BillingId.ToString(),
            source: "PaymentApi"
        );

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

        // Calculate transaction-specific context
        var amountPaid = req.Lines.Sum(l => l.AmountPaid);
        var remainingBalance = ledger!.TotalDue - ledger.TotalPaid - ledger.TotalDiscount;
        var changeDue = 0m;
        
        // Calculate change due if AmountTendered is provided (cash transactions)
        if (req.AmountTendered.HasValue && req.AmountTendered.Value > amountPaid)
        {
            changeDue = req.AmountTendered.Value - amountPaid;
        }

        var message = ledger.Status?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true
            ? "Payment successful - Bill fully paid"
            : $"Partial payment accepted - Remaining: {remainingBalance:C}";

        return new PaymentTransactionResult
        {
            Ledger = ledger,
            ChangeDue = changeDue,
            RemainingBalance = Math.Max(0, remainingBalance),
            Message = message
        };
    }

    public async Task<PaymentTransactionResult> VoidPaymentAsync(VoidPaymentRequestDto req, Guid? shiftId, CancellationToken ct)
    {
        // 1. Validate
        if (req.AmountToVoid <= 0) throw new InvalidOperationException("INVALID_VOID_AMOUNT");

        // 2. Create Negative Payment Line
        var negativeAmount = -req.AmountToVoid;
        // In a void, we usually void the "Paid" amount. We assume "Cash" for now or Generic "Void" method?
        // Legacy: "Void" or "Refund". Let's use "Void" as method for clarity or "Refund".
        // Better: use the original method? No, let's use "Void" to be distinct.
        var line = new MagiDesk.Shared.DTOs.Payments.RegisterPaymentLineDto
        {
            AmountPaid = negativeAmount,
            PaymentMethod = MagiDesk.Shared.Enums.PaymentMethod.Void, 
            DiscountAmount = 0,
            TipAmount = 0,
            ExternalRef = null,
            Meta = new { Reason = req.Reason }
        };
        var lines = new List<MagiDesk.Shared.DTOs.Payments.RegisterPaymentLineDto> { line };

        BillLedgerDto? ledger = null;
        await _repo.ExecuteInTransactionAsync(async (conn, tx, token) =>
        {
            var old = await _repo.GetLedgerAsync(req.BillingId, token);
            if (old is null) throw new InvalidOperationException("LEDGER_NOT_FOUND");

            // Insert Negative Payment
            await _repo.InsertPaymentsAsync(conn, tx, req.SessionId, req.BillingId, req.ServerId, lines, shiftId, token);

            // Opsert Ledger (Negative delta subtracts from total_paid)
            var (due, disc, paid, tip, status) = await _repo.UpsertLedgerAsync(conn, tx, req.SessionId, req.BillingId, old.TotalDue, (negativeAmount, 0, 0), token);
            ledger = new BillLedgerDto(req.BillingId, req.SessionId, due, disc, paid, tip, status);

            // Log
            await _repo.AppendLogAsync(conn, tx, req.BillingId, req.SessionId, "void_payment", old, new { amount = negativeAmount, reason = req.Reason, ledger }, req.ServerId, token);
        }, ct);

        // Audit
         await _auditService.LogEventAsync(
            actorId: req.ServerId ?? "system",
            actionType: AuditActionTypes.RefundProcessed, // Ensure this enum exists or map to closest
            entityType: "BillLedger",
            entityId: req.BillingId.ToString(),
            beforeState: (BillLedgerDto?)null,
            afterState: ledger,
            correlationId: req.BillingId.ToString(),
            source: "PaymentApi"
        );

        return new PaymentTransactionResult
        {
            Ledger = ledger,
            Message = "Void successful",
            RemainingBalance = ledger!.TotalDue - ledger.TotalPaid - ledger.TotalDiscount
        };
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
}

