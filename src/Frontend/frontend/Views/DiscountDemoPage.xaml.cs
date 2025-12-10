using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MagiDesk.Frontend.Views;

public sealed partial class DiscountDemoPage : Page
{
    private string _mockBillingId = "BILL-12345";
    private Guid _mockCustomerId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private decimal _originalTotal = 45.50m;

    public DiscountDemoPage()
    {
        this.InitializeComponent();
        Loaded += DiscountDemoPage_Loaded;
    }

    private async void DiscountDemoPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize the discount panel with mock data
        try
        {
            await DemoDiscountPanel.InitializeAsync(_mockBillingId, _mockCustomerId);
            
            // Subscribe to discount events
            DemoDiscountPanel.DiscountApplied += OnDiscountApplied;
            DemoDiscountPanel.DiscountRemoved += OnDiscountRemoved;
            DemoDiscountPanel.SummaryChanged += OnSummaryChanged;
        }
        catch (Exception ex)
        {
            // Handle initialization error gracefully
            await ShowErrorAsync("Demo Error", "This is a demo - backend API not available. UI components are functional.");
        }
    }

    private void OnDiscountApplied(object sender, DiscountAppliedEventArgs e)
    {
        UpdateTotals();
        ShowNotification($"✅ Applied: {e.DiscountName} (-${e.DiscountAmount:F2})");
    }

    private void OnDiscountRemoved(object sender, DiscountRemovedEventArgs e)
    {
        UpdateTotals();
        ShowNotification($"❌ Removed discount");
    }

    private void OnSummaryChanged(object sender, DiscountSummaryChangedEventArgs e)
    {
        UpdateTotals();
    }

    private async void UpdateTotals()
    {
        try
        {
            var totalSavings = await DemoDiscountPanel.GetTotalSavingsAsync();
            var newTotal = _originalTotal - totalSavings;
            
            FinalTotalText.Text = $"${newTotal:F2}";
            SavingsText.Text = totalSavings > 0 ? $"You saved: ${totalSavings:F2}" : "No discounts applied";
        }
        catch
        {
            // Handle gracefully for demo
        }
    }

    private async void BrowseDiscounts_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new DiscountManagementDialog(_mockBillingId, _mockCustomerId);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                ShowNotification($"✅ Applied {dialog.SelectedDiscounts.Count} discount(s)");
                UpdateTotals();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Demo", "Discount browser opened! (Backend API needed for full functionality)");
        }
    }

    private async void AutoApply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Mock auto-apply for demo
            ShowNotification("⚡ Auto-applying best discounts...");
            
            // Simulate delay
            await Task.Delay(1000);
            
            // Mock result
            var mockSavings = 11.38m; // 25% VIP discount
            var newTotal = _originalTotal - mockSavings;
            
            FinalTotalText.Text = $"${newTotal:F2}";
            SavingsText.Text = $"You saved: ${mockSavings:F2}";
            
            ShowNotification("✅ Applied VIP discount (25% off) - $11.38 saved!");
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Demo", "Auto-apply demonstrated! (Backend API needed for real discounts)");
        }
    }

    private async void RedeemVoucher_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Redeem Voucher",
            Content = CreateVoucherContent(),
            PrimaryButtonText = "Redeem",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ShowNotification("🎫 Voucher redeemed successfully!");
        }
    }

    private StackPanel CreateVoucherContent()
    {
        var panel = new StackPanel { Spacing = 12 };
        
        panel.Children.Add(new TextBlock 
        { 
            Text = "Enter voucher code:", 
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
        });
        
        var textBox = new TextBox 
        { 
            PlaceholderText = "e.g., SAVE20, WELCOME15",
            Text = "VIP25" // Pre-filled for demo
        };
        panel.Children.Add(textBox);
        
        panel.Children.Add(new TextBlock 
        { 
            Text = "💡 Demo codes: VIP25, WELCOME15, SAVE20", 
            FontSize = 12,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        });
        
        return panel;
    }

    private void ShowNotification(string message)
    {
        // In a real app, this would show a toast notification or info bar
        // For demo, we'll just update the UI temporarily
        var originalText = SavingsText.Text;
        SavingsText.Text = message;
        
        // Reset after 3 seconds
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (s, e) =>
        {
            SavingsText.Text = originalText;
            timer.Stop();
        };
        timer.Start();
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
