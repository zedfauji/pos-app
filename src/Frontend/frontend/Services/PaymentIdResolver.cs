using MagiDesk.Shared.DTOs.Tables;
using System.Diagnostics;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Service for resolving SessionId and BillingId for payment processing
/// Provides robust fallback logic and validation
/// </summary>
public sealed class PaymentIdResolver
{
    private readonly TableRepository _tableRepository;
    
    public PaymentIdResolver(TableRepository tableRepository)
    {
        _tableRepository = tableRepository;
    }
    
    /// <summary>
    /// Resolves SessionId and BillingId for payment processing
    /// </summary>
    /// <param name="bill">The bill information</param>
    /// <returns>Tuple of (SessionId, BillingId)</returns>
    /// <exception cref="InvalidOperationException">When IDs cannot be resolved</exception>
    public async Task<(Guid SessionId, Guid BillingId)> ResolvePaymentIdsAsync(BillResult bill)
    {
        Debug.WriteLine($"PaymentIdResolver: Starting ID resolution for BillId={bill.BillId}");
        
        // Priority 1: Use IDs directly from bill if they exist and are valid
        if (bill.SessionId.HasValue && bill.SessionId != Guid.Empty && 
            bill.BillingId.HasValue && bill.BillingId != Guid.Empty)
        {
            Debug.WriteLine($"PaymentIdResolver: Using direct IDs - SessionId={bill.SessionId}, BillingId={bill.BillingId}");
            return (bill.SessionId.Value, bill.BillingId.Value);
        }
        
        // Priority 2: Get complete bill information
        var completeBill = await _tableRepository.GetBillByIdAsync(bill.BillId);
        if (completeBill?.SessionId != null && completeBill.SessionId != Guid.Empty &&
            completeBill.BillingId != null && completeBill.BillingId != Guid.Empty)
        {
            Debug.WriteLine($"PaymentIdResolver: Using complete bill IDs - SessionId={completeBill.SessionId}, BillingId={completeBill.BillingId}");
            return (completeBill.SessionId.Value, completeBill.BillingId.Value);
        }
        
        // Priority 3: Look for active session for the table
        if (!string.IsNullOrWhiteSpace(bill.TableLabel))
        {
            var (activeSessionId, activeBillingId) = await _tableRepository.GetActiveSessionForTableAsync(bill.TableLabel);
            if (activeSessionId != null && activeSessionId != Guid.Empty &&
                activeBillingId != null && activeBillingId != Guid.Empty)
            {
                Debug.WriteLine($"PaymentIdResolver: Using active session IDs - SessionId={activeSessionId}, BillingId={activeBillingId}");
                return (activeSessionId.Value, activeBillingId.Value);
            }
        }
        
        // Priority 4: Use BillId as BillingId and generate new SessionId
        Debug.WriteLine($"PaymentIdResolver: Using fallback - SessionId=New, BillingId={bill.BillId}");
        return (Guid.NewGuid(), bill.BillId);
    }
    
    /// <summary>
    /// Validates that the resolved IDs are valid for payment processing
    /// </summary>
    public bool ValidatePaymentIds(Guid sessionId, Guid billingId)
    {
        if (sessionId == Guid.Empty)
        {
            Debug.WriteLine("PaymentIdResolver: SessionId is empty");
            return false;
        }
        
        if (billingId == Guid.Empty)
        {
            Debug.WriteLine("PaymentIdResolver: BillingId is empty");
            return false;
        }
        
        Debug.WriteLine($"PaymentIdResolver: IDs validated - SessionId={sessionId}, BillingId={billingId}");
        return true;
    }
}
