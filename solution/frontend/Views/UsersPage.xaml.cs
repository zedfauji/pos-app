using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.DTOs.Auth;

namespace MagiDesk.Frontend.Views;

public sealed partial class UsersPage : Page
{
    public ObservableCollection<UserDto> Users { get; set; } = new();
    
    private UserSearchRequest _currentSearchRequest = new() { Page = 1, PageSize = 20 };
    private PagedResult<UserDto>? _currentResult;
    private bool _isLoading = false;

    public UsersPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        Loaded += UsersPage_Loaded;
    }

    private async void UsersPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
        await LoadUserStatsAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading) return;
        
        try
        {
            _isLoading = true;
            LoadingBar.IsIndeterminate = true;
            LoadingBar.Visibility = Visibility.Visible;
            
            _currentResult = await App.UsersApi!.GetUsersPagedAsync(_currentSearchRequest);
            Users.Clear();
            
            if (_currentResult?.Items != null)
            {
                foreach (var user in _currentResult.Items)
                {
                    Users.Add(user);
                }
            }
            
            UpdatePaginationInfo();
            UpdateUserStats();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load users: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadUserStatsAsync()
    {
        try
        {
            var stats = await App.UsersApi!.GetUserStatsAsync();
            if (stats != null)
            {
                UserStatsText.Text = $"{stats.TotalUsers} total users • {stats.ActiveUsers} active • {stats.AdminUsers} admins";
            }
        }
        catch
        {
            UserStatsText.Text = "Stats unavailable";
        }
    }

    private void UpdatePaginationInfo()
    {
        if (_currentResult != null)
        {
            var start = (_currentResult.Page - 1) * _currentResult.PageSize + 1;
            var end = Math.Min(_currentResult.Page * _currentResult.PageSize, _currentResult.TotalCount);
            PaginationInfo.Text = $"Showing {start}-{end} of {_currentResult.TotalCount} users";
            
            PrevPageButton.IsEnabled = _currentResult.HasPreviousPage;
            NextPageButton.IsEnabled = _currentResult.HasNextPage;
        }
    }

    private void UpdateUserStats()
    {
        var activeCount = Users.Count(u => u.IsActive);
        var roleGroups = Users.GroupBy(u => u.Role).Select(g => $"{g.Count()} {g.Key}").ToList();
        UserStatsText.Text = $"{Users.Count} users loaded • {activeCount} active • {string.Join(", ", roleGroups)}";
    }

    // Event Handlers
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
        await LoadUserStatsAsync();
        ShowInfo("Users refreshed");
    }

    private async void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentSearchRequest = new UserSearchRequest 
        {
            SearchTerm = SearchBox.Text,
            Page = 1,
            PageSize = _currentSearchRequest.PageSize,
            Role = _currentSearchRequest.Role,
            IsActive = _currentSearchRequest.IsActive,
            SortBy = _currentSearchRequest.SortBy,
            SortDescending = _currentSearchRequest.SortDescending
        };
        await LoadAsync();
    }

    private async void RoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RoleFilterCombo.SelectedItem is ComboBoxItem item)
        {
            _currentSearchRequest = new UserSearchRequest 
            {
                SearchTerm = _currentSearchRequest.SearchTerm,
                Page = 1,
                PageSize = _currentSearchRequest.PageSize,
                Role = item.Tag?.ToString(),
                IsActive = _currentSearchRequest.IsActive,
                SortBy = _currentSearchRequest.SortBy,
                SortDescending = _currentSearchRequest.SortDescending
            };
            await LoadAsync();
        }
    }

    private async void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StatusFilterCombo.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString();
            _currentSearchRequest = new UserSearchRequest 
            {
                SearchTerm = _currentSearchRequest.SearchTerm,
                Page = 1,
                PageSize = _currentSearchRequest.PageSize,
                Role = _currentSearchRequest.Role,
                IsActive = string.IsNullOrEmpty(tag) ? null : bool.Parse(tag),
                SortBy = _currentSearchRequest.SortBy,
                SortDescending = _currentSearchRequest.SortDescending
            };
            await LoadAsync();
        }
    }

    private async void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortCombo.SelectedItem is ComboBoxItem item)
        {
            var parts = item.Tag?.ToString()?.Split('_');
            if (parts?.Length == 2)
            {
                _currentSearchRequest = new UserSearchRequest 
                {
                    SearchTerm = _currentSearchRequest.SearchTerm,
                    Page = 1,
                    PageSize = _currentSearchRequest.PageSize,
                    Role = _currentSearchRequest.Role,
                    IsActive = _currentSearchRequest.IsActive,
                    SortBy = parts[0],
                    SortDescending = parts[1] == "desc"
                };
                await LoadAsync();
            }
        }
    }

    private async void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        RoleFilterCombo.SelectedIndex = 0;
        StatusFilterCombo.SelectedIndex = 0;
        SortCombo.SelectedIndex = 0;
        
        _currentSearchRequest = new UserSearchRequest { Page = 1, PageSize = _currentSearchRequest.PageSize };
        await LoadAsync();
        ShowInfo("Filters cleared");
    }

    private async void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentResult?.HasPreviousPage == true)
        {
            _currentSearchRequest = new UserSearchRequest 
            {
                SearchTerm = _currentSearchRequest.SearchTerm,
                Page = _currentSearchRequest.Page - 1,
                PageSize = _currentSearchRequest.PageSize,
                Role = _currentSearchRequest.Role,
                IsActive = _currentSearchRequest.IsActive,
                SortBy = _currentSearchRequest.SortBy,
                SortDescending = _currentSearchRequest.SortDescending
            };
            await LoadAsync();
        }
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (_currentResult?.HasNextPage == true)
        {
            _currentSearchRequest = new UserSearchRequest 
            {
                SearchTerm = _currentSearchRequest.SearchTerm,
                Page = _currentSearchRequest.Page + 1,
                PageSize = _currentSearchRequest.PageSize,
                Role = _currentSearchRequest.Role,
                IsActive = _currentSearchRequest.IsActive,
                SortBy = _currentSearchRequest.SortBy,
                SortDescending = _currentSearchRequest.SortDescending
            };
            await LoadAsync();
        }
    }

    private async void PageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PageSizeCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int pageSize))
        {
            _currentSearchRequest = new UserSearchRequest 
            {
                SearchTerm = _currentSearchRequest.SearchTerm,
                Page = 1,
                PageSize = pageSize,
                Role = _currentSearchRequest.Role,
                IsActive = _currentSearchRequest.IsActive,
                SortBy = _currentSearchRequest.SortBy,
                SortDescending = _currentSearchRequest.SortDescending
            };
            await LoadAsync();
        }
    }

    private void UsersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = UsersListView.SelectedItems.Count > 0;
        EditButton.IsEnabled = UsersListView.SelectedItems.Count == 1;
        ToggleActiveButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private void UsersListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is UserDto user)
        {
            UsersListView.SelectedItem = user;
        }
    }

    private void SelectAll_Checked(object sender, RoutedEventArgs e)
    {
        UsersListView.SelectAll();
    }

    private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
    {
        UsersListView.SelectedItems.Clear();
    }

    private async void BulkAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BulkActionCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
        {
            var action = item.Tag.ToString();
            var selectedUsers = UsersListView.SelectedItems.Cast<UserDto>().ToList();
            
            if (selectedUsers.Count == 0)
            {
                ShowError("No users selected");
                BulkActionCombo.SelectedIndex = 0;
                return;
            }

            await PerformBulkAction(action, selectedUsers);
            BulkActionCombo.SelectedIndex = 0;
        }
    }

    private async Task PerformBulkAction(string action, List<UserDto> users)
    {
        try
        {
            var count = 0;
            foreach (var user in users)
            {
                switch (action)
                {
                    case "activate":
                        if (await App.UsersApi!.UpdateUserAsync(user.UserId!, new UpdateUserRequest { IsActive = true }))
                            count++;
                        break;
                    case "deactivate":
                        if (await App.UsersApi!.UpdateUserAsync(user.UserId!, new UpdateUserRequest { IsActive = false }))
                            count++;
                        break;
                    case "delete":
                        if (await App.UsersApi!.DeleteUserAsync(user.UserId!))
                            count++;
                        break;
                }
            }
            
            await LoadAsync();
            ShowInfo($"Bulk action completed: {count} users {action}d");
        }
        catch (Exception ex)
        {
            ShowError($"Bulk action failed: {ex.Message}");
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var allUsers = await App.UsersApi!.GetUsersPagedAsync(new UserSearchRequest { PageSize = 1000 });
            if (allUsers?.Items != null)
            {
                // Simple CSV export
                var csv = "Username,Role,Status,Created,Updated\n";
                foreach (var user in allUsers.Items)
                {
                    csv += $"{user.Username},{user.Role},{(user.IsActive ? "Active" : "Inactive")},{user.CreatedAt:yyyy-MM-dd},{user.UpdatedAt:yyyy-MM-dd}\n";
                }
                
                var picker = new Windows.Storage.Pickers.FileSavePicker();
                picker.FileTypeChoices.Add("CSV Files", new[] { ".csv" });
                picker.SuggestedFileName = "users_export";
                
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    await Windows.Storage.FileIO.WriteTextAsync(file, csv);
                    ShowInfo("Users exported successfully");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Export failed: {ex.Message}");
        }
    }

    private async void QuickEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UserDto user)
        {
            UsersListView.SelectedItem = user;
            Edit_Click(sender, e);
        }
    }

    private async void QuickToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UserDto user)
        {
            try
            {
                var success = await App.UsersApi!.UpdateUserAsync(user.UserId!, new UpdateUserRequest { IsActive = !user.IsActive });
                if (success)
                {
                    await LoadAsync();
                    ShowInfo($"User {user.Username} {(user.IsActive ? "deactivated" : "activated")}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to toggle user status: {ex.Message}");
            }
        }
    }

    private async void ResetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (UsersListView.SelectedItem is not UserDto user) return;
        
        var dlg = new ContentDialog
        {
            Title = $"Reset Password for {user.Username}",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        
        var passBox = new PasswordBox { Header = "New Password (6+ digits)", PlaceholderText = "Enter new password" };
        dlg.Content = passBox;
        
        var result = await dlg.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(passBox.Password))
        {
            if (!passBox.Password.All(char.IsDigit) || passBox.Password.Length < 6)
            {
                ShowError("Password must be 6+ digits only");
                return;
            }
            
            try
            {
                var success = await App.UsersApi!.UpdateUserAsync(user.UserId!, new UpdateUserRequest { Password = passBox.Password });
                if (success)
                {
                    ShowInfo($"Password reset for {user.Username}");
                }
                else
                {
                    ShowError("Failed to reset password");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Password reset failed: {ex.Message}");
            }
        }
    }

    private async void ViewDetails_Click(object sender, RoutedEventArgs e)
    {
        if (UsersListView.SelectedItem is not UserDto user) return;
        
        var dlg = new ContentDialog
        {
            Title = $"User Details: {user.Username}",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };
        
        var details = new StackPanel { Spacing = 8 };
        details.Children.Add(new TextBlock { Text = $"User ID: {user.UserId}", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas") });
        details.Children.Add(new TextBlock { Text = $"Username: {user.Username}" });
        details.Children.Add(new TextBlock { Text = $"Role: {user.Role}" });
        details.Children.Add(new TextBlock { Text = $"Status: {(user.IsActive ? "Active" : "Inactive")}" });
        details.Children.Add(new TextBlock { Text = $"Created: {user.CreatedAt:F}" });
        details.Children.Add(new TextBlock { Text = $"Last Updated: {user.UpdatedAt:F}" });
        
        dlg.Content = details;
        await dlg.ShowAsync();
    }

    private async void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        var selectedUsers = UsersListView.SelectedItems.Cast<UserDto>().ToList();
        if (selectedUsers.Count == 0) return;
        
        try
        {
            var count = 0;
            foreach (var user in selectedUsers)
            {
                var success = await App.UsersApi!.UpdateUserAsync(user.UserId!, new UpdateUserRequest { IsActive = !user.IsActive });
                if (success) count++;
            }
            
            await LoadAsync();
            ShowInfo($"Status toggled for {count} users");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to toggle status: {ex.Message}");
        }
    }

    private async Task CheckConnectivityAsync()
    {
        try
        {
            var api = App.UsersApi;
            if (api != null)
            {
                // Simple ping to check connectivity
                await api.GetUsersPagedAsync(new UserSearchRequest { Page = 1, PageSize = 1 });
            }
        }
        catch
        {
            ShowError("Unable to connect to Users API");
        }
    }

    // Legacy methods for old XAML compatibility
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
        var roleBox = new ComboBox { Header = "Role", ItemsSource = new[] { "Owner", "Administrator", "Manager", "Server", "Cashier", "Host" }, SelectedIndex = 3 };
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
        var created = await App.UsersApi!.CreateUserAsync(new CreateUserRequest { Username = userBox.Text.Trim(), Password = passBox.Password, Role = roleBox.SelectedItem?.ToString() ?? "Server" });
        if (created == null) { ShowError("Failed to create user"); return; }
        await LoadAsync();
        ShowInfo("User created");
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        var u = UsersListView.SelectedItem as UserDto ?? (sender as FrameworkElement)?.DataContext as UserDto;
        if (u == null) { ShowError("Select a user"); return; }
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
        var roleBox = new ComboBox { Header = "Role", ItemsSource = new[] { "Owner", "Administrator", "Manager", "Server", "Cashier", "Host" }, SelectedItem = u.Role };
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
        var req = new UpdateUserRequest { Username = userBox.Text.Trim(), Password = newPass, Role = roleBox.SelectedItem?.ToString(), IsActive = activeToggle.IsOn };
        var updated = await App.UsersApi!.UpdateUserAsync(u.UserId!, req);
        if (!updated) { ShowError("Failed to update user"); return; }
        await LoadAsync();
        ShowInfo("User updated");
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selectedUsers = UsersListView.SelectedItems.Cast<UserDto>().ToList();
        if (selectedUsers.Count == 0) { ShowError("Select users to delete"); return; }
        
        var dlg = new ContentDialog
        {
            Title = "Delete Users",
            Content = $"Are you sure you want to delete {selectedUsers.Count} user(s)?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };
        
        var res = await dlg.ShowAsync();
        if (res != ContentDialogResult.Primary) return;
        
        var count = 0;
        foreach (var user in selectedUsers)
        {
            if (await App.UsersApi!.DeleteUserAsync(user.UserId!))
                count++;
        }
        
        await LoadAsync();
        ShowInfo($"Deleted {count} users");
    }

    private void ShowError(string message)
    {
        var infoBar = StatusInfoBar ?? FindName("Info") as InfoBar;
        if (infoBar != null)
        {
            infoBar.Message = message;
            infoBar.Severity = InfoBarSeverity.Error;
            infoBar.IsOpen = true;
        }
    }

    private void ShowInfo(string message)
    {
        var infoBar = StatusInfoBar ?? FindName("Info") as InfoBar;
        if (infoBar != null)
        {
            infoBar.Message = message;
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.IsOpen = true;
        }
    }

    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Legacy method for old XAML - redirect to new ListView handler
        UsersListView_SelectionChanged(sender, e);
    }
}
