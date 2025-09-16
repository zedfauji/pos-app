using System.Text.Json;
using CustomerApi.Data;
using CustomerApi.DTOs;
using CustomerApi.Models;
using CustomerApi.Services.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CustomerApi.Services;

public class CommunicationService : ICommunicationService
{
    private readonly CustomerDbContext _context;
    private readonly ILogger<CommunicationService> _logger;
    private readonly Dictionary<CommunicationProvider, ICommunicationProvider> _providers;

    public CommunicationService(
        CustomerDbContext context,
        ILogger<CommunicationService> logger,
        IEnumerable<ICommunicationProvider> providers)
    {
        _context = context;
        _logger = logger;
        _providers = providers.ToDictionary(p => p.ProviderType, p => p);
    }

    public async Task<bool> SendMessageAsync(SendMessageRequest request)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId);
        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", request.CustomerId);
            return false;
        }

        // Determine recipient based on provider type
        var recipient = GetRecipientForProvider(customer, request.ProviderType);
        if (string.IsNullOrEmpty(recipient))
        {
            _logger.LogWarning("No {ProviderType} contact information for customer {CustomerId}", 
                request.ProviderType, request.CustomerId);
            return false;
        }

        // Get provider configuration
        var providerConfig = await GetActiveProviderConfigAsync(request.ProviderType);
        if (providerConfig == null)
        {
            _logger.LogError("No active provider configuration found for {ProviderType}", request.ProviderType);
            return false;
        }

        // Get message content from template if specified
        var subject = request.Subject;
        var messageBody = request.MessageBody;

        if (request.TemplateId.HasValue)
        {
            var template = await _context.CommunicationTemplates.FindAsync(request.TemplateId.Value);
            if (template != null && template.IsActive)
            {
                subject = ProcessTemplate(template.SubjectTemplate, customer, request.Variables);
                messageBody = ProcessTemplate(template.BodyTemplate, customer, request.Variables);
            }
        }

        // Create communication log entry
        var log = new CommunicationLog
        {
            CustomerId = request.CustomerId,
            ProviderId = providerConfig.ProviderId,
            TemplateId = request.TemplateId,
            CampaignId = request.CampaignId,
            TriggerId = request.TriggerId,
            ProviderType = request.ProviderType,
            Recipient = recipient,
            Subject = subject,
            MessageBody = messageBody,
            Status = MessageStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CommunicationLogs.Add(log);
        await _context.SaveChangesAsync();

        // Send message using appropriate provider
        if (_providers.TryGetValue(request.ProviderType, out var provider))
        {
            // Configure provider if needed
            await ConfigureProviderAsync(provider, providerConfig);

            var metadata = new Dictionary<string, object>
            {
                ["log_id"] = log.LogId.ToString(),
                ["customer_id"] = request.CustomerId.ToString()
            };

            if (request.CampaignId.HasValue)
                metadata["campaign_id"] = request.CampaignId.Value.ToString();

            var success = await provider.SendMessageAsync(recipient, subject ?? "", messageBody, metadata);

            // Update log status
            log.Status = success ? MessageStatus.Sent : MessageStatus.Failed;
            log.SentAt = success ? DateTime.UtcNow : null;
            log.FailedAt = success ? null : DateTime.UtcNow;
            log.FailureReason = success ? null : "Failed to send via provider";
            log.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (success)
            {
                _logger.LogInformation("Message sent successfully to {Recipient} via {Provider}", 
                    recipient, request.ProviderType);
            }
            else
            {
                _logger.LogError("Failed to send message to {Recipient} via {Provider}", 
                    recipient, request.ProviderType);
            }

            return success;
        }

        _logger.LogError("Provider {ProviderType} not available", request.ProviderType);
        return false;
    }

    public async Task<List<CommunicationLogDto>> GetCommunicationLogsAsync(Guid customerId, int page = 1, int pageSize = 50)
    {
        var logs = await _context.CommunicationLogs
            .Where(l => l.CustomerId == customerId)
            .Include(l => l.Customer)
            .Include(l => l.Provider)
            .Include(l => l.Template)
            .Include(l => l.Campaign)
            .Include(l => l.Trigger)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return logs.Select(MapLogToDto).ToList();
    }

    public async Task<List<CommunicationProviderDto>> GetProvidersAsync(CommunicationProvider? providerType = null)
    {
        var query = _context.CommunicationProviderConfigs.AsQueryable();

        if (providerType.HasValue)
        {
            query = query.Where(p => p.ProviderType == providerType.Value);
        }

        var providers = await query
            .OrderBy(p => p.ProviderType)
            .ThenBy(p => p.Priority)
            .ToListAsync();

        return providers.Select(MapProviderToDto).ToList();
    }

    public async Task<CommunicationProviderDto> CreateProviderAsync(CommunicationProviderDto request)
    {
        var provider = new CommunicationProviderConfig
        {
            ProviderName = request.ProviderName,
            ProviderType = request.ProviderType,
            ConfigurationJson = request.ConfigurationJson,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            Priority = request.Priority,
            RateLimitPerMinute = request.RateLimitPerMinute,
            RateLimitPerHour = request.RateLimitPerHour,
            RateLimitPerDay = request.RateLimitPerDay,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Ensure only one default provider per type
        if (provider.IsDefault)
        {
            await SetOtherProvidersNonDefaultAsync(provider.ProviderType);
        }

        _context.CommunicationProviderConfigs.Add(provider);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created communication provider {ProviderName} for {ProviderType}",
            provider.ProviderName, provider.ProviderType);

        return MapProviderToDto(provider);
    }

    public async Task<CommunicationProviderDto?> UpdateProviderAsync(Guid providerId, CommunicationProviderDto request)
    {
        var provider = await _context.CommunicationProviderConfigs.FindAsync(providerId);
        if (provider == null) return null;

        provider.ProviderName = request.ProviderName;
        provider.ConfigurationJson = request.ConfigurationJson;
        provider.IsActive = request.IsActive;
        provider.Priority = request.Priority;
        provider.RateLimitPerMinute = request.RateLimitPerMinute;
        provider.RateLimitPerHour = request.RateLimitPerHour;
        provider.RateLimitPerDay = request.RateLimitPerDay;
        provider.UpdatedAt = DateTime.UtcNow;

        // Handle default provider logic
        if (request.IsDefault && !provider.IsDefault)
        {
            await SetOtherProvidersNonDefaultAsync(provider.ProviderType);
            provider.IsDefault = true;
        }
        else if (!request.IsDefault && provider.IsDefault)
        {
            provider.IsDefault = false;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated communication provider {ProviderId}", providerId);

        return MapProviderToDto(provider);
    }

    public async Task<bool> DeleteProviderAsync(Guid providerId)
    {
        var provider = await _context.CommunicationProviderConfigs.FindAsync(providerId);
        if (provider == null) return false;

        // Don't delete if there are associated messages
        var hasMessages = await _context.CommunicationLogs.AnyAsync(l => l.ProviderId == providerId);
        if (hasMessages)
        {
            _logger.LogWarning("Cannot delete provider {ProviderId} - has associated messages", providerId);
            return false;
        }

        _context.CommunicationProviderConfigs.Remove(provider);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted communication provider {ProviderId}", providerId);
        return true;
    }

    public async Task<List<CommunicationTemplateDto>> GetTemplatesAsync(CommunicationProvider? providerType = null)
    {
        var query = _context.CommunicationTemplates.AsQueryable();

        if (providerType.HasValue)
        {
            query = query.Where(t => t.ProviderType == providerType.Value);
        }

        var templates = await query
            .Where(t => t.IsActive)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return templates.Select(MapTemplateToDto).ToList();
    }

    public async Task<CommunicationTemplateDto> CreateTemplateAsync(CommunicationTemplateDto request)
    {
        var template = new CommunicationTemplate
        {
            Name = request.Name,
            Description = request.Description,
            ProviderType = request.ProviderType,
            SubjectTemplate = request.SubjectTemplate,
            BodyTemplate = request.BodyTemplate,
            VariablesJson = request.VariablesJson,
            IsActive = request.IsActive,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CommunicationTemplates.Add(template);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created communication template {TemplateName} for {ProviderType}",
            template.Name, template.ProviderType);

        return MapTemplateToDto(template);
    }

    public async Task<CommunicationTemplateDto?> UpdateTemplateAsync(Guid templateId, CommunicationTemplateDto request)
    {
        var template = await _context.CommunicationTemplates.FindAsync(templateId);
        if (template == null) return null;

        template.Name = request.Name;
        template.Description = request.Description;
        template.SubjectTemplate = request.SubjectTemplate;
        template.BodyTemplate = request.BodyTemplate;
        template.VariablesJson = request.VariablesJson;
        template.IsActive = request.IsActive;
        template.Category = request.Category;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated communication template {TemplateId}", templateId);

        return MapTemplateToDto(template);
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId)
    {
        var template = await _context.CommunicationTemplates.FindAsync(templateId);
        if (template == null) return false;

        // Soft delete - just mark as inactive
        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted communication template {TemplateId}", templateId);
        return true;
    }

    public async Task<bool> RetryFailedMessageAsync(Guid logId)
    {
        var log = await _context.CommunicationLogs
            .Include(l => l.Customer)
            .Include(l => l.Provider)
            .FirstOrDefaultAsync(l => l.LogId == logId);

        if (log == null || !log.CanRetry) return false;

        var request = new SendMessageRequest
        {
            CustomerId = log.CustomerId,
            ProviderType = log.ProviderType,
            TemplateId = log.TemplateId,
            Subject = log.Subject,
            MessageBody = log.MessageBody,
            CampaignId = log.CampaignId,
            TriggerId = log.TriggerId
        };

        log.RetryCount++;
        log.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await SendMessageAsync(request);
    }

    public async Task ProcessFailedMessagesAsync()
    {
        var failedMessages = await _context.CommunicationLogs
            .Where(l => l.Status == MessageStatus.Failed && l.RetryCount < l.MaxRetries)
            .Where(l => l.FailedAt.HasValue && l.FailedAt.Value.AddMinutes(l.RetryCount * 5) < DateTime.UtcNow)
            .Take(100) // Process in batches
            .ToListAsync();

        var retryCount = 0;
        foreach (var log in failedMessages)
        {
            if (await RetryFailedMessageAsync(log.LogId))
            {
                retryCount++;
            }
        }

        _logger.LogInformation("Processed {Count} failed messages for retry", retryCount);
    }

    public async Task<Dictionary<string, object>> GetCommunicationStatsAsync()
    {
        var totalMessages = await _context.CommunicationLogs.CountAsync();
        var sentMessages = await _context.CommunicationLogs.CountAsync(l => l.Status == MessageStatus.Sent);
        var deliveredMessages = await _context.CommunicationLogs.CountAsync(l => l.Status == MessageStatus.Delivered);
        var failedMessages = await _context.CommunicationLogs.CountAsync(l => l.Status == MessageStatus.Failed);
        var openedMessages = await _context.CommunicationLogs.CountAsync(l => l.OpenedAt.HasValue);

        var messagesByProvider = await _context.CommunicationLogs
            .GroupBy(l => l.ProviderType)
            .Select(g => new { Provider = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Provider, x => (object)x.Count);

        return new Dictionary<string, object>
        {
            ["total_messages"] = totalMessages,
            ["sent_messages"] = sentMessages,
            ["delivered_messages"] = deliveredMessages,
            ["failed_messages"] = failedMessages,
            ["opened_messages"] = openedMessages,
            ["delivery_rate"] = totalMessages > 0 ? (double)deliveredMessages / totalMessages * 100 : 0,
            ["open_rate"] = deliveredMessages > 0 ? (double)openedMessages / deliveredMessages * 100 : 0,
            ["messages_by_provider"] = messagesByProvider
        };
    }

    private static string GetRecipientForProvider(Customer customer, CommunicationProvider providerType)
    {
        return providerType switch
        {
            CommunicationProvider.Email => customer.Email ?? "",
            CommunicationProvider.SMS => customer.Phone ?? "",
            CommunicationProvider.WhatsApp => customer.Phone ?? "",
            _ => ""
        };
    }

    private async Task<CommunicationProviderConfig?> GetActiveProviderConfigAsync(CommunicationProvider providerType)
    {
        return await _context.CommunicationProviderConfigs
            .Where(p => p.ProviderType == providerType && p.IsActive)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Priority)
            .FirstOrDefaultAsync();
    }

    private async Task ConfigureProviderAsync(ICommunicationProvider provider, CommunicationProviderConfig config)
    {
        if (!provider.IsConfigured)
        {
            var configuration = JsonSerializer.Deserialize<Dictionary<string, object>>(config.ConfigurationJson) 
                               ?? new Dictionary<string, object>();
            
            // Use reflection to call Configure method if it exists
            var configureMethod = provider.GetType().GetMethod("Configure");
            if (configureMethod != null)
            {
                configureMethod.Invoke(provider, new object[] { configuration });
            }
        }
    }

    private static string ProcessTemplate(string? template, Customer customer, Dictionary<string, object>? variables = null)
    {
        if (string.IsNullOrEmpty(template)) return "";

        var processed = template
            .Replace("{CustomerName}", customer.FirstName)
            .Replace("{FullName}", customer.FullName)
            .Replace("{Email}", customer.Email ?? "")
            .Replace("{Phone}", customer.Phone ?? "")
            .Replace("{MembershipLevel}", customer.MembershipLevel?.Name ?? "")
            .Replace("{LoyaltyPoints}", customer.LoyaltyPoints.ToString())
            .Replace("{TotalSpent}", customer.TotalSpent.ToString("C"));

        // Process custom variables if provided
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                processed = processed.Replace($"{{{variable.Key}}}", variable.Value?.ToString() ?? "");
            }
        }

        return processed;
    }

    private async Task SetOtherProvidersNonDefaultAsync(CommunicationProvider providerType)
    {
        var otherDefaults = await _context.CommunicationProviderConfigs
            .Where(p => p.ProviderType == providerType && p.IsDefault)
            .ToListAsync();

        foreach (var provider in otherDefaults)
        {
            provider.IsDefault = false;
        }
    }

    private static CommunicationLogDto MapLogToDto(CommunicationLog log)
    {
        return new CommunicationLogDto
        {
            LogId = log.LogId,
            CustomerId = log.CustomerId,
            CustomerName = log.Customer.FullName,
            ProviderId = log.ProviderId,
            ProviderName = log.Provider?.ProviderName,
            TemplateId = log.TemplateId,
            TemplateName = log.Template?.Name,
            CampaignId = log.CampaignId,
            CampaignName = log.Campaign?.Name,
            TriggerId = log.TriggerId,
            TriggerName = log.Trigger?.Name,
            ProviderType = log.ProviderType,
            Recipient = log.Recipient,
            Subject = log.Subject,
            MessageBody = log.MessageBody,
            Status = log.Status,
            ExternalMessageId = log.ExternalMessageId,
            SentAt = log.SentAt,
            DeliveredAt = log.DeliveredAt,
            OpenedAt = log.OpenedAt,
            ClickedAt = log.ClickedAt,
            FailedAt = log.FailedAt,
            FailureReason = log.FailureReason,
            RetryCount = log.RetryCount,
            MaxRetries = log.MaxRetries,
            CanRetry = log.CanRetry,
            DeliveryTime = log.DeliveryTime,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt
        };
    }

    private static CommunicationProviderDto MapProviderToDto(CommunicationProviderConfig provider)
    {
        return new CommunicationProviderDto
        {
            ProviderId = provider.ProviderId,
            ProviderName = provider.ProviderName,
            ProviderType = provider.ProviderType,
            ConfigurationJson = provider.ConfigurationJson,
            IsActive = provider.IsActive,
            IsDefault = provider.IsDefault,
            Priority = provider.Priority,
            RateLimitPerMinute = provider.RateLimitPerMinute,
            RateLimitPerHour = provider.RateLimitPerHour,
            RateLimitPerDay = provider.RateLimitPerDay,
            CreatedAt = provider.CreatedAt,
            UpdatedAt = provider.UpdatedAt
        };
    }

    private static CommunicationTemplateDto MapTemplateToDto(CommunicationTemplate template)
    {
        return new CommunicationTemplateDto
        {
            TemplateId = template.TemplateId,
            Name = template.Name,
            Description = template.Description,
            ProviderType = template.ProviderType,
            SubjectTemplate = template.SubjectTemplate,
            BodyTemplate = template.BodyTemplate,
            VariablesJson = template.VariablesJson,
            IsActive = template.IsActive,
            Category = template.Category,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            CreatedBy = template.CreatedBy
        };
    }
}
