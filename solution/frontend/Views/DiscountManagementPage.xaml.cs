using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class DiscountManagementPage : Page
{
    private readonly DiscountService _discountService;
    private ObservableCollection<DiscountItemViewModel> _activeDiscounts = new();
    private ObservableCollection<VoucherItemViewModel> _vouchers = new();
    private ObservableCollection<ComboItemViewModel> _combos = new();

    public DiscountManagementPage()
    {
        this.InitializeComponent();
        
        // Initialize service with mock configuration
        _discountService = new DiscountService(
            new HttpClient(), 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscountService>.Instance,
            App.CustomerApi!,
            new MockConfiguration()
        );
        
        ActiveDiscountsList.ItemsSource = _activeDiscounts;
        VouchersList.ItemsSource = _vouchers;
        CombosList.ItemsSource = _combos;
        
        Loaded += DiscountManagementPage_Loaded;
    }

    private async void DiscountManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // Load mock data for demonstration
            LoadMockData();
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Error", $"Failed to load discount data: {ex.Message}");
        }
    }

    private void LoadMockData()
    {
        // Mock active discounts
        _activeDiscounts.Clear();
        _activeDiscounts.Add(new DiscountItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "VIP Customer Discount",
            Description = "25% off for VIP members",
            DiscountValue = "25%",
            EndDate = DateTime.Now.AddDays(30).ToString("MMM dd, yyyy"),
            Type = "Percentage"
        });
        
        _activeDiscounts.Add(new DiscountItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "New Customer Welcome",
            Description = "15% off first order",
            DiscountValue = "15%",
            EndDate = DateTime.Now.AddDays(60).ToString("MMM dd, yyyy"),
            Type = "Percentage"
        });

        _activeDiscounts.Add(new DiscountItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Happy Hour Special",
            Description = "$5 off orders over $25",
            DiscountValue = "$5.00",
            EndDate = DateTime.Now.AddDays(7).ToString("MMM dd, yyyy"),
            Type = "Fixed Amount"
        });

        // Mock vouchers
        _vouchers.Clear();
        _vouchers.Add(new VoucherItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "VIP Member Discount",
            Code = "VIP25",
            ExpiryDate = DateTime.Now.AddDays(30).ToString("MMM dd, yyyy"),
            Value = "25%"
        });

        _vouchers.Add(new VoucherItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Welcome New Customer",
            Code = "WELCOME15",
            ExpiryDate = DateTime.Now.AddDays(60).ToString("MMM dd, yyyy"),
            Value = "15%"
        });

        _vouchers.Add(new VoucherItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Save 20%",
            Code = "SAVE20",
            ExpiryDate = DateTime.Now.AddDays(14).ToString("MMM dd, yyyy"),
            Value = "20%"
        });

        // Mock combo deals
        _combos.Clear();
        _combos.Add(new ComboItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Burger Combo",
            Description = "Burger + Fries + Drink",
            OriginalPrice = "$21.50",
            ComboPrice = "$18.99",
            SavingsAmount = "Save $2.51"
        });

        _combos.Add(new ComboItemViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Pizza Deal",
            Description = "Large Pizza + 2 Drinks",
            OriginalPrice = "$25.00",
            ComboPrice = "$22.99",
            SavingsAmount = "Save $2.01"
        });
    }

    private void UpdateStatistics()
    {
        TotalDiscountsText.Text = (_activeDiscounts.Count + _vouchers.Count + _combos.Count).ToString();
        ActiveDiscountsText.Text = _activeDiscounts.Count.ToString();
        TotalSavingsText.Text = "$1,247.50"; // Mock total savings
        UsageCountText.Text = "342"; // Mock usage count
    }

    private async void NewDiscount_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DiscountEditDialog();
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await LoadDataAsync();
        }
    }

    private async void NewActiveDiscount_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DiscountEditDialog();
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await LoadDataAsync();
        }
    }

    private async void NewVoucher_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new VoucherEditDialog();
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await LoadDataAsync();
        }
    }

    private async void NewCombo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ComboEditDialog();
        dialog.XamlRoot = this.XamlRoot;
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await LoadDataAsync();
        }
    }

    private async void EditDiscount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DiscountItemViewModel discount)
        {
            var dialog = new DiscountEditDialog(discount);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadDataAsync();
            }
        }
    }

    private async void EditVoucher_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is VoucherItemViewModel voucher)
        {
            var dialog = new VoucherEditDialog(voucher);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadDataAsync();
            }
        }
    }

    private async void EditCombo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ComboItemViewModel combo)
        {
            var dialog = new ComboEditDialog(combo);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await LoadDataAsync();
            }
        }
    }

    private async void DeleteDiscount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DiscountItemViewModel discount)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Discount",
                Content = $"Are you sure you want to delete '{discount.Name}'?",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _activeDiscounts.Remove(discount);
                UpdateStatistics();
            }
        }
    }

    private async void DeleteVoucher_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is VoucherItemViewModel voucher)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Voucher",
                Content = $"Are you sure you want to delete voucher '{voucher.Code}'?",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _vouchers.Remove(voucher);
                UpdateStatistics();
            }
        }
    }

    private async void DeleteCombo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ComboItemViewModel combo)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Combo Deal",
                Content = $"Are you sure you want to delete '{combo.Name}'?",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _combos.Remove(combo);
                UpdateStatistics();
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Implement search filtering
        var searchText = SearchBox.Text.ToLower();
        // Filter lists based on search text
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Implement type filtering
        if (FilterComboBox.SelectedItem is ComboBoxItem item)
        {
            var filterType = item.Content.ToString();
            // Filter lists based on type
        }
    }

    private void ActiveDiscountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection changes
    }

    private async void Analytics_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DiscountAnalyticsDialog();
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        
        await dialog.ShowAsync();
    }
}

// ViewModels for data binding
public class DiscountItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountValue { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class VoucherItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ExpiryDate { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class ComboItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OriginalPrice { get; set; } = string.Empty;
    public string ComboPrice { get; set; } = string.Empty;
    public string SavingsAmount { get; set; } = string.Empty;
}
