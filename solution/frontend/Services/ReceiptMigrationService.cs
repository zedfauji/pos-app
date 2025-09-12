using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Services
{
    /// <summary>
    /// Service for migrating old BillResult format to new PDFSharp-based ReceiptService
    /// </summary>
    public sealed class ReceiptMigrationService
    {
        private readonly ReceiptService _receiptService;
        private readonly ILogger<ReceiptMigrationService> _logger;

        public ReceiptMigrationService(ReceiptService receiptService, ILogger<ReceiptMigrationService>? logger = null)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _logger = logger ?? new NullLogger<ReceiptMigrationService>();
        }

        /// <summary>
        /// Convert BillResult to ReceiptService.ReceiptData and generate PDF
        /// </summary>
        public async Task<string> GenerateReceiptFromBillAsync(BillResult bill, int paperWidthMm, decimal taxPercent, bool isProForma = false)
        {
            try
            {
                // Validate inputs
                if (bill == null)
                    throw new ArgumentException("Bill cannot be null", nameof(bill));
                
                if (bill.BillId == Guid.Empty)
                    throw new ArgumentException("Bill ID cannot be empty", nameof(bill.BillId));
                
                if (paperWidthMm <= 0)
                    throw new ArgumentException("Paper width must be greater than 0", nameof(paperWidthMm));
                
                if (taxPercent < 0 || taxPercent > 100)
                    throw new ArgumentException("Tax percent must be between 0 and 100", nameof(taxPercent));

                _logger.LogInformation("GenerateReceiptFromBillAsync: Starting for Bill ID: {BillId}", bill.BillId);

                // Convert BillResult to ReceiptService.ReceiptData
                var receiptData = ConvertBillResultToReceiptData(bill, paperWidthMm, taxPercent, isProForma);

                // Generate PDF using ReceiptService
                var fileName = $"receipt_{bill.BillId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = await _receiptService.SavePdfAsync(receiptData, fileName);

                _logger.LogInformation("GenerateReceiptFromBillAsync: Receipt generated successfully: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateReceiptFromBillAsync: Failed to generate receipt for Bill ID: {BillId}", bill.BillId);
                throw new InvalidOperationException($"Failed to generate receipt from bill: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Print BillResult directly to printer using PDFSharp
        /// </summary>
        public async Task<bool> PrintBillAsync(BillResult bill, int paperWidthMm, decimal taxPercent, string printerName, bool isProForma = false)
        {
            try
            {
                // Validate inputs
                if (bill == null)
                    throw new ArgumentException("Bill cannot be null", nameof(bill));
                
                if (bill.BillId == Guid.Empty)
                    throw new ArgumentException("Bill ID cannot be empty", nameof(bill.BillId));
                
                if (paperWidthMm <= 0)
                    throw new ArgumentException("Paper width must be greater than 0", nameof(paperWidthMm));
                
                if (taxPercent < 0 || taxPercent > 100)
                    throw new ArgumentException("Tax percent must be between 0 and 100", nameof(taxPercent));
                
                if (string.IsNullOrEmpty(printerName))
                    throw new ArgumentException("Printer name cannot be null or empty", nameof(printerName));

                _logger.LogInformation("PrintBillAsync: Starting for Bill ID: {BillId}, Printer: {PrinterName}", bill.BillId, printerName);

                // Convert BillResult to ReceiptService.ReceiptData
                var receiptData = ConvertBillResultToReceiptData(bill, paperWidthMm, taxPercent, isProForma);

                // Print using ReceiptService
                await _receiptService.PrintPdfAsync(receiptData, printerName);

                _logger.LogInformation("PrintBillAsync: Bill printed successfully to {PrinterName}", printerName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PrintBillAsync: Failed to print bill for Bill ID: {BillId}", bill.BillId);
                throw new InvalidOperationException($"Failed to print bill: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convert BillResult to ReceiptService.ReceiptData format
        /// </summary>
        private ReceiptService.ReceiptData ConvertBillResultToReceiptData(BillResult bill, int paperWidthMm, decimal taxPercent, bool isProForma)
        {
            try
            {
                // Validate inputs
                if (bill == null)
                    throw new ArgumentException("Bill cannot be null", nameof(bill));
                
                if (bill.BillId == Guid.Empty)
                    throw new ArgumentException("Bill ID cannot be empty", nameof(bill.BillId));
                
                if (paperWidthMm <= 0)
                    throw new ArgumentException("Paper width must be greater than 0", nameof(paperWidthMm));
                
                if (taxPercent < 0 || taxPercent > 100)
                    throw new ArgumentException("Tax percent must be between 0 and 100", nameof(taxPercent));

                // Calculate totals
                decimal timeCost = bill.TimeCost;
                decimal itemsCost = bill.ItemsCost;
                decimal subtotal = timeCost + itemsCost;
                decimal tax = Math.Round(subtotal * (taxPercent / 100m), 2);
                decimal total = bill.TotalAmount > 0m ? bill.TotalAmount : subtotal + tax;

                // Convert items
                var receiptItems = new List<ReceiptService.ReceiptItem>();

                // Add time cost as an item
                if (timeCost > 0)
                {
                    receiptItems.Add(new ReceiptService.ReceiptItem
                    {
                        Name = $"Table Time ({bill.TotalTimeMinutes} min)",
                        Quantity = 1,
                        Price = timeCost
                    });
                }

                // Add bill items
                if (bill.Items != null && bill.Items.Count > 0)
                {
                    foreach (var item in bill.Items)
                    {
                        receiptItems.Add(new ReceiptService.ReceiptItem
                        {
                            Name = item.name?.Trim() ?? item.itemId ?? "Item",
                            Quantity = item.quantity,
                            Price = item.price
                        });
                    }
                }

                // Create receipt data
                var receiptData = new ReceiptService.ReceiptData
                {
                    StoreName = "Billiard Palace",
                    BillId = bill.BillId.ToString(),
                    Date = bill.EndTime,
                    IsProForma = isProForma,
                    Items = receiptItems,
                    Tax = tax,
                    TotalAmount = total,
                    Footer = isProForma ? "This is a pre-bill. Please pay at the counter." : "Thank you for playing!",
                    TableNumber = bill.TableLabel,
                    Address = "123 Pool Street, City, State 12345",
                    Phone = "555-0123"
                };

                _logger.LogDebug("ConvertBillResultToReceiptData: Converted Bill ID: {BillId}, Items: {ItemCount}, Total: {Total}", 
                    bill.BillId, receiptItems.Count, total);

                return receiptData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertBillResultToReceiptData: Failed to convert Bill ID: {BillId}", bill.BillId);
                throw new InvalidOperationException($"Failed to convert bill result: {ex.Message}", ex);
            }
        }
    }
}
