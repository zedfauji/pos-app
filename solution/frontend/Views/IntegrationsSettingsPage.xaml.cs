using System.Collections.ObjectModel;
using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class IntegrationsSettingsPage : Page, ISettingsSubPage
{
    private IntegrationSettings _settings = new();
    private ObservableCollection<PaymentGateway> _paymentGateways = new();
    private ObservableCollection<WebhookEndpoint> _webhookEndpoints = new();
    private ObservableCollection<ApiEndpoint> _apiEndpoints = new();

    public IntegrationsSettingsPage()
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
        if (settings is IntegrationSettings integrationSettings)
        {
            _settings = integrationSettings;
            LoadSettingsToUI();
        }
    }

    public IntegrationSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void InitializeUI()
    {
        // Initialize lists
        PaymentGatewaysList.ItemsSource = _paymentGateways;
        WebhookEndpointsList.ItemsSource = _webhookEndpoints;
        ApiEndpointsList.ItemsSource = _apiEndpoints;
        
        // Set default values
        WebhookTimeoutBox.Value = 10000;
        WebhookMaxRetriesBox.Value = 3;
        ApiDefaultTimeoutBox.Value = 10000;
        ApiDefaultRetriesBox.Value = 3;
        CrmSyncIntervalBox.Value = 60;

        // Populate combo boxes
        DefaultGatewayCombo.ItemsSource = new List<string> { "Stripe", "Square", "PayPal" };
        CrmProviderCombo.ItemsSource = new List<string> { "Salesforce", "HubSpot", "Pipedrive" };
    }

    private void LoadSettingsToUI()
    {
        // Payment Gateway Settings
        DefaultGatewayCombo.SelectedItem = _settings.PaymentGateways.DefaultGateway;
        EnableTestModeCheck.IsChecked = _settings.PaymentGateways.EnableTestMode;

        // Payment Gateways List
        _paymentGateways.Clear();
        foreach (var gateway in _settings.PaymentGateways.Gateways)
        {
            _paymentGateways.Add(new PaymentGateway
            {
                Name = gateway.Name,
                Provider = gateway.Provider,
                ApiKey = gateway.ApiKey,
                SecretKey = gateway.SecretKey,
                WebhookSecret = gateway.WebhookSecret,
                IsEnabled = gateway.IsEnabled,
                IsTestMode = gateway.IsTestMode
            });
        }

        // Webhook Settings
        EnableWebhooksCheck.IsChecked = _settings.Webhooks.EnableWebhooks;
        WebhookTimeoutBox.Value = _settings.Webhooks.TimeoutMs;
        WebhookMaxRetriesBox.Value = _settings.Webhooks.MaxRetries;

        // Webhook Endpoints List
        _webhookEndpoints.Clear();
        foreach (var endpoint in _settings.Webhooks.Endpoints)
        {
            _webhookEndpoints.Add(new WebhookEndpoint
            {
                Name = endpoint.Name,
                Url = endpoint.Url,
                Events = endpoint.Events,
                Secret = endpoint.Secret,
                IsEnabled = endpoint.IsEnabled
            });
        }

        // CRM Settings
        EnableCrmSyncCheck.IsChecked = _settings.Crm.EnableCrmSync;
        CrmProviderCombo.SelectedItem = _settings.Crm.CrmProvider;
        CrmApiEndpointBox.Text = _settings.Crm.ApiEndpoint;
        CrmApiKeyBox.Password = _settings.Crm.ApiKey;
        SyncCustomersCheck.IsChecked = _settings.Crm.SyncCustomers;
        SyncOrdersCheck.IsChecked = _settings.Crm.SyncOrders;
        CrmSyncIntervalBox.Value = _settings.Crm.SyncIntervalMinutes;

        // API Settings
        ApiDefaultTimeoutBox.Value = _settings.Api.DefaultTimeoutMs;
        ApiDefaultRetriesBox.Value = _settings.Api.DefaultRetries;
        EnableApiLoggingCheck.IsChecked = _settings.Api.EnableApiLogging;

        // API Endpoints List
        _apiEndpoints.Clear();
        foreach (var endpoint in _settings.Api.Endpoints)
        {
            _apiEndpoints.Add(new ApiEndpoint
            {
                Name = endpoint.Name,
                BaseUrl = endpoint.BaseUrl,
                IsEnabled = endpoint.IsEnabled,
                ApiKey = endpoint.ApiKey,
                TimeoutMs = endpoint.TimeoutMs,
                MaxRetries = endpoint.MaxRetries
            });
        }
    }

    private void CollectUIValuesToSettings()
    {
        // Payment Gateway Settings
        _settings.PaymentGateways.DefaultGateway = DefaultGatewayCombo.SelectedItem as string ?? "";
        _settings.PaymentGateways.EnableTestMode = EnableTestModeCheck.IsChecked ?? false;

        // Payment Gateways List
        _settings.PaymentGateways.Gateways.Clear();
        foreach (var gateway in _paymentGateways)
        {
            _settings.PaymentGateways.Gateways.Add(new PaymentGateway
            {
                Name = gateway.Name,
                Provider = gateway.Provider,
                ApiKey = gateway.ApiKey,
                SecretKey = gateway.SecretKey,
                WebhookSecret = gateway.WebhookSecret,
                IsEnabled = gateway.IsEnabled,
                IsTestMode = gateway.IsTestMode
            });
        }

        // Webhook Settings
        _settings.Webhooks.EnableWebhooks = EnableWebhooksCheck.IsChecked ?? false;
        _settings.Webhooks.TimeoutMs = (int)WebhookTimeoutBox.Value;
        _settings.Webhooks.MaxRetries = (int)WebhookMaxRetriesBox.Value;

        // Webhook Endpoints List
        _settings.Webhooks.Endpoints.Clear();
        foreach (var endpoint in _webhookEndpoints)
        {
            _settings.Webhooks.Endpoints.Add(new WebhookEndpoint
            {
                Name = endpoint.Name,
                Url = endpoint.Url,
                Events = endpoint.Events,
                Secret = endpoint.Secret,
                IsEnabled = endpoint.IsEnabled
            });
        }

        // CRM Settings
        _settings.Crm.EnableCrmSync = EnableCrmSyncCheck.IsChecked ?? false;
        _settings.Crm.CrmProvider = CrmProviderCombo.SelectedItem as string ?? "";
        _settings.Crm.ApiEndpoint = CrmApiEndpointBox.Text;
        _settings.Crm.ApiKey = CrmApiKeyBox.Password;
        _settings.Crm.SyncCustomers = SyncCustomersCheck.IsChecked ?? false;
        _settings.Crm.SyncOrders = SyncOrdersCheck.IsChecked ?? false;
        _settings.Crm.SyncIntervalMinutes = (int)CrmSyncIntervalBox.Value;

        // API Settings
        _settings.Api.DefaultTimeoutMs = (int)ApiDefaultTimeoutBox.Value;
        _settings.Api.DefaultRetries = (int)ApiDefaultRetriesBox.Value;
        _settings.Api.EnableApiLogging = EnableApiLoggingCheck.IsChecked ?? false;

        // API Endpoints List
        _settings.Api.Endpoints.Clear();
        foreach (var endpoint in _apiEndpoints)
        {
            _settings.Api.Endpoints.Add(new ApiEndpoint
            {
                Name = endpoint.Name,
                BaseUrl = endpoint.BaseUrl,
                IsEnabled = endpoint.IsEnabled,
                ApiKey = endpoint.ApiKey,
                TimeoutMs = endpoint.TimeoutMs,
                MaxRetries = endpoint.MaxRetries
            });
        }
    }

    private void AddPaymentGateway_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _paymentGateways.Add(new PaymentGateway
        {
            Name = "New Gateway",
            Provider = "Stripe",
            IsEnabled = false,
            IsTestMode = true
        });
    }

    private void RemovePaymentGateway_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (PaymentGatewaysList.SelectedItem is PaymentGateway selectedGateway)
        {
            _paymentGateways.Remove(selectedGateway);
        }
    }

    private void ConfigureGatewayKeys_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // TODO: Implement gateway key configuration dialog
    }

    private void AddWebhookEndpoint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _webhookEndpoints.Add(new WebhookEndpoint
        {
            Name = "New Endpoint",
            Url = "https://example.com/webhook",
            Events = new List<string>(),
            IsEnabled = false
        });
    }

    private void RemoveWebhookEndpoint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (WebhookEndpointsList.SelectedItem is WebhookEndpoint selectedEndpoint)
        {
            _webhookEndpoints.Remove(selectedEndpoint);
        }
    }

    private void AddApiEndpoint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _apiEndpoints.Add(new ApiEndpoint
        {
            Name = "New Endpoint",
            BaseUrl = "https://api.example.com",
            IsEnabled = true,
            TimeoutMs = 10000,
            MaxRetries = 3
        });
    }

    private void RemoveApiEndpoint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ApiEndpointsList.SelectedItem is ApiEndpoint selectedEndpoint)
        {
            _apiEndpoints.Remove(selectedEndpoint);
        }
    }
}

