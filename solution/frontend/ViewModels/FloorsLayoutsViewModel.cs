using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.UI.Dispatching;
using MagiDesk.Shared.DTOs.Tables;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public class FloorsLayoutsViewModel : INotifyPropertyChanged
{
    private readonly HttpClient _httpClient;
    private readonly DispatcherQueue _dispatcher;
    private FloorDto? _selectedFloor;
    private TableLayoutDto? _selectedTable;
    private bool _isLoading;
    private string? _errorMessage;
    private LayoutSettingsDto _layoutSettings = new();
    private double _zoomLevel = 1.0;
    private bool _isDesignMode = true;

    public FloorsLayoutsViewModel()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _httpClient = new HttpClient(new HttpClientHandler 
        { 
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator 
        });
        
        // Get API base URL from configuration (same pattern as TableRepository)
        var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
        var cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
            .Build();
        
        var baseUrl = cfg["TablesApi:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }
        
        Floors = new ObservableCollection<FloorDto>();
        Tables = new ObservableCollection<TableLayoutDto>();
        
        Func<object?, Task> loadFloors = async _ => await LoadFloorsAsync();
        Func<object?, Task> saveLayout = async _ => await SaveLayoutAsync();
        Func<object?, Task> loadSettings = async _ => await LoadLayoutSettingsAsync();
        
        // Note: AddFloorCommand, DeleteFloorCommand, DuplicateFloorCommand are handled by UI event handlers
        // They're kept here for binding but actual logic is in FloorsLayoutsPage.xaml.cs
        LoadFloorsCommand = new Services.RelayCommand(loadFloors);
        AddFloorCommand = new Services.RelayCommand(async _ => { /* Handled by AddFloor_Click */ await Task.CompletedTask; });
        DeleteFloorCommand = new Services.RelayCommand(async _ => { /* Handled by DeleteFloor_Click */ await Task.CompletedTask; });
        DuplicateFloorCommand = new Services.RelayCommand(async _ => { /* Handled by DuplicateFloor_Click */ await Task.CompletedTask; });
        SaveLayoutCommand = new Services.RelayCommand(saveLayout);
        LoadLayoutSettingsCommand = new Services.RelayCommand(loadSettings);
    }

    public ObservableCollection<FloorDto> Floors { get; }
    public ObservableCollection<TableLayoutDto> Tables { get; }

    public FloorDto? SelectedFloor
    {
        get => _selectedFloor;
        set
        {
            if (SetProperty(ref _selectedFloor, value))
            {
                if (value != null)
                {
                    _ = LoadFloorLayoutAsync(value.FloorId);
                }
                else
                {
                    Tables.Clear();
                }
            }
        }
    }

    public TableLayoutDto? SelectedTable
    {
        get => _selectedTable;
        set => SetProperty(ref _selectedTable, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public LayoutSettingsDto LayoutSettings
    {
        get => _layoutSettings;
        set => SetProperty(ref _layoutSettings, value);
    }

    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.25, 4.0));
    }

    public bool IsDesignMode
    {
        get => _isDesignMode;
        set => SetProperty(ref _isDesignMode, value);
    }

    public ICommand LoadFloorsCommand { get; }
    public ICommand AddFloorCommand { get; }
    public ICommand DeleteFloorCommand { get; }
    public ICommand DuplicateFloorCommand { get; }
    public ICommand SaveLayoutCommand { get; }
    public ICommand LoadLayoutSettingsCommand { get; }

    public async Task LoadFloorsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.GetAsync("floors");
            if (response.IsSuccessStatusCode)
            {
                var floors = await response.Content.ReadFromJsonAsync<List<FloorDto>>();
                if (floors != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        Floors.Clear();
                        foreach (var floor in floors)
                        {
                            Floors.Add(floor);
                        }
                        
                        // Select default floor if available
                        var defaultFloor = Floors.FirstOrDefault(f => f.IsDefault);
                        if (defaultFloor != null)
                        {
                            SelectedFloor = defaultFloor;
                        }
                        else if (Floors.Count > 0)
                        {
                            SelectedFloor = Floors[0];
                        }
                    });
                }
            }
            else
            {
                ErrorMessage = $"Failed to load floors: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading floors: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadFloorLayoutAsync(Guid floorId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.GetAsync($"floors/{floorId}/layout");
            if (response.IsSuccessStatusCode)
            {
                var layout = await response.Content.ReadFromJsonAsync<FloorLayoutDto>();
                if (layout != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        Tables.Clear();
                        int index = 0;
                        foreach (var table in layout.Tables)
                        {
                            // If table has no position, assign a default grid position
                            if (table.XPosition == 0 && table.YPosition == 0)
                            {
                                // Arrange tables in a grid pattern
                                int cols = 5;
                                int row = index / cols;
                                int col = index % cols;
                                table.XPosition = (col + 1) * 150; // 150px spacing
                                table.YPosition = (row + 1) * 150;
                            }
                            Tables.Add(table);
                            index++;
                        }
                    });
                }
            }
            else
            {
                ErrorMessage = $"Failed to load layout: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading layout: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<FloorDto?> CreateFloorAsync(string floorName, string? description, bool isDefault)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var request = new CreateFloorRequest
            {
                FloorName = floorName,
                Description = description,
                IsDefault = isDefault,
                DisplayOrder = Floors.Count
            };
            
            var response = await _httpClient.PostAsJsonAsync("floors", request);
            if (response.IsSuccessStatusCode)
            {
                var floor = await response.Content.ReadFromJsonAsync<FloorDto>();
                if (floor != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        Floors.Add(floor);
                        SelectedFloor = floor;
                    });
                    return floor;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to create floor: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating floor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return null;
    }

    public async Task<bool> UpdateFloorAsync(Guid floorId, UpdateFloorRequest request)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.PutAsJsonAsync($"floors/{floorId}", request);
            if (response.IsSuccessStatusCode)
            {
                await LoadFloorsAsync();
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to update floor: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating floor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    public async Task<bool> DeleteFloorAsync(Guid floorId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.DeleteAsync($"floors/{floorId}");
            if (response.IsSuccessStatusCode)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    var floor = Floors.FirstOrDefault(f => f.FloorId == floorId);
                    if (floor != null)
                    {
                        Floors.Remove(floor);
                        if (SelectedFloor?.FloorId == floorId)
                        {
                            SelectedFloor = Floors.FirstOrDefault();
                        }
                    }
                });
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to delete floor: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting floor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    public async Task<bool> DuplicateFloorAsync(Guid floorId, string newFloorName, bool copyTables)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var request = new DuplicateFloorRequest
            {
                NewFloorName = newFloorName,
                CopyTables = copyTables
            };
            
            var response = await _httpClient.PostAsJsonAsync($"floors/{floorId}/duplicate", request);
            if (response.IsSuccessStatusCode)
            {
                await LoadFloorsAsync();
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to duplicate floor: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error duplicating floor: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    public async Task<bool> DeleteTableAsync(Guid tableId)
    {
        if (SelectedFloor == null) return false;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.DeleteAsync($"floors/{SelectedFloor.FloorId}/tables/{tableId}");
            if (response.IsSuccessStatusCode)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    var table = Tables.FirstOrDefault(t => t.TableId == tableId);
                    if (table != null)
                    {
                        Tables.Remove(table);
                        if (SelectedTable?.TableId == tableId)
                        {
                            SelectedTable = null;
                        }
                    }
                });
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to delete table: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting table: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    public async Task<bool> DuplicateTableAsync(Guid tableId)
    {
        if (SelectedFloor == null) return false;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.PostAsync($"floors/{SelectedFloor.FloorId}/tables/{tableId}/duplicate", null);
            if (response.IsSuccessStatusCode)
            {
                await LoadFloorLayoutAsync(SelectedFloor.FloorId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to duplicate table: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error duplicating table: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    public async Task SaveLayoutAsync()
    {
        if (SelectedFloor == null) return;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            // Save each table's position/properties
            foreach (var table in Tables)
            {
                var updateRequest = new UpdateTableLayoutRequest
                {
                    XPosition = table.XPosition,
                    YPosition = table.YPosition,
                    Rotation = table.Rotation,
                    TableName = table.TableName,
                    TableNumber = table.TableNumber,
                    TableType = table.TableType,
                    Size = table.Size,
                    Width = table.Width,
                    Height = table.Height,
                    BillingRate = table.BillingRate,
                    AutoStartTimer = table.AutoStartTimer,
                    IconStyle = table.IconStyle,
                    GroupingTags = table.GroupingTags,
                    IsLocked = table.IsLocked
                };
                
                var response = await _httpClient.PutAsJsonAsync(
                    $"floors/{SelectedFloor.FloorId}/tables/{table.TableId}", 
                    updateRequest);
                
                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = $"Failed to save table {table.TableName}";
                    return;
                }
            }
            
            // Reload layout to ensure consistency
            await LoadFloorLayoutAsync(SelectedFloor.FloorId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving layout: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadLayoutSettingsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("layout/settings");
            if (response.IsSuccessStatusCode)
            {
                var settings = await response.Content.ReadFromJsonAsync<LayoutSettingsDto>();
                if (settings != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        LayoutSettings = settings;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading layout settings: {ex.Message}");
        }
    }

    public async Task<bool> SaveLayoutSettingsAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            
            var response = await _httpClient.PutAsJsonAsync("layout/settings", LayoutSettings);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Failed to save layout settings: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving layout settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
        
        return false;
    }

    public async Task<TableLayoutDto?> CreateTableAsync(Guid floorId, string tableName, string? tableNumber, string tableType)
    {
        if (SelectedFloor == null) return null;
        
        try
        {
            var request = new CreateTableLayoutRequest
            {
                FloorId = floorId,
                TableName = tableName,
                TableNumber = tableNumber,
                TableType = tableType,
                XPosition = 100, // Default position
                YPosition = 100,
                Size = LayoutSettings.DefaultTableSize
            };
            
            var response = await _httpClient.PostAsJsonAsync($"floors/{floorId}/tables", request);
            if (response.IsSuccessStatusCode)
            {
                var table = await response.Content.ReadFromJsonAsync<TableLayoutDto>();
                if (table != null)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        Tables.Add(table);
                        SelectedTable = table;
                    });
                    return table;
                }
            }
            else
            {
                ErrorMessage = $"Failed to create table: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating table: {ex.Message}";
        }
        
        return null;
    }

    public async Task UpdateTablePositionAsync(Guid tableId, double x, double y)
    {
        if (SelectedFloor == null) return;
        
        try
        {
            // Snap to grid if enabled
            if (LayoutSettings.SnapToGrid)
            {
                x = Math.Round(x / LayoutSettings.GridSize) * LayoutSettings.GridSize;
                y = Math.Round(y / LayoutSettings.GridSize) * LayoutSettings.GridSize;
            }
            
            var updateRequest = new UpdateTableLayoutRequest
            {
                XPosition = x,
                YPosition = y
            };
            
            var response = await _httpClient.PutAsJsonAsync(
                $"floors/{SelectedFloor.FloorId}/tables/{tableId}", 
                updateRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var table = Tables.FirstOrDefault(t => t.TableId == tableId);
                if (table != null)
                {
                    table.XPosition = x;
                    table.YPosition = y;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating table position: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


