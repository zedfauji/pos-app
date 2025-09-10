using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class OrderItemLineVm : INotifyPropertyChanged
    {
        public long OrderItemId { get; init; }
        private string _name = string.Empty; public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string ModifiersText { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        private int _quantity;
        public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } }
        public decimal LineTotal => UnitPrice * Quantity;
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }

    public sealed class OrderDetailViewModel : INotifyPropertyChanged
    {
        private readonly OrderApiService _orders;
        private readonly MenuApiService _menu;
        private readonly Dictionary<long, string> _nameCache = new();
        public long OrderId { get; private set; }

        public ObservableCollection<OrderItemLineVm> Items { get; } = new();
        public ObservableCollection<OrderApiService.OrderLogDto> Logs { get; } = new();

        private decimal _subtotal; public decimal Subtotal { get => _subtotal; private set { _subtotal = value; OnPropertyChanged(); } }
        private decimal _discountTotal; public decimal DiscountTotal { get => _discountTotal; private set { _discountTotal = value; OnPropertyChanged(); } }
        private decimal _taxTotal; public decimal TaxTotal { get => _taxTotal; private set { _taxTotal = value; OnPropertyChanged(); } }
        private decimal _total; public decimal Total { get => _total; private set { _total = value; OnPropertyChanged(); } }
        private decimal _profit; public decimal Profit { get => _profit; private set { _profit = value; OnPropertyChanged(); } }
        public decimal ProfitMargin => Total > 0 ? Profit / Total : 0m;

        public ICommand IncrementQtyCommand { get; }
        public ICommand DecrementQtyCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LoadLogsCommand { get; }

        public OrderDetailViewModel()
        {
            // CRITICAL FIX: Ensure OrdersApi and MenuApi are initialized before creating ViewModel
            // This prevents InvalidOperationException if services are null
            if (App.OrdersApi == null)
            {
                throw new InvalidOperationException("OrdersApi not initialized. Ensure App.InitializeApiAsync() has completed successfully.");
            }
            if (App.Menu == null)
            {
                throw new InvalidOperationException("MenuApi not initialized. Ensure App.InitializeApiAsync() has completed successfully.");
            }
            
            _orders = App.OrdersApi;
            _menu = App.Menu;
            
            IncrementQtyCommand = new RelayCommand(async o => { if (o is OrderItemLineVm l) await UpdateQuantityAsync(l, l.Quantity + 1); });
            DecrementQtyCommand = new RelayCommand(async o => { if (o is OrderItemLineVm l && l.Quantity > 1) await UpdateQuantityAsync(l, l.Quantity - 1); });
            DeleteItemCommand = new RelayCommand(async o => { if (o is OrderItemLineVm l) await DeleteItemAsync(l); });
            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            LoadLogsCommand = new RelayCommand(async _ => await LoadLogsAsync());
        }

        public async Task InitializeAsync(long orderId, CancellationToken ct = default)
        {
            OrderId = orderId;
            await LoadAsync(ct);
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (OrderId <= 0) return;
            var dto = await _orders.GetOrderAsync(OrderId, ct);
            if (dto is null) return;
            Items.Clear();
            foreach (var i in dto.Items)
            {
                Items.Add(new OrderItemLineVm
                {
                    OrderItemId = i.Id,
                    Name = i.MenuItemId.HasValue ? $"Item #{i.MenuItemId}" : (i.ComboId.HasValue ? $"Combo #{i.ComboId}" : "Item"),
                    ModifiersText = string.Empty, // TODO: display modifiers if available in API
                    UnitPrice = i.BasePrice + i.PriceDelta,
                    Quantity = i.Quantity
                });
            }
            // Enrich names from MenuApi
            _ = EnrichItemNamesAsync(dto.Items);
            Subtotal = dto.Subtotal;
            DiscountTotal = dto.DiscountTotal;
            TaxTotal = dto.TaxTotal;
            Total = dto.Total;
            Profit = dto.ProfitTotal;
        }

        private async Task EnrichItemNamesAsync(IReadOnlyList<OrderApiService.OrderItemDto> items, CancellationToken ct = default)
        {
            foreach (var orderItem in items)
            {
                if (orderItem.MenuItemId is long menuItemId)
                {
                    if (!_nameCache.TryGetValue(menuItemId, out var name))
                    {
                        try
                        {
                            var mi = await _menu.GetItemByIdAsync(menuItemId, ct);
                            if (mi != null)
                            {
                                name = mi.Name;
                                _nameCache[menuItemId] = name;
                            }
                        }
                        catch { }
                    }
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        var vm = Items.FirstOrDefault(x => x.OrderItemId == orderItem.Id);
                        if (vm != null) vm.Name = name;
                    }
                }
            }
        }

        private async Task UpdateQuantityAsync(OrderItemLineVm line, int newQty, CancellationToken ct = default)
        {
            var dto = new OrderApiService.UpdateOrderItemDto(line.OrderItemId, newQty, null);
            var updated = await _orders.UpdateItemAsync(OrderId, dto, ct);
            if (updated is null) return;
            await LoadAsync(ct);
        }

        private async Task DeleteItemAsync(OrderItemLineVm line, CancellationToken ct = default)
        {
            var updated = await _orders.DeleteItemAsync(OrderId, line.OrderItemId, ct);
            if (updated is null) return;
            await LoadAsync(ct);
            await LoadLogsAsync(ct);
        }

        public async Task LoadLogsAsync(CancellationToken ct = default)
        {
            Logs.Clear();
            var pr = await _orders.ListLogsAsync(OrderId, 1, 100, ct);
            foreach (var l in pr.Items) Logs.Add(l);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }
}
