using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MagiDesk.Frontend.ViewModels;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.Dialogs
{
    public sealed partial class MenuItemDialog : ContentDialog
    {
        public MenuItemDetailsViewModel Vm { get; }

        public MenuItemDialog(MenuItemVm item)
        {
            this.InitializeComponent();
            Vm = new MenuItemDetailsViewModel(item);
            this.DataContext = Vm;
            this.Loaded += async (_, __) => await SafeLoadAsync();
        }

        private async Task SafeLoadAsync()
        {
            try 
            { 
                await Vm.LoadAsync(); 
            } 
            catch (Exception ex)
            {
                // Log the error for debugging but don't crash the dialog
            }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (Vm.HasError)
            {
                args.Cancel = true;
                return;
            }

            if (!OrderContext.HasActiveOrder)
            {
                // No active order. Show error message and keep dialog open.
                args.Cancel = true;
                
                // Instead of showing a new dialog, update the current dialog's content
                var errorTextBlock = new TextBlock
                {
                    Text = "Please choose a table first before adding items to the order.",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                
                // Add error message to the dialog content
                if (this.Content is Panel panel)
                {
                    panel.Children.Add(errorTextBlock);
                }
                else
                {
                    // Fallback: create a new StackPanel with existing content and error
                    var stackPanel = new StackPanel();
                    if (this.Content != null)
                    {
                        stackPanel.Children.Add(this.Content as UIElement ?? new TextBlock { Text = this.Content.ToString() });
                    }
                    stackPanel.Children.Add(errorTextBlock);
                    this.Content = stackPanel;
                }
                
                return;
            }

            try
            {
                var ok = await Vm.AddToOrderAsync(OrderContext.CurrentOrderId!.Value);
                if (!ok) args.Cancel = true;
            }
            catch
            {
                args.Cancel = true;
            }
        }
    }
}
