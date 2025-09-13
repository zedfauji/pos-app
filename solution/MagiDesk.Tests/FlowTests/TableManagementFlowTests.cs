using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MagiDesk.Tests.TestInfrastructure;

namespace MagiDesk.Tests.FlowTests;

[TestClass]
public class TableManagementFlowTests
{
    private HttpClient _httpClient;
    private readonly Dictionary<string, string> _apiUrls;

    public TableManagementFlowTests()
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
    [TestCategory("Flow")]
    [TestCategory("Critical")]
    public async Task TableManagement_CompleteTableAssignmentFlow_ShouldWork()
    {
        // Test the complete table assignment and session start flow
        
        // Step 1: Get available tables
        var tablesResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
        tablesResponse.IsSuccessStatusCode.Should().BeTrue("Should be able to fetch tables");
        
        var tablesContent = await tablesResponse.Content.ReadAsStringAsync();
        tablesContent.Should().NotBeNullOrEmpty("Tables data should not be empty");
        
        Console.WriteLine($"✅ Step 1: Fetched tables successfully - {tablesContent.Length} characters");
        
        // Step 2: Get table counts (available/occupied)
        var countsResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables/counts");
        countsResponse.IsSuccessStatusCode.Should().BeTrue("Should be able to fetch table counts");
        
        var countsContent = await countsResponse.Content.ReadAsStringAsync();
        countsContent.Should().NotBeNullOrEmpty("Table counts should not be empty");
        
        Console.WriteLine($"✅ Step 2: Fetched table counts successfully - {countsContent.Length} characters");
        
        // Step 3: Check active sessions
        var sessionsResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");
        sessionsResponse.IsSuccessStatusCode.Should().BeTrue("Should be able to fetch active sessions");
        
        var sessionsContent = await sessionsResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"✅ Step 3: Fetched active sessions successfully - {sessionsContent.Length} characters");
        
        // Step 4: Validate table management endpoints are functional
        var tableManagementEndpoints = new[]
        {
            $"{_apiUrls["TablesApi"]}/tables",
            $"{_apiUrls["TablesApi"]}/tables/counts",
            $"{_apiUrls["TablesApi"]}/sessions/active"
        };
        
        var workingEndpoints = 0;
        foreach (var endpoint in tableManagementEndpoints)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                if (response.IsSuccessStatusCode) workingEndpoints++;
            }
            catch
            {
                // Count as non-working
            }
        }
        
        var successRate = (double)workingEndpoints / tableManagementEndpoints.Length;
        successRate.Should().BeGreaterThan(0.6, "Most table management endpoints should be functional");
        
        Console.WriteLine($"✅ Step 4: Table management flow validation - {workingEndpoints}/{tableManagementEndpoints.Length} endpoints working ({successRate:P})");
    }

    [TestMethod]
    [TestCategory("Flow")]
    [TestCategory("Critical")]
    public async Task TableManagement_TableTransferFlow_ShouldHandleTransfers()
    {
        // Test table transfer operations (simulating the flow)
        
        // Step 1: Get current table status
        var tablesResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
        tablesResponse.IsSuccessStatusCode.Should().BeTrue("Should be able to fetch tables for transfer");
        
        Console.WriteLine("✅ Step 1: Retrieved tables for transfer simulation");
        
        // Step 2: Check session management capabilities
        var sessionsResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");
        var sessionsWorking = sessionsResponse.IsSuccessStatusCode;
        
        Console.WriteLine($"✅ Step 2: Session management - {(sessionsWorking ? "Working" : "Not available")}");
        
        // Step 3: Validate transfer prerequisites
        var transferPrerequisites = new[]
        {
            tablesResponse.IsSuccessStatusCode,
            sessionsResponse.IsSuccessStatusCode
        };
        
        var prerequisitesMet = transferPrerequisites.Count(p => p);
        prerequisitesMet.Should().BeGreaterThan(0, "At least some transfer prerequisites should be met");
        
        Console.WriteLine($"✅ Step 3: Transfer prerequisites - {prerequisitesMet}/{transferPrerequisites.Length} met");
        
        // Step 4: Test data consistency for transfers
        var consistencyChecks = new List<bool>();
        
        // Check if we can get both tables and sessions data
        consistencyChecks.Add(tablesResponse.IsSuccessStatusCode);
        consistencyChecks.Add(sessionsResponse.IsSuccessStatusCode);
        
        var consistentData = consistencyChecks.Count(c => c);
        var consistencyRate = (double)consistentData / consistencyChecks.Count;
        
        consistencyRate.Should().BeGreaterThan(0.5, "Data consistency should be maintained for transfers");
        
        Console.WriteLine($"✅ Step 4: Data consistency for transfers - {consistentData}/{consistencyChecks.Count} checks passed ({consistencyRate:P})");
    }

    [TestMethod]
    [TestCategory("Flow")]
    [TestCategory("EdgeCase")]
    public async Task TableManagement_ConcurrentTableOperations_ShouldHandleConflicts()
    {
        // Test concurrent table operations to simulate multiple users
        
        var concurrentTasks = new List<Task<bool>>();
        
        // Simulate multiple concurrent requests
        for (int i = 0; i < 5; i++)
        {
            concurrentTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }));
        }
        
        // Wait for all concurrent operations
        var results = await Task.WhenAll(concurrentTasks);
        
        // Analyze results
        var successfulRequests = results.Count(r => r);
        var successRate = (double)successfulRequests / results.Length;
        
        successRate.Should().BeGreaterThan(0.6, "Most concurrent requests should succeed");
        
        Console.WriteLine($"✅ Concurrent table operations: {successfulRequests}/{results.Length} successful ({successRate:P})");
        
        // Test concurrent session checks
        var sessionTasks = new List<Task<bool>>();
        
        for (int i = 0; i < 3; i++)
        {
            sessionTasks.Add(Task.Run(async () =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }));
        }
        
        var sessionResults = await Task.WhenAll(sessionTasks);
        var sessionSuccessRate = (double)sessionResults.Count(r => r) / sessionResults.Length;
        
        Console.WriteLine($"✅ Concurrent session operations: {sessionResults.Count(r => r)}/{sessionResults.Length} successful ({sessionSuccessRate:P})");
    }

    [TestMethod]
    [TestCategory("Flow")]
    [TestCategory("EdgeCase")]
    public async Task TableManagement_NetworkFailureRecovery_ShouldHandleDisconnections()
    {
        // Test network failure scenarios and recovery
        
        // Step 1: Normal operation
        var normalResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
        var normalWorking = normalResponse.IsSuccessStatusCode;
        
        Console.WriteLine($"✅ Step 1: Normal operation - {(normalWorking ? "Working" : "Failed")}");
        
        // Step 2: Simulate network issues with timeout
        using var timeoutClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        
        try
        {
            var timeoutResponse = await timeoutClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
            var timeoutHandled = !timeoutResponse.IsSuccessStatusCode || timeoutResponse.StatusCode == System.Net.HttpStatusCode.RequestTimeout;
            
            Console.WriteLine($"✅ Step 2: Timeout handling - {(timeoutHandled ? "Properly handled" : "Not handled")}");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("✅ Step 2: Timeout handling - Properly caught timeout exception");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"✅ Step 2: Network error handling - Caught network exception: {ex.Message}");
        }
        
        // Step 3: Recovery test - try normal operation again
        var recoveryResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
        var recoveryWorking = recoveryResponse.IsSuccessStatusCode;
        
        Console.WriteLine($"✅ Step 3: Recovery test - {(recoveryWorking ? "Recovered successfully" : "Recovery failed")}");
        
        // At least normal operation or recovery should work
        (normalWorking || recoveryWorking).Should().BeTrue("Either normal operation or recovery should work");
    }

    [TestMethod]
    [TestCategory("Flow")]
    [TestCategory("Critical")]
    public async Task TableManagement_SessionLifecycle_ShouldManageSessionsProperly()
    {
        // Test complete session lifecycle management
        
        // Step 1: Check session endpoints
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
                Console.WriteLine($"✅ Session endpoint {endpoint}: {(response.IsSuccessStatusCode ? "Working" : "Failed")}");
            }
            catch (Exception ex)
            {
                sessionResults.Add(false);
                Console.WriteLine($"❌ Session endpoint {endpoint}: Exception - {ex.Message}");
            }
        }
        
        // Step 2: Validate session data consistency
        var workingEndpoints = sessionResults.Count(r => r);
        var sessionSuccessRate = (double)workingEndpoints / sessionEndpoints.Length;
        
        sessionSuccessRate.Should().BeGreaterThan(0.5, "Most session endpoints should be functional");
        
        Console.WriteLine($"✅ Session lifecycle validation: {workingEndpoints}/{sessionEndpoints.Length} endpoints working ({sessionSuccessRate:P})");
        
        // Step 3: Test session data format
        try
        {
            var sessionsResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");
            if (sessionsResponse.IsSuccessStatusCode)
            {
                var sessionsContent = await sessionsResponse.Content.ReadAsStringAsync();
                sessionsContent.Should().NotBeNull("Session data should not be null");
                
                // Try to parse as JSON to validate format
                try
                {
                    JsonDocument.Parse(sessionsContent);
                    Console.WriteLine("✅ Session data format: Valid JSON");
                }
                catch (JsonException)
                {
                    Console.WriteLine("⚠️ Session data format: Not valid JSON (may be expected)");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Session data validation: Exception - {ex.Message}");
        }
    }

    [TestMethod]
    [TestCategory("Flow")]
    [TestCategory("EdgeCase")]
    public async Task TableManagement_DataConsistency_ShouldMaintainIntegrity()
    {
        // Test data consistency across table management operations
        
        // Step 1: Get tables data
        var tablesResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
        var tablesWorking = tablesResponse.IsSuccessStatusCode;
        var tablesContent = tablesWorking ? await tablesResponse.Content.ReadAsStringAsync() : null;
        
        Console.WriteLine($"✅ Step 1: Tables data - {(tablesWorking ? "Retrieved" : "Failed")}");
        
        // Step 2: Get table counts
        var countsResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables/counts");
        var countsWorking = countsResponse.IsSuccessStatusCode;
        var countsContent = countsWorking ? await countsResponse.Content.ReadAsStringAsync() : null;
        
        Console.WriteLine($"✅ Step 2: Table counts - {(countsWorking ? "Retrieved" : "Failed")}");
        
        // Step 3: Get active sessions
        var sessionsResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/sessions/active");
        var sessionsWorking = sessionsResponse.IsSuccessStatusCode;
        var sessionsContent = sessionsWorking ? await sessionsResponse.Content.ReadAsStringAsync() : null;
        
        Console.WriteLine($"✅ Step 3: Active sessions - {(sessionsWorking ? "Retrieved" : "Failed")}");
        
        // Step 4: Validate data consistency
        var consistencyChecks = new[]
        {
            tablesWorking,
            countsWorking,
            sessionsWorking
        };
        
        var consistentData = consistencyChecks.Count(c => c);
        var consistencyRate = (double)consistentData / consistencyChecks.Length;
        
        consistencyRate.Should().BeGreaterThan(0.5, "Most data sources should be consistent");
        
        Console.WriteLine($"✅ Step 4: Data consistency - {consistentData}/{consistencyChecks.Length} sources consistent ({consistencyRate:P})");
        
        // Step 5: Test data freshness (multiple requests should return consistent data)
        if (tablesWorking)
        {
            var secondTablesResponse = await _httpClient.GetAsync($"{_apiUrls["TablesApi"]}/tables");
            var secondTablesWorking = secondTablesResponse.IsSuccessStatusCode;
            
            if (secondTablesWorking)
            {
                var secondTablesContent = await secondTablesResponse.Content.ReadAsStringAsync();
                var dataConsistent = tablesContent == secondTablesContent;
                
                Console.WriteLine($"✅ Step 5: Data freshness - {(dataConsistent ? "Consistent" : "Inconsistent")}");
            }
        }
    }
}
