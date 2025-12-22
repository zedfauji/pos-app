using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MagiDesk.Core.Commands
{
    using MagiDesk.Shared.Enums;
    
    public record StopSessionCommand(
        Guid SessionId, 
        PaymentMethod PaymentMethod = PaymentMethod.Cash, 
        decimal AmountTendered = 0, 
        decimal TipAmount = 0, 
        decimal DiscountAmount = 0, 
        string? CustomerEmail = null
    );
    public record StopSessionResult(Guid BillingId, decimal TotalAmount, BillResult Bill);

    public class StopSessionCommandHandler : ICommandHandler<StopSessionCommand, StopSessionResult>
    {
        private readonly ITableRepository _tableRepo;
        private readonly IBillingRepository _billingRepo;
        private readonly IBillingService _billingService;
        private readonly IOrderIntegrationService _orderService; // To fetch items

        public StopSessionCommandHandler(
            ITableRepository tableRepo, 
            IBillingRepository billingRepo, 
            IBillingService billingService,
            IOrderIntegrationService orderService)
        {
            _tableRepo = tableRepo;
            _billingRepo = billingRepo;
            _billingService = billingService;
            _orderService = orderService;
        }

        public async Task<StopSessionResult> HandleAsync(StopSessionCommand command, CancellationToken cancellationToken = default)
        {
            // 1. Fetch Session
            var session = await _tableRepo.GetSessionByIdAsync(command.SessionId);
            if (session == null) throw new ArgumentException("Session not found");
            if (session.Status != "active") throw new InvalidOperationException("Session is not active");

            var endTime = DateTime.UtcNow;

            // 2. Fetch Items
            var items = await _orderService.GetOrderItemsForSessionAsync(command.SessionId);

            // 3. Calculate Costs
            var (timeCost, itemCost, rawTotal) = _billingService.CalculateTotal(session.StartTime, endTime, items);
            
            // Apply Discount
            var total = rawTotal - command.DiscountAmount;
            if (total < 0) total = 0;

            // Calculate Change
            var changeDue = 0m;
            if (command.PaymentMethod == PaymentMethod.Cash && command.AmountTendered > 0)
            {
                if (command.AmountTendered < total)
                {
                    throw new InvalidOperationException($"Insufficient payment. Total: {total:C}, Tendered: {command.AmountTendered:C}");
                }
                changeDue = command.AmountTendered - total;
            }

            // 4. Create Bill DTO
            var bill = new BillResult
            {
                BillId = Guid.NewGuid(),
                BillingId = session.BillingId,
                SessionId = session.SessionId,
                TableLabel = session.TableId,
                ServerName = session.ServerName,
                StartTime = session.StartTime,
                EndTime = endTime,
                TotalTimeMinutes = (int)(endTime - session.StartTime).TotalMinutes,
                Items = items,
                TimeCost = timeCost,
                ItemsCost = itemCost,
                TotalAmount = total,
                
                // Payment Info
                PaymentMethod = command.PaymentMethod,
                AmountTendered = command.AmountTendered,
                ChangeDue = changeDue,
                TipAmount = command.TipAmount,
                DiscountAmount = command.DiscountAmount,
                CustomerEmail = command.CustomerEmail
            };

            // 5. Persist Bill
            await _billingRepo.CreateBillAsync(bill);

            // 6. Stop Session (Updates DB to closed)
            await _tableRepo.StopSessionAsync(session.SessionId, endTime);
            
            // Email Simulation
            if (!string.IsNullOrEmpty(command.CustomerEmail))
            {
                Console.WriteLine($"[EmailService] Sending receipt to {command.CustomerEmail} for Bill {bill.BillId}");
            }

            // 8. Return Result
            return new StopSessionResult(bill.BillId, total, bill);
        }
    }
}
