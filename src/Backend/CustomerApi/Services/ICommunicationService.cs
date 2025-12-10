using CustomerApi.DTOs;
using CustomerApi.Models;

namespace CustomerApi.Services;

public interface ICommunicationService
{
    Task<bool> SendMessageAsync(SendMessageRequest request);
    Task<List<CommunicationLogDto>> GetCommunicationLogsAsync(Guid customerId, int page = 1, int pageSize = 50);
    Task<List<CommunicationProviderDto>> GetProvidersAsync(CommunicationProvider? providerType = null);
    Task<CommunicationProviderDto> CreateProviderAsync(CommunicationProviderDto request);
    Task<CommunicationProviderDto?> UpdateProviderAsync(Guid providerId, CommunicationProviderDto request);
    Task<bool> DeleteProviderAsync(Guid providerId);
    Task<List<CommunicationTemplateDto>> GetTemplatesAsync(CommunicationProvider? providerType = null);
    Task<CommunicationTemplateDto> CreateTemplateAsync(CommunicationTemplateDto request);
    Task<CommunicationTemplateDto?> UpdateTemplateAsync(Guid templateId, CommunicationTemplateDto request);
    Task<bool> DeleteTemplateAsync(Guid templateId);
    Task<bool> RetryFailedMessageAsync(Guid logId);
    Task ProcessFailedMessagesAsync();
    Task<Dictionary<string, object>> GetCommunicationStatsAsync();
}
