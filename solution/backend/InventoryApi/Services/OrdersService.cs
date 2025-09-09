using MagiDesk.Shared.DTOs;

namespace InventoryApi.Services;

public interface IOrdersService
{
    Task<string> FinalizeOrderAsync(OrderDto order, CancellationToken ct = default);
    Task<CartDraftDto> SaveDraftAsync(CartDraftDto draft, CancellationToken ct = default);
    Task<CartDraftDto?> GetDraftAsync(string draftId, CancellationToken ct = default);
    Task<List<CartDraftDto>> GetDraftsAsync(int limit = 20, CancellationToken ct = default);
    Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct = default);
    Task<List<OrdersJobDto>> GetJobsAsync(int limit = 10, CancellationToken ct = default);
    Task<List<OrderNotificationDto>> GetNotificationsAsync(int limit = 50, CancellationToken ct = default);
}

public class OrdersService : IOrdersService
{
    // Legacy removed: InventoryApi no longer handles orders persistence.

    public async Task<string> FinalizeOrderAsync(OrderDto order, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(order.OrderId)) order.OrderId = Guid.NewGuid().ToString("N");
        order.Status = "submitted";
        order.CreatedAt = DateTime.UtcNow;
        return Guid.NewGuid().ToString("N");
    }

    public async Task<CartDraftDto> SaveDraftAsync(CartDraftDto draft, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draft.CartId)) draft.CartId = Guid.NewGuid().ToString("N");
        draft.Status = "draft";
        draft.CreatedAt = draft.CreatedAt == default ? DateTime.UtcNow : draft.CreatedAt;
        return draft;
    }

    public async Task<CartDraftDto?> GetDraftAsync(string draftId, CancellationToken ct = default)
    {
        return null;
    }

    public async Task<List<CartDraftDto>> GetDraftsAsync(int limit = 20, CancellationToken ct = default)
    {
        return new List<CartDraftDto>();
    }

    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct = default)
    {
        return new List<OrderDto>();
    }

    public async Task<List<OrdersJobDto>> GetJobsAsync(int limit = 10, CancellationToken ct = default)
    {
        return new List<OrdersJobDto>();
    }

    public async Task<List<OrderNotificationDto>> GetNotificationsAsync(int limit = 50, CancellationToken ct = default)
    {
        return new List<OrderNotificationDto>();
    }
}
