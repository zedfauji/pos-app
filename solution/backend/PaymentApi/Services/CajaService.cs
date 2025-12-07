using PaymentApi.Models;
using PaymentApi.Repositories;
using MagiDesk.Shared.Authorization.Services;
using System.Net.Http;

namespace PaymentApi.Services;

public sealed class CajaService : ICajaService
{
    private readonly ICajaRepository _repository;
    private readonly IRbacService _rbacService;
    private readonly ILogger<CajaService> _logger;

    public CajaService(
        ICajaRepository repository,
        IRbacService rbacService,
        ILogger<CajaService> logger)
    {
        _repository = repository;
        _rbacService = rbacService;
        _logger = logger;
    }

    public async Task<CajaSessionDto> OpenCajaAsync(OpenCajaRequestDto request, string userId, CancellationToken ct = default)
    {
        // Validate permissions
        var canOpen = await _rbacService.HasPermissionAsync(userId, "caja:open", ct);
        if (!canOpen)
        {
            throw new UnauthorizedAccessException("No tiene permisos para abrir la caja. Se requiere permiso 'caja:open'.");
        }

        // Check for existing active session
        var existing = await _repository.GetActiveSessionAsync(ct);
        if (existing != null)
        {
            throw new InvalidOperationException($"Ya existe una caja abierta desde {existing.OpenedAt:g} por {existing.OpenedByUserId}. Debe cerrarla primero.");
        }

        // Validate opening amount
        if (request.OpeningAmount < 0)
        {
            throw new ArgumentException("El monto de apertura no puede ser negativo.", nameof(request.OpeningAmount));
        }

        // Create new session
        var session = new CajaSessionDto(
            Guid.NewGuid(),
            userId,
            null,
            DateTimeOffset.UtcNow,
            null,
            request.OpeningAmount,
            null,
            null,
            null,
            "open",
            request.Notes
        );

        var created = await _repository.OpenSessionAsync(session, ct);
        _logger.LogInformation("Caja session opened: {SessionId} by {UserId} with amount {Amount}", 
            created.Id, userId, request.OpeningAmount);

        return created;
    }

    public async Task<CajaSessionDto> CloseCajaAsync(CloseCajaRequestDto request, string userId, CancellationToken ct = default)
    {
        // Validate permissions
        var canClose = await _rbacService.HasPermissionAsync(userId, "caja:close", ct);
        if (!canClose)
        {
            throw new UnauthorizedAccessException("No tiene permisos para cerrar la caja. Se requiere permiso 'caja:close'.");
        }

        // Get active session
        var session = await _repository.GetActiveSessionAsync(ct);
        if (session == null)
        {
            throw new InvalidOperationException("No hay una caja abierta para cerrar.");
        }

        // Check for active table sessions (via TablesApi if configured)
        var tablesApiUrl = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL");
        if (!string.IsNullOrWhiteSpace(tablesApiUrl))
        {
            try
            {
                using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator })
                { BaseAddress = new Uri(tablesApiUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(5) };
                
                var response = await http.GetAsync("sessions/active", ct);
                if (response.IsSuccessStatusCode)
                {
                    var sessionsJson = await response.Content.ReadAsStringAsync(ct);
                    var sessions = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonArray>(sessionsJson);
                    
                    if (sessions != null && sessions.Count > 0)
                    {
                        throw new InvalidOperationException($"No puede cerrar la caja con {sessions.Count} mesa(s) abierta(s). Debe cerrar todas las sesiones de mesa primero.");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw validation errors
            }
            catch
            {
                // If TablesApi is unavailable, allow close (fail open for resilience)
                _logger.LogWarning("TablesApi unavailable, skipping active sessions check");
            }
        }

        // Validate closing amount
        if (request.ClosingAmount < 0)
        {
            throw new ArgumentException("El monto de cierre no puede ser negativo.", nameof(request.ClosingAmount));
        }

        // Calculate system total
        var systemTotal = await _repository.CalculateSystemTotalAsync(session.Id, ct);
        var expectedTotal = session.OpeningAmount + systemTotal;
        var difference = request.ClosingAmount - expectedTotal;

        // Check for large differences (require manager override)
        var absDifference = Math.Abs(difference);
        if (absDifference > 10m && absDifference > expectedTotal * 0.01m) // > $10 or > 1%
        {
            if (!request.ManagerOverride)
            {
                var hasManagerPermission = await _rbacService.HasPermissionAsync(userId, "caja:close", ct);
                if (!hasManagerPermission)
                {
                    throw new InvalidOperationException(
                        $"La diferencia es significativa (${difference:F2}). Se requiere autorización de gerente para cerrar.");
                }
            }
        }

        // Close session
        var closed = await _repository.CloseSessionAsync(
            session.Id,
            request.ClosingAmount,
            systemTotal,
            difference,
            userId,
            request.Notes,
            ct);

        _logger.LogInformation("Caja session closed: {SessionId} by {UserId}. Difference: {Difference}", 
            closed.Id, userId, difference);

        return closed;
    }

    public async Task<CajaSessionDto?> GetActiveSessionAsync(CancellationToken ct = default)
    {
        return await _repository.GetActiveSessionAsync(ct);
    }

    public async Task<CajaSessionDto?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _repository.GetSessionByIdAsync(sessionId, ct);
    }

    public async Task<CajaReportDto> GetSessionReportAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId, ct);
        if (session == null)
        {
            throw new KeyNotFoundException($"Caja session {sessionId} not found.");
        }

        var transactions = await _repository.GetTransactionsBySessionAsync(sessionId, ct);
        
        var salesTotal = transactions
            .Where(t => t.TransactionType == "sale")
            .Sum(t => t.Amount);
        
        var refundsTotal = transactions
            .Where(t => t.TransactionType == "refund")
            .Sum(t => t.Amount);
        
        var tipsTotal = transactions
            .Where(t => t.TransactionType == "tip")
            .Sum(t => t.Amount);
        
        var depositsTotal = transactions
            .Where(t => t.TransactionType == "deposit")
            .Sum(t => t.Amount);
        
        var withdrawalsTotal = transactions
            .Where(t => t.TransactionType == "withdrawal")
            .Sum(t => t.Amount);

        var systemTotal = session.SystemCalculatedTotal ?? await _repository.CalculateSystemTotalAsync(sessionId, ct);

        return new CajaReportDto(
            session.Id,
            session.OpenedAt,
            session.ClosedAt,
            session.OpeningAmount,
            session.ClosingAmount,
            systemTotal,
            session.Difference,
            transactions.Count,
            salesTotal,
            refundsTotal,
            tipsTotal,
            depositsTotal,
            withdrawalsTotal,
            session.OpenedByUserId,
            session.ClosedByUserId,
            session.Notes
        );
    }

    public async Task<(IReadOnlyList<CajaSessionSummaryDto> Items, int Total)> GetSessionsHistoryAsync(
        DateTimeOffset? startDate, 
        DateTimeOffset? endDate, 
        int page, 
        int pageSize, 
        CancellationToken ct = default)
    {
        return await _repository.GetSessionsAsync(startDate, endDate, page, pageSize, ct);
    }

    public async Task<bool> ValidateActiveSessionAsync(CancellationToken ct = default)
    {
        var session = await _repository.GetActiveSessionAsync(ct);
        return session != null;
    }
}
