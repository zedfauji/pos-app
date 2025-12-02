using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Services;

public class FloorsTablesService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiBaseUrl;

    public FloorsTablesService()
    {
        _httpClient = new HttpClient(new HttpClientHandler 
        { 
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
        });
        
        // Get API base URL from configuration
        var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
            .Build();
        
        _apiBaseUrl = cfg["TablesApi:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_apiBaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<List<FloorDto>> GetFloorsAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return new List<FloorDto>();
            
            var response = await _httpClient.GetAsync("floors");
            if (response.IsSuccessStatusCode)
            {
                var floors = await response.Content.ReadFromJsonAsync<List<FloorDto>>();
                return floors ?? new List<FloorDto>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading floors: {ex.Message}");
        }
        return new List<FloorDto>();
    }

    public async Task<List<TableLayoutDto>> GetAllTablesAsync(Guid? floorId = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return new List<TableLayoutDto>();
            
            var tables = new List<TableLayoutDto>();
            
            if (floorId.HasValue)
            {
                // Get tables for specific floor
                var response = await _httpClient.GetAsync($"floors/{floorId}/tables");
                if (response.IsSuccessStatusCode)
                {
                    var floorTables = await response.Content.ReadFromJsonAsync<List<TableLayoutDto>>();
                    if (floorTables != null)
                    {
                        tables.AddRange(floorTables);
                    }
                }
            }
            else
            {
                // Get all tables from all floors
                var floors = await GetFloorsAsync();
                foreach (var floor in floors.Where(f => f.IsActive))
                {
                    var response = await _httpClient.GetAsync($"floors/{floor.FloorId}/tables");
                    if (response.IsSuccessStatusCode)
                    {
                        var floorTables = await response.Content.ReadFromJsonAsync<List<TableLayoutDto>>();
                        if (floorTables != null)
                        {
                            tables.AddRange(floorTables);
                        }
                    }
                }
            }
            
            return tables;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading tables: {ex.Message}");
        }
        return new List<TableLayoutDto>();
    }

    public async Task<TableLayoutDto?> GetTableAsync(Guid floorId, Guid tableId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiBaseUrl)) return null;
            
            var response = await _httpClient.GetAsync($"floors/{floorId}/tables/{tableId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TableLayoutDto>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading table: {ex.Message}");
        }
        return null;
    }

    // Convert TableLayoutDto to TableStatusDto for backward compatibility
    public TableStatusDto ToTableStatusDto(TableLayoutDto table)
    {
        return new TableStatusDto
        {
            Label = table.TableName,
            Type = table.TableType,
            Occupied = table.Status == "occupied",
            OrderId = table.OrderId,
            StartTime = table.StartTime,
            Server = table.Server
        };
    }
}

