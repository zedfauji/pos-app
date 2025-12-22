namespace MagiDesk.Core.Entities;

public class Shift
{
    public Guid ShiftId { get; set; }
    public int ShiftNumber { get; set; }
    public int OpenedByUserId { get; set; }
    public string OpenedByName { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal StartingCash { get; set; }
    
    public int? ClosedByUserId { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    public decimal? DeclaredCash { get; set; }
    public decimal? ExpectedCash { get; set; }
    public decimal? Difference { get; set; }
    public string? DifferenceCategory { get; set; }
    public string? CloseReason { get; set; }
    
    public string Status { get; set; } // "open", "closed"
    public Guid? IdempotencyKey { get; set; }
}
