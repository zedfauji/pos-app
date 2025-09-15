using System.ComponentModel.DataAnnotations;

namespace MenuApi.Models;

/// <summary>
/// Represents a menu version with change tracking
/// </summary>
public sealed record MenuVersionHistoryDto
{
    public long Id { get; init; }
    public long MenuItemId { get; init; }
    public string Version { get; init; } = "";
    public string ChangeType { get; init; } = ""; // Created, Updated, Deleted, PriceChanged, etc.
    public string ChangedBy { get; init; } = "";
    public DateTime ChangedAt { get; init; }
    public string? ChangeDescription { get; init; }
    public Dictionary<string, object>? PreviousValues { get; init; }
    public Dictionary<string, object>? NewValues { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Request DTO for creating a menu version
/// </summary>
public sealed record CreateMenuVersionHistoryDto
{
    [Required]
    public long MenuItemId { get; init; }
    
    [Required]
    public string ChangeType { get; init; } = "";
    
    [Required]
    public string ChangedBy { get; init; } = "";
    
    public string? ChangeDescription { get; init; }
    
    public Dictionary<string, object>? PreviousValues { get; init; }
    
    public Dictionary<string, object>? NewValues { get; init; }
}

/// <summary>
/// Query DTO for menu version history
/// </summary>
public sealed record MenuVersionHistoryQueryDto
{
    public long? MenuItemId { get; init; }
    public string? ChangeType { get; init; }
    public string? ChangedBy { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public bool? IsActive { get; init; }
    public int? PageSize { get; init; } = 50;
    public int? PageNumber { get; init; } = 1;
}

/// <summary>
/// Response DTO for menu version history with pagination
/// </summary>
public sealed record MenuVersionHistoryResponseDto
{
    public IReadOnlyList<MenuVersionHistoryDto> Versions { get; init; } = new List<MenuVersionHistoryDto>();
    public int TotalCount { get; init; }
    public int PageSize { get; init; }
    public int PageNumber { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

/// <summary>
/// DTO for menu change summary
/// </summary>
public sealed record MenuChangeSummaryDto
{
    public long MenuItemId { get; init; }
    public string MenuItemName { get; init; } = "";
    public int TotalChanges { get; init; }
    public DateTime LastChanged { get; init; }
    public string LastChangedBy { get; init; } = "";
    public string LastChangeType { get; init; } = "";
    public string? LastChangeDescription { get; init; }
}

/// <summary>
/// DTO for rollback operation
/// </summary>
public sealed record MenuRollbackDto
{
    [Required]
    public long VersionId { get; init; }
    
    [Required]
    public string RolledBackBy { get; init; } = "";
    
    public string? RollbackReason { get; init; }
}
