using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MagiDesk.Tests.TestInfrastructure;

namespace MagiDesk.Tests.ProductionTests;

[TestClass]
public class ProductionApiTests
{
    private HttpClient _httpClient;
    private readonly Dictionary<string, string> _apiUrls;

    public ProductionApiTests()
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
    [TestCategory("Production")]
    [TestCategory("HealthCheck")]
    public async Task HealthCheck_AllDeployedAPIs_ShouldReturnHealthy()
    {
        // Arrange
        var healthChecks = new List<Task<HttpResponseMessage>>();

        // Act - Check all deployed APIs simultaneously
        foreach (var apiUrl in _apiUrls.Values)
        {
            healthChecks.Add(_httpClient.GetAsync($"{apiUrl}/health"));
        }

        var responses = await Task.WhenAll(healthChecks);

        // Assert
        responses.Should().HaveCount(_apiUrls.Count);
        responses.All(r => r.IsSuccessStatusCode).Should().BeTrue();
        
        // Log the results
        foreach (var kvp in _apiUrls)
        {
            var response = responses[Array.IndexOf(_apiUrls.Values.ToArray(), kvp.Value)];
            Console.WriteLine($"{kvp.Key}: {response.StatusCode} - {kvp.Value}");
        }
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Tables")]
    public async Task TablesAPI_GetAllTables_ShouldReturnTables()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Tables API Response: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Tables")]
    public async Task TablesAPI_GetTableCounts_ShouldReturnCounts()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables/counts");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Table Counts: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Tables")]
    public async Task TablesAPI_GetActiveSessions_ShouldReturnSessions()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Active Sessions: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Menu")]
    public async Task MenuAPI_GetMenuItems_ShouldReturnItems()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Menu Items Response: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Inventory")]
    public async Task InventoryAPI_GetItems_ShouldReturnItems()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/api/inventory/items");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Inventory Items Response: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Inventory")]
    public async Task InventoryAPI_HealthCheck_ShouldReturnHealthy()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Inventory Health: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Payments")]
    public async Task PaymentAPI_ValidationEndpoint_ShouldReturnStatus()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Payment Validation: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Settings")]
    public async Task SettingsAPI_GetSettings_ShouldReturnSettings()
    {
        // Act
        var response = await _httpClient.GetAsync($"{_apiUrls["SettingsApi"]}/api/settings/frontend");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Settings Response: {responseContent}");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Performance")]
    public async Task Performance_ResponseTimes_ShouldBeAcceptable()
    {
        // Arrange
        var apiTests = new Dictionary<string, Func<Task<HttpResponseMessage>>>
        {
            ["Tables API"] = () => _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables"),
            ["Menu API"] = () => _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items"),
            ["Inventory API"] = () => _httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/api/inventory/items"),
            ["Payment API"] = () => _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate"),
            ["Settings API"] = () => _httpClient.GetAsync($"{_apiUrls["SettingsApi"]}/api/settings/frontend")
        };

        var results = new Dictionary<string, (bool success, long responseTime)>();

        // Act
        foreach (var test in apiTests)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await test.Value();
                stopwatch.Stop();
                results[test.Key] = (response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                results[test.Key] = (false, stopwatch.ElapsedMilliseconds);
                Console.WriteLine($"Error testing {test.Key}: {ex.Message}");
            }
        }

        // Assert
        foreach (var result in results)
        {
            result.Value.success.Should().BeTrue($"API {result.Key} should be accessible");
            result.Value.responseTime.Should().BeLessThan(10000, $"API {result.Key} should respond within 10 seconds");
            Console.WriteLine($"{result.Key}: {(result.Value.success ? "SUCCESS" : "FAILED")} - {result.Value.responseTime}ms");
        }
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("CrashPrevention")]
    public async Task CrashPrevention_InvalidEndpoints_ShouldReturnNotFound()
    {
        // Arrange
        var invalidEndpoints = new[]
        {
            $"{_apiUrls["TablesApi"]}/nonexistent",
            $"{_apiUrls["MenuApi"]}/api/nonexistent",
            $"{_apiUrls["InventoryApi"]}/api/nonexistent",
            $"{_apiUrls["PaymentApi"]}/api/nonexistent",
            $"{_apiUrls["SettingsApi"]}/api/nonexistent"
        };

        // Act & Assert
        foreach (var endpoint in invalidEndpoints)
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            Console.WriteLine($"Endpoint {endpoint}: {response.StatusCode} ✓");
        }
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Security")]
    public async Task Security_SQLInjectionAttempts_ShouldBeRejected()
    {
        // Arrange
        var injectionAttempts = TestDataFactory.CreateSQLInjectionAttempts();

        foreach (var injection in injectionAttempts)
        {
            var payload = new { Username = injection, Password = "test" };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Try to login with SQL injection
            var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/auth/login", content);

            // Assert - Should reject malicious input
            response.StatusCode.Should().BeOneOf(
                System.Net.HttpStatusCode.BadRequest,
                System.Net.HttpStatusCode.Unauthorized,
                System.Net.HttpStatusCode.NotFound);
            
            Console.WriteLine($"SQL Injection '{injection}': {response.StatusCode} ✓");
        }
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Security")]
    public async Task Security_XSSAttempts_ShouldBeSanitized()
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
                System.Net.HttpStatusCode.Unauthorized,
                System.Net.HttpStatusCode.NotFound);
            
            Console.WriteLine($"XSS Attempt '{xss}': {response.StatusCode} ✓");
        }
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("Concurrency")]
    public async Task Concurrency_MultipleRequests_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Send 20 concurrent requests to different APIs
        for (int i = 0; i < 20; i++)
        {
            var apiIndex = i % _apiUrls.Count;
            var apiUrl = _apiUrls.Values.ToArray()[apiIndex];
            tasks.Add(_httpClient.GetAsync($"{apiUrl}/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().HaveCount(20);
        responses.All(r => r.IsSuccessStatusCode).Should().BeTrue();
        
        Console.WriteLine($"Concurrent requests: {responses.Count(r => r.IsSuccessStatusCode)}/{responses.Length} successful");
    }

    [TestMethod]
    [TestCategory("Production")]
    [TestCategory("DataIntegrity")]
    public async Task DataIntegrity_DatabaseConnectivity_ShouldBeWorking()
    {
        // This test verifies that the APIs can connect to the database
        // by checking that they return data (not empty responses)
        
        var dataEndpoints = new[]
        {
            ($"{_apiUrls["TablesApi"]}/tables", "Tables"),
            ($"{_apiUrls["MenuApi"]}/api/menu/items", "Menu Items"),
            ($"{_apiUrls["InventoryApi"]}/api/inventory/items", "Inventory Items")
        };

        foreach (var (endpoint, name) in dataEndpoints)
        {
            // Act
            var response = await _httpClient.GetAsync(endpoint);
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"API should be accessible for {name}");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty($"Database should return data for {name}");
            
            Console.WriteLine($"{name}: {(response.IsSuccessStatusCode ? "SUCCESS" : "FAILED")} - Data length: {content.Length}");
        }
    }
}
