using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Menu;
using MagiDesk.Shared.DTOs;
using MagiDesk.Shared.DTOs.Tables;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MagiDesk.Client.ViewModels;

public partial class TableWorkspaceViewModel : ObservableObject
{
    private readonly ITableApi _tableApi;
    private readonly IMenuApi _menuApi;
    private readonly IDialogService _dialogService;
    private readonly IPrinterService _printerService;
    private readonly ShellViewModel _shell;

    [ObservableProperty]
    private string _tableLabel = string.Empty;

    [ObservableProperty]
    private Guid? _sessionId;

    [ObservableProperty]
    private ObservableCollection<MenuItemDto> _menuItems = new();

    [ObservableProperty]
    private ObservableCollection<OrderItemDto> _ticketItems = new();

    [ObservableProperty]
    private ObservableCollection<ItemLine> _orderedItems = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // New Properties for Flexible Tables
    [ObservableProperty]
    private bool _isPreSession = true; // Default to true to show overlay while loading/if empty

    [ObservableProperty]
    private bool _isSessionActive;

    [ObservableProperty]
    private TableConfigDto _tableConfig = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSessionCommand))]
    private string _serverName = string.Empty;

    // Status Bar Properties
    [ObservableProperty]
    private string _sessionStartTime = string.Empty;

    [ObservableProperty]
    private string _currentTime = DateTime.Now.ToString("t");

    // Timer placeholders (could be implemented with DispatcherTimer)
    [ObservableProperty]
    private string _duration = "00:00";

    public TableWorkspaceViewModel(
        ITableApi tableApi, 
        IMenuApi menuApi, 
        IDialogService dialogService, 
        IPrinterService printerService, 
        ShellViewModel shell)
    {
        _tableApi = tableApi;
        _menuApi = menuApi;
        _dialogService = dialogService;
        _printerService = printerService;
        _shell = shell;
        
        // Timer for Current Time update (optional but nice)
        // For simplicity, just set it on load.
    }

    public async Task InitializeAsync(string label)
    {
        TableLabel = label;
        TicketItems.Clear();
        OrderedItems.Clear();
        StatusMessage = string.Empty;
        IsPreSession = true; // Reset state
        IsSessionActive = false;
        
        await LoadSessionDataAsync();
        // Load menu only if we are active or just pre-loading (optional)
        if (IsSessionActive) await LoadMenuAsync();
    }

    private async Task LoadSessionDataAsync()
    {
        try 
        {
            var tables = await _tableApi.GetTablesAsync();
            var table = tables.FirstOrDefault(t => t.Label == TableLabel);
            if (table != null)
            {
                // Update Config
                TableConfig = table.Config ?? new TableConfigDto();
                
                // Determine State
                SessionId = table.CurrentSessionId; 
                IsSessionActive = SessionId.HasValue;
                IsPreSession = !IsSessionActive;

                if (IsSessionActive)
                {
                    // Active State
                    ServerName = table.Server ?? string.Empty; // Read only
                    var startTime = table.StartTime; 
                    if (startTime.HasValue)
                    {
                        SessionStartTime = startTime.Value.ToLocalTime().ToString("t");
                        var diff = DateTime.UtcNow - startTime.Value;
                        Duration = $"{(int)diff.TotalHours}h {diff.Minutes}m";
                    }
                    else
                    {
                         SessionStartTime = "--";
                         Duration = "0h 0m";
                    }

                    var items = await _tableApi.GetItemsAsync(TableLabel);
                    OrderedItems.Clear();
                    foreach(var item in items) OrderedItems.Add(item);
                }
                else
                {
                    // Pre-Session State
                    OrderedItems.Clear(); // Clear any old items
                    // Pre-fill server name from logged in user if available
                    // We access Shell's AuthService via reflection or if available directly?
                    // ShellViewModel exposes AuthService public property
                    /* 
                       Note: AuthService might return null/empty if not logged in.
                    */
                    // Using dynamic check or direct property if allowed
                     ServerName = _shell.AuthService?.CurrentUsername ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading table: {ex.Message}";
        }
    }

    private async Task LoadMenuAsync()
    {
        try
        {
            // Only load if empty to save bandwidth
            if (MenuItems.Any()) return;

            var result = await _menuApi.ListItemsAsync(new MenuItemQueryDto(null, null, null, true));
            if (result.IsSuccessStatusCode && result.Content != null)
            {
                MenuItems.Clear();
                foreach(var item in result.Content.Items) MenuItems.Add(item);
            }
        }
        catch { }
    }

    [RelayCommand(CanExecute = nameof(CanStartSession))]
    public async Task StartSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(ServerName) && TableConfig.RequiresServer)
        {
            await _dialogService.ShowMessageAsync("Validation Error", "Server Name is required to start this table.");
            return;
        }

        try
        {
            // We need a Server ID too. For now, use ServerName as ID or generate one.
            // Ideally use Current User ID.
            string serverId = _shell.AuthService?.CurrentUserId ?? Guid.NewGuid().ToString();

            var request = new StartSessionRequest(serverId, ServerName);
            var result = await _tableApi.StartSessionAsync(TableLabel, request);
            
            if (result.IsSuccessStatusCode)
            {
                await LoadSessionDataAsync(); // Refresh to Active State
                await LoadMenuAsync();
            }
            else
            {
                await _dialogService.ShowMessageAsync("Error", "Failed to start session.");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }

    private bool CanStartSession()
    {
        if (TableConfig?.RequiresServer == true)
        {
            return !string.IsNullOrWhiteSpace(ServerName);
        }
        return true;
    }

    [RelayCommand]
    public void AddToTicket(MenuItemDto item)
    {
        if (!IsSessionActive) return; // Guard

        var existing = TicketItems.FirstOrDefault(x => x.ItemId == item.Id.ToString());
        if (existing != null)
        {
            existing.Quantity++;
            // Trigger property change for UI
            var idx = TicketItems.IndexOf(existing);
            TicketItems[idx] = new OrderItemDto 
            { 
                ItemId = existing.ItemId, 
                Quantity = existing.Quantity, 
                Price = existing.Price 
            }; 
        }
        else
        {
            TicketItems.Add(new OrderItemDto 
            { 
                ItemId = item.Id.ToString(), 
                Quantity = 1, 
                Price = item.SellingPrice
            });
        }
    }

    [RelayCommand]
    public void RemoveFromTicket(OrderItemDto item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity--;
            var idx = TicketItems.IndexOf(item);
             TicketItems[idx] = new OrderItemDto { ItemId = item.ItemId, Quantity = item.Quantity, Price = item.Price };
        }
        else
        {
            TicketItems.Remove(item);
        }
    }

    [RelayCommand]
    public async Task SubmitOrderAsync()
    {
        if (!IsSessionActive) return;
        if (!TicketItems.Any()) return;

        try
        {
            var request = new OrderRequest { Items = TicketItems.ToList() };
            var result = await _tableApi.PostOrderAsync(TableLabel, request);
            
            if (result.IsSuccessStatusCode)
            {
                TicketItems.Clear();
                await LoadSessionDataAsync(); // Refresh ordered items
                await _dialogService.ShowMessageAsync("Success", "Order sent to kitchen.");
            }
            else
            {
                var errorContent = result.Error?.Content ?? "Unknown error";
                await _dialogService.ShowMessageAsync("Error", $"Failed to submit order: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    public void GoBack()
    {
        _shell.NavigateToTables();
    }

    [RelayCommand]
    public void GoToPayments()
    {
        if (SessionId.HasValue)
        {
            // Use navigation parameters to direct the PaymentHub / PaymentWorkspace
            var navParams = new Views.PaymentWorkspaceNavParams(SessionId.Value, Guid.Empty, TableLabel);
            _shell.NavigateToPaymentWorkspace(navParams);
        }
    }

    [RelayCommand]
    public async Task PrintBillAsync()
    {
        try
        {
            var bill = await _tableApi.GetBillPreviewAsync(TableLabel);
            // Mock printing for now
            StatusMessage = "Printing bill...";
            await Task.Delay(1000);
            StatusMessage = "Bill printed.";
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", $"Print failed: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task MoveTableAsync()
    {
        if (!SessionId.HasValue) return;

        try
        {
            // 1. Get Empty Tables
            var tables = await _tableApi.GetTablesAsync();
            var emptyTables = tables.Where(t => t.CurrentSessionId == null && t.Label != TableLabel)
                                    .Select(t => t.Label)
                                    .OrderBy(x => x)
                                    .ToList();

            if (!emptyTables.Any())
            {
                await _dialogService.ShowMessageAsync("Move Failed", "No empty tables available.");
                return;
            }

            // 2. Select Target
            var target = await _dialogService.RequestSelectionAsync("Move Session To...", emptyTables);
            if (string.IsNullOrEmpty(target)) return;

            // 3. Attempt Move (Check Mode)
            var response = await _tableApi.MoveSessionAsync(TableLabel, target, false);
            
            if (!response.IsSuccessStatusCode)
            {
                await _dialogService.ShowMessageAsync("Error", "Failed to initiate move.");
                return;
            }

            var result = response.Content;

            // 4. Handle Confirmation
            if (!result.Success && result.ConfirmationNeeded)
            {
                var confirm = await _dialogService.RequestConfirmationAsync("Billing Change Required", result.Message + "\n\nProceed?");
                if (confirm)
                {
                     var forceResponse = await _tableApi.MoveSessionAsync(TableLabel, target, true);
                     if (forceResponse.IsSuccessStatusCode && forceResponse.Content.Success)
                     {
                         await _dialogService.ShowMessageAsync("Move Complete", forceResponse.Content.Message);
                         _shell.NavigateToTables();
                     }
                     else
                     {
                         await _dialogService.ShowMessageAsync("Error", forceResponse.Content?.Message ?? "Force move failed.");
                     }
                }
            }
            else if (result.Success)
            {
                await _dialogService.ShowMessageAsync("Success", result.Message);
                _shell.NavigateToTables();
            }
            else
            {
                 await _dialogService.ShowMessageAsync("Error", result.Message);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    public async Task EndSessionAsync()
    {
        if (!SessionId.HasValue) return;

        var confirmed = await _dialogService.RequestConfirmationAsync(
            "End Session", 
            "Are you sure you want to end this session? \n\nThis will free the table but leave the bill UNSETTLED if not paid. \n\nUse this only if customers have left without paying via the app (e.g. cash at counter).");
        
        if (!confirmed) return;

        try
        {
            await _tableApi.EndSessionAsync(SessionId.Value); 
            _shell.NavigateToTables();
        }
        catch (Exception ex)
        {
             await _dialogService.ShowMessageAsync("Error", $"Failed to end session: {ex.Message}");
        }
    }
}
