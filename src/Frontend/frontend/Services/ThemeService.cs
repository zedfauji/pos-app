namespace MagiDesk.Frontend.Services;

public static class ThemeService
{
    public static void Apply(bool dark)
    {
        // CRITICAL FIX: Remove Window.Current usage to prevent COM exceptions in WinUI 3 Desktop Apps
        // Window.Current is a Windows Runtime COM interop call that causes Marshal.ThrowExceptionForHR errors
        // Use App.MainWindow instead for thread-safe access
        if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement root)
        {
            root.RequestedTheme = dark ? Microsoft.UI.Xaml.ElementTheme.Dark : Microsoft.UI.Xaml.ElementTheme.Light;
        }
    }
}
