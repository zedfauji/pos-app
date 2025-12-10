using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class ReceiptSettingsViewModel : INotifyPropertyChanged
{
    private readonly ReceiptService _receiptService;
    private readonly SettingsApiService _settingsService;
    private readonly ILogger<ReceiptSettingsViewModel> _logger;
    private readonly IConfiguration _configuration;

    // Business Information
    private string _businessName = "MagiDesk Billiard Club";
    public string BusinessName 
    { 
        get => _businessName; 
        set 
        { 
            _businessName = value; 
            OnPropertyChanged(); 
        } 
    }

    private string _businessAddress = "123 Main Street, City, State 12345";
    public string BusinessAddress 
    { 
        get => _businessAddress; 
        set 
        { 
            _businessAddress = value; 
            OnPropertyChanged(); 
        } 
    }

    private string _businessPhone = "(555) 123-4567";
    public string BusinessPhone 
    { 
        get => _businessPhone; 
        set 
        { 
            _businessPhone = value; 
            OnPropertyChanged(); 
        } 
    }

    // Printer Settings
    public ObservableCollection<string> AvailablePrinters { get; } = new();
    private string? _selectedPrinter;
    public string? SelectedPrinter 
    { 
        get => _selectedPrinter; 
        set 
        { 
            _selectedPrinter = value; 
            OnPropertyChanged(); 
        } 
    }

    public ObservableCollection<string> ReceiptSizes { get; } = new() { "58mm", "80mm" };
    private string _selectedReceiptSize = "80mm";
    public string SelectedReceiptSize 
    { 
        get => _selectedReceiptSize; 
        set 
        { 
            _selectedReceiptSize = value; 
            OnPropertyChanged(); 
        } 
    }

    private bool _autoPrintOnPayment = true;
    public bool AutoPrintOnPayment 
    { 
        get => _autoPrintOnPayment; 
        set 
        { 
            _autoPrintOnPayment = value; 
            OnPropertyChanged(); 
        } 
    }

    private bool _previewBeforePrint = true;
    public bool PreviewBeforePrint 
    { 
        get => _previewBeforePrint; 
        set 
        { 
            _previewBeforePrint = value; 
            OnPropertyChanged(); 
        } 
    }

    private bool _printProForma = true;
    public bool PrintProForma 
    { 
        get => _printProForma; 
        set 
        { 
            _printProForma = value; 
            OnPropertyChanged(); 
        } 
    }

    private bool _printFinalReceipt = true;
    public bool PrintFinalReceipt 
    { 
        get => _printFinalReceipt; 
        set 
        { 
            _printFinalReceipt = value; 
            OnPropertyChanged(); 
        } 
    }

    private int _copiesForFinalReceipt = 2;
    public int CopiesForFinalReceipt 
    { 
        get => _copiesForFinalReceipt; 
        set 
        { 
            _copiesForFinalReceipt = value; 
            OnPropertyChanged(); 
        } 
    }

    // Test Receipt
    private UIElement? _testReceiptPreview;
    public UIElement? TestReceiptPreview 
    { 
        get => _testReceiptPreview; 
        private set 
        { 
            _testReceiptPreview = value; 
            OnPropertyChanged(); 
        } 
    }

    // Status
    private bool _isLoading = false;
    public bool IsLoading 
    { 
        get => _isLoading; 
        private set 
        { 
            _isLoading = value; 
            OnPropertyChanged(); 
        } 
    }

    public bool IsNotLoading => !IsLoading;

    private string? _errorMessage;
    public string? ErrorMessage 
    { 
        get => _errorMessage; 
        private set 
        { 
            _errorMessage = value; 
            OnPropertyChanged(); 
        } 
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private string? _successMessage;
    public string? SuccessMessage 
    { 
        get => _successMessage; 
        private set 
        { 
            _successMessage = value; 
            OnPropertyChanged(); 
        } 
    }

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

    public ReceiptSettingsViewModel(
        ReceiptService receiptService, 
        SettingsApiService settingsService, 
        ILogger<ReceiptSettingsViewModel>? logger, 
        IConfiguration? configuration)
    {
        _receiptService = receiptService;
        _settingsService = settingsService;
        _logger = logger ?? new NullLogger<ReceiptSettingsViewModel>();
        _configuration = configuration ?? new ConfigurationBuilder().Build();
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogInformation("Loading receipt settings");

            // Load settings from SettingsApi
            var settings = await _settingsService.GetAppSettingsAsync();
            if (settings?.ReceiptSettings != null)
            {
                BusinessName = settings.ReceiptSettings.BusinessName ?? BusinessName;
                BusinessAddress = settings.ReceiptSettings.BusinessAddress ?? BusinessAddress;
                BusinessPhone = settings.ReceiptSettings.BusinessPhone ?? BusinessPhone;
                SelectedPrinter = settings.ReceiptSettings.DefaultPrinter;
                SelectedReceiptSize = settings.ReceiptSettings.ReceiptSize ?? SelectedReceiptSize;
                AutoPrintOnPayment = settings.ReceiptSettings.AutoPrintOnPayment ?? AutoPrintOnPayment;
                PreviewBeforePrint = settings.ReceiptSettings.PreviewBeforePrint ?? PreviewBeforePrint;
                PrintProForma = settings.ReceiptSettings.PrintProForma ?? PrintProForma;
                PrintFinalReceipt = settings.ReceiptSettings.PrintFinalReceipt ?? PrintFinalReceipt;
                CopiesForFinalReceipt = settings.ReceiptSettings.CopiesForFinalReceipt ?? CopiesForFinalReceipt;
            }

            // Load available printers
            await LoadAvailablePrintersAsync();

            _logger.LogInformation("Receipt settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load receipt settings");
            ErrorMessage = $"Failed to load settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            _logger.LogInformation("Saving receipt settings");

            var receiptSettings = new SettingsApiService.ReceiptSettings
            {
                BusinessName = BusinessName,
                BusinessAddress = BusinessAddress,
                BusinessPhone = BusinessPhone,
                DefaultPrinter = SelectedPrinter,
                ReceiptSize = SelectedReceiptSize,
                AutoPrintOnPayment = AutoPrintOnPayment,
                PreviewBeforePrint = PreviewBeforePrint,
                PrintProForma = PrintProForma,
                PrintFinalReceipt = PrintFinalReceipt,
                CopiesForFinalReceipt = CopiesForFinalReceipt
            };

            var appSettings = new SettingsApiService.AppSettings
            {
                ReceiptSettings = receiptSettings
            };

            await _settingsService.SaveAppSettingsAsync(appSettings);

            SuccessMessage = "Settings saved successfully!";
            _logger.LogInformation("Receipt settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save receipt settings");
            ErrorMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            _logger.LogInformation("Resetting receipt settings to defaults");

            BusinessName = "MagiDesk Billiard Club";
            BusinessAddress = "123 Main Street, City, State 12345";
            BusinessPhone = "(555) 123-4567";
            SelectedPrinter = null;
            SelectedReceiptSize = "80mm";
            AutoPrintOnPayment = true;
            PreviewBeforePrint = true;
            PrintProForma = true;
            PrintFinalReceipt = true;
            CopiesForFinalReceipt = 2;

            SuccessMessage = "Settings reset to defaults!";
            _logger.LogInformation("Receipt settings reset to defaults");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset receipt settings");
            ErrorMessage = $"Failed to reset settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task GenerateTestReceiptAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogInformation("Generating test receipt");

            var testReceiptData = CreateTestReceiptData();
            var preview = await _receiptService.GenerateReceiptPreviewAsync(testReceiptData);
            TestReceiptPreview = preview;

            _logger.LogInformation("Test receipt generated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate test receipt");
            ErrorMessage = $"Failed to generate test receipt: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task PrintTestReceiptAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogInformation("Printing test receipt");

            var testReceiptData = CreateTestReceiptData();
            var parentWindow = GetParentWindow();
            if (parentWindow == null)
            {
                ErrorMessage = "Could not find parent window for printing";
                _logger.LogError("Parent window is null");
                return;
            }
            
            var success = await _receiptService.PrintReceiptAsync(testReceiptData, parentWindow, showPreview: PreviewBeforePrint);

            if (success)
            {
                SuccessMessage = "Test receipt printed successfully!";
                _logger.LogInformation("Test receipt printed successfully");
            }
            else
            {
                ErrorMessage = "Failed to print test receipt";
                _logger.LogWarning("Test receipt printing failed or was cancelled");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to print test receipt");
            ErrorMessage = $"Failed to print test receipt: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ExportTestReceiptPdfAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            _logger.LogInformation("Exporting test receipt as PDF");

            var testReceiptData = CreateTestReceiptData();
            var filePath = await _receiptService.ExportReceiptAsPdfAsync(testReceiptData);

            if (!string.IsNullOrEmpty(filePath))
            {
                SuccessMessage = $"Test receipt exported to: {filePath}";
                _logger.LogInformation("Test receipt exported as PDF: {FilePath}", filePath);
            }
            else
            {
                ErrorMessage = "PDF export cancelled";
                _logger.LogInformation("Test receipt PDF export cancelled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export test receipt as PDF");
            ErrorMessage = $"Failed to export test receipt: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAvailablePrintersAsync()
    {
        try
        {
            // In a real implementation, this would query the system for available printers
            // For now, we'll add some mock printers
            AvailablePrinters.Clear();
            AvailablePrinters.Add("Default Printer");
            AvailablePrinters.Add("Thermal Printer 58mm");
            AvailablePrinters.Add("Thermal Printer 80mm");
            AvailablePrinters.Add("PDF Printer");

            if (string.IsNullOrEmpty(SelectedPrinter) && AvailablePrinters.Count > 0)
            {
                SelectedPrinter = AvailablePrinters[0];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available printers");
        }
    }

    private ReceiptService.ReceiptData CreateTestReceiptData()
    {
        return new ReceiptService.ReceiptData
        {
            BusinessName = BusinessName,
            BusinessAddress = BusinessAddress,
            BusinessPhone = BusinessPhone,
            TableNumber = "Test Table 1",
            ServerName = "Test Server",
            StartTime = DateTime.Now.AddHours(-1),
            EndTime = DateTime.Now,
            Items = new List<ReceiptService.ReceiptItem>
            {
                new() { Name = "Billiard Session", Quantity = 1, UnitPrice = 25.00m, Subtotal = 25.00m },
                new() { Name = "Drinks", Quantity = 2, UnitPrice = 5.00m, Subtotal = 10.00m },
                new() { Name = "Snacks", Quantity = 1, UnitPrice = 8.00m, Subtotal = 8.00m }
            },
            Subtotal = 43.00m,
            DiscountAmount = 5.00m,
            TaxAmount = 3.04m,
            TotalAmount = 41.04m,
            BillId = "TEST-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            PaymentMethod = "Cash",
            AmountPaid = 50.00m,
            Change = 8.96m,
            IsProForma = false
        };
    }

    /// <summary>
    /// Get the parent window for printing
    /// </summary>
    private Window? GetParentWindow()
    {
        try
        {
            // Try to get the current window
            // Try to get the main window first (more reliable)
            var mainWindow = App.MainWindow;
            if (mainWindow != null)
            {
                return mainWindow;
            }
            
            // CRITICAL FIX: Remove Window.Current usage to prevent COM exceptions in WinUI 3 Desktop Apps
            // Window.Current is a Windows Runtime COM interop call that causes Marshal.ThrowExceptionForHR errors
            _logger.LogWarning("Could not find parent window for printing");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parent window");
            return null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
