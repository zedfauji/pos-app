using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class OrdersViewModel
{
    private readonly ApiService _api;

    public ObservableCollection<OrderDto> Orders { get; } = new();
    public ObservableCollection<OrdersJobDto> Jobs { get; } = new();
    public ObservableCollection<OrderNotificationDto> Notifications { get; } = new();

    public OrdersViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Orders.Clear();
        var list = await _api.GetOrdersAsync(ct);
        foreach (var o in list) Orders.Add(o);
    }

    public async Task RefreshJobsAsync(CancellationToken ct = default)
    {
        Jobs.Clear();
        var list = await _api.GetOrdersJobsAsync(10, ct);
        foreach (var j in list) Jobs.Add(j);
    }

    public async Task RefreshNotificationsAsync(CancellationToken ct = default)
    {
        Notifications.Clear();
        var list = await _api.GetOrderNotificationsAsync(50, ct);
        foreach (var n in list) Notifications.Add(n);
    }

    public async Task<string?> FinalizeOrderAsync(OrderDto order, CancellationToken ct = default)
    {
        var (success, jobId, orderId) = await _api.FinalizeOrderAsync(order, ct);
        if (success)
        {
            await LoadAsync(ct);
            await RefreshJobsAsync(ct);
            await RefreshNotificationsAsync(ct);
            return orderId;
        }
        return null;
    }

    public async Task<CartDraftDto?> SaveDraftAsync(CartDraftDto draft, CancellationToken ct = default)
    {
        var saved = await _api.SaveCartDraftAsync(draft, ct);
        await RefreshJobsAsync(ct);
        return saved;
    }

    public async Task<CartDraftDto?> GetDraftAsync(string id, CancellationToken ct = default)
        => await _api.GetCartDraftAsync(id, ct);
}
