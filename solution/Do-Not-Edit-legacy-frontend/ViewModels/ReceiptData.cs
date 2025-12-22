using System.Collections.Generic;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class ReceiptData
    {
        public long OrderId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemLineVm> Items { get; set; } = new();
    }
}
