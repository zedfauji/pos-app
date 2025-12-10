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

    private readonly MenuApiService? _menuService;

    public ModifierManagementPage()
    {
        try
        {
            this.InitializeComponent();
            _menuService = App.Menu;
            
            if (_menuService == null)
            {
                ShowErrorDialog("Service Not Available", "Menu service is not available. Please restart the application or contact support.");
                return;
            }
            
            Loaded += async (s, e) => await LoadModifiersAsync();
        }
        catch (Exception ex)
        {
            // Show error dialog instead of letting the exception crash the app
            ShowErrorDialog("Initialization Error", $"Failed to initialize Modifier Management page: {ex.Message}");
        }
    }

    private async Task LoadModifiersAsync()
    {
        if (_menuService == null) return;

        try
        {
            Modifiers.Clear();
            
            var query = new MenuApiService.ModifierQuery(Q: null, Page: 1, PageSize: 100);
            var modifiers = await _menuService.ListModifiersAsync(query);

            foreach (var modifier in modifiers)
            {
                var vm = new ModifierManagementViewModel
                {
                    Id = modifier.Id,
                    Name = modifier.Name,
                    Description = "", // ModifierDto doesn't have Description in the current API
                    IsRequired = modifier.IsRequired,
                    AllowMultiple = modifier.AllowMultiple,
                    MaxSelections = modifier.MaxSelections,
                    Options = new ObservableCollection<ModifierOptionManagementViewModel>()
                };

                foreach (var option in modifier.Options)
                {
                    vm.Options.Add(new ModifierOptionManagementViewModel
                    {
                        Id = option.Id,
                        Name = option.Name,
                        PriceDelta = option.PriceDelta,
                        SortOrder = option.SortOrder
                    });
                }

                Modifiers.Add(vm);
            }

        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to load modifiers: {ex.Message}");
        }
    }

    private void ModifiersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ModifiersListView.SelectedItem is ModifierManagementViewModel modifier)
        {
            SelectedModifier = modifier;
        }
    }

    private async void AddModifier_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Dialogs.ModifierCrudDialog();
            dialog.XamlRoot = this.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await LoadModifiersAsync(); // Refresh the list
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to add modifier: {ex.Message}");
        }
    }

    private async void EditModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierManagementViewModel modifier)
        {
            try
            {
                var dialog = new Dialogs.ModifierCrudDialog(modifier);
                dialog.XamlRoot = this.XamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    await LoadModifiersAsync(); // Refresh the list
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to edit modifier: {ex.Message}");
            }
        }
    }

    private async void DeleteModifier_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ModifierManagementViewModel modifier)
        {
            try
            {
                var confirmDialog = new ContentDialog
                {
                    Title = "Delete Modifier",
                    Content = $"Are you sure you want to delete '{modifier.Name}'? This action cannot be undone.",
                    PrimaryButtonText = "Delete",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary && _menuService != null)
                {
                    var success = await _menuService.DeleteModifierAsync(modifier.Id);
                    if (success)
                    {
                        await LoadModifiersAsync(); // Refresh the list
                    }
                    else
                    {
                        ShowErrorDialog("Error", "Failed to delete modifier. Please try again.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to delete modifier: {ex.Message}");
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
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        SelectedModifier = null;
    }

    private async void ShowErrorDialog(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message },
                PrimaryButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            // Fallback: Log error if dialog fails
        }
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
