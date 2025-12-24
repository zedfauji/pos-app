using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Inventory;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Serilog;

namespace MagiDesk.Client.ViewModels
{
    public partial class InventoryViewModel : BaseViewModel
    {
        private readonly IInventoryApi _api;
        private readonly IDialogService _dialogService; // Would need to expand IDialog for inputs, but for now we'll simulate or use simple confirm.

        [ObservableProperty]
        private ObservableCollection<InventoryItemDto> _items = new();

        public InventoryViewModel(IInventoryApi api, IDialogService dialogService)
        {
            _api = api;
            _dialogService = dialogService;
            _ = LoadItemsAsync();
        }

        [RelayCommand]
        public async Task LoadItemsAsync()
        {
            IsLoading = true;
            try
            {
                var response = await _api.GetItemsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    Items.Clear();
                    foreach (var item in response.Content) Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load inventory items");
                await _dialogService.ShowMessageAsync("Error", "Failed to load inventory items. Please try again.");
            }
            finally
            {
                IsLoading = false;
            }
        }
    
        [RelayCommand]
        public async Task AddItemAsync()
        {
             var dialog = new Views.Dialogs.InventoryItemDialog();
             dialog.XamlRoot = App.Current.MainWindow.Content.XamlRoot;
             
             var result = await dialog.ShowAsync();
             if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
             {
                 var newItem = new CreateInventoryItemDto 
                 { 
                    Name = dialog.ItemName,
                    Unit = dialog.ItemUnit, 
                    InitialQuantity = dialog.InitialQuantity, 
                    ReorderLevel = dialog.ReorderLevel 
                 };
                 
                 try 
                 {
                     await _api.CreateItemAsync(newItem);
                     await LoadItemsAsync();
                 }
                 catch (Exception ex)
                 {
                     Log.Error(ex, "Failed to create inventory item: {ItemName}", newItem.Name);
                     await _dialogService.ShowMessageAsync("Error", "Failed to create inventory item. Please try again.");
                 }
             }
        }

        [RelayCommand]
        public async Task DeleteItemAsync(InventoryItemDto item)
        {
             // In a real app we might ask for confirmation
             try 
             {
                 await _api.DeleteItemAsync(item.Id);
                 Items.Remove(item);
             }
             catch (Exception ex)
             {
                 Log.Error(ex, "Failed to delete inventory item: {ItemId}, {ItemName}", item.Id, item.Name);
                 await _dialogService.ShowMessageAsync("Error", "Failed to delete inventory item. Please try again.");
             }
        }

        [RelayCommand]
        public async Task AdjustStockAsync(InventoryItemDto item)
        {
            // Simple +1 adjustment for test
            var adj = new AdjustStockDto { ChangeAmount = 1, Reason = "Manual Add" };
            try 
            {
                await _api.AdjustStockAsync(item.Id, adj);
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to adjust stock for item: {ItemId}, {ItemName}, Change: {ChangeAmount}", item.Id, item.Name, adj.ChangeAmount);
                await _dialogService.ShowMessageAsync("Error", "Failed to adjust stock. Please try again.");
            }
        }
         
        [RelayCommand]
        public async Task ReduceStockAsync(InventoryItemDto item)
        {
            // Simple -1 adjustment for test
            var adj = new AdjustStockDto { ChangeAmount = -1, Reason = "Manual Reduce" };
            try 
            {
                await _api.AdjustStockAsync(item.Id, adj);
                await LoadItemsAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to reduce stock for item: {ItemId}, {ItemName}, Change: {ChangeAmount}", item.Id, item.Name, adj.ChangeAmount);
                await _dialogService.ShowMessageAsync("Error", "Failed to reduce stock. Please try again.");
            }
        }
    }
}
