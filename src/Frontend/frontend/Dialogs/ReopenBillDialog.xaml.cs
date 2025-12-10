using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Tables;
using System.Collections.ObjectModel;
using System.Linq;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class ReopenBillDialog : ContentDialog
{
    public BillResult Bill { get; }
    public string SelectedTableLabel { get; private set; } = "";
    public bool ReopenWithSameTable => SameTableRadio.IsChecked == true;
    
    private readonly ObservableCollection<TableStatusDto> _availableTables = new();
    private readonly Services.TableRepository _tableRepository;

    public string BillInfo => $"Bill #{Bill.BillId} - Table: {Bill.TableLabel} - Amount: ${Bill.TotalAmount:F2}";

    public ReopenBillDialog(BillResult bill, Services.TableRepository tableRepository)
    {
        Bill = bill;
        _tableRepository = tableRepository;
        this.InitializeComponent();
        this.DataContext = this;
        LoadAvailableTablesAsync();
    }

    private async void LoadAvailableTablesAsync()
    {
        try
        {
            var tables = await _tableRepository.GetAllAsync();
            _availableTables.Clear();
            
            // Filter to show only available tables (not the current table)
            var available = tables
                .Where(t => !t.Occupied && t.Label != Bill.TableLabel)
                .OrderBy(t => t.Label)
                .ToList();
            
            foreach (var table in available)
            {
                _availableTables.Add(table);
            }
            
            AvailableTablesComboBox.ItemsSource = _availableTables;
            
            if (_availableTables.Count == 0)
            {
                ShowError("No available tables found. Please ensure there are free tables before reopening with a different table.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load available tables: {ex.Message}");
        }
    }

    private void DifferentTableRadio_Checked(object sender, RoutedEventArgs e)
    {
        TableSelectionPanel.Visibility = Visibility.Visible;
        HideError();
    }

    private void DifferentTableRadio_Unchecked(object sender, RoutedEventArgs e)
    {
        TableSelectionPanel.Visibility = Visibility.Collapsed;
        AvailableTablesComboBox.SelectedItem = null;
        SelectedTableLabel = "";
    }

    private void AvailableTablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is TableStatusDto table)
        {
            SelectedTableLabel = table.Label;
            HideError();
        }
    }

    private void ShowError(string message)
    {
        ErrorMessageText.Text = message;
        ErrorMessageBorder.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorMessageBorder.Visibility = Visibility.Collapsed;
        ErrorMessageText.Text = "";
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate selection
        if (DifferentTableRadio.IsChecked == true)
        {
            if (string.IsNullOrEmpty(SelectedTableLabel))
            {
                args.Cancel = true;
                ShowError("Please select a table to reopen the bill.");
                return;
            }
        }
        
        HideError();
    }
}

