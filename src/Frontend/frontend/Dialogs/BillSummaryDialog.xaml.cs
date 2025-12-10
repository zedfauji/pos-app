using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class BillSummaryDialog : ContentDialog
{
    private readonly BillResult _bill;
    private readonly ILogger<BillSummaryDialog>? _logger;

    public BillSummaryDialog(BillResult bill, ILogger<BillSummaryDialog>? logger = null)
    {
        this.InitializeComponent();
        _bill = bill ?? throw new ArgumentNullException(nameof(bill));
        _logger = logger;
        
        // Set the data context to this dialog for binding
        DataContext = this;
        
        // Subscribe to button click events
        PrimaryButtonClick += OnPrimaryButtonClick;
        SecondaryButtonClick += OnSecondaryButtonClick;
    }

    #region Properties for Data Binding

    public string BillId => _bill.BillId.ToString("N")[..8].ToUpper();
    public string StatusText => _bill.EndTime == default(DateTime) ? "OPEN" : "CLOSED";
    public string StatusColor => _bill.EndTime == default(DateTime) ? "#FF8C00" : "#107C10";
    public string TableLabel => _bill.TableLabel ?? "Unknown Table";
    public string ServerName => _bill.ServerName ?? "Unknown Server";
    public string StartTimeFormatted => _bill.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
    public string DurationText => GetDurationText();
    public string SubtotalFormatted => $"${CalculatedItemsCost:F2}";
    public string TaxFormatted => "$0.00"; // Tax not calculated separately in current system
    public string ServiceChargeFormatted => $"${_bill.TimeCost:F2}";
    public string TotalAmountFormatted => $"${CalculatedTotalAmount:F2}";
    
    // Calculate totals from actual items
    private decimal CalculatedItemsCost => _bill.Items?.Sum(item => item.price * item.quantity) ?? 0m;
    private decimal CalculatedTotalAmount => CalculatedItemsCost + _bill.TimeCost;

    public List<BillItemViewModel> Items => _bill.Items?.Select(item => new BillItemViewModel(item)).ToList() ?? new List<BillItemViewModel>();

    #endregion

    #region Event Handlers

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            _logger?.LogInformation("Printing pre-payment receipt for bill {BillId}", _bill.BillId);
            
            // Defer the closing to allow async operation
            var deferral = args.GetDeferral();
            
            try
            {
                await PrintPrePaymentReceipt();
                
                // Show success message
                await ShowSuccessDialog("Pre-payment receipt printed successfully!");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to print pre-payment receipt");
                await ShowErrorDialog($"Failed to print pre-payment receipt: {ex.Message}");
            }
            finally
            {
                deferral.Complete();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in primary button click");
            await ShowErrorDialog($"An error occurred: {ex.Message}");
        }
    }

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Just close the dialog
        _logger?.LogInformation("Bill summary dialog closed without printing");
    }

    #endregion

    #region Private Methods

    private string GetDurationText()
    {
        if (_bill.EndTime == default(DateTime))
        {
            var duration = DateTime.Now - _bill.StartTime;
            return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
        else
        {
            var duration = _bill.EndTime - _bill.StartTime;
            return $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    private async Task PrintPrePaymentReceipt()
    {
        if (App.ReceiptService == null)
        {
            throw new InvalidOperationException("Receipt service not available");
        }

        var migrationService = new ReceiptMigrationService(App.ReceiptService);
        var (paper, tax) = ReadPrinterConfig();
        
        // Generate pre-payment receipt (isProForma = true)
        await migrationService.PrintBillAsync(_bill, paper, tax, "Default Printer", isProForma: true);
    }

    private (int paperMm, decimal taxPercent) ReadPrinterConfig()
    {
        try
        {
            var userCfgPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "MagiDesk", "appsettings.user.json");
            
            var cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .Build();
                
            var paperStr = cfg["Printer:PaperWidthMm"] ?? "58";
            var taxStr = cfg["Printer:TaxPercent"] ?? "10";
            
            return (int.Parse(paperStr), decimal.Parse(taxStr));
        }
        catch
        {
            return (58, 10m); // Default values
        }
    }

    private async Task ShowSuccessDialog(string message)
    {
        // Prevent multiple dialogs from opening simultaneously
        if (_isDialogOpen) return;
        
        try
        {
            _isDialogOpen = true;
            var dialog = new ContentDialog()
            {
                Title = "Success",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show success dialog");
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private static bool _isDialogOpen = false;
    
    private async Task ShowErrorDialog(string message)
    {
        // Prevent multiple dialogs from opening simultaneously
        if (_isDialogOpen) return;
        
        try
        {
            _isDialogOpen = true;
            var dialog = new ContentDialog()
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show error dialog");
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    #endregion

    #region BillItemViewModel

    public class BillItemViewModel
    {
        public string Name { get; }
        public int Quantity { get; }
        public decimal UnitPrice { get; }
        public decimal TotalPrice { get; }
        public string UnitPriceFormatted { get; }
        public string TotalPriceFormatted { get; }

        public BillItemViewModel(ItemLine item)
        {
            Name = item.name ?? "Unknown Item";
            Quantity = item.quantity;
            UnitPrice = item.price;
            TotalPrice = Quantity * UnitPrice;
            UnitPriceFormatted = $"${UnitPrice:F2}";
            TotalPriceFormatted = $"${TotalPrice:F2}";
        }
    }

    #endregion
}
