using MagiDesk.Shared.DTOs.Reporting;
using Refit;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services;

public interface IReportingApi
{
    [Get("/api/reporting/z-report")]
    Task<ZReportDto> GetZReportAsync();
}
