namespace OrderApi.Models;

public sealed record OrderAnalyticsDto(
    // KPI Metrics
    int OrdersToday,
    decimal RevenueToday,
    decimal AverageOrderValue,
    decimal CompletionRate,
    
    // Status Monitoring
    int PendingOrders,
    int InProgressOrders,
    int ReadyForDeliveryOrders,
    int CompletedTodayOrders,
    
    // Performance Metrics
    int AveragePrepTimeMinutes,
    string PeakHour,
    decimal EfficiencyScore,
    
    // Analytics Summary
    int TotalOrders,
    decimal TotalRevenue,
    int AverageOrderTimeMinutes,
    decimal CustomerSatisfactionRate,
    decimal ReturnRate,
    
    // Alerts
    int AlertCount,
    string AlertMessage,
    
    // Recent Activity
    IReadOnlyList<RecentActivityDto> RecentActivities
);

public sealed record RecentActivityDto(
    string Title,
    string Description,
    string Timestamp,
    string ActivityType
);

public sealed record OrderStatusSummaryDto(
    string Status,
    int Count,
    decimal Percentage
);

public sealed record OrderTrendDto(
    DateTime Date,
    int OrderCount,
    decimal Revenue,
    decimal AverageOrderValue
);

public sealed record OrderAnalyticsRequestDto(
    DateTime? FromDate,
    DateTime? ToDate,
    string ReportType // "daily", "weekly", "monthly", "performance"
);
