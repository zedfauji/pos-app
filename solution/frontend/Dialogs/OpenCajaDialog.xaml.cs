using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class OpenCajaDialog : ContentDialog
{
    public decimal? OpeningAmount { get; private set; }
    public string? Notes { get; private set; }

    public OpenCajaDialog()
    {
        this.InitializeComponent();
    }

    private void OpeningAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        HideError();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate opening amount
        if (string.IsNullOrWhiteSpace(OpeningAmountTextBox.Text))
        {
            args.Cancel = true;
            ShowError("El monto de apertura es requerido.");
            return;
        }

        if (!decimal.TryParse(OpeningAmountTextBox.Text, out var amount) || amount < 0)
        {
            args.Cancel = true;
            ShowError("El monto debe ser un número válido mayor o igual a cero.");
            return;
        }

        OpeningAmount = amount;
        Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
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
