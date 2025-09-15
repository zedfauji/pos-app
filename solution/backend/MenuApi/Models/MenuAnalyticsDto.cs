using System.ComponentModel.DataAnnotations;

namespace MenuApi.Models;

public sealed record MenuAnalyticsDto
{
    public long MenuItemId { get; init; }
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public int TotalQuantitySold { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal ProfitMargin { get; init; }
    public decimal CostOfGoodsSold { get; init; }
    public DateTime LastSold { get; init; }
    public DateTime FirstSold { get; init; }
    public bool IsTopSeller { get; init; }
    public int Ranking { get; init; }
    public decimal ConversionRate { get; init; }
    public TimeSpan AveragePreparationTime { get; init; }
    public int CustomerRating { get; init; }
    public int ReviewCount { get; init; }
}

public sealed record MenuPerformanceDto
{
    public DateTime Date { get; init; }
    public string Category { get; init; } = "";
    public decimal Revenue { get; init; }
    public int Orders { get; init; }
    public int QuantitySold { get; init; }
    public decimal AverageOrderValue { get; init; }
    public decimal ProfitMargin { get; init; }
    public int UniqueCustomers { get; init; }
    public decimal CustomerSatisfactionScore { get; init; }
}

public sealed record MenuTrendDto
{
    public string Category { get; init; } = "";
    public string Trend { get; init; } = ""; // "Rising", "Stable", "Declining"
    public decimal ChangePercentage { get; init; }
    public int PeriodDays { get; init; }
    public decimal RevenueChange { get; init; }
    public int OrderChange { get; init; }
    public string Recommendation { get; init; } = "";
}

public sealed record MenuInsightDto
{
    public string Type { get; init; } = ""; // "Performance", "Trend", "Recommendation", "Alert"
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Priority { get; init; } = ""; // "High", "Medium", "Low"
    public string ActionRequired { get; init; } = "";
    public DateTime GeneratedAt { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
}

public sealed record MenuTemplateDto
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public bool IsPublic { get; init; }
    public string CreatedBy { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public List<MenuItemTemplateDto> Items { get; init; } = new();
}

public sealed record MenuItemTemplateDto
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal SuggestedPrice { get; init; }
    public string PictureUrl { get; init; } = "";
    public bool IsDiscountable { get; init; }
    public bool IsPartOfCombo { get; init; }
    public List<string> SuggestedModifiers { get; init; } = new();
}

public sealed record BulkOperationDto
{
    public string Operation { get; init; } = ""; // "UpdatePrices", "ChangeCategory", "ToggleAvailability", "UpdateImages"
    public List<long> MenuItemIds { get; init; } = new();
    public Dictionary<string, object> Parameters { get; init; } = new();
    public string User { get; init; } = "";
}

public sealed record BulkOperationResultDto
{
    public string Operation { get; set; } = "";
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration { get; set; }
}

public sealed record MenuVersionDto
{
    public long Id { get; init; }
    public string VersionName { get; init; } = "";
    public string Description { get; init; } = "";
    public bool IsActive { get; init; }
    public string CreatedBy { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public List<MenuItemVersionDto> Items { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public sealed record MenuItemVersionDto
{
    public long MenuItemId { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public decimal Price { get; init; }
    public string PictureUrl { get; init; } = "";
    public bool IsAvailable { get; init; }
    public int Version { get; init; }
    public DateTime ChangedAt { get; init; }
    public string ChangedBy { get; init; } = "";
    public string ChangeType { get; init; } = ""; // "Created", "Updated", "Deleted", "Restored"
}

public sealed record MenuExportDto
{
    public string Format { get; init; } = ""; // "JSON", "CSV", "PDF", "Excel"
    public string IncludeData { get; init; } = ""; // "All", "Active", "Category", "Custom"
    public List<string> Categories { get; init; } = new();
    public bool IncludeImages { get; init; }
    public bool IncludeAnalytics { get; init; }
    public bool IncludeModifiers { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public sealed record MenuImportDto
{
    public string Format { get; init; } = ""; // "JSON", "CSV", "Excel"
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public string ImportMode { get; init; } = ""; // "Create", "Update", "Upsert", "Replace"
    public bool ValidateOnly { get; init; }
    public bool SkipErrors { get; init; }
    public Dictionary<string, object> Mapping { get; init; } = new();
}

public sealed record MenuValidationResultDto
{
    public bool IsValid { get; init; }
    public List<MenuValidationErrorDto> Errors { get; init; } = new();
    public List<MenuValidationWarningDto> Warnings { get; init; } = new();
    public int TotalItems { get; init; }
    public int ValidItems { get; init; }
    public int InvalidItems { get; init; }
    public Dictionary<string, object> Summary { get; init; } = new();
}

public sealed record MenuValidationErrorDto
{
    public string Field { get; init; } = "";
    public string Message { get; init; } = "";
    public string Severity { get; init; } = ""; // "Error", "Warning", "Info"
    public int? RowNumber { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
}

public sealed record MenuValidationWarningDto
{
    public string Field { get; init; } = "";
    public string Message { get; init; } = "";
    public string Suggestion { get; init; } = "";
    public int? RowNumber { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
}
