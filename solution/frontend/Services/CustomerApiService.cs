using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Services;

public class CustomerApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CustomerApiService(HttpClient httpClient, ILogger<CustomerApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
    }

    // Customer Management
    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/customers/{customerId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CustomerDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by ID {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phone)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/customers/by-phone/{Uri.EscapeDataString(phone)}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CustomerDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by phone {Phone}", phone);
            return null;
        }
    }

    public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/customers/by-email/{Uri.EscapeDataString(email)}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CustomerDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer by email {Email}", email);
            return null;
        }
    }

    public async Task<CustomerSearchResponse?> SearchCustomersAsync(CustomerSearchRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/customers/search", content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                try
                {
                    return JsonSerializer.Deserialize<CustomerSearchResponse>(responseJson, _jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "JSON deserialization error. Response: {Response}", responseJson);
                    // Return empty response instead of null to prevent crashes
                    return new CustomerSearchResponse { Customers = new List<CustomerDto>(), TotalCount = 0 };
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return null;
        }
    }

    public async Task<CustomerDto?> CreateCustomerAsync(CreateCustomerRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/customers", content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CustomerDto>(responseJson, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return null;
        }
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/customers/{customerId}", content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CustomerDto>(responseJson, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<bool> DeleteCustomerAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/customers/{customerId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<CustomerStatsDto?> GetCustomerStatsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/customers/stats");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CustomerStatsDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer statistics");
            return null;
        }
    }

    public async Task<bool> ProcessOrderAsync(Guid customerId, ProcessOrderRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/customers/{customerId}/process-order", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> UpdateMembershipLevelAsync(Guid customerId, Guid membershipLevelId)
    {
        try
        {
            var request = new { MembershipLevelId = membershipLevelId };
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"api/customers/{customerId}/membership", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating membership level for customer {CustomerId}", customerId);
            return false;
        }
    }

    // Membership Management
    public async Task<List<MembershipLevelDto>?> GetAllMembershipLevelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/membership");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MembershipLevelDto>>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting membership levels");
            return null;
        }
    }

    public async Task<MembershipLevelDto?> GetMembershipLevelByIdAsync(Guid membershipLevelId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/membership/{membershipLevelId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MembershipLevelDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting membership level {MembershipLevelId}", membershipLevelId);
            return null;
        }
    }

    public async Task<MembershipLevelDto?> GetDefaultMembershipLevelAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/membership/default");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MembershipLevelDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default membership level");
            return null;
        }
    }

    // Wallet Management
    public async Task<WalletDto?> GetWalletByCustomerIdAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/wallet/customer/{customerId}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WalletDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<bool> AddFundsAsync(Guid customerId, AddWalletFundsRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/wallet/customer/{customerId}/add-funds", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding funds for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> DeductFundsAsync(Guid customerId, DeductWalletFundsRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/wallet/customer/{customerId}/deduct-funds", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting funds for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> CanDeductAsync(Guid customerId, decimal amount)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/wallet/customer/{customerId}/can-deduct/{amount}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(json, _jsonOptions);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking wallet balance for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<WalletTransactionResponse?> GetWalletTransactionsAsync(Guid customerId, int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/wallet/customer/{customerId}/transactions?page={page}&pageSize={pageSize}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<WalletTransactionResponse>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet transactions for customer {CustomerId}", customerId);
            return null;
        }
    }

    // Loyalty Management
    public async Task<int> GetLoyaltyPointsAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/loyalty/customer/{customerId}/points");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<int>(json, _jsonOptions);
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty points for customer {CustomerId}", customerId);
            return 0;
        }
    }

    public async Task<int> CalculatePointsForOrderAsync(Guid customerId, decimal orderAmount)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/loyalty/calculate-points/{customerId}?orderAmount={orderAmount}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<int>(json, _jsonOptions);
            }
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating loyalty points for customer {CustomerId}", customerId);
            return 0;
        }
    }

    public async Task<bool> AddLoyaltyPointsAsync(Guid customerId, AddLoyaltyPointsRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/loyalty/customer/{customerId}/add-points", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding loyalty points for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> RedeemLoyaltyPointsAsync(Guid customerId, RedeemLoyaltyPointsRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"api/loyalty/customer/{customerId}/redeem-points", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming loyalty points for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<bool> CanRedeemPointsAsync(Guid customerId, int points)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/loyalty/customer/{customerId}/can-redeem/{points}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(json, _jsonOptions);
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking loyalty points balance for customer {CustomerId}", customerId);
            return false;
        }
    }

    public async Task<LoyaltyTransactionResponse?> GetLoyaltyTransactionsAsync(Guid customerId, int page = 1, int pageSize = 20)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/loyalty/customer/{customerId}/transactions?page={page}&pageSize={pageSize}");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LoyaltyTransactionResponse>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty transactions for customer {CustomerId}", customerId);
            return null;
        }
    }

    public async Task<LoyaltyStatsDto?> GetLoyaltyStatsAsync(Guid customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/loyalty/customer/{customerId}/stats");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LoyaltyStatsDto>(json, _jsonOptions);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty statistics for customer {CustomerId}", customerId);
            return null;
        }
    }
}

// DTOs for frontend use (matching backend DTOs)
public record CustomerDto
{
    public Guid CustomerId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PhotoUrl { get; init; }
    public Guid MembershipLevelId { get; init; }
    [JsonIgnore]
    public MembershipLevelDto? MembershipLevel { get; init; }
    public DateTime? MembershipStartDate { get; init; }
    public DateTime? MembershipExpiryDate { get; init; }
    public bool IsMembershipExpired { get; init; }
    public int? DaysUntilExpiry { get; init; }
    public decimal TotalSpent { get; init; }
    public int TotalVisits { get; init; }
    public int LoyaltyPoints { get; init; }
    public WalletDto? Wallet { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? Notes { get; init; }
}

public record MembershipLevelDto
{
    public Guid MembershipLevelId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal LoyaltyMultiplier { get; init; }
    public decimal? MinimumSpendRequirement { get; init; }
    public int? ValidityMonths { get; init; }
    public string? ColorHex { get; init; }
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public decimal? MaxWalletBalance { get; init; }
    public bool FreeDelivery { get; init; }
    public bool PrioritySupport { get; init; }
    public int BirthdayBonusPoints { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record WalletDto
{
    public Guid WalletId { get; init; }
    public Guid CustomerId { get; init; }
    public decimal Balance { get; init; }
    public decimal TotalLoaded { get; init; }
    public decimal TotalSpent { get; init; }
    public DateTime? LastTransactionDate { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record WalletTransactionDto
{
    public Guid TransactionId { get; init; }
    public Guid WalletId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal BalanceAfter { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ReferenceId { get; init; }
    public Guid? OrderId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record LoyaltyTransactionDto
{
    public Guid TransactionId { get; init; }
    public Guid CustomerId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int Points { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public decimal? OrderAmount { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool IsExpired { get; init; }
    public Guid? RelatedTransactionId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

// Request/Response DTOs
public record CustomerSearchRequest
{
    public string? SearchTerm { get; init; }
    public Guid? MembershipLevelId { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsMembershipExpired { get; init; }
    public DateTime? CreatedAfter { get; init; }
    public DateTime? CreatedBefore { get; init; }
    public decimal? MinTotalSpent { get; init; }
    public decimal? MaxTotalSpent { get; init; }
    public int? MinLoyaltyPoints { get; init; }
    public string SortBy { get; init; } = "FirstName";
    public bool SortDescending { get; init; } = false;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record CustomerSearchResponse
{
    public List<CustomerDto> Customers { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record CreateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PhotoUrl { get; init; }
    public Guid? MembershipLevelId { get; init; }
    public DateTime? MembershipExpiryDate { get; init; }
    public string? Notes { get; init; }
}

public record UpdateCustomerRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? PhotoUrl { get; init; }
    public Guid? MembershipLevelId { get; init; }
    public DateTime? MembershipExpiryDate { get; init; }
    public bool? IsActive { get; init; }
    public string? Notes { get; init; }
}

public record ProcessOrderRequest
{
    public Guid OrderId { get; init; }
    public decimal OrderAmount { get; init; }
    public decimal? WalletAmountUsed { get; init; }
    public int? LoyaltyPointsRedeemed { get; init; }
    public string? CreatedBy { get; init; }
}

public record AddWalletFundsRequest
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ReferenceId { get; init; }
}

public record DeductWalletFundsRequest
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ReferenceId { get; init; }
    public Guid? OrderId { get; init; }
}

public record WalletTransactionResponse
{
    public List<WalletTransactionDto> Transactions { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record AddLoyaltyPointsRequest
{
    public int Points { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
    public decimal? OrderAmount { get; init; }
}

public record RedeemLoyaltyPointsRequest
{
    public int Points { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid? OrderId { get; init; }
}

public record LoyaltyTransactionResponse
{
    public List<LoyaltyTransactionDto> Transactions { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public record CustomerStatsDto
{
    public int TotalCustomers { get; init; }
    public int ActiveCustomers { get; init; }
    public int ExpiredMemberships { get; init; }
    public decimal TotalCustomerValue { get; init; }
    public decimal AverageCustomerValue { get; init; }
    public int TotalLoyaltyPoints { get; init; }
    public Dictionary<string, int> CustomersByMembershipLevel { get; init; } = new();
    public Dictionary<string, decimal> RevenueByMembershipLevel { get; init; } = new();
}

public record LoyaltyStatsDto
{
    public int CurrentBalance { get; init; }
    public int TotalEarned { get; init; }
    public int TotalRedeemed { get; init; }
    public int TotalExpired { get; init; }
    public int ExpiringIn30Days { get; init; }
}
