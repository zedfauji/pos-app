using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Shared.DTOs;
using System.Windows.Input;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorDialog : ContentDialog
{
    public ExtendedVendorDto Vendor { get; private set; }
    public VendorDialogViewModel ViewModel { get; private set; }

    public VendorDialog(ExtendedVendorDto? existingVendor = null)
    {
        this.InitializeComponent();
        
        if (existingVendor != null)
        {
            Vendor = existingVendor;
            ViewModel = new VendorDialogViewModel(existingVendor, "Edit Vendor");
            LoadExistingItem();
        }
        else
        {
            Vendor = new ExtendedVendorDto
            {
                Id = string.Empty,
                Name = string.Empty,
                ContactPerson = string.Empty,
                Phone = string.Empty,
                Email = string.Empty,
                Address = string.Empty,
                Status = "active",
                PaymentTerms = "Net 30",
                CreditLimit = 0,
                TotalOrders = 0,
                PendingOrders = 0,
                AverageDeliveryTimeDays = 0
            };
            ViewModel = new VendorDialogViewModel(Vendor, "Add New Vendor");
        }
        
        this.DataContext = ViewModel;
    }

    private void LoadExistingItem()
    {
        NameBox.Text = Vendor.Name ?? string.Empty;
        ContactPersonBox.Text = Vendor.ContactPerson ?? string.Empty;
        PhoneBox.Text = Vendor.Phone ?? string.Empty;
        EmailBox.Text = Vendor.Email ?? string.Empty;
        AddressBox.Text = Vendor.Address ?? string.Empty;
        
        // Set ComboBox selections
        foreach (ComboBoxItem item in StatusBox.Items)
        {
            if (item.Tag?.ToString() == Vendor.Status)
            {
                StatusBox.SelectedItem = item;
                break;
            }
        }
        
        foreach (ComboBoxItem item in PaymentTermsBox.Items)
        {
            if (item.Tag?.ToString() == Vendor.PaymentTerms)
            {
                PaymentTermsBox.SelectedItem = item;
                break;
            }
        }
        
        CreditLimitBox.Value = (double)Vendor.CreditLimit;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Update vendor with form data
        Vendor.Name = NameBox.Text.Trim();
        Vendor.ContactPerson = ContactPersonBox.Text.Trim();
        Vendor.Phone = PhoneBox.Text.Trim();
        Vendor.Email = EmailBox.Text.Trim();
        Vendor.Address = AddressBox.Text.Trim();
        Vendor.Status = (StatusBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "active";
        Vendor.PaymentTerms = (PaymentTermsBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Net 30";
        Vendor.CreditLimit = (decimal)CreditLimitBox.Value;
    }
}

public class VendorDialogViewModel
{
    public ExtendedVendorDto Vendor { get; set; }
    public string Title { get; set; }
    public ICommand SaveCommand { get; }

    public VendorDialogViewModel(ExtendedVendorDto vendor, string title)
    {
        Vendor = vendor;
        Title = title;
        SaveCommand = new SimpleCommand(() => { /* Save logic handled in dialog */ });
    }
}

public class SimpleCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public SimpleCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
