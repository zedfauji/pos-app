using MagiDesk.Core.Interfaces;
using MagiDesk.Core.Services;
using MagiDesk.Shared.DTOs.Tables;
using NSubstitute;
using MagiDesk.Shared.DTOs.Tables;
using NSubstitute;
using System.Collections.Generic;
using Xunit;

namespace MagiDesk.Core.Tests
{
    public class BillingServiceTests
    {
        private IBillingService _service;
        private IBillingRepository _mockRepo;

        public BillingServiceTests()
        {
            _mockRepo = Substitute.For<IBillingRepository>();
            _service = new BillingService(_mockRepo);
        }

        [Fact]
        public void CalculateTotal_Under10Minutes_ReturnsZeroTimeCost()
        {
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(9);
            var items = new List<ItemLine>();

            var (timeCost, _, total) = _service.CalculateTotal(start, end, items);

            Assert.Equal(0m, timeCost);
            Assert.Equal(0m, total);
        }

        [Fact]
        public void CalculateTotal_Exactly10Minutes_ReturnsZeroTimeCost()
        {
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(10);
            var items = new List<ItemLine>();

            var (timeCost, _, total) = _service.CalculateTotal(start, end, items);

            Assert.Equal(0m, timeCost);
        }

        [Fact]
        public void CalculateTotal_11Minutes_ReturnsCostFor1Minute()
        {
            // 11 mins total. 10 free. 1 min chargeable.
            // 70 / 60 = 1.1666... per min.
            // Cost for 1 min = 1.17 (rounded)
            
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(11);
            var items = new List<ItemLine>();

            var (timeCost, _, total) = _service.CalculateTotal(start, end, items);

            // 1 minute * (70/60) = 1.16666
            // Rounded to 2 decimals = 1.17
            Assert.Equal(1.17m, timeCost);
        }

        [Fact]
        public void CalculateTotal_70Minutes_ReturnsCostFor60Minutes()
        {
            // 70 mins total. 10 free. 60 chargeable.
            // 60 mins = 1 hour. Should be exactly $70.
            
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(70); 
            var items = new List<ItemLine>();

            var (timeCost, _, total) = _service.CalculateTotal(start, end, items);

            Assert.Equal(70.00m, timeCost);
        }

        [Fact]
        public void CalculateTotal_WithItems_AddsItemCost()
        {
            var start = DateTime.UtcNow;
            var end = start.AddMinutes(5); // Free time
            var items = new List<ItemLine> 
            {
                new ItemLine { price = 10.0m, quantity = 2 } // 20.0
            };

            var (timeCost, itemCost, total) = _service.CalculateTotal(start, end, items);

            Assert.Equal(0m, timeCost);
            Assert.Equal(20.0m, itemCost);
            Assert.Equal(20.0m, total);
        }
    }
}
