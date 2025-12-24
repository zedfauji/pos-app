using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Tables;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using Refit;

namespace MagiDesk.Client.ViewModels;

public partial class TableManagementViewModel : ObservableObject
{
    private readonly ITableApi _tableApi;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<TableStatusDto> _tables = new();

    [ObservableProperty]
    private ObservableCollection<TableTypeDto> _types = new();

    [ObservableProperty]
    private bool _isLoading;

    // --- Editing State (Tables) ---
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTableEditorOpen))]
    private TableStatusDto? _selectedTable;

    [ObservableProperty]
    private bool _isTableEditorOpen; // Controls visibility of side panel

    [ObservableProperty]
    private bool _isNewTable;

    // Form Fields for Table
    [ObservableProperty]
    private string _editTableName = string.Empty;
    [ObservableProperty]
    private TableTypeDto? _editTableType;
    [ObservableProperty]
    private int _editTableCapacity = 4;
    [ObservableProperty]
    private bool _editTableActive = true;


    // --- Editing State (Types) ---
    [ObservableProperty]
    private TableTypeDto? _selectedType;

    [ObservableProperty]
    private bool _isTypeEditorOpen;

    // Form Fields for Type
    [ObservableProperty]
    private string _editTypeName = string.Empty;
    [ObservableProperty]
    private decimal _editTypeRate;
    [ObservableProperty]
    private bool _editTypeHasTimer;
    [ObservableProperty]
    private bool _editTypeReqServer;
    [ObservableProperty]
    private bool _editTypeAllowOrders;


    public TableManagementViewModel(ITableApi tableApi, IDialogService dialogService, INavigationService navigationService)
    {
        _tableApi = tableApi;
        _dialogService = dialogService;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var tTask = _tableApi.GetTablesAsync();
            var tyTask = _tableApi.GetTableTypesAsync();
            
            await Task.WhenAll(tTask, tyTask);
            var tResult = await tTask;
            var tyResult = await tyTask;

            Tables.Clear();
            foreach (var t in tResult) Tables.Add(t);

            Types.Clear();
            foreach (var ty in tyResult) Types.Add(ty);
        }
        catch (Exception ex)
        {
           await _dialogService.ShowMessageAsync("Error", "Failed to load data: " + ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // --- Table Actions ---

    [RelayCommand]
    public void OpenAddTable()
    {
        SelectedTable = null;
        IsNewTable = true;
        
        // Reset Fields
        EditTableName = $"Table {Tables.Count + 1}";
        EditTableType = Types.FirstOrDefault();
        EditTableCapacity = 4;
        EditTableActive = true;
        
        IsTypeEditorOpen = false;
        IsTableEditorOpen = true;
    }

    [RelayCommand]
    public void OpenEditTable(TableStatusDto table)
    {
        if (table == null) return;
        SelectedTable = table;
        IsNewTable = false;
        
        // Populate Fields
        EditTableName = table.Label;
        EditTableType = Types.FirstOrDefault(t => t.Id == table.TypeId) ?? Types.FirstOrDefault();
        EditTableCapacity = table.Capacity;
        EditTableActive = table.IsActive != false; // null check

        IsTypeEditorOpen = false;
        IsTableEditorOpen = true;
    }

    [RelayCommand]
    public void CloseTableEditor()
    {
        IsTableEditorOpen = false;
        SelectedTable = null;
    }

    [RelayCommand]
    public async Task SaveTableAsync()
    {
        if (string.IsNullOrWhiteSpace(EditTableName)) 
        {
            await _dialogService.ShowMessageAsync("Validation", "Name is required.");
            return;
        }
        if (EditTableType == null)
        {
            await _dialogService.ShowMessageAsync("Validation", "Type is required.");
            return;
        }

        try
        {
            if (IsNewTable)
            {
                var req = new CreateTableRequest 
                {
                    Name = EditTableName,
                    TypeId = EditTableType.Id,
                    Capacity = EditTableCapacity
                };
                var response = await _tableApi.AddTableAsync(req);
                if (!response.IsSuccessStatusCode) throw new Exception(response.Error.Content);
            }
            else
            {
                if (SelectedTable == null) return;
                var req = new UpdateTableRequest
                {
                    Name = EditTableName,
                    TypeId = EditTableType.Id,
                    Capacity = EditTableCapacity,
                    IsActive = EditTableActive
                };
                var response = await _tableApi.UpdateTableAsync(SelectedTable.TableId, req);
                if (!response.IsSuccessStatusCode) throw new Exception(response.Error.Content);
            }

            CloseTableEditor();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    public async Task DeleteTableAsync(TableStatusDto table)
    {
        if (table == null) return;
        
        var confirm = await _dialogService.RequestConfirmationAsync("Confirm Delete", $"Are you sure you want to delete {table.Label}?");
        if (!confirm) return;

        try
        {
            var response = await _tableApi.DeleteTableAsync(table.TableId);
            if (!response.IsSuccessStatusCode)
            {
                 // Handle specific conflict (Occupied)
                 if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                 {
                     await _dialogService.ShowMessageAsync("Blocked", "Cannot delete occupied table.");
                 }
                 else
                 {
                     await _dialogService.ShowMessageAsync("Error", response.Error?.Content ?? "Delete failed");
                 }
                 return;
            }
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }


    // --- Type Actions ---

    [RelayCommand]
    public async Task OpenEditTypeAsync(TableTypeDto type)
    {
        if (type == null) return;
        SelectedType = type;
        
        // Populate
        EditTypeName = type.Name;
        EditTypeRate = type.HourlyRate;
        EditTypeHasTimer = type.HasTimer;
        EditTypeReqServer = type.RequiresServer;
        EditTypeAllowOrders = type.AllowOrders;

        IsTableEditorOpen = false;
        IsTypeEditorOpen = true;
        
        await _dialogService.ShowMessageAsync("Warning", 
            "Modifying Type configuration will affect FUTURE billing calculations.\n" +
            "Active sessions may still use old rates until they are stopped or moved.");
    }

    [RelayCommand]
    public void CloseTypeEditor()
    {
        IsTypeEditorOpen = false;
        SelectedType = null;
    }

    [RelayCommand]
    public async Task SaveTypeAsync()
    {
        if (SelectedType == null) return;
        
        try
        {
            var req = new UpdateTableTypeRequest
            {
                Name = EditTypeName,
                HourlyRate = EditTypeRate,
                HasTimer = EditTypeHasTimer,
                RequiresServer = EditTypeReqServer,
                AllowOrders = EditTypeAllowOrders
            };
            
            var response = await _tableApi.UpdateTableTypeAsync(SelectedType.Id, req);
            if (!response.IsSuccessStatusCode) throw new Exception(response.Error.Content);
            
            CloseTypeEditor();
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
             await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }
}
