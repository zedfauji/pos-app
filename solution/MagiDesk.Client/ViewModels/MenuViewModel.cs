using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Shared.DTOs.Menu;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace MagiDesk.Client.ViewModels;

public partial class MenuViewModel : ObservableObject
{
    private readonly IMenuApi _menuApi;

    [ObservableProperty]
    private ObservableCollection<MenuItemDto> _items = new();

    [ObservableProperty]
    private bool _isLoading;

    public MenuViewModel(IMenuApi menuApi)
    {
        _menuApi = menuApi;
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
                var app = (App)Microsoft.UI.Xaml.Application.Current;
                app.MainWindow.DispatcherQueue.TryEnqueue(() => 
                {
                    Items.Clear();
                    foreach(var item in result.Content.Items)
                    {
                        Items.Add(item);
                    }
                });
            }
        }
        catch
        {
            // Handle error
        }
        finally
        {
            IsLoading = false;
        }
    }
}
