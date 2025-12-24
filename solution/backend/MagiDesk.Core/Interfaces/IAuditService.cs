using System;
using System.Threading.Tasks;
using MagiDesk.Core.Enums;

namespace MagiDesk.Core.Interfaces
{
    public interface IAuditService
    {
        Task LogEventAsync<T>(
            string actorId,
            string actionType,
            string entityType,
            string entityId,
            T? beforeState,
            T? afterState,
            string correlationId,
            string source = "API");

        Task LogEventAsync(
            string actorId,
            string actionType,
            string entityType,
            string entityId,
            string correlationId,
            string source = "API");
    }
}
