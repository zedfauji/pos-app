# ReceiptBuilder Usage Guide

## Quick Start

### Basic Usage
```csharp
using var builder = new ReceiptBuilder(logger);
builder.Initialize(ReceiptConfiguration.Get80mmConfiguration());

// Build receipt content
builder.DrawHeader("My Business", "123 Main St", "555-0123");
builder.DrawReceiptInfo("Final Receipt", "BILL-001", DateTime.Now, "Table 5");
builder.DrawOrderItems(items);
builder.DrawTotals(subtotal, discount, tax, total);
builder.DrawFooter("Thank you for your business!");

// Save as PDF
await builder.SaveAsPdfAsync("receipt.pdf");

// Or print directly
await builder.PrintAsync("Thermal Printer");
```

## Step-by-Step Guide

### 1. Initialize ReceiptBuilder

```csharp
// Create logger (optional)
var logger = serviceProvider.GetService<ILogger<ReceiptBuilder>>();

// Create builder instance
using var builder = new ReceiptBuilder(logger);

// Initialize with configuration
builder.Initialize(ReceiptConfiguration.Get80mmConfiguration());
```

### 2. Configure Receipt Layout

```csharp
// Use preset configurations
var config58mm = ReceiptConfiguration.Get58mmConfiguration();
var config80mm = ReceiptConfiguration.Get80mmConfiguration();

// Or create custom configuration
var customConfig = new ReceiptConfiguration
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

builder.Initialize(customConfig);
```

### 3. Draw Receipt Content

#### Header Section
```csharp
// Business information (centered)
builder.DrawHeader(
    businessName: "Billiard Palace",
    address: "123 Pool Street, City, State 12345",
    phone: "555-0123"
);
```

#### Receipt Information
```csharp
// Receipt metadata
builder.DrawReceiptInfo(
    receiptType: "Final Receipt",
    billId: "BILL-2024-001",
    date: DateTime.Now,
    tableNumber: "Table 5"
);
```

#### Order Items
```csharp
// Prepare items
var items = new List<ReceiptItem>
{
    new ReceiptItem { Name = "Pool Table (1 hour)", Quantity = 2, UnitPrice = 15.00m, LineTotal = 30.00m },
    new ReceiptItem { Name = "Soft Drink", Quantity = 3, UnitPrice = 2.50m, LineTotal = 7.50m },
    new ReceiptItem { Name = "Snacks", Quantity = 1, UnitPrice = 8.00m, LineTotal = 8.00m }
};

// Draw items
builder.DrawOrderItems(items);
```

#### Totals Section
```csharp
// Calculate totals
decimal subtotal = 45.50m;
decimal discountAmount = 5.00m;
decimal taxAmount = 3.64m; // 8% tax
decimal totalAmount = 44.14m;

// Draw totals
builder.DrawTotals(subtotal, discountAmount, taxAmount, totalAmount);
```

#### Footer
```csharp
// Footer message
builder.DrawFooter("Thank you for playing at Billiard Palace!");
```

### 4. Output Options

#### Save as PDF
```csharp
// Save to file
await builder.SaveAsPdfAsync("receipts/receipt_001.pdf");

// Save to user's Documents folder
var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var filePath = Path.Combine(documentsPath, "Receipts", $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
await builder.SaveAsPdfAsync(filePath);
```

#### Print Directly
```csharp
// Print to specific printer
await builder.PrintAsync("Thermal Printer 80mm");

// Print to default printer
await builder.PrintAsync("Microsoft Print to PDF");
```

#### Get PDF Bytes
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

## Error Handling Best Practices

### Try-Catch Blocks
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

### Validation
```csharp
// Validate inputs before using ReceiptBuilder
if (string.IsNullOrEmpty(billId))
    throw new ArgumentException("Bill ID cannot be null or empty");

if (items == null || !items.Any())
    throw new ArgumentException("Items list cannot be null or empty");

if (string.IsNullOrEmpty(filePath))
    throw new ArgumentException("File path cannot be null or empty");
```

## Performance Tips

### Memory Management
```csharp
// Always use 'using' statement for proper disposal
using var builder = new ReceiptBuilder(_logger);

// Don't keep builder instances in memory longer than necessary
// PDF generation is CPU-bound, not memory-bound
```

### Async Operations
```csharp
// Use async/await for I/O operations
await builder.SaveAsPdfAsync(filePath);
await builder.PrintAsync(printerName);

// Use cancellation tokens for long operations
await builder.SaveAsPdfAsync(filePath, cancellationToken);
```

### Batch Operations
```csharp
// Generate multiple receipts efficiently
var tasks = receiptDataList.Select(async data =>
{
    using var builder = new ReceiptBuilder(_logger);
    builder.Initialize(configuration);
    
    // Build receipt...
    
    await builder.SaveAsPdfAsync(GetFilePath(data.BillId));
});

await Task.WhenAll(tasks);
```

## Common Patterns

### Receipt Template
```csharp
public class ReceiptTemplate
{
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    public ReceiptConfiguration Configuration { get; set; } = ReceiptConfiguration.Get80mmConfiguration();
}

public async Task GenerateReceiptAsync(ReceiptTemplate template, ReceiptData data)
{
    using var builder = new ReceiptBuilder(_logger);
    builder.Initialize(template.Configuration);
    
    builder.DrawHeader(template.BusinessName, template.Address, template.Phone);
    builder.DrawReceiptInfo(data.ReceiptType, data.BillId, data.Date, data.TableNumber);
    builder.DrawOrderItems(data.Items);
    builder.DrawTotals(data.Subtotal, data.Discount, data.Tax, data.Total);
    builder.DrawFooter(template.FooterMessage);
    
    await builder.SaveAsPdfAsync(data.FilePath);
}
```

### Configuration Factory
```csharp
public static class ReceiptConfigurationFactory
{
    public static ReceiptConfiguration GetConfiguration(string printerType)
    {
        return printerType.ToLower() switch
        {
            "58mm" => ReceiptConfiguration.Get58mmConfiguration(),
            "80mm" => ReceiptConfiguration.Get80mmConfiguration(),
            _ => ReceiptConfiguration.Get80mmConfiguration()
        };
    }
    
    public static ReceiptConfiguration GetCustomConfiguration(double width, double height)
    {
        return new ReceiptConfiguration
        {
            Width = width,
            Height = height,
            FontFamily = "Arial",
            HeaderFontSize = Math.Max(8, width / 6),
            BodyFontSize = Math.Max(6, width / 8),
            TotalFontSize = Math.Max(7, width / 7),
            FooterFontSize = Math.Max(5, width / 10)
        };
    }
}
