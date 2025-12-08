# UsersViewModel

The `UsersViewModel` manages user data, CRUD operations, and user management UI state.

## Overview

**Location:** `solution/frontend/ViewModels/UsersViewModel.cs`  
**Purpose:** User management operations  
**Pattern:** MVVM with INotifyPropertyChanged

## Key Properties

### Data Collections

```csharp
public ObservableCollection<UserDto> Users { get; }
public string[] RoleOptions { get; }
public string[] ActiveStatusOptions { get; }
public string[] SortOptions { get; }
```

### State Properties

```csharp
public bool IsLoading { get; set; }
public bool IsConnected { get; set; }
public string SearchTerm { get; set; }
public string SelectedRole { get; set; }
public bool? SelectedActiveStatus { get; set; }
public string SortBy { get; set; }
public bool SortDescending { get; set; }
```

### Pagination

```csharp
public int CurrentPage { get; set; }
public int PageSize { get; set; }
public int TotalPages { get; set; }
public int TotalUsers { get; set; }
```

### Selection

```csharp
public UserDto? SelectedUser { get; set; }
```

### Status Messages

```csharp
public string StatusMessage { get; set; }
public InfoBarSeverity StatusSeverity { get; set; }
public bool ShowStatusMessage { get; set; }
```

## Commands

### Data Loading

```csharp
public ICommand LoadUsersCommand { get; }
public ICommand RefreshCommand { get; }
public ICommand SearchCommand { get; }
```

### CRUD Operations

```csharp
public ICommand CreateUserCommand { get; }
public ICommand EditUserCommand { get; }
public ICommand DeleteUserCommand { get; }
public ICommand ToggleActiveCommand { get; }
```

### Pagination

```csharp
public ICommand FirstPageCommand { get; }
public ICommand PreviousPageCommand { get; }
public ICommand NextPageCommand { get; }
public ICommand LastPageCommand { get; }
```

## Key Methods

### LoadUsersAsync

Loads users from the API with current filters and pagination.

```csharp
public async Task LoadUsersAsync()
{
    IsLoading = true;
    try
    {
        var request = new UserSearchRequest
        {
            SearchTerm = SearchTerm,
            Role = SelectedRole == "All" ? null : SelectedRole,
            IsActive = SelectedActiveStatus,
            SortBy = SortBy,
            SortDescending = SortDescending,
            Page = CurrentPage,
            PageSize = PageSize
        };
        
        var response = await _userApiService.GetUsersAsync(request);
        
        Users.Clear();
        foreach (var user in response.Items)
        {
            Users.Add(user);
        }
        
        TotalUsers = response.TotalCount;
        TotalPages = response.TotalPages;
    }
    catch (Exception ex)
    {
        ShowError($"Failed to load users: {ex.Message}");
    }
    finally
    {
        IsLoading = false;
    }
}
```

### CreateUserAsync

Creates a new user.

```csharp
public async Task CreateUserAsync()
{
    // Opens dialog to create user
    // Calls API to create
    // Refreshes user list
}
```

### EditUserAsync

Edits the selected user.

```csharp
public async Task EditUserAsync()
{
    if (SelectedUser == null) return;
    
    // Opens dialog to edit user
    // Calls API to update
    // Refreshes user list
}
```

### DeleteUserAsync

Deletes the selected user.

```csharp
public async Task DeleteUserAsync()
{
    if (SelectedUser == null) return;
    
    // Confirms deletion
    // Calls API to delete
    // Refreshes user list
}
```

### SearchAsync

Performs search with current filters.

```csharp
public async Task SearchAsync()
{
    CurrentPage = 1; // Reset to first page
    await LoadUsersAsync();
}
```

## Usage Example

```csharp
// In XAML page code-behind
var viewModel = new UsersViewModel(App.UsersApi);
this.DataContext = viewModel;

// Load users on page load
await viewModel.LoadUsersAsync();
```

## XAML Binding Example

```xml
<Page DataContext="{x:Bind ViewModel}">
    <Grid>
        <ListView ItemsSource="{x:Bind ViewModel.Users, Mode=OneWay}"
                  SelectedItem="{x:Bind ViewModel.SelectedUser, Mode=TwoWay}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Username}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
        
        <Button Content="Create User"
                Command="{x:Bind ViewModel.CreateUserCommand}" />
        
        <Button Content="Edit"
                Command="{x:Bind ViewModel.EditUserCommand}"
                IsEnabled="{x:Bind ViewModel.SelectedUser, Mode=OneWay, Converter={StaticResource IsNotNullConverter}}" />
    </Grid>
</Page>
```

## Dependencies

- `UserApiService` - Injected via constructor
- `UserDto` - From `MagiDesk.Shared.DTOs.Users`
- `UserSearchRequest` - Search request DTO

## Permissions Required

- `user:view` - To view users
- `user:create` - To create users
- `user:update` - To edit users
- `user:delete` - To delete users

## Related Documentation

- [Users Page](../views/users-page.md)
- [UserApiService](../services/api-services.md)
- [RBAC System](../../security/rbac.md)
