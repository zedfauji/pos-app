using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using System.Windows.Input;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class MenuSelectionDialog : ContentDialog
{
    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();
    public ObservableCollection<SelectedItemViewModel> SelectedItems { get; } = new();
    public ObservableCollection<ServerViewModel> AvailableServers { get; } = new();
    
    public ServerViewModel? SelectedServer { get; set; }
    public string TableLabel { get; set; } = "";
    public Guid SessionId { get; set; }
    public long? OrderId { get; set; }

    private readonly MenuApiService? _menuService;
    private readonly OrderApiService? _orderService;

    public MenuSelectionDialog()
    {
        try
        {
            this.InitializeComponent();
            _menuService = App.Menu;
            _orderService = App.OrdersApi;
            
            if (_menuService == null)
            {
                ShowErrorDialog("Service Not Available", "Menu service is not available. Please restart the application or contact support.");
                return;
            }
            
            if (_orderService == null)
            {
                ShowErrorDialog("Service Not Available", "Order service is not available. Please restart the application or contact support.");
                return;
            }
            
            LoadMenuItems();
            LoadServers();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            // Show error dialog instead of letting the exception crash the app
            ShowErrorDialog("Initialization Error", $"Failed to initialize Menu Selection dialog: {ex.Message}");
        }
    }

    private async void LoadMenuItems()
    {
        try
        {
            if (_menuService == null)
            {
                ShowErrorDialog("Service Not Available", "Menu service is not available.");
                return;
            }
            
            var query = new MenuApiService.ItemsQuery(Q: null, Category: null, GroupName: null, AvailableOnly: true);
            var items = await _menuService.ListItemsAsync(query);
            MenuItems.Clear();
            
            foreach (var item in items)
            {
                // Load item details with modifiers
                var itemDetails = await _menuService.GetItemBySkuAsync(item.Sku);
                var modifiers = itemDetails?.Modifiers ?? Array.Empty<MenuApiService.ModifierDto>();
                
                MenuItems.Add(new MenuItemViewModel(item, modifiers)
                {
                    QuantityMinusCommand = new RelayCommand(() => DecreaseQuantity(item.Id)),
                    QuantityPlusCommand = new RelayCommand(() => IncreaseQuantity(item.Id)),
                    AddItemCommand = new RelayCommand(() => AddItemToSelection(item.Id))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load menu items: {ex.Message}");
        }
    }

    private async void LoadServers()
    {
        try
        {
            // Load servers from UserApiService
            if (App.UsersApi != null)
            {
                var users = await App.UsersApi.GetUsersAsync();
                AvailableServers.Clear();
                
                foreach (var user in users)
                {
                    AvailableServers.Add(new ServerViewModel 
                    { 
                        Id = user.UserId ?? Guid.NewGuid().ToString(), 
                        Name = user.Username ?? $"User {user.UserId}" 
                    });
                }
                
                if (AvailableServers.Count > 0)
                    SelectedServer = AvailableServers[0];
            }
            else
            {
                // Fallback to mock data if service not available
                AvailableServers.Add(new ServerViewModel { Id = "server1", Name = "Server 1" });
                AvailableServers.Add(new ServerViewModel { Id = "server2", Name = "Server 2" });
                AvailableServers.Add(new ServerViewModel { Id = "server3", Name = "Server 3" });
                
                if (AvailableServers.Count > 0)
                    SelectedServer = AvailableServers[0];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load servers: {ex.Message}");
            // Fallback to mock data
            AvailableServers.Add(new ServerViewModel { Id = "server1", Name = "Server 1" });
            AvailableServers.Add(new ServerViewModel { Id = "server2", Name = "Server 2" });
            AvailableServers.Add(new ServerViewModel { Id = "server3", Name = "Server 3" });
            
            if (AvailableServers.Count > 0)
                SelectedServer = AvailableServers[0];
        }
    }

    private void DecreaseQuantity(long itemId)
    {
        var item = MenuItems.FirstOrDefault(i => i.Id == itemId);
        if (item != null && item.SelectedQuantity > 0)
        {
            item.SelectedQuantity--;
        }
    }

    private void IncreaseQuantity(long itemId)
    {
        var item = MenuItems.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.SelectedQuantity++;
        }
    }

    private async void AddItemToSelection(long itemId)
    {
        var menuItem = MenuItems.FirstOrDefault(i => i.Id == itemId);
        if (menuItem != null && menuItem.SelectedQuantity > 0)
        {
            // Validate required modifiers
            var invalidModifiers = menuItem.Modifiers.Where(m => !m.IsValid).ToList();
            if (invalidModifiers.Any())
            {
                var requiredModifiers = string.Join(", ", invalidModifiers.Select(m => m.DisplayName));
                await ShowErrorDialog("Required Modifiers", $"Please select required modifiers: {requiredModifiers}");
                return;
            }

            // Collect customizations
            var customizations = new List<CustomizationViewModel>();
            var customizationTexts = new List<string>();
            
            foreach (var modifier in menuItem.Modifiers)
            {
                foreach (var selectedOption in modifier.SelectedOptions)
                {
                    customizations.Add(new CustomizationViewModel
                    {
                        ModifierId = modifier.Id,
                        OptionId = selectedOption.Id,
                        Name = $"{modifier.Name}: {selectedOption.Name}",
                        PriceDelta = selectedOption.PriceDelta
                    });
                    
                    customizationTexts.Add($"{modifier.Name}: {selectedOption.Name}");
                }
            }

            var selectedItem = new SelectedItemViewModel
            {
                Id = menuItem.Id,
                Name = menuItem.Name,
                Quantity = menuItem.SelectedQuantity,
                BasePrice = menuItem.BasePrice,
                Customizations = customizations,
                CustomizationsText = customizationTexts.Any() ? string.Join(", ", customizationTexts) : "No customizations"
            };

            SelectedItems.Add(selectedItem);
            menuItem.SelectedQuantity = 0; // Reset quantity
            
            // Clear modifier selections
            foreach (var modifier in menuItem.Modifiers)
            {
                modifier.SelectedOptions.Clear();
            }
            
            UpdateSummary();
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is SelectedItemViewModel item)
        {
            SelectedItems.Remove(item);
            UpdateSummary();
        }
    }

    private void UpdateSummary()
    {
        ItemsCountText.Text = $"Items: {ItemsCount}";
        TotalAmountText.Text = $"Total: ${TotalAmount:F2}";
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MenuItemViewModel item)
        {
            AddItemToSelection(item.Id);
        }
    }

    private void QuantityMinus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MenuItemViewModel item)
        {
            DecreaseQuantity(item.Id);
        }
    }

    private void QuantityPlus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MenuItemViewModel item)
        {
            IncreaseQuantity(item.Id);
        }
    }

    private void ModifierOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ModifierOptionViewModel option)
        {
            // Find the parent modifier
            var modifier = MenuItems.SelectMany(m => m.Modifiers)
                .FirstOrDefault(m => m.Options.Contains(option));
            
            if (modifier != null)
            {
                if (checkBox.IsChecked == true)
                {
                    modifier.SelectOption(option);
                }
                else
                {
                    modifier.DeselectOption(option);
                }
            }
        }
    }

    private void ShowCustomizationPanel(MenuItemViewModel item)
    {
        if (item.HasModifiers)
        {
            ModifiersItemsControl.ItemsSource = item.Modifiers;
            CustomizationPanel.Visibility = Visibility.Visible;
        }
        else
        {
            CustomizationPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void CustomizeItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MenuItemViewModel item)
        {
            ShowCustomizationPanel(item);
        }
    }

    private async void AddToOrder_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedItems.Count == 0 || SelectedServer == null) return;

        try
        {
            var orderItems = SelectedItems.Select(item => new OrderApiService.CreateOrderItemDto(
                item.Id,
                null, // ComboId
                item.Quantity,
                item.Customizations.Select(c => new OrderApiService.ModifierSelectionDto(c.ModifierId, c.OptionId)).ToList()
            )).ToList();

            if (OrderId.HasValue)
            {
                // Add to existing order
                if (_orderService == null)
                {
                    ShowErrorDialog("Service Not Available", "Order service is not available.");
                    return;
                }
                await _orderService.AddItemsAsync(OrderId.Value, orderItems);
            }
            else
            {
                // Create new order
                var request = new OrderApiService.CreateOrderRequestDto(
                    SessionId,
                    null, // BillingId
                    TableLabel,
                    SelectedServer.Id,
                    SelectedServer.Name,
                    orderItems
                );
                
                if (_orderService == null)
                {
                    ShowErrorDialog("Service Not Available", "Order service is not available.");
                    return;
                }
                var order = await _orderService.CreateOrderAsync(request);
                if (order != null)
                {
                    OrderId = order.Id;
                }
            }

            this.Hide();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to add items to order: {ex.Message}");
            await ShowErrorDialog("Order Error", $"Failed to add items to order: {ex.Message}");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    private async Task ShowErrorDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // Fallback: Log error if dialog fails
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }

    // Properties for binding
    public int ItemsCount => SelectedItems.Sum(i => i.Quantity);
    public decimal TotalAmount => SelectedItems.Sum(i => i.Quantity * i.BasePrice);
    public bool CanAddToOrder => SelectedItems.Count > 0 && SelectedServer != null;
}

// ViewModels
public class MenuItemViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal BasePrice { get; set; }
    public string SKU { get; set; } = "";
    public string? PictureUrl { get; set; }
    public int SelectedQuantity { get; set; }
    public bool CanAdd => SelectedQuantity > 0;
    public string PriceText => $"${BasePrice:F2}";
    
    public ObservableCollection<ModifierViewModel> Modifiers { get; } = new();
    public bool HasModifiers => Modifiers.Count > 0;

    public ICommand? QuantityMinusCommand { get; set; }
    public ICommand? QuantityPlusCommand { get; set; }
    public ICommand? AddItemCommand { get; set; }

    public MenuItemViewModel() { }

    public MenuItemViewModel(MenuApiService.MenuItemDto dto, IReadOnlyList<MenuApiService.ModifierDto> modifiers)
    {
        Id = dto.Id;
        Name = dto.Name;
        Description = dto.Description ?? "";
        Category = dto.Category ?? "";
        BasePrice = dto.SellingPrice;
        SKU = dto.Sku ?? "";
        PictureUrl = dto.PictureUrl;
        
        // Convert modifiers to ViewModels
        foreach (var modifier in modifiers)
        {
            Modifiers.Add(new ModifierViewModel(modifier));
        }
    }
}

public class ModifierViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool AllowMultiple { get; set; }
    public int? MaxSelections { get; set; }
    public ObservableCollection<ModifierOptionViewModel> Options { get; } = new();
    public ObservableCollection<ModifierOptionViewModel> SelectedOptions { get; } = new();
    
    public string DisplayName => IsRequired ? $"{Name} *" : Name;
    public bool IsValid => !IsRequired || SelectedOptions.Count > 0;

    public ModifierViewModel() { }

    public ModifierViewModel(MenuApiService.ModifierDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        IsRequired = dto.IsRequired;
        AllowMultiple = dto.AllowMultiple;
        MaxSelections = dto.MaxSelections;
        
        foreach (var option in dto.Options)
        {
            Options.Add(new ModifierOptionViewModel(option));
        }
    }
    
    public void SelectOption(ModifierOptionViewModel option)
    {
        if (!AllowMultiple)
        {
            SelectedOptions.Clear();
        }
        
        if (MaxSelections.HasValue && SelectedOptions.Count >= MaxSelections.Value)
        {
            return; // Can't select more
        }
        
        if (!SelectedOptions.Contains(option))
        {
            SelectedOptions.Add(option);
        }
    }
    
    public void DeselectOption(ModifierOptionViewModel option)
    {
        SelectedOptions.Remove(option);
    }
}

public class ModifierOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal PriceDelta { get; set; }
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }
    
    public string DisplayName => PriceDelta != 0 ? $"{Name} (${PriceDelta:+0.00;-0.00;0})" : Name;
    public string PriceText => PriceDelta switch
    {
        > 0 => $"+${PriceDelta:F2}",
        < 0 => $"-${Math.Abs(PriceDelta):F2}",
        _ => ""
    };

    public ModifierOptionViewModel() { }

    public ModifierOptionViewModel(MenuApiService.ModifierOptionDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        PriceDelta = dto.PriceDelta;
        IsAvailable = dto.IsAvailable;
        SortOrder = dto.SortOrder;
    }
}

public class SelectedItemViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal BasePrice { get; set; }
    public List<CustomizationViewModel> Customizations { get; set; } = new();
    public string CustomizationsText { get; set; } = "";
    public string QuantityText => $"Qty: {Quantity}";
    public decimal TotalPrice => BasePrice + Customizations.Sum(c => c.PriceDelta);
    public string TotalPriceText => $"${TotalPrice:F2}";
}

public class CustomizationViewModel
{
    public long ModifierId { get; set; }
    public long OptionId { get; set; }
    public string Name { get; set; } = "";
    public decimal PriceDelta { get; set; }
}

public class ServerViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}

// Simple RelayCommand implementation
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
