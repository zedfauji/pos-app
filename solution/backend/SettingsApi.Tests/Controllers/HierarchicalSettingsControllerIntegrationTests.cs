using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using SettingsApi.Controllers;
using MagiDesk.Shared.DTOs.Settings;

namespace SettingsApi.Tests.Controllers;

public class HierarchicalSettingsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HierarchicalSettingsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add test services here if needed
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSettings_ShouldReturnOk_WhenValidHostProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings?host=test-host");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSettingsCategory_ShouldReturnOk_WhenValidCategoryProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/general?host=test-host");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSettingsCategory_ShouldReturnNotFound_WhenInvalidCategoryProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/invalid?host=test-host");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutSettings_ShouldReturnNoContent_WhenValidSettingsProvided()
    {
        // Arrange
        var settings = CreateTestSettings();
        var json = JsonSerializer.Serialize(settings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/api/v2/settings?host=test-host", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PutSettings_ShouldReturnBadRequest_WhenInvalidSettingsProvided()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/api/v2/settings?host=test-host", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutSettingsCategory_ShouldReturnNoContent_WhenValidCategorySettingsProvided()
    {
        // Arrange
        var generalSettings = new GeneralSettings
        {
            BusinessName = "Test Business",
            BusinessAddress = "123 Test St",
            BusinessPhone = "555-1234",
            BusinessEmail = "test@business.com",
            Theme = "Light",
            Language = "en-US",
            TimeZone = "UTC",
            Currency = "USD"
        };
        
        var json = JsonSerializer.Serialize(generalSettings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/api/v2/settings/general?host=test-host", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetAuditHistory_ShouldReturnOk_WhenValidHostProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/audit/history?host=test-host&limit=10");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAuditHistoryByUser_ShouldReturnOk_WhenValidUserIdProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/audit/history/user/test-user?host=test-host&limit=10");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateUserAccess_ShouldReturnOk_WhenValidParametersProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/access/validate?category=general&userId=test-user&action=view");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserAccessibleCategories_ShouldReturnOk_WhenValidUserIdProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/access/categories/test-user");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetUserCategoryAccess_ShouldReturnOk_WhenValidRequestProvided()
    {
        // Arrange
        var request = new SetUserAccessRequest
        {
            UserId = "test-user",
            Category = "general",
            CanView = true,
            CanEdit = false
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v2/settings/access/set", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BulkResetSettings_ShouldReturnOk_WhenValidCategoriesProvided()
    {
        // Arrange
        var request = new BulkResetRequest
        {
            Categories = new List<string> { "general", "pos" }
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v2/settings/bulk/reset?host=test-host", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BulkExportSettings_ShouldReturnOk_WhenValidCategoriesProvided()
    {
        // Arrange
        var request = new BulkExportRequest
        {
            Categories = new List<string> { "general", "pos" }
        };
        
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v2/settings/bulk/export?host=test-host", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        responseContent.Should().Contain("General");
        responseContent.Should().Contain("Pos");
    }

    [Fact]
    public async Task BulkValidateSettings_ShouldReturnOk_WhenValidSettingsProvided()
    {
        // Arrange
        var settings = CreateTestSettings();
        var json = JsonSerializer.Serialize(settings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v2/settings/bulk/validate", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportSettings_ShouldReturnOk_WhenValidHostProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/v2/settings/export?host=test-host");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("General");
        content.Should().Contain("Pos");
    }

    [Fact]
    public async Task ImportSettings_ShouldReturnOk_WhenValidJsonProvided()
    {
        // Arrange
        var settings = CreateTestSettings();
        var json = JsonSerializer.Serialize(settings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v2/settings/import?host=test-host", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TestConnections_ShouldReturnOk_WhenCalled()
    {
        // Act
        var response = await _client.PostAsync("/api/v2/settings/test-connections?host=test-host", null);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    private static HierarchicalSettings CreateTestSettings()
    {
        return new HierarchicalSettings
        {
            General = new GeneralSettings
            {
                BusinessName = "Test Business",
                BusinessAddress = "123 Test St",
                BusinessPhone = "555-1234",
                BusinessEmail = "test@business.com",
                Theme = "Light",
                Language = "en-US",
                TimeZone = "UTC",
                Currency = "USD"
            },
            Pos = new PosSettings
            {
                DefaultTaxRate = 8.5m,
                AutoOpenCashDrawer = true,
                RequireCustomerForOrders = false,
                DefaultOrderType = "DineIn",
                MaxTableCapacity = 8,
                EnableTableService = true,
                AutoAssignTables = false
            },
            Inventory = new InventorySettings
            {
                LowStockThreshold = 10,
                AutoReorderEnabled = true,
                DefaultVendor = "Test Vendor",
                TrackInventory = true,
                AllowNegativeStock = false,
                ReorderPoint = 5,
                MaxStockLevel = 100
            },
            Customers = new CustomersSettings
            {
                EnableMembershipTiers = true,
                DefaultTier = "Bronze",
                EnableWalletSystem = true,
                WalletMinBalance = 0m,
                WalletMaxBalance = 1000m,
                EnableLoyaltyProgram = true,
                PointsPerDollar = 1,
                PointsRedemptionRate = 100
            },
            Payments = new PaymentsSettings
            {
                EnableCashPayments = true,
                EnableCardPayments = true,
                EnableDigitalWallets = true,
                EnableSplitPayments = true,
                MaxSplitCount = 4,
                DefaultTipPercentage = 18m,
                EnableCustomTips = true,
                RequireReceiptPrint = true
            },
            Printers = new PrinterSettings
            {
                DefaultReceiptPrinter = "Receipt Printer",
                DefaultKitchenPrinter = "Kitchen Printer",
                ReceiptPaperSize = "80mm",
                AutoPrintReceipts = true,
                PrintOrderCopies = 2,
                EnablePrintPreview = true
            },
            Notifications = new NotificationsSettings
            {
                EnableEmailNotifications = true,
                EnableSmsNotifications = false,
                EnablePushNotifications = true,
                EmailFromAddress = "noreply@business.com",
                SmsProvider = "Twilio",
                PushNotificationService = "Firebase"
            },
            Security = new SecuritySettings
            {
                EnableRbac = true,
                SessionTimeoutMinutes = 30,
                MaxLoginAttempts = 5,
                PasswordMinLength = 8,
                RequirePasswordComplexity = true,
                EnableAuditLogging = true,
                EnableTwoFactorAuth = false
            },
            Integrations = new IntegrationsSettings
            {
                EnablePaymentGateways = true,
                DefaultPaymentGateway = "Stripe",
                EnableWebhooks = true,
                WebhookSecret = "test-secret",
                EnableCrmSync = false,
                CrmProvider = "Salesforce",
                ApiRateLimit = 1000
            },
            System = new SystemSettings
            {
                LogLevel = "Information",
                EnablePerformanceMonitoring = true,
                BackupRetentionDays = 30,
                AutoBackupEnabled = true,
                BackupSchedule = "Daily",
                EnableSystemMaintenance = true,
                MaintenanceWindow = "02:00-04:00"
            }
        };
    }
}
