namespace MagiDesk.Shared.DTOs.Tables;

/// <summary>
/// Request to calculate split payment amount.
/// Supports splitting by specific items, percentage, or fixed amount.
/// </summary>
public class CalculateSplitRequest
{
    /// <summary>
    /// Optional: List of item IDs to include in the split calculation.
    /// Used for "Split by Item" feature.
    /// </summary>
    public List<string>? ItemIds { get; set; }

    /// <summary>
    /// Optional: Percentage of the total bill (0-100).
    /// Used for "Split by Percentage" feature.
    /// </summary>
    public decimal? Percentage { get; set; }

    /// <summary>
    /// Optional: Fixed amount to validate/calculate change for.
    /// Used for "Split by Amount" or validation.
    /// </summary>
    public decimal? FixedAmount { get; set; }
}
