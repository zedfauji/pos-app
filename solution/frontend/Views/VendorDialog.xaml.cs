using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs;
using MagiDesk.Frontend.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RelayCommand = MagiDesk.Frontend.Services.RelayCommand;

namespace MagiDesk.Frontend.Views;

public sealed partial class VendorDialog : ContentDialog, INotifyPropertyChanged
{
    private VendorViewModel _viewModel;

    public VendorViewModel ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != value)
            {
                _viewModel = value;
                OnPropertyChanged();
            }
        }
    }

    public VendorDialog(VendorDto? vendorDto = null)
    {
        ViewModel = new VendorViewModel(vendorDto);
        this.InitializeComponent();
        this.DataContext = this;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VendorViewModel : INotifyPropertyChanged
{
    private ExtendedVendorDtoForDialog _vendor;

    public ExtendedVendorDtoForDialog Vendor
    {
        get => _vendor;
        set
        {
            if (_vendor != value)
            {
                _vendor = value;
                OnPropertyChanged();
            }
        }
    }

    public string Title { get; set; } = "Vendor";

    public Services.RelayCommand SaveCommand { get; }

    public VendorViewModel(VendorDto? vendorDto = null)
    {
        _vendor = ExtendedVendorDtoForDialog.FromVendorDto(vendorDto ?? new VendorDto());
        Title = vendorDto == null ? "New Vendor" : "Edit Vendor";
        
        Func<object?, Task> saveAction = async (object? _) =>
        {
            // Validation and save logic handled by ContentDialog.PrimaryButtonClick
            await Task.CompletedTask;
        };
        Func<object?, bool> canExecute = (object? _) => true;
        
        SaveCommand = new Services.RelayCommand(saveAction, canExecute);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Extended VendorDto to support additional properties expected by XAML
public class ExtendedVendorDtoForDialog : INotifyPropertyChanged
{
    private string? _id;
    private string _name = string.Empty;
    private string? _contactInfo;
    private string _status = "active";
    private decimal _budget;
    private string? _reminder;
    private bool _reminderEnabled;
    private string? _notes;
    private string? _contactPerson;
    private string? _phone;
    private string? _email;
    private string? _address;
    private string? _paymentTerms;

    public string? Id
    {
        get => _id;
        set { if (_id != value) { _id = value; OnPropertyChanged(); } }
    }

    public string Name
    {
        get => _name;
        set { if (_name != value) { _name = value; OnPropertyChanged(); } }
    }

    public string? ContactInfo
    {
        get => _contactInfo;
        set { if (_contactInfo != value) { _contactInfo = value; OnPropertyChanged(); } }
    }

    public string Status
    {
        get => _status;
        set { if (_status != value) { _status = value; OnPropertyChanged(); } }
    }

    public decimal Budget
    {
        get => _budget;
        set { if (_budget != value) { _budget = value; OnPropertyChanged(); } }
    }

    public string? Reminder
    {
        get => _reminder;
        set { if (_reminder != value) { _reminder = value; OnPropertyChanged(); } }
    }

    public bool ReminderEnabled
    {
        get => _reminderEnabled;
        set { if (_reminderEnabled != value) { _reminderEnabled = value; OnPropertyChanged(); } }
    }

    public string? Notes
    {
        get => _notes;
        set { if (_notes != value) { _notes = value; OnPropertyChanged(); } }
    }

    public string? ContactPerson
    {
        get => _contactPerson;
        set { if (_contactPerson != value) { _contactPerson = value; OnPropertyChanged(); } }
    }

    public string? Phone
    {
        get => _phone;
        set { if (_phone != value) { _phone = value; OnPropertyChanged(); } }
    }

    public string? Email
    {
        get => _email;
        set { if (_email != value) { _email = value; OnPropertyChanged(); } }
    }

    public string? Address
    {
        get => _address;
        set { if (_address != value) { _address = value; OnPropertyChanged(); } }
    }

    public string? PaymentTerms
    {
        get => _paymentTerms;
        set { if (_paymentTerms != value) { _paymentTerms = value; OnPropertyChanged(); } }
    }

    public static ExtendedVendorDtoForDialog FromVendorDto(VendorDto dto)
    {
        return new ExtendedVendorDtoForDialog
        {
            Id = dto.Id,
            Name = dto.Name,
            ContactInfo = dto.ContactInfo,
            Status = dto.Status,
            Budget = dto.Budget,
            Reminder = dto.Reminder,
            ReminderEnabled = dto.ReminderEnabled,
            Notes = dto.Notes
        };
    }

    public VendorDto ToVendorDto()
    {
        return new VendorDto
        {
            Id = Id,
            Name = Name,
            ContactInfo = ContactInfo,
            Status = Status,
            Budget = Budget,
            Reminder = Reminder,
            ReminderEnabled = ReminderEnabled,
            Notes = Notes
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

