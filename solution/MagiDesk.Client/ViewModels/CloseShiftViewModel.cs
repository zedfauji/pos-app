using CommunityToolkit.Mvvm.ComponentModel;
using MagiDesk.Shared.DTOs.Shifts;

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

    public CloseShiftViewModel(ShiftDto currentShift)
    {
        _currentShift = currentShift;
        // In a real app, we'd query Expected Cash here. 
        // For now, assume Expected = Starting + Sales (Zero for now as report API not fully linked to this VM)
        ExpectedCash = (double)currentShift.StartingCash; 
    }

    partial void OnDeclaredCashChanged(double value)
    {
        Difference = value - ExpectedCash;
        IsShort = Difference < 0;
        IsOver = Difference > 0;
    }
}
