using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace MagiDesk.Frontend.Views;

public sealed partial class OrderReceiptDialog : ContentDialog
{
    public Dictionary<string, int> ReceivedItems { get; private set; }

    public OrderReceiptDialog(string orderId)
    {
        this.InitializeComponent();
        Title = $"Receive Order - {orderId}";
        ReceivedItems = new Dictionary<string, int>();
    }
}



