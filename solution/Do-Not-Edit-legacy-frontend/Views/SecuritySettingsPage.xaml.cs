using MagiDesk.Shared.DTOs.Settings;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views;

public sealed partial class SecuritySettingsPage : Page, ISettingsSubPage
{
    private SecuritySettings _settings = new();

    public SecuritySettingsPage()
    {
        this.InitializeComponent();
    }

    public void SetSubCategory(string subCategoryKey)
    {
        // Sub-category logic can be added here if needed
    }

    public void SetSettings(object settings)
    {
        if (settings is SecuritySettings securitySettings)
        {
            _settings = securitySettings;
            LoadSettingsToUI();
        }
    }

    public SecuritySettings GetCurrentSettings()
    {
        CollectUIValuesToSettings();
        return _settings;
    }

    private void LoadSettingsToUI()
    {
        // RBAC Settings
        EnforceRolePermissionsSwitch.IsOn = _settings.Rbac.EnforceRolePermissions;
        AllowRoleInheritanceSwitch.IsOn = _settings.Rbac.AllowRoleInheritance;
        RequireManagerOverrideForRestrictedSwitch.IsOn = _settings.Rbac.RequireManagerOverride;

        // Login Settings
        RequireStrongPasswordsSwitch.IsOn = _settings.Login.RequireStrongPasswords;
        EnableTwoFactorSwitch.IsOn = _settings.Login.EnableTwoFactor;
        MaxLoginAttemptsBox.Value = _settings.Login.MaxLoginAttempts;
        LockoutDurationMinutesBox.Value = _settings.Login.LockoutDurationMinutes;
        PasswordExpiryDaysBox.Value = _settings.Login.PasswordExpiryDays;

        // Session Settings
        EnableAutoLogoutSwitch.IsOn = _settings.Sessions.EnableAutoLogout;
        RequireReauthForSensitiveSwitch.IsOn = _settings.Sessions.RequireReauthForSensitive;
        SessionTimeoutMinutesBox.Value = _settings.Sessions.SessionTimeoutMinutes;
        MaxConcurrentSessionsBox.Value = _settings.Sessions.MaxConcurrentSessions;

        // Audit Settings
        EnableAuditLoggingSwitch.IsOn = _settings.Audit.EnableAuditLogging;
        LogUserActionsSwitch.IsOn = _settings.Audit.LogUserActions;
        LogSystemEventsSwitch.IsOn = _settings.Audit.LogSystemEvents;
        LogDataChangesSwitch.IsOn = _settings.Audit.LogDataChanges;
        RetentionDaysBox.Value = _settings.Audit.RetentionDays;
    }

    private void CollectUIValuesToSettings()
    {
        // RBAC Settings
        _settings.Rbac.EnforceRolePermissions = EnforceRolePermissionsSwitch.IsOn;
        _settings.Rbac.AllowRoleInheritance = AllowRoleInheritanceSwitch.IsOn;
        _settings.Rbac.RequireManagerOverride = RequireManagerOverrideForRestrictedSwitch.IsOn;

        // Login Settings
        _settings.Login.RequireStrongPasswords = RequireStrongPasswordsSwitch.IsOn;
        _settings.Login.EnableTwoFactor = EnableTwoFactorSwitch.IsOn;
        _settings.Login.MaxLoginAttempts = (int)MaxLoginAttemptsBox.Value;
        _settings.Login.LockoutDurationMinutes = (int)LockoutDurationMinutesBox.Value;
        _settings.Login.PasswordExpiryDays = (int)PasswordExpiryDaysBox.Value;

        // Session Settings
        _settings.Sessions.EnableAutoLogout = EnableAutoLogoutSwitch.IsOn;
        _settings.Sessions.RequireReauthForSensitive = RequireReauthForSensitiveSwitch.IsOn;
        _settings.Sessions.SessionTimeoutMinutes = (int)SessionTimeoutMinutesBox.Value;
        _settings.Sessions.MaxConcurrentSessions = (int)MaxConcurrentSessionsBox.Value;

        // Audit Settings
        _settings.Audit.EnableAuditLogging = EnableAuditLoggingSwitch.IsOn;
        _settings.Audit.LogUserActions = LogUserActionsSwitch.IsOn;
        _settings.Audit.LogSystemEvents = LogSystemEventsSwitch.IsOn;
        _settings.Audit.LogDataChanges = LogDataChangesSwitch.IsOn;
        _settings.Audit.RetentionDays = (int)RetentionDaysBox.Value;
    }
}
