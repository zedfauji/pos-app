using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class CloseCajaDialog : ContentDialog
{
    private readonly CajaService.CajaSessionDto _session;
    
    public decimal? ClosingAmount { get; private set; }
    public string? Notes { get; private set; }
    public bool ManagerOverride { get; private set; }

    public CloseCajaDialog(CajaService.CajaSessionDto session)
    {
        _session = session;
        this.InitializeComponent();
        this.DataContext = session;
        
        // Set formatted values
        SystemTotalText.Text = $"${session.OpeningAmount:F2}";
        OpeningAmountRun.Text = $"{session.OpeningAmount:F2}";
    }

    private void ClosingAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        HideError();
        
        // Calculate difference
        if (decimal.TryParse(ClosingAmountTextBox.Text, out var closingAmount))
        {
            var systemTotal = _session.SystemCalculatedTotal ?? _session.OpeningAmount;
            var difference = closingAmount - (_session.OpeningAmount + systemTotal);
            DifferenceText.Text = $"${difference:F2}";
            
            // Change color based on difference
            if (Math.Abs(difference) > 10)
            {
                DifferenceText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else
            {
                DifferenceText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
            }
        }
        else
        {
            DifferenceText.Text = "$0.00";
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate closing amount
        if (string.IsNullOrWhiteSpace(ClosingAmountTextBox.Text))
        {
            args.Cancel = true;
            ShowError("El monto de cierre es requerido.");
            return;
        }

        if (!decimal.TryParse(ClosingAmountTextBox.Text, out var amount) || amount < 0)
        {
            args.Cancel = true;
            ShowError("El monto debe ser un número válido mayor o igual a cero.");
            return;
        }

        ClosingAmount = amount;
        Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
        ManagerOverride = ManagerOverrideCheckBox.IsChecked == true;
        HideError();
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
}
