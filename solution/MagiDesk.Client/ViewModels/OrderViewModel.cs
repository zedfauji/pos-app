using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Menu;
using MagiDesk.Shared.DTOs; // For OrderItemDto
using MagiDesk.Shared.DTOs.Tables; // For OrderRequest
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MagiDesk.Client.ViewModels;

public partial class OrderViewModel : ObservableObject
{
    private readonly ITableApi _tableApi;
    private readonly IMenuApi _menuApi;
    private readonly IDialogService _dialogService;
    private readonly IPrinterService _printerService;
    private readonly ShellViewModel _shell; // To navigate back

    [ObservableProperty]
    private string _tableLabel = string.Empty;

    [ObservableProperty]
    private ObservableCollection<MenuItemDto> _menuItems = new();

    [ObservableProperty]
    private ObservableCollection<OrderItemDto> _ticketItems = new();

    public OrderViewModel(ITableApi tableApi, IMenuApi menuApi, IDialogService dialogService, IPrinterService printerService, ShellViewModel shell)
    {
        _tableApi = tableApi;
        _menuApi = menuApi;
        _dialogService = dialogService;
        _printerService = printerService;
        _shell = shell;
    }

    [ObservableProperty]
    private Guid? _sessionId;

    public async Task InitializeAsync(string label)
    {
        TableLabel = label;
        TicketItems.Clear();
        await LoadMenuAsync();
        await LoadSessionIdAsync();
    }

    private async Task LoadSessionIdAsync()
    {
        try 
        {
            var tables = await _tableApi.GetTablesAsync();
            var table = tables.FirstOrDefault(t => t.Label == TableLabel);
            if (table != null)
            {
               SessionId = table.CurrentSessionId; 
            }
        }
        catch {}
    }

    private async Task LoadMenuAsync()
    {
        try
        {
            var result = await _menuApi.ListItemsAsync(new MenuItemQueryDto(null, null, null, true));
            if (result.IsSuccessStatusCode && result.Content != null)
            {
                MenuItems.Clear();
                foreach(var item in result.Content.Items) MenuItems.Add(item);
            }
        }
        catch { }
    }

    [RelayCommand]
    public void AddToTicket(MenuItemDto item)
    {
        var existing = TicketItems.FirstOrDefault(x => x.ItemId == item.Id.ToString()); // ItemId is string in DTO
        if (existing != null)
        {
            existing.Quantity++;
            // Refresh logic
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
                Price = item.SellingPrice // Map SellingPrice to Price
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
        if (!TicketItems.Any()) return;

        try
        {
            var request = new OrderRequest { Items = TicketItems.ToList() };
            var result = await _tableApi.PostOrderAsync(TableLabel, request);
            
            if (result.IsSuccessStatusCode)
            {
                await _dialogService.ShowMessageAsync("Success", "Order sent to kitchen.");
                _shell.NavigateToTables(); // Go back to map
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
    public void Cancel()
    {
        _shell.NavigateToTables();
    }

    [RelayCommand]
    public async Task ViewOrdersAsync()
    {
        try
        {
            var items = await _tableApi.GetItemsAsync(TableLabel);
            var itemsText = items.Any() 
                ? string.Join("\n", items.Select(i => $"{i.quantity}x {i.name} @ {i.price:C}"))
                : "No orders found for this table session.";
            
            await _dialogService.ShowMessageAsync("Debug: Submitted Orders", 
                $"Table: {TableLabel}\nSession ID: {SessionId}\n\nOrders:\n{itemsText}");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("Error", $"Failed to fetch orders: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task StopSessionAsync()
    {
        try
        {
            if (SessionId == null) 
            {
                await _dialogService.ShowMessageAsync("Error", "Session ID missing.");
                return;
            }

            // Fetch current stats/bill preview
            var items = await _tableApi.GetItemsAsync(TableLabel);
            var total = items.Sum(i => i.price * i.quantity);

            // Show Payment Dialog via robust service (handles locking)
            var request = await _dialogService.ShowPaymentDialogAsync((double)total);
            if (request == null) return;

            var apiResult = await _tableApi.StopSessionAsync(SessionId.Value, request);
            if (!apiResult.IsSuccessStatusCode || apiResult.Content == null)
            {
                await _dialogService.ShowMessageAsync("Error", "Failed to close session.");
                return;
            }

            var bill = apiResult.Content;
            await _printerService.PrintReceiptAsync(bill);
            
            // Give payment dialog time to close fully
            await Task.Delay(500);

            // Ensure UI operations happen on the UI thread
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            app.MainWindow.DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await _dialogService.ShowMessageAsync("Success", $"Session closed. Total: {bill.TotalAmount:C}");
                    _shell.NavigateToTables();
                }
                catch (Exception innerEx)
                {
                     System.Diagnostics.Debug.WriteLine($"Error in UI callback: {innerEx}");
                }
            });
        }
        catch (Exception ex)
        {
             await _dialogService.ShowMessageAsync("Error", ex.Message);
        }
    }

}
