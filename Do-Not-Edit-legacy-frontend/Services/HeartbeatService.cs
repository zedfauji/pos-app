using System.Diagnostics;

namespace MagiDesk.Frontend.Services;

/// <summary>
/// Heartbeat service to keep sessions alive and detect crashed sessions
/// </summary>
public class HeartbeatService : IDisposable
{
    private readonly Timer _heartbeatTimer;
    private readonly HttpClient _httpClient;
    private readonly string _tablesApiUrl;
    private bool _disposed = false;
    private volatile bool _isHeartbeatActive = false;

    public HeartbeatService()
    {
        _tablesApiUrl = Environment.GetEnvironmentVariable("TABLESAPI_BASEURL") ?? "https://magidesk-tables-904541739138.northamerica-south1.run.app";
        
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_tablesApiUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(5)
        };

        // Start heartbeat timer - send heartbeat every 30 seconds
        _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        Debug.WriteLine("HeartbeatService: Started heartbeat timer (30 second intervals)");
    }

    private async void SendHeartbeat(object? state)
    {
        if (_disposed || !_isHeartbeatActive) return;

        try
        {
            var sessionId = Services.OrderContext.CurrentSessionId;
            if (string.IsNullOrWhiteSpace(sessionId)) return;

            Debug.WriteLine($"HeartbeatService: Sending heartbeat for session {sessionId}");
            
            // Send heartbeat to TablesApi
            var response = await _httpClient.PostAsync($"sessions/{Uri.EscapeDataString(sessionId)}/heartbeat", new StringContent(""));
            
            if (response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"HeartbeatService: Heartbeat successful for session {sessionId}");
            }
            else
            {
                Debug.WriteLine($"HeartbeatService: Heartbeat failed for session {sessionId} - Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"HeartbeatService: Heartbeat error: {ex.Message}");
        }
    }

    /// <summary>
    /// Start heartbeat for the current session
    /// </summary>
    public void StartHeartbeat()
    {
        _isHeartbeatActive = true;
        Debug.WriteLine("HeartbeatService: Heartbeat started");
    }

    /// <summary>
    /// Stop heartbeat
    /// </summary>
    public void StopHeartbeat()
    {
        _isHeartbeatActive = false;
        Debug.WriteLine("HeartbeatService: Heartbeat stopped");
    }

    /// <summary>
    /// Check if heartbeat is active
    /// </summary>
    public bool IsHeartbeatActive => _isHeartbeatActive;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _heartbeatTimer?.Dispose();
            _httpClient?.Dispose();
            Debug.WriteLine("HeartbeatService: Disposed");
        }
    }
}
