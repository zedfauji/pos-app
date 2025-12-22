using System;

namespace MagiDesk.Shared.DTOs.Inventory
{
    public class InventoryItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateInventoryItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal InitialQuantity { get; set; }
        public decimal ReorderLevel { get; set; }
    }

    public class AdjustStockDto
    {
        public decimal ChangeAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
