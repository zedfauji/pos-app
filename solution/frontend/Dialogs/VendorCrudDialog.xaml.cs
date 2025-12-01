using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class VendorCrudDialog : ContentDialog
{
    public EditableVendorVm Item { get; private set; }

    public VendorCrudDialog(VendorDisplay? existingVendor = null)
    {
        // Initialize Item before InitializeComponent so XAML compiler can resolve types
        Item = existingVendor != null 
            ? EditableVendorVm.FromVendorDisplay(existingVendor) 
            : new EditableVendorVm();
        
        this.InitializeComponent();

        // Set DataContext for x:Bind to work properly
        this.DataContext = this;

        // Clear error message on load
        ErrorMessageText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(Item.Name))
        {
            ShowError("Name is required");
            args.Cancel = true;
            return;
        }

        // If reminder is enabled but no day selected, show warning but allow
        if (Item.ReminderEnabled && string.IsNullOrWhiteSpace(Item.Reminder))
        {
            // Allow but could show warning
        }
    }

    private void ShowError(string message)
    {
        ErrorMessageText.Text = message;
        ErrorMessageText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
    }
}

public class EditableVendorVm : System.ComponentModel.INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string? _contactInfo;
    private string _status = "active";
    private double _budget;
    private string? _reminder;
    private bool _reminderEnabled;
    private string? _notes;

    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

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

    public string? ContactInfo
    {
        get => _contactInfo;
        set
        {
            if (_contactInfo != value)
            {
                _contactInfo = value;
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

    public double Budget
    {
        get => _budget;
        set
        {
            if (_budget != value)
            {
                _budget = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Reminder
    {
        get => _reminder;
        set
        {
            if (_reminder != value)
            {
                _reminder = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ReminderEnabled
    {
        get => _reminderEnabled;
        set
        {
            if (_reminderEnabled != value)
            {
                _reminderEnabled = value;
                OnPropertyChanged();
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes != value)
            {
                _notes = value;
                OnPropertyChanged();
            }
        }
    }

    public static EditableVendorVm FromVendorDisplay(VendorDisplay vendor)
    {
        return new EditableVendorVm
        {
            Id = vendor.Id,
            Name = vendor.Name,
            ContactInfo = vendor.ContactInfo,
            Status = vendor.Status,
            Budget = (double)vendor.Budget,
            Reminder = vendor.Reminder,
            ReminderEnabled = vendor.ReminderEnabled,
            Notes = vendor.Notes
        };
    }

    public VendorDto ToDto()
    {
        return new VendorDto
        {
            Id = string.IsNullOrWhiteSpace(Id) ? null : Id,
            Name = Name,
            ContactInfo = ContactInfo,
            Status = Status,
            Budget = (decimal)Budget,
            Reminder = Reminder,
            ReminderEnabled = ReminderEnabled,
            Notes = Notes
        };
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }
}

