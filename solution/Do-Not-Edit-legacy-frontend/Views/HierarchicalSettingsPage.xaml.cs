using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Data;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Settings;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;

namespace MagiDesk.Frontend.Views;

    public class SettingsCategory
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool HasChanges { get; set; }
        public bool IsConnected { get; set; }
        public string Status { get; set; } = string.Empty;
    }

public sealed partial class HierarchicalSettingsPage : Page
{
    public HierarchicalSettingsViewModel ViewModel { get; }
    private readonly Dictionary<string, Type> _settingsPages;
    private string _currentFilter = "all";
    private string _currentSearchQuery = "";
    private List<SettingsCategory> _allCategories = new();
    private List<SettingsCategory> _filteredCategories = new();

    public HierarchicalSettingsPage()
    {
        this.InitializeComponent();
        
        // Initialize ViewModel
        ViewModel = new HierarchicalSettingsViewModel();
        this.DataContext = ViewModel;

        // Initialize settings pages mapping
        _settingsPages = new Dictionary<string, Type>
        {
            { "general", typeof(GeneralSettingsPage) },
            { "pos", typeof(PosSettingsPage) },
            { "inventory", typeof(InventorySettingsPage) },
            { "customers", typeof(CustomersSettingsPage) },
            { "payments", typeof(PaymentsSettingsPage) },
            { "printers", typeof(PrinterSettingsPage) },
            { "notifications", typeof(NotificationsSettingsPage) },
            { "security", typeof(SecuritySettingsPage) },
            { "integrations", typeof(IntegrationsSettingsPage) },
            { "system", typeof(SystemSettingsPage) }
        };

        // Initialize UI
        InitializeSettingsTree();
        
        // Load settings
        _ = LoadSettingsAsync();
    }

    private void InitializeSettingsTree()
    {
        var treeItems = new List<SettingsTreeItem>
        {
            new() { 
                Key = "general", 
                DisplayName = "General", 
                Icon = "\uE80F", 
                Description = "Business information, theme, language, and basic settings",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "general.business", DisplayName = "Business Info", Icon = "\uE821" },
                    new() { Key = "general.appearance", DisplayName = "Appearance", Icon = "\uE790" },
                    new() { Key = "general.localization", DisplayName = "Language & Region", Icon = "\uE774" }
                }
            },
            new() { 
                Key = "pos", 
                DisplayName = "Point of Sale", 
                Icon = "\uE7BF", 
                Description = "Cash drawer, table layout, shifts, and tax settings",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "pos.cashdrawer", DisplayName = "Cash Drawer", Icon = "\uE8C7" },
                    new() { Key = "pos.tables", DisplayName = "Table Layout", Icon = "\uE8EA" },
                    new() { Key = "pos.shifts", DisplayName = "Shifts", Icon = "\uE823" },
                    new() { Key = "pos.tax", DisplayName = "Tax Settings", Icon = "\uE8AB" }
                }
            },
            new() { 
                Key = "inventory", 
                DisplayName = "Inventory", 
                Icon = "\uE8F1", 
                Description = "Stock thresholds, reorder settings, and vendor defaults",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "inventory.stock", DisplayName = "Stock Management", Icon = "\uE8F1" },
                    new() { Key = "inventory.reorder", DisplayName = "Reorder Settings", Icon = "\uE8C8" },
                    new() { Key = "inventory.vendors", DisplayName = "Vendor Defaults", Icon = "\uE8D4" }
                }
            },
            new() { 
                Key = "customers", 
                DisplayName = "Customers & Membership", 
                Icon = "\uE716", 
                Description = "Membership tiers, wallet settings, and loyalty programs",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "customers.membership", DisplayName = "Membership Tiers", Icon = "\uE8D4" },
                    new() { Key = "customers.wallet", DisplayName = "Wallet System", Icon = "\uE8C7" },
                    new() { Key = "customers.loyalty", DisplayName = "Loyalty Program", Icon = "\uE734" }
                }
            },
            new() { 
                Key = "payments", 
                DisplayName = "Payments", 
                Icon = "\uE8C7", 
                Description = "Payment methods, discounts, surcharges, and split payments",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "payments.methods", DisplayName = "Payment Methods", Icon = "\uE8C7" },
                    new() { Key = "payments.discounts", DisplayName = "Discounts", Icon = "\uE8AB" },
                    new() { Key = "payments.surcharges", DisplayName = "Surcharges", Icon = "\uE8C8" },
                    new() { Key = "payments.splits", DisplayName = "Split Payments", Icon = "\uE8C9" }
                }
            },
            new() { 
                Key = "printers", 
                DisplayName = "Printers & Devices", 
                Icon = "\uE749", 
                Description = "Receipt printers, kitchen printers, and device configuration",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "printers.designer", DisplayName = "Receipt Designer", Icon = "\uE8A5" },
                    new() { Key = "printers.receipt", DisplayName = "Receipt Printers", Icon = "\uE749" },
                    new() { Key = "printers.kitchen", DisplayName = "Kitchen Printers", Icon = "\uE8EA" },
                    new() { Key = "printers.devices", DisplayName = "Device Management", Icon = "\uE8B7" },
                    new() { Key = "printers.jobs", DisplayName = "Print Jobs", Icon = "\uE8F4" }
                }
            },
            new() { 
                Key = "notifications", 
                DisplayName = "Notifications", 
                Icon = "\uE7E7", 
                Description = "Email, SMS, push notifications, and alerts",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "notifications.email", DisplayName = "Email Settings", Icon = "\uE715" },
                    new() { Key = "notifications.sms", DisplayName = "SMS Settings", Icon = "\uE8BD" },
                    new() { Key = "notifications.push", DisplayName = "Push Notifications", Icon = "\uE7E7" },
                    new() { Key = "notifications.alerts", DisplayName = "Alert Settings", Icon = "\uE783" }
                }
            },
            new() { 
                Key = "security", 
                DisplayName = "Security & Roles", 
                Icon = "\uE72E", 
                Description = "RBAC, login policies, sessions, and audit settings",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "security.rbac", DisplayName = "Role Permissions", Icon = "\uE716" },
                    new() { Key = "security.login", DisplayName = "Login Policies", Icon = "\uE8AF" },
                    new() { Key = "security.sessions", DisplayName = "Session Management", Icon = "\uE823" },
                    new() { Key = "security.audit", DisplayName = "Audit Logging", Icon = "\uE8F4" }
                }
            },
            new() { 
                Key = "integrations", 
                DisplayName = "Integrations", 
                Icon = "\uE8C8", 
                Description = "Payment gateways, webhooks, CRM sync, and API endpoints",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "integrations.gateways", DisplayName = "Payment Gateways", Icon = "\uE8C7" },
                    new() { Key = "integrations.webhooks", DisplayName = "Webhooks", Icon = "\uE8C8" },
                    new() { Key = "integrations.crm", DisplayName = "CRM Sync", Icon = "\uE8D4" },
                    new() { Key = "integrations.api", DisplayName = "API Endpoints", Icon = "\uE968" }
                }
            },
            new() { 
                Key = "system", 
                DisplayName = "System", 
                Icon = "\uE713", 
                Description = "Logging, tracing, background jobs, and performance settings",
                Children = new ObservableCollection<SettingsTreeItem>
                {
                    new() { Key = "system.logging", DisplayName = "Logging", Icon = "\uE8F4" },
                    new() { Key = "system.tracing", DisplayName = "Tracing", Icon = "\uE8F5" },
                    new() { Key = "system.jobs", DisplayName = "Background Jobs", Icon = "\uE823" },
                    new() { Key = "system.performance", DisplayName = "Performance", Icon = "\uE8F6" }
                }
            }
        };

        // Populate TreeView
        foreach (var item in treeItems)
        {
            var treeViewItem = CreateTreeViewItem(item);
            SettingsTreeView.RootNodes.Add(treeViewItem);
        }
        
        // Populate _allCategories for search functionality
        _allCategories = treeItems.Select(t => new SettingsCategory 
        { 
            Key = t.Key, 
            DisplayName = t.DisplayName, 
            Description = t.Description, 
            Icon = t.Icon, 
            HasChanges = false 
        }).ToList();
        
        _filteredCategories = new List<SettingsCategory>(_allCategories);
        
        // Set default filter selection
        UpdateFilterButtonStyles();
        
        // Set default ComboBox selection to "All Categories"
        if (CategoryFilterComboBox != null)
        {
            CategoryFilterComboBox.SelectedIndex = 0; // "All Categories" is the first item
        }
    }

    private void UpdateFilterButtonStyles()
    {
        // No longer needed with ComboBox - this method is kept for compatibility
        // The ComboBox handles its own selection state
    }

    private void PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(_currentSearchQuery))
        {
            _filteredCategories = new List<SettingsCategory>(_allCategories);
        }
        else
        {
            var query = _currentSearchQuery.ToLower();
            _filteredCategories = _allCategories.Where(c => 
                c.DisplayName.ToLower().Contains(query) ||
                c.Description.ToLower().Contains(query) ||
                c.Key.ToLower().Contains(query)
            ).ToList();
        }
        
        ApplyFilters();
        UpdateSettingsTree();
        
        // Update search results text
        if (SearchResultsText != null)
        {
            if (_filteredCategories.Count == _allCategories.Count)
            {
                SearchResultsText.Visibility = Visibility.Collapsed;
            }
            else
            {
                SearchResultsText.Text = $"Found {_filteredCategories.Count} of {_allCategories.Count} categories";
                SearchResultsText.Visibility = Visibility.Visible;
            }
        }
    }

    private void ApplyFilters()
    {
        var filtered = _filteredCategories.AsEnumerable();
        
        // Apply category filter
        if (_currentFilter != "all")
        {
            filtered = filtered.Where(c => c.Key == _currentFilter);
        }
        
        // Apply changed filter
        if (_currentFilter == "changed")
        {
            filtered = filtered.Where(c => c.HasChanges);
        }
        
        _filteredCategories = filtered.ToList();
    }

    private void UpdateSettingsTree()
    {
        if (SettingsTreeView == null) return;
        
        SettingsTreeView.RootNodes.Clear();
        
        foreach (var category in _filteredCategories)
        {
            var treeItem = new SettingsTreeItem
            {
                Key = category.Key,
                DisplayName = category.DisplayName,
                Description = category.Description,
                Icon = category.Icon,
                HasChanges = category.HasChanges
            };
            
            var node = CreateTreeViewItem(treeItem);
            SettingsTreeView.RootNodes.Add(node);
        }
    }

    private TreeViewNode CreateTreeViewItem(SettingsTreeItem item)
    {
        var node = new TreeViewNode
        {
            Content = item,
            IsExpanded = false
        };

        if (item.Children?.Any() == true)
        {
            foreach (var child in item.Children)
            {
                var childNode = CreateTreeViewItem(child);
                node.Children.Add(childNode);
            }
        }

        return node;
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            LoadingRing.IsActive = true;
            StatusText.Text = "Loading settings...";

            await ViewModel.LoadSettingsAsync();
            
            HostKeyText.Text = $"Host: {ViewModel.HostKey}";
            UpdateConnectionStatus(true);
            StatusText.Text = "Settings loaded successfully";
            LastSavedText.Text = $"Last saved: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Failed to load settings";
            UpdateConnectionStatus(false);
            
            // Show error to user
            ValidationErrorTeachingTip.Subtitle = $"Error loading settings: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void SettingsTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.FirstOrDefault() is TreeViewNode node && 
            node.Content is SettingsTreeItem item)
        {
            NavigateToSettingsPage(item);
        }
    }

    private async void TreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && node.Content is SettingsTreeItem item)
        {
            CategoryTitleText.Text = item.DisplayName;
            CategoryDescriptionText.Text = item.Description ?? "";

            // Get the root category key
            var categoryKey = item.Key.Split('.')[0];
            
            if (_settingsPages.TryGetValue(categoryKey, out var pageType))
            {
                // Ensure settings are loaded before navigating
                if (ViewModel.IsLoading)
                {
                    StatusText.Text = "Loading settings, please wait...";
                    return;
                }

                // Create page instance and pass the specific subcategory if needed
                var page = Activator.CreateInstance(pageType) as Page;
                if (page is ISettingsSubPage subPage)
                {
                    subPage.SetSubCategory(item.Key);
                    var categorySettings = ViewModel.GetCategorySettings(categoryKey);
                    System.Diagnostics.Debug.WriteLine($"Setting {categoryKey} settings: {categorySettings?.GetType().Name}");
                    subPage.SetSettings(categorySettings);
                }
                
                SettingsContentFrame.Content = page;
            }
            else
            {
                // Show placeholder for unimplemented pages
                var placeholder = new TextBlock
                {
                    Text = $"Settings page for '{item.DisplayName}' is not yet implemented.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    Opacity = 0.7
                };
                SettingsContentFrame.Content = placeholder;
            }
        }
    }

    private void NavigateToSettingsPage(SettingsTreeItem item)
    {
        try
        {
            CategoryTitleText.Text = item.DisplayName;
            CategoryDescriptionText.Text = item.Description ?? "";

            // Get the root category key
            var categoryKey = item.Key.Split('.')[0];
            
            if (_settingsPages.TryGetValue(categoryKey, out var pageType))
            {
                // Create page instance and pass the specific subcategory if needed
                var page = Activator.CreateInstance(pageType) as Page;
                if (page is ISettingsSubPage subPage)
                {
                    subPage.SetSubCategory(item.Key);
                    subPage.SetSettings(ViewModel.GetCategorySettings(categoryKey));
                }
                
                SettingsContentFrame.Content = page;
            }
            else
            {
                // Show placeholder for unimplemented pages
                var placeholder = new TextBlock
                {
                    Text = $"Settings page for '{item.DisplayName}' is not yet implemented.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    Opacity = 0.7
                };
                SettingsContentFrame.Content = placeholder;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error navigating to settings page: {ex.Message}";
        }
    }

    private async void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingRing.IsActive = true;
            StatusText.Text = "Saving all settings...";

            // Collect current UI values from the active settings page
            CollectCurrentPageSettings();

            var success = await ViewModel.SaveAllSettingsAsync();
            
            if (success)
            {
                SaveSuccessTeachingTip.IsOpen = true;
                StatusText.Text = "All settings saved successfully";
                LastSavedText.Text = $"Last saved: {DateTime.Now:HH:mm:ss}";
            }
            else
            {
                ValidationErrorTeachingTip.Subtitle = "Failed to save settings. Please try again.";
                ValidationErrorTeachingTip.IsOpen = true;
                StatusText.Text = "Failed to save settings";
            }
        }
        catch (Exception ex)
        {
            ValidationErrorTeachingTip.Subtitle = $"Error saving settings: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
            StatusText.Text = "Error saving settings";
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private async void SaveCategory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingRing.IsActive = true;
            StatusText.Text = "Saving category...";

            // Collect current UI values from the active settings page
            CollectCurrentPageSettings();

            // Get current category from selected tree item
            var selectedNode = SettingsTreeView.SelectedNode;
            if (selectedNode?.Content is SettingsTreeItem item)
            {
                var categoryKey = item.Key.Split('.')[0];
                var success = await ViewModel.SaveCategoryAsync(categoryKey);
                
                if (success)
                {
                    SaveSuccessTeachingTip.Subtitle = $"Category '{item.DisplayName}' saved successfully.";
                    SaveSuccessTeachingTip.IsOpen = true;
                    StatusText.Text = "Category saved successfully";
                }
                else
                {
                    ValidationErrorTeachingTip.Subtitle = "Failed to save category. Please try again.";
                    ValidationErrorTeachingTip.IsOpen = true;
                    StatusText.Text = "Failed to save category";
                }
            }
        }
        catch (Exception ex)
        {
            ValidationErrorTeachingTip.Subtitle = $"Error saving category: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
            StatusText.Text = "Error saving category";
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Reset to Defaults",
            Content = "Are you sure you want to reset all settings to their default values? This action cannot be undone.",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            try
            {
                LoadingRing.IsActive = true;
                StatusText.Text = "Resetting to defaults...";

                var success = await ViewModel.ResetToDefaultsAsync();
                
                if (success)
                {
                    SaveSuccessTeachingTip.Subtitle = "Settings reset to defaults successfully.";
                    SaveSuccessTeachingTip.IsOpen = true;
                    StatusText.Text = "Settings reset successfully";
                    
                    // Refresh the current page
                    var selectedNode = SettingsTreeView.SelectedNode;
                    if (selectedNode?.Content is SettingsTreeItem item)
                    {
                        NavigateToSettingsPage(item);
                    }
                }
                else
                {
                    ValidationErrorTeachingTip.Subtitle = "Failed to reset settings. Please try again.";
                    ValidationErrorTeachingTip.IsOpen = true;
                    StatusText.Text = "Failed to reset settings";
                }
            }
            catch (Exception ex)
            {
                ValidationErrorTeachingTip.Subtitle = $"Error resetting settings: {ex.Message}";
                ValidationErrorTeachingTip.IsOpen = true;
                StatusText.Text = "Error resetting settings";
            }
            finally
            {
                LoadingRing.IsActive = false;
            }
        }
    }

    private async void ResetCategory_Click(object sender, RoutedEventArgs e)
    {
        var selectedNode = SettingsTreeView.SelectedNode;
        if (selectedNode?.Content is SettingsTreeItem item)
        {
            var dialog = new ContentDialog
            {
                Title = "Reset Category",
                Content = $"Are you sure you want to reset '{item.DisplayName}' settings to their default values?",
                PrimaryButtonText = "Reset",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    LoadingRing.IsActive = true;
                    StatusText.Text = "Resetting category...";

                    var categoryKey = item.Key.Split('.')[0];
                    var success = await ViewModel.ResetCategoryAsync(categoryKey);
                    
                    if (success)
                    {
                        SaveSuccessTeachingTip.Subtitle = $"Category '{item.DisplayName}' reset successfully.";
                        SaveSuccessTeachingTip.IsOpen = true;
                        StatusText.Text = "Category reset successfully";
                        
                        // Refresh the current page
                        NavigateToSettingsPage(item);
                    }
                    else
                    {
                        ValidationErrorTeachingTip.Subtitle = "Failed to reset category. Please try again.";
                        ValidationErrorTeachingTip.IsOpen = true;
                        StatusText.Text = "Failed to reset category";
                    }
                }
                catch (Exception ex)
                {
                    ValidationErrorTeachingTip.Subtitle = $"Error resetting category: {ex.Message}";
                    ValidationErrorTeachingTip.IsOpen = true;
                    StatusText.Text = "Error resetting category";
                }
                finally
                {
                    LoadingRing.IsActive = false;
                }
            }
        }
    }

    private async void TestConnections_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingRing.IsActive = true;
            StatusText.Text = "Testing connections...";

            var results = await ViewModel.TestConnectionsAsync();
            
            // Populate results list
            var resultItems = results.Select(r => new ConnectionTestResult
            {
                Name = r.Key,
                IsConnected = r.Value,
                Status = r.Value ? "Connected" : "Failed"
            }).ToList();

            ConnectionResultsList.ItemsSource = resultItems;
            
            // Show dialog
            await ConnectionTestDialog.ShowAsync();
            
            StatusText.Text = "Connection test completed";
        }
        catch (Exception ex)
        {
            ValidationErrorTeachingTip.Subtitle = $"Error testing connections: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
            StatusText.Text = "Error testing connections";
        }
        finally
        {
            LoadingRing.IsActive = false;
        }
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        ConnectionStatusDot.Fill = isConnected ? 
            new SolidColorBrush(Microsoft.UI.Colors.Green) : 
            new SolidColorBrush(Microsoft.UI.Colors.Red);
        ConnectionStatusText.Text = $"Connection: {(isConnected ? "Connected" : "Disconnected")}";
    }

    private void CollectCurrentPageSettings()
    {
        try
        {
            // Get the currently displayed settings page
            if (SettingsContentFrame.Content is PrinterSettingsPage printerPage)
            {
                // Collect current UI values and update the ViewModel
                var currentSettings = printerPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("printers", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected printer settings: DefaultPrinter={currentSettings.Receipt.DefaultPrinter}");
            }
            else if (SettingsContentFrame.Content is GeneralSettingsPage generalPage)
            {
                var currentSettings = generalPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("general", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected general settings: BusinessName={currentSettings.BusinessName}");
            }
            else if (SettingsContentFrame.Content is PosSettingsPage posPage)
            {
                var currentSettings = posPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("pos", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected POS settings: DefaultTaxRate={currentSettings.Tax.DefaultTaxRate}");
            }
            else if (SettingsContentFrame.Content is InventorySettingsPage inventoryPage)
            {
                var currentSettings = inventoryPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("inventory", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected Inventory settings: LowStockThreshold={currentSettings.Stock.LowStockThreshold}");
            }
            else if (SettingsContentFrame.Content is CustomersSettingsPage customersPage)
            {
                var currentSettings = customersPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("customers", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected Customers settings: EnableWalletSystem={currentSettings.Wallet.EnableWalletSystem}");
            }
            else if (SettingsContentFrame.Content is PaymentsSettingsPage paymentsPage)
            {
                var currentSettings = paymentsPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("payments", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected Payments settings: MaxDiscountPercentage={currentSettings.Discounts.MaxDiscountPercentage}");
            }
            else if (SettingsContentFrame.Content is NotificationsSettingsPage notificationsPage)
            {
                var currentSettings = notificationsPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("notifications", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected Notifications settings: EnableEmail={currentSettings.Email.EnableEmail}");
            }
            else if (SettingsContentFrame.Content is SecuritySettingsPage securityPage)
            {
                var currentSettings = securityPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("security", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected Security settings: EnforceRolePermissions={currentSettings.Rbac.EnforceRolePermissions}");
            }
            else if (SettingsContentFrame.Content is IntegrationsSettingsPage integrationsPage)
            {
                var currentSettings = integrationsPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("integrations", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected Integrations settings: EnableWebhooks={currentSettings.Webhooks.EnableWebhooks}");
            }
            else if (SettingsContentFrame.Content is SystemSettingsPage systemPage)
            {
                var currentSettings = systemPage.GetCurrentSettings();
                ViewModel.UpdateCategorySettings("system", currentSettings);
                System.Diagnostics.Debug.WriteLine($"Collected System settings: LogLevel={currentSettings.Logging.LogLevel}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error collecting current page settings: {ex.Message}");
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        await ImportExportDialog.ShowAsync();
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        await ImportExportDialog.ShowAsync();
    }

    private async void ExportToFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ImportExportStatusPanel.Visibility = Visibility.Visible;
            ImportExportProgressRing.IsActive = true;
            ImportExportStatusText.Text = "Exporting settings...";

            var fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            fileSavePicker.FileTypeChoices.Add("JSON Files", new List<string>() { ".json" });
            fileSavePicker.SuggestedFileName = $"magidesk-settings-{DateTime.Now:yyyyMMdd-HHmmss}.json";

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(fileSavePicker, hwnd);

            var file = await fileSavePicker.PickSaveFileAsync();
            if (file != null)
            {
                var settingsJson = await ViewModel.ExportSettingsToJsonAsync();
                await FileIO.WriteTextAsync(file, settingsJson);
                
                ImportExportStatusText.Text = "Settings exported successfully!";
                SaveSuccessTeachingTip.Subtitle = $"Settings exported to {file.Name}";
                SaveSuccessTeachingTip.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            ImportExportStatusText.Text = $"Export failed: {ex.Message}";
            ValidationErrorTeachingTip.Subtitle = $"Export failed: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
        }
        finally
        {
            ImportExportProgressRing.IsActive = false;
        }
    }

    private async void ExportToClipboard_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ImportExportStatusPanel.Visibility = Visibility.Visible;
            ImportExportProgressRing.IsActive = true;
            ImportExportStatusText.Text = "Exporting to clipboard...";

            var settingsJson = await ViewModel.ExportSettingsToJsonAsync();
            
            var dataPackage = new DataPackage();
            dataPackage.SetText(settingsJson);
            Clipboard.SetContent(dataPackage);
            
            ImportExportStatusText.Text = "Settings copied to clipboard!";
            SaveSuccessTeachingTip.Subtitle = "Settings copied to clipboard";
            SaveSuccessTeachingTip.IsOpen = true;
        }
        catch (Exception ex)
        {
            ImportExportStatusText.Text = $"Export failed: {ex.Message}";
            ValidationErrorTeachingTip.Subtitle = $"Export failed: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
        }
        finally
        {
            ImportExportProgressRing.IsActive = false;
        }
    }

    private async void ImportFromFile_Click(object sender, RoutedEventArgs e)
    {
        await FilePickerDialog.ShowAsync();
    }

    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            fileOpenPicker.FileTypeFilter.Add(".json");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(fileOpenPicker, hwnd);

            var file = await fileOpenPicker.PickSingleFileAsync();
            if (file != null)
            {
                FilePathTextBox.Text = file.Path;
            }
        }
        catch (Exception ex)
        {
            ValidationErrorTeachingTip.Subtitle = $"File selection failed: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
        }
    }

    private async void ImportSelectedFile_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
        {
            ValidationErrorTeachingTip.Subtitle = "Please select a file first";
            ValidationErrorTeachingTip.IsOpen = true;
            return;
        }

        try
        {
            ImportExportStatusPanel.Visibility = Visibility.Visible;
            ImportExportProgressRing.IsActive = true;
            ImportExportStatusText.Text = "Importing settings...";

            var success = await ViewModel.ImportSettingsFromFileAsync(FilePathTextBox.Text);
            
            if (success)
            {
                ImportExportStatusText.Text = "Settings imported successfully!";
                SaveSuccessTeachingTip.Subtitle = "Settings imported successfully";
                SaveSuccessTeachingTip.IsOpen = true;
                
                // Refresh the current page
                var selectedNode = SettingsTreeView.SelectedNode;
                if (selectedNode?.Content is SettingsTreeItem item)
                {
                    NavigateToSettingsPage(item);
                }
            }
            else
            {
                ImportExportStatusText.Text = "Import failed - invalid settings file";
                ValidationErrorTeachingTip.Subtitle = "Import failed - invalid settings file";
                ValidationErrorTeachingTip.IsOpen = true;
            }
        }
        catch (Exception ex)
        {
            ImportExportStatusText.Text = $"Import failed: {ex.Message}";
            ValidationErrorTeachingTip.Subtitle = $"Import failed: {ex.Message}";
            ValidationErrorTeachingTip.IsOpen = true;
        }
        finally
        {
            ImportExportProgressRing.IsActive = false;
        }
    }

    private void NavigateToCategory(string categoryKey)
    {
        if (_settingsPages.TryGetValue(categoryKey, out var pageType))
        {
            var page = Activator.CreateInstance(pageType) as Page;
            if (page != null)
            {
                SettingsContentFrame.Content = page;
            }
        }
    }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && item.Tag is string filter)
            {
                _currentFilter = filter;
                ApplyFilters();
                UpdateSettingsTree();
            }
        }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion != null)
        {
            var category = args.ChosenSuggestion as SettingsCategory;
            if (category != null)
            {
                NavigateToCategory(category.Key);
            }
        }
        else if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            _currentSearchQuery = args.QueryText;
            PerformSearch();
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var query = sender.Text?.ToLower() ?? "";
            
            if (string.IsNullOrWhiteSpace(query))
            {
                sender.ItemsSource = null;
                return;
            }

            // Filter categories based on search query
            var suggestions = _allCategories.Where(c => 
                c.DisplayName.ToLower().Contains(query) ||
                c.Description.ToLower().Contains(query) ||
                c.Key.ToLower().Contains(query)
            ).Take(5).ToList();

            sender.ItemsSource = suggestions;
        }
    }

    private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var category = args.SelectedItem as SettingsCategory;
        if (category != null)
        {
            NavigateToCategory(category.Key);
        }
    }

    private void UpdateSearchResults()
    {
        if (SearchResultsText != null)
        {
            if (_filteredCategories.Count == 0)
            {
                SearchResultsText.Text = "No settings found matching your search.";
            }
            else if (_filteredCategories.Count == _allCategories.Count)
            {
                SearchResultsText.Text = $"Showing all {_allCategories.Count} settings categories.";
            }
            else
            {
                SearchResultsText.Text = $"Found {_filteredCategories.Count} settings categories matching your search.";
            }
        }
    }
}

// Supporting classes
public class SettingsTreeItem
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Icon { get; set; } = "\uE713";
    public string? Description { get; set; }
    public bool HasChanges { get; set; } = false;
    public ObservableCollection<SettingsTreeItem>? Children { get; set; }
}

public class ConnectionTestResult
{
    public string Name { get; set; } = "";
    public bool IsConnected { get; set; }
    public string Status { get; set; } = "";
}

public interface ISettingsSubPage
{
    void SetSubCategory(string subCategoryKey);
    void SetSettings(object settings);
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }
        return false;
    }
}

public class BooleanToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red;
        }
        return Microsoft.UI.Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

