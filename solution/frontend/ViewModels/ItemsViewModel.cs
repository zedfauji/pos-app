using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class ItemsViewModel
{
    private readonly ApiService? _api;
    public VendorDto? SelectedVendor { get; set; }
    public ObservableCollection<ItemDto> Items { get; } = new();
    public bool HasError { get; private set; }
    public string? ErrorMessage { get; private set; }

    public ItemsViewModel(ApiService? api)
    {
        // CRITICAL FIX: Handle null ApiService gracefully instead of throwing exception
        if (api == null)
        {
            HasError = true;
            ErrorMessage = "ApiService is not available. Please restart the application or contact support.";
            return;
        }
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Items.Clear();
        // Legacy vendor items loading removed - using new inventory system
    }

    public async Task<ItemDto?> CreateAsync(ItemDto dto, CancellationToken ct = default)
    {
        // Legacy vendor item creation removed - using new inventory system
        return null;
    }

    public async Task<ItemDto?> UpdateAsync(ItemDto dto, CancellationToken ct = default)
    {
        // Legacy vendor item update removed - using new inventory system
        return null;
    }

    public async Task<bool> DeleteAsync(ItemDto dto, CancellationToken ct = default)
    {
        // Legacy vendor item deletion removed - using new inventory system
        return false;
    }
}
