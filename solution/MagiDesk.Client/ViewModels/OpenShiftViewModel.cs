using CommunityToolkit.Mvvm.ComponentModel;

namespace MagiDesk.Client.ViewModels;

public partial class OpenShiftViewModel : ObservableObject
{
    [ObservableProperty] private double startingCash;
    [ObservableProperty] private string note = ""; // Default empty string to avoid null
}
