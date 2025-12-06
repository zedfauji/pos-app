using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagiDesk.Shared.DTOs;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorDialog : ContentDialog
{
    public VendorDialogViewModel ViewModel { get; }

    public VendorDialog(VendorDto? vendor = null)
    {
        ViewModel = new VendorDialogViewModel(vendor);
        InitializeComponent();
        DataContext = this;
    }

    public VendorDto GetVendor()
    {
        return ViewModel.GetVendorDto();
    }
}

public class VendorDialogViewModel : INotifyPropertyChanged
{
    private string _title = "Vendor Details";
    private string _name = string.Empty;
    private string _contactPerson = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private string _status = "Active";
    private string _paymentTerms = "Net 30";
    private decimal _creditLimit;

    public VendorDialogViewModel(VendorDto? vendor = null)
    {
        if (vendor != null)
        {
            _name = vendor.Name ?? string.Empty;
            _status = vendor.Status ?? "Active";
            
            // Parse ContactInfo if it exists (format may vary)
            if (!string.IsNullOrWhiteSpace(vendor.ContactInfo))
            {
                var contactInfo = vendor.ContactInfo;
                // Simple parsing - could be enhanced
                if (contactInfo.Contains("@"))
                {
                    _email = contactInfo;
                }
                else if (contactInfo.Contains("-") || contactInfo.Contains("("))
                {
                    _phone = contactInfo;
                }
                else
                {
                    _contactPerson = contactInfo;
                }
            }
            
            _creditLimit = vendor.Budget;
        }
        
        // Initialize Vendor property
        _vendor = new VendorInfo
        {
            Name = _name,
            ContactPerson = _contactPerson,
            Phone = _phone,
            Email = _email,
            Address = _address,
            Status = _status,
            PaymentTerms = _paymentTerms
        };
        
        // Subscribe to Vendor property changes
        _vendor.PropertyChanged += (s, e) =>
        {
            if (s is VendorInfo vi)
            {
                _name = vi.Name;
                _contactPerson = vi.ContactPerson;
                _phone = vi.Phone;
                _email = vi.Email;
                _address = vi.Address;
                _status = vi.Status;
                _paymentTerms = vi.PaymentTerms;
            }
        };
        
        SaveCommand = new Services.RelayCommand((object? p) => { OnSave(p); });
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private VendorInfo _vendor;

    public VendorInfo Vendor
    {
        get
        {
            if (_vendor == null)
            {
                _vendor = new VendorInfo
                {
                    Name = _name,
                    ContactPerson = _contactPerson,
                    Phone = _phone,
                    Email = _email,
                    Address = _address,
                    Status = _status,
                    PaymentTerms = _paymentTerms
                };
                // Subscribe to changes
                _vendor.PropertyChanged += (s, e) =>
                {
                    if (s is VendorInfo vi)
                    {
                        _name = vi.Name;
                        _contactPerson = vi.ContactPerson;
                        _phone = vi.Phone;
                        _email = vi.Email;
                        _address = vi.Address;
                        _status = vi.Status;
                        _paymentTerms = vi.PaymentTerms;
                    }
                };
            }
            return _vendor;
        }
    }

    public ICommand SaveCommand { get; }

    private void OnSave(object? parameter)
    {
        // Validation can be added here
    }

    public VendorDto GetVendorDto()
    {
        // Combine contact fields into ContactInfo
        var contactInfo = string.Empty;
        if (!string.IsNullOrWhiteSpace(_contactPerson))
            contactInfo = _contactPerson;
        if (!string.IsNullOrWhiteSpace(_phone))
            contactInfo += (string.IsNullOrWhiteSpace(contactInfo) ? "" : ", ") + _phone;
        if (!string.IsNullOrWhiteSpace(_email))
            contactInfo += (string.IsNullOrWhiteSpace(contactInfo) ? "" : ", ") + _email;
        if (!string.IsNullOrWhiteSpace(_address))
            contactInfo += (string.IsNullOrWhiteSpace(contactInfo) ? "" : ", ") + _address;

        return new VendorDto
        {
            Name = _name,
            ContactInfo = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo,
            Status = _status,
            Budget = _creditLimit
        };
    }

    public decimal CreditLimit
    {
        get => _creditLimit;
        set => SetProperty(ref _creditLimit, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

// Helper class for binding
public class VendorInfo : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _contactPerson = string.Empty;
    private string _phone = string.Empty;
    private string _email = string.Empty;
    private string _address = string.Empty;
    private string _status = "Active";
    private string _paymentTerms = "Net 30";

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string ContactPerson
    {
        get => _contactPerson;
        set
        {
            if (_contactPerson != value)
            {
                _contactPerson = value;
                OnPropertyChanged();
            }
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (_phone != value)
            {
                _phone = value;
                OnPropertyChanged();
            }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnPropertyChanged();
            }
        }
    }

    public string Address
    {
        get => _address;
        set
        {
            if (_address != value)
            {
                _address = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    public string PaymentTerms
    {
        get => _paymentTerms;
        set
        {
            if (_paymentTerms != value)
            {
                _paymentTerms = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


