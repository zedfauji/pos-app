using MagiDesk.Core.Entities;
using MagiDesk.Core.Interfaces;

namespace MagiDesk.Infrastructure.Services;

public class ShiftService : IShiftService
{
    private readonly IShiftRepository _repository;

    public ShiftService(IShiftRepository repository)
    {
        _repository = repository;
    }

    public async Task<Shift?> GetCurrentOpenShiftAsync()
    {
        return await _repository.GetCurrentOpenShiftAsync();
    }

    public async Task<Shift> OpenShiftAsync(string userId, string userName, decimal startingCash, Guid idempotencyKey)
    {
        // 1. Check if open
        var existing = await _repository.GetCurrentOpenShiftAsync();
        if (existing != null)
        {
            if (existing.IdempotencyKey == idempotencyKey) return existing;
            throw new InvalidOperationException("A shift is already open.");
        }

        // 2. Create
        var newShift = new Shift
        {
            ShiftId = Guid.NewGuid(),
            OpenedByUserId = userId,
            OpenedByName = userName,
            OpenedAt = DateTime.UtcNow,
            StartingCash = startingCash,
            Status = "open",
            IdempotencyKey = idempotencyKey
        };

        await _repository.InsertAsync(newShift);
        return newShift;
    }

    public async Task<Shift> OpenSystemShiftAsync(string userId, string userName)
    {
        return await OpenShiftAsync(userId, userName, 0m, Guid.NewGuid());
    }

    public async Task<ShiftBlockers> GetShiftBlockersAsync(Guid shiftId)
    {
        var tables = await _repository.HasActiveTablesAsync(shiftId);
        var bills = await _repository.HasUnsettledBillsAsync(shiftId);
        
        // Count queries would be better but booleans verify the blocker condition
        return new ShiftBlockers(tables, bills, tables ? 1 : 0, bills ? 1 : 0);
    }

    public async Task<ShiftCloseResult> CloseShiftAsync(Guid shiftId, string userId, string userName, decimal declaredCash, Guid idempotencyKey, string? note)
    {
        var shift = await _repository.GetShiftByIdAsync(shiftId);
        if (shift == null) throw new ArgumentException("Shift not found");
        
        if (shift.Status == "closed")
        {
            if (shift.IdempotencyKey == idempotencyKey) 
                return new ShiftCloseResult(true, shift, Array.Empty<string>());
            throw new InvalidOperationException("Shift is already closed.");
        }

        // Validate
        var blockers = await GetShiftBlockersAsync(shiftId);
        if (blockers.HasActiveTables || blockers.HasUnsettledBills)
        {
            var errors = new List<string>();
            if (blockers.HasActiveTables) errors.Add("Active tables exist.");
            if (blockers.HasUnsettledBills) errors.Add("Unsettled bills exist.");
            return new ShiftCloseResult(false, shift, errors);
        }

        // Calculate functionality
        var cashSales = await _repository.GetCashSalesAsync(shiftId);
        var cashTips = await _repository.GetCashTipsAsync(shiftId);
        // var cashOuts = await _repository.GetCashOutsAsync(shiftId); // Removed for MVP if not in interface
        decimal cashOuts = 0;

        var expectedCash = shift.StartingCash + cashSales + cashTips - cashOuts;
        var difference = declaredCash - expectedCash;

        string diffCategory = difference switch
        {
            0 => "Perfect",
            > 0 => "Overage",
            < 0 => "Shortage"
        };

        // Update Entity
        shift.ClosedByUserId = userId;
        shift.ClosedByName = userName;
        shift.ClosedAt = DateTime.UtcNow;
        shift.DeclaredCash = declaredCash;
        shift.ExpectedCash = expectedCash;
        shift.Difference = difference;
        shift.DifferenceCategory = diffCategory;
        shift.CloseReason = note;
        shift.Status = "closed";

        await _repository.UpdateAsync(shift);
        
        return new ShiftCloseResult(true, shift, Array.Empty<string>());
    }

    public async Task<IEnumerable<Shift>> GetHistoryAsync(int limit, int offset)
    {
        return await _repository.GetShiftsHistoryAsync(limit, offset);
    }
}
