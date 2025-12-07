# Testing Overview

## Testing Strategy

MagiDesk POS uses a comprehensive testing strategy covering unit tests, integration tests, and end-to-end tests.

## Test Types

### Unit Tests

**Purpose**: Test individual components in isolation

**Coverage**:
- Service methods
- Business logic
- Utility functions
- Data transformations

**Framework**: xUnit

**Location**: `solution/MagiDesk.Tests/`

### Integration Tests

**Purpose**: Test component interactions

**Coverage**:
- API endpoints
- Database operations
- Service integrations
- External API calls

**Framework**: xUnit + ASP.NET Core Test Host

**Location**: `solution/backend/{ApiName}.Tests/`

### End-to-End Tests

**Purpose**: Test complete user workflows

**Coverage**:
- User login flow
- Order creation flow
- Payment processing flow
- Inventory management flow

**Framework**: WinAppDriver (for frontend)

**Location**: `solution/MagiDesk.Tests/`

## Test Organization

```
solution/
├── MagiDesk.Tests/          # Frontend and E2E tests
│   ├── Unit/
│   ├── Integration/
│   └── E2E/
├── backend/
│   ├── UsersApi.Tests/
│   ├── MenuApi.Tests/
│   └── ...
└── shared/
    └── MagiDesk.Shared.Tests/  # Shared library tests
```

## Running Tests

### All Tests

```powershell
# Run all tests
dotnet test solution/MagiDesk.sln
```

### Specific Test Project

```powershell
# Run UsersApi tests
dotnet test solution/backend/UsersApi.Tests/UsersApi.Tests.csproj
```

### Specific Test Class

```powershell
# Run specific test class
dotnet test --filter "FullyQualifiedName~UsersServiceTests"
```

### Specific Test Method

```powershell
# Run specific test
dotnet test --filter "FullyQualifiedName~UsersServiceTests.GetUserByIdAsync_ValidId_ReturnsUser"
```

## Test Coverage

### Coverage Goals

| Component | Target Coverage |
|-----------|----------------|
| Services | > 80% |
| Controllers | > 70% |
| Utilities | > 90% |
| Overall | > 75% |

### Generate Coverage Report

```powershell
# Install coverlet
dotnet tool install -g coverlet.console

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## CI/CD Integration

Tests run automatically in CI/CD pipeline:

1. **On Pull Request**: All tests run
2. **On Merge**: All tests + coverage report
3. **Before Deployment**: Full test suite

See [CI/CD Documentation](../devops/ci-cd.md) for details.

## Best Practices

1. **AAA Pattern**: Arrange, Act, Assert
2. **Test Isolation**: Each test is independent
3. **Descriptive Names**: Clear test method names
4. **Test Data**: Use test fixtures and builders
5. **Mock External Dependencies**: Mock databases, APIs
6. **Fast Tests**: Keep unit tests fast
7. **Maintain Tests**: Keep tests up to date with code

## Test Examples

### Unit Test Example

```csharp
[Fact]
public async Task GetUserByIdAsync_ValidId_ReturnsUser()
{
    // Arrange
    var userId = Guid.NewGuid().ToString();
    var expectedUser = new UserDto { UserId = userId };
    _mockRepository.Setup(r => r.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedUser);

    // Act
    var result = await _service.GetUserByIdAsync(userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(userId, result.UserId);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task GetUsers_ReturnsPagedUsers()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/v2/users?page=1&pageSize=10");

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<PagedResult<UserDto>>(content);
    Assert.NotNull(result);
    Assert.True(result.Items.Count > 0);
}
```

## Test Data Management

### Test Fixtures

Use test fixtures for common test data:

```csharp
public class UserTestFixture
{
    public UserDto CreateTestUser() => new UserDto
    {
        UserId = Guid.NewGuid().ToString(),
        Username = "testuser",
        Role = "Server"
    };
}
```

### Database Seeding

For integration tests, seed test database:

```csharp
protected override void SeedDatabase(DbContext context)
{
    context.Users.Add(new User { Username = "testuser" });
    context.SaveChanges();
}
```

## Troubleshooting

### Tests Failing

1. **Check Test Output**: Review test logs
2. **Check Dependencies**: Verify test dependencies
3. **Check Environment**: Verify test environment setup
4. **Check Data**: Verify test data is correct

### Slow Tests

1. **Use Mocks**: Mock slow dependencies
2. **Parallel Execution**: Enable parallel test execution
3. **Optimize Database**: Use in-memory database for tests
4. **Review Test Scope**: Reduce test scope if needed
