# Table Management

Complete documentation for table management system in MagiDesk POS.

## Overview

The table management system handles billiard and bar table sessions, including starting sessions, tracking time, managing orders, generating bills, and stopping sessions.

## Architecture

### Core Components

1. **TablesPage.xaml.cs** - Main UI controller for table management
2. **TableRepository.cs** - Data access layer for table operations
3. **TablesApi** - Backend API service for table management
4. **OrderApiService** - Integration with order management system

### Database Schema

- **table_status** - Current state of all tables
- **table_sessions** - Active and historical table sessions
- **bills** - Generated bills from completed sessions
- **orders** - Orders associated with table sessions

## Table Start Flow

### Frontend Process

```csharp
private async void StartMenu_Click(object sender, RoutedEventArgs e)
{
    // 1. Validate table selection
    if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
    
    // 2. Prompt for server selection
    var (ok, user) = await PromptSelectEmployeeAsync();
    if (!ok || user == null) return;
    
    // 3. Start session via API
    var startResult = await _repo.StartSessionAsync(item.Label, user.UserId, user.Username);
    
    // 4. Handle start result
    if (!startResult.ok) {
        // Show error dialog
        return;
    }
    
    // 5. Set context and create/find order
    OrderContext.CurrentSessionId = startResult.sessionId;
    OrderContext.CurrentBillingId = startResult.billingId;
    
    // 6. Create or find existing order
    var ordersSvc = App.OrdersApi;
    if (ordersSvc != null && Guid.TryParse(startResult.sessionId, out var sessionGuid)) {
        var list = await ordersSvc.GetOrdersBySessionAsync(sessionGuid);
        var current = list.FirstOrDefault(o => o.SessionId == sessionGuid);
        
        if (current == null) {
            // Create new order
            var req = new CreateOrderRequestDto(sessionGuid, billingGuid, item.Label, user.UserId, user.Username, Array.Empty<CreateOrderItemDto>());
            var created = await ordersSvc.CreateOrderAsync(req);
            if (created != null) {
                OrderContext.CurrentOrderId = created.Id;
            }
        } else {
            OrderContext.CurrentOrderId = current.Id;
        }
    }
    
    // 7. Refresh UI
    await RefreshFromStoreAsync();
}
```

### Backend Process

```csharp
app.MapPost("/tables/{label}/start", async (string label, StartSessionRequest req) =>
{
    // 1. Check for existing active session
    const string hasActiveSql = "SELECT COUNT(1) FROM public.table_sessions WHERE table_label = @label AND status = 'active'";
    var count = await cmd.ExecuteScalarAsync();
    if (count > 0) return Results.Conflict("Active session already exists");
    
    // 2. Create new session
    var sessionId = Guid.NewGuid();
    var billingId = Guid.NewGuid();
    var now = DateTime.UtcNow;
    
    const string insertSql = @"INSERT INTO public.table_sessions(session_id, table_label, server_id, server_name, start_time, status, billing_id)
                               VALUES(@sid, @label, @srvId, @srvName, @start, 'active', @bid)";
    
    // 3. Update table status
    const string updateSql = @"UPDATE public.table_status SET occupied = true, start_time = @start, server = @srvName, updated_at = now() WHERE label = @label";
    
    // 4. Return session details
    return Results.Ok(new { sessionId, billingId });
});
```

## Table Stop Flow

### Frontend Process

```csharp
private async void StopMenu_Click(object sender, RoutedEventArgs e)
{
    // 1. Validate table selection
    if ((sender as FrameworkElement)?.DataContext is not TableItem item) return;
    
    // 2. Show progress dialog
    var progressDialog = new ContentDialog { Title = "Stopping Session", Content = $"Stopping session for table {item.Label}..." };
    progressDialog.ShowAsync();
    
    try {
        // 3. Validate table state
        if (!item.Occupied) {
            // Show error - table not occupied
            return;
        }
        
        // 4. Close any open orders
        var ordersSvc = App.OrdersApi;
        if (ordersSvc != null) {
            var (activeSessionId, activeBillingId) = await _repo.GetActiveSessionForTableAsync(item.Label);
            if (activeSessionId.HasValue) {
                var openOrders = await ordersSvc.GetOrdersBySessionAsync(activeSessionId.Value, includeHistory: false);
                var ordersToClose = openOrders.Where(o => o.Status == "open").ToList();
                
                foreach (var order in ordersToClose) {
                    try {
                        await ordersSvc.CloseOrderAsync(order.Id);
                    } catch (Exception ex) {
                        // Log error but continue
                    }
                }
            }
        }
        
        // 5. Stop the table session
        var stopResult = await _repo.StopSessionAsync(item.Label);
        progressDialog.Hide();
        
        // 6. Handle result and refresh UI
        await RefreshFromStoreAsync();
        
        // 7. Handle bill display/printing
        if (stopResult.bill != null) {
            // Show bill dialog with print option
        }
        
    } catch (Exception ex) {
        progressDialog.Hide();
        // Show unexpected error dialog
    }
}
```

### Backend Process

```csharp
app.MapPost("/tables/{label}/stop", async (string label) =>
{
    // 1. Find active session
    const string findSql = @"SELECT session_id, server_id, server_name, start_time, billing_id FROM public.table_sessions
                             WHERE table_label = @label AND status = 'active' ORDER BY start_time DESC LIMIT 1";
    
    // 2. Validate session exists
    if (!await rdr.ReadAsync()) return Results.NotFound("No active session for this table");
    
    // 3. Calculate session duration
    var end = DateTime.UtcNow;
    var minutes = (int)Math.Max(0, (end - startTime).TotalMinutes);
    
    // 4. Close session
    const string closeSql = "UPDATE public.table_sessions SET end_time = @end, status = 'closed' WHERE session_id = @sid";
    
    // 5. Read session items
    const string itemsSql = @"SELECT items FROM public.table_sessions WHERE session_id = @sid";
    
    // 6. Compute costs
    decimal ratePerMinute = await GetRatePerMinuteAsync(conn, defaultRatePerMinute);
    decimal itemsCost = items.Sum(i => i.price * i.quantity);
    decimal timeCost = ratePerMinute * minutes;
    decimal total = timeCost + itemsCost;
    
    // 7. Create bill
    var billId = Guid.NewGuid();
    const string billSql = @"INSERT INTO public.bills(bill_id, billing_id, session_id, table_label, server_id, server_name, start_time, end_time, total_time_minutes, items, time_cost, items_cost, total_amount, status, is_settled, payment_state)
                            VALUES(@bid, @billingId, @sid, @label, @srvId, @srvName, @start, @end, @mins, @items, @timeCost, @itemsCost, @total, 'awaiting_payment', false, 'not-paid')";
    
    // 8. Mark table available
    const string freeSql = @"UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL, updated_at = now() WHERE label = @l";
    
    // 9. Return bill result
    return Results.Ok(new BillResult { ... });
});
```

## Bill Generation

### Cost Calculation

1. **Time Cost:**
   - Duration = `end_time - start_time`
   - Minutes = `Math.Ceiling(duration.TotalMinutes)`
   - Time Cost = `ratePerMinute * minutes`

2. **Items Cost:**
   - Sum of all items in session
   - Items stored as JSONB in `table_sessions.items`

3. **Total:**
   - Total = `timeCost + itemsCost`

### Bill Status

- **awaiting_payment:** Bill generated, waiting for payment
- **closed:** Bill paid and settled

## Error Handling

### Common Error Scenarios

1. **"No active session" Error**
   - **Cause:** Table shows as occupied but no active session in database
   - **Recovery:** Force free option available
   - **Prevention:** Proper session state synchronization

2. **Database Connection Issues**
   - **Cause:** Network connectivity, database server down
   - **Recovery:** Retry mechanism, offline mode fallback
   - **Prevention:** Connection pooling, health checks

3. **API Service Unavailable**
   - **Cause:** Backend service down, network issues
   - **Recovery:** Service restart, network troubleshooting
   - **Prevention:** Service monitoring, load balancing

### Recovery Mechanisms

1. **Force Free Table**
   - Closes any lingering active sessions
   - Marks table as available
   - Clears all session-related data

2. **Session Recovery**
   - Rehydrates session data from active sessions API
   - Restores missing start times from local cache
   - Synchronizes local and remote state

3. **Order Cleanup**
   - Closes open orders before stopping session
   - Handles order closure failures gracefully
   - Maintains data integrity

## Diagnostic System

### Comprehensive Diagnostics

```csharp
private async Task<string> DiagnoseTableIssuesAsync(string tableLabel)
{
    var diagnostics = new System.Text.StringBuilder();
    diagnostics.AppendLine($"=== Table Diagnostics for '{tableLabel}' ===");
    diagnostics.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    
    // 1. Check local table state
    var localTable = BilliardTables.Concat(BarTables).FirstOrDefault(t => t.Label == tableLabel);
    if (localTable != null) {
        diagnostics.AppendLine("Local Table State:");
        diagnostics.AppendLine($"  Label: {localTable.Label}");
        diagnostics.AppendLine($"  Occupied: {localTable.Occupied}");
        diagnostics.AppendLine($"  StartTime: {localTable.StartTime}");
        diagnostics.AppendLine($"  Server: {localTable.Server}");
    }
    
    // 2. Check API connectivity
    diagnostics.AppendLine("API Connectivity:");
    diagnostics.AppendLine($"  TablesApi BaseUrl: {_repo.SourceLabel}");
    diagnostics.AppendLine($"  OrdersApi Available: {App.OrdersApi != null}");
    
    // 3. Check active sessions
    try {
        var (sessionId, billingId) = await _repo.GetActiveSessionForTableAsync(tableLabel);
        diagnostics.AppendLine("Active Session Check:");
        diagnostics.AppendLine($"  SessionId: {sessionId}");
        diagnostics.AppendLine($"  BillingId: {billingId}");
    } catch (Exception ex) {
        diagnostics.AppendLine($"Active Session Check: ERROR - {ex.Message}");
    }
    
    return diagnostics.ToString();
}
```

## Related Documentation

- [TablesApi Reference](../backend/tables-api.md)
- [Payment Flow](./payments.md)
- [Order Processing](./orders.md)
