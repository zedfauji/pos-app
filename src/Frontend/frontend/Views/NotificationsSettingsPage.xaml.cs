using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class NotificationsSettingsPage : Page, ISettingsSubPage
{
    private NotificationSettings _settings = new();
    private ObservableCollection<ThresholdAlert> _thresholdAlerts = new();

    public NotificationsSettingsPage()
    {
        this.InitializeComponent();
        InitializeUI();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        // Handle subcategory-specific logic if needed
        // Could show/hide sections based on subcategory
    }

    public void SetSettings(object settings)
    {
        if (settings is NotificationSettings notificationSettings)
        {
            _settings = notificationSettings;
            LoadSettingsToUI();
        }
    }

    public NotificationSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void InitializeUI()
    {
        // Initialize threshold alerts list
        ThresholdAlertsList.ItemsSource = _thresholdAlerts;
        
        // Set default values
        SmtpPortBox.Value = 587;
        AlertVolumeSlider.Value = 50;

        // Populate SMS provider combo
        SmsProviderCombo.ItemsSource = new List<string> { "Twilio", "AWS SNS", "Azure Communication Services" };
    }

    private void LoadSettingsToUI()
    {
        // Email Settings
        EnableEmailCheck.IsChecked = _settings.Email.EnableEmail;
        SmtpServerBox.Text = _settings.Email.SmtpServer;
        SmtpPortBox.Value = _settings.Email.SmtpPort;
        EmailUsernameBox.Text = _settings.Email.Username;
        EmailPasswordBox.Password = _settings.Email.Password;
        UseSslCheck.IsChecked = _settings.Email.UseSsl;
        FromAddressBox.Text = _settings.Email.FromAddress;
        FromNameBox.Text = _settings.Email.FromName;

        // SMS Settings
        EnableSmsCheck.IsChecked = _settings.Sms.EnableSms;
        SmsProviderCombo.SelectedItem = _settings.Sms.Provider;
        SmsApiKeyBox.Password = _settings.Sms.ApiKey;
        SmsApiSecretBox.Password = _settings.Sms.ApiSecret;
        SmsFromNumberBox.Text = _settings.Sms.FromNumber;

        // Push Settings
        EnablePushCheck.IsChecked = _settings.Push.EnablePush;
        ShowStockAlertsCheck.IsChecked = _settings.Push.ShowStockAlerts;
        ShowOrderAlertsCheck.IsChecked = _settings.Push.ShowOrderAlerts;
        ShowPaymentAlertsCheck.IsChecked = _settings.Push.ShowPaymentAlerts;
        ShowSystemAlertsCheck.IsChecked = _settings.Push.ShowSystemAlerts;

        // Alert Settings
        EnableSoundAlertsCheck.IsChecked = _settings.Alerts.EnableSoundAlerts;
        AlertVolumeSlider.Value = _settings.Alerts.AlertVolume;
        AlertSoundPathBox.Text = _settings.Alerts.AlertSoundPath;

        // Threshold Alerts
        _thresholdAlerts.Clear();
        foreach (var alert in _settings.Alerts.ThresholdAlerts)
        {
            _thresholdAlerts.Add(new ThresholdAlert
            {
                Name = alert.Name,
                Type = alert.Type,
                Threshold = alert.Threshold,
                Condition = alert.Condition,
                IsEnabled = alert.IsEnabled
            });
        }
    }

    private void CollectUIValuesToSettings()
    {
        // Email Settings
        _settings.Email.EnableEmail = EnableEmailCheck.IsChecked ?? false;
        _settings.Email.SmtpServer = SmtpServerBox.Text;
        _settings.Email.SmtpPort = (int)SmtpPortBox.Value;
        _settings.Email.Username = EmailUsernameBox.Text;
        _settings.Email.Password = EmailPasswordBox.Password;
        _settings.Email.UseSsl = UseSslCheck.IsChecked ?? false;
        _settings.Email.FromAddress = FromAddressBox.Text;
        _settings.Email.FromName = FromNameBox.Text;

        // SMS Settings
        _settings.Sms.EnableSms = EnableSmsCheck.IsChecked ?? false;
        _settings.Sms.Provider = SmsProviderCombo.SelectedItem as string ?? "";
        _settings.Sms.ApiKey = SmsApiKeyBox.Password;
        _settings.Sms.ApiSecret = SmsApiSecretBox.Password;
        _settings.Sms.FromNumber = SmsFromNumberBox.Text;

        // Push Settings
        _settings.Push.EnablePush = EnablePushCheck.IsChecked ?? false;
        _settings.Push.ShowStockAlerts = ShowStockAlertsCheck.IsChecked ?? false;
        _settings.Push.ShowOrderAlerts = ShowOrderAlertsCheck.IsChecked ?? false;
        _settings.Push.ShowPaymentAlerts = ShowPaymentAlertsCheck.IsChecked ?? false;
        _settings.Push.ShowSystemAlerts = ShowSystemAlertsCheck.IsChecked ?? false;

        // Alert Settings
        _settings.Alerts.EnableSoundAlerts = EnableSoundAlertsCheck.IsChecked ?? false;
        _settings.Alerts.AlertVolume = (int)AlertVolumeSlider.Value;
        _settings.Alerts.AlertSoundPath = AlertSoundPathBox.Text;

        // Threshold Alerts
        _settings.Alerts.ThresholdAlerts.Clear();
        foreach (var alert in _thresholdAlerts)
        {
            _settings.Alerts.ThresholdAlerts.Add(new ThresholdAlert
            {
                Name = alert.Name,
                Type = alert.Type,
                Threshold = alert.Threshold,
                Condition = alert.Condition,
                IsEnabled = alert.IsEnabled
            });
        }
    }

    private void AddThresholdAlert_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _thresholdAlerts.Add(new ThresholdAlert
        {
            Name = "New Alert",
            Type = "Stock",
            Threshold = 0,
            Condition = "Below",
            IsEnabled = true
        });
    }

    private void RemoveThresholdAlert_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ThresholdAlertsList.SelectedItem is ThresholdAlert selectedAlert)
        {
            _thresholdAlerts.Remove(selectedAlert);
        }
    }
}

