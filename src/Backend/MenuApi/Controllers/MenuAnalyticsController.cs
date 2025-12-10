using Microsoft.AspNetCore.Mvc;
using MenuApi.Models;
using MenuApi.Services;

namespace MenuApi.Controllers;

[ApiController]
[Route("api/menu/analytics")]
public sealed class MenuAnalyticsController : ControllerBase
{
    private readonly IMenuAnalyticsService _analyticsService;
    private readonly ILogger<MenuAnalyticsController> _logger;

    public MenuAnalyticsController(IMenuAnalyticsService analyticsService, ILogger<MenuAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive menu analytics
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MenuAnalyticsDto>>> GetAnalytics([FromQuery] MenuAnalyticsQueryDto query, CancellationToken ct)
    {
        try
        {
            var analytics = await _analyticsService.GetMenuAnalyticsAsync(query, ct);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu analytics");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get menu performance data
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<IReadOnlyList<MenuPerformanceDto>>> GetPerformance([FromQuery] MenuPerformanceQueryDto query, CancellationToken ct)
    {
        try
        {
            var performance = await _analyticsService.GetMenuPerformanceAsync(query, ct);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu performance");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get menu trends analysis
    /// </summary>
    [HttpGet("trends")]
    public async Task<ActionResult<IReadOnlyList<MenuTrendDto>>> GetTrends([FromQuery] MenuTrendQueryDto query, CancellationToken ct)
    {
        try
        {
            var trends = await _analyticsService.GetMenuTrendsAsync(query, ct);
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu trends");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get menu insights and recommendations
    /// </summary>
    [HttpGet("insights")]
    public async Task<ActionResult<IReadOnlyList<MenuInsightDto>>> GetInsights([FromQuery] MenuInsightQueryDto query, CancellationToken ct)
    {
        try
        {
            var insights = await _analyticsService.GetMenuInsightsAsync(query, ct);
            return Ok(insights);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting menu insights");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get analytics for a specific menu item
    /// </summary>
    [HttpGet("items/{id}")]
    public async Task<ActionResult<MenuAnalyticsDto>> GetItemAnalytics(long id, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        try
        {
            var analytics = await _analyticsService.GetItemAnalyticsAsync(id, fromDate, toDate, ct);
            return Ok(analytics);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item analytics for {MenuItemId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get dashboard data for menu analytics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<Dictionary<string, object>>> GetDashboard([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
    {
        try
        {
            var dashboard = await _analyticsService.GetMenuDashboardDataAsync(fromDate, toDate, ct);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get detailed analytics report
    /// </summary>
    [HttpGet("detailed-report")]
    public async Task<ActionResult<Dictionary<string, object>>> GetDetailedReport([FromQuery] MenuAnalyticsQueryDto query)
    {
        try
        {
            var report = await _analyticsService.GetDetailedReportAsync(query);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed report");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get export data for analytics
    /// </summary>
    [HttpGet("export")]
    public async Task<ActionResult<Dictionary<string, object>>> GetExportData([FromQuery] MenuAnalyticsQueryDto query, [FromQuery] string format = "json")
    {
        try
        {
            var exportData = await _analyticsService.GetExportDataAsync(query, format);
            return Ok(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting export data");
            return StatusCode(500, "Internal server error");
        }
    }
}
