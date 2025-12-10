using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Services;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/bulk")]
public sealed class MenuBulkOperationController : ControllerBase
{
    private readonly IMenuBulkOperationService _bulkService;
    private readonly ILogger<MenuBulkOperationController> _logger;

    public MenuBulkOperationController(IMenuBulkOperationService bulkService, ILogger<MenuBulkOperationController> logger)
    {
        _bulkService = bulkService;
        _logger = logger;
    }

    /// <summary>
    /// Execute a bulk operation on multiple menu items
    /// </summary>
    [HttpPost("execute")]
    public async Task<ActionResult<BulkOperationResultDto>> ExecuteBulkOperation([FromBody] BulkOperationDto operation, CancellationToken ct)
    {
        try
        {
            var result = await _bulkService.ExecuteBulkOperationAsync(operation, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing bulk operation");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update prices for multiple menu items
    /// </summary>
    [HttpPost("update-prices")]
    public async Task<ActionResult<BulkOperationResultDto>> UpdatePrices([FromBody] UpdatePricesRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkService.UpdatePricesAsync(
                request.MenuItemIds, 
                request.PriceChange, 
                request.ChangeType, 
                "api-user", 
                ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prices");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Change category for multiple menu items
    /// </summary>
    [HttpPost("change-category")]
    public async Task<ActionResult<BulkOperationResultDto>> ChangeCategory([FromBody] ChangeCategoryRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkService.ChangeCategoryAsync(
                request.MenuItemIds, 
                request.NewCategory, 
                "api-user", 
                ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing category");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Toggle availability for multiple menu items
    /// </summary>
    [HttpPost("toggle-availability")]
    public async Task<ActionResult<BulkOperationResultDto>> ToggleAvailability([FromBody] ToggleAvailabilityRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkService.ToggleAvailabilityAsync(
                request.MenuItemIds, 
                request.IsAvailable, 
                "api-user", 
                ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling availability");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update images for multiple menu items
    /// </summary>
    [HttpPost("update-images")]
    public async Task<ActionResult<BulkOperationResultDto>> UpdateImages([FromBody] UpdateImagesRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkService.UpdateImagesAsync(
                request.MenuItemIds, 
                request.ImageUrl, 
                "api-user", 
                ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating images");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Apply discount to multiple menu items
    /// </summary>
    [HttpPost("apply-discount")]
    public async Task<ActionResult<BulkOperationResultDto>> ApplyDiscount([FromBody] ApplyDiscountRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkService.ApplyDiscountAsync(
                request.MenuItemIds, 
                request.DiscountPercentage, 
                "api-user", 
                ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying discount");
            return StatusCode(500, "Internal server error");
        }
    }
}

public sealed record UpdatePricesRequest
{
    public List<long> MenuItemIds { get; init; } = new();
    public decimal PriceChange { get; init; }
    public string ChangeType { get; init; } = ""; // "Add", "Subtract", "Multiply", "Set"
}

public sealed record ChangeCategoryRequest
{
    public List<long> MenuItemIds { get; init; } = new();
    public string NewCategory { get; init; } = "";
}

public sealed record ToggleAvailabilityRequest
{
    public List<long> MenuItemIds { get; init; } = new();
    public bool IsAvailable { get; init; }
}

public sealed record UpdateImagesRequest
{
    public List<long> MenuItemIds { get; init; } = new();
    public string ImageUrl { get; init; } = "";
}

public sealed record ApplyDiscountRequest
{
    public List<long> MenuItemIds { get; init; } = new();
    public decimal DiscountPercentage { get; init; }
}
