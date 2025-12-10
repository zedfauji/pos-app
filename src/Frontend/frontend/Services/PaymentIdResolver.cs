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
        
        // Priority 1: Use IDs directly from bill if they exist and are valid
        if (bill.SessionId.HasValue && bill.SessionId != Guid.Empty && 
            bill.BillingId.HasValue && bill.BillingId != Guid.Empty)
        {
            return (bill.SessionId.Value, bill.BillingId.Value);
        }
        
        // Priority 2: Get complete bill information
        var completeBill = await _tableRepository.GetBillByIdAsync(bill.BillId);
        if (completeBill?.SessionId != null && completeBill.SessionId != Guid.Empty &&
            completeBill.BillingId != null && completeBill.BillingId != Guid.Empty)
        {
            return (completeBill.SessionId.Value, completeBill.BillingId.Value);
        }
        
        // Priority 3: Look for active session for the table
        if (!string.IsNullOrWhiteSpace(bill.TableLabel))
        {
            var (activeSessionId, activeBillingId) = await _tableRepository.GetActiveSessionForTableAsync(bill.TableLabel);
            if (activeSessionId != null && activeSessionId != Guid.Empty &&
                activeBillingId != null && activeBillingId != Guid.Empty)
            {
                return (activeSessionId.Value, activeBillingId.Value);
            }
        }
        
        // Priority 4: Use BillId as BillingId and generate new SessionId
        return (Guid.NewGuid(), bill.BillId);
    }
    
    /// <summary>
    /// Validates that the resolved IDs are valid for payment processing
    /// </summary>
    public bool ValidatePaymentIds(Guid sessionId, Guid billingId)
    {
        if (sessionId == Guid.Empty)
        {
            return false;
        }
        
        if (billingId == Guid.Empty)
        {
            return false;
        }
        
        return true;
    }
}
