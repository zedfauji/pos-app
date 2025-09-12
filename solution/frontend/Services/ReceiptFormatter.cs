using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Services
{
    public static class ReceiptFormatter
    {
        // Converts paper width in mm to pixels at 96 DPI (approx)
        private static double MmToPx(int mm)
        {
            if (mm <= 0)
                throw new ArgumentOutOfRangeException(nameof(mm), "Millimeters must be greater than 0");
            
            return mm * 96.0 / 25.4;
        }

        public static FrameworkElement BuildReceiptView(BillResult bill, int paperWidthMm, decimal taxPercent)
        {
            // Validate parameters
            if (bill == null)
                throw new ArgumentNullException(nameof(bill), "Bill cannot be null");
            
            if (paperWidthMm <= 0)
                throw new ArgumentOutOfRangeException(nameof(paperWidthMm), "Paper width must be greater than 0");
            
            if (taxPercent < 0 || taxPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(taxPercent), "Tax percent must be between 0 and 100");

            var width = MmToPx(paperWidthMm);
            var panel = new StackPanel
            {
                Width = width,
                Spacing = 4,
                Padding = new Thickness(6),
            };

            // Mono font for alignment
            var mono = new FontFamily("Consolas");

            string Line(string text) => text ?? string.Empty;
            string Center(string text) => text; // Could center using TextAlignment on TextBlock

            // Header
            panel.Children.Add(new TextBlock { Text = Center("Billiard Receipt"), FontWeight = Microsoft.UI.Text.FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Table: {bill.TableLabel}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Server: {bill.ServerName}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Start: {bill.StartTime:yyyy-MM-dd HH:mm}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"End  : {bill.EndTime:yyyy-MM-dd HH:mm}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Time : {bill.TotalTimeMinutes} min"), FontFamily = mono });

            panel.Children.Add(new TextBlock { Text = new string('-', 32), FontFamily = mono });

            // Items
            if (bill.Items != null && bill.Items.Count > 0)
            {
                foreach (var it in bill.Items)
                {
                    var name = it.name?.Trim() ?? it.itemId ?? "Item";
                    var qty = it.quantity;
                    var price = it.price;
                    panel.Children.Add(new TextBlock { Text = Line(name), FontFamily = mono });
                    panel.Children.Add(new TextBlock { Text = Line($"  x{qty} @ {price:0.##}"), FontFamily = mono });
                }
            }
            else
            {
                panel.Children.Add(new TextBlock { Text = "No items", FontFamily = mono });
            }

            panel.Children.Add(new TextBlock { Text = new string('-', 32), FontFamily = mono });

            // Totals (prefer API-provided costs when available)
            decimal timeCost = bill.TimeCost;
            decimal itemsCost = bill.ItemsCost;
            decimal subtotal = timeCost + itemsCost;
            decimal tax = Math.Round(subtotal * (taxPercent / 100m), 2);
            decimal total = bill.TotalAmount > 0m ? bill.TotalAmount : subtotal + tax;

            panel.Children.Add(new TextBlock { Text = Line($"Time Cost   : {timeCost:0.00}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Items Cost  : {itemsCost:0.00}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Subtotal    : {subtotal:0.00}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"Tax {taxPercent:0.#}%  : {tax:0.00}"), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Line($"TOTAL       : {total:0.00}"), FontWeight = Microsoft.UI.Text.FontWeights.Bold, FontFamily = mono });

            panel.Children.Add(new TextBlock { Text = new string('-', 32), FontFamily = mono });
            panel.Children.Add(new TextBlock { Text = Center("Thank you!"), HorizontalAlignment = HorizontalAlignment.Center, FontFamily = mono });

            return new Border { Child = panel };
        }
    }
}
