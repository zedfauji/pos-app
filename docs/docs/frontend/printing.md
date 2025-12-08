# Frontend Printing System

Complete documentation for the frontend printing system using PDFSharp in WinUI 3.

## Overview

The frontend printing system provides receipt generation and printing capabilities using PDFSharp, with support for thermal printers and comprehensive error handling.

## Architecture

### Components

1. **ReceiptBuilder** - PDF generation engine
2. **ReceiptService** - High-level service wrapper
3. **ReceiptConfiguration** - Layout configuration
4. **Printing Helpers** - Utility classes

## ReceiptService Usage

### Basic Usage

```csharp
public class BillingPage
{
    private readonly ReceiptService _receiptService;
    
    public async Task PrintReceiptAsync(Bill bill)
    {
        try
        {
            var receiptData = new ReceiptData
            {
                BillId = bill.BillingId.ToString(),
                TableNumber = bill.TableLabel,
                Items = bill.Items.Select(i => new ReceiptItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price,
                    LineTotal = i.Total
                }).ToList(),
                Subtotal = bill.ItemsCost,
                Tax = bill.TaxAmount,
                Total = bill.TotalAmount,
                PaymentMethod = bill.PaymentMethod
            };
            
            await _receiptService.GenerateFinalReceiptAsync(receiptData);
        }
        catch (Exception ex)
        {
            // Handle error
            await ShowErrorDialogAsync("Failed to print receipt", ex.Message);
        }
    }
}
```

## Integration with Pages

### TablesPage Integration

```csharp
private async void PrintBill_Click(object sender, RoutedEventArgs e)
{
    if (SelectedTable?.Bill == null) return;
    
    try
    {
        var bill = SelectedTable.Bill;
        await _receiptService.GenerateFinalReceiptAsync(new ReceiptData
        {
            BillId = bill.BillingId.ToString(),
            TableNumber = bill.TableLabel,
            Items = bill.Items,
            Subtotal = bill.ItemsCost,
            Tax = 0,
            Total = bill.TotalAmount
        });
        
        await ShowSuccessDialogAsync("Receipt printed successfully");
    }
    catch (Exception ex)
    {
        await ShowErrorDialogAsync("Print failed", ex.Message);
    }
}
```

### BillingPage Integration

```csharp
private async void PrintPreBill_Click(object sender, RoutedEventArgs e)
{
    var currentBill = GetCurrentBill();
    if (currentBill == null) return;
    
    try
    {
        await _receiptService.GeneratePreBillAsync(
            currentBill.BillingId.ToString(),
            currentBill.TableLabel,
            currentBill.Items
        );
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to print pre-bill");
    }
}
```

## Configuration

### App Settings

```json
{
  "Receipt": {
    "PrinterName": "Thermal Printer 80mm",
    "PaperWidth": 80,
    "SaveToFile": true,
    "ReceiptsFolder": "Receipts"
  }
}
```

### Runtime Configuration

```csharp
var config = ReceiptConfiguration.Get80mmConfiguration();
config.FontFamily = AppSettings.Receipt.FontFamily ?? "Courier New";
config.PrinterName = AppSettings.Receipt.PrinterName;
```

## Error Handling

### COM Exception Prevention

The printing system avoids COM interop issues by:
- Using PDFSharp instead of WinUI PrintManager
- Generating PDFs in memory first
- Using system print dialogs instead of WinUI dialogs
- Proper thread marshaling

### Error Recovery

```csharp
try
{
    await _receiptService.GenerateFinalReceiptAsync(receiptData);
}
catch (PrinterNotFoundException ex)
{
    // Offer to save as PDF instead
    await _receiptService.SavePdfAsync(receiptData, filePath);
}
catch (FilePermissionException ex)
{
    // Request permissions or use alternative location
    await RequestFilePermissionsAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected printing error");
    await ShowErrorDialogAsync("Printing failed", ex.Message);
}
```

## Thread Safety

### UI Thread Operations

All printing operations are properly marshaled to the UI thread:

```csharp
await SafeDispatcher.RunOnUIThreadAsync(async () =>
{
    await _receiptService.GenerateFinalReceiptAsync(receiptData);
}, cancellationToken);
```

### Background Processing

PDF generation can run on background threads:

```csharp
var pdfBytes = await Task.Run(() =>
{
    using var builder = new ReceiptBuilder(_logger);
    builder.Initialize(config);
    // Build receipt...
    return builder.GetPdfBytes();
});

// Then print on UI thread
await SafeDispatcher.RunOnUIThreadAsync(() =>
{
    // Print PDF bytes
}, cancellationToken);
```

## Performance Optimization

### Async Operations

All I/O operations are async:

```csharp
public async Task GenerateReceiptAsync(ReceiptData data)
{
    await Task.Run(() => GeneratePdf(data));
    await SavePdfAsync(data.FilePath);
    await PrintPdfAsync(data.PrinterName);
}
```

### Memory Management

Proper disposal patterns:

```csharp
using var builder = new ReceiptBuilder(_logger);
builder.Initialize(config);
// Use builder...
// Automatically disposed
```

## Related Documentation

- [Receipt Printing Feature](../features/printing.md)
- [ReceiptBuilder Architecture](../../solution/docs/knowledge/printing/architecture-overview.md)
