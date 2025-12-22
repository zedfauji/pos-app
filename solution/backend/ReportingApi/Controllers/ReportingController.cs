using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Reporting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ReportingApi.Controllers
{
    [ApiController]
    [Route("api/reporting")]
    public class ReportingController : ControllerBase
    {
        private readonly IReportingRepository _repo;

        public ReportingController(IReportingRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("z-report")]
        public async Task<ActionResult<ZReportDto>> GetZReport()
        {
            // Default to "Now" on the server.
            // In a real system, we might accept a ?date=... query param.
            // For MVP Phase 1, we defined it as "Current Operational Day".
            
            var now = DateTime.UtcNow;
            var report = await _repo.GetZReportAsync(now);
            return Ok(report);
        }
    }
}
