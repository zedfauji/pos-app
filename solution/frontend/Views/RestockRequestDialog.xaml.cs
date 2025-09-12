using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views;

public sealed partial class RestockRequestDialog : ContentDialog
{
    public RestockRequestDto Request { get; private set; }

    public RestockRequestDialog()
    {
        this.InitializeComponent();
        
        Request = new RestockRequestDto
        {
            RequestId = Guid.NewGuid().ToString(),
            Status = "Pending",
            RequestedQuantity = 1,
            UnitCost = 0,
            CreatedDate = DateTime.Now
        };
        
        LoadItems();
        LoadVendors();
    }

    private async void LoadItems()
    {
        try
        {
            // TODO: Load items from inventory service
            // For now, add sample items
            var sampleItems = new[]
            {
                "Coffee Beans",
                "Burger Patties", 
                "Cooking Oil",
                "Paper Cups",
                "Paper Napkins",
                "Salt",
                "Pepper",
                "Sugar",
                "Flour",
                "Cheese"
            };

            foreach (var item in sampleItems)
            {
                ItemComboBox.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
        }
    }

    private async void LoadVendors()
    {
        try
        {
            // TODO: Load vendors from vendor service
            // For now, add sample vendors
            var sampleVendors = new[]
            {
                "Coffee Supply Co.",
                "Meat Suppliers Inc.",
                "Supply Solutions",
                "Fresh Produce Ltd.",
                "Equipment Masters"
            };

            foreach (var vendor in sampleVendors)
            {
                VendorComboBox.Items.Add(vendor);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading vendors: {ex.Message}");
        }
    }

    private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ItemComboBox.SelectedItem is string selectedItem)
        {
            Request.ItemName = selectedItem;
            
            // Update current stock display (mock data)
            var mockStock = GetMockCurrentStock(selectedItem);
            CurrentStockText.Text = mockStock.ToString();
            Request.CurrentStock = mockStock;
        }
    }

    private int GetMockCurrentStock(string itemName)
    {
        // Mock current stock data
        return itemName switch
        {
            "Coffee Beans" => 15,
            "Burger Patties" => 8,
            "Cooking Oil" => 3,
            "Paper Cups" => 45,
            "Paper Napkins" => 25,
            "Salt" => 12,
            "Pepper" => 7,
            "Sugar" => 20,
            "Flour" => 5,
            "Cheese" => 10,
            _ => 0
        };
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate form
        if (string.IsNullOrEmpty(Request.ItemName))
        {
            args.Cancel = true;
            return;
        }

        if (QuantityBox.Value <= 0)
        {
            args.Cancel = true;
            return;
        }

        // Update request with form data
        Request.RequestedQuantity = (int)QuantityBox.Value;
        Request.VendorName = VendorComboBox.SelectedItem?.ToString() ?? "TBD";
        Request.UnitCost = (decimal)UnitCostBox.Value;
        Request.TotalCost = Request.RequestedQuantity * Request.UnitCost;
        Request.Notes = NotesBox.Text.Trim();
    }
}
