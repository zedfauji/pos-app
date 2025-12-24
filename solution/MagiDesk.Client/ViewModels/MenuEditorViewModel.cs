using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Menu;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace MagiDesk.Client.ViewModels
{
    public partial class MenuEditorViewModel : BaseViewModel
    {
        private readonly IMenuApi _menuApi;
        private readonly IDialogService _dialogService;
        private readonly IDispatcherService _dispatcherService;

        [ObservableProperty]
        private ObservableCollection<MenuItemDto> _menuItems = new();

        public MenuEditorViewModel(IMenuApi menuApi, IDialogService dialogService, IDispatcherService dispatcherService)
        {
            _menuApi = menuApi;
            _dialogService = dialogService;
            _dispatcherService = dispatcherService;
        }

        public async Task InitializeAsync()
        {
            await LoadItemsAsync();
        }

        [RelayCommand]
        public async Task LoadItemsAsync()
        {
            IsLoading = true;
            ClearError();
            try
            {
                var response = await _menuApi.ListItemsAsync(new MenuItemQueryDto(null, null, null, null, 1, 100)); // Load first 100 for now
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    _dispatcherService.InvokeOnUIThread(() =>
                    {
                        MenuItems = new ObservableCollection<MenuItemDto>(response.Content.Items);
                    });
                }
                else
                {
                    ErrorMessage = "Failed to load menu items.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading items: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AddItemAsync()
        {
            var result = await _dialogService.ShowMenuItemDialogAsync(null);
            if (result != null)
            {
                IsLoading = true;
                try
                {
                   var response = await _menuApi.CreateMenuItemAsync(result);
                   if (response.IsSuccessStatusCode)
                   {
                       await LoadItemsAsync();
                   }
                   else
                   {
                       await _dialogService.ShowMessageAsync("Error", "Failed to create item.");
                   }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowMessageAsync("Error", $"Failed to create item: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        public async Task EditItemAsync(MenuItemDto item)
        {
            if (item == null) return;

            // Convert to CreateDto for dialog (simplification for reuse) or create specific Edit DTO
            // For now, let's map to CreateDto to prepopulate the dialog
            var existingData = new CreateMenuItemDto(
                item.Sku, item.Name, item.Description, item.Category, item.GroupName, 
                item.BasePrice, item.PictureUrl, item.IsDiscountable, item.IsPartOfCombo, item.IsAvailable
            );

            var result = await _dialogService.ShowMenuItemDialogAsync(existingData, isEdit: true);
            
            if (result != null)
            {
                // Map back to UpdateMenuItemDto
                var updateDto = new UpdateMenuItemDto(
                    result.Name, result.Description, result.Category, result.GroupName, 
                    result.BasePrice, result.PictureUrl, result.IsDiscountable, result.IsPartOfCombo, result.IsAvailable
                );

                IsLoading = true;
                try
                {
                    var response = await _menuApi.UpdateMenuItemAsync(item.Id, updateDto);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadItemsAsync();
                    }
                    else
                    {
                         await _dialogService.ShowMessageAsync("Error", "Failed to update item.");
                    }
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowMessageAsync("Error", $"Failed to update item: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        public async Task DeleteItemAsync(MenuItemDto item)
        {
            if (item == null) return;
            
            // Confirm
            var confirm = await _dialogService.RequestConfirmationAsync("Delete Item", $"Are you sure you want to delete '{item.Name}'?");
            if (!confirm) return;

            IsLoading = true;
            try
            {
                var response = await _menuApi.DeleteMenuItemAsync(item.Id);
                if (response.IsSuccessStatusCode)
                {
                    _dispatcherService.InvokeOnUIThread(() =>
                    {
                        MenuItems.Remove(item);
                    });
                }
                else
                {
                    await _dialogService.ShowMessageAsync("Error", "Failed to delete item.");
                }
            }
            catch (Exception ex)
            {
               await _dialogService.ShowMessageAsync("Error", $"Failed to delete item: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
