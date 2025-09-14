using MagiDesk.Shared.DTOs;

namespace InventoryApi.Repositories;

public interface ICashFlowRepository
{
    Task<List<CashFlow>> GetCashFlowHistoryAsync(CancellationToken ct = default);
    Task<CashFlow?> GetCashFlowByIdAsync(string id, CancellationToken ct = default);
    Task<string> AddCashFlowAsync(CashFlow entry, CancellationToken ct = default);
    Task<bool> UpdateCashFlowAsync(CashFlow entry, CancellationToken ct = default);
    Task<bool> DeleteCashFlowAsync(string id, CancellationToken ct = default);
}
