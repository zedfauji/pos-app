using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TablesApi.Controllers
{
    [ApiController]
    [Route("tables")]
    public class TablesController : ControllerBase
    {
        private readonly ITableRepository _repository;

        public TablesController(ITableRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            var tables = await _repository.GetAllAsync();
            return Ok(tables);
        }

        [HttpGet("{label}/bill-preview")]
        public async Task<ActionResult<BillPreviewDto>> GetBillPreview(string label)
        {
            var preview = await _repository.GetBillPreviewAsync(label);
            return Ok(preview);
        }
    }
}
