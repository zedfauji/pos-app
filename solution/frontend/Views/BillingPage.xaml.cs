using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Converters;
using MagiDesk.Frontend.Dialogs;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Logging;
using System;

namespace MagiDesk.Frontend.Views;

public sealed partial class BillingPage : Page
{
    private BillingViewModel? _viewModel;
    private readonly TableRepository _tableRepository;

    public BillingPage()
    {
        this.InitializeComponent();
        _tableRepository = new TableRepository();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (_viewModel == null)
        {
            _viewModel = new BillingViewModel(_tableRepository);
            DataContext = _viewModel;
        }
        
        await _viewModel.LoadBillsAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _viewModel?.Dispose();
    }

    private Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
    {
        return (App.MainWindow?.Content as FrameworkElement)?.XamlRoot ?? this.XamlRoot;
    }

    private void BillsList_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Ensure the row under the pointer is selected so context menu actions have SelectedItem
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is BillItemViewModel bi)
        {
            BillsList.SelectedItem = bi;
        }
    }

    private async void ViewReceipt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var billItem = GetBillItemFromSender(sender);
            if (billItem == null) return;

            Log.Info($"Bill summary requested for bill {billItem.BillId}");
            
            var fullBill = await _viewModel?.GetBillDetailsAsync(billItem.BillId);
            if (fullBill == null)
            {
                await ShowErrorDialog("Failed to load bill details");
                return;
            }

            await ShowBillSummaryDialog(fullBill);
        }
        catch (Exception ex)
        {
            Log.Error("Bill summary failed", ex);
            await ShowErrorDialog($"Failed to show bill summary: {ex.Message}");
        }
    }

    private async void Reprint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var billItem = GetBillItemFromSender(sender);
            if (billItem == null) return;

            Log.Info($"Reprint requested for bill {billItem.BillId}");
            
            var fullBill = await _viewModel?.GetBillDetailsAsync(billItem.BillId);
            if (fullBill == null)
            {
                await ShowErrorDialog("Failed to load bill details");
                return;
            }

            await PrintBill(fullBill);
        }
        catch (Exception ex)
        {
            Log.Error("Reprint failed", ex);
            await ShowErrorDialog($"Failed to reprint receipt: {ex.Message}");
        }
    }

    private async void CloseBill_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var billItem = GetBillItemFromSender(sender);
            if (billItem == null) return;

            var result = await ShowConfirmationDialog(
                "Close Bill",
                $"Are you sure you want to close bill {billItem.ShortBillId}? This action cannot be undone.");

            if (result == ContentDialogResult.Primary)
            {
                var success = await _viewModel?.CloseBillAsync(billItem.BillId);
                if (!success)
                {
                    await ShowErrorDialog("Failed to close bill. Please ensure all payments are settled.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Close bill failed", ex);
            await ShowErrorDialog($"Failed to close bill: {ex.Message}");
        }
    }

    private BillItemViewModel? GetBillItemFromSender(object sender)
    {
        if (sender is Button button && button.Tag is BillItemViewModel billItem)
        {
            return billItem;
        }
        
        if (sender is FrameworkElement fe && fe.DataContext is BillItemViewModel billItem2)
        {
            return billItem2;
        }
        
        if (BillsList.SelectedItem is BillItemViewModel selectedBill)
        {
            return selectedBill;
        }
        
        return null;
    }

    private static bool _isBillSummaryDialogOpen = false;

    private async Task ShowBillSummaryDialog(BillResult bill)
    {
        // Prevent multiple dialogs from opening simultaneously
        if (_isBillSummaryDialogOpen) return;
        
        try
        {
            _isBillSummaryDialogOpen = true;
            var dialog = new BillSummaryDialog(bill);
            dialog.XamlRoot = this.XamlRoot;
            
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                Log.Info($"Pre-payment receipt printed for bill {bill.BillId}");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Bill summary dialog failed", ex);
            await ShowErrorDialog($"Failed to show bill summary: {ex.Message}");
        }
        finally
        {
            _isBillSummaryDialogOpen = false;
        }
    }

    private async Task ShowReceiptPreview(BillResult bill)
    {
        try
        {
            if (App.ReceiptService == null)
            {
                await ShowErrorDialog("Receipt service not available");
                return;
            }

            var migrationService = new ReceiptMigrationService(App.ReceiptService);
            var (paper, tax) = ReadPrinterConfig();
            var pdfPath = await migrationService.GenerateReceiptFromBillAsync(bill, paper, tax, false);
            
            await ShowInfoDialog(
                "Receipt Preview",
                $"Receipt generated successfully!\n\nPDF saved to:\n{pdfPath}\n\nYou can open this file to preview the receipt.");
        }
        catch (Exception ex)
        {
            Log.Error("PDF preview generation failed", ex);
            await ShowErrorDialog($"Failed to generate PDF preview: {ex.Message}");
        }
    }

    private async Task PrintBill(BillResult bill)
    {
        try
        {
            if (App.ReceiptService == null)
            {
                await ShowErrorDialog("Receipt service not available");
                return;
            }

            var migrationService = new ReceiptMigrationService(App.ReceiptService);
            var (paper, tax) = ReadPrinterConfig();
            await migrationService.PrintBillAsync(bill, paper, tax, "Default Printer", isProForma: false);
            
            await ShowInfoDialog("Print Success", "Receipt sent to printer successfully!");
        }
        catch (Exception ex)
        {
            Log.Error("Print failed", ex);
            await ShowErrorDialog($"Failed to print receipt: {ex.Message}");
        }
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
            var taxStr = cfg["Printer:TaxPercent"] ?? "0";
            
            int.TryParse(paperStr, out int paper);
            decimal.TryParse(taxStr, System.Globalization.NumberStyles.Number, 
                System.Globalization.CultureInfo.InvariantCulture, out decimal tax);
                
            return (paper, tax);
        }
        catch 
        { 
            return (58, 0m); 
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        await new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "Close",
            XamlRoot = GetXamlRoot()
        }.ShowAsync();
    }

    private async Task ShowInfoDialog(string title, string message)
    {
        await new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = GetXamlRoot()
        }.ShowAsync();
    }

    private async Task<ContentDialogResult> ShowConfirmationDialog(string title, string message)
    {
        return await new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = GetXamlRoot()
        }.ShowAsync();
    }
}