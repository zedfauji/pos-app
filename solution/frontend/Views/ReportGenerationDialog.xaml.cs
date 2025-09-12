using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace MagiDesk.Frontend.Views;

public sealed partial class ReportGenerationDialog : ContentDialog
{
    public Dictionary<string, object> ReportParameters { get; private set; }

    public ReportGenerationDialog()
    {
        this.InitializeComponent();
        ReportParameters = new Dictionary<string, object>();
        
        // Set default dates
        StartDatePicker.Date = DateTime.Now.AddDays(-30);
        EndDatePicker.Date = DateTime.Now;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate form
        if (StartDatePicker.Date == null || EndDatePicker.Date == null)
        {
            args.Cancel = true;
            return;
        }

        if (StartDatePicker.Date > EndDatePicker.Date)
        {
            args.Cancel = true;
            return;
        }

        // Build report parameters
        ReportParameters.Clear();
        ReportParameters["ReportType"] = (ReportTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "InventorySummary";
        ReportParameters["StartDate"] = StartDatePicker.Date.DateTime;
        ReportParameters["EndDate"] = EndDatePicker.Date.DateTime;
        ReportParameters["IncludeInventory"] = IncludeInventoryCheckBox.IsChecked ?? false;
        ReportParameters["IncludeVendorOrders"] = IncludeVendorOrdersCheckBox.IsChecked ?? false;
        ReportParameters["IncludeSystemEvents"] = IncludeSystemEventsCheckBox.IsChecked ?? false;
        ReportParameters["IncludeErrorsOnly"] = IncludeErrorsCheckBox.IsChecked ?? false;
        ReportParameters["ExportToCSV"] = ExportToCSVCheckBox.IsChecked ?? false;
        ReportParameters["IncludeCharts"] = IncludeChartsCheckBox.IsChecked ?? false;
        ReportParameters["DetailedLogs"] = DetailedLogsCheckBox.IsChecked ?? false;
    }
}
