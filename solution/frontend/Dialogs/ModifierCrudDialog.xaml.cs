using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Dialogs;

// Editable wrapper for modifier management
public sealed class EditableModifierVm : INotifyPropertyChanged
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }
    
    public ObservableCollection<EditableModifierOptionVm> Options { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class EditableModifierOptionVm : INotifyPropertyChanged
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }
    }
    
    public int SortOrder { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed partial class ModifierCrudDialog : ContentDialog
{
    public EditableModifierVm Modifier { get; }
    private readonly MenuApiService? _menuService;
    private bool _isEditMode;

    public ModifierCrudDialog(ModifierManagementViewModel? existingModifier = null)
    {
        this.InitializeComponent();
        _menuService = App.Menu;
        _isEditMode = existingModifier != null;

        if (existingModifier != null)
        {
            Modifier = new EditableModifierVm
            {
                Id = existingModifier.Id,
                Name = existingModifier.Name,
                Description = existingModifier.Description,
                IsRequired = existingModifier.IsRequired,
                AllowMultiple = existingModifier.AllowMultiple,
                MaxSelections = existingModifier.MaxSelections,
                Options = new ObservableCollection<EditableModifierOptionVm>()
            };

            foreach (var option in existingModifier.Options)
            {
                Modifier.Options.Add(new EditableModifierOptionVm
                {
                    Id = option.Id,
                    Name = option.Name,
                    PriceDelta = option.PriceDelta,
                    SortOrder = option.SortOrder
                });
            }
        }
        else
        {
            Modifier = new EditableModifierVm
            {
                Id = 0,
                Name = "New Modifier",
                Description = "",
                IsRequired = false,
                AllowMultiple = false,
                MaxSelections = null,
                Options = new ObservableCollection<EditableModifierOptionVm>()
            };
        }

        this.DataContext = Modifier;
    }

    private void AddOption_Click(object sender, RoutedEventArgs e)
    {
        var newOption = new EditableModifierOptionVm
        {
            Id = 0,
            Name = "New Option",
            PriceDelta = 0,
            SortOrder = Modifier.Options.Count + 1
        };
        
        Modifier.Options.Add(newOption);
    }

    private void MoveOptionUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is EditableModifierOptionVm option)
        {
            var index = Modifier.Options.IndexOf(option);
            if (index > 0)
            {
                Modifier.Options.Move(index, index - 1);
                UpdateSortOrders();
            }
        }
    }

    private void MoveOptionDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is EditableModifierOptionVm option)
        {
            var index = Modifier.Options.IndexOf(option);
            if (index < Modifier.Options.Count - 1)
            {
                Modifier.Options.Move(index, index + 1);
                UpdateSortOrders();
            }
        }
    }

    private void DeleteOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is EditableModifierOptionVm option)
        {
            Modifier.Options.Remove(option);
        }
    }

    private void UpdateSortOrders()
    {
        for (int i = 0; i < Modifier.Options.Count; i++)
        {
            Modifier.Options[i].SortOrder = i + 1;
        }
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(Modifier.Name))
            {
                ShowErrorDialog("Validation Error", "Modifier name is required.");
                args.Cancel = true;
                return;
            }

            if (_menuService == null)
            {
                ShowErrorDialog("Service Error", "Menu service is not available.");
                args.Cancel = true;
                return;
            }

            // Create DTOs
            var optionDtos = Modifier.Options.Select(opt => new MenuApiService.CreateModifierOptionDto(
                opt.Name, opt.PriceDelta, true, opt.SortOrder)).ToList();

            if (_isEditMode)
            {
                var updateDto = new MenuApiService.UpdateModifierDto(
                    Modifier.Name,
                    Modifier.Description,
                    Modifier.IsRequired,
                    Modifier.AllowMultiple,
                    null, // MinSelections not supported in current API
                    Modifier.MaxSelections,
                    optionDtos.Select(opt => new MenuApiService.UpdateModifierOptionDto(
                        opt.Name, opt.PriceDelta, true, opt.SortOrder)).ToList()
                );

                var result = await _menuService.UpdateModifierAsync(Modifier.Id, updateDto);
                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to update modifier. Please try again.");
                    args.Cancel = true;
                    return;
                }
            }
            else
            {
                var createDto = new MenuApiService.CreateModifierDto(
                    Modifier.Name,
                    Modifier.Description,
                    Modifier.IsRequired,
                    Modifier.AllowMultiple,
                    null, // MinSelections not supported in current API
                    Modifier.MaxSelections,
                    optionDtos
                );

                var result = await _menuService.CreateModifierAsync(createDto);
                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to create modifier. Please try again.");
                    args.Cancel = true;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog("Error", $"Failed to save modifier: {ex.Message}");
            args.Cancel = true;
        }
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
            System.Diagnostics.Debug.WriteLine($"Failed to show error dialog: {ex.Message}");
        }
    }
}
