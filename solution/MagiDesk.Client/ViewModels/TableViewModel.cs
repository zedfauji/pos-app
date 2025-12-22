using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Client.Services.Dtos; // For StartSessionRequest
using MagiDesk.Shared.DTOs.Tables;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;

using Serilog;

namespace MagiDesk.Client.ViewModels;

public partial class TableViewModel : ObservableObject, IDisposable
{
    private readonly ITableApi _tableApi;
    private readonly IDialogService _dialogService;
    private readonly IPrinterService _printerService;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty]
    private ObservableCollection<TableStatusDto> _tables = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    private readonly ShellViewModel _shellViewModel;

    public TableViewModel(ITableApi tableApi, IDialogService dialogService, IPrinterService printerService, ShellViewModel shellViewModel)
    {
        _tableApi = tableApi;
        _dialogService = dialogService;
        _printerService = printerService;
        _shellViewModel = shellViewModel;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(5)); // Poll every 5s
        _ = PollTablesAsync();
    }
    
    private async Task PollTablesAsync()
    {
        while (await _timer.WaitForNextTickAsync(_cts.Token))
        {
            await LoadTablesAsync();
        }
    }

    [RelayCommand]
    public async Task LoadTablesAsync()
    {
        // System.Diagnostics.Debug.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} LoadTablesAsync: Start");
        try
        {
            var tables = await _tableApi.GetTablesAsync();
            
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(() => 
            {
                try
                {
                    // System.Diagnostics.Debug.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss.fff} LoadTablesAsync: UI Update Start. Count={tables.Count}");
                    if (Tables.Count == 0)
                    {
                         foreach(var t in tables) Tables.Add(t);
                    }
                    else
                    {
                        foreach(var t in tables)
                        {
                            var existing = Tables.FirstOrDefault(x => x.Label == t.Label);
                            if (existing != null)
                            {
                                if (existing != t)
                                {
                                    var idx = Tables.IndexOf(existing);
                                    Tables[idx] = t;
                                }
                            }
                            else
                            {
                                Tables.Add(t);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadTables UI update failed: {ex}");
                }
            });
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Failed to load tables: " + ex.Message;
        }
    }

    [RelayCommand]
    public async Task SelectTableAsync(TableStatusDto table)
    {
        if (table == null) return;
        
        // Always navigate to the Table Workspace.
        // The Workspace handles both Active (Order) and Inactive (Pre-Session) states.
        _shellViewModel.NavigateToOrder(table.Label);
        
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task MoveTableAsync(TableStatusDto table)
    {
        if (table == null || !table.Occupied) return;

        var freeTables = Tables.Where(t => !t.Occupied).Select(t => t.Label).ToList();
        if (!freeTables.Any())
        {
            await _dialogService.ShowMessageAsync("Info", "No free tables available.");
            return;
        }

        var targetParams = await _dialogService.RequestSelectionAsync("Move to...", freeTables);
        if (string.IsNullOrEmpty(targetParams)) return;

        try
        {
            var result = await _tableApi.MoveSessionAsync(table.Label, targetParams);
            if (!result.IsSuccessStatusCode)
            {
                var errorContent = result.Error?.Content ?? "Unknown error";
                 await _dialogService.ShowMessageAsync("Error", $"Failed to move table: {errorContent}");
            }
            else
            {
                 await LoadTablesAsync();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }
    


    [RelayCommand]
    public async Task CloseSessionAsync(TableStatusDto table)
    {
        if (table == null || !table.Occupied) return;

        try
        {
            if (table.CurrentSessionId == null)
            {
                await _dialogService.ShowMessageAsync("Error", "Session ID missing on table.");
                return;
            }

            // Fetch Bill Preview to get BillingId
            var preview = await _tableApi.GetBillPreviewAsync(table.Label);
            
            Log.Information($"[CloseSession] Navigating to Payment Workspace for table {table.Label}");
            
            // Navigate to Payment Workspace instead of showing dialog
            // Note: BillingId = SessionId in this system
            var navParams = new Views.PaymentWorkspaceNavParams(
                table.CurrentSessionId.Value,
                table.CurrentSessionId.Value,  // BillingId = SessionId
                table.Label
            );
            
            _shellViewModel.NavigateToPaymentWorkspace(navParams);
        }
        catch (Exception ex)
        {
            Log.Error($"[CloseSession] ERROR: {ex}");
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer.Dispose();
    }
}
