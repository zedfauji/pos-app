using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace MagiDesk.Frontend.Views;

// Simple mock configuration for discount services
public class MockConfiguration : IConfiguration
{
    public string? this[string key] 
    { 
        get => key switch
        {
            "DiscountApi:BaseUrl" => "https://magidesk-discount-904541739138.northamerica-south1.run.app",
            "CustomerApi:BaseUrl" => "https://magidesk-customer-904541739138.northamerica-south1.run.app",
            "OrderApi:BaseUrl" => "https://magidesk-order-904541739138.northamerica-south1.run.app",
            _ => null
        };
        set { }
    }
    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);
    public IConfigurationSection GetSection(string key) => new MockConfigurationSection();
}

public class MockConfigurationSection : IConfigurationSection
{
    public string? this[string key] { get => null; set { } }
    public string Key => string.Empty;
    public string Path => string.Empty;
    public string? Value { get => null; set { } }
    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();
    public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);
    public IConfigurationSection GetSection(string key) => this;
}

public sealed partial class DiscountManagementDialog : ContentDialog
{
    private readonly DiscountService _discountService;
    private readonly CustomerApiService _customerService;
    private readonly string _billingId;
    private readonly Guid _customerId;
    
    private ObservableCollection<AvailableDiscountDto> _autoDiscounts = new();
    private ObservableCollection<AvailableDiscountDto> _manualDiscounts = new();
    private List<AvailableDiscountDto> _selectedDiscounts = new();
    
    public List<AvailableDiscountDto> SelectedDiscounts => _selectedDiscounts;
    public decimal TotalSavings { get; private set; }

    public DiscountManagementDialog(string billingId, Guid customerId)
    {
        this.InitializeComponent();
        _billingId = billingId;
        _customerId = customerId;
        
        // Get services from App
        _discountService = new DiscountService(
            new HttpClient(), 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscountService>.Instance,
            App.CustomerApi!,
            new MockConfiguration()
        );
        _customerService = App.CustomerApi!;
        
        AutoDiscountsList.ItemsSource = _autoDiscounts;
        ManualDiscountsList.ItemsSource = _manualDiscounts;
        
        Loaded += DiscountManagementDialog_Loaded;
    }

    private async void DiscountManagementDialog_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCustomerInfoAsync();
        await LoadAvailableDiscountsAsync();
    }

    private async Task LoadCustomerInfoAsync()
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(_customerId);
            if (customer != null)
            {
                CustomerInfoText.Text = $"Customer: {customer.FullName} ({customer.Email})";
            }
        }
        catch (Exception ex)
        {
            CustomerInfoText.Text = "Customer: Unknown";
        }
    }

    private async Task LoadAvailableDiscountsAsync()
    {
        try
        {
            var discounts = await _discountService.GetAvailableDiscountsAsync(_customerId, _billingId);
            
            _autoDiscounts.Clear();
            _manualDiscounts.Clear();
            
            foreach (var discount in discounts)
            {
                if (discount.IsAutoApply)
                {
                    _autoDiscounts.Add(discount);
                }
                else
                {
                    _manualDiscounts.Add(discount);
                }
            }
            
            // Show empty state if no discounts
            EmptyStatePanel.Visibility = (discounts.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            
            // Auto-apply automatic discounts
            await ApplyAutomaticDiscountsAsync();
        }
        catch (Exception ex)
        {
            // Show error message
            EmptyStatePanel.Visibility = Visibility.Visible;
        }
    }

    private async Task ApplyAutomaticDiscountsAsync()
    {
        foreach (var discount in _autoDiscounts)
        {
            try
            {
                await _discountService.ApplyDiscountAsync(_billingId, discount.Id, discount.Type);
            }
            catch (Exception ex)
            {
                // Log error but continue with other discounts
            }
        }
    }

    private void ManualDiscountsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedDiscounts.Clear();
        
        foreach (var item in ManualDiscountsList.SelectedItems)
        {
            if (item is AvailableDiscountDto discount)
            {
                _selectedDiscounts.Add(discount);
            }
        }
        
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var count = _selectedDiscounts.Count;
        
        if (count == 0)
        {
            SelectedDiscountsText.Text = "No discounts selected";
            TotalSavingsText.Text = "Total Savings: $0.00";
            TotalSavings = 0;
        }
        else
        {
            SelectedDiscountsText.Text = $"{count} discount{(count > 1 ? "s" : "")} selected";
            
            // Calculate estimated savings (this is approximate)
            var estimatedSavings = _selectedDiscounts.Sum(d => d.DiscountAmount ?? 0);
            TotalSavingsText.Text = $"Estimated Savings: ${estimatedSavings:F2}";
            TotalSavings = estimatedSavings;
        }
        
        ClearAllButton.IsEnabled = count > 0;
        PreviewButton.IsEnabled = count > 0;
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        ManualDiscountsList.SelectedItems.Clear();
        _selectedDiscounts.Clear();
        UpdateSummary();
    }

    private async void Preview_Click(object sender, RoutedEventArgs e)
    {
        // Show preview of total with discounts applied
        var previewDialog = new ContentDialog
        {
            Title = "Discount Preview",
            Content = CreatePreviewContent(),
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };
        
        await previewDialog.ShowAsync();
    }

    private StackPanel CreatePreviewContent()
    {
        var panel = new StackPanel { Spacing = 8 };
        
        panel.Children.Add(new TextBlock 
        { 
            Text = "Selected Discounts:", 
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
        });
        
        foreach (var discount in _selectedDiscounts)
        {
            var discountPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            discountPanel.Children.Add(new TextBlock { Text = "•" });
            discountPanel.Children.Add(new TextBlock { Text = discount.Name });
            discountPanel.Children.Add(new TextBlock 
            { 
                Text = discount.DiscountPercentage.HasValue 
                    ? $"({discount.DiscountPercentage}%)" 
                    : $"(${discount.DiscountAmount})",
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
            });
            panel.Children.Add(discountPanel);
        }
        
        panel.Children.Add(new Border { Height = 1, Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"], Margin = new Thickness(0, 8, 0, 8) });
        panel.Children.Add(new TextBlock 
        { 
            Text = $"Estimated Total Savings: ${TotalSavings:F2}",
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"]
        });
        
        return panel;
    }

    private async void RedeemVoucher_Click(object sender, RoutedEventArgs e)
    {
        var voucherDialog = new VoucherRedemptionDialog(_customerId, _billingId);
        var result = await voucherDialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            // Refresh discounts to show newly redeemed voucher
            await LoadAvailableDiscountsAsync();
        }
    }

    private async void ViewCombos_Click(object sender, RoutedEventArgs e)
    {
        var comboDialog = new ComboDealsDialog(_billingId);
        await comboDialog.ShowAsync();
    }
}

// Voucher Redemption Dialog
public sealed partial class VoucherRedemptionDialog : ContentDialog
{
    private readonly Guid _customerId;
    private readonly string _billingId;
    private readonly DiscountService _discountService;

    public VoucherRedemptionDialog(Guid customerId, string billingId)
    {
        _customerId = customerId;
        _billingId = billingId;
        _discountService = new DiscountService(
            new HttpClient(), 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscountService>.Instance,
            App.CustomerApi!,
            new MockConfiguration()
        );
        
        InitializeDialog();
    }

    private void InitializeDialog()
    {
        Title = "Redeem Voucher";
        PrimaryButtonText = "Redeem";
        SecondaryButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;
        
        var panel = new StackPanel { Spacing = 16 };
        
        panel.Children.Add(new TextBlock 
        { 
            Text = "Enter voucher code:", 
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
        });
        
        var voucherCodeBox = new TextBox 
        { 
            Name = "VoucherCodeBox",
            PlaceholderText = "Enter voucher code",
            CharacterCasing = CharacterCasing.Upper
        };
        panel.Children.Add(voucherCodeBox);
        
        var infoBar = new InfoBar
        {
            Name = "VoucherInfoBar",
            IsOpen = false
        };
        panel.Children.Add(infoBar);
        
        Content = panel;
        
        PrimaryButtonClick += async (s, e) =>
        {
            var deferral = e.GetDeferral();
            
            try
            {
                var code = voucherCodeBox.Text?.Trim();
                if (string.IsNullOrEmpty(code))
                {
                    infoBar.Severity = InfoBarSeverity.Warning;
                    infoBar.Message = "Please enter a voucher code";
                    infoBar.IsOpen = true;
                    e.Cancel = true;
                    return;
                }
                
                var voucher = await _discountService.RedeemVoucherAsync(code, _customerId, _billingId);
                if (voucher == null)
                {
                    infoBar.Severity = InfoBarSeverity.Error;
                    infoBar.Message = "Invalid voucher code or voucher has expired";
                    infoBar.IsOpen = true;
                    e.Cancel = true;
                    return;
                }
                
                // Success - voucher redeemed
            }
            catch (Exception ex)
            {
                infoBar.Severity = InfoBarSeverity.Error;
                infoBar.Message = "Error redeeming voucher. Please try again.";
                infoBar.IsOpen = true;
                e.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        };
    }
}

// Combo Deals Dialog
public sealed partial class ComboDealsDialog : ContentDialog
{
    private readonly string _billingId;
    private readonly DiscountService _discountService;
    private ObservableCollection<ComboDto> _combos = new();

    public ComboDealsDialog(string billingId)
    {
        _billingId = billingId;
        _discountService = new DiscountService(
            new HttpClient(), 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscountService>.Instance,
            App.CustomerApi!,
            new MockConfiguration()
        );
        
        InitializeDialog();
        Loaded += ComboDealsDialog_Loaded;
    }

    private void InitializeDialog()
    {
        Title = "Combo Deals";
        CloseButtonText = "Close";
        Width = 600;
        Height = 500;
        
        var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var listView = new ListView 
        { 
            ItemsSource = _combos,
            SelectionMode = ListViewSelectionMode.None
        };
        
        // Create item template for combos
        var template = new DataTemplate();
        // Template creation would go here - simplified for brevity
        
        scrollViewer.Content = listView;
        Content = scrollViewer;
    }

    private async void ComboDealsDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var combos = await _discountService.GetAvailableCombosAsync();
            _combos.Clear();
            foreach (var combo in combos)
            {
                _combos.Add(combo);
            }
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }
}
