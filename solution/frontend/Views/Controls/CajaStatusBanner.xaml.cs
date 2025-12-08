using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MagiDesk.Frontend.Views.Controls;

public sealed partial class CajaStatusBanner : UserControl
{
    public static readonly DependencyProperty IsVisibleProperty =
        DependencyProperty.Register(nameof(IsVisible), typeof(bool), typeof(CajaStatusBanner), 
            new PropertyMetadata(false, OnIsVisibleChanged));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(CajaStatusBanner), 
            new PropertyMetadata("", OnStatusTextChanged));

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public CajaStatusBanner()
    {
        this.InitializeComponent();
        Loaded += CajaStatusBanner_Loaded;
    }

    private void CajaStatusBanner_Loaded(object sender, RoutedEventArgs e)
    {
        _ = UpdateStatusAsync();
        
        // Refresh every 30 seconds
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(30);
        timer.Tick += async (s, args) => await UpdateStatusAsync();
        timer.Start();
    }

    private async System.Threading.Tasks.Task UpdateStatusAsync()
    {
        if (App.CajaService == null)
        {
            IsVisible = false;
            return;
        }

        try
        {
            var session = await App.CajaService.GetActiveSessionAsync();
            IsVisible = session != null;
            
            if (session != null)
            {
                StatusText = $"Desde {session.OpenedAt:HH:mm} - Abierta por: {session.OpenedByUserId}";
            }
        }
        catch
        {
            IsVisible = false;
        }
    }

    private static void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CajaStatusBanner banner)
        {
            banner.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnStatusTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CajaStatusBanner banner)
        {
            banner.StatusDetailsText.Text = (string)e.NewValue;
        }
    }
}
