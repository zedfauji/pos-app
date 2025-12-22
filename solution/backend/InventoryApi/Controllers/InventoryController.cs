using MagiDesk.Core.Entities;
using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Inventory;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryApi.Controllers
{
    [ApiController]
    [Route("inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryRepository _repository;

        public InventoryController(IInventoryRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetItems()
        {
            var items = await _repository.GetAllAsync();
            var dtos = items.Select(i => new InventoryItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Unit = i.Unit,
                Quantity = i.Quantity,
                ReorderLevel = i.ReorderLevel,
                UpdatedAt = i.UpdatedAt
            });
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItem(long id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();
            
            return Ok(new InventoryItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Unit = item.Unit,
                Quantity = item.Quantity,
                ReorderLevel = item.ReorderLevel,
                UpdatedAt = item.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemDto request)
        {
            var item = new InventoryItem
            {
                Name = request.Name,
                Unit = request.Unit,
                Quantity = request.InitialQuantity,
                ReorderLevel = request.ReorderLevel
            };

            var id = await _repository.CreateAsync(item);
            
            // Also log initial stock transaction if quantity > 0
            if (request.InitialQuantity > 0)
            {
                await _repository.AddTransactionAsync(new InventoryTransaction
                {
                    ItemId = id,
                    ChangeAmount = request.InitialQuantity,
                    Reason = "Initial Stock"
                });
            }

            return Ok(new { id });
        }

        [HttpPost("{id}/adjust")]
        public async Task<IActionResult> AdjustStock(long id, [FromBody] AdjustStockDto request)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();

            item.Quantity += request.ChangeAmount;
            
            // Allow negative stock? For now, yes, logic might be corrected later or strictly enforced.
            // If we enforce >= 0:
            // if (item.Quantity < 0) return BadRequest("Insufficient stock");

            await _repository.UpdateAsync(item);
            await _repository.AddTransactionAsync(new InventoryTransaction
            {
                ItemId = id,
                ChangeAmount = request.ChangeAmount,
                Reason = request.Reason
            });

            return Ok(new { success = true, newQuantity = item.Quantity });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(long id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null) return NotFound();

            await _repository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("stats")]
        public async Task<ActionResult<MagiDesk.Shared.DTOs.Reports.InventoryStatsDto>> GetStats()
        {
            var stats = await _repository.GetStatsAsync();
            return Ok(stats);
        }
    }
}
