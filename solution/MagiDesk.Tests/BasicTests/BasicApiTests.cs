using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MagiDesk.Tests.TestInfrastructure;

namespace MagiDesk.Tests.BasicTests;

[TestClass]
public class BasicApiTests
{
    private HttpClient _httpClient;
    private readonly Dictionary<string, string> _apiUrls;

    public BasicApiTests()
    {
        _apiUrls = TestConfiguration.GetTestApiUrls();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [TestInitialize]
    public void Setup()
    {
        _httpClient?.Dispose();
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    [TestCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_OrderApi_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["OrderApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_PaymentApi_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_MenuApi_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_InventoryApi_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_SettingsApi_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["SettingsApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_TablesApi_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("Login")]
    public async Task LoginFlow_ValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var loginRequest = TestDataFactory.CreateTestLoginRequest();
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/auth/login", content);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("Login")]
    public async Task LoginFlow_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new { Username = "invalid", Password = "invalid" };
        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("Tables")]
    public async Task TableFlow_GetAllTables_ShouldReturnTables()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("Tables")]
    public async Task TableFlow_GetTableCounts_ShouldReturnCounts()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables/counts");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("Payments")]
    public async Task PaymentFlow_ValidationEndpoint_ShouldReturnStatus()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("CrashPrevention")]
    public async Task CrashPrevention_InvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var json = TestDataFactory.CreateInvalidJson();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("CrashPrevention")]
    public async Task CrashPrevention_EmptyJson_ShouldReturnBadRequest()
    {
        // Arrange
        var json = TestDataFactory.CreateEmptyJson();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/orders", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("CrashPrevention")]
    public async Task CrashPrevention_ConcurrentRequests_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send 10 concurrent health check requests
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_httpClient.GetAsync($"{_apiUrls["OrderApi"]}/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(10);
        responses.All(r => r.IsSuccessStatusCode).Should().BeTrue();
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("CrashPrevention")]
    public async Task CrashPrevention_SQLInjectionAttempts_ShouldBeRejected()
    {
        // Arrange
        var injectionAttempts = TestDataFactory.CreateSQLInjectionAttempts();

        foreach (var injection in injectionAttempts)
        {
            var payload = new { Username = injection, Password = "test" };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/auth/login", content);

            // Assert - Should reject malicious input
            response.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.BadRequest,
                System.Net.HttpStatusCode.Unauthorized);
        }
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("CrashPrevention")]
    public async Task CrashPrevention_XSSAttempts_ShouldBeSanitized()
    {
        // Arrange
        var xssAttempts = TestDataFactory.CreateXSSAttempts();

        foreach (var xss in xssAttempts)
        {
            var payload = new { Username = xss, Password = "test" };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/auth/login", content);

            // Assert - Should sanitize malicious input
            response.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.BadRequest,
                System.Net.HttpStatusCode.Unauthorized);
        }
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("Performance")]
    public async Task Performance_ResponseTime_ShouldBeAcceptable()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["OrderApi"]}/health");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should respond within 5 seconds
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("ErrorHandling")]
    public async Task ErrorHandling_NonExistentEndpoint_ShouldReturnNotFound()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["OrderApi"]}/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [TestMethod]
    [TestCategory("Critical")]
    [TestCategory("ErrorHandling")]
    public async Task ErrorHandling_MalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        var malformedJsonArray = TestDataFactory.CreateMalformedJsonArray();

        foreach (var malformedJson in malformedJsonArray)
        {
            var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/orders", content);

            // Assert - Should handle gracefully
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }
}




