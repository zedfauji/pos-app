using MagiDesk.Core.Entities;

namespace MagiDesk.Core.Interfaces;

public interface IShiftRepository
{
    Task<Shift?> GetCurrentOpenShiftAsync();
    Task<Shift?> GetShiftByIdAsync(Guid shiftId);
    Task<IEnumerable<Shift>> GetShiftsHistoryAsync(int limit, int offset);
    
    Task InsertAsync(Shift shift);
    Task UpdateAsync(Shift shift);
    
    Task<bool> HasActiveTablesAsync(Guid shiftId);
    Task<bool> HasUnsettledBillsAsync(Guid shiftId);
    
    // Reporting support
    Task<decimal> GetCashSalesAsync(Guid shiftId);
    Task<decimal> GetCashTipsAsync(Guid shiftId); // If tracking tips separately
}
