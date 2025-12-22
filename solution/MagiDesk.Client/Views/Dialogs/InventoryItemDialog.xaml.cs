using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Client.Views.Dialogs
{
    public sealed partial class InventoryItemDialog : ContentDialog
    {
        public string ItemName => NameBox.Text;
        public string ItemUnit => UnitBox.Text;
        public decimal InitialQuantity => (decimal)QuantityBox.Value;
        public decimal ReorderLevel => (decimal)ReorderBox.Value;

        public InventoryItemDialog()
        {
            this.InitializeComponent();
        }
    }
}
