using MenuApi.Models;
using MenuApi.Repositories;

namespace MenuApi.Services;

public interface IMenuAnalyticsService
{
    Task<IReadOnlyList<MenuAnalyticsDto>> GetMenuAnalyticsAsync(MenuAnalyticsQueryDto query, CancellationToken ct);
    Task<IReadOnlyList<MenuPerformanceDto>> GetMenuPerformanceAsync(MenuPerformanceQueryDto query, CancellationToken ct);
    Task<IReadOnlyList<MenuTrendDto>> GetMenuTrendsAsync(MenuTrendQueryDto query, CancellationToken ct);
    Task<IReadOnlyList<MenuInsightDto>> GetMenuInsightsAsync(MenuInsightQueryDto query, CancellationToken ct);
    Task<MenuAnalyticsDto> GetItemAnalyticsAsync(long menuItemId, DateTime? fromDate, DateTime? toDate, CancellationToken ct);
    Task<Dictionary<string, object>> GetMenuDashboardDataAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct);
    Task<Dictionary<string, object>> GetDetailedReportAsync(MenuAnalyticsQueryDto query);
    Task<Dictionary<string, object>> GetExportDataAsync(MenuAnalyticsQueryDto query, string format);
}

public interface IMenuTemplateService
{
    Task<PagedResult<MenuTemplateDto>> ListTemplatesAsync(MenuTemplateQueryDto query, CancellationToken ct);
    Task<MenuTemplateDto?> GetTemplateAsync(long id, CancellationToken ct);
    Task<MenuTemplateDto> CreateTemplateAsync(CreateMenuTemplateDto dto, string user, CancellationToken ct);
    Task<MenuTemplateDto> UpdateTemplateAsync(long id, UpdateMenuTemplateDto dto, string user, CancellationToken ct);
    Task DeleteTemplateAsync(long id, string user, CancellationToken ct);
    Task<BulkOperationResultDto> ApplyTemplateAsync(ApplyMenuTemplateDto dto, string user, CancellationToken ct);
    Task<MenuTemplateDto> CreateTemplateFromCurrentMenuAsync(string name, string description, string category, string user, CancellationToken ct);
}

public interface IMenuVersionService
{
    Task<PagedResult<MenuVersionDto>> ListVersionsAsync(MenuVersionQueryDto query, CancellationToken ct);
    Task<MenuVersionDto?> GetVersionAsync(long id, CancellationToken ct);
    Task<MenuVersionDto> CreateVersionAsync(CreateMenuVersionDto dto, string user, CancellationToken ct);
    Task<MenuVersionDto> ActivateVersionAsync(ActivateMenuVersionDto dto, string user, CancellationToken ct);
    Task DeleteVersionAsync(long id, string user, CancellationToken ct);
    Task<MenuVersionDto> GetCurrentVersionAsync(CancellationToken ct);
    Task<IReadOnlyList<MenuItemVersionDto>> GetItemVersionHistoryAsync(long menuItemId, CancellationToken ct);
}

public interface IMenuBulkOperationService
{
    Task<BulkOperationResultDto> ExecuteBulkOperationAsync(BulkOperationDto operation, CancellationToken ct);
    Task<BulkOperationResultDto> UpdatePricesAsync(List<long> menuItemIds, decimal priceChange, string changeType, string user, CancellationToken ct);
    Task<BulkOperationResultDto> ChangeCategoryAsync(List<long> menuItemIds, string newCategory, string user, CancellationToken ct);
    Task<BulkOperationResultDto> ToggleAvailabilityAsync(List<long> menuItemIds, bool isAvailable, string user, CancellationToken ct);
    Task<BulkOperationResultDto> UpdateImagesAsync(List<long> menuItemIds, string imageUrl, string user, CancellationToken ct);
    Task<BulkOperationResultDto> ApplyDiscountAsync(List<long> menuItemIds, decimal discountPercentage, string user, CancellationToken ct);
}

public interface IMenuExportImportService
{
    Task<byte[]> ExportMenuAsync(MenuExportQueryDto query, CancellationToken ct);
    Task<MenuValidationResultDto> ValidateImportAsync(MenuImportQueryDto query, byte[] data, CancellationToken ct);
    Task<BulkOperationResultDto> ImportMenuAsync(MenuImportQueryDto query, byte[] data, string user, CancellationToken ct);
    Task<Dictionary<string, object>> GetImportPreviewAsync(MenuImportQueryDto query, byte[] data, CancellationToken ct);
    Task<IReadOnlyList<string>> GetSupportedFormatsAsync();
}

public interface IMenuValidationService
{
    Task<MenuValidationResultDto> ValidateMenuAsync(CancellationToken ct);
    Task<MenuValidationResultDto> ValidateMenuItemAsync(long menuItemId, CancellationToken ct);
    Task<MenuValidationResultDto> ValidateCategoryAsync(string category, CancellationToken ct);
    Task<IReadOnlyList<MenuValidationErrorDto>> GetValidationErrorsAsync(string? category, CancellationToken ct);
    Task<IReadOnlyList<MenuValidationWarningDto>> GetValidationWarningsAsync(string? category, CancellationToken ct);
    Task<bool> FixValidationIssuesAsync(List<long> menuItemIds, string user, CancellationToken ct);
}

/// <summary>
/// Interface for enhanced menu versioning service with history tracking
/// </summary>
public interface IMenuVersioningService
{
    Task<MenuVersionHistoryDto> CreateVersionAsync(CreateMenuVersionHistoryDto request, CancellationToken ct = default);
    Task<MenuVersionHistoryResponseDto> GetVersionHistoryAsync(MenuVersionHistoryQueryDto query, CancellationToken ct = default);
    Task<IReadOnlyList<MenuChangeSummaryDto>> GetChangeSummariesAsync(long? menuItemId = null, CancellationToken ct = default);
    Task<MenuVersionHistoryDto?> GetVersionAsync(long versionId, CancellationToken ct = default);
    Task<bool> RollbackToVersionAsync(MenuRollbackDto request, CancellationToken ct = default);
    Task<IReadOnlyList<MenuVersionHistoryDto>> GetRecentChangesAsync(int count = 10, CancellationToken ct = default);
}
