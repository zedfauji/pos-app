using MenuApi.Models;
using Microsoft.Extensions.Logging;

namespace MenuApi.Services;

/// <summary>
/// Implementation of menu versioning service
/// </summary>
public sealed class MenuVersioningService : IMenuVersioningService
{
    private readonly ILogger<MenuVersioningService> _logger;
    private readonly List<MenuVersionHistoryDto> _versions = new();
    private long _nextVersionId = 1;

    public MenuVersioningService(ILogger<MenuVersioningService> logger)
    {
        _logger = logger;
        InitializeSampleData();
    }

    public async Task<MenuVersionHistoryDto> CreateVersionAsync(CreateMenuVersionHistoryDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating menu version for item {MenuItemId} with change type {ChangeType}", 
                request.MenuItemId, request.ChangeType);

            var version = new MenuVersionHistoryDto
            {
                Id = _nextVersionId++,
                MenuItemId = request.MenuItemId,
                Version = GenerateVersionNumber(request.MenuItemId),
                ChangeType = request.ChangeType,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow,
                ChangeDescription = request.ChangeDescription,
                PreviousValues = request.PreviousValues,
                NewValues = request.NewValues,
                IsActive = true
            };

            _versions.Add(version);
            
            // Mark previous versions as inactive
            var previousVersions = _versions.Where(v => v.MenuItemId == request.MenuItemId && v.Id != version.Id).ToList();
            foreach (var prevVersion in previousVersions)
            {
                var index = _versions.FindIndex(v => v.Id == prevVersion.Id);
                if (index >= 0)
                {
                    _versions[index] = prevVersion with { IsActive = false };
                }
            }

            return await Task.FromResult(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu version");
            throw;
        }
    }

    public async Task<MenuVersionHistoryResponseDto> GetVersionHistoryAsync(MenuVersionHistoryQueryDto query, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting version history for query");

            var filteredVersions = _versions.AsQueryable();

            if (query.MenuItemId.HasValue)
            {
                filteredVersions = filteredVersions.Where(v => v.MenuItemId == query.MenuItemId.Value);
            }

            if (!string.IsNullOrEmpty(query.ChangeType))
            {
                filteredVersions = filteredVersions.Where(v => v.ChangeType == query.ChangeType);
            }

            if (!string.IsNullOrEmpty(query.ChangedBy))
            {
                filteredVersions = filteredVersions.Where(v => v.ChangedBy.Contains(query.ChangedBy));
            }

            if (query.FromDate.HasValue)
            {
                filteredVersions = filteredVersions.Where(v => v.ChangedAt >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                filteredVersions = filteredVersions.Where(v => v.ChangedAt <= query.ToDate.Value);
            }

            if (query.IsActive.HasValue)
            {
                filteredVersions = filteredVersions.Where(v => v.IsActive == query.IsActive.Value);
            }

            var totalCount = filteredVersions.Count();
            var pageSize = query.PageSize ?? 50;
            var pageNumber = query.PageNumber ?? 1;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedVersions = filteredVersions
                .OrderByDescending(v => v.ChangedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new MenuVersionHistoryResponseDto
            {
                Versions = pagedVersions,
                TotalCount = totalCount,
                PageSize = pageSize,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version history");
            throw;
        }
    }

    public async Task<IReadOnlyList<MenuChangeSummaryDto>> GetChangeSummariesAsync(long? menuItemId = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting change summaries");

            var summaries = _versions
                .Where(v => !menuItemId.HasValue || v.MenuItemId == menuItemId.Value)
                .GroupBy(v => v.MenuItemId)
                .Select(g => new MenuChangeSummaryDto
                {
                    MenuItemId = g.Key,
                    MenuItemName = GetMenuItemName(g.Key),
                    TotalChanges = g.Count(),
                    LastChanged = g.Max(v => v.ChangedAt),
                    LastChangedBy = g.OrderByDescending(v => v.ChangedAt).First().ChangedBy,
                    LastChangeType = g.OrderByDescending(v => v.ChangedAt).First().ChangeType,
                    LastChangeDescription = g.OrderByDescending(v => v.ChangedAt).First().ChangeDescription
                })
                .OrderByDescending(s => s.LastChanged)
                .ToList();

            return await Task.FromResult(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting change summaries");
            throw;
        }
    }

    public async Task<MenuVersionHistoryDto?> GetVersionAsync(long versionId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting version {VersionId}", versionId);

            var version = _versions.FirstOrDefault(v => v.Id == versionId);
            return await Task.FromResult(version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting version {VersionId}", versionId);
            throw;
        }
    }

    public async Task<bool> RollbackToVersionAsync(MenuRollbackDto request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Rolling back to version {VersionId}", request.VersionId);

            var targetVersion = _versions.FirstOrDefault(v => v.Id == request.VersionId);
            if (targetVersion == null)
            {
                return false;
            }

            // Create a rollback version
            var rollbackVersion = new MenuVersionHistoryDto
            {
                Id = _nextVersionId++,
                MenuItemId = targetVersion.MenuItemId,
                Version = GenerateVersionNumber(targetVersion.MenuItemId),
                ChangeType = "Rollback",
                ChangedBy = request.RolledBackBy,
                ChangedAt = DateTime.UtcNow,
                ChangeDescription = $"Rolled back to version {targetVersion.Version}. Reason: {request.RollbackReason}",
                PreviousValues = targetVersion.NewValues,
                NewValues = targetVersion.PreviousValues,
                IsActive = true
            };

            _versions.Add(rollbackVersion);

            // Mark all other versions as inactive
            var otherVersions = _versions.Where(v => v.MenuItemId == targetVersion.MenuItemId && v.Id != rollbackVersion.Id).ToList();
            foreach (var version in otherVersions)
            {
                var index = _versions.FindIndex(v => v.Id == version.Id);
                if (index >= 0)
                {
                    _versions[index] = version with { IsActive = false };
                }
            }

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back to version {VersionId}", request.VersionId);
            throw;
        }
    }

    public async Task<IReadOnlyList<MenuVersionHistoryDto>> GetRecentChangesAsync(int count = 10, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting recent changes");

            var recentChanges = _versions
                .OrderByDescending(v => v.ChangedAt)
                .Take(count)
                .ToList();

            return await Task.FromResult(recentChanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent changes");
            throw;
        }
    }

    private string GenerateVersionNumber(long menuItemId)
    {
        var itemVersions = _versions.Count(v => v.MenuItemId == menuItemId);
        return $"v{itemVersions + 1}.0";
    }

    private string GetMenuItemName(long menuItemId)
    {
        // Simulate menu item names
        var names = new Dictionary<long, string>
        {
            { 1, "Margherita Pizza" },
            { 2, "Caesar Salad" },
            { 3, "Chocolate Cake" },
            { 4, "Coca Cola" },
            { 5, "French Fries" }
        };

        return names.TryGetValue(menuItemId, out var name) ? name : $"Menu Item {menuItemId}";
    }

    private void InitializeSampleData()
    {
        var sampleVersions = new[]
        {
            new MenuVersionHistoryDto
            {
                Id = _nextVersionId++,
                MenuItemId = 1,
                Version = "v1.0",
                ChangeType = "Created",
                ChangedBy = "admin",
                ChangedAt = DateTime.UtcNow.AddDays(-30),
                ChangeDescription = "Initial menu item creation",
                NewValues = new Dictionary<string, object> { ["name"] = "Margherita Pizza", ["price"] = 12.99m },
                IsActive = false
            },
            new MenuVersionHistoryDto
            {
                Id = _nextVersionId++,
                MenuItemId = 1,
                Version = "v2.0",
                ChangeType = "PriceChanged",
                ChangedBy = "manager",
                ChangedAt = DateTime.UtcNow.AddDays(-15),
                ChangeDescription = "Updated price due to ingredient cost increase",
                PreviousValues = new Dictionary<string, object> { ["price"] = 12.99m },
                NewValues = new Dictionary<string, object> { ["price"] = 14.99m },
                IsActive = true
            },
            new MenuVersionHistoryDto
            {
                Id = _nextVersionId++,
                MenuItemId = 2,
                Version = "v1.0",
                ChangeType = "Created",
                ChangedBy = "admin",
                ChangedAt = DateTime.UtcNow.AddDays(-25),
                ChangeDescription = "Initial menu item creation",
                NewValues = new Dictionary<string, object> { ["name"] = "Caesar Salad", ["price"] = 8.99m },
                IsActive = true
            }
        };

        _versions.AddRange(sampleVersions);
    }
}
