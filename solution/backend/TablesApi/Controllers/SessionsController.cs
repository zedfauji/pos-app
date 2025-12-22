using MagiDesk.Core.Commands;
using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Shared.DTOs.Reports;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace TablesApi.Controllers
{
    [ApiController]
    [Route("tables")]
    public class SessionsController : ControllerBase
    {
        private readonly ITableRepository _repository;
        private readonly IOrderIntegrationService _orderService;
        private readonly IBillingService _billingService;
        private readonly ICommandHandler<StopSessionCommand, StopSessionResult> _stopSessionHandler;

        public SessionsController(
            ITableRepository repository, 
            IOrderIntegrationService orderService, 
            IBillingService billingService,
            ICommandHandler<StopSessionCommand, StopSessionResult> stopSessionHandler)
        {
            _repository = repository;
            _orderService = orderService;
            _billingService = billingService;
            _stopSessionHandler = stopSessionHandler;
        }

        [HttpPost("{label}/start")]
        [TablesApi.Filters.RequireOpenShift]
        public async Task<IActionResult> StartSession(string label, [FromBody] StartSessionRequest request)
        {
            try 
            {
                var sessionId = await _repository.StartSessionAsync(label, request.ServerId, request.ServerName);
                return Ok(new { session_id = sessionId }); 
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        
        [HttpPost("{label}/order")]
        [TablesApi.Filters.RequireOpenShift]
        public async Task<IActionResult> PostOrder(string label, [FromBody] MagiDesk.Shared.DTOs.Tables.OrderRequest request)
        {
             var session = await _repository.GetActiveSessionByTableAsync(label);
             if (session == null) return NotFound(new { message = "No active session for table" });

             await _orderService.PostOrderAsync(label, session.SessionId.ToString(), request.Items);
             return Ok(); 
        }

        [HttpPost("{label}/move")]
        public async Task<IActionResult> MoveSession(string label, [FromQuery] string to)
        {
             var session = await _repository.GetActiveSessionByTableAsync(label);
             if (session == null) return NotFound(new { message = "No active session for table" });

             try
             {
                 await _repository.MoveSessionAsync(session.SessionId, label, to);
                 return Ok(new { success = true, fromTable = label, toTable = to });
             }
             catch (Exception ex)
             {
                 return BadRequest(new { message = ex.Message });
             }
        }

        [HttpGet("{label}/items")]
        public async Task<ActionResult<List<MagiDesk.Shared.DTOs.Tables.ItemLine>>> GetItems(string label)
        {
             var session = await _repository.GetActiveSessionByTableAsync(label);
             // If no active session, we might return empty list or NotFound. 
             // Returning empty list is safer for UI, but NotFound is more semantically correct if the table is closed.
             // Given the client usually calls this when viewing details, empty list or NotFound are both plausible.
             // However, for valid logic, if there is no session, there are no items. 
             if (session == null) return Ok(new List<MagiDesk.Shared.DTOs.Tables.ItemLine>());

             var items = await _orderService.GetOrderItemsForSessionAsync(session.SessionId);
             return Ok(items);
        }

        [HttpPost("{sessionId}/stop")]
        [TablesApi.Filters.RequireOpenShift]
        public async Task<IActionResult> StopSession(Guid sessionId, [FromBody] StopSessionRequest request)
        {
             try 
             {
                 var command = new StopSessionCommand(sessionId)
                 {
                     PaymentMethod = request.PaymentMethod,
                     AmountTendered = request.AmountTendered,
                     TipAmount = request.TipAmount,
                     DiscountAmount = request.DiscountAmount,
                     CustomerEmail = request.CustomerEmail
                 };

                 var result = await _stopSessionHandler.HandleAsync(command);
                 return Ok(result.Bill);
             }
             catch(ArgumentException ex) { return NotFound(ex.Message); }
             catch(InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
             catch(Exception ex) { return StatusCode(500, ex.Message); }
        }
        
        [HttpPost("{sessionId}/end")]
        [TablesApi.Filters.RequireOpenShift]
        public async Task<IActionResult> EndSession(Guid sessionId)
        {
            try
            {
                await _repository.EndSessionAsync(sessionId);
                return Ok(new { success = true, sessionId = sessionId, status = "ended", message = "Session ended, bill created." });
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<MagiDesk.Shared.DTOs.Reports.SalesStatsDto>> GetStats()
        {
            var stats = await _billingService.GetDailySalesStatsAsync();
            return Ok(stats);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessions()
        {
            var sessions = await _repository.GetActiveSessionsAsync();
            return Ok(sessions);
        }

        [HttpPost("{label}/calculate-split")]

        public async Task<ActionResult<MagiDesk.Shared.DTOs.Tables.CalculateSplitResult>> CalculateSplit(
            string label, 
            [FromBody] MagiDesk.Shared.DTOs.Tables.CalculateSplitRequest request)
        {
            var session = await _repository.GetActiveSessionByTableAsync(label);
            if (session == null) return NotFound(new { message = "No active session for table" });

            var items = await _orderService.GetOrderItemsForSessionAsync(session.SessionId);
            
            // Calculate based on the request type
            decimal amountToPay;
            List<MagiDesk.Shared.DTOs.Tables.ItemLine> includedItems = new();

            if (request.ItemIds != null && request.ItemIds.Count > 0)
            {
                // Split by specific items
                includedItems = items.Where(i => request.ItemIds.Contains(i.itemId)).ToList();
                amountToPay = includedItems.Sum(i => i.price * i.quantity);
            }
            else if (request.Percentage.HasValue)
            {
                // Split by percentage
                var total = items.Sum(i => i.price * i.quantity);
                amountToPay = Math.Round(total * (request.Percentage.Value / 100m), 2);
                includedItems = items.ToList(); // Include all items for display
            }
            else if (request.FixedAmount.HasValue)
            {
                // Validate fixed amount
                amountToPay = request.FixedAmount.Value;
                includedItems = items.ToList(); // Include all items for display
            }
            else
            {
                // Default to full bill
                amountToPay = items.Sum(i => i.price * i.quantity);
                includedItems = items.ToList();
            }

            var result = new MagiDesk.Shared.DTOs.Tables.CalculateSplitResult
            {
                AmountToPay = Math.Round(amountToPay, 2),
                TaxShare = 0, // TODO: Implement tax calculation if needed
                SuggestedGratuity = Math.Round(amountToPay * 0.15m, 2), // 15% suggested tip
                Items = includedItems
            };

            return Ok(result);
        }

    }
}
