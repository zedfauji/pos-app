using System.Diagnostics;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Service for calculating split payments with proper tip and discount distribution
/// </summary>
public sealed class SplitPaymentCalculator
{
    /// <summary>
    /// Calculates how to distribute tips and discounts across split payment methods
    /// </summary>
    /// <param name="totalAmount">Total amount to be paid</param>
    /// <param name="tipAmount">Total tip amount</param>
    /// <param name="discountAmount">Total discount amount</param>
    /// <param name="splitAmounts">Amounts for each payment method</param>
    /// <returns>Split payment details with distributed tips and discounts</returns>
    public SplitPaymentResult CalculateSplitPayment(
        decimal totalAmount, 
        decimal tipAmount, 
        decimal discountAmount, 
        Dictionary<string, decimal> splitAmounts)
    {
        Debug.WriteLine($"SplitPaymentCalculator: Calculating split payment - Total={totalAmount:C}, Tip={tipAmount:C}, Discount={discountAmount:C}");
        
        var result = new SplitPaymentResult();
        var netAmount = totalAmount + tipAmount - discountAmount;
        
        // Validate split amounts
        var splitTotal = splitAmounts.Values.Sum();
        if (Math.Abs(splitTotal - netAmount) > 0.01m)
        {
            throw new InvalidOperationException($"Split total ({splitTotal:C}) must equal net amount ({netAmount:C})");
        }
        
        // Calculate proportional distribution
        foreach (var kvp in splitAmounts)
        {
            var method = kvp.Key;
            var amount = kvp.Value;
            var proportion = amount / netAmount;
            
            var splitDetail = new SplitPaymentDetail
            {
                PaymentMethod = method,
                AmountPaid = amount,
                TipAmount = Math.Round(tipAmount * proportion, 2),
                DiscountAmount = Math.Round(discountAmount * proportion, 2),
                NetAmount = amount - Math.Round(discountAmount * proportion, 2)
            };
            
            result.SplitDetails.Add(splitDetail);
            Debug.WriteLine($"SplitPaymentCalculator: {method} - Amount={amount:C}, Tip={splitDetail.TipAmount:C}, Discount={splitDetail.DiscountAmount:C}");
        }
        
        // Adjust for rounding differences
        AdjustForRounding(result, tipAmount, discountAmount);
        
        result.TotalAmount = totalAmount;
        result.TotalTip = tipAmount;
        result.TotalDiscount = discountAmount;
        result.NetAmount = netAmount;
        
        Debug.WriteLine($"SplitPaymentCalculator: Final result - Net={result.NetAmount:C}, Tip={result.TotalTip:C}, Discount={result.TotalDiscount:C}");
        
        return result;
    }
    
    private void AdjustForRounding(SplitPaymentResult result, decimal expectedTip, decimal expectedDiscount)
    {
        var actualTip = result.SplitDetails.Sum(s => s.TipAmount);
        var actualDiscount = result.SplitDetails.Sum(s => s.DiscountAmount);
        
        var tipDifference = expectedTip - actualTip;
        var discountDifference = expectedDiscount - actualDiscount;
        
        // Adjust the largest split to account for rounding differences
        if (Math.Abs(tipDifference) > 0.01m || Math.Abs(discountDifference) > 0.01m)
        {
            var largestSplit = result.SplitDetails.OrderByDescending(s => s.AmountPaid).First();
            largestSplit.TipAmount += tipDifference;
            largestSplit.DiscountAmount += discountDifference;
            largestSplit.NetAmount = largestSplit.AmountPaid - largestSplit.DiscountAmount;
            
            Debug.WriteLine($"SplitPaymentCalculator: Adjusted {largestSplit.PaymentMethod} for rounding - Tip={largestSplit.TipAmount:C}, Discount={largestSplit.DiscountAmount:C}");
        }
    }
}

public class SplitPaymentResult
{
    public List<SplitPaymentDetail> SplitDetails { get; } = new();
    public decimal TotalAmount { get; set; }
    public decimal TotalTip { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetAmount { get; set; }
}

public class SplitPaymentDetail
{
    public string PaymentMethod { get; set; } = "";
    public decimal AmountPaid { get; set; }
    public decimal TipAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
}
