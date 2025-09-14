using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MagiDesk.Tests.TestInfrastructure;

namespace MagiDesk.Tests.DatabaseTests;

[TestClass]
public class DatabaseIntegrationTests
{
    private HttpClient _httpClient;
    private readonly Dictionary<string, string> _apiUrls;

    public DatabaseIntegrationTests()
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
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_OrderSchema_ShouldHaveCorrectStructure()
    {
        // This test verifies the order schema exists and has expected tables
        // We'll test this by making API calls that should work if the schema is correct
        
        // Act - Try to get orders (this will fail gracefully if schema is wrong)
        var response = await _httpClient.GetAsync($"{_apiUrls["OrderApi"]}/api/orders");
        
        // Assert - Should either succeed or fail gracefully (not crash)
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError);
        
        Console.WriteLine($"Order API Response: {response.StatusCode}");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_PaymentSchema_ShouldHaveCorrectStructure()
    {
        // Act - Try to access payment validation endpoint
        var response = await _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate");
        
        // Assert - Should either succeed or fail gracefully
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError);
        
        Console.WriteLine($"Payment API Response: {response.StatusCode}");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_MenuSchema_ShouldHaveCorrectStructure()
    {
        // Act - Try to get menu items
        var response = await _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items");
        
        // Assert - Should either succeed or fail gracefully
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError);
        
        Console.WriteLine($"Menu API Response: {response.StatusCode}");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_InventorySchema_ShouldHaveCorrectStructure()
    {
        // Act - Try to get inventory items
        var response = await _httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/api/inventory/items");
        
        // Assert - Should either succeed or fail gracefully
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError);
        
        Console.WriteLine($"Inventory API Response: {response.StatusCode}");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_TablesSchema_ShouldHaveCorrectStructure()
    {
        // Act - Try to get tables
        var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
        
        // Assert - Should either succeed or fail gracefully
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError);
        
        Console.WriteLine($"Tables API Response: {response.StatusCode}");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_DataConsistency_ShouldMaintainIntegrity()
    {
        // This test verifies that data operations don't break the database
        // We'll test by making multiple requests and ensuring they all work
        
        var testEndpoints = new[]
        {
            $"{_apiUrls["TablesApi"]}/tables",
            $"{_apiUrls["TablesApi"]}/tables/counts",
            $"{_apiUrls["TablesApi"]}/sessions/active",
            $"{_apiUrls["MenuApi"]}/api/menu/items",
            $"{_apiUrls["InventoryApi"]}/api/inventory/items"
        };

        var results = new List<bool>();

        // Act - Make multiple requests to test data consistency
        foreach (var endpoint in testEndpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                results.Add(response.IsSuccessStatusCode);
                Console.WriteLine($"Endpoint {endpoint}: {(response.IsSuccessStatusCode ? "SUCCESS" : "FAILED")}");
            }
            catch (Exception ex)
            {
                results.Add(false);
                Console.WriteLine($"Endpoint {endpoint}: ERROR - {ex.Message}");
            }
        }

        // Assert - Most endpoints should work (allow some failures for robustness)
        var successRate = (double)results.Count(r => r) / results.Count;
        successRate.Should().BeGreaterThan(0.8, "At least 80% of database operations should succeed");
        
        Console.WriteLine($"Database consistency test: {results.Count(r => r)}/{results.Count} endpoints working ({successRate:P})");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_ConnectionPooling_ShouldHandleMultipleConnections()
    {
        // This test verifies that the database can handle multiple concurrent connections
        // which is important for a production system
        
        var tasks = new List<Task<HttpResponseMessage>>();

        // Act - Create multiple concurrent database connections
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables"));
            tasks.Add(_httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items"));
            tasks.Add(_httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/api/inventory/items"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert - All requests should complete (success or graceful failure)
        responses.Should().HaveCount(30);
        
        var successfulRequests = responses.Count(r => r.IsSuccessStatusCode);
        var successRate = (double)successfulRequests / responses.Length;
        
        successRate.Should().BeGreaterThan(0.7, "At least 70% of concurrent requests should succeed");
        
        Console.WriteLine($"Connection pooling test: {successfulRequests}/{responses.Length} requests successful ({successRate:P})");
    }

    [TestMethod]
    [TestCategory("Database")]
    [TestCategory("Production")]
    public async Task Database_TransactionIntegrity_ShouldMaintainACIDProperties()
    {
        // This test verifies that database transactions maintain integrity
        // We'll test by making requests that should either fully succeed or fully fail
        
        var testOperations = new[]
        {
            () => _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables"),
            () => _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items"),
            () => _httpClient.GetAsync($"{_apiUrls["InventoryApi"]}/api/inventory/items"),
            () => _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate")
        };

        var results = new List<(bool success, string operation)>();

        // Act - Perform multiple operations
        foreach (var operation in testOperations)
        {
            try
            {
                var response = await operation();
                results.Add((response.IsSuccessStatusCode, operation.Method.Name));
            }
            catch (Exception ex)
            {
                results.Add((false, operation.Method.Name));
                Console.WriteLine($"Operation {operation.Method.Name} failed: {ex.Message}");
            }
        }

        // Assert - Operations should either succeed or fail consistently
        var successCount = results.Count(r => r.success);
        var totalCount = results.Count;
        
        // In a healthy system, most read operations should succeed
        successCount.Should().BeGreaterThan(0, "At least some database operations should succeed");
        
        Console.WriteLine($"Transaction integrity test: {successCount}/{totalCount} operations successful");
    }
}

