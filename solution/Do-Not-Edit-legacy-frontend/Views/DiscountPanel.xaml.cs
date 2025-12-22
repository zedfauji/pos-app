using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace MagiDesk.Frontend.Views;

public sealed partial class DiscountPanel : UserControl
{
    private readonly BillingDiscountService _billingDiscountService;
    private readonly DiscountService _discountService;
    
    private ObservableCollection<AvailableDiscountDto> _autoDiscounts = new();
    private ObservableCollection<AppliedDiscountDto> _appliedDiscounts = new();
    
    private string _billingId = string.Empty;
    private Guid _customerId = Guid.Empty;
    
    public event EventHandler<DiscountAppliedEventArgs>? DiscountApplied;
    public event EventHandler<DiscountRemovedEventArgs>? DiscountRemoved;
    public event EventHandler<DiscountSummaryChangedEventArgs>? SummaryChanged;

    public DiscountPanel()
    {
        this.InitializeComponent();
        
        // Initialize services
        _discountService = new DiscountService(
            new HttpClient(), 
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscountService>.Instance,
            App.CustomerApi!,
            new MockConfiguration()
        );
        
        _billingDiscountService = new BillingDiscountService(
            new HttpClient(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BillingDiscountService>.Instance,
            _discountService,
            new MockConfiguration()
        );
        
        AutoDiscountsRepeater.ItemsSource = _autoDiscounts;
        ManualDiscountsRepeater.ItemsSource = _appliedDiscounts;
    }

    public async Task InitializeAsync(string billingId, Guid customerId)
    {
        _billingId = billingId;
        _customerId = customerId;
        
        await LoadDiscountsAsync();
        await AutoApplyEligibleDiscountsAsync();
    }

    private async Task LoadDiscountsAsync()
    {
        try
        {
            // Load available discounts
            var availableDiscounts = await _discountService.GetAvailableDiscountsAsync(_customerId, _billingId);
            
            _autoDiscounts.Clear();
            foreach (var discount in availableDiscounts.Where(d => d.IsAutoApply))
            {
                _autoDiscounts.Add(discount);
            }
            
            // Load applied discounts
            var appliedDiscounts = await _discountService.GetAppliedDiscountsAsync(_billingId);
            
            _appliedDiscounts.Clear();
            foreach (var discount in appliedDiscounts)
            {
                _appliedDiscounts.Add(discount);
            }
            
            UpdateUI();
            AutoApplyButton.IsEnabled = _autoDiscounts.Any();
        }
        catch (Exception ex)
        {
            // Handle error - show empty state
            ShowEmptyState();
        }
    }

    private async Task AutoApplyEligibleDiscountsAsync()
    {
        try
        {
            var result = await _billingDiscountService.AutoApplyDiscountsAsync(_billingId, _customerId);
            
            if (result.Success && result.AppliedDiscounts > 0)
            {
                // Refresh applied discounts
                await LoadAppliedDiscountsAsync();
                
                // Notify parent of changes
                OnSummaryChanged(new DiscountSummaryChangedEventArgs
                {
                    TotalDiscounts = _appliedDiscounts.Count + _autoDiscounts.Count,
                    TotalSavings = result.TotalSavings
                });
            }
        }
        catch (Exception ex)
        {
            // Handle error silently for auto-apply
        }
    }

    private async Task LoadAppliedDiscountsAsync()
    {
        try
        {
            var appliedDiscounts = await _discountService.GetAppliedDiscountsAsync(_billingId);
            
            _appliedDiscounts.Clear();
            foreach (var discount in appliedDiscounts)
            {
                _appliedDiscounts.Add(discount);
            }
            
            UpdateUI();
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }

    private void UpdateUI()
    {
        var hasAutoDiscounts = _autoDiscounts.Any();
        var hasAppliedDiscounts = _appliedDiscounts.Any();
        var hasAnyDiscounts = hasAutoDiscounts || hasAppliedDiscounts;
        
        // Show/hide sections
        AutoDiscountsPanel.Visibility = hasAutoDiscounts ? Visibility.Visible : Visibility.Collapsed;
        ManualDiscountsPanel.Visibility = hasAppliedDiscounts ? Visibility.Visible : Visibility.Collapsed;
        SummaryPanel.Visibility = hasAnyDiscounts ? Visibility.Visible : Visibility.Collapsed;
        EmptyStatePanel.Visibility = hasAnyDiscounts ? Visibility.Collapsed : Visibility.Visible;
        
        // Update counts and totals
        var totalDiscounts = _autoDiscounts.Count + _appliedDiscounts.Count;
        var totalSavings = _appliedDiscounts.Sum(d => d.DiscountAmount);
        
        if (totalDiscounts > 0)
        {
            DiscountCountBadge.Visibility = Visibility.Visible;
            DiscountCountText.Text = totalDiscounts.ToString();
        }
        else
        {
            DiscountCountBadge.Visibility = Visibility.Collapsed;
        }
        
        TotalDiscountsText.Text = $"Total Discounts Applied: {totalDiscounts}";
        TotalSavingsText.Text = $"You saved: ${totalSavings:F2}";
    }

    private void ShowEmptyState()
    {
        AutoDiscountsPanel.Visibility = Visibility.Collapsed;
        ManualDiscountsPanel.Visibility = Visibility.Collapsed;
        SummaryPanel.Visibility = Visibility.Collapsed;
        EmptyStatePanel.Visibility = Visibility.Visible;
        DiscountCountBadge.Visibility = Visibility.Collapsed;
        AutoApplyButton.IsEnabled = false;
    }

    private async void BrowseAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new DiscountManagementDialog(_billingId, _customerId);
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // Apply selected discounts
                foreach (var discount in dialog.SelectedDiscounts)
                {
                    await ApplyDiscountAsync(discount);
                }
                
                // Refresh the panel
                await LoadAppliedDiscountsAsync();
            }
        }
        catch (Exception ex)
        {
            // Show error message
            await ShowErrorAsync("Error", "Failed to load discount browser. Please try again.");
        }
    }

    private async void AutoApply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AutoApplyButton.IsEnabled = false;
            await AutoApplyEligibleDiscountsAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Error", "Failed to auto-apply discounts. Please try again.");
        }
        finally
        {
            AutoApplyButton.IsEnabled = true;
        }
    }

    private async void RemoveDiscount_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is string discountId)
            {
                var success = await _billingDiscountService.RemoveDiscountFromBillingAsync(_billingId, discountId);
                
                if (success)
                {
                    await LoadAppliedDiscountsAsync();
                    
                    OnDiscountRemoved(new DiscountRemovedEventArgs
                    {
                        DiscountId = discountId,
                        BillingId = _billingId
                    });
                }
                else
                {
                    await ShowErrorAsync("Error", "Failed to remove discount. Please try again.");
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Error", "Failed to remove discount. Please try again.");
        }
    }

    private async void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = "Clear All Discounts",
                Content = "Are you sure you want to remove all applied discounts?",
                PrimaryButtonText = "Yes, Clear All",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };
            
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                var discountsToRemove = _appliedDiscounts.ToList();
                
                foreach (var discount in discountsToRemove)
                {
                    await _billingDiscountService.RemoveDiscountFromBillingAsync(_billingId, discount.Id);
                }
                
                await LoadAppliedDiscountsAsync();
                
                OnSummaryChanged(new DiscountSummaryChangedEventArgs
                {
                    TotalDiscounts = 0,
                    TotalSavings = 0
                });
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Error", "Failed to clear discounts. Please try again.");
        }
    }

    private async Task ApplyDiscountAsync(AvailableDiscountDto discount)
    {
        try
        {
            var result = await _billingDiscountService.ApplyDiscountToBillingAsync(_billingId, discount.Id, discount.Type);
            
            if (result.Success)
            {
                OnDiscountApplied(new DiscountAppliedEventArgs
                {
                    DiscountId = discount.Id,
                    DiscountName = discount.Name,
                    DiscountAmount = result.DiscountAmount,
                    BillingId = _billingId
                });
            }
            else
            {
                await ShowErrorAsync("Error", $"Failed to apply discount: {result.Message}");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Error", "Failed to apply discount. Please try again.");
        }
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

    // Event handlers
    private void OnDiscountApplied(DiscountAppliedEventArgs e)
    {
        DiscountApplied?.Invoke(this, e);
    }

    private void OnDiscountRemoved(DiscountRemovedEventArgs e)
    {
        DiscountRemoved?.Invoke(this, e);
    }

    private void OnSummaryChanged(DiscountSummaryChangedEventArgs e)
    {
        SummaryChanged?.Invoke(this, e);
    }

    // Public methods for external control
    public async Task RefreshAsync()
    {
        await LoadDiscountsAsync();
    }

    public async Task<decimal> GetTotalSavingsAsync()
    {
        return _appliedDiscounts.Sum(d => d.DiscountAmount);
    }

    public int GetTotalDiscountCount()
    {
        return _autoDiscounts.Count + _appliedDiscounts.Count;
    }
}

// Event argument classes
public class DiscountAppliedEventArgs : EventArgs
{
    public string DiscountId { get; set; } = string.Empty;
    public string DiscountName { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public string BillingId { get; set; } = string.Empty;
}

public class DiscountRemovedEventArgs : EventArgs
{
    public string DiscountId { get; set; } = string.Empty;
    public string BillingId { get; set; } = string.Empty;
}

public class DiscountSummaryChangedEventArgs : EventArgs
{
    public int TotalDiscounts { get; set; }
    public decimal TotalSavings { get; set; }
}
