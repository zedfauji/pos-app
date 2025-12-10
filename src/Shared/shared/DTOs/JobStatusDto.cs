namespace MagiDesk.Shared.DTOs;

public class JobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g., syncProductNames
    public string Status { get; set; } = string.Empty; // running, completed, failed
    public int UpdatedCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Error { get; set; }
}
