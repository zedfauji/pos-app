using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Text;

namespace MagiDesk.Frontend.Dialogs;

public sealed partial class TableStatusDialog : ContentDialog
{
    public TableStatusDialog()
    {
        this.InitializeComponent();
    }

    public async Task LoadTableStatusAsync(string tableLabel)
    {
        try
        {
            // Set table name
            TableNameText.Text = tableLabel;

            // Initialize with default values
            StatusText.Text = "Loading...";
            StatusColor.Color = Microsoft.UI.Colors.Orange;
            ServerText.Text = "Loading...";
            StartTimeText.Text = "Loading...";
            TotalTimeText.Text = "Loading...";
            TotalOrdersText.Text = "0";
            TotalAmountText.Text = "$0.00";

            // Get table repository and orders service
            var tableRepo = new Services.TableRepository();
            var ordersService = App.OrdersApi;
            

            if (tableRepo == null || ordersService == null)
            {
                StatusText.Text = "Services not available";
                StatusColor.Color = Microsoft.UI.Colors.Red;
                return;
            }

            // Get active session for the table
            var (sessionId, billingId) = await tableRepo.GetActiveSessionForTableAsync(tableLabel);
            
            // If no active session found, try to get recent sessions for this table
            if (!sessionId.HasValue)
            {
                try
                {
                    var recentSessions = await tableRepo.GetSessionsAsync(limit: 10, table: tableLabel);
                    
                    if (recentSessions.Count > 0)
                    {
                        var mostRecentSession = recentSessions.OrderByDescending(s => s.StartTime).First();
                        sessionId = mostRecentSession.SessionId;
                        billingId = mostRecentSession.BillingId;
                    }
                }
                catch (Exception ex)
                {
                }
            }
            
            if (!sessionId.HasValue)
            {
                StatusText.Text = "No session found";
                StatusColor.Color = Microsoft.UI.Colors.Gray;
                ServerText.Text = "N/A";
                StartTimeText.Text = "N/A";
                TotalTimeText.Text = "N/A";
                return;
            }

            // Get session details
            var sessions = await tableRepo.GetSessionsAsync(limit: 10, table: tableLabel);
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId.Value);
            
            if (session != null)
            {
                ServerText.Text = session.ServerName ?? "Unknown";
                StartTimeText.Text = session.StartTime.ToString("HH:mm:ss");
                
                var elapsed = DateTimeOffset.UtcNow - session.StartTime;
                TotalTimeText.Text = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                
                StatusText.Text = "Active Session";
                StatusColor.Color = Microsoft.UI.Colors.Green;
            }
            else
            {
                ServerText.Text = "Unknown";
                StartTimeText.Text = "Unknown";
                TotalTimeText.Text = "Unknown";
                StatusText.Text = "Session not found";
                StatusColor.Color = Microsoft.UI.Colors.Orange;
            }

            // Get orders for this session
            var orders = await ordersService.GetOrdersBySessionAsync(sessionId.Value, includeHistory: true);
            
            // If no orders found and we have a billing ID, try to get orders by billing ID
            if (orders.Count == 0 && billingId.HasValue)
            {
                try
                {
                    var billingOrders = await ordersService.GetOrdersByBillingIdAsync(billingId.Value);
                    if (billingOrders.Count > 0)
                    {
                        orders = billingOrders;
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                }
            }
            
            // Debug: Log order details
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                }
            }
            
            // Clear existing content
            OrdersPanel.Children.Clear();
            
            // Add a debug message to the UI
            if (orders.Count > 0)
            {
                var debugText = new TextBlock
                {
                    Text = $"DEBUG: Found {orders.Count} orders with {orders.Sum(o => o.Items.Count)} total items",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Blue),
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 5, 0, 5)
                };
                OrdersPanel.Children.Add(debugText);
            }

            if (orders.Count == 0)
            {
                var noOrdersText = new TextBlock
                {
                    Text = billingId.HasValue ? 
                        "No orders found for this session (Billing may have been generated)" : 
                        "No orders found for this session",
                    FontSize = 16,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 20, 0, 0)
                };
                OrdersPanel.Children.Add(noOrdersText);
                
                // Add session and billing information for debugging
                var sessionInfoText = new TextBlock
                {
                    Text = $"Session ID: {sessionId.Value}",
                    FontSize = 12,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Microsoft.UI.Xaml.Thickness(0, 10, 0, 0)
                };
                OrdersPanel.Children.Add(sessionInfoText);
                
                if (billingId.HasValue)
                {
                    var billingInfoText = new TextBlock
                    {
                        Text = $"Billing ID: {billingId.Value}",
                        FontSize = 12,
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange),
                        Margin = new Microsoft.UI.Xaml.Thickness(0, 5, 0, 0)
                    };
                    OrdersPanel.Children.Add(billingInfoText);
                }
            }
            else
            {
                // Group orders by status
                var groupedOrders = orders.GroupBy(o => o.Status).OrderBy(g => g.Key);

                foreach (var group in groupedOrders)
                {
                    var groupHeader = new Border
                    {
                        Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(4),
                        Padding = new Microsoft.UI.Xaml.Thickness(12, 8, 12, 8),
                        Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 8)
                    };

                    var groupHeaderText = new TextBlock
                    {
                        Text = $"{group.Key} Orders ({group.Count()})",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 14
                    };

                    groupHeader.Child = groupHeaderText;
                    OrdersPanel.Children.Add(groupHeader);

                    foreach (var order in group.OrderByDescending(o => o.Id))
                    {
                        var orderCard = CreateOrderCard(order);
                        OrdersPanel.Children.Add(orderCard);
                    }
                }
            }

            // Update summary
            TotalOrdersText.Text = orders.Count.ToString();
            var totalAmount = orders.Sum(o => o.Total);
            TotalAmountText.Text = totalAmount.ToString("C2");
        }
        catch (Exception ex)
        {
            // Set error state
            StatusText.Text = $"Error loading status: {ex.Message}";
            StatusColor.Color = Microsoft.UI.Colors.Red;
            ServerText.Text = "Error";
            StartTimeText.Text = "Error";
            TotalTimeText.Text = "Error";
            TotalOrdersText.Text = "0";
            TotalAmountText.Text = "$0.00";
            
            // Clear orders panel and show error message
            OrdersPanel.Children.Clear();
            var errorText = new TextBlock
            {
                Text = $"Error loading table status: {ex.Message}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 20, 0, 20)
            };
            OrdersPanel.Children.Add(errorText);
            
        }
    }

    private Border CreateOrderCard(Services.OrderApiService.OrderDto order)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.White),
            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
            BorderThickness = new Microsoft.UI.Xaml.Thickness(1),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(6),
            Padding = new Microsoft.UI.Xaml.Thickness(12),
            Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 8)
        };

        var stackPanel = new StackPanel();

        // Order header
        var headerPanel = new StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal
        };

        var orderIdText = new TextBlock
        {
            Text = $"Order #{order.Id}",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 14
        };

        var orderTotalText = new TextBlock
        {
            Text = order.Total.ToString("C2"),
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkGreen)
        };

        headerPanel.Children.Add(orderIdText);
        headerPanel.Children.Add(orderTotalText);
        stackPanel.Children.Add(headerPanel);

        // Order status
        var statusText = new TextBlock
        {
            Text = $"Status: {order.Status} | Delivery: {order.DeliveryStatus}",
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
            Margin = new Microsoft.UI.Xaml.Thickness(0, 4, 0, 8)
        };
        stackPanel.Children.Add(statusText);

        // Items
        if (order.Items.Count > 0)
        {
            var itemsHeader = new TextBlock
            {
                Text = "Items:",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 4)
            };
            stackPanel.Children.Add(itemsHeader);

            foreach (var item in order.Items)
            {
                var itemPanel = new StackPanel
                {
                    Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                    Margin = new Microsoft.UI.Xaml.Thickness(12, 2, 0, 2)
                };

                var itemText = new TextBlock
                {
                    Text = $"• {item.Quantity}x Item #{item.MenuItemId ?? item.ComboId}",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray)
                };

                var itemTotalText = new TextBlock
                {
                    Text = $"({item.LineTotal:C2})",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Microsoft.UI.Xaml.Thickness(8, 0, 0, 0)
                };

                var deliveredText = new TextBlock
                {
                    Text = $"Delivered: {item.DeliveredQuantity}/{item.Quantity}",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(item.DeliveredQuantity == item.Quantity ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Orange),
                    Margin = new Microsoft.UI.Xaml.Thickness(8, 0, 0, 0)
                };

                itemPanel.Children.Add(itemText);
                itemPanel.Children.Add(itemTotalText);
                itemPanel.Children.Add(deliveredText);
                stackPanel.Children.Add(itemPanel);
            }
        }
        else
        {
            var noItemsText = new TextBlock
            {
                Text = "No items in this order",
                FontSize = 11,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Microsoft.UI.Xaml.Thickness(12, 2, 0, 2)
            };
            stackPanel.Children.Add(noItemsText);
        }

        card.Child = stackPanel;
        return card;
    }
}
