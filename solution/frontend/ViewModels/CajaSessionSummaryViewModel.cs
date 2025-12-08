using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class CajaSessionSummaryViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private readonly CajaService.CajaSessionSummaryDto _session;

    public CajaSessionSummaryViewModel(CajaService.CajaSessionSummaryDto session)
    {
        _session = session;
    }

    public Guid Id => _session.Id;
    public DateTimeOffset OpenedAt => _session.OpenedAt;
    public DateTimeOffset? ClosedAt => _session.ClosedAt;
    public decimal OpeningAmount => _session.OpeningAmount;
    public decimal? ClosingAmount => _session.ClosingAmount;
    public decimal? SystemCalculatedTotal => _session.SystemCalculatedTotal;
    public decimal? Difference => _session.Difference;
    public string Status => _session.Status;
    public string OpenedByUserId => _session.OpenedByUserId;
    public string? ClosedByUserId => _session.ClosedByUserId;

    // Formatted properties
    public string OpenedAtFormatted => $"Abierta: {_session.OpenedAt:g}";
    public string OpeningAmountFormatted => $"${_session.OpeningAmount:F2}";
    public string ClosingAmountFormatted => _session.ClosingAmount.HasValue ? $"${_session.ClosingAmount.Value:F2}" : "";

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}
