using MagiDesk.Shared.DTOs;
using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Core.Interfaces
{
    public interface IOrderIntegrationService
    {
        Task PostOrderAsync(string tableLabel, string sessionId, List<OrderItemDto> items);
        Task<List<ItemLine>> GetOrderItemsForSessionAsync(Guid sessionId);
    }
}
