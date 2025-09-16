using CustomerApi.DTOs;
using CustomerApi.Models;

namespace CustomerApi.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId);
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phone);
    Task<CustomerDto?> GetCustomerByEmailAsync(string email);
    Task<(List<CustomerDto> Customers, int TotalCount)> SearchCustomersAsync(CustomerSearchRequest request);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerDto?> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request);
    Task<bool> DeleteCustomerAsync(Guid customerId);
    Task<CustomerStatsDto> GetCustomerStatsAsync();
    Task<bool> ProcessOrderAsync(Guid customerId, ProcessOrderRequest request);
    Task<bool> UpdateMembershipLevelAsync(Guid customerId, Guid membershipLevelId);
    Task<List<CustomerDto>> GetExpiringMembershipsAsync(int daysAhead = 30);
    Task<int> ProcessMembershipExpiriesAsync();
}

public interface IMembershipService
{
    Task<List<MembershipLevelDto>> GetAllMembershipLevelsAsync();
    Task<MembershipLevelDto?> GetMembershipLevelByIdAsync(Guid membershipLevelId);
    Task<MembershipLevelDto> GetDefaultMembershipLevelAsync();
    Task<bool> CanUpgradeMembershipAsync(Guid customerId, Guid targetMembershipLevelId);
    Task<MembershipLevelDto?> GetRecommendedMembershipAsync(decimal totalSpent);
}

public interface IWalletService
{
    Task<WalletDto?> GetWalletByCustomerIdAsync(Guid customerId);
    Task<WalletDto> CreateWalletAsync(Guid customerId);
    Task<bool> AddFundsAsync(Guid customerId, AddWalletFundsRequest request);
    Task<bool> DeductFundsAsync(Guid customerId, decimal amount, string description, string? referenceId = null, Guid? orderId = null);
    Task<bool> CanDeductAsync(Guid customerId, decimal amount);
    Task<(List<WalletTransactionDto> Transactions, int TotalCount)> GetWalletTransactionsAsync(Guid customerId, int page = 1, int pageSize = 20);
}

public interface ILoyaltyService
{
    Task<int> GetLoyaltyPointsAsync(Guid customerId);
    Task<int> CalculatePointsForOrderAsync(Guid customerId, decimal orderAmount);
    Task<bool> AddLoyaltyPointsAsync(Guid customerId, int points, string description, Guid? orderId = null, decimal? orderAmount = null);
    Task<bool> RedeemLoyaltyPointsAsync(Guid customerId, int points, string description, Guid? orderId = null);
    Task<bool> CanRedeemPointsAsync(Guid customerId, int points);
    Task<(List<LoyaltyTransactionDto> Transactions, int TotalCount)> GetLoyaltyTransactionsAsync(Guid customerId, int page = 1, int pageSize = 20);
    Task<int> ProcessExpiredPointsAsync();
    Task<List<LoyaltyTransactionDto>> GetExpiringPointsAsync(Guid customerId, int daysAhead = 30);
    Task<LoyaltyStatsDto> GetLoyaltyStatsAsync(Guid customerId);
    Task<int> ProcessBirthdayBonusAsync();
}
