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
        if (string.IsNullOrWhiteSpace(req.BillingId))
            throw new InvalidOperationException("INVALID_BILLING_ID: Billing ID cannot be null or empty");
        
        // Validate billing ID format (should be immutable format)
        if (!_idService.IsValidBillingId(req.BillingId))
        {
            _idService.LogImmutableIdModificationAttempt("BillingId", req.BillingId, "InvalidFormat");
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
                            billingIdNode?.ToString() == req.BillingId)
                        {
                            billingIdExists = true;
                            break;
                        }
                    }
                }
                
                if (!billingIdExists)
                {
                    System.Diagnostics.Debug.WriteLine($"PaymentService: Billing ID {req.BillingId} not found in active sessions, rejecting payment");
                    throw new InvalidOperationException($"BILL_NOT_FOUND: Billing ID {req.BillingId} does not exist in active sessions");
                }
                
                System.Diagnostics.Debug.WriteLine($"PaymentService: Billing ID {req.BillingId} exists in TablesApi, proceeding with payment");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"PaymentService: HTTP error checking TablesApi: {ex.Message}");
                throw new InvalidOperationException($"TABLESAPI_UNAVAILABLE: Cannot verify billing ID {req.BillingId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PaymentService: Error checking TablesApi: {ex.Message}");
                throw new InvalidOperationException($"TABLESAPI_ERROR: Cannot verify billing ID {req.BillingId}");
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
                    
                    var settleUrl = $"bills/by-billing/{Uri.EscapeDataString(req.BillingId)}/settle";
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

    public async Task<BillLedgerDto> CloseBillAsync(string billingId, string? serverId, CancellationToken ct)
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

    public async Task<BillLedgerDto> ApplyDiscountAsync(string billingId, string sessionId, decimal discountAmount, string? discountReason, string? serverId, CancellationToken ct)
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

    public Task<BillLedgerDto?> GetLedgerAsync(string billingId, CancellationToken ct)
        => _repo.GetLedgerAsync(billingId, ct);

    public Task<IReadOnlyList<PaymentDto>> ListPaymentsAsync(string billingId, CancellationToken ct)
        => _repo.ListPaymentsAsync(billingId, ct);

    public async Task<PagedResult<PaymentLogDto>> ListLogsAsync(string billingId, int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await _repo.ListLogsAsync(billingId, page, pageSize, ct);
        return new PagedResult<PaymentLogDto>(items, total);
    }

    public Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync(int limit, CancellationToken ct)
        => _repo.GetAllPaymentsAsync(limit, ct);
}
