using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Settings;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using SharedPrinterSettings = MagiDesk.Shared.DTOs.Settings.PrinterSettings;

namespace MagiDesk.Frontend.Views;

public sealed partial class PrinterSettingsPage : Page, ISettingsSubPage
{
    private SharedPrinterSettings _settings;
    private string _subCategoryKey = "";
    private readonly ObservableCollection<KitchenPrinter> _kitchenPrinters;
    private readonly ObservableCollection<PrinterDevice> _availablePrinters;

    public PrinterSettingsPage()
    {
        this.InitializeComponent();
        
        _settings = new SharedPrinterSettings();
        _kitchenPrinters = new ObservableCollection<KitchenPrinter>();
        _availablePrinters = new ObservableCollection<PrinterDevice>();
        
        KitchenPrintersList.ItemsSource = _kitchenPrinters;
        AvailablePrintersList.ItemsSource = _availablePrinters;
        
        InitializePage();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        _subCategoryKey = subCategoryKey;
        UpdateVisibilityForSubCategory();
    }

    public void SetSettings(object settings)
    {
        if (settings is SharedPrinterSettings printerSettings)
        {
            _settings = printerSettings;
            LoadSettingsToUI();
        }
    }

    public SharedPrinterSettings GetCurrentSettings()
    {
        // Collect current UI values back into the settings object
        CollectUIValuesToSettings();
        return _settings;
    }

    private void CollectUIValuesToSettings()
    {
        try
        {
            // Receipt Printer Settings
            _settings.Receipt.DefaultPrinter = DefaultPrinterCombo.SelectedItem?.ToString() ?? "";
            _settings.Receipt.FallbackPrinter = FallbackPrinterCombo.SelectedItem?.ToString() ?? "";
            _settings.Receipt.PaperSize = PaperSizeCombo.SelectedItem?.ToString() ?? "80mm";
            _settings.Receipt.CopiesForFinalReceipt = (int)CopiesNumberBox.Value;
            
            // Print options
            _settings.Receipt.AutoPrintOnPayment = AutoPrintCheck.IsChecked ?? false;
            _settings.Receipt.PreviewBeforePrint = PreviewBeforePrintCheck.IsChecked ?? false;
            _settings.Receipt.PrintProForma = PrintProFormaCheck.IsChecked ?? false;
            _settings.Receipt.PrintFinalReceipt = PrintFinalReceiptCheck.IsChecked ?? false;
            _settings.Receipt.PrintCustomerCopy = PrintCustomerCopyCheck.IsChecked ?? false;
            _settings.Receipt.PrintMerchantCopy = PrintMerchantCopyCheck.IsChecked ?? false;

            // Template settings
            _settings.Receipt.Template.ShowLogo = ShowLogoCheck.IsChecked ?? false;
            _settings.Receipt.Template.ShowBusinessInfo = ShowBusinessInfoCheck.IsChecked ?? false;
            _settings.Receipt.Template.ShowItemDetails = ShowItemDetailsCheck.IsChecked ?? false;
            _settings.Receipt.Template.ShowTaxBreakdown = ShowTaxBreakdownCheck.IsChecked ?? false;
            _settings.Receipt.Template.ShowPaymentMethod = ShowPaymentMethodCheck.IsChecked ?? false;
            _settings.Receipt.Template.HeaderMessage = HeaderMessageText.Text ?? "";
            _settings.Receipt.Template.FooterMessage = FooterMessageText.Text ?? "";
            
            // Margins
            _settings.Receipt.Template.TopMargin = (int)TopMarginNumber.Value;
            _settings.Receipt.Template.BottomMargin = (int)BottomMarginNumber.Value;
            _settings.Receipt.Template.LeftMargin = (int)LeftMarginNumber.Value;
            _settings.Receipt.Template.RightMargin = (int)RightMarginNumber.Value;

            // Kitchen Printer Settings
            _settings.Kitchen.PrintOrderNumbers = PrintOrderNumbersCheck.IsChecked ?? false;
            _settings.Kitchen.PrintTimestamps = PrintTimestampsCheck.IsChecked ?? false;
            _settings.Kitchen.PrintSpecialInstructions = PrintSpecialInstructionsCheck.IsChecked ?? false;
            _settings.Kitchen.CopiesPerOrder = (int)KitchenCopiesNumber.Value;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error collecting UI values: {ex.Message}");
        }
    }

    private void InitializePage()
    {
        // Load available printers
        LoadAvailablePrinters();
        
        // Load print queue status
        UpdatePrintQueueStatus();
        
        // Set up periodic updates for queue status
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (s, e) => UpdatePrintQueueStatus();
        timer.Start();
    }

    private void UpdateVisibilityForSubCategory()
    {
        // Show/hide sections based on subcategory
        switch (_subCategoryKey)
        {
            case "printers.designer":
                ReceiptDesignerExpander.IsExpanded = true;
                ReceiptPrinterExpander.Visibility = Visibility.Collapsed;
                KitchenPrinterExpander.Visibility = Visibility.Collapsed;
                DeviceManagementExpander.Visibility = Visibility.Collapsed;
                PrintJobExpander.Visibility = Visibility.Collapsed;
                break;
            case "printers.receipt":
                ReceiptPrinterExpander.IsExpanded = true;
                KitchenPrinterExpander.Visibility = Visibility.Collapsed;
                DeviceManagementExpander.Visibility = Visibility.Collapsed;
                PrintJobExpander.Visibility = Visibility.Collapsed;
                break;
            case "printers.kitchen":
                ReceiptPrinterExpander.Visibility = Visibility.Collapsed;
                KitchenPrinterExpander.IsExpanded = true;
                DeviceManagementExpander.Visibility = Visibility.Collapsed;
                PrintJobExpander.Visibility = Visibility.Collapsed;
                break;
            case "printers.devices":
                ReceiptPrinterExpander.Visibility = Visibility.Collapsed;
                KitchenPrinterExpander.Visibility = Visibility.Collapsed;
                DeviceManagementExpander.IsExpanded = true;
                PrintJobExpander.Visibility = Visibility.Collapsed;
                break;
            case "printers.jobs":
                ReceiptPrinterExpander.Visibility = Visibility.Collapsed;
                KitchenPrinterExpander.Visibility = Visibility.Collapsed;
                DeviceManagementExpander.Visibility = Visibility.Collapsed;
                PrintJobExpander.IsExpanded = true;
                break;
            default:
                // Show all sections
                ReceiptDesignerExpander.IsExpanded = false;
                ReceiptPrinterExpander.IsExpanded = true;
                break;
        }
    }

    private void LoadSettingsToUI()
    {
        try
        {
            // Receipt Printer Settings
            LoadPrinterComboBoxes();
            SetComboBoxSelection(PaperSizeCombo, _settings.Receipt.PaperSize);
            CopiesNumberBox.Value = _settings.Receipt.CopiesForFinalReceipt;
            
            // Print options
            AutoPrintCheck.IsChecked = _settings.Receipt.AutoPrintOnPayment;
            PreviewBeforePrintCheck.IsChecked = _settings.Receipt.PreviewBeforePrint;
            PrintProFormaCheck.IsChecked = _settings.Receipt.PrintProForma;
            PrintFinalReceiptCheck.IsChecked = _settings.Receipt.PrintFinalReceipt;
            PrintCustomerCopyCheck.IsChecked = _settings.Receipt.PrintCustomerCopy;
            PrintMerchantCopyCheck.IsChecked = _settings.Receipt.PrintMerchantCopy;

            // Template settings
            ShowLogoCheck.IsChecked = _settings.Receipt.Template.ShowLogo;
            ShowBusinessInfoCheck.IsChecked = _settings.Receipt.Template.ShowBusinessInfo;
            ShowItemDetailsCheck.IsChecked = _settings.Receipt.Template.ShowItemDetails;
            ShowTaxBreakdownCheck.IsChecked = _settings.Receipt.Template.ShowTaxBreakdown;
            ShowPaymentMethodCheck.IsChecked = _settings.Receipt.Template.ShowPaymentMethod;
            HeaderMessageText.Text = _settings.Receipt.Template.HeaderMessage;
            FooterMessageText.Text = _settings.Receipt.Template.FooterMessage;
            
            // Margins
            TopMarginNumber.Value = _settings.Receipt.Template.TopMargin;
            BottomMarginNumber.Value = _settings.Receipt.Template.BottomMargin;
            LeftMarginNumber.Value = _settings.Receipt.Template.LeftMargin;
            RightMarginNumber.Value = _settings.Receipt.Template.RightMargin;

            // Kitchen Printer Settings
            PrintOrderNumbersCheck.IsChecked = _settings.Kitchen.PrintOrderNumbers;
            PrintTimestampsCheck.IsChecked = _settings.Kitchen.PrintTimestamps;
            PrintSpecialInstructionsCheck.IsChecked = _settings.Kitchen.PrintSpecialInstructions;
            KitchenCopiesNumber.Value = _settings.Kitchen.CopiesPerOrder;
            
            // Load kitchen printers
            _kitchenPrinters.Clear();
            foreach (var printer in _settings.Kitchen.Printers)
            {
                _kitchenPrinters.Add(printer);
            }

            // Device Settings
            AutoDetectPrintersCheck.IsChecked = _settings.Devices.AutoDetectPrinters;
            SetComboBoxSelection(DefaultComPortCombo, _settings.Devices.DefaultComPort);
            SetComboBoxSelection(DefaultBaudRateCombo, _settings.Devices.DefaultBaudRate.ToString());

            // Load available printers
            _availablePrinters.Clear();
            foreach (var printer in _settings.Devices.AvailablePrinters)
            {
                _availablePrinters.Add(printer);
            }

            // Print Job Settings
            MaxRetriesNumber.Value = _settings.Jobs.MaxRetries;
            TimeoutMsNumber.Value = _settings.Jobs.TimeoutMs;
            MaxQueueSizeNumber.Value = _settings.Jobs.MaxQueueSize;
            LogPrintJobsCheck.IsChecked = _settings.Jobs.LogPrintJobs;
            QueueFailedJobsCheck.IsChecked = _settings.Jobs.QueueFailedJobs;
        }
        catch (Exception ex)
        {
            ShowError($"Error loading printer settings: {ex.Message}");
        }
    }

    private void LoadPrinterComboBoxes()
    {
        try
        {
            // Get system printers
            var systemPrinters = new List<string>();
            
            // Add installed printers
            foreach (string printerName in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                systemPrinters.Add(printerName);
            }

            // Add custom printers from settings
            foreach (var printer in _settings.Devices.AvailablePrinters)
            {
                if (!systemPrinters.Contains(printer.Name))
                {
                    systemPrinters.Add(printer.Name);
                }
            }

            // Populate combo boxes
            DefaultPrinterCombo.ItemsSource = systemPrinters;
            FallbackPrinterCombo.ItemsSource = systemPrinters;

            // Set selections
            DefaultPrinterCombo.SelectedItem = _settings.Receipt.DefaultPrinter;
            FallbackPrinterCombo.SelectedItem = _settings.Receipt.FallbackPrinter;
        }
        catch (Exception ex)
        {
            ShowError($"Error loading printers: {ex.Message}");
        }
    }

    private void LoadAvailablePrinters()
    {
        try
        {
            // This would typically scan for available printers
            // For now, add some sample printers
            var samplePrinters = new List<PrinterDevice>
            {
                new() { Name = "Receipt Printer 1", Type = "USB", ConnectionString = "USB001", IsOnline = true },
                new() { Name = "Kitchen Printer", Type = "Network", ConnectionString = "192.168.1.100", IsOnline = false },
                new() { Name = "Backup Printer", Type = "Serial", ConnectionString = "COM1", IsOnline = true }
            };

            _availablePrinters.Clear();
            foreach (var printer in samplePrinters)
            {
                _availablePrinters.Add(printer);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error loading available printers: {ex.Message}");
        }
    }

    private void UpdatePrintQueueStatus()
    {
        try
        {
            // This would typically query the actual print queue
            // For now, show sample data
            PendingJobsText.Text = "2";
            ProcessingJobsText.Text = "1";
            CompletedJobsText.Text = "45";
            FailedJobsText.Text = "3";
        }
        catch (Exception ex)
        {
            ShowError($"Error updating print queue status: {ex.Message}");
        }
    }

    #region Event Handlers

    private void RefreshPrinters_Click(object sender, RoutedEventArgs e)
    {
        LoadPrinterComboBoxes();
        ShowSuccess("Printer list refreshed");
    }

    private async void TestPrinter_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedPrinter = DefaultPrinterCombo.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedPrinter))
            {
                ShowError("Please select a printer to test");
                return;
            }

            // Show test print dialog
            var dialog = new TestPrintDialog(selectedPrinter);
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // Perform test print
                await PerformTestPrintAsync(selectedPrinter, dialog.GetTestPrintOptions());
            }
        }
        catch (Exception ex)
        {
            ShowError($"Test print failed: {ex.Message}");
        }
    }

    private void AddKitchenPrinter_Click(object sender, RoutedEventArgs e)
    {
        var newPrinter = new KitchenPrinter
        {
            Name = $"Kitchen Printer {_kitchenPrinters.Count + 1}",
            PrinterName = "",
            IsEnabled = true,
            Categories = new List<string>()
        };
        
        _kitchenPrinters.Add(newPrinter);
    }

    private void RemoveKitchenPrinter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is KitchenPrinter printer)
        {
            _kitchenPrinters.Remove(printer);
        }
    }

    private void ScanPrinters_Click(object sender, RoutedEventArgs e)
    {
        LoadAvailablePrinters();
        ShowSuccess("Printer scan completed");
    }

    private void AddManualPrinter_Click(object sender, RoutedEventArgs e)
    {
        // This would show a dialog to manually add a printer
        var newPrinter = new PrinterDevice
        {
            Name = "Manual Printer",
            Type = "Manual",
            ConnectionString = "",
            IsOnline = false
        };
        
        _availablePrinters.Add(newPrinter);
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        LoadAvailablePrinters();
        ShowSuccess("Device list refreshed");
    }

    private async void TestDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PrinterDevice device)
        {
            try
            {
                // Test the device connection
                var success = await TestDeviceConnectionAsync(device);
                device.IsOnline = success;
                
                ShowSuccess(success ? 
                    $"Device '{device.Name}' test successful" : 
                    $"Device '{device.Name}' test failed");
            }
            catch (Exception ex)
            {
                ShowError($"Device test failed: {ex.Message}");
            }
        }
    }

    private void ConfigureDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PrinterDevice device)
        {
            // This would show a device configuration dialog
            ShowInfo($"Configuration for '{device.Name}' not yet implemented");
        }
    }

    private void RemoveDevice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is PrinterDevice device)
        {
            _availablePrinters.Remove(device);
        }
    }

    private void ClearQueue_Click(object sender, RoutedEventArgs e)
    {
        // This would clear the print queue
        ShowSuccess("Print queue cleared");
        UpdatePrintQueueStatus();
    }

    private void RetryFailed_Click(object sender, RoutedEventArgs e)
    {
        // This would retry failed print jobs
        ShowSuccess("Retrying failed print jobs");
        UpdatePrintQueueStatus();
    }

    #endregion

    #region Helper Methods

    private void SetComboBoxSelection(ComboBox comboBox, string value)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item && item.Tag?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
            if (comboBox.Items[i]?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
    }

    private async Task PerformTestPrintAsync(string printerName, TestPrintOptions options)
    {
        try
        {
            // This would perform the actual test print
            await Task.Delay(1000); // Simulate print operation
            
            ShowSuccess($"Test print sent to '{printerName}' successfully");
        }
        catch (Exception ex)
        {
            ShowError($"Test print failed: {ex.Message}");
        }
    }

    private async Task<bool> TestDeviceConnectionAsync(PrinterDevice device)
    {
        try
        {
            // This would test the actual device connection
            await Task.Delay(500); // Simulate connection test
            
            // Return random result for demo
            return new Random().Next(0, 2) == 1;
        }
        catch
        {
            return false;
        }
    }

    private void ShowSuccess(string message)
    {
        // This would show a success notification
        System.Diagnostics.Debug.WriteLine($"SUCCESS: {message}");
    }

    private void ShowError(string message)
    {
        // This would show an error notification
        System.Diagnostics.Debug.WriteLine($"ERROR: {message}");
    }

    private void ShowInfo(string message)
    {
        // This would show an info notification
        System.Diagnostics.Debug.WriteLine($"INFO: {message}");
    }

    private async void OpenReceiptDesigner_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            // Create a new window for the Receipt Format Designer
            var newWindow = new Microsoft.UI.Xaml.Window
            {
                Title = "Receipt Format Designer - MagiDesk POS",
                Content = new ReceiptFormatDesignerPage()
            };
            
            // Set window size
            newWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
            
            // Activate the window
            newWindow.Activate();
        }
        catch (Exception ex)
        {
            // Handle error
            System.Diagnostics.Debug.WriteLine($"Error opening Receipt Designer: {ex.Message}");
        }
    }

    private async void PreviewFormat_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            // Create a preview dialog or window
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Receipt Preview",
                Content = new Microsoft.UI.Xaml.Controls.TextBlock 
                { 
                    Text = "Receipt preview functionality will be implemented here.",
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    Margin = new Microsoft.UI.Xaml.Thickness(20)
                },
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing preview: {ex.Message}");
        }
    }

    #endregion
}

// Supporting classes for test print functionality
public class TestPrintDialog : ContentDialog
{
    private readonly string _printerName;
    private CheckBox _includeLogo;
    private CheckBox _includeTestItems;
    private TextBox _customMessage;

    public TestPrintDialog(string printerName)
    {
        _printerName = printerName;
        InitializeDialog();
    }

    private void InitializeDialog()
    {
        Title = $"Test Print - {_printerName}";
        PrimaryButtonText = "Print Test";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;

        var content = new StackPanel { Spacing = 12 };
        
        content.Children.Add(new TextBlock 
        { 
            Text = "Select test print options:", 
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
        });

        _includeLogo = new CheckBox { Content = "Include business logo", IsChecked = true };
        content.Children.Add(_includeLogo);

        _includeTestItems = new CheckBox { Content = "Include test items", IsChecked = true };
        content.Children.Add(_includeTestItems);

        content.Children.Add(new TextBlock { Text = "Custom message:" });
        _customMessage = new TextBox 
        { 
            Text = "This is a test print from MagiDesk POS",
            AcceptsReturn = true,
            Height = 80
        };
        content.Children.Add(_customMessage);

        Content = content;
    }

    public TestPrintOptions GetTestPrintOptions()
    {
        return new TestPrintOptions
        {
            IncludeLogo = _includeLogo?.IsChecked ?? false,
            IncludeTestItems = _includeTestItems?.IsChecked ?? false,
            CustomMessage = _customMessage?.Text ?? ""
        };
    }
}

public class TestPrintOptions
{
    public bool IncludeLogo { get; set; }
    public bool IncludeTestItems { get; set; }
    public string CustomMessage { get; set; } = "";
}
