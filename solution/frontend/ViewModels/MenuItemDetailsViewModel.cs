using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels
{
    public sealed class ModifierOptionVm : INotifyPropertyChanged
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal PriceDelta { get; init; }
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }

    public sealed class ModifierVm : INotifyPropertyChanged
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsRequired { get; init; }
        public bool AllowMultiple { get; init; }
        public int? MaxSelections { get; init; }
        public ObservableCollection<ModifierOptionVm> Options { get; } = new();
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }

    public sealed class MenuItemDetailsViewModel : INotifyPropertyChanged
    {
        private readonly MenuApiService _menu;
        private readonly OrderApiService _orders;
        private readonly MenuItemVm _item;

        public string Sku => _item.Sku;
        public string Name { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public string? PictureUrl { get; private set; }
        public decimal Price { get; private set; }
        public ObservableCollection<ModifierVm> Modifiers { get; } = new();

        private int _quantity = 1;
        public int Quantity { get => _quantity; set { _quantity = value < 1 ? 1 : value; OnPropertyChanged(); } }

        public bool IsBusy { get; private set; }
        public string? Error { get; private set; }
        public bool HasError => !string.IsNullOrEmpty(Error);

        public MenuItemDetailsViewModel(MenuItemVm item)
        {
            _menu = App.Menu ?? throw new InvalidOperationException("MenuApi not initialized");
            _orders = App.OrdersApi ?? throw new InvalidOperationException("OrdersApi not initialized");
            _item = item;
            Name = item.Name;
            PictureUrl = item.PictureUrl;
            Price = item.Price;
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            try
            {
                IsBusy = true; OnPropertyChanged(nameof(IsBusy));
                var details = string.IsNullOrWhiteSpace(Sku) ? null : await _menu.GetItemBySkuAsync(Sku, ct);
                if (details is null) { Error = "Item not found"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return; }
                Name = details.Item.Name; OnPropertyChanged(nameof(Name));
                Description = details.Item.Description; OnPropertyChanged(nameof(Description));
                PictureUrl = details.Item.PictureUrl; OnPropertyChanged(nameof(PictureUrl));
                Price = details.Item.SellingPrice; OnPropertyChanged(nameof(Price));
                Modifiers.Clear();
                foreach (var m in details.Modifiers)
                {
                    var vm = new ModifierVm
                    {
                        Id = m.Id,
                        Name = m.Name,
                        IsRequired = m.IsRequired,
                        AllowMultiple = m.AllowMultiple,
                        MaxSelections = m.MaxSelections
                    };
                    foreach (var o in m.Options)
                        vm.Options.Add(new ModifierOptionVm { Id = o.Id, Name = o.Name, PriceDelta = o.PriceDelta });
                    Modifiers.Add(vm);
                }
            }
            finally
            {
                IsBusy = false; OnPropertyChanged(nameof(IsBusy));
            }
        }

        public async Task<bool> AddToOrderAsync(long orderId, CancellationToken ct = default)
        {
            var selections = new List<OrderApiService.ModifierSelectionDto>();
            foreach (var m in Modifiers)
            {
                var selected = m.Options.Where(o => o.IsSelected).ToList();
                if (m.IsRequired && selected.Count == 0) { Error = $"Select at least one for {m.Name}"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
                if (!m.AllowMultiple && selected.Count > 1) { Error = $"Only one allowed for {m.Name}"; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); return false; }
                foreach (var o in selected) selections.Add(new OrderApiService.ModifierSelectionDto(m.Id, o.Id));
            }
            var items = new[] { new OrderApiService.CreateOrderItemDto(MenuItemId: _item.MenuItemId, ComboId: null, Quantity: Quantity, Modifiers: selections) };
            var updated = await _orders.AddItemsAsync(orderId, items, ct);
            return updated is not null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? m = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(m));
    }
}
