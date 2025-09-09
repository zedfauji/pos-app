using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

// General Orders VM (non vendor-specific), backed by OrderApiService
public class OrdersViewModel
{
    private readonly OrderApiService _orders;

    public ObservableCollection<OrderApiService.OrderDto> Orders { get; } = new();
    public ObservableCollection<OrderApiService.OrderLogDto> Logs { get; } = new();

    private string? _sessionId;
    public void SetSession(string sessionId) => _sessionId = sessionId;

    public OrdersViewModel()
    {
        _orders = App.OrdersApi ?? throw new InvalidOperationException("OrdersApi not initialized");
    }

    public async Task LoadAsync(bool includeHistory = false, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_sessionId)) { Orders.Clear(); return; }
        await LoadBySessionAsync(_sessionId!, includeHistory, ct);
    }

    public async Task LoadBySessionAsync(string sessionId, bool includeHistory = false, CancellationToken ct = default)
    {
        Orders.Clear();
        var list = await _orders.GetOrdersBySessionAsync(sessionId, includeHistory, ct);
        foreach (var o in list) Orders.Add(o);
    }

    public async Task AddItemsAsync(long orderId, IReadOnlyList<OrderApiService.CreateOrderItemDto> items, CancellationToken ct = default)
    {
        var updated = await _orders.AddItemsAsync(orderId, items, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task UpdateItemAsync(long orderId, OrderApiService.UpdateOrderItemDto item, CancellationToken ct = default)
    {
        var updated = await _orders.UpdateItemAsync(orderId, item, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task DeleteItemAsync(long orderId, long orderItemId, CancellationToken ct = default)
    {
        var updated = await _orders.DeleteItemAsync(orderId, orderItemId, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task CloseOrderAsync(long orderId, CancellationToken ct = default)
    {
        var updated = await _orders.CloseOrderAsync(orderId, ct);
        await ReplaceOrderAsync(updated);
    }

    public async Task LoadLogsAsync(long orderId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
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
