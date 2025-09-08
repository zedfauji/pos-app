namespace MagiDesk.Frontend.Services;

public static class ThemeService
{
    public static void Apply(bool dark)
    {
        if (Microsoft.UI.Xaml.Window.Current?.Content is Microsoft.UI.Xaml.FrameworkElement root)
        {
            root.RequestedTheme = dark ? Microsoft.UI.Xaml.ElementTheme.Dark : Microsoft.UI.Xaml.ElementTheme.Light;
        }
    }
}
