using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Client.ViewModels;

namespace MagiDesk.Client.Views
{
    public class ViewModelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TableTemplate { get; set; } = null!;
        public DataTemplate MenuTemplate { get; set; } = null!;
        public DataTemplate OrderTemplate { get; set; } = null!;
        public DataTemplate InventoryTemplate { get; set; } = null!;
        public DataTemplate ReportsTemplate { get; set; } = null!;
        public DataTemplate LoginTemplate { get; set; } = null!;
        public DataTemplate MenuEditorTemplate { get; set; } = null!;
        public DataTemplate SettingsTemplate { get; set; } = null!;
        public DataTemplate PaymentHubTemplate { get; set; } = null!;
        public DataTemplate PaymentWorkspaceTemplate { get; set; } = null!;

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return GetTemplate(item) ?? base.SelectTemplateCore(item);
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return GetTemplate(item) ?? base.SelectTemplateCore(item, container);
        }

        private DataTemplate GetTemplate(object item)
        {
            return item switch
            {
                TableViewModel => TableTemplate,
                MenuViewModel => MenuTemplate,
                OrderViewModel => OrderTemplate,
                InventoryViewModel => InventoryTemplate,
                ReportsViewModel => ReportsTemplate,
                LoginViewModel => LoginTemplate,
                MenuEditorViewModel => MenuEditorTemplate,
                SettingsViewModel => SettingsTemplate,
                PaymentHubViewModel => PaymentHubTemplate,
                PaymentWorkspaceViewModel => PaymentWorkspaceTemplate,
                _ => null
            };
        }
    }
}
