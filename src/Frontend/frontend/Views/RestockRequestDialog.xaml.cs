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
        }
    }

    private async void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ItemComboBox.SelectedItem is string selectedItem)
        {
            Request.ItemName = selectedItem;
            
            try
            {
                // Get real current stock from inventory service
                if (App.Api != null)
                {
                    var items = await App.Api.GetInventoryAsync();
                    var item = items.FirstOrDefault(i => i.productname == selectedItem);
                    if (item != null)
                    {
                        var currentStock = (int)item.saldo;
                        CurrentStockText.Text = currentStock.ToString();
                        Request.CurrentStock = currentStock;
                    }
                    else
                    {
                        CurrentStockText.Text = "0";
                        Request.CurrentStock = 0;
                    }
                }
                else
                {
                    CurrentStockText.Text = "N/A";
                    Request.CurrentStock = 0;
                }
            }
            catch (Exception ex)
            {
                CurrentStockText.Text = "Error";
                Request.CurrentStock = 0;
            }
        }
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



