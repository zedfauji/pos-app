namespace MagiDesk.Shared.DTOs.Reports
{
    public class SalesStatsDto
    {
        public decimal TotalSalesToday { get; set; }
        public int ClosedSessionsToday { get; set; }
        public int OpenSessionsCount { get; set; }
    }
}
