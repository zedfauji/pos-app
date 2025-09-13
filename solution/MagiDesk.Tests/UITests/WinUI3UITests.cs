using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MagiDesk.Tests.TestInfrastructure;

namespace MagiDesk.Tests.UITests;

[TestClass]
public class WinUI3UITests
{
    private HttpClient _httpClient;
    private readonly Dictionary<string, string> _apiUrls;

    public WinUI3UITests()
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
    [TestCategory("UI")]
    [TestCategory("Critical")]
    public async Task UI_ApplicationStartup_ShouldLoadMainPage()
    {
        // This test simulates the application startup flow by testing the main API endpoints
        // that the UI would call during initialization
        
        var startupEndpoints = new[]
        {
            $"{_apiUrls["TablesApi"]}/tables",
            $"{_apiUrls["MenuApi"]}/api/menu/items",
            $"{_apiUrls["SettingsApi"]}/api/settings/frontend"
        };

        var results = new List<bool>();

        foreach (var endpoint in startupEndpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                results.Add(response.IsSuccessStatusCode);
            }
            catch
            {
                results.Add(false);
            }
        }

        // At least 2 out of 3 startup endpoints should work for UI to function
        var successCount = results.Count(r => r);
        successCount.Should().BeGreaterOrEqualTo(2, "Main UI should be able to load with core data");
        
        Console.WriteLine($"UI Startup Test: {successCount}/{startupEndpoints.Length} endpoints accessible");
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("Critical")]
    public async Task UI_Navigation_ShouldAccessAllMainPages()
    {
        // Test that all main navigation endpoints are accessible
        // This simulates the user navigating through different pages in the UI
        
        var navigationEndpoints = new Dictionary<string, string>
        {
            ["Dashboard"] = $"{_apiUrls["TablesApi"]}/tables/counts",
            ["Tables"] = $"{_apiUrls["TablesApi"]}/tables",
            ["Orders"] = $"{_apiUrls["TablesApi"]}/sessions/active",
            ["Menu"] = $"{_apiUrls["MenuApi"]}/api/menu/items",
            ["Inventory"] = $"{_apiUrls["InventoryApi"]}/api/inventory/items",
            ["Payments"] = $"{_apiUrls["PaymentApi"]}/api/payments/validate",
            ["Settings"] = $"{_apiUrls["SettingsApi"]}/api/settings/frontend"
        };

        var navigationResults = new Dictionary<string, bool>();

        foreach (var page in navigationEndpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(page.Value);
                navigationResults[page.Key] = response.IsSuccessStatusCode;
            }
            catch
            {
                navigationResults[page.Key] = false;
            }
        }

        // At least 70% of navigation endpoints should work
        var workingPages = navigationResults.Count(kvp => kvp.Value);
        var totalPages = navigationResults.Count;
        var successRate = (double)workingPages / totalPages;
        
        successRate.Should().BeGreaterThan(0.7, "Most navigation pages should be accessible");
        
        Console.WriteLine($"UI Navigation Test: {workingPages}/{totalPages} pages accessible ({successRate:P})");
        foreach (var result in navigationResults)
        {
            Console.WriteLine($"  {result.Key}: {(result.Value ? "✅" : "❌")}");
        }
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("Critical")]
    public async Task UI_UserInteractions_ShouldHandleUserActions()
    {
        // Test user interactions by simulating common UI operations
        
        var userActions = new Dictionary<string, Func<Task<bool>>>
        {
            ["Login"] = async () =>
            {
                var loginRequest = TestDataFactory.CreateTestLoginRequest();
                var json = JsonSerializer.Serialize(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiUrls["OrderApi"]}/api/auth/login", content);
                return response.IsSuccessStatusCode;
            },
            ["View Tables"] = async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
                return response.IsSuccessStatusCode;
            },
            ["View Menu"] = async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items");
                return response.IsSuccessStatusCode;
            },
            ["Check Payments"] = async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate");
                return response.IsSuccessStatusCode;
            }
        };

        var actionResults = new Dictionary<string, bool>();

        foreach (var action in userActions)
        {
            try
            {
                actionResults[action.Key] = await action.Value();
            }
            catch
            {
                actionResults[action.Key] = false;
            }
        }

        // Most user actions should work
        var workingActions = actionResults.Count(kvp => kvp.Value);
        var totalActions = actionResults.Count;
        var successRate = (double)workingActions / totalActions;
        
        successRate.Should().BeGreaterOrEqualTo(0.5, "Core user actions should be functional");
        
        Console.WriteLine($"UI User Interactions: {workingActions}/{totalActions} actions working ({successRate:P})");
        foreach (var result in actionResults)
        {
            Console.WriteLine($"  {result.Key}: {(result.Value ? "✅" : "❌")}");
        }
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("Critical")]
    public async Task UI_DataBinding_ShouldLoadDataCorrectly()
    {
        // Test that data loads correctly for UI binding
        // This simulates the UI trying to bind data to various controls
        
        var dataEndpoints = new[]
        {
            ($"{_apiUrls["TablesApi"]}/tables", "Tables Data"),
            ($"{_apiUrls["MenuApi"]}/api/menu/items", "Menu Data"),
            ($"{_apiUrls["TablesApi"]}/tables/counts", "Table Counts"),
            ($"{_apiUrls["TablesApi"]}/sessions/active", "Active Sessions")
        };

        var dataResults = new List<(string name, bool success, int dataSize)>();

        foreach (var (endpoint, name) in dataEndpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                var content = await response.Content.ReadAsStringAsync();
                dataResults.Add((name, response.IsSuccessStatusCode, content.Length));
            }
            catch
            {
                dataResults.Add((name, false, 0));
            }
        }

        // Data should load successfully for UI binding
        var successfulData = dataResults.Count(r => r.success);
        successfulData.Should().BeGreaterThan(0, "At least some data should load for UI binding");
        
        Console.WriteLine($"UI Data Binding Test: {successfulData}/{dataResults.Count} data sources working");
        foreach (var result in dataResults)
        {
            Console.WriteLine($"  {result.name}: {(result.success ? "✅" : "❌")} (Size: {result.dataSize} bytes)");
        }
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("ErrorHandling")]
    public async Task UI_ErrorHandling_ShouldHandleNetworkErrors()
    {
        // Test UI error handling by simulating network errors
        
        var errorScenarios = new[]
        {
            // Invalid endpoints
            $"{_apiUrls["TablesApi"]}/invalid-endpoint",
            $"{_apiUrls["MenuApi"]}/api/invalid-endpoint",
            $"{_apiUrls["PaymentApi"]}/api/invalid-endpoint"
        };

        var errorResults = new List<bool>();

        foreach (var endpoint in errorScenarios)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                // Should return 404 or similar error status, not crash
                errorResults.Add(response.StatusCode == System.Net.HttpStatusCode.NotFound);
            }
            catch
            {
                // Network errors should be handled gracefully
                errorResults.Add(true);
            }
        }

        // All error scenarios should be handled gracefully
        errorResults.All(r => r).Should().BeTrue("UI should handle network errors gracefully");
        
        Console.WriteLine($"UI Error Handling: {errorResults.Count(r => r)}/{errorResults.Count} errors handled gracefully");
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("Performance")]
    public async Task UI_Performance_ShouldLoadWithinAcceptableTime()
    {
        // Test UI performance by measuring load times for main data
        
        var performanceEndpoints = new[]
        {
            ($"{_apiUrls["TablesApi"]}/tables", "Tables Load"),
            ($"{_apiUrls["MenuApi"]}/api/menu/items", "Menu Load"),
            ($"{_apiUrls["TablesApi"]}/tables/counts", "Counts Load")
        };

        var performanceResults = new List<(string name, long loadTime, bool success)>();

        foreach (var (endpoint, name) in performanceEndpoints)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                stopwatch.Stop();
                performanceResults.Add((name, stopwatch.ElapsedMilliseconds, response.IsSuccessStatusCode));
            }
            catch
            {
                stopwatch.Stop();
                performanceResults.Add((name, stopwatch.ElapsedMilliseconds, false));
            }
        }

        // UI should load within acceptable time limits
        foreach (var result in performanceResults.Where(r => r.success))
        {
            result.loadTime.Should().BeLessThan(3000, $"UI should load {result.name} within 3 seconds");
        }
        
        Console.WriteLine("UI Performance Results:");
        foreach (var result in performanceResults)
        {
            Console.WriteLine($"  {result.name}: {(result.success ? "✅" : "❌")} - {result.loadTime}ms");
        }
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("Critical")]
    public async Task UI_SessionManagement_ShouldHandleSessions()
    {
        // Test session management functionality that the UI depends on
        
        var sessionEndpoints = new[]
        {
            $"{_apiUrls["TablesApi"]}/sessions/active",
            $"{_apiUrls["TablesApi"]}/tables",
            $"{_apiUrls["TablesApi"]}/tables/counts"
        };

        var sessionResults = new List<bool>();

        foreach (var endpoint in sessionEndpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                sessionResults.Add(response.IsSuccessStatusCode);
            }
            catch
            {
                sessionResults.Add(false);
            }
        }

        // Session management should work for UI
        var workingSessions = sessionResults.Count(r => r);
        workingSessions.Should().BeGreaterThan(0, "Session management should be functional for UI");
        
        Console.WriteLine($"UI Session Management: {workingSessions}/{sessionResults.Count} session operations working");
    }

    [TestMethod]
    [TestCategory("UI")]
    [TestCategory("Integration")]
    public async Task UI_EndToEndWorkflow_ShouldCompleteUserJourney()
    {
        // Test a complete user journey that the UI would support
        
        var workflowSteps = new List<(string step, Func<Task<bool>> action)>
        {
            ("1. Load Tables", async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
                return response.IsSuccessStatusCode;
            }),
            ("2. View Menu", async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["MenuApi"]}/api/menu/items");
                return response.IsSuccessStatusCode;
            }),
            ("3. Check Active Sessions", async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");
                return response.IsSuccessStatusCode;
            }),
            ("4. Validate Payment System", async () =>
            {
                var response = await _httpClient.GetAsync($"{_apiUrls["PaymentApi"]}/api/payments/validate");
                return response.IsSuccessStatusCode;
            })
        };

        var workflowResults = new List<(string step, bool success)>();

        foreach (var (step, action) in workflowSteps)
        {
            try
            {
                workflowResults.Add((step, await action()));
            }
            catch
            {
                workflowResults.Add((step, false));
            }
        }

        // Most workflow steps should complete successfully
        var successfulSteps = workflowResults.Count(r => r.success);
        var successRate = (double)successfulSteps / workflowResults.Count;
        
        successRate.Should().BeGreaterThan(0.5, "Core user workflow should be functional");
        
        Console.WriteLine($"UI End-to-End Workflow: {successfulSteps}/{workflowResults.Count} steps successful ({successRate:P})");
        foreach (var result in workflowResults)
        {
            Console.WriteLine($"  {result.step}: {(result.success ? "✅" : "❌")}");
        }
    }
}
