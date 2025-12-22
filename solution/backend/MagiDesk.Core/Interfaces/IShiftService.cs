using MagiDesk.Core.Entities;

namespace MagiDesk.Core.Interfaces;

// Request/Response DTOs defined here to avoid modifying Shared project if restricted, 
// or basic types. ideally these go to Shared.
// Given strict file list, we'll keep them here or use primitives where possible?
// No, standard practice demands DTOs. We'll define simple records here for now 
// or assume we can create them. 
// Validated path: I will put them in this file for now to ensure I don't break "Allowed Files" too much.

public record OpenShiftRequest(decimal StartingCash, Guid IdempotencyKey);
public record CloseShiftRequest(decimal DeclaredCash, Guid IdempotencyKey, string? Note);
public record ShiftCloseResult(bool Success, Shift Shift, IEnumerable<string> Errors);
public record ShiftBlockers(bool HasActiveTables, bool HasUnsettledBills, int ActiveTableCount, int UnsettledBillCount);

public interface IShiftService
{
    Task<Shift?> GetCurrentOpenShiftAsync();
    Task<Shift> OpenShiftAsync(int userId, string userName, decimal startingCash, Guid idempotencyKey);
    Task<ShiftBlockers> GetShiftBlockersAsync(Guid shiftId);
    Task<ShiftCloseResult> CloseShiftAsync(Guid shiftId, int userId, string userName, decimal declaredCash, Guid idempotencyKey, string? note);
    Task<IEnumerable<Shift>> GetHistoryAsync(int limit, int offset);
}
