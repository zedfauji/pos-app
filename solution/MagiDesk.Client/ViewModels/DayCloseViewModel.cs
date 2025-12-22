using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Reporting;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels;

public partial class DayCloseViewModel : ObservableObject
{
    private readonly IReportingApi _reportingApi;
    private readonly IPrinterService _printerService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready to close day.";

    public DayCloseViewModel(IReportingApi reportingApi, IPrinterService printerService, IDialogService dialogService)
    {
        _reportingApi = reportingApi;
        _printerService = printerService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task PrintZReportAsync()
    {
        IsLoading = true;
        StatusMessage = "Fetching report...";
        try
        {
            var report = await _reportingApi.GetZReportAsync();
            StatusMessage = "Printing...";
            
            // Format for Thermal Printer
            var sb = new StringBuilder();
            sb.AppendLine("      MAGIDESK POS      ");
            sb.AppendLine("      Z-REPORT (DAY END)     ");
            sb.AppendLine("================================");
            sb.AppendLine($"Date: {report.StartTime:yyyy-MM-dd}");
            sb.AppendLine($"Generated: {report.GeneratedAt:HH:mm}");
            sb.AppendLine($"By: {report.GeneratedBy}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Total Orders: {report.TotalOrders}");
            sb.AppendLine($"CASH:       {report.TotalCash:C}");
            sb.AppendLine($"CARD:       {report.TotalCard:C}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"TOTAL SALES: {report.TotalSales:C}");
            sb.AppendLine("================================");
            sb.AppendLine("      END OF REPORT     ");
            sb.AppendLine(" ");
            
            await _printerService.PrintTextAsync(sb.ToString());
            StatusMessage = "Report printed successfully.";
            await _dialogService.ShowMessageAsync("Success", "Z-Report Printed.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print Z-Report");
            StatusMessage = "Error: " + ex.Message;
            await _dialogService.ShowMessageAsync("Error", "Failed to print report: " + ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
