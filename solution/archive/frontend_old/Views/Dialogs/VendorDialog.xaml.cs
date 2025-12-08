using MagiDesk.Shared.DTOs;
using System.Linq;

namespace MagiDesk.Frontend.Views;

public sealed class VendorDialog : ContentDialog
{
    private readonly VendorDto _dto;
    private readonly TextBox _nameText = new() { Header = "Name" };
    private readonly TextBox _contactText = new() { Header = "Contact Info" };
    private readonly ComboBox _statusCombo = new() { Header = "Status" };

    public VendorDialog(VendorDto dto)
    {
        _dto = dto;

        Title = "Vendor";
        PrimaryButtonText = "Save";
        CloseButtonText = "Cancel";

        _statusCombo.Items.Add(new ComboBoxItem { Content = "active" });
        _statusCombo.Items.Add(new ComboBoxItem { Content = "inactive" });

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(_nameText);
        panel.Children.Add(_contactText);
        panel.Children.Add(_statusCombo);
        Content = panel;

        Loaded += OnLoaded;
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _nameText.Text = _dto.Name;
        _contactText.Text = _dto.ContactInfo ?? string.Empty;
        var status = _dto.Status?.ToLowerInvariant() == "inactive" ? "inactive" : "active";
        foreach (var item in _statusCombo.Items.OfType<ComboBoxItem>())
        {
            item.IsSelected = string.Equals(item.Content?.ToString(), status, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _dto.Name = _nameText.Text?.Trim() ?? string.Empty;
        _dto.ContactInfo = string.IsNullOrWhiteSpace(_contactText.Text) ? null : _contactText.Text.Trim();
        _dto.Status = (_statusCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "active";
    }
}
