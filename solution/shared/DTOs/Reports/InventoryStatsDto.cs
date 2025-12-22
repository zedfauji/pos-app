namespace MagiDesk.Shared.DTOs.Reports
{
    public class InventoryStatsDto
    {
        public int TotalItemCount { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; } // Quantity * Cost? We don't have Cost yet. Just Placeholder.
    }
}
