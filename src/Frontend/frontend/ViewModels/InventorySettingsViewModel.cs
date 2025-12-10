using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class InventorySettingsViewModel : INotifyPropertyChanged
{
    private readonly InventorySettingsService _settingsService;
    private InventorySettings? _settings;
    private bool _isLoading;
    private string _statusMessage = "Ready";

    public InventorySettingsViewModel(InventorySettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        Vendors = new ObservableCollection<VendorOption>();
    }

    public InventorySettings? Settings
    {
        get => _settings;
        private set
        {
            _settings = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<VendorOption> Vendors { get; }

    public async Task LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading settings...";

            // Load inventory settings
            Settings = await _settingsService.GetInventorySettingsAsync();
            
            // Load vendors for dropdown
            await LoadVendorsAsync();

            StatusMessage = "Settings loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> SaveSettingsAsync()
    {
        if (Settings == null) return false;

        try
        {
            IsLoading = true;
            StatusMessage = "Saving settings...";

            var success = await _settingsService.SaveInventorySettingsAsync(Settings);
            
            if (success)
            {
                StatusMessage = "Settings saved successfully";
            }
            else
            {
                StatusMessage = "Failed to save settings";
            }

            return success;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadVendorsAsync()
    {
        try
        {
            Vendors.Clear();
            var vendors = await _settingsService.GetVendorsAsync();
            
            foreach (var vendor in vendors)
            {
                Vendors.Add(new VendorOption
                {
                    Id = vendor.Id,
                    Name = vendor.Name
                });
            }
        }
        catch (Exception ex)
        {
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class VendorOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
