using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class InventoryManagementViewModel
{
    private readonly ApiService _api;

    public ObservableCollection<InventoryItemDto> InventoryItems { get; } = new();
    public ObservableCollection<InventoryItemDto> FilteredItems { get; } = new();

    public string? SearchText { get; set; }

    public InventoryManagementViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        InventoryItems.Clear();
        FilteredItems.Clear();
        
        try
        {
            // TODO: Replace with actual inventory API call
            // var items = await _api.GetInventoryItemsAsync(ct);
            
            // For now, create some sample data
            var sampleItems = new List<InventoryItemDto>
            {
                new InventoryItemDto { Id = Guid.NewGuid(), Sku = "COF-BEAN-001", Name = "Coffee Beans", Category = "Raw Materials", Quantity = 50, UnitCost = 15.99m },
                new InventoryItemDto { Id = Guid.NewGuid(), Sku = "BUR-PAT-001", Name = "Burger Patties", Category = "Raw Materials", Quantity = 100, UnitCost = 2.50m },
                new InventoryItemDto { Id = Guid.NewGuid(), Sku = "FRI-OIL-001", Name = "Cooking Oil", Category = "Raw Materials", Quantity = 25, UnitCost = 8.99m },
                new InventoryItemDto { Id = Guid.NewGuid(), Sku = "BEV-COK-001", Name = "Coca Cola", Category = "Beverages", Quantity = 200, UnitCost = 1.25m },
                new InventoryItemDto { Id = Guid.NewGuid(), Sku = "BEV-BEE-001", Name = "Corona Beer", Category = "Beverages", Quantity = 48, UnitCost = 2.99m }
            };

            foreach (var item in sampleItems)
            {
                InventoryItems.Add(item);
            }
            
            FilterItems();
        }
        catch (Exception ex)
        {
            // TODO: Handle error properly
            System.Diagnostics.Debug.WriteLine($"Error loading inventory: {ex.Message}");
        }
    }

    public void FilterItems()
    {
        FilteredItems.Clear();
        
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var item in InventoryItems)
            {
                FilteredItems.Add(item);
            }
        }
        else
        {
            var searchLower = SearchText.ToLower();
            foreach (var item in InventoryItems)
            {
                if (item.Name.ToLower().Contains(searchLower) ||
                    item.Sku.ToLower().Contains(searchLower) ||
                    item.Category.ToLower().Contains(searchLower))
                {
                    FilteredItems.Add(item);
                }
            }
        }
    }
}

// TODO: Replace with actual DTO from shared project
public class InventoryItemDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
