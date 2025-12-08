# OrdersViewModel

The `OrdersViewModel` manages order data and operations for a session.

## Overview

**Location:** `solution/frontend/ViewModels/OrdersViewModel.cs`  
**Purpose:** Order operations for a table session  
**Pattern:** MVVM with ObservableCollections

## Key Properties

### Data Collections

```csharp
public ObservableCollection<OrderApiService.OrderDto> Orders { get; }
public ObservableCollection<OrderApiService.OrderLogDto> Logs { get; }
```

### State Properties

```csharp
public bool HasError { get; private set; }
public string? ErrorMessage { get; private set; }
private Guid? _sessionId;
```

## Key Methods

### SetSession

Sets the session ID for loading orders.

```csharp
public void SetSession(Guid sessionId) => _sessionId = sessionId;
```

### LoadAsync

Loads orders for the current session.

```csharp
public async Task LoadAsync(bool includeHistory = false, CancellationToken ct = default)
{
    if (!_sessionId.HasValue) 
    { 
        Orders.Clear(); 
        return; 
    }
    await LoadBySessionAsync(_sessionId.Value, includeHistory, ct);
}
```

### LoadBySessionAsync

Loads orders for a specific session.

```csharp
public async Task LoadBySessionAsync(Guid sessionId, bool includeHistory = false, CancellationToken ct = default)
{
    if (_orders == null) return;
    
    Orders.Clear();
    var list = await _orders.GetOrdersBySessionAsync(sessionId, includeHistory, ct);
    foreach (var o in list) Orders.Add(o);
}
```

### AddItemsAsync

Adds items to an order.

```csharp
public async Task AddItemsAsync(
    long orderId, 
    IReadOnlyList<OrderApiService.CreateOrderItemDto> items, 
    CancellationToken ct = default)
{
    if (_orders == null) return;
    
    var updated = await _orders.AddItemsAsync(orderId, items, ct);
    await ReplaceOrderAsync(updated);
}
```

### UpdateItemAsync

Updates an order item.

```csharp
public async Task UpdateItemAsync(
    long orderId, 
    OrderApiService.UpdateOrderItemDto item, 
    CancellationToken ct = default)
{
    if (_orders == null) return;
    
    var updated = await _orders.UpdateItemAsync(orderId, item, ct);
    await ReplaceOrderAsync(updated);
}
```

### DeleteItemAsync

Deletes an order item.

```csharp
public async Task DeleteItemAsync(
    long orderId, 
    long orderItemId, 
    CancellationToken ct = default)
{
    if (_orders == null) return;
    
    var updated = await _orders.DeleteItemAsync(orderId, orderItemId, ct);
    await ReplaceOrderAsync(updated);
}
```

### CloseOrderAsync

Closes an order.

```csharp
public async Task CloseOrderAsync(long orderId, CancellationToken ct = default)
{
    if (_orders == null) return;
    
    var updated = await _orders.CloseOrderAsync(orderId, ct);
    await ReplaceOrderAsync(updated);
}
```

### LoadLogsAsync

Loads order logs for an order.

```csharp
public async Task LoadLogsAsync(
    long orderId, 
    int page = 1, 
    int pageSize = 50, 
    CancellationToken ct = default)
{
    if (_orders == null) return;
    
    Logs.Clear();
    var pr = await _orders.ListLogsAsync(orderId, page, pageSize, ct);
    foreach (var l in pr.Items) Logs.Add(l);
}
```

## Usage Example

```csharp
var viewModel = new OrdersViewModel();
viewModel.SetSession(sessionId);
await viewModel.LoadAsync();

// Add items to order
await viewModel.AddItemsAsync(orderId, items);

// Close order
await viewModel.CloseOrderAsync(orderId);
```

## Dependencies

- `OrderApiService` - From `App.OrdersApi`
- `OrderDto` - Order data transfer object
- `OrderLogDto` - Order log data transfer object

## Error Handling

The ViewModel handles null `OrderApiService` gracefully:

```csharp
if (_orders == null)
{
    HasError = true;
    ErrorMessage = "OrdersApi is not available. Please restart the application or contact support.";
}
```

## Related Documentation

- [Orders Page](../views/orders-page.md)
- [OrderApiService](../services/api-services.md)
- [Order API](../../backend/order-api.md)
