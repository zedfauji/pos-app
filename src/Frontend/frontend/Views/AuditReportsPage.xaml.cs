using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class AuditReportsPage : Page, IToolbarConsumer
{
    private readonly AuditReportsViewModel _vm;

    public AuditReportsPage()
    {
        this.InitializeComponent();
        
        // Create services with proper HTTP clients and loggers
        var auditService = new AuditService(new HttpClient(), new SimpleLogger<AuditService>());
        var inventoryService = new InventoryService(new HttpClient(), new SimpleLogger<InventoryService>());
        
        _vm = new AuditReportsViewModel(auditService, inventoryService);
        this.DataContext = _vm;
        
        Loaded += AuditReportsPage_Loaded;
    }

    private async void AuditReportsPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private async void GenerateReport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ReportGenerationDialog();
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await _vm.GenerateCustomReportAsync(dialog.ReportParameters);
            await _vm.LoadDataAsync();
        }
    }

    private async void ExportData_Click(object sender, RoutedEventArgs e)
    {
        await _vm.ExportAllDataAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchText = SearchBox.Text;
        _vm.ApplyFilters();
    }

    private void ReportTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = ReportTypeFilter.SelectedItem as ComboBoxItem;
        _vm.ReportTypeFilter = selectedItem?.Tag?.ToString() ?? string.Empty;
        _vm.ApplyFilters();
    }

    private async void GenerateInventoryReport_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = ReportPeriod.SelectedItem as ComboBoxItem;
        var days = int.Parse(selectedItem?.Tag?.ToString() ?? "30");
        
        await _vm.GenerateInventoryReportAsync(days);
    }

    private async void ExportInventoryReport_Click(object sender, RoutedEventArgs e)
    {
        await _vm.ExportInventoryReportAsync();
    }

    // IToolbarConsumer implementation
    public void OnAdd()
    {
        GenerateReport_Click(this, new RoutedEventArgs());
    }

    public void OnEdit()
    {
        // Not applicable for audit reports
    }

    public void OnDelete()
    {
        // Not applicable for audit reports
    }

    public void OnRefresh()
    {
        Refresh_Click(this, new RoutedEventArgs());
    }
}



