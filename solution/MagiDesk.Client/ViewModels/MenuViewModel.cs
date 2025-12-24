using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Menu;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using Serilog;

namespace MagiDesk.Client.ViewModels;

public partial class MenuViewModel : BaseViewModel
{
    private readonly IMenuApi _menuApi;
    private readonly IDispatcherService _dispatcherService;

    [ObservableProperty]
    private ObservableCollection<MenuItemDto> _items = new();

    public MenuViewModel(IMenuApi menuApi, IDispatcherService dispatcherService)
    {
        _menuApi = menuApi;
        _dispatcherService = dispatcherService;
        LoadMenuCommand.Execute(null);
    }

    [RelayCommand]
    public async Task LoadMenuAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _menuApi.ListItemsAsync(new MenuItemQueryDto(null, null, null, true));
            if (result.IsSuccessStatusCode && result.Content != null)
            {
                _dispatcherService.InvokeOnUIThread(() => 
                {
                    Items.Clear();
                    foreach(var item in result.Content.Items)
                    {
                        Items.Add(item);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load menu items");
            // Error handled silently for now - UI will show empty menu
            // In future: Could show error message to user via IDialogService
        }
        finally
        {
            IsLoading = false;
        }
    }
}
