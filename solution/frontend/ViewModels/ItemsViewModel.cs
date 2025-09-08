using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class ItemsViewModel
{
    private readonly ApiService _api;
    public VendorDto? SelectedVendor { get; set; }
    public ObservableCollection<ItemDto> Items { get; } = new();

    public ItemsViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Items.Clear();
        if (SelectedVendor?.Id is null) return;
        var list = await _api.GetItemsAsync(SelectedVendor.Id!, ct);
        foreach (var i in list)
            Items.Add(i);
    }

    public async Task<ItemDto?> CreateAsync(ItemDto dto, CancellationToken ct = default)
    {
        if (SelectedVendor?.Id is null) return null;
        return await _api.CreateItemAsync(SelectedVendor.Id!, dto, ct);
    }

    public async Task<ItemDto?> UpdateAsync(ItemDto dto, CancellationToken ct = default)
    {
        if (SelectedVendor?.Id is null) return null;
        return await _api.UpdateItemAsync(SelectedVendor.Id!, dto, ct);
    }

    public async Task<bool> DeleteAsync(ItemDto dto, CancellationToken ct = default)
    {
        if (SelectedVendor?.Id is null || dto.Id is null) return false;
        return await _api.DeleteItemAsync(SelectedVendor.Id!, dto.Id!, ct);
    }
}
