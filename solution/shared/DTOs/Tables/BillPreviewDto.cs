using System.Collections.Generic;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Shared.DTOs.Tables
{
    public class BillPreviewDto
    {
        public List<ItemLine> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
