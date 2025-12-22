using System;
using System.Threading.Tasks;
using MagiDesk.Shared.DTOs.Reports;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Core.Interfaces
{
    public interface IBillingRepository
    {
        Task<Guid> CreateBillAsync(BillResult bill);
        Task<SalesStatsDto> GetDailySalesStatsAsync();
    }
}
