using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Views;

public sealed partial class ModifierManagementPage : Page, INotifyPropertyChanged
{
    public ObservableCollection<ModifierManagementViewModel> Modifiers { get; } = new();
    
    private ModifierManagementViewModel? _selectedModifier;
    public ModifierManagementViewModel? SelectedModifier
    {
        get => _selectedModifier;
        set
        {
            if (_selectedModifier != value)
            {
                _selectedModifier = value;
                OnPropertyChanged(nameof(SelectedModifier));
                ModifierDetailsPanel.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private readonly MenuApiService _menuService;

    public ModifierManagementPage()
    {
        this.InitializeComponent();
        _menuService = App.Menu ?? throw new InvalidOperationException("Menu service not available");
        LoadModifiers();
    }

    private async void LoadModifiers()
    {
        try
        {
            // TODO: Implement GetModifiersAsync in MenuApiService
            // For now, create sample modifiers
            Modifiers.Clear();
            
            // Sample modifiers for demonstration
            var sampleModifiers = new[]
            {
                new ModifierManagementViewModel
                {
                    Id = 1,
                    Name = "Size",
                    Description = "Choose your drink size",
                    IsRequired = true,
                    AllowMultiple = false,
                    MaxSelections = 1,
                    Options = new ObservableCollection<ModifierOptionManagementViewModel>
                    {
                        new() { Id = 1, Name = "Small", PriceDelta = 0, SortOrder = 1 },
                        new() { Id = 2, Name = "Medium", PriceDelta = 1.50m, SortOrder = 2 },
                        new() { Id = 3, Name = "Large", PriceDelta = 2.50m, SortOrder = 3 }
                    }
                },
                new ModifierManagementViewModel
                {
                    Id = 2,
                    Name = "Flavor",
                    Description = "Add flavor to your drink",
                    IsRequired = false,
                    AllowMultiple = true,
                    MaxSelections = 3,
                    Options = new ObservableCollection<ModifierOptionManagementViewModel>
                    {
                        new() { Id = 4, Name = "Vanilla", PriceDelta = 0.50m, SortOrder = 1 },
                        new() { Id = 5, Name = "Chocolate", PriceDelta = 0.50m, SortOrder = 2 },
                        new() { Id = 6, Name = "Strawberry", PriceDelta = 0.50m, SortOrder = 3 },
                        new() { Id = 7, Name = "Caramel", PriceDelta = 0.75m, SortOrder = 4 }
                    }
                },
                new ModifierManagementViewModel
                {
                    Id = 3,
                    Name = "Salsa",
                    Description = "Choose your salsa heat level",
                    IsRequired = true,
                    AllowMultiple = false,
                    MaxSelections = 1,
                    Options = new ObservableCollection<ModifierOptionManagementViewModel>
                    {
                        new() { Id = 8, Name = "Mild", PriceDelta = 0, SortOrder = 1 },
                        new() { Id = 9, Name = "Medium", PriceDelta = 0, SortOrder = 2 },
                        new() { Id = 10, Name = "Hot", PriceDelta = 0, SortOrder = 3 },
                        new() { Id = 11, Name = "Extra Hot", PriceDelta = 0.25m, SortOrder = 4 }
                    }
                }
            };

            foreach (var modifier in sampleModifiers)
            {
                Modifiers.Add(modifier);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load modifiers: {ex.Message}");
        }
    }

    private void ModifiersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModifiersListView.SelectedItem is ModifierManagementViewModel modifier)
        {
            SelectedModifier = modifier;
        }
    }

    private void AddModifier_Click(object sender, RoutedEventArgs e)
    {
        var newModifier = new ModifierManagementViewModel
        {
            Id = Modifiers.Count + 1,
            Name = "New Modifier",
            Description = "",
            IsRequired = false,
            AllowMultiple = false,
            MaxSelections = 1,
            Options = new ObservableCollection<ModifierOptionManagementViewModel>()
        };
        
        Modifiers.Add(newModifier);
        SelectedModifier = newModifier;
    }

    private void EditModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierManagementViewModel modifier)
        {
            SelectedModifier = modifier;
        }
    }

    private void DeleteModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierManagementViewModel modifier)
        {
            Modifiers.Remove(modifier);
            if (SelectedModifier == modifier)
            {
                SelectedModifier = null;
            }
        }
    }

    private void AddOption_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedModifier != null)
        {
            var newOption = new ModifierOptionManagementViewModel
            {
                Id = SelectedModifier.Options.Count + 1,
                Name = "New Option",
                PriceDelta = 0,
                SortOrder = SelectedModifier.Options.Count + 1
            };
            
            SelectedModifier.Options.Add(newOption);
        }
    }

    private void EditOption_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement option editing dialog
        System.Diagnostics.Debug.WriteLine("Edit option clicked");
    }

    private void DeleteOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierOptionManagementViewModel option && SelectedModifier != null)
        {
            SelectedModifier.Options.Remove(option);
        }
    }

    private void SaveModifier_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement save to backend
        System.Diagnostics.Debug.WriteLine($"Saving modifier: {SelectedModifier?.Name}");
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        SelectedModifier = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// ViewModels for Modifier Management
public class ModifierManagementViewModel : INotifyPropertyChanged
{
    public long Id { get; set; }
    
    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }
    
    private string _description = "";
    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }
    }
    
    private bool _isRequired;
    public bool IsRequired
    {
        get => _isRequired;
        set
        {
            if (_isRequired != value)
            {
                _isRequired = value;
                OnPropertyChanged(nameof(IsRequired));
            }
        }
    }
    
    private bool _allowMultiple;
    public bool AllowMultiple
    {
        get => _allowMultiple;
        set
        {
            if (_allowMultiple != value)
            {
                _allowMultiple = value;
                OnPropertyChanged(nameof(AllowMultiple));
            }
        }
    }
    
    private int? _maxSelections;
    public int? MaxSelections
    {
        get => _maxSelections;
        set
        {
            if (_maxSelections != value)
            {
                _maxSelections = value;
                OnPropertyChanged(nameof(MaxSelections));
            }
        }
    }
    
    public ObservableCollection<ModifierOptionManagementViewModel> Options { get; set; } = new();
    public int OptionsCount => Options.Count;
    public string IsRequiredText => IsRequired ? "Required" : "Optional";
    public string AllowMultipleText => AllowMultiple ? "Multiple" : "Single";

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class ModifierOptionManagementViewModel : INotifyPropertyChanged
{
    public long Id { get; set; }
    
    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }
    
    private decimal _priceDelta;
    public decimal PriceDelta
    {
        get => _priceDelta;
        set
        {
            if (_priceDelta != value)
            {
                _priceDelta = value;
                OnPropertyChanged(nameof(PriceDelta));
            }
        }
    }
    
    public int SortOrder { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// Value Converters
public class BoolToRequiredTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool isRequired ? (isRequired ? "Required" : "Optional") : "Optional";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

public class BoolToMultipleTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool allowMultiple ? (allowMultiple ? "Multiple" : "Single") : "Single";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
