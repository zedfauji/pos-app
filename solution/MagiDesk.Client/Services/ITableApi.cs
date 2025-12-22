using Refit;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Shared.DTOs.Reports;
using MagiDesk.Client.Services.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace MagiDesk.Client.Services;

public interface ITableApi
{
    [Get("/tables/types")]
    Task<List<TableTypeDto>> GetTableTypesAsync(CancellationToken ct = default);

    [Get("/tables")]
    Task<List<TableStatusDto>> GetTablesAsync(CancellationToken ct = default);

    [Post("/tables/{label}/start")]
    Task<IApiResponse<SessionResult>> StartSessionAsync(string label, [Body] StartSessionRequest request, CancellationToken ct = default);

    // The original StopSessionAsync was:
    // [Post("/tables/{label}/stop")]
    // Task<IApiResponse<BillResult>> StopSessionAsync(string label, CancellationToken ct = default);
    // It is being replaced/updated by the provided snippet.
    [Post("/tables/{sessionId}/stop")]
    Task<ApiResponse<BillResult>> StopSessionAsync(Guid sessionId, [Body] StopSessionRequest request, CancellationToken ct = default);

    [Post("/tables/{label}/move")]
    Task<IApiResponse<MoveSessionResult>> MoveSessionAsync(string label, [Query] string to, [Query] bool force = false, CancellationToken ct = default);

    [Get("/tables/{label}/items")]
    Task<List<ItemLine>> GetItemsAsync(string label, CancellationToken ct = default);

    [Post("/tables/{label}/order")]
    Task<IApiResponse<object>> PostOrderAsync(string label, [Body] OrderRequest request, CancellationToken ct = default);

    [Get("/tables/stats")]
    Task<MagiDesk.Shared.DTOs.Reports.SalesStatsDto> GetStatsAsync(CancellationToken ct = default);

    [Get("/tables/{label}/bill-preview")]
    Task<BillPreviewDto> GetBillPreviewAsync(string label, CancellationToken ct = default);

    [Get("/tables/active")]
    Task<List<SessionOverview>> GetActiveSessionsAsync(CancellationToken ct = default);

    [Post("/tables/{label}/calculate-split")]
    Task<CalculateSplitResult> CalculateSplitAsync(string label, [Body] CalculateSplitRequest request, CancellationToken ct = default);

    [Post("/tables/{sessionId}/end")]
    Task<IApiResponse> EndSessionAsync(Guid sessionId, CancellationToken ct = default);

    // Payment Hub - Bills endpoints
    [Get("/bills/unsettled")]
    Task<List<BillDto>> GetUnsettledBillsAsync(CancellationToken ct = default);

    [Get("/bills/{billId}")]
    Task<BillDto> GetBillAsync(Guid billId, CancellationToken ct = default);

    // Settle an unsettled bill (mark as Paid)
    [Post("/bills/{billId}/settle")]
    Task<IApiResponse> SettleBillAsync(Guid billId, [Body] SettleBillRequest request, CancellationToken ct = default);
}

// Defining local wrapper DTOs if they aren't in Shared (or assuming they are consistent)
// StartSessionRequest is likely in Shared based on Program.cs usages.
// BillResult and SessionResult were created anonymously in Program.cs.
// I might need to define them in Client if Shared doesn't have them.
