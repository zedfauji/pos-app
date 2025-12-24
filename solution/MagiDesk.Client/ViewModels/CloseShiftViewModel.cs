using CommunityToolkit.Mvvm.ComponentModel;
using MagiDesk.Shared.DTOs.Shifts;
using MagiDesk.Shared.DTOs.Reporting;

namespace MagiDesk.Client.ViewModels;

public partial class CloseShiftViewModel : ObservableObject
{
    private readonly ShiftDto _currentShift;

    [ObservableProperty] private double declaredCash;
    [ObservableProperty] private string note = ""; // Default empty string to avoid null
    [ObservableProperty] private double expectedCash; // Could be calculated if reporting API was integrated

    [ObservableProperty] private double difference;
    [ObservableProperty] private bool isShort;
    [ObservableProperty] private bool isOver;

    // Report Data
    [ObservableProperty] private double totalSales;
    [ObservableProperty] private double totalCashSales;
    [ObservableProperty] private double totalCardSales;

    public CloseShiftViewModel(ShiftDto currentShift, ZReportDto? report = null)
    {
        _currentShift = currentShift;
        
        if (report != null)
        {
            TotalSales = (double)report.TotalSales;
            TotalCashSales = (double)report.TotalCash;
            TotalCardSales = (double)report.TotalCard;
        }

        ExpectedCash = (double)currentShift.StartingCash + TotalCashSales;
    }

    partial void OnDeclaredCashChanged(double value)
    {
        Difference = value - ExpectedCash;
        IsShort = Difference < 0;
        IsOver = Difference > 0;
    }
}
