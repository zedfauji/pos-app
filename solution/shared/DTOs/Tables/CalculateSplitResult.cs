namespace MagiDesk.Shared.DTOs.Tables;

/// <summary>
/// Result of split calculation.
/// All monetary calculations are performed server-side.
/// </summary>
public class CalculateSplitResult
{
    /// <summary>
    /// The calculated amount to pay for this split.
    /// </summary>
    public decimal AmountToPay { get; set; }

    /// <summary>
    /// Optional: Tax share for this split (if applicable).
    /// </summary>
    public decimal TaxShare { get; set; }

    /// <summary>
    /// Optional: Suggested gratuity for this split.
    /// </summary>
    public decimal SuggestedGratuity { get; set; }

    /// <summary>
    /// Items included in this split (for display/confirmation).
    /// </summary>
    public List<ItemLine> Items { get; set; } = new();
}
