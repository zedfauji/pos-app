using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using SettingsApi.Services;
using MagiDesk.Shared.DTOs.Settings;
using FluentAssertions;
using Xunit;

namespace SettingsApi.Tests.Services;

public class HierarchicalSettingsServiceTests : IDisposable
{
    private readonly Mock<ILogger<HierarchicalSettingsService>> _mockLogger;
    private readonly Mock<NpgsqlDataSource> _mockDataSource;
    private readonly Mock<NpgsqlConnection> _mockConnection;
    private readonly Mock<NpgsqlTransaction> _mockTransaction;
    private readonly HierarchicalSettingsService _service;

    public HierarchicalSettingsServiceTests()
    {
        _mockLogger = new Mock<ILogger<HierarchicalSettingsService>>();
        _mockDataSource = new Mock<NpgsqlDataSource>();
        _mockConnection = new Mock<NpgsqlConnection>();
        _mockTransaction = new Mock<NpgsqlTransaction>();

        _mockDataSource.Setup(ds => ds.OpenConnectionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(_mockConnection.Object);
        
        _mockConnection.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(_mockTransaction.Object);

        _service = new HierarchicalSettingsService(_mockDataSource.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSettingsAsync_ShouldReturnDefaultSettings_WhenNoSettingsExist()
    {
        // Arrange
        var mockReader = new Mock<NpgsqlDataReader>();
        mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        var mockCommand = new Mock<NpgsqlCommand>();
        mockCommand.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(mockReader.Object);
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Act
        var result = await _service.GetSettingsAsync("test-host");

        // Assert
        result.Should().NotBeNull();
        result.General.Should().NotBeNull();
        result.Pos.Should().NotBeNull();
        result.Inventory.Should().NotBeNull();
        result.Customers.Should().NotBeNull();
        result.Payments.Should().NotBeNull();
        result.Printers.Should().NotBeNull();
        result.Notifications.Should().NotBeNull();
        result.Security.Should().NotBeNull();
        result.Integrations.Should().NotBeNull();
        result.System.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveSettingsAsync_ShouldSaveAllCategories_WhenValidSettingsProvided()
    {
        // Arrange
        var settings = CreateTestSettings();
        var mockCommand = new Mock<NpgsqlCommand>();
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.SaveSettingsAsync(settings, "test-host");

        // Assert
        result.Should().BeTrue();
        mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.AtLeast(9));
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateSettingsAsync_ShouldReturnTrue_WhenAllSettingsAreValid()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var result = await _service.ValidateSettingsAsync(settings);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSettingsAsync_ShouldReturnFalse_WhenSettingsAreInvalid()
    {
        // Arrange
        var settings = CreateTestSettings();
        settings.General.BusinessName = ""; // Invalid empty business name
        settings.Pos.DefaultTaxRate = -1; // Invalid negative tax rate

        // Act
        var result = await _service.ValidateSettingsAsync(settings);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExportSettingsToJsonAsync_ShouldReturnValidJson_WhenSettingsExist()
    {
        // Arrange
        var settings = CreateTestSettings();
        var mockReader = new Mock<NpgsqlDataReader>();
        mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        var mockCommand = new Mock<NpgsqlCommand>();
        mockCommand.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(mockReader.Object);
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Act
        var result = await _service.ExportSettingsToJsonAsync("test-host");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("General");
        result.Should().Contain("Pos");
        result.Should().Contain("Inventory");
    }

    [Fact]
    public async Task ImportSettingsFromJsonAsync_ShouldReturnTrue_WhenValidJsonProvided()
    {
        // Arrange
        var settings = CreateTestSettings();
        var json = System.Text.Json.JsonSerializer.Serialize(settings);
        
        var mockCommand = new Mock<NpgsqlCommand>();
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportSettingsFromJsonAsync(json, "test-host");

        // Assert
        result.Should().BeTrue();
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportSettingsFromJsonAsync_ShouldReturnFalse_WhenInvalidJsonProvided()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = await _service.ImportSettingsFromJsonAsync(invalidJson, "test-host");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateUserAccessAsync_ShouldReturnTrue_WhenUserHasAccess()
    {
        // Arrange
        var mockReader = new Mock<NpgsqlDataReader>();
        mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockReader.Setup(r => r.GetBoolean(It.IsAny<int>())).Returns(true);
        
        var mockCommand = new Mock<NpgsqlCommand>();
        mockCommand.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Act
        var result = await _service.ValidateUserAccessAsync("general", "user1", "view");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserAccessibleCategoriesAsync_ShouldReturnCategories_WhenUserHasAccess()
    {
        // Arrange
        var mockReader = new Mock<NpgsqlDataReader>();
        mockReader.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true)
                  .ReturnsAsync(true)
                  .ReturnsAsync(false);
        mockReader.Setup(r => r.GetString(It.IsAny<int>())).Returns("general");
        
        var mockCommand = new Mock<NpgsqlCommand>();
        mockCommand.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(mockReader.Object);
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Act
        var result = await _service.GetUserAccessibleCategoriesAsync("user1");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("general");
    }

    [Fact]
    public async Task BulkResetSettingsAsync_ShouldResetSpecifiedCategories_WhenValidCategoriesProvided()
    {
        // Arrange
        var categories = new List<string> { "general", "pos" };
        var mockCommand = new Mock<NpgsqlCommand>();
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);
        _mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkResetSettingsAsync(categories, "test-host");

        // Assert
        result.Should().BeTrue();
        mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        _mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkExportSettingsAsync_ShouldExportSpecifiedCategories_WhenValidCategoriesProvided()
    {
        // Arrange
        var categories = new List<string> { "general", "pos" };
        var mockReader = new Mock<NpgsqlDataReader>();
        mockReader.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        var mockCommand = new Mock<NpgsqlCommand>();
        mockCommand.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(mockReader.Object);
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Act
        var result = await _service.BulkExportSettingsAsync(categories, "test-host");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("General");
        result.Should().Contain("Pos");
    }

    [Fact]
    public async Task BulkValidateSettingsAsync_ShouldReturnValidationResults_WhenSettingsProvided()
    {
        // Arrange
        var settings = CreateTestSettings();

        // Act
        var result = await _service.BulkValidateSettingsAsync(settings);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // No validation errors for valid settings
    }

    [Fact]
    public async Task GetAuditHistoryAsync_ShouldReturnAuditEntries_WhenHistoryExists()
    {
        // Arrange
        var mockReader = new Mock<NpgsqlDataReader>();
        mockReader.SetupSequence(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true)
                  .ReturnsAsync(false);
        
        mockReader.Setup(r => r.GetInt64(It.IsAny<int>())).Returns(1);
        mockReader.Setup(r => r.GetString(It.IsAny<int>())).Returns("test-host");
        mockReader.Setup(r => r.GetDateTime(It.IsAny<int>())).Returns(DateTime.UtcNow);
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        
        var mockCommand = new Mock<NpgsqlCommand>();
        mockCommand.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(mockReader.Object);
        
        _mockConnection.Setup(c => c.CreateCommand()).Returns(mockCommand.Object);

        // Act
        var result = await _service.GetAuditHistoryAsync("test-host");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
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

    public void Dispose()
    {
        _mockConnection?.Dispose();
        _mockTransaction?.Dispose();
        _mockDataSource?.Dispose();
    }
}
