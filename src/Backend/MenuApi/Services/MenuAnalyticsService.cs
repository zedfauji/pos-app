using MenuApi.Models;
using MenuApi.Repositories;

namespace MenuApi.Services;

public sealed class MenuAnalyticsService : IMenuAnalyticsService
{
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<MenuAnalyticsService> _logger;

    public MenuAnalyticsService(IMenuRepository menuRepository, ILogger<MenuAnalyticsService> logger)
    {
        _menuRepository = menuRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MenuAnalyticsDto>> GetMenuAnalyticsAsync(MenuAnalyticsQueryDto query, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting menu analytics for period {FromDate} to {ToDate}", query.FromDate, query.ToDate);
            
            // This would typically query order data, but for now we'll simulate with menu data
            var menuItems = await _menuRepository.ListItemsAsync(new MenuItemQueryDto(Q: null, Category: null, Group: null, AvailableOnly: null), ct);
            
            var analytics = new List<MenuAnalyticsDto>();
            
            foreach (var item in menuItems.Items)
            {
                // Simulate analytics data - in real implementation, this would come from order/transaction data
                var analyticsItem = new MenuAnalyticsDto
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    Category = item.Category,
                    TotalRevenue = CalculateSimulatedRevenue(item),
                    TotalOrders = CalculateSimulatedOrders(item),
                    TotalQuantitySold = CalculateSimulatedQuantity(item),
                    AverageOrderValue = item.SellingPrice,
                    ProfitMargin = CalculateSimulatedProfitMargin(item),
                    CostOfGoodsSold = CalculateSimulatedCOGS(item),
                    LastSold = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)),
                    FirstSold = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
                    IsTopSeller = Random.Shared.NextDouble() > 0.7,
                    Ranking = Random.Shared.Next(1, 100),
                    ConversionRate = (decimal)(Random.Shared.NextDouble()),
                    AveragePreparationTime = TimeSpan.FromMinutes(Random.Shared.Next(5, 30)),
                    CustomerRating = Random.Shared.Next(3, 6),
                    ReviewCount = Random.Shared.Next(0, 50)
                };
                
                analytics.Add(analyticsItem);
            }
            
            // Apply filters and sorting
            var filteredAnalytics = ApplyAnalyticsFilters(analytics, query);
            
            return filteredAnalytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu analytics");
            throw;
        }
    }

    public async Task<IReadOnlyList<MenuPerformanceDto>> GetMenuPerformanceAsync(MenuPerformanceQueryDto query, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting menu performance for period {FromDate} to {ToDate}", query.FromDate, query.ToDate);
            
            var performance = new List<MenuPerformanceDto>();
            var currentDate = query.FromDate;
            
            while (currentDate <= query.ToDate)
            {
                var nextDate = query.GroupBy switch
                {
                    "Week" => currentDate.AddDays(7),
                    "Month" => currentDate.AddMonths(1),
                    _ => currentDate.AddDays(1)
                };
                
                // Simulate performance data
                var performanceItem = new MenuPerformanceDto
                {
                    Date = currentDate,
                    Category = query.Category ?? "All",
                    Revenue = CalculateSimulatedDailyRevenue(currentDate),
                    Orders = Random.Shared.Next(10, 100),
                    QuantitySold = Random.Shared.Next(50, 500),
                    AverageOrderValue = (decimal)Random.Shared.NextDouble() * 50 + 10,
                    ProfitMargin = (decimal)(Random.Shared.NextDouble()) * 0.4m + 0.1m,
                    UniqueCustomers = Random.Shared.Next(5, 50),
                    CustomerSatisfactionScore = (decimal)Random.Shared.NextDouble() * 2 + 3
                };
                
                performance.Add(performanceItem);
                currentDate = nextDate;
            }
            
            return performance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu performance");
            throw;
        }
    }

    public async Task<IReadOnlyList<MenuTrendDto>> GetMenuTrendsAsync(MenuTrendQueryDto query, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting menu trends for period {FromDate} to {ToDate}", query.FromDate, query.ToDate);
            
            var trends = new List<MenuTrendDto>();
            var categories = query.Categories.Any() ? query.Categories : new List<string> { "Appetizers", "Beverages", "Entrees", "Sides" };
            
            foreach (var category in categories)
            {
                var trend = new MenuTrendDto
                {
                    Category = category,
                    Trend = GetRandomTrend(),
                    ChangePercentage = (decimal)Random.Shared.NextDouble() * 0.5m - 0.25m, // -25% to +25%
                    PeriodDays = (int)(query.ToDate - query.FromDate).TotalDays,
                    RevenueChange = (decimal)Random.Shared.NextDouble() * 1000 - 500,
                    OrderChange = Random.Shared.Next(-50, 50),
                    Recommendation = GetTrendRecommendation(category)
                };
                
                trends.Add(trend);
            }
            
            return trends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu trends");
            throw;
        }
    }

    public async Task<IReadOnlyList<MenuInsightDto>> GetMenuInsightsAsync(MenuInsightQueryDto query, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting menu insights");
            
            var insights = new List<MenuInsightDto>
            {
                new MenuInsightDto
                {
                    Type = "Performance",
                    Title = "Top Performing Category",
                    Description = "Beverages category shows 25% increase in revenue this month",
                    Priority = "Medium",
                    ActionRequired = "Consider expanding beverage offerings",
                    GeneratedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, object> { ["category"] = "Beverages", ["increase"] = 0.25m }
                },
                new MenuInsightDto
                {
                    Type = "Trend",
                    Title = "Declining Item Alert",
                    Description = "Classic Burger shows 15% decrease in orders",
                    Priority = "High",
                    ActionRequired = "Review pricing and promotion strategy",
                    GeneratedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, object> { ["item"] = "Classic Burger", ["decrease"] = 0.15m }
                },
                new MenuInsightDto
                {
                    Type = "Recommendation",
                    Title = "Price Optimization Opportunity",
                    Description = "French Fries could increase price by 10% without affecting demand",
                    Priority = "Low",
                    ActionRequired = "Test price increase in limited market",
                    GeneratedAt = DateTime.UtcNow,
                    Data = new Dictionary<string, object> { ["item"] = "French Fries", ["suggestedIncrease"] = 0.10m }
                }
            };
            
            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu insights");
            throw;
        }
    }

    public async Task<MenuAnalyticsDto> GetItemAnalyticsAsync(long menuItemId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        try
        {
            var item = await _menuRepository.GetItemAsync(menuItemId, ct);
            if (item == null)
            {
                throw new ArgumentException($"Menu item {menuItemId} not found");
            }
            
            // Simulate detailed analytics for a single item
            return new MenuAnalyticsDto
            {
                MenuItemId = item.Item.Id,
                Name = item.Item.Name,
                Category = item.Item.Category,
                TotalRevenue = CalculateSimulatedRevenue(item.Item),
                TotalOrders = CalculateSimulatedOrders(item.Item),
                TotalQuantitySold = CalculateSimulatedQuantity(item.Item),
                AverageOrderValue = item.Item.SellingPrice,
                ProfitMargin = CalculateSimulatedProfitMargin(item.Item),
                CostOfGoodsSold = CalculateSimulatedCOGS(item.Item),
                LastSold = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 7)),
                FirstSold = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
                IsTopSeller = Random.Shared.NextDouble() > 0.8,
                Ranking = Random.Shared.Next(1, 20),
                ConversionRate = (decimal)(Random.Shared.NextDouble()),
                AveragePreparationTime = TimeSpan.FromMinutes(Random.Shared.Next(5, 20)),
                CustomerRating = Random.Shared.Next(4, 6),
                ReviewCount = Random.Shared.Next(5, 30)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item analytics for {MenuItemId}", menuItemId);
            throw;
        }
    }

    public async Task<Dictionary<string, object>> GetMenuDashboardDataAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        try
        {
            var analytics = await GetMenuAnalyticsAsync(new MenuAnalyticsQueryDto 
            { 
                FromDate = fromDate, 
                ToDate = toDate 
            }, ct);
            
            var performance = await GetMenuPerformanceAsync(new MenuPerformanceQueryDto
            {
                FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30),
                ToDate = toDate ?? DateTime.UtcNow,
                GroupBy = "Day"
            }, ct);
            
            var insights = await GetMenuInsightsAsync(new MenuInsightQueryDto(), ct);
            
            return new Dictionary<string, object>
            {
                ["totalRevenue"] = analytics.Sum(a => a.TotalRevenue),
                ["totalOrders"] = analytics.Sum(a => a.TotalOrders),
                ["totalItems"] = analytics.Count,
                ["topSeller"] = analytics.OrderByDescending(a => a.TotalRevenue).FirstOrDefault()?.Name ?? "N/A",
                ["averageRating"] = analytics.Average(a => a.CustomerRating),
                ["totalProfit"] = analytics.Sum(a => a.TotalRevenue * a.ProfitMargin),
                ["performanceData"] = performance,
                ["insights"] = insights,
                ["generatedAt"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu dashboard data");
            throw;
        }
    }

    private decimal CalculateSimulatedRevenue(MenuItemDto item)
    {
        return item.SellingPrice * Random.Shared.Next(10, 100);
    }

    private int CalculateSimulatedOrders(MenuItemDto item)
    {
        return Random.Shared.Next(5, 50);
    }

    private int CalculateSimulatedQuantity(MenuItemDto item)
    {
        return Random.Shared.Next(20, 200);
    }

    private decimal CalculateSimulatedProfitMargin(MenuItemDto item)
    {
        return (decimal)Random.Shared.NextDouble() * 0.4m + 0.1m; // 10% to 50%
    }

    private decimal CalculateSimulatedCOGS(MenuItemDto item)
    {
        return item.SellingPrice * (1 - CalculateSimulatedProfitMargin(item));
    }

    private decimal CalculateSimulatedDailyRevenue(DateTime date)
    {
        return (decimal)Random.Shared.NextDouble() * 1000 + 100;
    }

    private string GetRandomTrend()
    {
        var trends = new[] { "Rising", "Stable", "Declining" };
        return trends[Random.Shared.Next(trends.Length)];
    }

    private string GetTrendRecommendation(string category)
    {
        return category switch
        {
            "Beverages" => "Consider seasonal drink specials",
            "Appetizers" => "Bundle with main courses for better margins",
            "Entrees" => "Focus on signature dishes",
            "Sides" => "Offer combo deals",
            _ => "Monitor performance closely"
        };
    }

    private IReadOnlyList<MenuAnalyticsDto> ApplyAnalyticsFilters(List<MenuAnalyticsDto> analytics, MenuAnalyticsQueryDto query)
    {
        var filtered = analytics.AsEnumerable();
        
        if (query.Category != null)
        {
            filtered = filtered.Where(a => a.Category == query.Category);
        }
        
        if (query.Categories.Any())
        {
            filtered = filtered.Where(a => query.Categories.Contains(a.Category));
        }
        
        if (!query.IncludeInactive)
        {
            // In real implementation, this would filter based on actual availability
        }
        
        if (query.SortBy != null)
        {
            filtered = query.SortBy switch
            {
                "Revenue" => query.SortDirection == "Desc" ? filtered.OrderByDescending(a => a.TotalRevenue) : filtered.OrderBy(a => a.TotalRevenue),
                "Orders" => query.SortDirection == "Desc" ? filtered.OrderByDescending(a => a.TotalOrders) : filtered.OrderBy(a => a.TotalOrders),
                "Quantity" => query.SortDirection == "Desc" ? filtered.OrderByDescending(a => a.TotalQuantitySold) : filtered.OrderBy(a => a.TotalQuantitySold),
                "Profit" => query.SortDirection == "Desc" ? filtered.OrderByDescending(a => a.ProfitMargin) : filtered.OrderBy(a => a.ProfitMargin),
                "Rating" => query.SortDirection == "Desc" ? filtered.OrderByDescending(a => a.CustomerRating) : filtered.OrderBy(a => a.CustomerRating),
                _ => filtered.OrderByDescending(a => a.TotalRevenue)
            };
        }
        
        if (query.TopCount.HasValue)
        {
            filtered = filtered.Take(query.TopCount.Value);
        }
        
        return filtered.ToList();
    }

    public async Task<Dictionary<string, object>> GetDetailedReportAsync(MenuAnalyticsQueryDto query)
    {
        // Simulate detailed reporting data
        await Task.Delay(100); // Simulate API call
        
        var report = new Dictionary<string, object>
        {
            ["summary"] = new
            {
                totalItems = 45,
                activeItems = 42,
                inactiveItems = 3,
                totalRevenue = 125000.50m,
                averageOrderValue = 28.75m,
                topPerformingCategory = "Beverages",
                leastPerformingCategory = "Desserts"
            },
            ["performanceMetrics"] = new
            {
                conversionRate = 0.68,
                customerSatisfaction = 4.2,
                repeatOrderRate = 0.45,
                averagePreparationTime = 8.5,
                wastePercentage = 0.12
            },
            ["trends"] = new[]
            {
                new { period = "Last 7 days", revenue = 8750.25m, orders = 305, growth = 0.15 },
                new { period = "Last 30 days", revenue = 32500.75m, orders = 1180, growth = 0.08 },
                new { period = "Last 90 days", revenue = 87500.50m, orders = 3045, growth = 0.12 }
            },
            ["recommendations"] = new[]
            {
                "Consider promoting low-performing items during peak hours",
                "Implement dynamic pricing for high-demand items",
                "Review preparation times for items with high waste percentage",
                "Create combo deals for complementary items"
            }
        };
        
        return report;
    }

    public async Task<Dictionary<string, object>> GetExportDataAsync(MenuAnalyticsQueryDto query, string format)
    {
        // Simulate export data generation
        await Task.Delay(200);
        
        var exportData = new Dictionary<string, object>
        {
            ["format"] = format,
            ["generatedAt"] = DateTime.UtcNow,
            ["data"] = new
            {
                items = new[]
                {
                    new { name = "Margherita Pizza", category = "Main Course", revenue = 1250.50m, orders = 45, rating = 4.5 },
                    new { name = "Caesar Salad", category = "Appetizer", revenue = 875.25m, orders = 32, rating = 4.2 },
                    new { name = "Chocolate Cake", category = "Dessert", revenue = 450.75m, orders = 18, rating = 4.8 }
                },
                summary = new
                {
                    totalRevenue = 2576.50m,
                    totalOrders = 95,
                    averageRating = 4.5
                }
            }
        };
        
        return exportData;
    }
}
