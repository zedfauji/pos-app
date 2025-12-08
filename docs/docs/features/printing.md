# Receipt Printing

Complete documentation for the PDFSharp-based receipt printing system in MagiDesk POS.

## Overview

The receipt printing system uses PDFSharp for PDF generation and printing, providing support for thermal printers (58mm and 80mm), flexible layouts, and comprehensive error handling.

## Architecture

### Core Components

1. **ReceiptBuilder** - Core PDF generation class
2. **ReceiptService** - High-level service orchestrating receipt operations
3. **ReceiptConfiguration** - Configuration for receipt appearance and layout

## ReceiptBuilder Class

**Location:** `Services/ReceiptBuilder.cs`

### Key Features

- Configurable page sizes (58mm/80mm)
- Customizable fonts, margins, and spacing
- Structured receipt layout (header, items, totals, footer)
- Thread-safe operations
- Comprehensive error handling

### Initialization

```csharp
using var builder = new ReceiptBuilder(logger);
builder.Initialize(ReceiptConfiguration.Get80mmConfiguration());
```

### Configuration Options

#### 58mm Thermal Printer
```csharp
var config = ReceiptConfiguration.Get58mmConfiguration();
// Width: 58mm, Height: 200mm
// Smaller fonts and margins for narrow format
```

#### 80mm Thermal Printer
```csharp
var config = ReceiptConfiguration.Get80mmConfiguration();
// Width: 80mm, Height: 200mm
// Standard fonts and margins for wide format
```

#### Custom Configuration
```csharp
var config = new ReceiptConfiguration
{
    Width = 80,
    Height = 300,
    FontFamily = "Courier New",
    HeaderFontSize = 14,
    BodyFontSize = 12,
    TotalFontSize = 13,
    FooterFontSize = 10,
    MarginLeft = 8,
    MarginRight = 8,
    MarginTop = 8,
    MarginBottom = 8,
    LineHeight = 14,
    LineSpacing = 3
};
```

## Receipt Building

### Header Section

```csharp
builder.DrawHeader(
    businessName: "Billiard Palace",
    address: "123 Pool Street, City, State 12345",
    phone: "555-0123"
);
```

### Receipt Information

```csharp
builder.DrawReceiptInfo(
    receiptType: "Final Receipt",
    billId: "BILL-2024-001",
    date: DateTime.Now,
    tableNumber: "Table 5"
);
```

### Order Items

```csharp
var items = new List<ReceiptItem>
{
    new ReceiptItem { Name = "Pool Table (1 hour)", Quantity = 2, UnitPrice = 15.00m, LineTotal = 30.00m },
    new ReceiptItem { Name = "Soft Drink", Quantity = 3, UnitPrice = 2.50m, LineTotal = 7.50m },
    new ReceiptItem { Name = "Snacks", Quantity = 1, UnitPrice = 8.00m, LineTotal = 8.00m }
};

builder.DrawOrderItems(items);
```

### Totals Section

```csharp
decimal subtotal = 45.50m;
decimal discountAmount = 5.00m;
decimal taxAmount = 3.64m; // 8% tax
decimal totalAmount = 44.14m;

builder.DrawTotals(subtotal, discountAmount, taxAmount, totalAmount);
```

### Footer

```csharp
builder.DrawFooter("Thank you for playing at Billiard Palace!");
```

## Output Options

### Save as PDF

```csharp
// Save to file
await builder.SaveAsPdfAsync("receipts/receipt_001.pdf");

// Save to user's Documents folder
var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var filePath = Path.Combine(documentsPath, "Receipts", $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
await builder.SaveAsPdfAsync(filePath);
```

### Print Directly

```csharp
// Print to specific printer
await builder.PrintAsync("Thermal Printer 80mm");

// Print to default printer
await builder.PrintAsync("Microsoft Print to PDF");
```

### Get PDF Bytes

```csharp
// Get PDF as byte array for further processing
byte[] pdfBytes = builder.GetPdfBytes();

// Use in other operations
await File.WriteAllBytesAsync("receipt.pdf", pdfBytes);
```

## Complete Example: Pre-Bill Receipt

```csharp
public async Task GeneratePreBillAsync(string billId, string tableNumber, List<OrderItem> items)
{
    try
    {
        using var builder = new ReceiptBuilder(_logger);
        builder.Initialize(ReceiptConfiguration.Get80mmConfiguration());
        
        // Header
        builder.DrawHeader("Billiard Palace", "123 Pool Street", "555-0123");
        
        // Receipt info
        builder.DrawReceiptInfo("Pre-Bill", billId, DateTime.Now, tableNumber);
        
        // Items
        var receiptItems = items.Select(item => new ReceiptItem
        {
            Name = item.Name,
            Quantity = item.Quantity,
            UnitPrice = item.Price,
            LineTotal = item.Price * item.Quantity
        }).ToList();
        
        builder.DrawOrderItems(receiptItems);
        
        // Totals
        var subtotal = items.Sum(i => i.Price * i.Quantity);
        var tax = subtotal * 0.08m; // 8% tax
        var total = subtotal + tax;
        
        builder.DrawTotals(subtotal, 0, tax, total);
        
        // Footer
        builder.DrawFooter("This is a pre-bill. Please pay at the counter.");
        
        // Save PDF
        var fileName = $"prebill_{billId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(GetReceiptsFolder(), fileName);
        await builder.SaveAsPdfAsync(filePath);
        
        _logger.LogInformation($"Pre-bill generated: {filePath}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to generate pre-bill for {billId}");
        throw;
    }
}
```

## Complete Example: Final Receipt

```csharp
public async Task GenerateFinalReceiptAsync(string billId, PaymentData paymentData)
{
    try
    {
        using var builder = new ReceiptBuilder(_logger);
        builder.Initialize(ReceiptConfiguration.Get80mmConfiguration());
        
        // Header
        builder.DrawHeader(
            paymentData.BusinessName ?? "Billiard Palace",
            paymentData.Address,
            paymentData.Phone
        );
        
        // Receipt info
        builder.DrawReceiptInfo(
            "Final Receipt",
            billId,
            paymentData.PaymentDate,
            paymentData.TableNumber
        );
        
        // Items
        builder.DrawOrderItems(paymentData.Items);
        
        // Totals with payment details
        builder.DrawTotals(
            paymentData.Subtotal,
            paymentData.DiscountAmount,
            paymentData.TaxAmount,
            paymentData.TotalAmount
        );
        
        // Payment method
        if (!string.IsNullOrEmpty(paymentData.PaymentMethod))
        {
            builder.DrawFooter($"Paid by {paymentData.PaymentMethod}. Thank you!");
        }
        else
        {
            builder.DrawFooter("Thank you for your business!");
        }
        
        // Save and print
        var fileName = $"receipt_{billId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(GetReceiptsFolder(), fileName);
        await builder.SaveAsPdfAsync(filePath);
        
        // Print if printer is configured
        if (!string.IsNullOrEmpty(paymentData.PrinterName))
        {
            await builder.PrintAsync(paymentData.PrinterName);
        }
        
        _logger.LogInformation($"Final receipt generated: {filePath}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to generate final receipt for {billId}");
        throw;
    }
}
```

## Thread Safety

### Critical Race Conditions Addressed

1. **Multiple Bills Printing in Parallel**
   - Each ReceiptBuilder instance is isolated
   - No shared state between concurrent operations
   - Async/await patterns prevent blocking

2. **Window/UI Race Conditions**
   - No UI thread dependencies
   - PDF generation is CPU-bound operation
   - Proper disposal patterns

3. **Initialization Guarantees**
   - ValidateConfiguration() ensures all parameters are valid
   - Fail-fast with descriptive error messages
   - No partial initialization states

## Error Handling

### Parameter Validation

- All input parameters validated before use
- Configuration validation ensures proper setup
- File path validation for save operations
- Printer name validation for print operations

### Exception Handling

```csharp
try
{
    using var builder = new ReceiptBuilder(_logger);
    builder.Initialize(configuration);
    
    // Build receipt...
    
    await builder.SaveAsPdfAsync(filePath);
}
catch (ArgumentException ex)
{
    _logger.LogError(ex, "Invalid configuration or parameters");
    throw;
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "ReceiptBuilder not properly initialized");
    throw;
}
catch (IOException ex)
{
    _logger.LogError(ex, "File system error during PDF save");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during receipt generation");
    throw;
}
```

## Performance Considerations

### Memory Management

- Using statements ensure proper disposal
- PDF documents are generated in memory
- Temporary files cleaned up automatically
- No memory leaks from graphics objects

### Async Operations

- All I/O operations are async
- Non-blocking PDF generation
- Proper cancellation token support
- Background processing for large receipts

## Dependencies

### Required Packages

- `PdfSharpCore` - PDF generation library
- `SixLabors.Fonts` - Font rendering support
- `SixLabors.ImageSharp` - Image processing
- `SharpZipLib` - Compression support

### System Requirements

- .NET 8.0 or later
- Windows 10/11 (WinUI 3)
- Sufficient memory for PDF generation
- Printer drivers for direct printing

## Migration from WinUI Printing APIs

### Replaced Components

- `PrintManager` → `ReceiptBuilder`
- `PrintDocument` → PDF generation
- `RenderTargetBitmap` → PDF rendering
- WinUI print dialogs → System print dialogs

### Benefits of Migration

- No COM interop issues
- Better thermal printer support
- PDF archiving capabilities
- Improved error handling
- Thread-safe operations
- No UI thread dependencies

## Troubleshooting

### Common Issues

1. **Font Not Found**: Ensure system fonts are available
2. **Printer Not Available**: Check printer drivers and connectivity
3. **File Permission Errors**: Verify write permissions for PDF output
4. **Memory Issues**: Monitor memory usage for large receipts

### Debug Logging

All operations include structured logging:
- Initialization steps
- Drawing operations
- File operations
- Print operations
- Error conditions

## Future Enhancements

### Planned Features

- Barcode/QR code support
- Logo/image embedding
- Multi-language support
- Custom receipt templates
- Print preview functionality
- Batch printing operations

## Related Documentation

- [ReceiptBuilder Usage Guide](../knowledge/printing/usage-guide.md)
- [Printing Architecture](../knowledge/printing/architecture-overview.md)
