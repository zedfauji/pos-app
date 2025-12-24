using MagiDesk.Core.Interfaces;
using MagiDesk.Shared.DTOs;
using MagiDesk.Shared.DTOs.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Added for Task

namespace MagiDesk.Core.Services
{
    public class BillingService : IBillingService
    {
        // Note: Rules could be driven by config or DB
        private const decimal HourlyRate = 70.0m;
        private const int FreeMinutes = 10;
        
        private readonly IBillingRepository _repository;

        public BillingService(IBillingRepository repository)
        {
            _repository = repository;
        }

        public (decimal TimeCost, decimal ItemCost, decimal Total) CalculateTotal(DateTime startTime, DateTime endTime, IEnumerable<MagiDesk.Shared.DTOs.Tables.ItemLine> items)
        {
            // 1. Calculate Duration
            var duration = endTime - startTime;
            var totalMinutes = duration.TotalMinutes;

            // 2. Apply Grace Period Rule (First 10 mins free)
            var chargeableMinutes = Math.Max(0, totalMinutes - FreeMinutes);

            // 3. Calculate Time Cost
            // Rate per minute = 70 / 60
            decimal timeCost = 0;
            if (chargeableMinutes > 0)
            {
                 decimal ratePerMinute = HourlyRate / 60.0m;
                 timeCost = (decimal)chargeableMinutes * ratePerMinute;
            }
            
            // Round to 2 decimal places for currency
            timeCost = Math.Round(timeCost, 2);

            // 4. Calculate Item Cost
            decimal itemCost = 0;
            if (items != null)
            {
                itemCost = items.Sum(i => i.price * i.quantity);
            }

            // FINANCIAL PARITY: Tax Calculation
            // TODO: Load from Settings (Unified Tax Policy)
            const decimal TaxRate = 0.08m;
            
            var subtotal = timeCost + itemCost;
            var tax = Math.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
            var total = Math.Round(subtotal + tax, 2, MidpointRounding.AwayFromZero);

            return (timeCost, itemCost, total);
        }

        public async Task<MagiDesk.Shared.DTOs.Reports.SalesStatsDto> GetDailySalesStatsAsync()
        {
            return await _repository.GetDailySalesStatsAsync();
        }
    }
}
