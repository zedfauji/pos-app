using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Configuration;

namespace MagiDesk.Frontend.ViewModels
{
    public class OrderByTableItem : INotifyPropertyChanged
    {
        public long OrderId { get; set; }
        public string TableLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DeliveryStatus { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string ServerName { get; set; } = string.Empty;
        
        public string TotalFormatted => $"${Total:F2}";
        public string CreatedAtFormatted => CreatedAt.ToString("HH:mm");
        public string StatusColor
        {
            get
            {
                return Status.ToLower() switch
                {
                    "open" => "#0078D4",
                    "pending" => "#FF8C00",
                    "waiting" => "#FF8C00",
                    "preparing" => "#0078D4",
                    "delivered" => "#107C10",
                    "closed" => "#404040",
                    _ => "#404040"
                };
            }
        }
        
        public string DeliveryStatusColor
        {
            get
            {
                return DeliveryStatus.ToLower() switch
                {
                    "delivered" => "#107C10",
                    "pending" => "#FF8C00",
                    "preparing" => "#0078D4",
                    "waiting" => "#FF8C00",
                    _ => "#404040"
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        public void NotifyPropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
    }

    public class TableOrdersGroup : INotifyPropertyChanged
    {
        public string TableLabel { get; set; } = string.Empty;
        public ObservableCollection<OrderByTableItem> Orders { get; } = new();
        public int OrderCount => Orders.Count;
        public decimal TotalRevenue => Orders.Sum(o => o.Total);
        public bool HasActiveOrders => Orders.Any(o => o.Status.ToLower() != "closed");
        
        public string TotalRevenueFormatted => $"${TotalRevenue:F2}";
        public string OrderCountText => $"{OrderCount} order{(OrderCount == 1 ? "" : "s")}";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        public void NotifyPropertyChanged(string propertyName) => OnPropertyChanged(propertyName);
    }

    public sealed class OrdersManagementViewModel : INotifyPropertyChanged
    {
        private readonly OrderApiService? _orders;
        private readonly TableRepository? _tables;
        private readonly Dictionary<string, TableOrdersGroup> _tableGroups = new();

        public ObservableCollection<TableOrdersGroup> TableGroups { get; } = new();
        
        private bool _isLoading = false;
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); } }
        
        private bool _hasError = false;
        public bool HasError { get => _hasError; set { _hasError = value; OnPropertyChanged(); } }
        
        private string? _errorMessage;
        public string? ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }

        public ICommand RefreshCommand { get; }
        public ICommand LoadOrderCommand { get; }
        public ICommand MarkDeliveredCommand { get; }
        public ICommand MarkWaitingCommand { get; }
        public ICommand CloseOrderCommand { get; }

        public OrdersManagementViewModel()
        {
            _orders = App.OrdersApi;
            
            // Create TableRepository using parameterless constructor (reads from appsettings.json)
            _tables = new TableRepository();
            
            if (_orders == null)
            {
                HasError = true;
                ErrorMessage = "Orders API is not available. Please restart the application.";
            }
            
            if (_tables == null)
            {
                HasError = true;
                ErrorMessage = "Tables API is not available. Please restart the application.";
            }

            RefreshCommand = new Services.RelayCommand(async _ => await LoadOrdersAsync());
            LoadOrderCommand = new Services.RelayCommand(async order => await LoadOrderDetailsAsync((OrderByTableItem)order));
            MarkDeliveredCommand = new Services.RelayCommand(async order => await MarkOrderDeliveredAsync((OrderByTableItem)order));
            MarkWaitingCommand = new Services.RelayCommand(async order => await MarkOrderWaitingAsync((OrderByTableItem)order));
            CloseOrderCommand = new Services.RelayCommand(async order => await CloseOrderAsync((OrderByTableItem)order));
        }

        public async Task LoadOrdersAsync()
        {
            if (_orders == null || _tables == null) 
            {
                System.Diagnostics.Debug.WriteLine("OrdersManagementViewModel: _orders or _tables is null");
                HasError = true;
                ErrorMessage = "Orders or Tables API is not available.";
                return;
            }

            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;

                System.Diagnostics.Debug.WriteLine("OrdersManagementViewModel: Starting to load orders...");

                // Get all active sessions first
                var activeSessions = await _tables.GetActiveSessionsAsync();
                System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Found {activeSessions?.Count ?? 0} active sessions");
                
                if (activeSessions == null || activeSessions.Count == 0)
                {
                    // No active sessions, show empty state
                    TableGroups.Clear();
                    _tableGroups.Clear();
                    System.Diagnostics.Debug.WriteLine("OrdersManagementViewModel: No active sessions found");
                    return;
                }

                // Clear existing data
                TableGroups.Clear();
                _tableGroups.Clear();

                // Group sessions by table and fetch orders for each session
                var sessionsByTable = activeSessions.GroupBy(s => s.TableId);
                System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Grouped into {sessionsByTable.Count()} table groups");

                foreach (var tableGroup in sessionsByTable)
                {
                    var tableOrdersGroup = new TableOrdersGroup
                    {
                        TableLabel = tableGroup.Key
                    };

                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Processing table {tableGroup.Key} with {tableGroup.Count()} sessions");

                    foreach (var session in tableGroup)
                    {
                        try
                        {
                            // Try to get orders for this session
                            var orders = await _orders.GetOrdersBySessionAsync(session.SessionId);
                            System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Found {orders?.Count ?? 0} orders for session {session.SessionId}");
                            
                            if (orders != null)
                            {
                                foreach (var order in orders)
                                {
                                    var orderItem = new OrderByTableItem
                                    {
                                        OrderId = order.Id,
                                        TableLabel = session.TableId,
                                        Status = order.Status,
                                        DeliveryStatus = order.DeliveryStatus,
                                        Total = order.Total,
                                        ItemCount = order.Items?.Count ?? 0,
                                        CreatedAt = DateTimeOffset.Now, // You'd get this from actual order data
                                        ServerName = session.ServerName
                                    };

                                    tableOrdersGroup.Orders.Add(orderItem);
                                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Added order {order.Id} to table {session.TableId}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing session {session.SessionId}: {ex.Message}");
                        }
                    }

                    if (tableOrdersGroup.Orders.Count > 0)
                    {
                        TableGroups.Add(tableOrdersGroup);
                        _tableGroups[tableGroup.Key] = tableOrdersGroup;
                        System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Added table group {tableGroup.Key} with {tableOrdersGroup.Orders.Count} orders");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Total table groups: {TableGroups.Count}");

                // Notify property changes
                foreach (var group in TableGroups)
                {
                    group.NotifyPropertyChanged(nameof(group.OrderCount));
                    group.NotifyPropertyChanged(nameof(group.TotalRevenue));
                    group.NotifyPropertyChanged(nameof(group.HasActiveOrders));
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to load orders: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading orders: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadOrderDetailsAsync(OrderByTableItem order)
        {
            try
            {
                // Navigate to order details page
                // This would typically involve navigation to the detailed order view
                System.Diagnostics.Debug.WriteLine($"Loading order details for Order {order.OrderId}");
                
                // For now, we'll just show a message
                // In a real implementation, you'd navigate to the order details page
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading order details: {ex.Message}");
            }
        }

        public async Task MarkOrderDeliveredAsync(OrderByTableItem order)
        {
            if (_orders == null) return;

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Marking order {order.OrderId} as delivered");

                // First, get the order details to get the items
                var orderDetails = await _orders.GetOrderAsync(order.OrderId);
                if (orderDetails == null)
                {
                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Could not get order details for order {order.OrderId}");
                    return;
                }

                // Create ItemDeliveryDto for all items (marking them as fully delivered)
                var itemDeliveries = orderDetails.Items.Select(item => 
                    new OrderApiService.ItemDeliveryDto(item.Id, item.Quantity)).ToList();

                System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Marking {itemDeliveries.Count} items as delivered for order {order.OrderId}");

                // Call the API to mark items as delivered
                var updatedOrder = await _orders.MarkItemsDeliveredAsync(order.OrderId, itemDeliveries);
                if (updatedOrder != null)
                {
                    // Update the local order status
                    order.Status = "delivered";
                    order.DeliveryStatus = "delivered";
                    order.NotifyPropertyChanged(nameof(order.Status));
                    order.NotifyPropertyChanged(nameof(order.StatusColor));
                    order.NotifyPropertyChanged(nameof(order.DeliveryStatus));
                    order.NotifyPropertyChanged(nameof(order.DeliveryStatusColor));

                    // Refresh the table group
                    if (_tableGroups.TryGetValue(order.TableLabel, out var group))
                    {
                        group.NotifyPropertyChanged(nameof(group.HasActiveOrders));
                    }

                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Successfully marked order {order.OrderId} as delivered");
                    
                    // Refresh the entire orders list to get updated data from server
                    await LoadOrdersAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Failed to mark order {order.OrderId} as delivered - API returned null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking order as delivered: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task MarkOrderWaitingAsync(OrderByTableItem order)
        {
            if (_orders == null) return;

            try
            {
                IsLoading = true;
                System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Marking order {order.OrderId} as waiting");

                // Call the API to mark order as waiting
                var updatedOrder = await _orders.MarkOrderWaitingAsync(order.OrderId);
                if (updatedOrder != null)
                {
                    // Update the local order status
                    order.Status = "waiting";
                    order.NotifyPropertyChanged(nameof(order.Status));
                    order.NotifyPropertyChanged(nameof(order.StatusColor));

                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Successfully marked order {order.OrderId} as waiting");
                    
                    // Refresh the entire orders list to get updated data from server
                    await LoadOrdersAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OrdersManagementViewModel: Failed to mark order {order.OrderId} as waiting - API returned null");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking order as waiting: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CloseOrderAsync(OrderByTableItem order)
        {
            if (_orders == null) return;

            try
            {
                IsLoading = true;

                // This would call the actual API to close the order
                // For now, we'll just update the local status
                order.Status = "closed";
                order.NotifyPropertyChanged(nameof(order.Status));
                order.NotifyPropertyChanged(nameof(order.StatusColor));

                // Refresh the table group
                if (_tableGroups.TryGetValue(order.TableLabel, out var group))
                {
                    group.NotifyPropertyChanged(nameof(group.HasActiveOrders));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing order: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
