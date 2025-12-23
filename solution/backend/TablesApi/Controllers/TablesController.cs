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

        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<MagiDesk.Shared.DTOs.Tables.TableTypeDto>>> GetTableTypes()
        {
            var types = await _repository.GetTableTypesAsync();
            return Ok(types);
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

        [HttpPost]
        public async Task<ActionResult<TableStatusDto>> CreateTable([FromBody] CreateTableRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _repository.AddTableAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                 return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TableStatusDto>> UpdateTable(Guid id, [FromBody] UpdateTableRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _repository.UpdateTableAsync(id, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTable(Guid id)
        {
            try
            {
                await _repository.DeleteTableAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("types/{id}")]
        public async Task<ActionResult<TableTypeDto>> UpdateTableType(int id, [FromBody] UpdateTableTypeRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _repository.UpdateTableTypeAsync(id, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
