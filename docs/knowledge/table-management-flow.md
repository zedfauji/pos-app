# Table Management Flow Documentation

## Overview
This document provides comprehensive documentation of the table management system in MagiDesk, including the complete flow for starting, stopping, and managing table sessions, along with troubleshooting and diagnostic capabilities.

## Table Management Architecture

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

## Complete Table Management Flow

### 1. Table Start Flow

#### Frontend Process (TablesPage.xaml.cs)
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

#### Backend Process (TablesApi)
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

### 2. Table Stop Flow (Enhanced)

#### Frontend Process (TablesPage.xaml.cs)
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
        
        if (!stopResult.ok) {
            // 6. Enhanced error handling with recovery options
            var errorMessage = stopResult.message ?? "Unknown error occurred";
            var canRecover = errorMessage.IndexOf("no active session", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           errorMessage.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           errorMessage.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0;
            
            if (canRecover) {
                // Offer force free option
                var dlg = new ContentDialog {
                    Title = "Session Recovery Required",
                    Content = $"The table '{item.Label}' shows as occupied but there is no active session in the database.\n\n" +
                             $"This can happen when:\n" +
                             $"• The session was already stopped\n" +
                             $"• Database connectivity issues occurred\n" +
                             $"• The session was manually cleared\n\n" +
                             $"Would you like to force free this table?",
                    PrimaryButtonText = "Force Free",
                    CloseButtonText = "Cancel"
                };
                
                var choice = await dlg.ShowAsync();
                if (choice == ContentDialogResult.Primary) {
                    var freed = await _repo.ForceFreeAsync(item.Label);
                    if (freed) {
                        // Show success message
                    } else {
                        // Show failure message
                    }
                    await RefreshFromStoreAsync();
                }
            } else {
                // Show detailed error with diagnostics option
                var errorDialog = new ContentDialog {
                    Title = "Unable to Stop Session",
                    Content = $"Failed to stop session for table '{item.Label}'.\n\n" +
                             $"Error: {errorMessage}\n\n" +
                             $"Possible causes:\n" +
                             $"• Database connection issues\n" +
                             $"• API service unavailable\n" +
                             $"• Network connectivity problems\n" +
                             $"• Server-side processing error",
                    PrimaryButtonText = "Run Diagnostics",
                    CloseButtonText = "OK"
                };
                
                var errorChoice = await errorDialog.ShowAsync();
                if (errorChoice == ContentDialogResult.Primary) {
                    // Run comprehensive diagnostics
                    var diagnostics = await DiagnoseTableIssuesAsync(item.Label);
                    // Show diagnostics dialog with copy to clipboard option
                }
            }
            return;
        }
        
        // 7. Refresh UI
        await RefreshFromStoreAsync();
        
        // 8. Handle bill display/printing
        if (stopResult.bill != null) {
            var bill = stopResult.bill;
            var itemsText = bill.Items != null && bill.Items.Count > 0
                ? ("\nItems:\n" + string.Join("\n", bill.Items.Select(i => $" - {i.name} x{i.quantity} @ {i.price:C2}")))
                : string.Empty;
            var text = $"Stopped {item.Label}. Total minutes: {bill.TotalTimeMinutes}{itemsText}";
            
            var dlg = new ContentDialog {
                Title = "Session Stopped",
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = "Print",
                SecondaryButtonText = "View",
                CloseButtonText = "Close"
            };
            
            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Secondary) {
                // Show receipt preview
            } else if (res == ContentDialogResult.Primary) {
                // Print receipt using PDFSharp
            }
        }
        
    } catch (Exception ex) {
        progressDialog.Hide();
        // Show unexpected error dialog
    }
}
```

#### Backend Process (TablesApi)
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

### 3. Force Free Flow (Recovery)

#### Frontend Process
```csharp
private async Task<bool> ForceFreeAsync(string label)
{
    return await _repo.ForceFreeAsync(label);
}
```

#### Backend Process
```csharp
app.MapPost("/tables/{label}/force-free", async (string label) =>
{
    // 1. Close any lingering 'active' sessions
    const string closeAll = "UPDATE public.table_sessions SET end_time = now(), status = 'closed' WHERE table_label = @label AND status = 'active'";
    
    // 2. Free the table
    const string freeSql2 = @"UPDATE public.table_status SET occupied = false, start_time = NULL, server = NULL, updated_at = now() WHERE label = @l";
    
    return Results.Ok(new { freed = true });
});
```

## Diagnostic System

### Comprehensive Diagnostics Method
```csharp
private async Task<string> DiagnoseTableIssuesAsync(string tableLabel)
{
    var diagnostics = new System.Text.StringBuilder();
    diagnostics.AppendLine($"=== Table Diagnostics for '{tableLabel}' ===");
    diagnostics.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    diagnostics.AppendLine();
    
    // 1. Check local table state
    var localTable = BilliardTables.Concat(BarTables).FirstOrDefault(t => t.Label == tableLabel);
    if (localTable != null) {
        diagnostics.AppendLine("Local Table State:");
        diagnostics.AppendLine($"  Label: {localTable.Label}");
        diagnostics.AppendLine($"  Occupied: {localTable.Occupied}");
        diagnostics.AppendLine($"  StartTime: {localTable.StartTime}");
        diagnostics.AppendLine($"  Server: {localTable.Server}");
        diagnostics.AppendLine($"  OrderId: {localTable.OrderId}");
        diagnostics.AppendLine($"  ThresholdMinutes: {localTable.ThresholdMinutes}");
    } else {
        diagnostics.AppendLine("Local Table State: NOT FOUND");
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
    
    // 4. Check open orders
    try {
        var ordersSvc = App.OrdersApi;
        if (ordersSvc != null) {
            var (sessionId, _) = await _repo.GetActiveSessionForTableAsync(tableLabel);
            if (sessionId.HasValue) {
                var orders = await ordersSvc.GetOrdersBySessionAsync(sessionId.Value, includeHistory: false);
                var openOrders = orders.Where(o => o.Status == "open").ToList();
                diagnostics.AppendLine("Open Orders Check:");
                diagnostics.AppendLine($"  Total Orders: {orders.Count}");
                diagnostics.AppendLine($"  Open Orders: {openOrders.Count}");
                foreach (var order in openOrders) {
                    diagnostics.AppendLine($"    Order {order.Id}: {order.Status} (Total: {order.Total:C2})");
                }
            } else {
                diagnostics.AppendLine("Open Orders Check: No active session found");
            }
        } else {
            diagnostics.AppendLine("Open Orders Check: OrdersApi not available");
        }
    } catch (Exception ex) {
        diagnostics.AppendLine($"Open Orders Check: ERROR - {ex.Message}");
    }
    
    // 5. Check database schema
    try {
        await _repo.EnsureSchemaAsync();
        diagnostics.AppendLine("Database Schema: OK");
    } catch (Exception ex) {
        diagnostics.AppendLine($"Database Schema: ERROR - {ex.Message}");
    }
    
    return diagnostics.ToString();
}
```

## Error Handling and Recovery

### Common Error Scenarios

1. **"No active session" Error**
   - **Cause**: Table shows as occupied but no active session in database
   - **Recovery**: Force free option available
   - **Prevention**: Proper session state synchronization

2. **Database Connection Issues**
   - **Cause**: Network connectivity, database server down
   - **Recovery**: Retry mechanism, offline mode fallback
   - **Prevention**: Connection pooling, health checks

3. **API Service Unavailable**
   - **Cause**: Backend service down, network issues
   - **Recovery**: Service restart, network troubleshooting
   - **Prevention**: Service monitoring, load balancing

4. **Order Management Issues**
   - **Cause**: OrdersApi service unavailable, order state inconsistencies
   - **Recovery**: Manual order closure, order state repair
   - **Prevention**: Proper order lifecycle management

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

## Best Practices

### Development
1. **Always validate table state** before operations
2. **Use comprehensive error handling** with specific recovery options
3. **Implement diagnostic capabilities** for troubleshooting
4. **Maintain session state consistency** between frontend and backend
5. **Handle network failures gracefully** with retry mechanisms

### Operations
1. **Monitor API health** regularly
2. **Check database connectivity** before critical operations
3. **Use diagnostics** when issues occur
4. **Maintain backup strategies** for session data
5. **Document error scenarios** and recovery procedures

### Testing
1. **Test error scenarios** thoroughly
2. **Validate recovery mechanisms** work correctly
3. **Test network failure scenarios**
4. **Verify data consistency** after recovery operations
5. **Test diagnostic capabilities** in various failure states

## Troubleshooting Guide

### When "Unable to Stop" Occurs

1. **Check Diagnostics**
   - Right-click table → "Run Diagnostics"
   - Review local state, API connectivity, active sessions
   - Copy diagnostics to clipboard for analysis

2. **Verify API Connectivity**
   - Check TablesApi service status
   - Verify network connectivity
   - Test database connection

3. **Check Session State**
   - Verify active session exists in database
   - Check for session state inconsistencies
   - Review session history

4. **Use Recovery Options**
   - Try "Force Free" if no active session found
   - Close open orders manually if needed
   - Restart services if necessary

5. **Escalate if Needed**
   - Contact system administrator
   - Provide diagnostic information
   - Document steps taken

This comprehensive documentation provides a complete understanding of the table management flow, error handling, and troubleshooting capabilities in the MagiDesk system.
