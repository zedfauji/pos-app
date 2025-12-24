using System;
using System.Threading.Tasks;
using MagiDesk.Shared.DTOs.Reporting;

namespace MagiDesk.Core.Interfaces
{
    public interface IReportingRepository
    {
        Task<ZReportDto> GetZReportAsync(DateTime dateUtc);
        Task<ZReportDto> GetShiftReportAsync(Guid shiftId);
    }
}
