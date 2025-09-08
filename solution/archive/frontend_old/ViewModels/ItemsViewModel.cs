using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public partial class ItemsViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private VendorDto? selectedVendor;

    public ObservableCollection<ItemDto> Items { get; } = new();

    public ItemsViewModel(ApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        if (SelectedVendor == null || string.IsNullOrEmpty(SelectedVendor.Id)) return;
        IsBusy = true;
        try
        {
            Items.Clear();
            var list = await _api.GetItemsAsync(SelectedVendor.Id!);
            if (list != null)
            {
                foreach (var i in list)
                    Items.Add(i);
            }
        }
        finally { IsBusy = false; }
    }

    public Task<ItemDto?> CreateAsync(ItemDto dto)
        => SelectedVendor == null ? Task.FromResult<ItemDto?>(null) : _api.CreateItemAsync(SelectedVendor.Id!, dto);

    public async Task<ItemDto?> UpdateAsync(ItemDto dto)
    {
        if (SelectedVendor == null || string.IsNullOrWhiteSpace(dto.Id)) return null;
        var ok = await _api.UpdateItemAsync(SelectedVendor.Id!, dto.Id!, dto);
        return ok ? dto : null;
    }

    public async Task<bool> DeleteAsync(ItemDto item)
    {
        if (SelectedVendor == null || string.IsNullOrEmpty(item.Id)) return false;
        return await _api.DeleteItemAsync(SelectedVendor.Id!, item.Id!);
    }
}
