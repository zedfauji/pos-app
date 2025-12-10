using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace MagiDesk.Frontend.Views;

public sealed partial class OrderDetailsDialog : ContentDialog
{
    public OrderDetailsDialog(string orderId)
    {
        this.InitializeComponent();
        Title = $"Order Details - {orderId}";
    }
}



