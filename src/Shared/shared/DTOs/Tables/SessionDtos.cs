namespace MagiDesk.Shared.DTOs.Tables;

public class SessionOverview
{
    public Guid SessionId { get; set; }
    public Guid? BillingId { get; set; }
    public string TableId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = "active";
    public int ItemsCount { get; set; }
    public decimal Total { get; set; }
}
