using MagiDesk.Shared.Enums;

namespace MagiDesk.Shared.DTOs.Tables
{
    public class StopSessionRequest
    {
        public PaymentMethod PaymentMethod { get; set; }
        public decimal AmountTendered { get; set; }
        public decimal TipAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? CustomerEmail { get; set; }
    }
}
