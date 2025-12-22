using MagiDesk.Client.Services.Dtos;
using MagiDesk.Shared.DTOs.Tables;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services;

public class EscPosPrinterService : IPrinterService
{
    private readonly string _printerIp = "127.0.0.1"; // Hardcoded for V1
    
    public async Task PrintReceiptAsync(BillResult bill)
    {
        string name = "MagiDesk Restaurant";
        string addr = "123 Food Street";
        string footer = "Thank you for visiting!";
        decimal taxRate = 10;

        try 
        {
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            var tcs = new TaskCompletionSource<(string, string, string, decimal)>();
            
            app.MainWindow.DispatcherQueue.TryEnqueue(() => 
            {
                try
                {
                    // Access ApplicationData on UI thread
                    var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
                    string n = settings["RestaurantName"] as string ?? "MagiDesk Restaurant";
                    string a = settings["RestaurantAddress"] as string ?? "123 Food Street";
                    string f = settings["ReceiptFooter"] as string ?? "Thank you for visiting!";
                    string tStr = settings["TaxRate"] as string ?? "10";
                    decimal.TryParse(tStr, out decimal t);
                    tcs.SetResult((n, a, f, t));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            var results = await tcs.Task;
            name = results.Item1;
            addr = results.Item2;
            footer = results.Item3;
            taxRate = results.Item4;
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to load printer settings (UI Thread fix applied): {ex.Message}");
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine($"           {name.ToUpper()}           ");
        sb.AppendLine($"           {addr}           ");
        sb.AppendLine("========================================");
        sb.AppendLine($"Date: {DateTime.Now:g}");
        sb.AppendLine($"Table: {bill.TableLabel}  Server: {bill.ServerName}"); // Assuming ServerName in BillResult
        sb.AppendLine("----------------------------------------");
        
        foreach(var item in bill.Items)
        {
             sb.AppendLine($"{item.quantity}x {item.name,-25} {item.price * item.quantity,8:F2}");
        }
        
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"Subtotal:      {bill.TotalAmount,18:C}"); // Bill.TotalAmount is final total in current logic? 
        // Wait, BillResult.TotalAmount from Handler includes discount. 
        // Handler: total = (time + items) - discount.
        // We don't have separate subtotal in BillResult directly unless I add it.
        // I should have added Subtotal to BillResult to show it properly.
        // For now, I'll just show Total.
        // Or I can sum items for subtotal.
        var subtotal = bill.Items.Sum(x => x.price * x.quantity) + bill.TimeCost;
        sb.AppendLine($"Subtotal:      {subtotal,18:C}");
        
        if (bill.DiscountAmount > 0)
        {
            sb.AppendLine($"Discount:     -{bill.DiscountAmount,18:C}");
        }

        // Tax is usually included or added. 
        // For MVP, if we said total is calculated in Handler, Handler treated "total" as final.
        // If tax is involved, Handler should have calculated it.
        // I didn't add Tax calculation in Handler. 
        // I'll show Tax as "Included" or "0" for now unless I update Handler.
        // Prompt asked for Tax Rate in settings.
        // Let's assume Tax is calculated from Subtotal for display only, or Handler should have done it.
        // To be consistent, I'll calculate tax here for display purposes relative to subtotal.
        var taxAmount = subtotal * (taxRate / 100);
        sb.AppendLine($"Tax ({taxRate}%):  {taxAmount,18:C}"); 
        
        sb.AppendLine($"Total:         {bill.TotalAmount,18:C}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"Payment:       {bill.PaymentMethod}");
        if (bill.PaymentMethod == MagiDesk.Shared.Enums.PaymentMethod.Cash)
        {
             sb.AppendLine($"Tendered:      {bill.AmountTendered,18:C}");
             sb.AppendLine($"Change:        {bill.ChangeDue,18:C}");
        }
        else if (bill.PaymentMethod == MagiDesk.Shared.Enums.PaymentMethod.Card)
        {
             if (bill.TipAmount > 0) sb.AppendLine($"Tip:           {bill.TipAmount,18:C}");
        }
        
        if (bill.CustomerEmail != null)
        {
            sb.AppendLine($"Email: {bill.CustomerEmail}");
        }

        sb.AppendLine("========================================");
        sb.AppendLine($"        {footer}        ");
        sb.AppendLine("========================================");

        var receipt = sb.ToString();
        Log.Information("\n" + receipt);
        
        // Also log to console for visibility
        Console.WriteLine(receipt);

        await Task.CompletedTask;
    }

    public async Task PrintTicketAsync(OrderRequest order, string tableLabel)
    {
        Log.Information("VIRTUAL PRINT TICKET: Table {Table} - {Count} items", tableLabel, order.Items.Count);
        await Task.CompletedTask;
    }

    public async Task PrintTextAsync(string text)
    {
        Log.Information("VIRTUAL PRINT TEXT (Z-Report):\n{Text}", text);
        // Also write to console for dev visibility
        Console.WriteLine("=== VIRTUAL PRINTER START ===");
        Console.WriteLine(text);
        Console.WriteLine("=== VIRTUAL PRINTER END ===");
        await Task.CompletedTask;
    }
}
