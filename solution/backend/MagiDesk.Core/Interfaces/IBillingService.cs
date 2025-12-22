using MagiDesk.Shared.DTOs;
using System;
using System.Collections.Generic;

namespace MagiDesk.Core.Interfaces
{
    public interface IBillingService
    {
        /// <summary>
        /// Calculates the total cost for a session based on duration and items.
        /// </summary>
        /// <returns>(TimeCost, ItemCost, Total)</returns>
        (decimal TimeCost, decimal ItemCost, decimal Total) CalculateTotal(DateTime startTime, DateTime endTime, IEnumerable<MagiDesk.Shared.DTOs.Tables.ItemLine> items);

        System.Threading.Tasks.Task<MagiDesk.Shared.DTOs.Reports.SalesStatsDto> GetDailySalesStatsAsync();
    }
}
