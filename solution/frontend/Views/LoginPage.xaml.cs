using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Auth;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Views;

public sealed partial class LoginPage : Page
{
    public LoginPage()
    {
        this.InitializeComponent();
        Loaded += LoginPage_Loaded;
    }

    private async void LoginPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize language UI
        ApplyLanguage();
        App.I18n.LanguageChanged += (_, __) => ApplyLanguage();
        // Set selector
        LanguageSelector.SelectedIndex = App.I18n.Current == AppLanguage.Eng ? 0 : 1;

        await SessionService.InitializeAsync();
        if (SessionService.Current != null)
        {
            NavigateToMain();
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        Info.IsOpen = false;
        try
        {
            if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                Info.Message = App.I18n.T("enter_user_passcode");
                Info.IsOpen = true; return;
            }
            // passcode must be digits only
            if (!PasswordBox.Password.All(char.IsDigit))
            {
                Info.Message = App.I18n.T("passcode_digits_only");
                Info.IsOpen = true; return;
            }
            var usersApi = App.UsersApi!;
            var res = await usersApi.LoginAsync(new LoginRequest { Username = UsernameBox.Text.Trim(), Password = PasswordBox.Password });
            if (res == null)
            {
                Info.Message = App.I18n.T("invalid_credentials");
                Info.IsOpen = true; return;
            }
            await SessionService.SaveAsync(new SessionDto { UserId = res.UserId, Username = res.Username, Role = res.Role, LastLoginAt = res.LastLoginAt });
            NavigateToMain();
        }
        catch (System.Exception ex)
        {
            Info.Message = $"Login failed: {ex.Message}";
            Info.IsOpen = true;
        }
    }

    private void NavigateToMain()
    {
        if (this.Parent is Frame f)
        {
            f.Navigate(typeof(MainPage));
        }
        else
        {
            var frame = new Frame();
            frame.Navigate(typeof(MainPage));
            (App.MainWindow!).Content = frame;
        }
    }

    private void ApplyLanguage()
    {
        try
        {
            TitleText.Text = App.I18n.T("sign_in");
            UsernameBox.PlaceholderText = App.I18n.T("username");
            PasswordBox.Header = App.I18n.T("passcode");
            LoginButton.Content = App.I18n.T("login");
            LanguageLabel.Text = App.I18n.T("language");
        }
        catch { }
    }

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var idx = LanguageSelector.SelectedIndex;
        App.I18n.Current = idx == 1 ? AppLanguage.Esp : AppLanguage.Eng;
    }
}
