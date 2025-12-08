# Performance Tuning

## Overview

This guide covers performance optimization strategies for MagiDesk POS system.

## Performance Metrics

### Target Metrics

| Metric | Target | Current |
|--------|--------|---------|
| **API Response Time (p95)** | < 200ms | ~150ms |
| **Database Query Time (p95)** | < 100ms | ~80ms |
| **Frontend Load Time** | < 2s | ~1.5s |
| **Concurrent Users** | 100+ | 100+ |

## Database Optimization

### Query Optimization

**Use Indexes:**
```sql
-- Create index on frequently queried columns
CREATE INDEX idx_users_username ON users.users(username);
CREATE INDEX idx_orders_created_at ON ord.orders(created_at);
```

**Avoid N+1 Queries:**
```csharp
// Bad: N+1 queries
var users = await _repository.GetUsersAsync();
foreach (var user in users)
{
    var roles = await _repository.GetUserRolesAsync(user.UserId); // N queries
}

// Good: Single query with join
var usersWithRoles = await _repository.GetUsersWithRolesAsync(); // 1 query
```

**Use Pagination:**
```csharp
// Always paginate large result sets
var users = await _service.GetUsersAsync(new UserSearchRequest
{
    Page = 1,
    PageSize = 20
});
```

### Connection Pooling

Configure connection pool size:

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=...;Pooling=true;MinPoolSize=5;MaxPoolSize=20;"
  }
}
```

### Database Maintenance

**Regular Maintenance:**
```sql
-- Analyze tables for query planner
ANALYZE;

-- Vacuum to reclaim space
VACUUM ANALYZE;
```

## API Optimization

### Caching

**Response Caching:**
```csharp
[ResponseCache(Duration = 300)] // Cache for 5 minutes
public async Task<IActionResult> GetMenuItems()
{
    // ...
}
```

**In-Memory Caching:**
```csharp
var cacheKey = $"menu-items-{version}";
if (!_cache.TryGetValue(cacheKey, out var items))
{
    items = await _repository.GetMenuItemsAsync();
    _cache.Set(cacheKey, items, TimeSpan.FromMinutes(5));
}
```

### Async Operations

**Always use async/await:**
```csharp
// Good
public async Task<UserDto> GetUserAsync(string userId)
{
    return await _repository.GetUserByIdAsync(userId);
}

// Bad
public UserDto GetUser(string userId)
{
    return _repository.GetUserById(userId).Result; // Blocks thread
}
```

### Compression

Enable response compression:

```csharp
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
```

## Frontend Optimization

### Lazy Loading

Load data on demand:

```csharp
private async void Page_Loaded(object sender, RoutedEventArgs e)
{
    // Load data asynchronously
    await LoadDataAsync();
}
```

### Virtualization

Use virtualization for large lists:

```xml
<ListView ItemsSource="{x:Bind ViewModel.Items}">
    <ListView.ItemsPanel>
        <ItemsPanelTemplate>
            <ItemsStackPanel AreStickyGroupHeadersEnabled="True" />
        </ItemsPanelTemplate>
    </ListView.ItemsPanel>
</ListView>
```

### Image Optimization

- Use appropriate image sizes
- Lazy load images
- Use WebP format when possible

## Monitoring Performance

### Application Insights

Track performance metrics:

```csharp
_telemetry.TrackDependency("Database", "GetUsers", startTime, duration, success);
```

### Performance Counters

Monitor key metrics:
- Request rate
- Response times
- Error rates
- Resource usage

## Best Practices

1. **Profile First**: Identify bottlenecks before optimizing
2. **Measure**: Always measure before and after
3. **Cache Wisely**: Cache frequently accessed, rarely changing data
4. **Async Everything**: Use async/await for I/O operations
5. **Database First**: Optimize database queries first
6. **Monitor**: Continuously monitor performance

## Common Performance Issues

### Slow Database Queries

**Symptoms:**
- High database CPU usage
- Slow API responses
- Timeout errors

**Solutions:**
- Add indexes
- Optimize queries
- Use connection pooling
- Consider read replicas

### High Memory Usage

**Symptoms:**
- Out of memory errors
- Slow garbage collection
- Service restarts

**Solutions:**
- Review object lifetimes
- Dispose resources properly
- Use object pooling
- Reduce cache sizes

### Slow API Responses

**Symptoms:**
- High response times
- Timeout errors
- Poor user experience

**Solutions:**
- Optimize database queries
- Add caching
- Use async operations
- Scale services

## Performance Testing

### Load Testing

Use tools like:
- **Apache JMeter**
- **k6**
- **Locust**

### Stress Testing

Test system under:
- High load
- Peak traffic
- Resource constraints

### Benchmarking

Establish baseline metrics:
- Response times
- Throughput
- Resource usage

## Continuous Improvement

1. **Regular Reviews**: Weekly performance reviews
2. **Monitor Trends**: Track performance over time
3. **Optimize Proactively**: Don't wait for issues
4. **Document Changes**: Record optimization efforts
5. **Learn**: Study performance patterns
