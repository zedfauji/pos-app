using Microsoft.UI.Xaml.Controls;
using MagiDesk.Shared.DTOs.Tables;
using System;

namespace MagiDesk.Frontend.Dialogs
{
    public sealed partial class BillDetailsDialog : ContentDialog
    {
        private readonly BillResult _bill;

        public BillDetailsDialog(BillResult bill)
        {
            _bill = bill;
            this.InitializeComponent();
            this.Title = $"Bill Details - {bill.BillId}";
            this.CloseButtonText = "Close";
        }

        private void InitializeComponent()
        {
            var stackPanel = new StackPanel();
            stackPanel.Spacing = 8;

            // Bill ID
            var billIdPanel = new StackPanel { Orientation = Orientation.Horizontal };
            billIdPanel.Children.Add(new TextBlock { Text = "Bill ID:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            billIdPanel.Children.Add(new TextBlock { Text = _bill.BillId.ToString() });
            stackPanel.Children.Add(billIdPanel);

            // Table
            var tablePanel = new StackPanel { Orientation = Orientation.Horizontal };
            tablePanel.Children.Add(new TextBlock { Text = "Table:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            tablePanel.Children.Add(new TextBlock { Text = _bill.TableLabel });
            stackPanel.Children.Add(tablePanel);

            // Server
            var serverPanel = new StackPanel { Orientation = Orientation.Horizontal };
            serverPanel.Children.Add(new TextBlock { Text = "Server:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            serverPanel.Children.Add(new TextBlock { Text = _bill.ServerName });
            stackPanel.Children.Add(serverPanel);

            // Amount
            var amountPanel = new StackPanel { Orientation = Orientation.Horizontal };
            amountPanel.Children.Add(new TextBlock { Text = "Amount:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            amountPanel.Children.Add(new TextBlock { Text = _bill.TotalAmount.ToString("C") });
            stackPanel.Children.Add(amountPanel);

            // Start Time
            var startTimePanel = new StackPanel { Orientation = Orientation.Horizontal };
            startTimePanel.Children.Add(new TextBlock { Text = "Start Time:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            startTimePanel.Children.Add(new TextBlock { Text = _bill.StartTime.ToString("MMM dd, yyyy HH:mm") });
            stackPanel.Children.Add(startTimePanel);

            // Duration
            var duration = DateTime.Now - _bill.StartTime;
            var durationPanel = new StackPanel { Orientation = Orientation.Horizontal };
            durationPanel.Children.Add(new TextBlock { Text = "Duration:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            durationPanel.Children.Add(new TextBlock { Text = $"{duration.TotalHours:F1} hours" });
            stackPanel.Children.Add(durationPanel);

            // Status
            var status = _bill.StartTime < DateTime.Now.AddDays(-7) ? "Overdue" : "Pending";
            var statusPanel = new StackPanel { Orientation = Orientation.Horizontal };
            statusPanel.Children.Add(new TextBlock { Text = "Status:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Width = 120 });
            statusPanel.Children.Add(new TextBlock { Text = status });
            stackPanel.Children.Add(statusPanel);

            this.Content = stackPanel;
        }
    }
}

