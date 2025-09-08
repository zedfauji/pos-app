namespace MagiDesk.Frontend.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        this.InitializeComponent();
        Loaded += DashboardPage_Loaded;
    }

    private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var vendors = await App.Api!.GetVendorsAsync();
            VendorsCountText.Text = (vendors?.Length ?? 0).ToString();

            int items = 0;
            if (vendors != null)
            {
                foreach (var v in vendors)
                {
                    var vi = await App.Api!.GetItemsAsync(v.Id!);
                    items += vi?.Length ?? 0;
                }
            }
            ItemsCountText.Text = items.ToString();
        }
        catch
        {
            // ignore
        }
    }
}
