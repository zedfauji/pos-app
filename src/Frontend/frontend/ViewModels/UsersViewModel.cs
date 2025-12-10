using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MagiDesk.Shared.DTOs.Users;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class UsersViewModel : INotifyPropertyChanged
{
    private readonly UserApiService _userApiService;
    private bool _isLoading;
    private bool _isConnected = true;
    private string _searchTerm = string.Empty;
    private string _selectedRole = "All";
    private bool? _selectedActiveStatus = null;
    private string _sortBy = "Username";
    private bool _sortDescending = false;
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalPages = 1;
    private int _totalUsers = 0;
    private UserDto? _selectedUser;
    private string _statusMessage = string.Empty;
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Success;
    private bool _showStatusMessage = false;

    public UsersViewModel(UserApiService userApiService)
    {
        _userApiService = userApiService;
        Users = new ObservableCollection<UserDto>();
        
        // Commands
        LoadUsersCommand = new RelayCommand(async () => await LoadUsersAsync());
        RefreshCommand = new RelayCommand(async () => await RefreshAsync());
        SearchCommand = new RelayCommand(async () => await SearchAsync());
        CreateUserCommand = new RelayCommand(async () => await CreateUserAsync());
        EditUserCommand = new RelayCommand(async () => await EditUserAsync(), () => SelectedUser != null);
        DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => SelectedUser != null);
        ToggleActiveCommand = new RelayCommand(async () => await ToggleActiveAsync(), () => SelectedUser != null);
        FirstPageCommand = new RelayCommand(async () => await GoToPageAsync(1), () => CurrentPage > 1);
        PreviousPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage - 1), () => CurrentPage > 1);
        NextPageCommand = new RelayCommand(async () => await GoToPageAsync(CurrentPage + 1), () => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(async () => await GoToPageAsync(TotalPages), () => CurrentPage < TotalPages);
        
        // Available options
        RoleOptions = new[] { "All", "admin", "employee" };
        ActiveStatusOptions = new[] { "All", "Active", "Inactive" };
        SortOptions = new[] { "Username", "Role", "CreatedAt", "UpdatedAt", "IsActive" };
    }

    public ObservableCollection<UserDto> Users { get; }
    public string[] RoleOptions { get; }
    public string[] ActiveStatusOptions { get; }
    public string[] SortOptions { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => SetProperty(ref _searchTerm, value);
    }

    public string SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    public string SelectedActiveStatus
    {
        get => _selectedActiveStatus switch
        {
            true => "Active",
            false => "Inactive",
            null => "All"
        };
        set
        {
            var newValue = value switch
            {
                "Active" => true,
                "Inactive" => false,
                _ => (bool?)null
            };
            SetProperty(ref _selectedActiveStatus, newValue);
        }
    }

    public string SortBy
    {
        get => _sortBy;
        set => SetProperty(ref _sortBy, value);
    }

    public bool SortDescending
    {
        get => _sortDescending;
        set => SetProperty(ref _sortDescending, value);
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                UpdatePaginationCommands();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                UpdatePaginationCommands();
            }
        }
    }

    public int TotalUsers
    {
        get => _totalUsers;
        set => SetProperty(ref _totalUsers, value);
    }

    public UserDto? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                UpdateSelectionCommands();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public InfoBarSeverity StatusSeverity
    {
        get => _statusSeverity;
        set => SetProperty(ref _statusSeverity, value);
    }

    public bool ShowStatusMessage
    {
        get => _showStatusMessage;
        set => SetProperty(ref _showStatusMessage, value);
    }

    public string PageInfo => $"Page {CurrentPage} of {TotalPages} ({TotalUsers} total users)";

    // Commands
    public ICommand LoadUsersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand CreateUserCommand { get; }
    public ICommand EditUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand ToggleActiveCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public async Task InitializeAsync()
    {
        await CheckConnectivityAsync();
        await LoadUsersAsync();
    }

    private async Task CheckConnectivityAsync()
    {
        try
        {
            IsConnected = await _userApiService.PingAsync();
            if (!IsConnected)
            {
                ShowStatus("Backend is unavailable. Data may be out of date or actions may fail.", InfoBarSeverity.Warning);
            }
        }
        catch
        {
            IsConnected = false;
            ShowStatus("Backend is unavailable. Data may be out of date or actions may fail.", InfoBarSeverity.Warning);
        }
    }

    private async Task LoadUsersAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            
            var request = new UserSearchRequest
            {
                SearchTerm = string.IsNullOrWhiteSpace(SearchTerm) ? null : SearchTerm,
                Role = SelectedRole == "All" ? null : SelectedRole,
                IsActive = _selectedActiveStatus,
                SortBy = SortBy,
                SortDescending = SortDescending,
                Page = CurrentPage,
                PageSize = PageSize
            };

            var result = await _userApiService.GetUsersPagedAsync(request);
            
            Users.Clear();
            foreach (var user in result.Items)
            {
                Users.Add(user);
            }

            TotalUsers = result.TotalCount;
            TotalPages = result.TotalPages;
            
            OnPropertyChanged(nameof(PageInfo));
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to load users: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAsync()
    {
        await CheckConnectivityAsync();
        await LoadUsersAsync();
        ShowStatus("Refreshed", InfoBarSeverity.Success);
    }

    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadUsersAsync();
    }

    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > TotalPages) return;
        CurrentPage = page;
        await LoadUsersAsync();
    }

    private async Task CreateUserAsync()
    {
        // This will be handled by the View showing a dialog
        // The View will call CreateUserWithDataAsync after getting user input
    }

    public async Task<bool> CreateUserWithDataAsync(string username, string password, string role)
    {
        try
        {
            IsLoading = true;
            
            var request = new CreateUserRequest
            {
                Username = username.Trim(),
                Password = password,
                Role = role
            };

            var user = await _userApiService.CreateUserAsync(request);
            if (user != null)
            {
                await LoadUsersAsync();
                ShowStatus("User created successfully", InfoBarSeverity.Success);
                return true;
            }
            
            ShowStatus("Failed to create user", InfoBarSeverity.Error);
            return false;
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to create user: {ex.Message}", InfoBarSeverity.Error);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EditUserAsync()
    {
        // This will be handled by the View showing a dialog
        // The View will call UpdateUserWithDataAsync after getting user input
    }

    public async Task<bool> UpdateUserWithDataAsync(string? username, string? password, string? role, bool? isActive)
    {
        if (SelectedUser == null) return false;

        try
        {
            IsLoading = true;
            
            var request = new UpdateUserRequest
            {
                Username = username,
                Password = password,
                Role = role,
                IsActive = isActive
            };

            var success = await _userApiService.UpdateUserAsync(SelectedUser.UserId!, request);
            if (success)
            {
                await LoadUsersAsync();
                ShowStatus("User updated successfully", InfoBarSeverity.Success);
                return true;
            }
            
            ShowStatus("Failed to update user", InfoBarSeverity.Error);
            return false;
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to update user: {ex.Message}", InfoBarSeverity.Error);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteUserAsync()
    {
        // This will be handled by the View showing a confirmation dialog
        // The View will call DeleteUserConfirmedAsync after confirmation
    }

    public async Task<bool> DeleteUserConfirmedAsync()
    {
        if (SelectedUser == null) return false;

        try
        {
            IsLoading = true;
            
            var success = await _userApiService.DeleteUserAsync(SelectedUser.UserId!);
            if (success)
            {
                await LoadUsersAsync();
                ShowStatus("User deleted successfully", InfoBarSeverity.Success);
                SelectedUser = null;
                return true;
            }
            
            ShowStatus("Failed to delete user", InfoBarSeverity.Error);
            return false;
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to delete user: {ex.Message}", InfoBarSeverity.Error);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedUser == null) return;

        try
        {
            IsLoading = true;
            
            var request = new UpdateUserRequest
            {
                Username = SelectedUser.Username,
                Role = SelectedUser.Role,
                IsActive = !SelectedUser.IsActive
            };

            var success = await _userApiService.UpdateUserAsync(SelectedUser.UserId!, request);
            if (success)
            {
                await LoadUsersAsync();
                ShowStatus(SelectedUser.IsActive ? "User deactivated" : "User activated", InfoBarSeverity.Success);
            }
            else
            {
                ShowStatus("Failed to toggle user status", InfoBarSeverity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Failed to toggle user status: {ex.Message}", InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        ShowStatusMessage = true;
    }

    private void UpdateSelectionCommands()
    {
        ((RelayCommand)EditUserCommand).RaiseCanExecuteChanged();
        ((RelayCommand)DeleteUserCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ToggleActiveCommand).RaiseCanExecuteChanged();
    }

    private void UpdatePaginationCommands()
    {
        ((RelayCommand)FirstPageCommand).RaiseCanExecuteChanged();
        ((RelayCommand)PreviousPageCommand).RaiseCanExecuteChanged();
        ((RelayCommand)NextPageCommand).RaiseCanExecuteChanged();
        ((RelayCommand)LastPageCommand).RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(PageInfo));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

// Simple RelayCommand implementation
public class RelayCommand : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _executeAsync();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
