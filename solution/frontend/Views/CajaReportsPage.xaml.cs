using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace MagiDesk.Frontend.Views;

public sealed partial class CajaReportsPage : Page
{
    public CajaReportViewModel ViewModel { get; }
    private Guid _sessionId;

    public CajaReportsPage()
    {
        this.InitializeComponent();
        ViewModel = new CajaReportViewModel();
        DataContext = ViewModel;
    }

    public CajaReportsPage(Guid sessionId) : this()
    {
        _sessionId = sessionId;
        Loaded += async (s, e) => await LoadReportAsync();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is Guid sessionId)
        {
            _sessionId = sessionId;
            LoadReportAsync();
        }
    }

    private async System.Threading.Tasks.Task LoadReportAsync()
    {
        if (_sessionId == Guid.Empty) return;
        await ViewModel.LoadReportAsync(_sessionId);
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement PDF export
        var dialog = new ContentDialog
        {
            Title = "Exportar PDF",
            Content = "Funcionalidad de exportación PDF próximamente disponible.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        _ = dialog.ShowAsync();
    }

    private void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement CSV export
        var dialog = new ContentDialog
        {
            Title = "Exportar CSV",
            Content = "Funcionalidad de exportación CSV próximamente disponible.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        _ = dialog.ShowAsync();
    }
}

public sealed class CajaReportViewModel : System.ComponentModel.INotifyPropertyChanged
{
    private readonly CajaService? _cajaService;
    private CajaService.CajaReportDto? _report;
    private bool _isLoading = false;

    public CajaReportViewModel()
    {
        _cajaService = App.CajaService;
    }

    public CajaService.CajaReportDto? Report
    {
        get => _report;
        set
        {
            _report = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OpenedAtFormatted));
            OnPropertyChanged(nameof(ClosedAtFormatted));
            OnPropertyChanged(nameof(OpeningAmountFormatted));
            OnPropertyChanged(nameof(ClosingAmountFormatted));
            OnPropertyChanged(nameof(SystemTotalFormatted));
            OnPropertyChanged(nameof(DifferenceFormatted));
            OnPropertyChanged(nameof(SalesTotalFormatted));
            OnPropertyChanged(nameof(RefundsTotalFormatted));
            OnPropertyChanged(nameof(TipsTotalFormatted));
            OnPropertyChanged(nameof(DepositsTotalFormatted));
            OnPropertyChanged(nameof(WithdrawalsTotalFormatted));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    // Formatted properties for display
    public string OpenedAtFormatted => _report?.OpenedAt.ToString("g") ?? "";
    public string ClosedAtFormatted => _report?.ClosedAt?.ToString("g") ?? "";
    public string OpeningAmountFormatted => _report != null ? $"${_report.OpeningAmount:F2}" : "$0.00";
    public string ClosingAmountFormatted => _report?.ClosingAmount != null ? $"${_report.ClosingAmount.Value:F2}" : "";
    public string SystemTotalFormatted => _report != null ? $"${_report.SystemCalculatedTotal:F2}" : "$0.00";
    public string DifferenceFormatted => _report?.Difference != null ? $"${_report.Difference.Value:F2}" : "";
    public string SalesTotalFormatted => _report != null ? $"${_report.SalesTotal:F2}" : "$0.00";
    public string RefundsTotalFormatted => _report != null ? $"${_report.RefundsTotal:F2}" : "$0.00";
    public string TipsTotalFormatted => _report != null ? $"${_report.TipsTotal:F2}" : "$0.00";
    public string DepositsTotalFormatted => _report != null ? $"${_report.DepositsTotal:F2}" : "$0.00";
    public string WithdrawalsTotalFormatted => _report != null ? $"${_report.WithdrawalsTotal:F2}" : "$0.00";

    public async System.Threading.Tasks.Task LoadReportAsync(Guid sessionId)
    {
        if (_cajaService == null) return;

        try
        {
            IsLoading = true;
            var report = await _cajaService.GetReportAsync(sessionId);
            Report = report; // This will trigger all property change notifications
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading report: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}
