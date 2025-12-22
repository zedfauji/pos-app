using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views
{
    public sealed partial class OrderPage : UserControl
    {
        public OrderPage()
        {
            this.InitializeComponent();
            // Name the root for ElementName bindings
            this.Content.SetValue(Microsoft.UI.Xaml.FrameworkElement.NameProperty, "RootGrid");
        }
    }
}
