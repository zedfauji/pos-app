using CustomerApi.DTOs;

namespace CustomerApi.Services;

public interface ISegmentationService
{
    // Segment management
    Task<CustomerSegmentDto> CreateSegmentAsync(CustomerSegmentDto request);
    Task<CustomerSegmentDto?> UpdateSegmentAsync(Guid segmentId, CustomerSegmentDto request);
    Task<bool> DeleteSegmentAsync(Guid segmentId);
    Task<CustomerSegmentDto?> GetSegmentAsync(Guid segmentId);
    Task<List<CustomerSegmentDto>> GetSegmentsAsync(bool includeInactive = false, int page = 1, int pageSize = 50);

    // Segment membership management
    Task<bool> RefreshSegmentMembershipAsync(Guid segmentId);
    Task<bool> AddCustomerToSegmentAsync(Guid segmentId, Guid customerId);
    Task<bool> RemoveCustomerFromSegmentAsync(Guid segmentId, Guid customerId);

    // Query and analytics
    Task<List<CustomerDto>> GetSegmentCustomersAsync(Guid segmentId, int page = 1, int pageSize = 50);
    Task<List<CustomerDto>> GetCustomersByCriteriaAsync(string criteriaJson, int page = 1, int pageSize = 50);
    Task<Dictionary<string, object>?> GetSegmentAnalyticsAsync(Guid segmentId);

    // Batch operations
    Task<bool> RefreshAllSegmentMembershipsAsync();
    Task<int> GetSegmentCustomerCountAsync(Guid segmentId);

    // Utility
    Task<List<CustomerSegmentDto>> GetCustomerSegmentsAsync(Guid customerId);
    Task<bool> IsCustomerInSegmentAsync(Guid customerId, Guid segmentId);
}
