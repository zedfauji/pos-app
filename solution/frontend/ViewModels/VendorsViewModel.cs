using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using System.ComponentModel;

namespace MagiDesk.Frontend.ViewModels;

public class VendorsViewModel : INotifyPropertyChanged
{
    private readonly IVendorService _vendorService;
    private readonly IVendorOrderService _vendorOrderService;

    private ObservableCollection<ExtendedVendorDto> _vendors = new();
    private ObservableCollection<ExtendedVendorDto> _filteredVendors = new();
    private string _searchText = string.Empty;
    private string _selectedStatus = string.Empty;

    public ObservableCollection<ExtendedVendorDto> FilteredVendors => _filteredVendors;

    // Stats properties
    public int TotalVendors { get; set; } = 0;
    public int ActiveVendors { get; set; } = 0;
    public int PendingOrders { get; set; } = 0;
    public string AverageDeliveryTime { get; set; } = "0 days";

    public event PropertyChangedEventHandler? PropertyChanged;

    public VendorsViewModel(IVendorService vendorService, IVendorOrderService vendorOrderService)
    {
        _vendorService = vendorService;
        _vendorOrderService = vendorOrderService;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            // TODO: Replace with actual API call
            // For now, create sample data
            _vendors.Clear();
            var sampleVendors = new[]
            {
                new ExtendedVendorDto 
                { 
                    Id = "VENDOR-001", 
                    Name = "Coffee Supply Co.", 
                    ContactPerson = "John Smith",
                    Phone = "+1-555-0101",
                    Email = "john@coffeesupply.com",
                    Address = "123 Coffee St, Seattle, WA 98101",
                    Status = "Active",
                    PaymentTerms = "Net 30",
                    CreditLimit = 50000m,
                    OrderCount = 15,
                    StatusColor = "Green"
                },
                new ExtendedVendorDto 
                { 
                    Id = "VENDOR-002", 
                    Name = "Meat Suppliers Inc.", 
                    ContactPerson = "Sarah Johnson",
                    Phone = "+1-555-0102",
                    Email = "sarah@meatsuppliers.com",
                    Address = "456 Meat Ave, Portland, OR 97201",
                    Status = "Active",
                    PaymentTerms = "Net 15",
                    CreditLimit = 75000m,
                    OrderCount = 8,
                    StatusColor = "Green"
                },
                new ExtendedVendorDto 
                { 
                    Id = "VENDOR-003", 
                    Name = "Supply Solutions", 
                    ContactPerson = "Mike Davis",
                    Phone = "+1-555-0103",
                    Email = "mike@supplysolutions.com",
                    Address = "789 Supply Blvd, San Francisco, CA 94102",
                    Status = "Inactive",
                    PaymentTerms = "Net 45",
                    CreditLimit = 30000m,
                    OrderCount = 3,
                    StatusColor = "Gray"
                },
                new ExtendedVendorDto 
                { 
                    Id = "VENDOR-004", 
                    Name = "Fresh Produce Ltd.", 
                    ContactPerson = "Lisa Wilson",
                    Phone = "+1-555-0104",
                    Email = "lisa@freshproduce.com",
                    Address = "321 Fresh Way, Los Angeles, CA 90210",
                    Status = "Active",
                    PaymentTerms = "Net 7",
                    CreditLimit = 25000m,
                    OrderCount = 22,
                    StatusColor = "Green"
                },
                new ExtendedVendorDto 
                { 
                    Id = "VENDOR-005", 
                    Name = "Equipment Masters", 
                    ContactPerson = "Robert Brown",
                    Phone = "+1-555-0105",
                    Email = "robert@equipmentmasters.com",
                    Address = "654 Equipment Dr, Chicago, IL 60601",
                    Status = "Suspended",
                    PaymentTerms = "Net 30",
                    CreditLimit = 100000m,
                    OrderCount = 1,
                    StatusColor = "Red"
                }
            };

            foreach (var vendor in sampleVendors)
            {
                _vendors.Add(vendor);
            }
            
            UpdateStats();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vendors: {ex.Message}");
        }
    }

    private void UpdateStats()
    {
        TotalVendors = _vendors.Count;
        ActiveVendors = _vendors.Count(v => v.Status == "Active");
        PendingOrders = _vendors.Sum(v => v.OrderCount);
        AverageDeliveryTime = "3.2 days"; // Sample data
        
        OnPropertyChanged(nameof(TotalVendors));
        OnPropertyChanged(nameof(ActiveVendors));
        OnPropertyChanged(nameof(PendingOrders));
        OnPropertyChanged(nameof(AverageDeliveryTime));
    }

    public async Task<ExtendedVendorDto?> GetVendorAsync(string id)
    {
        try
        {
            return _vendors.FirstOrDefault(v => v.Id == id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting vendor: {ex.Message}");
            return null;
        }
    }

    public async Task AddVendorAsync(ExtendedVendorDto vendor)
    {
        try
        {
            vendor.Id = $"VENDOR-{(_vendors.Count + 1):D003}";
            vendor.StatusColor = vendor.Status switch
            {
                "Active" => "Green",
                "Inactive" => "Gray",
                "Suspended" => "Red",
                _ => "Gray"
            };
            vendor.OrderCount = 0;
            
            _vendors.Add(vendor);
            UpdateStats();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding vendor: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateVendorAsync(string id, ExtendedVendorDto vendor)
    {
        try
        {
            var existingVendor = _vendors.FirstOrDefault(v => v.Id == id);
            if (existingVendor != null)
            {
                var index = _vendors.IndexOf(existingVendor);
                vendor.Id = id; // Preserve the ID
                vendor.StatusColor = vendor.Status switch
                {
                    "Active" => "Green",
                    "Inactive" => "Gray",
                    "Suspended" => "Red",
                    _ => "Gray"
                };
                vendor.OrderCount = existingVendor.OrderCount; // Preserve order count
                
                _vendors[index] = vendor;
                UpdateStats();
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating vendor: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteVendorAsync(string id)
    {
        try
        {
            var vendor = _vendors.FirstOrDefault(v => v.Id == id);
            if (vendor != null)
            {
                _vendors.Remove(vendor);
                UpdateStats();
                ApplyFilters();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting vendor: {ex.Message}");
            throw;
        }
    }

    public void Search(string searchText)
    {
        _searchText = searchText ?? string.Empty;
        ApplyFilters();
    }

    public void FilterByStatus(string status)
    {
        _selectedStatus = status ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _vendors.AsEnumerable();

        if (!string.IsNullOrEmpty(_searchText))
        {
            filtered = filtered.Where(v => 
                v.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                v.ContactPerson.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                v.Phone.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                v.Email.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(_selectedStatus))
        {
            filtered = filtered.Where(v => v.Status.Equals(_selectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        _filteredVendors.Clear();
        foreach (var vendor in filtered.OrderBy(v => v.Name))
        {
            _filteredVendors.Add(vendor);
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}