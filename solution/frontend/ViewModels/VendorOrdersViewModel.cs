using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public class VendorOrdersViewModel
{
    private readonly VendorOrdersApiService _vendorOrders;

    public ObservableCollection<VendorOrdersApiService.VendorOrderDto> Orders { get; } = new();

    public VendorOrdersViewModel()
    {
        _vendorOrders = App.VendorOrders ?? throw new InvalidOperationException("VendorOrders service not initialized");
    }

    public async Task LoadAsync(int limit = 100, CancellationToken ct = default)
    {
        Orders.Clear();
        var list = await _vendorOrders.ListOrdersAsync(limit, ct);
        foreach (var o in list) Orders.Add(o);
    }
}
