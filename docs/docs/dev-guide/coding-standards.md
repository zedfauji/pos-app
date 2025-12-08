# Coding Standards

This document outlines the coding standards and best practices for MagiDesk POS development.

## General Principles

### Code Quality
- **Readability**: Code should be self-documenting
- **Maintainability**: Easy to modify and extend
- **Performance**: Efficient and scalable
- **Security**: Follow security best practices

### Consistency
- Follow established patterns
- Use consistent naming conventions
- Maintain consistent code style

## C# Coding Standards

### Naming Conventions

#### Classes and Interfaces
```csharp
// PascalCase for classes
public class UserService { }

// PascalCase with 'I' prefix for interfaces
public interface IUserService { }
```

#### Methods and Properties
```csharp
// PascalCase for public members
public string UserName { get; set; }
public async Task<User> GetUserAsync(string userId) { }

// camelCase for private members
private string _userName;
private async Task<User> getUserAsync(string userId) { }
```

#### Constants
```csharp
// PascalCase for constants
public const string DefaultRole = "employee";
public static readonly string[] AllPermissions = { };
```

#### Local Variables
```csharp
// camelCase for local variables
var userName = "admin";
var userService = new UserService();
```

### Async/Await

**Always use async/await for I/O operations**:
```csharp
// ✅ Good
public async Task<User> GetUserAsync(string userId)
{
    return await _repository.GetUserAsync(userId);
}

// ❌ Bad
public User GetUser(string userId)
{
    return _repository.GetUser(userId).Result; // Blocking!
}
```

**ConfigureAwait(false) for library code**:
```csharp
public async Task<User> GetUserAsync(string userId)
{
    return await _repository.GetUserAsync(userId).ConfigureAwait(false);
}
```

### Exception Handling

**Use specific exceptions**:
```csharp
// ✅ Good
try
{
    await _service.ProcessOrderAsync(orderId);
}
catch (OrderNotFoundException ex)
{
    _logger.LogWarning(ex, "Order {OrderId} not found", orderId);
    return NotFound();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing order {OrderId}", orderId);
    return StatusCode(500, "Internal server error");
}

// ❌ Bad
try
{
    await _service.ProcessOrderAsync(orderId);
}
catch (Exception ex)
{
    // Too generic!
}
```

**Never swallow exceptions silently**:
```csharp
// ❌ Bad
try
{
    await _service.ProcessOrderAsync(orderId);
}
catch
{
    // Silent failure!
}

// ✅ Good
try
{
    await _service.ProcessOrderAsync(orderId);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing order {OrderId}", orderId);
    throw; // Or handle appropriately
}
```

### Null Safety

**Use null-conditional operators**:
```csharp
// ✅ Good
var userName = user?.UserName ?? "Unknown";

// ❌ Bad
var userName = user != null ? user.UserName : "Unknown";
```

**Use null-forgiving operator sparingly**:
```csharp
// ✅ Good (when you're certain it's not null)
var userName = user!.UserName;

// ❌ Bad (when it might be null)
var userName = user!.UserName; // Could throw NullReferenceException
```

### LINQ

**Prefer method syntax for complex queries**:
```csharp
// ✅ Good
var activeUsers = users
    .Where(u => u.IsActive)
    .OrderBy(u => u.UserName)
    .ToList();

// ❌ Bad (for complex queries)
var activeUsers = from u in users
                  where u.IsActive
                  orderby u.UserName
                  select u;
```

**Use FirstOrDefault() instead of First() when item might not exist**:
```csharp
// ✅ Good
var user = users.FirstOrDefault(u => u.UserId == userId);
if (user == null) return NotFound();

// ❌ Bad
var user = users.First(u => u.UserId == userId); // Throws if not found
```

## WinUI 3 Standards

### XAML Guidelines

**Use x:Bind for better performance**:
```xml
<!-- ✅ Good -->
<TextBlock Text="{x:Bind ViewModel.UserName}" />

<!-- ❌ Bad (slower) -->
<TextBlock Text="{Binding UserName}" />
```

**Use proper namespaces**:
```xml
<!-- ✅ Good (WinUI 3) -->
xmlns:muxc="using:Microsoft.UI.Xaml.Controls"

<!-- ❌ Bad (UWP) -->
xmlns:muxc="using:Windows.UI.Xaml.Controls"
```

**Never use unsupported properties**:
```xml
<!-- ❌ Bad (not supported in WinUI 3) -->
<DatePicker SelectedDateFormat="Short" />
<TextBlock TextTransform="Uppercase" />

<!-- ✅ Good -->
<DatePicker DateChanged="DatePicker_DateChanged" />
<!-- Convert to uppercase in code-behind or converter -->
```

### MVVM Pattern

**ViewModels should implement INotifyPropertyChanged**:
```csharp
public class UserViewModel : INotifyPropertyChanged
{
    private string _userName;
    public string UserName
    {
        get => _userName;
        set
        {
            if (_userName != value)
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

**Use RelayCommand for commands**:
```csharp
public class UserViewModel
{
    public ICommand SaveCommand { get; }
    
    public UserViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
    }
    
    private async Task SaveAsync()
    {
        // Save logic
    }
}
```

### Event Handlers

**Use proper WinUI 3 event signatures**:
```csharp
// ✅ Good
private void DatePicker_DateChanged(object? sender, DatePickerValueChangedEventArgs e)
{
    var newDate = e.NewDate;
}

// ❌ Bad (WPF signature)
private void DatePicker_DateChanged(object sender, SelectionChangedEventArgs e)
{
    // Wrong event args type!
}
```

## API Development Standards

### Controller Structure

**Use attribute routing**:
```csharp
[ApiController]
[Route("api/v2/[controller]")]
public class UsersController : ControllerBase
{
    // Controller implementation
}
```

**Use [RequiresPermission] attribute**:
```csharp
[HttpGet]
[RequiresPermission(Permissions.USER_VIEW)]
public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequest request)
{
    // Implementation
}
```

### Response Format

**Consistent response structure**:
```csharp
// ✅ Good
return Ok(new { data = users, message = "Success" });

// ❌ Bad (inconsistent)
return Ok(users);
return Json(users);
return new JsonResult(users);
```

### Error Handling

**Use proper HTTP status codes**:
```csharp
// ✅ Good
if (user == null) return NotFound();
if (!ModelState.IsValid) return BadRequest(ModelState);
if (unauthorized) return Forbid();

// ❌ Bad
if (user == null) return Ok(null);
if (!ModelState.IsValid) return Ok(ModelState);
```

## Database Standards

### SQL Queries

**Use parameterized queries**:
```csharp
// ✅ Good
var cmd = new NpgsqlCommand(
    "SELECT * FROM users WHERE user_id = @userId", conn);
cmd.Parameters.AddWithValue("@userId", userId);

// ❌ Bad (SQL injection risk)
var cmd = new NpgsqlCommand(
    $"SELECT * FROM users WHERE user_id = '{userId}'", conn);
```

**Use transactions for multi-step operations**:
```csharp
await using var conn = await _dataSource.OpenConnectionAsync(ct);
await using var tx = await conn.BeginTransactionAsync(ct);
try
{
    // Multiple operations
    await tx.CommitAsync(ct);
}
catch
{
    await tx.RollbackAsync(ct);
    throw;
}
```

### Repository Pattern

**Separate data access from business logic**:
```csharp
public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(string userId, CancellationToken ct);
    Task<User> CreateUserAsync(User user, CancellationToken ct);
}

public class UserRepository : IUserRepository
{
    // Implementation
}
```

## Testing Standards

### Unit Tests

**Follow AAA pattern**:
```csharp
[TestMethod]
public async Task GetUser_ValidId_ReturnsUser()
{
    // Arrange
    var userId = "test-user-id";
    var expectedUser = new User { UserId = userId };
    _mockRepository.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedUser);
    
    // Act
    var result = await _service.GetUserAsync(userId);
    
    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(userId, result.UserId);
}
```

### Integration Tests

**Test real API endpoints**:
```csharp
[TestMethod]
public async Task GetUsers_ReturnsUsers()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-User-Id", "test-user-id");
    
    // Act
    var response = await client.GetAsync("/api/v2/users");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var users = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>();
    Assert.IsNotNull(users);
}
```

## Documentation Standards

### XML Comments

**Document public APIs**:
```csharp
/// <summary>
/// Gets a user by their unique identifier.
/// </summary>
/// <param name="userId">The unique identifier of the user.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>The user if found, null otherwise.</returns>
public async Task<User?> GetUserByIdAsync(string userId, CancellationToken ct)
{
    // Implementation
}
```

### README Files

**Include in each project**:
- Purpose of the project
- Setup instructions
- Key components
- How to run tests

## Git Standards

### Commit Messages

**Follow conventional commits**:
```
feat: add user management API
fix: resolve permission check timeout
docs: update API documentation
refactor: simplify order processing logic
test: add integration tests for payment API
```

### Branch Naming

**Use descriptive branch names**:
```
feature/user-management
bugfix/permission-timeout
hotfix/critical-security-patch
refactor/order-service
```

## Code Review Checklist

- [ ] Code follows naming conventions
- [ ] All async operations use async/await
- [ ] Exceptions are handled appropriately
- [ ] Null safety is considered
- [ ] Tests are included
- [ ] Documentation is updated
- [ ] No hardcoded values
- [ ] Security considerations addressed
- [ ] Performance is acceptable
- [ ] Code is readable and maintainable

---

**Last Updated**: 2025-01-02

