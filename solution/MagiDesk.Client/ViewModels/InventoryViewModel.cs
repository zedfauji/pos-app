using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Inventory;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MagiDesk.Client.ViewModels
{
    public partial class InventoryViewModel : ObservableObject
    {
        private readonly IInventoryApi _api;
        private readonly IDialogService _dialogService; // Would need to expand IDialog for inputs, but for now we'll simulate or use simple confirm.

        [ObservableProperty]
        private ObservableCollection<InventoryItemDto> _items = new();

        [ObservableProperty]
        private bool _isLoading;

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
                 // Handle error
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
                 catch {}
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
             catch {}
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
             catch {}
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
             catch {}
        }
    }
}
