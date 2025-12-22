using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Core.Interfaces
{
    public interface ITableRepository
    {
        Task<IEnumerable<TableStatusDto>> GetAllAsync();
        Task<IEnumerable<SessionOverview>> GetActiveSessionsAsync();
        Task<SessionOverview?> GetActiveSessionByTableAsync(string tableLabel);
        Task<Guid> StartSessionAsync(string tableLabel, string serverId, string serverName);
        Task StopSessionAsync(Guid sessionId, DateTime endTime);
        Task<bool> IsTableOccupiedAsync(string tableLabel);
        Task MoveSessionAsync(Guid sessionId, string fromLabel, string toLabel);
        // Additional methods needed for logic
        Task<BillPreviewDto> GetBillPreviewAsync(string tableLabel);
        Task<SessionOverview?> GetSessionByIdAsync(Guid sessionId);
    }
}
