using System.ComponentModel.DataAnnotations;

namespace MenuApi.Models;

public sealed record MenuAnalyticsQueryDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? Category { get; init; }
    public List<string> Categories { get; init; } = new();
    public string? SortBy { get; init; } // "Revenue", "Orders", "Quantity", "Profit", "Rating"
    public string? SortDirection { get; init; } // "Asc", "Desc"
    public int? TopCount { get; init; }
    public bool IncludeInactive { get; init; }
    public string? GroupBy { get; init; } // "Day", "Week", "Month", "Category", "Item"
}

public sealed record MenuPerformanceQueryDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string? Category { get; init; }
    public List<string> Categories { get; init; } = new();
    public string? GroupBy { get; init; } // "Day", "Week", "Month"
    public bool IncludeComparisons { get; init; }
    public int? ComparisonPeriods { get; init; }
}

public sealed record MenuTrendQueryDto
{
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string? Category { get; init; }
    public List<string> Categories { get; init; } = new();
    public int MinPeriodDays { get; init; } = 7;
    public decimal MinChangeThreshold { get; init; } = 0.05m; // 5%
    public bool IncludeRecommendations { get; init; }
}

public sealed record MenuInsightQueryDto
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public List<string> InsightTypes { get; init; } = new();
    public List<string> Priorities { get; init; } = new();
    public bool IncludeResolved { get; init; }
    public int? Limit { get; init; }
}

public sealed record MenuTemplateQueryDto
{
    public string? Q { get; init; }
    public string? Category { get; init; }
    public bool? IsPublic { get; init; }
    public string? CreatedBy { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record CreateMenuTemplateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; init; } = "";
    
    [StringLength(500)]
    public string Description { get; init; } = "";
    
    [Required]
    [StringLength(50)]
    public string Category { get; init; } = "";
    
    public bool IsPublic { get; init; }
    public List<MenuItemTemplateDto> Items { get; init; } = new();
}

public sealed record UpdateMenuTemplateDto
{
    [StringLength(100)]
    public string? Name { get; init; }
    
    [StringLength(500)]
    public string? Description { get; init; }
    
    [StringLength(50)]
    public string? Category { get; init; }
    
    public bool? IsPublic { get; init; }
    public List<MenuItemTemplateDto>? Items { get; init; }
}

public sealed record ApplyMenuTemplateDto
{
    [Required]
    public long TemplateId { get; init; }
    
    [Required]
    public string ImportMode { get; init; } = ""; // "Create", "Update", "Upsert"
    
    public bool SkipExisting { get; init; }
    public bool ValidateOnly { get; init; }
    public Dictionary<string, string> CategoryMapping { get; init; } = new();
    public Dictionary<string, decimal> PriceAdjustments { get; init; } = new();
}

public sealed record MenuVersionQueryDto
{
    public string? Q { get; init; }
    public bool? IsActive { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record CreateMenuVersionDto
{
    [Required]
    [StringLength(100)]
    public string VersionName { get; init; } = "";
    
    [StringLength(500)]
    public string Description { get; init; } = "";
    
    public bool ActivateImmediately { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public sealed record ActivateMenuVersionDto
{
    [Required]
    public long VersionId { get; init; }
    
    public bool CreateBackup { get; init; } = true;
    public string? BackupDescription { get; init; }
}

public sealed record MenuExportQueryDto
{
    [Required]
    public string Format { get; init; } = "";
    
    public string IncludeData { get; init; } = "All";
    public List<string> Categories { get; init; } = new();
    public bool IncludeImages { get; init; }
    public bool IncludeAnalytics { get; init; }
    public bool IncludeModifiers { get; init; }
    public bool IncludeCombos { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? FileName { get; init; }
}

public sealed record MenuImportQueryDto
{
    [Required]
    public string Format { get; init; } = "";
    
    [Required]
    public string ImportMode { get; init; } = "";
    
    public bool ValidateOnly { get; init; }
    public bool SkipErrors { get; init; }
    public bool CreateBackup { get; init; } = true;
    public Dictionary<string, string> FieldMapping { get; init; } = new();
    public Dictionary<string, object> DefaultValues { get; init; } = new();
}
