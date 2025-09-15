using MenuApi.Models;
using MenuApi.Repositories;

namespace MenuApi.Services;

public sealed class MenuBulkOperationService : IMenuBulkOperationService
{
    private readonly IMenuRepository _menuRepository;
    private readonly ILogger<MenuBulkOperationService> _logger;

    public MenuBulkOperationService(IMenuRepository menuRepository, ILogger<MenuBulkOperationService> logger)
    {
        _menuRepository = menuRepository;
        _logger = logger;
    }

    public async Task<BulkOperationResultDto> ExecuteBulkOperationAsync(BulkOperationDto operation, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Executing bulk operation {Operation} on {Count} items", operation.Operation, operation.MenuItemIds.Count);
            
            var startTime = DateTime.UtcNow;
            var result = new BulkOperationResultDto
            {
                Operation = operation.Operation,
                TotalItems = operation.MenuItemIds.Count,
                SuccessCount = 0,
                FailureCount = 0,
                Errors = new List<string>(),
                Warnings = new List<string>(),
                CompletedAt = DateTime.UtcNow,
                Duration = TimeSpan.Zero
            };
            
            foreach (var menuItemId in operation.MenuItemIds)
            {
                try
                {
                    await ExecuteSingleOperationAsync(operation, menuItemId, ct);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Item {menuItemId}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to execute operation on item {MenuItemId}", menuItemId);
                }
            }
            
            result.CompletedAt = DateTime.UtcNow;
            result.Duration = result.CompletedAt - startTime;
            
            _logger.LogInformation("Bulk operation completed: {SuccessCount} successes, {FailureCount} failures", 
                result.SuccessCount, result.FailureCount);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bulk operation");
            throw;
        }
    }

    public async Task<BulkOperationResultDto> UpdatePricesAsync(List<long> menuItemIds, decimal priceChange, string changeType, string user, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Updating prices for {Count} items: {ChangeType} {PriceChange}", 
                menuItemIds.Count, changeType, priceChange);
            
            var operation = new BulkOperationDto
            {
                Operation = "UpdatePrices",
                MenuItemIds = menuItemIds,
                Parameters = new Dictionary<string, object>
                {
                    ["priceChange"] = priceChange,
                    ["changeType"] = changeType
                },
                User = user
            };
            
            return await ExecuteBulkOperationAsync(operation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prices");
            throw;
        }
    }

    public async Task<BulkOperationResultDto> ChangeCategoryAsync(List<long> menuItemIds, string newCategory, string user, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Changing category for {Count} items to {NewCategory}", 
                menuItemIds.Count, newCategory);
            
            var operation = new BulkOperationDto
            {
                Operation = "ChangeCategory",
                MenuItemIds = menuItemIds,
                Parameters = new Dictionary<string, object>
                {
                    ["newCategory"] = newCategory
                },
                User = user
            };
            
            return await ExecuteBulkOperationAsync(operation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing category");
            throw;
        }
    }

    public async Task<BulkOperationResultDto> ToggleAvailabilityAsync(List<long> menuItemIds, bool isAvailable, string user, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Toggling availability for {Count} items to {IsAvailable}", 
                menuItemIds.Count, isAvailable);
            
            var operation = new BulkOperationDto
            {
                Operation = "ToggleAvailability",
                MenuItemIds = menuItemIds,
                Parameters = new Dictionary<string, object>
                {
                    ["isAvailable"] = isAvailable
                },
                User = user
            };
            
            return await ExecuteBulkOperationAsync(operation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling availability");
            throw;
        }
    }

    public async Task<BulkOperationResultDto> UpdateImagesAsync(List<long> menuItemIds, string imageUrl, string user, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Updating images for {Count} items", menuItemIds.Count);
            
            var operation = new BulkOperationDto
            {
                Operation = "UpdateImages",
                MenuItemIds = menuItemIds,
                Parameters = new Dictionary<string, object>
                {
                    ["imageUrl"] = imageUrl
                },
                User = user
            };
            
            return await ExecuteBulkOperationAsync(operation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating images");
            throw;
        }
    }

    public async Task<BulkOperationResultDto> ApplyDiscountAsync(List<long> menuItemIds, decimal discountPercentage, string user, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Applying {DiscountPercentage}% discount to {Count} items", 
                discountPercentage, menuItemIds.Count);
            
            var operation = new BulkOperationDto
            {
                Operation = "ApplyDiscount",
                MenuItemIds = menuItemIds,
                Parameters = new Dictionary<string, object>
                {
                    ["discountPercentage"] = discountPercentage
                },
                User = user
            };
            
            return await ExecuteBulkOperationAsync(operation, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount");
            throw;
        }
    }

    private async Task ExecuteSingleOperationAsync(BulkOperationDto operation, long menuItemId, CancellationToken ct)
    {
        try
        {
            var item = await _menuRepository.GetItemAsync(menuItemId, ct);
            if (item == null)
            {
                throw new ArgumentException($"Menu item {menuItemId} not found");
            }
            
            switch (operation.Operation)
            {
                case "UpdatePrices":
                    await UpdateItemPriceAsync(item, operation.Parameters, ct);
                    break;
                case "ChangeCategory":
                    await UpdateItemCategoryAsync(item, operation.Parameters, ct);
                    break;
                case "ToggleAvailability":
                    await UpdateItemAvailabilityAsync(item, operation.Parameters, ct);
                    break;
                case "UpdateImages":
                    await UpdateItemImageAsync(item, operation.Parameters, ct);
                    break;
                case "ApplyDiscount":
                    await ApplyItemDiscountAsync(item, operation.Parameters, ct);
                    break;
                default:
                    throw new ArgumentException($"Unknown operation: {operation.Operation}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing single operation {Operation} on item {MenuItemId}", 
                operation.Operation, menuItemId);
            throw;
        }
    }

    private async Task UpdateItemPriceAsync(MenuItemDetailsDto item, Dictionary<string, object> parameters, CancellationToken ct)
    {
        var priceChange = (decimal)parameters["priceChange"];
        var changeType = (string)parameters["changeType"];
        
        decimal newPrice = changeType switch
        {
            "Add" => item.Item.SellingPrice + priceChange,
            "Subtract" => item.Item.SellingPrice - priceChange,
            "Multiply" => item.Item.SellingPrice * priceChange,
            "Set" => priceChange,
            _ => item.Item.SellingPrice
        };
        
        if (newPrice < 0)
        {
            throw new ArgumentException("Price cannot be negative");
        }
        
        var updateDto = new UpdateMenuItemDto(
            Name: null,
            Description: null,
            Category: null,
            GroupName: null,
            VendorPrice: null,
            SellingPrice: newPrice,
            Price: null,
            PictureUrl: null,
            IsDiscountable: null,
            IsPartOfCombo: null,
            IsAvailable: null
        );
        
        await _menuRepository.UpdateItemAsync(item.Item.Id, updateDto, "bulk-operation", ct);
    }

    private async Task UpdateItemCategoryAsync(MenuItemDetailsDto item, Dictionary<string, object> parameters, CancellationToken ct)
    {
        var newCategory = (string)parameters["newCategory"];
        
        if (string.IsNullOrWhiteSpace(newCategory))
        {
            throw new ArgumentException("Category cannot be empty");
        }
        
        var updateDto = new UpdateMenuItemDto(
            Name: null,
            Description: null,
            Category: newCategory,
            GroupName: null,
            VendorPrice: null,
            SellingPrice: null,
            Price: null,
            PictureUrl: null,
            IsDiscountable: null,
            IsPartOfCombo: null,
            IsAvailable: null
        );
        
        await _menuRepository.UpdateItemAsync(item.Item.Id, updateDto, "bulk-operation", ct);
    }

    private async Task UpdateItemAvailabilityAsync(MenuItemDetailsDto item, Dictionary<string, object> parameters, CancellationToken ct)
    {
        var isAvailable = (bool)parameters["isAvailable"];
        
        await _menuRepository.SetItemAvailabilityAsync(item.Item.Id, isAvailable, "bulk-operation", ct);
    }

    private async Task UpdateItemImageAsync(MenuItemDetailsDto item, Dictionary<string, object> parameters, CancellationToken ct)
    {
        var imageUrl = (string)parameters["imageUrl"];
        
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be empty");
        }
        
        var updateDto = new UpdateMenuItemDto(
            Name: null,
            Description: null,
            Category: null,
            GroupName: null,
            VendorPrice: null,
            SellingPrice: null,
            Price: null,
            PictureUrl: imageUrl,
            IsDiscountable: null,
            IsPartOfCombo: null,
            IsAvailable: null
        );
        
        await _menuRepository.UpdateItemAsync(item.Item.Id, updateDto, "bulk-operation", ct);
    }

    private async Task ApplyItemDiscountAsync(MenuItemDetailsDto item, Dictionary<string, object> parameters, CancellationToken ct)
    {
        var discountPercentage = (decimal)parameters["discountPercentage"];
        
        if (discountPercentage < 0 || discountPercentage > 100)
        {
            throw new ArgumentException("Discount percentage must be between 0 and 100");
        }
        
        var discountedPrice = item.Item.SellingPrice * (1 - discountPercentage / 100);
        
        var updateDto = new UpdateMenuItemDto(
            Name: null,
            Description: null,
            Category: null,
            GroupName: null,
            VendorPrice: null,
            SellingPrice: discountedPrice,
            Price: null,
            PictureUrl: null,
            IsDiscountable: null,
            IsPartOfCombo: null,
            IsAvailable: null
        );
        
        await _menuRepository.UpdateItemAsync(item.Item.Id, updateDto, "bulk-operation", ct);
    }
}
