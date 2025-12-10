using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CustomerApi.Services;

public class WalletService : IWalletService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(CustomerDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WalletDto?> GetWalletByCustomerIdAsync(Guid customerId)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.CustomerId == customerId);

        return wallet == null ? null : MapToDto(wallet);
    }

    public async Task<WalletDto> CreateWalletAsync(Guid customerId)
    {
        var existingWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.CustomerId == customerId);

        if (existingWallet != null)
        {
            return MapToDto(existingWallet);
        }

        var wallet = new Wallet
        {
            CustomerId = customerId
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created wallet {WalletId} for customer {CustomerId}", wallet.WalletId, customerId);

        return MapToDto(wallet);
    }

    public async Task<bool> AddFundsAsync(Guid customerId, AddWalletFundsRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.CustomerId == customerId);

            if (wallet == null || !wallet.IsActive)
            {
                return false;
            }

            // Check wallet balance limits if customer has membership level restrictions
            var customer = await _context.Customers
                .Include(c => c.MembershipLevel)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer?.MembershipLevel?.MaxWalletBalance.HasValue == true)
            {
                var newBalance = wallet.Balance + request.Amount;
                if (newBalance > customer.MembershipLevel.MaxWalletBalance.Value)
                {
                    _logger.LogWarning("Wallet funding would exceed maximum balance limit for customer {CustomerId}", customerId);
                    return false;
                }
            }

            wallet.AddFunds(request.Amount, request.Description, request.ReferenceId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Added {Amount:C} to wallet {WalletId} for customer {CustomerId}", 
                request.Amount, wallet.WalletId, customerId);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeductFundsAsync(Guid customerId, decimal amount, string description, string? referenceId = null, Guid? orderId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.CustomerId == customerId);

            if (wallet == null || !wallet.CanDeduct(amount))
            {
                return false;
            }

            wallet.DeductFunds(amount, description, referenceId);

            // Update the order ID in the transaction if provided
            if (orderId.HasValue)
            {
                var transaction_record = wallet.Transactions.LastOrDefault();
                if (transaction_record != null)
                {
                    transaction_record.OrderId = orderId.Value;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Deducted {Amount:C} from wallet {WalletId} for customer {CustomerId}", 
                amount, wallet.WalletId, customerId);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> CanDeductAsync(Guid customerId, decimal amount)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.CustomerId == customerId);

        return wallet?.CanDeduct(amount) ?? false;
    }

    public async Task<(List<WalletTransactionDto> Transactions, int TotalCount)> GetWalletTransactionsAsync(Guid customerId, int page = 1, int pageSize = 20)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.CustomerId == customerId);

        if (wallet == null)
        {
            return (new List<WalletTransactionDto>(), 0);
        }

        var query = _context.WalletTransactions
            .Where(t => t.WalletId == wallet.WalletId)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions.Select(MapTransactionToDto).ToList(), totalCount);
    }

    private static WalletDto MapToDto(Wallet wallet)
    {
        return new WalletDto
        {
            WalletId = wallet.WalletId,
            CustomerId = wallet.CustomerId,
            Balance = wallet.Balance,
            TotalLoaded = wallet.TotalLoaded,
            TotalSpent = wallet.TotalSpent,
            LastTransactionDate = wallet.LastTransactionDate,
            IsActive = wallet.IsActive,
            CreatedAt = wallet.CreatedAt,
            UpdatedAt = wallet.UpdatedAt
        };
    }

    private static WalletTransactionDto MapTransactionToDto(WalletTransaction transaction)
    {
        return new WalletTransactionDto
        {
            TransactionId = transaction.TransactionId,
            WalletId = transaction.WalletId,
            TransactionType = transaction.TransactionType.ToString(),
            Amount = transaction.Amount,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            ReferenceId = transaction.ReferenceId,
            OrderId = transaction.OrderId,
            CreatedAt = transaction.CreatedAt,
            CreatedBy = transaction.CreatedBy
        };
    }
}
