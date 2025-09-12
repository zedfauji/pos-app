using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using System.ComponentModel;

namespace MagiDesk.Frontend.ViewModels;

public class InventoryCrudViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;
    private readonly IVendorService _vendorService;

    private ObservableCollection<InventoryItemDto> _items = new();
    private ObservableCollection<InventoryItemDto> _filteredItems = new();
    private string _searchText = string.Empty;
    private string _selectedCategory = string.Empty;
    private string _selectedVendor = string.Empty;
    private string _selectedStatus = string.Empty;

    public ObservableCollection<InventoryItemDto> Items => _filteredItems;
    public ObservableCollection<VendorDto> Vendors { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public InventoryCrudViewModel(IInventoryService inventoryService, IVendorService vendorService)
    {
        _inventoryService = inventoryService;
        _vendorService = vendorService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            // TODO: Replace with actual API call
            // For now, create sample data
            _items.Clear();
            var sampleItems = new[]
            {
                new InventoryItemDto { Id = "1", Sku = "COF-BEAN-001", Name = "Coffee Beans", Category = "Food", Stock = 50, MinStock = 10, UnitCost = 15.99m, IsActive = true },
                new InventoryItemDto { Id = "2", Sku = "BUR-PAT-001", Name = "Burger Patties", Category = "Food", Stock = 25, MinStock = 5, UnitCost = 2.50m, IsActive = true },
                new InventoryItemDto { Id = "3", Sku = "FRI-OIL-001", Name = "Cooking Oil", Category = "Supplies", Stock = 8, MinStock = 3, UnitCost = 8.99m, IsActive = true },
                new InventoryItemDto { Id = "4", Sku = "CUP-PAPER-001", Name = "Paper Cups", Category = "Supplies", Stock = 200, MinStock = 50, UnitCost = 0.25m, IsActive = true },
                new InventoryItemDto { Id = "5", Sku = "NAP-PAPER-001", Name = "Paper Napkins", Category = "Supplies", Stock = 150, MinStock = 30, UnitCost = 0.10m, IsActive = true }
            };

            foreach (var item in sampleItems)
            {
                _items.Add(item);
            }
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        try
        {
            // Return sample categories
            return new[] { "Food", "Beverage", "Supplies", "Equipment", "Raw Material" };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<IEnumerable<VendorDto>> GetVendorsAsync()
    {
        try
        {
            // Return sample vendors
            var sampleVendors = new[]
            {
                new VendorDto { Id = "VENDOR-001", Name = "Coffee Supply Co.", Status = "Active" },
                new VendorDto { Id = "VENDOR-002", Name = "Meat Suppliers Inc.", Status = "Active" },
                new VendorDto { Id = "VENDOR-003", Name = "Supply Solutions", Status = "Active" }
            };
            
            Vendors.Clear();
            foreach (var vendor in sampleVendors)
            {
                Vendors.Add(vendor);
            }
            return sampleVendors;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vendors: {ex.Message}");
            return new List<VendorDto>();
        }
    }

    public async Task<InventoryItemDto?> GetItemAsync(string id)
    {
        try
        {
            return _items.FirstOrDefault(i => i.Id == id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting item: {ex.Message}");
            return null;
        }
    }

    public async Task AddItemAsync(InventoryItemDto item)
    {
        try
        {
            item.Id = Guid.NewGuid().ToString();
            _items.Add(item);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding item: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateItemAsync(string id, InventoryItemDto item)
    {
        try
        {
            var existingItem = _items.FirstOrDefault(i => i.Id == id);
            if (existingItem != null)
            {
                var index = _items.IndexOf(existingItem);
                _items[index] = item;
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating item: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteItemAsync(string id)
    {
        try
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _items.Remove(item);
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
            throw;
        }
    }

    public async Task AdjustStockAsync(string itemId, decimal adjustment, string? notes = null)
    {
        try
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                item.Stock = Math.Max(0, item.Stock + (int)adjustment);
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adjusting stock: {ex.Message}");
            throw;
        }
    }

    public void Search(string searchText)
    {
        _searchText = searchText ?? string.Empty;
        ApplyFilters();
    }

    public void FilterByCategory(string category)
    {
        _selectedCategory = category ?? string.Empty;
        ApplyFilters();
    }

    public void FilterByVendor(string vendorId)
    {
        _selectedVendor = vendorId ?? string.Empty;
        ApplyFilters();
    }

    public void FilterByStatus(string status)
    {
        _selectedStatus = status ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _items.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrEmpty(_searchText))
        {
            filtered = filtered.Where(item => 
                item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                item.Sku.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(_selectedCategory))
        {
            filtered = filtered.Where(item => item.Category == _selectedCategory);
        }

        // Apply vendor filter
        if (!string.IsNullOrEmpty(_selectedVendor))
        {
            filtered = filtered.Where(item => item.VendorId == _selectedVendor);
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(_selectedStatus))
        {
            filtered = _selectedStatus switch
            {
                "LowStock" => filtered.Where(item => item.Stock <= item.MinStock),
                "Active" => filtered.Where(item => item.IsActive),
                "Inactive" => filtered.Where(item => !item.IsActive),
                _ => filtered
            };
        }

        _filteredItems.Clear();
        foreach (var item in filtered)
        {
            _filteredItems.Add(item);
        }

        OnPropertyChanged(nameof(Items));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
