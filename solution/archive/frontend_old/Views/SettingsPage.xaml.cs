namespace MagiDesk.Frontend.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        BaseUrlText.Text = App.Api?.BaseUrl ?? string.Empty;
        StatusText.Text = string.Empty;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var url = BaseUrlText.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(url))
        {
            App.Api?.SetBaseUrl(url!);
            StatusText.Text = "Saved.";
        }
        else
        {
            StatusText.Text = "Enter a valid URL.";
        }
    }
}
