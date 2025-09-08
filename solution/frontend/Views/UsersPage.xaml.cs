using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Auth;
using MagiDesk.Frontend.Services;
using System.Collections.ObjectModel;

namespace MagiDesk.Frontend.Views;

public sealed partial class UsersPage : Page
{
    public ObservableCollection<UserDto> Users { get; } = new();

    public UsersPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        Loaded += UsersPage_Loaded;
    }

    private async void UsersPage_Loaded(object sender, RoutedEventArgs e)
    {
        await CheckConnectivityAsync();
        await LoadAsync();
    }

    private async Task CheckConnectivityAsync()
    {
        try
        {
            var ok = await App.UsersApi!.PingAsync();
            if (!ok)
            {
                Info.Severity = InfoBarSeverity.Warning;
                Info.Message = "Backend is unavailable. Data may be out of date or actions may fail.";
                Info.IsOpen = true;
            }
            else if (Info.IsOpen && Info.Severity == InfoBarSeverity.Warning)
            {
                Info.IsOpen = false;
            }
        }
        catch
        {
            Info.Severity = InfoBarSeverity.Warning;
            Info.Message = "Backend is unavailable. Data may be out of date or actions may fail.";
            Info.IsOpen = true;
        }
    }

    private void Grid_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Select the row under the pointer so context actions target it
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is UserDto user)
        {
            Grid.SelectedItem = user;
        }
    }

    private async Task LoadAsync()
    {
        Users.Clear();
        var api = App.UsersApi!;
        var list = await api.GetUsersAsync();
        foreach (var u in list) Users.Add(u);
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await CheckConnectivityAsync();
        await LoadAsync();
        ShowInfo("Refreshed");
    }

    private async void New_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ContentDialog
        {
            Title = "New User",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        var stack = new StackPanel { Spacing = 8 };
        var userBox = new TextBox { Header = "Username" };
        var passBox = new PasswordBox { Header = "Passcode (digits only)" };
        var roleBox = new ComboBox { Header = "Role", ItemsSource = new[] { "admin", "employee" }, SelectedIndex = 1 };
        stack.Children.Add(userBox);
        stack.Children.Add(passBox);
        stack.Children.Add(roleBox);
        dlg.Content = stack;
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(userBox.Text) || string.IsNullOrWhiteSpace(passBox.Password))
        {
            ShowError("Enter username and passcode"); return;
        }
        if (!passBox.Password.All(char.IsDigit)) { ShowError("Passcode must contain digits only"); return; }
        if (passBox.Password.Length < 6) { ShowError("Passcode must be at least 6 digits"); return; }
        var created = await App.UsersApi!.CreateUserAsync(new CreateUserRequest { Username = userBox.Text.Trim(), Password = passBox.Password, Role = roleBox.SelectedItem?.ToString() ?? "employee" });
        if (created == null) { ShowError("Failed to create user"); return; }
        await LoadAsync();
        ShowInfo("User created");
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserDto u) { ShowError("Select a user"); return; }
        var dlg = new ContentDialog
        {
            Title = "Edit User",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        var stack = new StackPanel { Spacing = 8 };
        var userBox = new TextBox { Header = "Username", Text = u.Username };
        var passBox = new PasswordBox { Header = "Reset Passcode (optional, digits only)" };
        var roleBox = new ComboBox { Header = "Role", ItemsSource = new[] { "admin", "employee" }, SelectedItem = u.Role };
        var activeToggle = new ToggleSwitch { Header = "Active", IsOn = u.IsActive };
        stack.Children.Add(userBox);
        stack.Children.Add(passBox);
        stack.Children.Add(roleBox);
        stack.Children.Add(activeToggle);
        dlg.Content = stack;
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        string? newPass = null;
        if (!string.IsNullOrWhiteSpace(passBox.Password))
        {
            if (!passBox.Password.All(char.IsDigit)) { ShowError("Passcode must contain digits only"); return; }
            if (passBox.Password.Length < 6) { ShowError("Passcode must be at least 6 digits"); return; }
            newPass = passBox.Password;
        }
        var ok = await App.UsersApi!.UpdateUserAsync(u.UserId!, new UpdateUserRequest { Username = userBox.Text.Trim(), Password = newPass, Role = roleBox.SelectedItem?.ToString() ?? u.Role, IsActive = activeToggle.IsOn });
        if (!ok) { ShowError("Failed to update user"); return; }
        await LoadAsync();
        ShowInfo("User updated");
    }

    private async void Deactivate_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserDto u) { ShowError("Select a user"); return; }
        var ok = await App.UsersApi!.UpdateUserAsync(u.UserId!, new UpdateUserRequest { Username = u.Username, Role = u.Role, IsActive = !u.IsActive });
        if (!ok) { ShowError("Failed to toggle active"); return; }
        await LoadAsync();
        ShowInfo(u.IsActive ? "User deactivated" : "User activated");
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not UserDto u) { ShowError("Select a user"); return; }
        var dlg = new ContentDialog { Title = $"Delete {u.Username}?", PrimaryButtonText = "Delete", CloseButtonText = "Cancel", XamlRoot = this.XamlRoot };
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        var ok = await App.UsersApi!.DeleteUserAsync(u.UserId!);
        if (!ok) { ShowError("Delete failed"); return; }
        await LoadAsync();
        ShowInfo("User deleted");
    }

    private void ShowInfo(string msg)
    {
        Info.Severity = InfoBarSeverity.Success; Info.Message = msg; Info.IsOpen = true;
    }
    private void ShowError(string msg)
    {
        Info.Severity = InfoBarSeverity.Error; Info.Message = msg; Info.IsOpen = true;
    }
}
