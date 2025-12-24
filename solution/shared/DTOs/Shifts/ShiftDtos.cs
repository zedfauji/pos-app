using System;

namespace MagiDesk.Shared.DTOs.Shifts;

public class ShiftDto
{
    public Guid ShiftId { get; set; }
    public int ShiftNumber { get; set; }
    public string OpenedByUserId { get; set; }
    public string OpenedByName { get; set; }
    public DateTime OpenedAt { get; set; }
    public decimal StartingCash { get; set; }
    public string Status { get; set; } // "open", "closed"
}

public class ShiftValidationDto
{
    public bool CanOperate { get; set; }
    public Guid? CurrentShiftId { get; set; }
    public int? ShiftNumber { get; set; }
    public string Status { get; set; }
}

public class OpenShiftRequest
{
    public decimal StartingCash { get; set; }
    public string Note { get; set; }
    public Guid? IdempotencyKey { get; set; }
}

public class CloseShiftRequest
{
    public decimal DeclaredCash { get; set; }
    public string Note { get; set; }
    public Guid? IdempotencyKey { get; set; }
}

public class ShiftCloseResultDto
{
    public bool Success { get; set; }
    public ShiftDto Shift { get; set; }
    public string[] Errors { get; set; }
}
