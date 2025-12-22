using System;

namespace MagiDesk.Core.Entities
{
    public class InventoryItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReorderLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class InventoryTransaction
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public decimal ChangeAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
