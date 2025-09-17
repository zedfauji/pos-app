using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class SystemSettingsPage : Page, ISettingsSubPage
{
    private SystemSettings _settings = new();

    public SystemSettingsPage()
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
        if (settings is SystemSettings systemSettings)
        {
            _settings = systemSettings;
            LoadSettingsToUI();
        }
    }

    public SystemSettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void InitializeUI()
    {
        // Set default values
        MaxLogFileSizeBox.Value = 10;
        MaxLogFilesBox.Value = 10;
        LogRetentionDaysBox.Value = 30;
        HeartbeatIntervalBox.Value = 5;
        CleanupIntervalBox.Value = 60;
        BackupIntervalBox.Value = 240;
        ConnectionPoolSizeBox.Value = 50;
        CacheExpirationBox.Value = 15;
        MaxConcurrentRequestsBox.Value = 1000;

        // Populate log level combo
        LogLevelCombo.ItemsSource = new List<string> 
        { 
            "Trace", "Debug", "Information", "Warning", "Error", "Critical" 
        };
    }

    private void LoadSettingsToUI()
    {
        // Logging Settings
        LogLevelCombo.SelectedItem = _settings.Logging.LogLevel;
        EnableFileLoggingCheck.IsChecked = _settings.Logging.EnableFileLogging;
        EnableConsoleLoggingCheck.IsChecked = _settings.Logging.EnableConsoleLogging;
        LogFilePathBox.Text = _settings.Logging.LogFilePath;
        MaxLogFileSizeBox.Value = _settings.Logging.MaxLogFileSizeMB;
        MaxLogFilesBox.Value = _settings.Logging.MaxLogFiles;
        LogRetentionDaysBox.Value = _settings.Logging.LogRetentionDays;

        // Tracing Settings
        EnableTracingCheck.IsChecked = _settings.Tracing.EnableTracing;
        TraceApiCallsCheck.IsChecked = _settings.Tracing.TraceApiCalls;
        TraceDbQueriesCheck.IsChecked = _settings.Tracing.TraceDbQueries;
        TraceUserActionsCheck.IsChecked = _settings.Tracing.TraceUserActions;
        TracingEndpointBox.Text = _settings.Tracing.TracingEndpoint;

        // Background Jobs Settings
        EnableBackgroundJobsCheck.IsChecked = _settings.BackgroundJobs.EnableBackgroundJobs;
        HeartbeatIntervalBox.Value = _settings.BackgroundJobs.HeartbeatIntervalMinutes;
        CleanupIntervalBox.Value = _settings.BackgroundJobs.CleanupIntervalMinutes;
        BackupIntervalBox.Value = _settings.BackgroundJobs.BackupIntervalMinutes;

        // Performance Settings
        ConnectionPoolSizeBox.Value = _settings.Performance.DatabaseConnectionPoolSize;
        CacheExpirationBox.Value = _settings.Performance.CacheExpirationMinutes;
        EnableResponseCompressionCheck.IsChecked = _settings.Performance.EnableResponseCompression;
        EnableResponseCachingCheck.IsChecked = _settings.Performance.EnableResponseCaching;
        MaxConcurrentRequestsBox.Value = _settings.Performance.MaxConcurrentRequests;
    }

    private void CollectUIValuesToSettings()
    {
        // Logging Settings
        _settings.Logging.LogLevel = LogLevelCombo.SelectedItem as string ?? "Information";
        _settings.Logging.EnableFileLogging = EnableFileLoggingCheck.IsChecked ?? false;
        _settings.Logging.EnableConsoleLogging = EnableConsoleLoggingCheck.IsChecked ?? false;
        _settings.Logging.LogFilePath = LogFilePathBox.Text;
        _settings.Logging.MaxLogFileSizeMB = (int)MaxLogFileSizeBox.Value;
        _settings.Logging.MaxLogFiles = (int)MaxLogFilesBox.Value;
        _settings.Logging.LogRetentionDays = (int)LogRetentionDaysBox.Value;

        // Tracing Settings
        _settings.Tracing.EnableTracing = EnableTracingCheck.IsChecked ?? false;
        _settings.Tracing.TraceApiCalls = TraceApiCallsCheck.IsChecked ?? false;
        _settings.Tracing.TraceDbQueries = TraceDbQueriesCheck.IsChecked ?? false;
        _settings.Tracing.TraceUserActions = TraceUserActionsCheck.IsChecked ?? false;
        _settings.Tracing.TracingEndpoint = TracingEndpointBox.Text;

        // Background Jobs Settings
        _settings.BackgroundJobs.EnableBackgroundJobs = EnableBackgroundJobsCheck.IsChecked ?? false;
        _settings.BackgroundJobs.HeartbeatIntervalMinutes = (int)HeartbeatIntervalBox.Value;
        _settings.BackgroundJobs.CleanupIntervalMinutes = (int)CleanupIntervalBox.Value;
        _settings.BackgroundJobs.BackupIntervalMinutes = (int)BackupIntervalBox.Value;

        // Performance Settings
        _settings.Performance.DatabaseConnectionPoolSize = (int)ConnectionPoolSizeBox.Value;
        _settings.Performance.CacheExpirationMinutes = (int)CacheExpirationBox.Value;
        _settings.Performance.EnableResponseCompression = EnableResponseCompressionCheck.IsChecked ?? false;
        _settings.Performance.EnableResponseCaching = EnableResponseCachingCheck.IsChecked ?? false;
        _settings.Performance.MaxConcurrentRequests = (int)MaxConcurrentRequestsBox.Value;
    }
}

