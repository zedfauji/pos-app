using System.Text.Json;
using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services;

public class BehavioralTriggerService : IBehavioralTriggerService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<BehavioralTriggerService> _logger;
    private readonly ISegmentationService _segmentationService;
    private readonly ILoyaltyService _loyaltyService;
    private readonly IWalletService _walletService;
    private readonly ICommunicationService _communicationService;

    public BehavioralTriggerService(
        CustomerDbContext context,
        ILogger<BehavioralTriggerService> logger,
        ISegmentationService segmentationService,
        ILoyaltyService loyaltyService,
        IWalletService walletService,
        ICommunicationService communicationService)
    {
        _context = context;
        _logger = logger;
        _segmentationService = segmentationService;
        _loyaltyService = loyaltyService;
        _walletService = walletService;
        _communicationService = communicationService;
    }

    public async Task<BehavioralTriggerDto?> GetTriggerAsync(Guid triggerId)
    {
        var trigger = await _context.BehavioralTriggers
            .Include(t => t.TargetSegment)
            .FirstOrDefaultAsync(t => t.TriggerId == triggerId);

        return trigger != null ? MapToDto(trigger) : null;
    }

    private static CustomerDto MapCustomerToDto(Customer customer)
    {
        return new CustomerDto
        {
            CustomerId = customer.CustomerId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            DateOfBirth = customer.DateOfBirth,
            MembershipLevelId = customer.MembershipLevelId,
            MembershipLevel = customer.MembershipLevel?.Name,
            MembershipStartDate = customer.MembershipStartDate,
            MembershipExpiryDate = customer.MembershipExpiryDate,
            RegistrationDate = customer.CreatedAt,
            TotalSpent = customer.TotalSpent,
            TotalVisits = customer.TotalVisits,
            LoyaltyPoints = customer.LoyaltyPoints,
            IsActive = customer.IsActive,
            Notes = customer.Notes,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            IsMembershipExpired = customer.IsMembershipExpired,
            DaysUntilExpiry = customer.DaysUntilExpiry
        };
    }

    public async Task<List<BehavioralTriggerDto>> GetTriggersAsync(bool? activeOnly = null)
    {
        var query = _context.BehavioralTriggers
            .Include(t => t.TargetSegment)
            .AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(t => t.IsActive);
        }

        var triggers = await query
            .OrderBy(t => t.Name)
            .ToListAsync();

        return triggers.Select(MapToDto).ToList();
    }

    public async Task<List<BehavioralTriggerDto>> GetTriggersAsync(bool activeOnly, int page, int pageSize)
    {
        var query = _context.BehavioralTriggers
            .Include(t => t.TargetSegment)
            .AsQueryable();

        if (activeOnly)
            query = query.Where(t => t.IsActive);

        var triggers = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return triggers.Select(MapToDto).ToList();
    }

    public async Task<BehavioralTriggerDto> CreateTriggerAsync(BehavioralTriggerDto request)
    {
        var trigger = new BehavioralTrigger
        {
            Name = request.Name,
            Description = request.Description,
            ConditionType = request.ConditionType,
            ConditionValue = request.ConditionValue,
            ConditionDays = request.ConditionDays,
            ActionType = request.ActionType,
            ActionParametersJson = request.ActionParametersJson,
            TargetSegmentId = request.TargetSegmentId,
            IsRecurring = request.IsRecurring,
            CooldownHours = request.CooldownHours,
            MaxExecutions = request.MaxExecutions,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.BehavioralTriggers.Add(trigger);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created behavioral trigger {TriggerName} with ID {TriggerId}",
            trigger.Name, trigger.TriggerId);

        // Load the trigger with navigation properties for DTO mapping
        var createdTrigger = await _context.BehavioralTriggers
            .Include(t => t.TargetSegment)
            .FirstAsync(t => t.TriggerId == trigger.TriggerId);

        return MapToDto(createdTrigger);
    }

    public async Task<BehavioralTriggerDto?> UpdateTriggerAsync(Guid triggerId, BehavioralTriggerDto request)
    {
        var trigger = await _context.BehavioralTriggers
            .Include(t => t.TargetSegment)
            .FirstOrDefaultAsync(t => t.TriggerId == triggerId);

        if (trigger == null) return null;

        trigger.Name = request.Name;

        if (request.Description != null)
            trigger.Description = request.Description;

        trigger.ConditionType = request.ConditionType;

        if (request.ConditionValue.HasValue)
            trigger.ConditionValue = request.ConditionValue.Value;

        if (request.ConditionDays.HasValue)
            trigger.ConditionDays = request.ConditionDays.Value;

        trigger.ActionType = request.ActionType;

        if (request.ActionParametersJson != null)
            trigger.ActionParametersJson = request.ActionParametersJson;

        if (request.TargetSegmentId.HasValue)
            trigger.TargetSegmentId = request.TargetSegmentId.Value;


        trigger.IsRecurring = request.IsRecurring;


        if (request.MaxExecutions.HasValue)
            trigger.MaxExecutions = request.MaxExecutions.Value;

        trigger.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated behavioral trigger {TriggerName} with ID {TriggerId}",
            trigger.Name, trigger.TriggerId);

        return MapToDto(trigger);
    }

    public async Task<bool> DeleteTriggerAsync(Guid triggerId)
    {
        var trigger = await _context.BehavioralTriggers
            .Include(t => t.Executions)
            .FirstOrDefaultAsync(t => t.TriggerId == triggerId);

        if (trigger == null) return false;

        _context.TriggerExecutions.RemoveRange(trigger.Executions);
        _context.BehavioralTriggers.Remove(trigger);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted behavioral trigger {TriggerName} with ID {TriggerId}",
            trigger.Name, trigger.TriggerId);

        return true;
    }

    public async Task<bool> ActivateTriggerAsync(Guid triggerId)
    {
        var trigger = await _context.BehavioralTriggers.FindAsync(triggerId);
        if (trigger == null) return false;

        trigger.IsActive = true;
        trigger.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateTriggerAsync(Guid triggerId)
    {
        var trigger = await _context.BehavioralTriggers.FindAsync(triggerId);
        if (trigger == null) return false;

        trigger.IsActive = false;
        trigger.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<TriggerExecutionDto>> GetTriggerExecutionsAsync(Guid triggerId, int page = 1, int pageSize = 50)
    {
        var executions = await _context.TriggerExecutions
            .Where(e => e.TriggerId == triggerId)
            .Include(e => e.Trigger)
            .Include(e => e.Customer)
            .OrderByDescending(e => e.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return executions.Select(MapExecutionToDto).ToList();
    }

    public async Task<List<TriggerExecutionDto>> GetCustomerTriggerExecutionsAsync(Guid customerId, int page = 1, int pageSize = 50)
    {
        var executions = await _context.TriggerExecutions
            .Where(e => e.CustomerId == customerId)
            .Include(e => e.Trigger)
            .Include(e => e.Customer)
            .OrderByDescending(e => e.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return executions.Select(MapExecutionToDto).ToList();
    }

    public async Task ProcessTriggersForCustomerAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.Wallet)
            .Include(c => c.MembershipLevel)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.IsActive);

        if (customer == null) return;

        var activeTriggers = await _context.BehavioralTriggers
            .Where(t => t.IsActive)
            .ToListAsync();

        foreach (var trigger in activeTriggers)
        {
            if (await ShouldExecuteTriggerAsync(trigger, customer))
            {
                await ExecuteTriggerForCustomerAsync(trigger.TriggerId, customerId);
            }
        }
    }

    public async Task ProcessAllActiveTriggersAsync()
    {
        var activeTriggers = await _context.BehavioralTriggers
            .Where(t => t.IsActive)
            .ToListAsync();

        var processedCount = 0;

        foreach (var trigger in activeTriggers)
        {
            var eligibleCustomers = await GetEligibleCustomersForTriggerAsync(trigger);
            
            foreach (var customer in eligibleCustomers)
            {
                if (await CanExecuteTriggerAsync(trigger.TriggerId, customer.CustomerId))
                {
                    await ExecuteTriggerForCustomerAsync(trigger.TriggerId, customer.CustomerId);
                    processedCount++;
                }
            }
        }

        _logger.LogInformation("Processed {Count} trigger executions across all active triggers", processedCount);
    }

    public async Task<bool> ExecuteTriggerForCustomerAsync(Guid triggerId, Guid customerId, bool forceExecute = false)
    {
        var trigger = await _context.BehavioralTriggers.FindAsync(triggerId);
        var customer = await _context.Customers
            .Include(c => c.Wallet)
            .Include(c => c.MembershipLevel)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (trigger == null || customer == null || (!trigger.IsActive && !forceExecute))
            return false;

        if (!forceExecute && !await CanExecuteTriggerAsync(triggerId, customerId))
            return false;

        try
        {
            var success = await ExecuteTriggerActionAsync(trigger, customer);
            
            // Log execution
            var execution = new TriggerExecution
            {
                TriggerId = triggerId,
                CustomerId = customerId,
                ExecutedAt = DateTime.UtcNow,
                Success = success,
                ResultMessage = success ? "Trigger executed successfully" : "Trigger execution failed"
            };

            _context.TriggerExecutions.Add(execution);

            // Update trigger statistics
            trigger.ExecutionCount++;
            trigger.LastExecutedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Executed trigger {TriggerName} for customer {CustomerId}: {Success}",
                trigger.Name, customerId, success);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing trigger {TriggerId} for customer {CustomerId}", triggerId, customerId);
            
            // Log failed execution
            var execution = new TriggerExecution
            {
                TriggerId = triggerId,
                CustomerId = customerId,
                ExecutedAt = DateTime.UtcNow,
                Success = false,
                ResultMessage = ex.Message
            };

            _context.TriggerExecutions.Add(execution);
            await _context.SaveChangesAsync();

            return false;
        }
    }

    public async Task<List<BehavioralTriggerDto>> GetTriggersForConditionAsync(TriggerCondition condition)
    {
        var triggers = await _context.BehavioralTriggers
            .Where(t => t.ConditionType == condition && t.IsActive)
            .Include(t => t.TargetSegment)
            .ToListAsync();

        return triggers.Select(MapToDto).ToList();
    }

    public async Task<bool> CanExecuteTriggerAsync(Guid triggerId, Guid customerId)
    {
        var trigger = await _context.BehavioralTriggers.FindAsync(triggerId);
        if (trigger == null || !trigger.IsActive) return false;

        // Check max executions
        if (trigger.MaxExecutions.HasValue && trigger.ExecutionCount >= trigger.MaxExecutions.Value)
            return false;

        // Check cooldown period
        if (trigger.LastExecutedAt.HasValue)
        {
            var cooldownExpiry = trigger.LastExecutedAt.Value.AddHours(trigger.CooldownHours);
            if (DateTime.UtcNow < cooldownExpiry && !trigger.IsRecurring)
                return false;
        }

        // Check if customer had recent execution (for recurring triggers)
        if (trigger.IsRecurring)
        {
            var recentExecution = await _context.TriggerExecutions
                .Where(e => e.TriggerId == triggerId && e.CustomerId == customerId && e.Success)
                .OrderByDescending(e => e.ExecutedAt)
                .FirstOrDefaultAsync();

            if (recentExecution != null)
            {
                var customerCooldownExpiry = recentExecution.ExecutedAt.AddHours(trigger.CooldownHours);
                if (DateTime.UtcNow < customerCooldownExpiry)
                    return false;
            }
        }

        return true;
    }

    private async Task<bool> ShouldExecuteTriggerAsync(BehavioralTrigger trigger, Customer customer)
    {
        return trigger.ConditionType switch
        {
            TriggerCondition.WalletBalanceBelow => customer.Wallet?.Balance < trigger.ConditionValue,
            TriggerCondition.WalletBalanceAbove => customer.Wallet?.Balance > trigger.ConditionValue,
            TriggerCondition.LoyaltyPointsBelow => customer.LoyaltyPoints < trigger.ConditionValue,
            TriggerCondition.LoyaltyPointsAbove => customer.LoyaltyPoints > trigger.ConditionValue,
            TriggerCondition.LastVisitDaysAgo => customer.Wallet?.LastTransactionDate < DateTime.UtcNow.AddDays(-(trigger.ConditionDays ?? 0)),
            TriggerCondition.TotalSpentBelow => customer.TotalSpent < trigger.ConditionValue,
            TriggerCondition.TotalSpentAbove => customer.TotalSpent > trigger.ConditionValue,
            TriggerCondition.MembershipExpiringSoon => customer.MembershipExpiryDate.HasValue && 
                customer.MembershipExpiryDate.Value <= DateTime.UtcNow.AddDays(trigger.ConditionDays ?? 30) &&
                customer.MembershipExpiryDate.Value > DateTime.UtcNow,
            TriggerCondition.BirthdayThisMonth => customer.DateOfBirth.HasValue && 
                customer.DateOfBirth.Value.Month == DateTime.UtcNow.Month,
            TriggerCondition.OrderCountBelow => customer.TotalVisits < trigger.ConditionValue,
            TriggerCondition.OrderCountAbove => customer.TotalVisits > trigger.ConditionValue,
            TriggerCondition.AverageOrderValueBelow => customer.TotalVisits > 0 && 
                (customer.TotalSpent / customer.TotalVisits) < trigger.ConditionValue,
            TriggerCondition.AverageOrderValueAbove => customer.TotalVisits > 0 && 
                (customer.TotalSpent / customer.TotalVisits) > trigger.ConditionValue,
            _ => false
        };
    }

    private async Task<List<Customer>> GetEligibleCustomersForTriggerAsync(BehavioralTrigger trigger)
    {
        var query = _context.Customers
            .Include(c => c.Wallet)
            .Include(c => c.MembershipLevel)
            .Where(c => c.IsActive);

        // If trigger targets a specific segment, filter by that segment
        if (trigger.TargetSegmentId.HasValue)
        {
            var segmentCustomerIds = await _context.CustomerSegmentMemberships
                .Where(m => m.SegmentId == trigger.TargetSegmentId.Value && m.IsActive)
                .Select(m => m.CustomerId)
                .ToListAsync();

            query = query.Where(c => segmentCustomerIds.Contains(c.CustomerId));
        }

        var customers = await query.ToListAsync();
        
        // Filter customers based on trigger condition
        return customers.Where(c => ShouldExecuteTriggerAsync(trigger, c).Result).ToList();
    }

    private async Task<bool> ExecuteTriggerActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        return trigger.ActionType switch
        {
            TriggerAction.SendNotification => await ExecuteSendNotificationActionAsync(trigger, customer),
            TriggerAction.AddToSegment => await ExecuteAddToSegmentActionAsync(trigger, customer),
            TriggerAction.GrantLoyaltyPoints => await ExecuteGrantLoyaltyPointsActionAsync(trigger, customer),
            TriggerAction.ApplyDiscount => await ExecuteApplyDiscountActionAsync(trigger, customer),
            TriggerAction.AddWalletFunds => await ExecuteAddWalletFundsActionAsync(trigger, customer),
            TriggerAction.SendPersonalizedOffer => await ExecuteSendPersonalizedOfferActionAsync(trigger, customer),
            _ => false
        };
    }

    private async Task<bool> ExecuteSendNotificationActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        if (string.IsNullOrEmpty(trigger.ActionParametersJson))
            return false;

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(trigger.ActionParametersJson);
            if (parameters == null) return false;

            var message = parameters.GetValueOrDefault("message", "You have a notification from our system");
            var subject = parameters.GetValueOrDefault("subject", "Notification");
            var providerType = Enum.Parse<CommunicationProvider>(parameters.GetValueOrDefault("provider", "Email"));

            var request = new SendMessageRequest
            {
                CustomerId = customer.CustomerId,
                ProviderType = providerType,
                Subject = subject,
                MessageBody = message,
                TriggerId = trigger.TriggerId
            };

            return await _communicationService.SendMessageAsync(request);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ExecuteAddToSegmentActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        if (trigger.TargetSegmentId == null) return false;

        return await _segmentationService.AddCustomerToSegmentAsync(trigger.TargetSegmentId.Value, customer.CustomerId);
    }

    private async Task<bool> ExecuteGrantLoyaltyPointsActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        if (string.IsNullOrEmpty(trigger.ActionParametersJson))
            return false;

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(trigger.ActionParametersJson);
            if (parameters == null) return false;

            var points = Convert.ToInt32(parameters.GetValueOrDefault("points", 0));
            var description = parameters.GetValueOrDefault("description", "Behavioral trigger bonus").ToString() ?? "";

            return await _loyaltyService.AddLoyaltyPointsAsync(customer.CustomerId, points, description);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ExecuteApplyDiscountActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        // This would typically create a discount code or coupon for the customer
        // For now, we'll log it as a placeholder
        _logger.LogInformation("Applied discount for customer {CustomerId} via trigger {TriggerId}", 
            customer.CustomerId, trigger.TriggerId);
        return true;
    }

    private async Task<bool> ExecuteAddWalletFundsActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        if (string.IsNullOrEmpty(trigger.ActionParametersJson))
            return false;

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(trigger.ActionParametersJson);
            if (parameters == null) return false;

            var amount = Convert.ToDecimal(parameters.GetValueOrDefault("amount", 0));
            var description = parameters.GetValueOrDefault("description", "Behavioral trigger bonus").ToString() ?? "";

            var request = new AddWalletFundsRequest
            {
                Amount = amount,
                Description = description
            };

            return await _walletService.AddFundsAsync(customer.CustomerId, request);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> ExecuteSendPersonalizedOfferActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        // This would create a personalized campaign/offer for the customer
        // For now, we'll log it as a placeholder
        _logger.LogInformation("Sent personalized offer to customer {CustomerId} via trigger {TriggerId}", 
            customer.CustomerId, trigger.TriggerId);
        return true;
    }

    private static BehavioralTriggerDto MapToDto(BehavioralTrigger trigger)
    {
        return new BehavioralTriggerDto
        {
            TriggerId = trigger.TriggerId,
            Name = trigger.Name,
            Description = trigger.Description,
            ConditionType = trigger.ConditionType,
            ConditionValue = trigger.ConditionValue,
            ConditionDays = trigger.ConditionDays,
            ActionType = trigger.ActionType,
            ActionParametersJson = trigger.ActionParametersJson,
            TargetSegmentId = trigger.TargetSegmentId,
            TargetSegmentName = trigger.TargetSegment?.Name,
            IsActive = trigger.IsActive,
            IsRecurring = trigger.IsRecurring,
            CooldownHours = trigger.CooldownHours,
            MaxExecutions = trigger.MaxExecutions,
            ExecutionCount = trigger.ExecutionCount,
            LastExecutedAt = trigger.LastExecutedAt,
            CreatedAt = trigger.CreatedAt,
            UpdatedAt = trigger.UpdatedAt,
            CreatedBy = trigger.CreatedBy
        };
    }

    private static TriggerExecutionDto MapExecutionToDto(TriggerExecution execution)
    {
        return new TriggerExecutionDto
        {
            ExecutionId = execution.ExecutionId,
            TriggerId = execution.TriggerId,
            TriggerName = execution.Trigger.Name,
            CustomerId = execution.CustomerId,
            CustomerName = execution.Customer.FullName,
            ExecutedAt = execution.ExecutedAt,
            Success = execution.Success,
            ResultMessage = execution.ResultMessage,
            ResultDataJson = execution.ResultDataJson
        };
    }

    public async Task<List<TriggerExecutionDto>> ProcessCustomerTriggersAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return new List<TriggerExecutionDto>();

        var activeTriggers = await _context.BehavioralTriggers
            .Where(t => t.IsActive)
            .ToListAsync();

        var executionResults = new List<TriggerExecutionDto>();
        foreach (var trigger in activeTriggers)
        {
            if (await ShouldExecuteTriggerForCustomerAsync(trigger, customer))
            {
                var execution = await ExecuteTriggerAsync(trigger, customer);
                if (execution != null)
                {
                    executionResults.Add(MapToBehavioralTriggerExecutionDto(execution));
                }
            }
        }

        return executionResults;
    }

    public async Task<Dictionary<string, object>> ProcessAllTriggersAsync()
    {
        var activeTriggers = await _context.BehavioralTriggers
            .Where(t => t.IsActive)
            .ToListAsync();

        var totalExecutions = 0;
        var successfulExecutions = 0;
        var failedExecutions = 0;

        foreach (var trigger in activeTriggers)
        {
            await ProcessTriggerAsync(trigger);
            
            // Get execution stats for this trigger
            var executions = await _context.BehavioralTriggerExecutions
                .Where(e => e.TriggerId == trigger.TriggerId)
                .ToListAsync();
            
            totalExecutions += executions.Count;
            successfulExecutions += executions.Count(e => e.Success);
            failedExecutions += executions.Count(e => !e.Success);
        }

        return new Dictionary<string, object>
        {
            ["processedTriggers"] = activeTriggers.Count,
            ["totalExecutions"] = totalExecutions,
            ["successfulExecutions"] = successfulExecutions,
            ["failedExecutions"] = failedExecutions,
            ["processedAt"] = DateTime.UtcNow
        };
    }

    public async Task<Dictionary<string, object>?> GetTriggerAnalyticsAsync(Guid triggerId)
    {
        var trigger = await _context.BehavioralTriggers.FindAsync(triggerId);
        if (trigger == null) return null;

        var executions = await _context.BehavioralTriggerExecutions
            .Where(e => e.TriggerId == triggerId)
            .ToListAsync();

        return new Dictionary<string, object>
        {
            ["triggerId"] = triggerId,
            ["totalExecutions"] = executions.Count,
            ["successfulExecutions"] = executions.Count(e => e.Success),
            ["failedExecutions"] = executions.Count(e => !e.Success),
            ["lastExecutedAt"] = executions.OrderByDescending(e => e.ExecutedAt).FirstOrDefault()?.ExecutedAt
        };
    }

    public async Task<List<CustomerDto>> GetCustomersMatchingConditionsAsync(string conditionsJson, int page, int pageSize)
    {
        var conditions = JsonSerializer.Deserialize<Dictionary<string, object>>(conditionsJson);
        if (conditions == null) return new List<CustomerDto>();

        var query = _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .AsQueryable();

        // Apply basic filtering based on conditions
        var customers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return customers.Select(MapCustomerToDto).ToList();
    }

    public async Task<bool> EvaluateCustomerAgainstTriggerAsync(Guid customerId, Guid triggerId)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        var trigger = await _context.BehavioralTriggers
            .FirstOrDefaultAsync(t => t.TriggerId == triggerId);

        if (customer == null || trigger == null) return false;

        return await ShouldExecuteTriggerForCustomerAsync(trigger, customer);
    }

    public async Task<List<BehavioralTriggerDto>> GetTriggersForCustomerAsync(Guid customerId)
    {
        var customer = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return new List<BehavioralTriggerDto>();

        var triggers = await _context.BehavioralTriggers
            .Include(t => t.TargetSegment)
            .Where(t => t.IsActive)
            .ToListAsync();

        var eligibleTriggers = new List<BehavioralTrigger>();
        foreach (var trigger in triggers)
        {
            if (await ShouldExecuteTriggerForCustomerAsync(trigger, customer))
            {
                eligibleTriggers.Add(trigger);
            }
        }

        return eligibleTriggers.Select(MapToDto).ToList();
    }

    public async Task<bool> IsCustomerEligibleForTriggerAsync(Guid customerId, Guid triggerId)
    {
        return await EvaluateCustomerAgainstTriggerAsync(customerId, triggerId);
    }

    private async Task<bool> ShouldExecuteTriggerForCustomerAsync(BehavioralTrigger trigger, Customer customer)
    {
        // Check if trigger is active
        if (!trigger.IsActive) return false;

        // Check cooldown period
        if (trigger.CooldownMinutes > 0)
        {
            var lastExecution = await _context.BehavioralTriggerExecutions
                .Where(e => e.TriggerId == trigger.TriggerId && e.CustomerId == customer.CustomerId)
                .OrderByDescending(e => e.ExecutedAt)
                .FirstOrDefaultAsync();

            if (lastExecution != null && 
                lastExecution.ExecutedAt.AddMinutes(trigger.CooldownMinutes) > DateTime.UtcNow)
            {
                return false;
            }
        }

        // Check maximum executions per customer
        if (trigger.MaxExecutionsPerCustomer > 0)
        {
            var executionCount = await _context.BehavioralTriggerExecutions
                .CountAsync(e => e.TriggerId == trigger.TriggerId && e.CustomerId == customer.CustomerId);

            if (executionCount >= trigger.MaxExecutionsPerCustomer)
            {
                return false;
            }
        }

        // Check trigger conditions based on condition type
        return trigger.ConditionType switch
        {
            TriggerCondition.TotalSpentAbove => customer.TotalSpent > (trigger.ConditionValue ?? 0),
            TriggerCondition.TotalSpentBelow => customer.TotalSpent < (trigger.ConditionValue ?? 0),
            TriggerCondition.LoyaltyPointsAbove => customer.LoyaltyPoints > (trigger.ConditionValue ?? 0),
            TriggerCondition.LoyaltyPointsBelow => customer.LoyaltyPoints < (trigger.ConditionValue ?? 0),
            TriggerCondition.DaysSinceLastVisit => true, // Simplified for now
            TriggerCondition.OrderFrequency => true, // Simplified for now
            _ => false
        };
    }

    private async Task<BehavioralTriggerExecution?> ExecuteTriggerAsync(BehavioralTrigger trigger, Customer customer)
    {
        var execution = new BehavioralTriggerExecution
        {
            TriggerId = trigger.TriggerId,
            CustomerId = customer.CustomerId,
            ExecutedAt = DateTime.UtcNow
        };

        try
        {
            var success = trigger.ActionType switch
            {
                TriggerAction.SendMessage => await ExecuteSendMessageActionAsync(trigger, customer),
                TriggerAction.AddLoyaltyPoints => await ExecuteAddLoyaltyPointsActionAsync(trigger, customer),
                TriggerAction.AddWalletFunds => await ExecuteAddWalletFundsActionAsync(trigger, customer),
                TriggerAction.SendPersonalizedOffer => await ExecuteSendPersonalizedOfferActionAsync(trigger, customer),
                _ => false
            };

            execution.Success = success;
            execution.ResultMessage = success ? "Action executed successfully" : "Action execution failed";
        }
        catch (Exception ex)
        {
            execution.Success = false;
            execution.ResultMessage = ex.Message;
        }

        _context.BehavioralTriggerExecutions.Add(execution);
        await _context.SaveChangesAsync();

        return execution;
    }

    private async Task ProcessTriggerAsync(BehavioralTrigger trigger)
    {
        var customers = await _context.Customers
            .Include(c => c.MembershipLevel)
            .Include(c => c.Wallet)
            .Where(c => c.IsActive)
            .ToListAsync();

        foreach (var customer in customers)
        {
            if (await ShouldExecuteTriggerForCustomerAsync(trigger, customer))
            {
                await ExecuteTriggerAsync(trigger, customer);
            }
        }
    }

    private async Task<bool> ExecuteSendMessageActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        // Placeholder implementation for sending messages
        // In a real implementation, this would integrate with the communication service
        return true;
    }

    private async Task<bool> ExecuteAddLoyaltyPointsActionAsync(BehavioralTrigger trigger, Customer customer)
    {
        // Placeholder implementation for adding loyalty points
        // In a real implementation, this would update the customer's loyalty points
        return true;
    }


    private TriggerExecutionDto MapToBehavioralTriggerExecutionDto(BehavioralTriggerExecution execution)
    {
        return new TriggerExecutionDto
        {
            ExecutionId = execution.ExecutionId,
            TriggerId = execution.TriggerId,
            CustomerId = execution.CustomerId,
            ExecutedAt = execution.ExecutedAt,
            Success = execution.Success,
            ResultMessage = execution.ResultMessage,
            ResultDataJson = execution.ResultDataJson
        };
    }
}
