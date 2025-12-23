using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

// General Orders VM (non vendor-specific), backed by OrderApiService
public class OrdersViewModel
{
    private readonly OrderApiService? _orders;

    public ObservableCollection<OrderApiService.OrderDto> Orders { get; } = new();
    public ObservableCollection<OrderApiService.OrderLogDto> Logs { get; } = new();

    public bool HasError { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Guid? _sessionId;
    public void SetSession(Guid sessionId) => _sessionId = sessionId;

    public OrdersViewModel()
    {
        // CRITICAL FIX: Handle null OrdersApi gracefully instead of throwing exception
        _orders = App.OrdersApi;
        
        if (_orders == null)
        {
            HasError = true;
            ErrorMessage = "OrdersApi is not available. Please restart the application or contact support.";
        }
    }

    public async Task LoadAsync(bool includeHistory = false, CancellationToken ct = default)
    {
        if (!_sessionId.HasValue) { Orders.Clear(); return; }
        await LoadBySessionAsync(_sessionId.Value, includeHistory, ct);
    }

    public async Task LoadBySessionAsync(Guid sessionId, bool includeHistory = false, CancellationToken ct = default)
    {
        if (_orders == null) return;
        
        Orders.Clear();
        var list = await _orders.GetOrdersBySessionAsync(sessionId, includeHistory, ct);
        foreach (var o in list) Orders.Add(o);
    }

    public async Task AddItemsAsync(long orderId, IReadOnlyList<OrderApiService.CreateOrderItemDto> items, CancellationToken ct = default)
    {
        if (_orders == null) return;
        
        var updated = await _orders.AddItemsAsync(orderId, items, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task UpdateItemAsync(long orderId, OrderApiService.UpdateOrderItemDto item, CancellationToken ct = default)
    {
        if (_orders == null) return;
        
        var updated = await _orders.UpdateItemAsync(orderId, item, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct = default)
    {
        if (_orders == null) return;
        
        var updated = await _orders.DeleteItemAsync(orderId, orderItemId, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task CloseOrderAsync(long orderId, CancellationToken ct = default)
    {
        if (_orders == null) return;
        
        var updated = await _orders.CloseOrderAsync(orderId, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task LoadLogsAsync(long orderId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        if (_orders == null) return;
        
        Logs.Clear();
        var pr = await _orders.ListLogsAsync(orderId, page, pageSize, ct);
        foreach (var l in pr.Items) Logs.Add(l);
    }

    private async Task ReplaceOrderAsync(OrderApiService.OrderDto? updated)
    {
        if (updated is null) return;
        var idx = Orders.ToList().FindIndex(o => o.Id == updated.Id);
        if (idx >= 0) Orders[idx] = updated;
        else Orders.Insert(0, updated);
        await Task.CompletedTask;
    }
}
