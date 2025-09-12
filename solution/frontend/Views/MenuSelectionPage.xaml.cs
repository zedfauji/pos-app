using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using System.ComponentModel;
using System.Windows.Input;

namespace MagiDesk.Frontend.Views;

public sealed partial class MenuSelectionPage : Page, INotifyPropertyChanged
{
    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();
    public ObservableCollection<SelectedItemViewModel> SelectedItems { get; } = new();
    public ObservableCollection<ServerViewModel> AvailableServers { get; } = new();
    
    private ServerViewModel? _selectedServer;
    public ServerViewModel? SelectedServer 
    { 
        get => _selectedServer; 
        set 
        { 
            _selectedServer = value; 
            OnPropertyChanged(nameof(SelectedServer));
            OnPropertyChanged(nameof(CanAddToOrder));
        } 
    }
    
    public string TableLabel { get; set; } = "";
    public Guid SessionId { get; set; }
    public long? OrderId { get; set; }
    
    public bool CanAddToOrder => SelectedItems.Count > 0 && SelectedServer != null;

    private readonly MenuApiService? _menuService;
    private readonly OrderApiService? _orderService;

    public MenuSelectionPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        
        try
        {
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
        }
        catch (Exception ex)
        {
            // Show error dialog instead of throwing exception
            ShowErrorDialog("Initialization Error", $"Failed to initialize Menu Selection page: {ex.Message}");
            return;
        }
        
        LoadServers();
        UpdateSummary();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is MenuSelectionParams @params)
        {
            SessionId = @params.SessionId;
            OrderId = @params.OrderId;
            TableLabel = @params.TableLabel;
            
            TableInfoText.Text = $"Table: {TableLabel}";
            LoadMenuItems();
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
                    QuantityMinusCommand = new Views.RelayCommand(() => DecreaseQuantity(item.Id)),
                    QuantityPlusCommand = new Views.RelayCommand(() => IncreaseQuantity(item.Id)),
                    AddItemCommand = new Views.RelayCommand(() => AddItemToSelection(item.Id))
                });
            }
            
            System.Diagnostics.Debug.WriteLine($"Loaded {MenuItems.Count} menu items");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load menu items: {ex.Message}");
            await ShowErrorDialog($"Failed to load menu items: {ex.Message}");
        }
    }

    private void LoadServers()
    {
        try
        {
            AvailableServers.Clear();
            AvailableServers.Add(new ServerViewModel { Id = 1, Name = "Server 1" });
            AvailableServers.Add(new ServerViewModel { Id = 2, Name = "Server 2" });
            AvailableServers.Add(new ServerViewModel { Id = 3, Name = "Server 3" });
            
            if (AvailableServers.Count > 0)
            {
                SelectedServer = AvailableServers[0];
            }
            
            System.Diagnostics.Debug.WriteLine($"Loaded {AvailableServers.Count} servers");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load servers: {ex.Message}");
        }
    }

    private void DecreaseQuantity(long itemId)
    {
        var item = MenuItems.FirstOrDefault(i => i.Id == itemId);
        if (item != null && item.SelectedQuantity > 0)
        {
            item.SelectedQuantity--;
            UpdateSummary();
        }
    }

    private void IncreaseQuantity(long itemId)
    {
        var item = MenuItems.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            item.SelectedQuantity++;
            UpdateSummary();
        }
    }

    private void AddItemToSelection(long itemId)
    {
        var menuItem = MenuItems.FirstOrDefault(i => i.Id == itemId);
        if (menuItem != null && menuItem.SelectedQuantity > 0)
        {
            // Validate required modifiers
            var invalidModifiers = menuItem.Modifiers.Where(m => !m.IsValid).ToList();
            if (invalidModifiers.Any())
            {
                var requiredModifiers = string.Join(", ", invalidModifiers.Select(m => m.DisplayName));
                System.Diagnostics.Debug.WriteLine($"Required modifiers not selected: {requiredModifiers}");
                ShowErrorDialog($"Required modifiers not selected: {requiredModifiers}");
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
            System.Diagnostics.Debug.WriteLine($"Added {selectedItem.Name} to selection");
        }
    }

    private void UpdateSummary()
    {
        var totalItems = SelectedItems.Sum(item => item.Quantity);
        var totalPrice = SelectedItems.Sum(item => item.TotalPrice);
        
        TotalItemsText.Text = totalItems.ToString();
        TotalPriceText.Text = totalPrice.ToString("C");
        
        OnPropertyChanged(nameof(CanAddToOrder));
    }

    private void ModifierOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ModifierOptionViewModel option)
        {
            var modifier = option.ParentModifier;
            if (modifier != null)
            {
                if (checkBox.IsChecked == true)
                {
                    if (!modifier.AllowMultiple)
                    {
                        // Clear other selections for single-select modifiers
                        foreach (var otherOption in modifier.Options)
                        {
                            if (otherOption != option)
                            {
                                otherOption.IsSelected = false;
                            }
                        }
                    }
                    modifier.SelectedOptions.Add(option);
                }
                else
                {
                    modifier.SelectedOptions.Remove(option);
                }
                
                modifier.OnPropertyChanged(nameof(modifier.IsValid));
                modifier.OnPropertyChanged(nameof(modifier.SelectedOptions));
            }
        }
    }

    private void CustomizeItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MenuItemViewModel item)
        {
            // Find the customization panel for this item
            var parent = button.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent.FindName("CustomizationPanel") is StackPanel panel)
                {
                    panel.Visibility = panel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
                parent = parent.Parent as FrameworkElement;
            }
        }
    }

    private async void AddToOrder_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedServer == null || SelectedItems.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("Cannot add to order: No server selected or no items selected");
            await ShowErrorDialog("Please select a server and add items to the order.");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"AddToOrder_Click: SelectedServer={SelectedServer?.Name}, SelectedItems.Count={SelectedItems.Count}, OrderId={OrderId}");
            
            var orderItems = new List<OrderApiService.CreateOrderItemDto>();
            
            foreach (var selectedItem in SelectedItems)
            {
                var modifiers = selectedItem.Customizations.Select(c => 
                    new OrderApiService.ModifierSelectionDto(c.ModifierId, c.OptionId)).ToList();
                
                orderItems.Add(new OrderApiService.CreateOrderItemDto(
                    selectedItem.Id, // MenuItemId
                    null, // ComboId
                    selectedItem.Quantity,
                    modifiers
                ));
            }

            if (OrderId.HasValue)
            {
                // Add to existing order
                if (_orderService == null)
                {
                    ShowErrorDialog("Service Not Available", "Order service is not available.");
                    return;
                }
                var result = await _orderService.AddItemsAsync(OrderId.Value, orderItems);
                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to add items to order {OrderId.Value}: AddItemsAsync returned null");
                    await ShowErrorDialog("Failed to add items to existing order. Please try again.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"Added {orderItems.Count} items to existing order {OrderId.Value}");
            }
            else
            {
                // Create new order
                var createRequest = new OrderApiService.CreateOrderRequestDto(
                    SessionId,
                    null, // BillingId
                    TableLabel,
                    SelectedServer.Id.ToString(),
                    SelectedServer.Name,
                    orderItems
                );
                
                System.Diagnostics.Debug.WriteLine($"Creating new order: SessionId={SessionId}, TableLabel={TableLabel}, ServerId={SelectedServer.Id}, ServerName={SelectedServer.Name}, ItemCount={orderItems.Count}");
                
                if (_orderService == null)
                {
                    ShowErrorDialog("Service Not Available", "Order service is not available.");
                    return;
                }
                var newOrder = await _orderService.CreateOrderAsync(createRequest);
                if (newOrder == null)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to create order: CreateOrderAsync returned null");
                    await ShowErrorDialog("Failed to create order. Please try again.");
                    return;
                }
                
                OrderId = newOrder.Id;
                System.Diagnostics.Debug.WriteLine($"Created new order {newOrder.Id} with {orderItems.Count} items");
            }

            // Navigate back to tables page
            Frame.Navigate(typeof(TablesPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to add items to order: {ex.Message}");
            await ShowErrorDialog($"Failed to add items to order: {ex.Message}");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TablesPage));
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        LoadMenuItems();
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        SelectedItems.Clear();
        UpdateSummary();
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

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is MenuItemViewModel item)
        {
            AddItemToSelection(item.Id);
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        await new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        }.ShowAsync();
    }

    private async void ShowErrorDialog(string title, string message)
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

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// ViewModels
    public class MenuItemViewModel : INotifyPropertyChanged
{
    public long Id { get; }
    public string Name { get; }
    public string Description { get; }
    public decimal BasePrice { get; }
    public string PriceText => $"${BasePrice:F2}";
    public string SKU { get; }
    public bool HasModifiers => Modifiers.Count > 0;
    
    public ObservableCollection<ModifierViewModel> Modifiers { get; } = new();
    
    private int _selectedQuantity;
    public int SelectedQuantity
    {
        get => _selectedQuantity;
        set
        {
            if (_selectedQuantity != value)
            {
                _selectedQuantity = value;
                OnPropertyChanged(nameof(SelectedQuantity));
                OnPropertyChanged(nameof(CanDecrease));
                OnPropertyChanged(nameof(CanAdd));
            }
        }
    }
    
    public bool CanDecrease => SelectedQuantity > 0;
    public bool CanAdd => SelectedQuantity > 0;

    public ICommand QuantityMinusCommand { get; set; } = null!;
    public ICommand QuantityPlusCommand { get; set; } = null!;
    public ICommand AddItemCommand { get; set; } = null!;

    public MenuItemViewModel(MenuApiService.MenuItemDto dto, IReadOnlyList<MenuApiService.ModifierDto> modifiers)
    {
        Id = dto.Id;
        Name = dto.Name;
        Description = dto.Description ?? "";
        BasePrice = dto.SellingPrice;
        SKU = dto.Sku;
        
        foreach (var modifier in modifiers)
        {
            Modifiers.Add(new ModifierViewModel(modifier));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class ModifierViewModel : INotifyPropertyChanged
{
    public long Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsRequired { get; }
    public bool AllowMultiple { get; }
    public int? MaxSelections { get; }
    
    public string DisplayName => IsRequired ? $"{Name} *" : Name;
    
    public ObservableCollection<ModifierOptionViewModel> Options { get; } = new();
    public ObservableCollection<ModifierOptionViewModel> SelectedOptions { get; } = new();
    
    public bool IsValid => !IsRequired || SelectedOptions.Count > 0;

    public ModifierViewModel(MenuApiService.ModifierDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Description = ""; // ModifierDto doesn't have Description property
        IsRequired = dto.IsRequired;
        AllowMultiple = dto.AllowMultiple;
        MaxSelections = dto.MaxSelections;
        
        foreach (var option in dto.Options)
        {
            Options.Add(new ModifierOptionViewModel(option, this));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class ModifierOptionViewModel : INotifyPropertyChanged
{
    public long Id { get; }
    public string Name { get; }
    public decimal PriceDelta { get; }
    public string DisplayName => PriceDelta != 0 ? $"{Name} ({PriceDelta:+$0.00;-$0.00;$0.00})" : Name;
    
    public ModifierViewModel? ParentModifier { get; }
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public ModifierOptionViewModel(MenuApiService.ModifierOptionDto dto, ModifierViewModel parent)
    {
        Id = dto.Id;
        Name = dto.Name;
        PriceDelta = dto.PriceDelta;
        ParentModifier = parent;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class SelectedItemViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public string QuantityText => $"Qty: {Quantity}";
    public decimal BasePrice { get; set; }
    public List<CustomizationViewModel> Customizations { get; set; } = new();
    public string CustomizationsText { get; set; } = "";
    
    public decimal TotalPrice => (BasePrice + Customizations.Sum(c => c.PriceDelta)) * Quantity;
    public string TotalPriceText => TotalPrice.ToString("C");
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
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class MenuSelectionParams
{
    public Guid SessionId { get; set; }
    public long? OrderId { get; set; }
    public string TableLabel { get; set; } = "";
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
