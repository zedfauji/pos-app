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
        private string _name = string.Empty; 
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public string ModifiersText { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        private int _quantity;
        public int Quantity { get => _quantity; set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); OnPropertyChanged(nameof(LineTotalFormatted)); } }
        public decimal LineTotal => UnitPrice * Quantity;
        public string LineTotalFormatted => $"${LineTotal:F2}";
        public string UnitPriceFormatted => $"${UnitPrice:F2}";
        
        // Delivery Properties
        private int _deliveredQuantity = 0;
        public int DeliveredQuantity { get => _deliveredQuantity; set { _deliveredQuantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanMarkDelivered)); OnPropertyChanged(nameof(DeliveryStatusColor)); } }
        
        private string _deliveryStatus = "Pending";
        public string DeliveryStatus { get => _deliveryStatus; set { _deliveryStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(DeliveryStatusColor)); } }
        
        public bool CanMarkDelivered => DeliveredQuantity < Quantity;
        
        public string DeliveryStatusColor
        {
            get
            {
                return DeliveryStatus.ToLower() switch
                {
                    "delivered" => "#107C10",
                    "pending" => "#FF8C00",
                    "preparing" => "#0078D4",
                    "waiting" => "#FF8C00",
                    _ => "#404040"
                };
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }

    public sealed class OrderDetailViewModel : INotifyPropertyChanged
    {
        private readonly OrderApiService? _orders;
        private readonly MenuApiService? _menu;
        private readonly Dictionary<long, string> _nameCache = new();
        public long OrderId { get; private set; }

        public ObservableCollection<OrderItemLineVm> Items { get; } = new();
        public ObservableCollection<OrderLogWithFormattedTime> Logs { get; } = new();

        private decimal _subtotal; public decimal Subtotal { get => _subtotal; private set { _subtotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubtotalFormatted)); } }
        private decimal _discountTotal; public decimal DiscountTotal { get => _discountTotal; private set { _discountTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(DiscountTotalFormatted)); } }
        private decimal _taxTotal; public decimal TaxTotal { get => _taxTotal; private set { _taxTotal = value; OnPropertyChanged(); OnPropertyChanged(nameof(TaxTotalFormatted)); } }
        private decimal _total; public decimal Total { get => _total; private set { _total = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalFormatted)); } }
        private decimal _profit; public decimal Profit { get => _profit; private set { _profit = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProfitMarginFormatted)); } }
        public decimal ProfitMargin => Total > 0 ? Profit / Total : 0m;
        
        // Formatted Properties
        public string SubtotalFormatted => $"${Subtotal:F2}";
        public string DiscountTotalFormatted => $"${DiscountTotal:F2}";
        public string TaxTotalFormatted => $"${TaxTotal:F2}";
        public string TotalFormatted => $"${Total:F2}";
        public string ProfitMarginFormatted => $"{ProfitMargin:P1}";
        
        // Additional Properties
        public int ItemsCount => Items.Count;
        public bool CanProcessPayment => Status.ToLower() == "open" || Status.ToLower() == "pending";

        // Order Status Properties
        private string _status = "Unknown"; 
        public string Status { get => _status; private set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); } }
        
        private string _deliveryStatus = "Unknown"; 
        public string DeliveryStatus { get => _deliveryStatus; private set { _deliveryStatus = value; OnPropertyChanged(); OnPropertyChanged(nameof(DeliveryStatusColor)); } }
        
        public string StatusColor
        {
            get
            {
                return Status.ToLower() switch
                {
                    "open" => "#0078D4",
                    "pending" => "#FF8C00",
                    "waiting" => "#FF8C00",
                    "preparing" => "#0078D4",
                    "delivered" => "#107C10",
                    "closed" => "#404040",
                    _ => "#404040"
                };
            }
        }
        
        public string DeliveryStatusColor
        {
            get
            {
                return DeliveryStatus.ToLower() switch
                {
                    "delivered" => "#107C10",
                    "pending" => "#FF8C00",
                    "preparing" => "#0078D4",
                    "waiting" => "#FF8C00",
                    _ => "#404040"
                };
            }
        }
        
        // Command Availability Properties
        private bool _canMarkWaiting = false; 
        public bool CanMarkWaiting { get => _canMarkWaiting; private set { _canMarkWaiting = value; OnPropertyChanged(); } }
        
        private bool _canMarkDelivered = false; 
        public bool CanMarkDelivered { get => _canMarkDelivered; private set { _canMarkDelivered = value; OnPropertyChanged(); } }
        
        private bool _canCloseOrder = false; 
        public bool CanCloseOrder { get => _canCloseOrder; private set { _canCloseOrder = value; OnPropertyChanged(); } }

        private bool _hasError = false;
        public bool HasError { get => _hasError; private set { _hasError = value; OnPropertyChanged(); } }
        
        private string? _errorMessage;
        public string? ErrorMessage { get => _errorMessage; private set { _errorMessage = value; OnPropertyChanged(); } }
        
        private bool _isLoading = false;
        public bool IsLoading { get => _isLoading; private set { _isLoading = value; OnPropertyChanged(); } }

        public ICommand IncrementQtyCommand { get; }
        public ICommand DecrementQtyCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LoadLogsCommand { get; }

        public OrderDetailViewModel()
        {
            // CRITICAL FIX: Handle null services gracefully instead of throwing exception
            _orders = App.OrdersApi;
            _menu = App.Menu;
            
            if (_orders == null)
            {
                HasError = true;
                ErrorMessage = "OrdersApi is not available. Please restart the application or contact support.";
            }
            
            if (_menu == null)
            {
                HasError = true;
                ErrorMessage = "MenuApi is not available. Please restart the application or contact support.";
            }
            
            IncrementQtyCommand = new Services.RelayCommand(async o => { if (o is OrderItemLineVm l) await UpdateQuantityAsync(l, l.Quantity + 1); });
            DecrementQtyCommand = new Services.RelayCommand(async o => { if (o is OrderItemLineVm l && l.Quantity > 1) await UpdateQuantityAsync(l, l.Quantity - 1); });
            DeleteItemCommand = new Services.RelayCommand(async o => { if (o is OrderItemLineVm l) await DeleteItemAsync(l); });
            RefreshCommand = new Services.RelayCommand(async _ => await LoadAsync());
            LoadLogsCommand = new Services.RelayCommand(async _ => await LoadLogsAsync());
        }

        public async Task InitializeAsync(long orderId, CancellationToken ct = default)
        {
            OrderId = orderId;
            await LoadAsync(ct);
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (OrderId <= 0) return;
            if (_orders == null) 
            {
                HasError = true;
                ErrorMessage = "Orders API is not available";
                return;
            }
            
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;
                
                var dto = await _orders.GetOrderAsync(OrderId, ct);
                if (dto is null) 
                {
                    HasError = true;
                    ErrorMessage = "Order not found";
                    return;
                }
                
                Items.Clear();
                foreach (var i in dto.Items)
                {
                    Items.Add(new OrderItemLineVm
                    {
                        OrderItemId = i.Id,
                        Name = i.MenuItemId.HasValue ? $"Item #{i.MenuItemId}" : (i.ComboId.HasValue ? $"Combo #{i.ComboId}" : "Item"),
                        ModifiersText = string.Empty, // TODO: display modifiers if available in API
                        UnitPrice = i.BasePrice + i.PriceDelta,
                        Quantity = i.Quantity,
                        DeliveredQuantity = 0, // TODO: Add DeliveredQuantity to OrderItemDto if needed
                        DeliveryStatus = "Pending" // TODO: Add DeliveryStatus to OrderItemDto if needed
                    });
                }
                
                // Enrich names from MenuApi
                _ = EnrichItemNamesAsync(dto.Items);
                
                Subtotal = dto.Subtotal;
                DiscountTotal = dto.DiscountTotal;
                TaxTotal = dto.TaxTotal;
                Total = dto.Total;
                Profit = dto.ProfitTotal;
                
                // Update order status properties
                Status = dto.Status ?? "Unknown";
                DeliveryStatus = dto.DeliveryStatus ?? "Unknown";
                
                // Update command availability based on order status
                CanMarkWaiting = Status == "open" || Status == "pending";
                CanMarkDelivered = Status == "waiting" || Status == "preparing";
                CanCloseOrder = Status == "open" || Status == "pending" || Status == "waiting" || Status == "preparing";
                
                // Notify property changes
                OnPropertyChanged(nameof(ItemsCount));
                OnPropertyChanged(nameof(CanProcessPayment));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to load order: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task EnrichItemNamesAsync(IReadOnlyList<OrderApiService.OrderItemDto> items, CancellationToken ct = default)
        {
            if (_menu == null) return;
            
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
                        catch (Exception ex)
                        {
                            // Log the error for debugging but don't crash the enrichment process
                        }
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
            if (_orders == null) return;
            
            var dto = new OrderApiService.UpdateOrderItemDto(line.OrderItemId, newQty, null);
            var updated = await _orders.UpdateItemAsync(OrderId, dto, ct);
            if (updated is null) return;
            await LoadAsync(ct);
        }

        private async Task DeleteItemAsync(OrderItemLineVm line, CancellationToken ct = default)
        {
            if (_orders == null) return;
            
            var updated = await _orders.DeleteItemAsync(OrderId, line.OrderItemId, ct);
            if (updated is null) return;
            await LoadAsync(ct);
            await LoadLogsAsync(ct);
        }

        public async Task LoadLogsAsync(CancellationToken ct = default)
        {
            if (_orders == null) return;
            
            try
            {
                Logs.Clear();
                var pr = await _orders.ListLogsAsync(OrderId, 1, 100, ct);
                foreach (var l in pr.Items) 
                {
                    // Add formatted timestamp property
                    var logWithFormattedTime = new OrderLogWithFormattedTime(l);
                    Logs.Add(logWithFormattedTime);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }

    public class OrderLogWithFormattedTime
    {
        public string Action { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public long Id { get; set; }
        public long OrderId { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public string? ServerId { get; set; }
        public string CreatedAtFormatted { get; }

        public OrderLogWithFormattedTime(OrderApiService.OrderLogDto original)
        {
            // Copy properties from original
            Action = original.Action;
            CreatedAt = original.CreatedAt;
            Id = original.Id;
            OrderId = original.OrderId;
            OldValue = original.OldValue;
            NewValue = original.NewValue;
            ServerId = original.ServerId;
            
            // Add formatted timestamp
            CreatedAtFormatted = CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
