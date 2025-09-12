using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class MenuItemVm
{
    public long MenuItemId { get; init; }
    public string Sku { get; init; } = "";
    public string Name { get; init; } = "";
    public string? PictureUrl { get; init; }
    public decimal Price { get; init; }
    public bool IsDiscountable { get; init; }
    public bool IsPartOfCombo { get; init; }
    public bool IsAvailable { get; set; }
    public string Category { get; init; } = "";
    public string? GroupName { get; init; }
    public string? Description { get; init; }
}

public sealed class MenuViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<string> Groups { get; } = new();
    public ObservableCollection<MenuItemVm> Items { get; } = new();

    private string? _searchText;
    public string? SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); _ = RefreshAsync(); } }

    private string? _selectedCategory;
    public string? SelectedCategory { get => _selectedCategory; set { _selectedCategory = value; OnPropertyChanged(); _ = RefreshAsync(); } }

    private string? _selectedGroup;
    public string? SelectedGroup { get => _selectedGroup; set { _selectedGroup = value; OnPropertyChanged(); _ = RefreshAsync(); } }

    public ICommand RefreshCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand SelectGroupCommand { get; }
    public ICommand AddToOrderCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public MenuViewModel()
    {
        RefreshCommand = new Services.RelayCommand(async _ => await RefreshAsync());
        SelectCategoryCommand = new Services.RelayCommand(async c => { SelectedCategory = c as string; await RefreshAsync(); });
        SelectGroupCommand = new Services.RelayCommand(async g => { SelectedGroup = g as string; await RefreshAsync(); });
        AddToOrderCommand = new Services.RelayCommand(async o => await AddToOrderAsync(o as MenuItemVm));
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Optional: preload categories/groups if your API supports
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var svc = App.Menu; // MenuApiService
            if (svc is null)
            {
                Items.Clear();
                return;
            }

            var q = BuildQuery();
            var list = await svc.ListItemsAsync(q);

            Items.Clear();
            foreach (var it in list)
            {
                Items.Add(new MenuItemVm
                {
                    MenuItemId = it.Id,
                    Sku = it.Sku,
                    Name = it.Name,
                    PictureUrl = it.PictureUrl,
                    Price = it.SellingPrice,
                    IsDiscountable = it.IsDiscountable,
                    IsPartOfCombo = it.IsPartOfCombo,
                    IsAvailable = it.IsAvailable
                });
            }
        }
        catch
        {
            // TODO: Surface error to UI via a status bar/InfoBar when integrating with a page
        }
    }

    private Services.MenuApiService.ItemsQuery BuildQuery()
        => new Services.MenuApiService.ItemsQuery(SearchText, SelectedCategory, SelectedGroup, true);

    private async Task AddToOrderAsync(MenuItemVm? item)
    {
        if (item is null) return;
        
        try
        {
            // Check if we have an active order/session
            if (App.OrdersApi == null)
            {
                // Show error dialog
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Orders service is not available. Please restart the application.",
                    CloseButtonText = "OK",
                    XamlRoot = App.MainWindow?.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            // Get current session/table context - need to implement proper session management
            // This requires implementing GetCurrentSession() method in ApiService
            var currentSessionId = MagiDesk.Frontend.Services.OrderContext.CurrentSessionId;
            if (string.IsNullOrEmpty(currentSessionId))
            {
                var errorDialog = new ContentDialog
                {
                    Title = "No Active Session",
                    Content = "Please select a table first before adding items to order.",
                    CloseButtonText = "OK",
                    XamlRoot = App.MainWindow?.Content.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            // Add item to current order using the correct OrderApiService method
            // First, we need to get or create an order for the current session
            var orders = await App.OrdersApi.GetOrdersBySessionAsync(Guid.Parse(currentSessionId));
            var currentOrder = orders.FirstOrDefault();
            
            if (currentOrder == null)
            {
                // Create a new order for this session
                var createOrderRequest = new OrderApiService.CreateOrderRequestDto(
                    SessionId: Guid.Parse(currentSessionId),
                    BillingId: null,
                    TableId: "Unknown", // TODO: Get actual table ID
                    ServerId: "Unknown", // TODO: Get actual server ID
                    ServerName: null,
                    Items: new List<OrderApiService.CreateOrderItemDto>
                    {
                        new OrderApiService.CreateOrderItemDto(
                            MenuItemId: item.MenuItemId,
                            ComboId: null,
                            Quantity: 1,
                            Modifiers: new List<OrderApiService.ModifierSelectionDto>()
                        )
                    }
                );
                currentOrder = await App.OrdersApi.CreateOrderAsync(createOrderRequest);
            }
            else
            {
                // Add item to existing order
                var newItem = new OrderApiService.CreateOrderItemDto(
                    MenuItemId: item.MenuItemId,
                    ComboId: null,
                    Quantity: 1,
                    Modifiers: new List<OrderApiService.ModifierSelectionDto>()
                );
                currentOrder = await App.OrdersApi.AddItemsAsync(currentOrder.Id, new List<OrderApiService.CreateOrderItemDto> { newItem });
            }
            
            // Show success feedback
            var successDialog = new ContentDialog
            {
                Title = "Item Added",
                Content = $"{item.Name} has been added to the order.",
                CloseButtonText = "OK",
                XamlRoot = App.MainWindow?.Content.XamlRoot
            };
            await successDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to add item to order: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = App.MainWindow?.Content.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}
