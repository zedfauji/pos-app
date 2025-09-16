using CustomerApi.DTOs;
using CustomerApi.Models;

namespace CustomerApi.Services;

public interface IBehavioralTriggerService
{
    // Trigger management
    Task<BehavioralTriggerDto> CreateTriggerAsync(BehavioralTriggerDto request);
    Task<BehavioralTriggerDto?> UpdateTriggerAsync(Guid triggerId, BehavioralTriggerDto request);
    Task<bool> DeleteTriggerAsync(Guid triggerId);
    Task<BehavioralTriggerDto?> GetTriggerAsync(Guid triggerId);
    Task<List<BehavioralTriggerDto>> GetTriggersAsync(bool includeInactive = false, int page = 1, int pageSize = 50);

    // Trigger execution
    Task<List<TriggerExecutionDto>> ProcessCustomerTriggersAsync(Guid customerId);
    Task<Dictionary<string, object>> ProcessAllTriggersAsync();

    // Trigger analysis
    Task<List<TriggerExecutionDto>> GetTriggerExecutionsAsync(Guid triggerId, int page = 1, int pageSize = 50);
    Task<Dictionary<string, object>?> GetTriggerAnalyticsAsync(Guid triggerId);

    // Customer evaluation
    Task<List<CustomerDto>> GetCustomersMatchingConditionsAsync(string conditionsJson, int page = 1, int pageSize = 50);
    Task<bool> EvaluateCustomerAgainstTriggerAsync(Guid customerId, Guid triggerId);

    // Utility
    Task<List<BehavioralTriggerDto>> GetTriggersForCustomerAsync(Guid customerId);
    Task<bool> IsCustomerEligibleForTriggerAsync(Guid customerId, Guid triggerId);
}
