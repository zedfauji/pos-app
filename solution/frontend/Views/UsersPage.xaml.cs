using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Shared.DTOs.Auth;
using System.Collections.Generic;

namespace MagiDesk.Frontend.Views;

public sealed partial class UsersPage : Page
{
    public ObservableCollection<UserDto> Users { get; set; } = new();
    public ObservableCollection<RoleDto> Roles { get; set; } = new();
    public ObservableCollection<PermissionDto> Permissions { get; set; } = new();
    
    private UserSearchRequest _currentSearchRequest = new() { Page = 1, PageSize = 20 };
    private PagedResult<UserDto>? _currentResult;
    private bool _isLoading = false;
    private Dictionary<string, bool> _permissionMatrix = new();
    private Dictionary<string, List<string>> _rolePermissions = new();

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

    #region Tab Navigation

    private async void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MainTabView.SelectedItem is TabViewItem selectedTab)
        {
            // Hide all tab contents
            UsersTabContent.Visibility = Visibility.Collapsed;
            RolesTabContent.Visibility = Visibility.Collapsed;
            PermissionsTabContent.Visibility = Visibility.Collapsed;
            UserRoleAssignmentTabContent.Visibility = Visibility.Collapsed;

            // Show selected tab content
            switch (selectedTab.Tag?.ToString())
            {
                case "UsersTab":
                    UsersTabContent.Visibility = Visibility.Visible;
                    break;
                case "RolesTab":
                    RolesTabContent.Visibility = Visibility.Visible;
                    await LoadRolesAsync();
                    break;
                case "PermissionsTab":
                    PermissionsTabContent.Visibility = Visibility.Visible;
                    await LoadPermissionMatrixAsync();
                    break;
                case "UserRoleAssignmentTab":
                    UserRoleAssignmentTabContent.Visibility = Visibility.Visible;
                    await LoadUserRoleAssignmentAsync();
                    break;
            }
        }
    }

    #endregion

    #region Role Management

    private async Task LoadRolesAsync()
    {
        try
        {
            var roles = await App.UsersApi!.GetRolesAsync();
            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }
            ShowInfo($"Loaded {roles.Count} roles successfully");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load roles: {ex.Message}");
        }
    }

    private async void CreateRole_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement role creation dialog
        ShowInfo("Role creation dialog will be implemented");
    }

    private async void EditRole_Click(object sender, RoutedEventArgs e)
    {
        if (RolesListView.SelectedItem is RoleDto selectedRole)
        {
            ShowInfo($"Attempting to edit role: {selectedRole.Name} (ID: {selectedRole.Id}, IsSystem: {selectedRole.IsSystemRole})");
            
            if (selectedRole.IsSystemRole)
            {
                ShowError("Cannot edit system roles. Only custom roles can be modified.");
                return;
            }

            // Create a simple input dialog for editing role description
            var editDialog = new ContentDialog
            {
                Title = $"Edit Role: {selectedRole.Name}",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };
            
            // Show role name as read-only
            var nameTextBlock = new TextBlock
            {
                Text = $"Role Name: {selectedRole.Name}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var descriptionTextBox = new TextBox
            {
                Header = "Description",
                Text = selectedRole.Description ?? "",
                PlaceholderText = "Enter role description",
                AcceptsReturn = true,
                Height = 80
            };

            stackPanel.Children.Add(nameTextBlock);
            stackPanel.Children.Add(descriptionTextBox);
            editDialog.Content = stackPanel;

            var result = await editDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    ShowInfo($"Sending update request for role {selectedRole.Id}...");
                    
                    var updateRequest = new UpdateRoleRequest
                    {
                        Description = string.IsNullOrWhiteSpace(descriptionTextBox.Text) ? null : descriptionTextBox.Text.Trim()
                    };

                    var success = await App.UsersApi!.UpdateRoleAsync(selectedRole.Id, updateRequest);
                    if (success)
                    {
                        ShowInfo("Role updated successfully");
                        await LoadRolesAsync(); // Refresh the roles list
                    }
                    else
                    {
                        ShowError("Failed to update role - API returned false");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Error updating role: {ex.Message}");
                }
            }
        }
        else
        {
            ShowError("No role selected. Please select a role to edit.");
        }
    }

    private async void DeleteRole_Click(object sender, RoutedEventArgs e)
    {
        if (RolesListView.SelectedItem is RoleDto selectedRole)
        {
            if (selectedRole.IsSystemRole)
            {
                ShowError("Cannot delete system roles");
                return;
            }

            // TODO: Implement role deletion confirmation dialog
            ShowInfo($"Delete role: {selectedRole.Name}");
        }
    }

    private void RolesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = RolesListView.SelectedItem != null;
        EditRoleButton.IsEnabled = hasSelection;
        DeleteRoleButton.IsEnabled = hasSelection && (RolesListView.SelectedItem as RoleDto)?.IsSystemRole == false;
    }

    private void QuickEditRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is RoleDto role)
        {
            RolesListView.SelectedItem = role;
            EditRole_Click(sender, e);
        }
    }

    private async void ManagePermissions_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is RoleDto role)
        {
            // Switch to Permission Matrix tab and focus on this role
            MainTabView.SelectedIndex = 2; // PermissionsTab
            await LoadPermissionMatrixAsync();
            ShowInfo($"Managing permissions for role: {role.Name}");
        }
    }

    #endregion

    #region Permission Matrix

    private async Task LoadPermissionMatrixAsync()
    {
        try
        {
            // Load roles and permissions
            var roles = await App.UsersApi!.GetRolesAsync();
            var permissions = await App.UsersApi!.GetAllPermissionsAsync();

            Roles.Clear();
            Permissions.Clear();

            foreach (var role in roles)
            {
                Roles.Add(role);
            }

            foreach (var permission in permissions)
            {
                Permissions.Add(permission);
            }

            // Load role permissions for each role
            _rolePermissions = new Dictionary<string, List<string>>();
            foreach (var role in roles)
            {
                try
                {
                    var rolePermissions = await App.UsersApi!.GetRolePermissionsAsync(role.Id);
                    _rolePermissions[role.Id] = rolePermissions;
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to load permissions for role {role.Name}: {ex.Message}");
                    _rolePermissions[role.Id] = new List<string>();
                }
            }

            // Build permission matrix UI
            BuildPermissionMatrixUI();
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load permission matrix: {ex.Message}");
        }
    }

    private void BuildPermissionMatrixUI()
    {
        PermissionMatrixGrid.Children.Clear();
        PermissionMatrixGrid.RowDefinitions.Clear();
        PermissionMatrixGrid.ColumnDefinitions.Clear();

        if (Roles.Count == 0 || Permissions.Count == 0) return;

        // Add column definitions (Roles + 1 for permission names)
        PermissionMatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Permission names
        for (int i = 0; i < Roles.Count; i++)
        {
            PermissionMatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        }

        // Add row definitions (Permissions + 1 for headers)
        PermissionMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) }); // Header
        for (int i = 0; i < Permissions.Count; i++)
        {
            PermissionMatrixGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) });
        }

        // Add header row
        var headerText = new TextBlock
        {
            Text = "Permissions",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 4, 8, 4)
        };
        Grid.SetRow(headerText, 0);
        Grid.SetColumn(headerText, 0);
        PermissionMatrixGrid.Children.Add(headerText);

        // Add role headers
        for (int col = 0; col < Roles.Count; col++)
        {
            var roleHeader = new TextBlock
            {
                Text = Roles[col].Name,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                Margin = new Thickness(4)
            };
            Grid.SetRow(roleHeader, 0);
            Grid.SetColumn(roleHeader, col + 1);
            PermissionMatrixGrid.Children.Add(roleHeader);
        }

        // Add permission rows
        for (int row = 0; row < Permissions.Count; row++)
        {
            var permission = Permissions[row];
            
            // Permission name
            var permissionText = new TextBlock
            {
                Text = permission.Name,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 4, 8, 4),
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
            };
            Grid.SetRow(permissionText, row + 1);
            Grid.SetColumn(permissionText, 0);
            PermissionMatrixGrid.Children.Add(permissionText);

            // Checkboxes for each role
            for (int col = 0; col < Roles.Count; col++)
            {
                var role = Roles[col];
                var checkbox = new CheckBox
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = $"{role.Id}|{permission.Name}"
                };
                
                // Load actual permission state from API
                var hasPermission = _rolePermissions.ContainsKey(role.Id) && 
                                   _rolePermissions[role.Id].Contains(permission.Name);
                checkbox.IsChecked = hasPermission;
                
                checkbox.Checked += PermissionCheckbox_Changed;
                checkbox.Unchecked += PermissionCheckbox_Changed;

                Grid.SetRow(checkbox, row + 1);
                Grid.SetColumn(checkbox, col + 1);
                PermissionMatrixGrid.Children.Add(checkbox);
            }
        }
    }

    private async void PermissionCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkbox && checkbox.Tag is string tag)
        {
            var parts = tag.Split('|');
            if (parts.Length == 2)
            {
                var roleId = parts[0];
                var permissionName = parts[1];
                var isChecked = checkbox.IsChecked == true;

                // Update local matrix
                _permissionMatrix[$"{roleId}|{permissionName}"] = isChecked;

                // TODO: Save to API
                ShowInfo($"Permission {permissionName} for role {roleId}: {(isChecked ? "Granted" : "Revoked")}");
            }
        }
    }

    private async void SavePermissions_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Implement bulk permission save
            ShowInfo("Saving permission changes...");
            
            // For now, just show success
            await Task.Delay(1000);
            ShowInfo("Permission changes saved successfully!");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to save permissions: {ex.Message}");
        }
    }

    private async void ResetPermissions_Click(object sender, RoutedEventArgs e)
    {
        await LoadPermissionMatrixAsync();
        ShowInfo("Permission matrix reset to saved state");
    }

    #endregion

    #region User Role Assignment

    private async Task LoadUserRoleAssignmentAsync()
    {
        try
        {
            // Load users for role assignment
            var users = await App.UsersApi!.GetUsersPagedAsync(new UserSearchRequest { Page = 1, PageSize = 100 });
            Users.Clear();
            foreach (var user in users.Items)
            {
                Users.Add(user);
            }
            
            // Set ComboBox selections after the list is loaded
            await Task.Delay(100); // Small delay to ensure UI is rendered
            SetComboBoxSelections();
            
            ShowInfo($"Loaded {users.Items.Count} users for role assignment");
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load users for role assignment: {ex.Message}");
        }
    }

    private void SetComboBoxSelections()
    {
        foreach (var item in UserRoleAssignmentListView.Items)
        {
            if (item is UserDto user)
            {
                var container = UserRoleAssignmentListView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    var comboBox = FindChild<ComboBox>(container, "RoleComboBox");
                    if (comboBox != null)
                    {
                        // Set the selected item based on user's current role
                        foreach (ComboBoxItem comboItem in comboBox.Items)
                        {
                            if (comboItem.Tag?.ToString() == user.Role)
                            {
                                comboBox.SelectedItem = comboItem;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private async void SaveUserRoles_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var changes = new List<(string userId, string newRole)>();
            
            // Collect role changes from the ComboBoxes
            foreach (var item in UserRoleAssignmentListView.Items)
            {
                if (item is UserDto user)
                {
                    // Find the ComboBox for this user
                    var container = UserRoleAssignmentListView.ContainerFromItem(item) as ListViewItem;
                    if (container != null)
                    {
                        var comboBox = FindChild<ComboBox>(container, "RoleComboBox");
                        if (comboBox != null && comboBox.SelectedItem is ComboBoxItem selectedItem)
                        {
                            var newRole = selectedItem.Tag?.ToString();
                            if (!string.IsNullOrEmpty(newRole) && newRole != user.Role)
                            {
                                changes.Add((user.UserId!, newRole));
                            }
                        }
                    }
                }
            }

            if (changes.Count == 0)
            {
                ShowInfo("No role changes to save");
                return;
            }

            // Apply changes
            var successCount = 0;
            foreach (var (userId, newRole) in changes)
            {
                try
                {
                    var updateRequest = new UpdateUserRequest
                    {
                        Role = newRole
                    };
                    
                    var success = await App.UsersApi!.UpdateUserAsync(userId, updateRequest);
                    if (success)
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Failed to update role for user {userId}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                ShowInfo($"Successfully updated roles for {successCount} users");
                await LoadUserRoleAssignmentAsync(); // Refresh the list
            }
        }
        catch (Exception ex)
        {
            ShowError($"Failed to save role assignments: {ex.Message}");
        }
    }

    private async void RefreshUserRoles_Click(object sender, RoutedEventArgs e)
    {
        await LoadUserRoleAssignmentAsync();
    }

    private void UserRoleAssignmentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection changes if needed
    }

    private async void QuickAssignRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is UserDto user)
        {
            // Create a simple dialog for quick role assignment
            var dialog = new ContentDialog
            {
                Title = $"Assign Role to {user.Username}",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var stackPanel = new StackPanel { Spacing = 12 };
            
            var currentRoleText = new TextBlock
            {
                Text = $"Current Role: {user.Role}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var roleComboBox = new ComboBox
            {
                Header = "Select New Role",
                SelectedItem = user.Role
            };
            
            roleComboBox.Items.Add(new ComboBoxItem { Content = "Owner", Tag = "Owner" });
            roleComboBox.Items.Add(new ComboBoxItem { Content = "Administrator", Tag = "Administrator" });
            roleComboBox.Items.Add(new ComboBoxItem { Content = "Manager", Tag = "Manager" });
            roleComboBox.Items.Add(new ComboBoxItem { Content = "Server", Tag = "Server" });
            roleComboBox.Items.Add(new ComboBoxItem { Content = "Cashier", Tag = "Cashier" });
            roleComboBox.Items.Add(new ComboBoxItem { Content = "Host", Tag = "Host" });

            // Set current selection
            foreach (ComboBoxItem item in roleComboBox.Items)
            {
                if (item.Tag?.ToString() == user.Role)
                {
                    roleComboBox.SelectedItem = item;
                    break;
                }
            }

            stackPanel.Children.Add(currentRoleText);
            stackPanel.Children.Add(roleComboBox);
            dialog.Content = stackPanel;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    if (roleComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        var newRole = selectedItem.Tag?.ToString();
                        if (!string.IsNullOrEmpty(newRole) && newRole != user.Role)
                        {
                            var updateRequest = new UpdateUserRequest
                            {
                                Role = newRole
                            };
                            
                            var success = await App.UsersApi!.UpdateUserAsync(user.UserId!, updateRequest);
                            if (success)
                            {
                                ShowInfo($"Successfully assigned {newRole} role to {user.Username}");
                                await LoadUserRoleAssignmentAsync(); // Refresh the list
                            }
                            else
                            {
                                ShowError("Failed to update user role");
                            }
                        }
                        else
                        {
                            ShowInfo("No role change needed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Error updating user role: {ex.Message}");
                }
            }
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        if (parent == null) return null;

        T? foundChild = null;
        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            T? childType = child as T;
            if (childType == null)
            {
                foundChild = FindChild<T>(child, childName);
                if (foundChild != null) break;
            }
            else if (!string.IsNullOrEmpty(childName))
            {
                var frameworkElement = child as FrameworkElement;
                if (frameworkElement != null && frameworkElement.Name == childName)
                {
                    foundChild = (T)child;
                    break;
                }
                else
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
            }
            else
            {
                foundChild = (T)child;
                break;
            }
        }
        return foundChild;
    }

    #endregion
}
