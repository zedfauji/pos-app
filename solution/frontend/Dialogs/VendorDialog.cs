using MagiDesk.Shared.DTOs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Dialogs;

public class VendorDialog : ContentDialog
{
    private readonly TextBox _name = new() { PlaceholderText = "Name" };
    private readonly TextBox _contact = new() { PlaceholderText = "Contact" };
    private readonly TextBox _status = new() { PlaceholderText = "Status" };
    private readonly NumberBox _budget = new() { SmallChange = 1, LargeChange = 10, Minimum = 0, Header = "Budget" };
    private readonly ToggleSwitch _reminderEnabled = new() { Header = "Weekly Reminder" };
    private readonly ComboBox _reminderDay = new() { PlaceholderText = "Weekday" };

    public VendorDto Dto { get; }

    public VendorDialog(VendorDto dto)
    {
        Dto = dto;
        Title = string.IsNullOrWhiteSpace(dto.Id) ? "Add Vendor" : "Edit Vendor";
        PrimaryButtonText = "Save";
        CloseButtonText = "Cancel";
        DefaultButton = ContentDialogButton.Primary;

        _name.Text = dto.Name ?? string.Empty;
        _contact.Text = dto.ContactInfo ?? string.Empty;
        _status.Text = dto.Status ?? string.Empty;
        _budget.Value = (double)dto.Budget;
        _reminderEnabled.IsOn = dto.ReminderEnabled;
        _reminderDay.ItemsSource = new[] { "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday" };
        if (!string.IsNullOrWhiteSpace(dto.Reminder)) _reminderDay.SelectedItem = dto.Reminder;

        Content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock{ Text = "Name"}, _name,
                new TextBlock{ Text = "Contact"}, _contact,
                new TextBlock{ Text = "Status"}, _status,
                _budget,
                _reminderEnabled,
                new TextBlock{ Text = "Reminder Day"}, _reminderDay,
            }
        };

        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Dto.Name = _name.Text;
        Dto.ContactInfo = _contact.Text;
        Dto.Status = _status.Text;
        Dto.Budget = (decimal)_budget.Value;
        Dto.ReminderEnabled = _reminderEnabled.IsOn;
        Dto.Reminder = _reminderDay.SelectedItem as string;
    }
}
