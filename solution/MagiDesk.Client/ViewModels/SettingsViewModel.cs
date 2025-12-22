using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace MagiDesk.Client.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private ApplicationDataContainer? _localSettings;

        [ObservableProperty]
        private string _restaurantName = "My Restaurant";

        [ObservableProperty]
        private string _restaurantAddress = "123 Food Street";

        [ObservableProperty]
        private string _receiptFooter = "Thank you for dining with us!";

        [ObservableProperty]
        private double _taxRate = 10.0; // Percent

        public DayCloseViewModel DayCloseViewModel { get; }

        public SettingsViewModel(IDialogService dialogService, DayCloseViewModel dayCloseViewModel)
        {
            _dialogService = dialogService;
            DayCloseViewModel = dayCloseViewModel;
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                 _localSettings = ApplicationData.Current.LocalSettings;
            }
            catch
            {
                // Likely running unpackaged or no identity
                _localSettings = null;
                return;
            }

            if (_localSettings == null) return;
            if (_localSettings.Values.TryGetValue("RestaurantName", out var name)) RestaurantName = (string)name;
            if (_localSettings.Values.TryGetValue("RestaurantAddress", out var addr)) RestaurantAddress = (string)addr;
            if (_localSettings.Values.TryGetValue("ReceiptFooter", out var footer)) ReceiptFooter = (string)footer;
            if (_localSettings.Values.TryGetValue("TaxRate", out var tax)) 
            {
                // Settings stores primitives, might need conversion
                if (double.TryParse(tax.ToString(), out double rate)) TaxRate = rate; 
            }
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            if (_localSettings == null)
            {
                await _dialogService.ShowMessageAsync("Error", "Cannot save settings: Local storage unavailable.");
                return;
            }

            _localSettings.Values["RestaurantName"] = RestaurantName;
            _localSettings.Values["RestaurantAddress"] = RestaurantAddress;
            _localSettings.Values["ReceiptFooter"] = ReceiptFooter;
            _localSettings.Values["TaxRate"] = TaxRate.ToString(); // Store decimal as string to be safe

            await _dialogService.ShowMessageAsync("Settings Saved", "Your preferences have been updated.");
        }
    }
}
