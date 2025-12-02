namespace MagiDesk.Shared.DTOs.Tables;

public sealed record ReopenSessionResult(
    Guid SessionId,
    Guid BillingId,
    string TableLabel,
    string Message
);

