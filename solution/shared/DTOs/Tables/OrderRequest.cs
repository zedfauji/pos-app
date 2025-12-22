using System.Collections.Generic;

namespace MagiDesk.Shared.DTOs.Tables
{
    public class OrderRequest
    {
        public List<MagiDesk.Shared.DTOs.OrderItemDto> Items { get; set; } = new();
    }
}
