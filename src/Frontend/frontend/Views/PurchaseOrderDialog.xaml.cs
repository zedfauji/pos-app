using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System;

namespace MagiDesk.Frontend.Views;

public sealed partial class PurchaseOrderDialog : ContentDialog
{
    public VendorOrderDto Order { get; private set; }

    public PurchaseOrderDialog(VendorOrderDto? existingOrder = null)
    {
        this.InitializeComponent();
        
        if (existingOrder != null)
        {
            Order = existingOrder;
            Title = "Edit Purchase Order";
            PrimaryButtonText = "Update Order";
            LoadExistingOrder();
        }
        else
        {
            Order = new VendorOrderDto
            {
                OrderId = string.Empty,
                OrderDate = DateTime.Now,
                ExpectedDeliveryDate = DateTime.Now.AddDays(7),
                Status = "Draft",
                Priority = "Medium",
                TotalValue = 0,
                ItemCount = 0
            };
        }
        
        LoadVendors();
    }

    private void LoadVendors()
    {
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

    private void LoadExistingOrder()
    {
        VendorComboBox.SelectedItem = Order.VendorName;
        
        foreach (ComboBoxItem item in PriorityComboBox.Items)
        {
            if (item.Tag?.ToString() == Order.Priority)
            {
                PriorityComboBox.SelectedItem = item;
                break;
            }
        }
        
        if (Order.ExpectedDeliveryDate.HasValue)
        {
            DeliveryDatePicker.Date = Order.ExpectedDeliveryDate.Value;
        }
        
        NotesBox.Text = Order.Notes ?? string.Empty;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate form
        if (VendorComboBox.SelectedItem == null)
        {
            args.Cancel = true;
            return;
        }

        // Update order with form data
        Order.VendorName = VendorComboBox.SelectedItem.ToString() ?? string.Empty;
        Order.Priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Medium";
        Order.ExpectedDeliveryDate = DeliveryDatePicker.Date.DateTime;
        Order.Notes = NotesBox.Text.Trim();
    }
}
