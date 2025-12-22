using System;
using System.Collections.Generic;

namespace MagiDesk.Client.Services.Dtos;

public class ValidateCredentialsRequest
{
    public string Pin { get; set; } = string.Empty;
}

public class SessionResult
{
    public Guid SessionId { get; set; }
    public string TableLabel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MoveResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FromTable { get; set; } = string.Empty;
    public string ToTable { get; set; } = string.Empty;
}

public class OrderItemRequest
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
