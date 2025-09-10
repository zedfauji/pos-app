# ReceiptBuilder Helper Methods & Attributes Reference

## ReceiptBuilder Class Methods

### Initialization Methods

#### `Initialize(ReceiptConfiguration? configuration = null)`
**Purpose**: Initialize the receipt builder with specified configuration
**Parameters**:
- `configuration`: Optional configuration object. If null, uses default settings
**Validation**: 
- Validates all configuration parameters
- Ensures page dimensions are positive
- Validates font sizes and margins
**Throws**: `ArgumentException` for invalid configuration, `InvalidOperationException` for initialization failures

### Drawing Methods

#### `DrawHeader(string businessName, string? address = null, string? phone = null)`
**Purpose**: Draw business header information at the top of the receipt
**Parameters**:
- `businessName`: Business name (required, centered, bold)
- `address`: Business address (optional, centered)
- `phone`: Business phone (optional, centered)
**Layout**: 
- Business name uses header font size
- Address and phone use body font size
- All text is centered
- Adds separator line after header

#### `DrawReceiptInfo(string receiptType, string billId, DateTime date, string? tableNumber = null)`
**Purpose**: Draw receipt metadata information
**Parameters**:
- `receiptType`: Type of receipt (e.g., "Pre-Bill", "Final Receipt")
- `billId`: Unique bill identifier
- `date`: Receipt date/time
- `tableNumber`: Table number (optional)
**Layout**:
- Receipt type is centered and bold
- Bill ID, date, and table number are left-aligned
- Adds separator line after info

#### `DrawOrderItems(IEnumerable<ReceiptItem> items)`
**Purpose**: Draw itemized list of order items
**Parameters**:
- `items`: Collection of receipt items
**Layout**:
- Items header ("Items:") in bold
- Each item shows: name, quantity, unit price, line total
- Unit price and line total are right-aligned
- Adds separator line after items

#### `DrawTotals(decimal subtotal, decimal discountAmount = 0, decimal taxAmount = 0, decimal totalAmount = 0)`
**Purpose**: Draw totals section with calculations
**Parameters**:
- `subtotal`: Subtotal amount
- `discountAmount`: Discount amount (optional)
- `taxAmount`: Tax amount (optional)
- `totalAmount`: Final total amount (optional)
**Layout**:
- All amounts are right-aligned
- Grand total uses larger font size
- Only draws non-zero amounts
- Adds separator line after totals

#### `DrawFooter(string? footerText = null)`
**Purpose**: Draw footer message at the bottom
**Parameters**:
- `footerText`: Footer message (optional)
**Layout**:
- Text is centered
- Uses footer font size
- Only draws if text is provided

### Output Methods

#### `SaveAsPdfAsync(string filePath)`
**Purpose**: Save the receipt as a PDF file
**Parameters**:
- `filePath`: Full path where to save the PDF
**Features**:
- Creates directory if it doesn't exist
- Saves PDF document to specified path
- Thread-safe operation
**Throws**: `ArgumentException` for invalid file path, `IOException` for file system errors

#### `GetPdfBytes()`
**Purpose**: Get the PDF document as a byte array
**Returns**: `byte[]` containing the PDF data
**Use Cases**:
- Memory-based operations
- Further processing
- Network transmission
**Thread Safety**: Safe to call from any thread

#### `PrintAsync(string printerName)`
**Purpose**: Print the receipt directly to a specified printer
**Parameters**:
- `printerName`: Name of the printer to use
**Process**:
1. Generates PDF bytes
2. Saves to temporary file
3. Uses system print command
4. Cleans up temporary file
**Throws**: `ArgumentException` for invalid printer name, `InvalidOperationException` for print failures

## ReceiptConfiguration Class Properties

### Page Dimensions
- **`Width`** (double): Page width in millimeters (default: 80)
- **`Height`** (double): Page height in millimeters (default: 200)

### Font Settings
- **`FontFamily`** (string): Font family name (default: "Arial")
- **`HeaderFontSize`** (double): Header text font size (default: 12)
- **`BodyFontSize`** (double): Body text font size (default: 10)
- **`TotalFontSize`** (double): Total amount font size (default: 11)
- **`FooterFontSize`** (double): Footer text font size (default: 9)

### Margins (in millimeters)
- **`MarginLeft`** (double): Left margin (default: 5)
- **`MarginRight`** (double): Right margin (default: 5)
- **`MarginTop`** (double): Top margin (default: 5)
- **`MarginBottom`** (double): Bottom margin (default: 5)

### Line Spacing
- **`LineHeight`** (double): Height of each text line (default: 12)
- **`LineSpacing`** (double): Spacing between lines (default: 2)

## Preset Configurations

### `Get58mmConfiguration()`
**Purpose**: Get configuration optimized for 58mm thermal printers
**Settings**:
- Width: 58mm
- Height: 200mm
- Smaller fonts and margins for narrow format
- Font sizes: Header=10, Body=8, Total=9, Footer=7
- Margins: 3mm all around
- Line height: 10, Line spacing: 1

### `Get80mmConfiguration()`
**Purpose**: Get configuration optimized for 80mm thermal printers
**Settings**:
- Width: 80mm
- Height: 200mm
- Standard fonts and margins for wide format
- Font sizes: Header=12, Body=10, Total=11, Footer=9
- Margins: 5mm all around
- Line height: 12, Line spacing: 2

## ReceiptItem Class Properties

### `Name` (string)
**Purpose**: Item name/description
**Required**: Yes
**Example**: "Pool Table (1 hour)", "Soft Drink", "Snacks"

### `Quantity` (int)
**Purpose**: Quantity of the item
**Required**: Yes
**Default**: 1
**Example**: 2, 3, 1

### `UnitPrice` (decimal)
**Purpose**: Price per unit
**Required**: Yes
**Default**: 0
**Example**: 15.00m, 2.50m, 8.00m

### `LineTotal` (decimal)
**Purpose**: Total price for this line (Quantity × UnitPrice)
**Required**: Yes
**Default**: 0
**Example**: 30.00m, 7.50m, 8.00m

## Error Handling & Validation

### Parameter Validation
All methods validate their parameters before processing:
- **Null checks**: Required parameters cannot be null
- **Empty string checks**: Required strings cannot be empty
- **Range validation**: Numeric values must be within valid ranges
- **Collection validation**: Collections cannot be null or empty

### Exception Types
- **`ArgumentException`**: Invalid parameters
- **`InvalidOperationException`**: Operation cannot be performed
- **`IOException`**: File system errors
- **`UnauthorizedAccessException`**: Permission errors

### Logging
All operations include structured logging:
- **Information**: Successful operations
- **Warning**: Non-critical issues
- **Error**: Failed operations with exceptions
- **Debug**: Detailed operation steps

## Thread Safety

### Safe Operations
- **PDF Generation**: CPU-bound, thread-safe
- **File Operations**: Async I/O operations
- **Memory Operations**: No shared state

### Thread Safety Patterns
```csharp
// Each operation is isolated
using var builder = new ReceiptBuilder(logger);
builder.Initialize(configuration);

// Build content (thread-safe)
builder.DrawHeader("Business Name");
builder.DrawOrderItems(items);

// Output (thread-safe)
await builder.SaveAsPdfAsync(filePath);
```

## Performance Considerations

### Memory Management
- **Disposal**: Always use `using` statements
- **PDF Size**: Typically 10-50KB per receipt
- **Memory Usage**: Minimal memory footprint

### Optimization Tips
- **Font Loading**: System fonts are cached
- **PDF Generation**: Single-pass generation
- **File I/O**: Async operations prevent blocking

## Example Usage Patterns

### Basic Receipt Generation
```csharp
using var builder = new ReceiptBuilder(logger);
builder.Initialize(ReceiptConfiguration.Get80mmConfiguration());

builder.DrawHeader("My Business", "123 Main St", "555-0123");
builder.DrawReceiptInfo("Final Receipt", "BILL-001", DateTime.Now, "Table 5");
builder.DrawOrderItems(items);
builder.DrawTotals(subtotal, discount, tax, total);
builder.DrawFooter("Thank you!");

await builder.SaveAsPdfAsync("receipt.pdf");
```

### Custom Configuration
```csharp
var config = new ReceiptConfiguration
{
    Width = 80,
    Height = 300,
    FontFamily = "Courier New",
    HeaderFontSize = 14,
    BodyFontSize = 12,
    MarginLeft = 10,
    MarginRight = 10
};

using var builder = new ReceiptBuilder(logger);
builder.Initialize(config);
// ... build receipt
```

### Error Handling
```csharp
try
{
    using var builder = new ReceiptBuilder(logger);
    builder.Initialize(configuration);
    
    // Build receipt...
    
    await builder.SaveAsPdfAsync(filePath);
}
catch (ArgumentException ex)
{
    logger.LogError(ex, "Invalid configuration or parameters");
    throw;
}
catch (IOException ex)
{
    logger.LogError(ex, "File system error during PDF save");
    throw;
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error during receipt generation");
    throw;
}
```
