using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels
{
    /// <summary>
    /// ViewModel for the Receipt Format Designer page
    /// </summary>
    public sealed class ReceiptFormatDesignerViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<ReceiptFormatDesignerViewModel> _logger;
        private readonly IConfiguration _configuration;

        // Business Information
        private string _businessName = "MagiDesk POS";
        private string _businessAddress = "123 Main Street, City, State 12345";
        private string _businessPhone = "(555) 123-4567";
        private string _businessEmail = "info@magidesk.com";
        private string _businessWebsite = "www.magidesk.com";

        // Logo Settings
        private bool _showLogo = true;
        private string _logoPath = "";
        private int _logoSizeIndex = 1; // Medium
        private int _logoPositionIndex = 1; // Center

        // Receipt Layout
        private int _paperSizeIndex = 1; // 80mm Thermal
        private double _fontSize = 12;
        private double _lineSpacing = 1.2;
        private double _horizontalMargin = 5;
        private double _verticalMargin = 5;

        // Receipt Content
        private bool _showDateTime = true;
        private bool _showTableNumber = true;
        private bool _showServerName = true;
        private bool _showItemDetails = true;
        private bool _showSubtotal = true;
        private bool _showTax = true;
        private bool _showDiscount = true;
        private bool _showPaymentMethod = true;
        private string _footerMessage = "Thank you for your business!\nPlease come again!";
        
        // Print Settings
        private bool _showPreviewBeforePrinting = true;

        // Tax Settings
        private double _taxRate = 13.0;
        private string _taxLabel = "HST";
        private bool _taxInclusive = false;

        public ReceiptFormatDesignerViewModel(ILogger<ReceiptFormatDesignerViewModel>? logger = null, IConfiguration? configuration = null)
        {
            _logger = logger ?? NullLoggerFactory.Create<ReceiptFormatDesignerViewModel>();
            _configuration = configuration ?? new ConfigurationBuilder().Build();
            
            LoadSettingsAsync();
        }

        #region Business Information Properties

        public string BusinessName
        {
            get => _businessName;
            set => SetProperty(ref _businessName, value);
        }

        public string BusinessAddress
        {
            get => _businessAddress;
            set => SetProperty(ref _businessAddress, value);
        }

        public string BusinessPhone
        {
            get => _businessPhone;
            set => SetProperty(ref _businessPhone, value);
        }

        public string BusinessEmail
        {
            get => _businessEmail;
            set => SetProperty(ref _businessEmail, value);
        }

        public string BusinessWebsite
        {
            get => _businessWebsite;
            set => SetProperty(ref _businessWebsite, value);
        }

        #endregion

        #region Logo Settings Properties

        public bool ShowLogo
        {
            get => _showLogo;
            set
            {
                SetProperty(ref _showLogo, value);
                OnPropertyChanged(nameof(HasLogo));
            }
        }

        public string LogoPath
        {
            get => _logoPath;
            set
            {
                SetProperty(ref _logoPath, value);
                OnPropertyChanged(nameof(HasLogo));
            }
        }

        public bool HasLogo => _showLogo && !string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath);

        public int LogoSizeIndex
        {
            get => _logoSizeIndex;
            set => SetProperty(ref _logoSizeIndex, value);
        }

        public int LogoPositionIndex
        {
            get => _logoPositionIndex;
            set => SetProperty(ref _logoPositionIndex, value);
        }

        public string LogoSize => LogoSizeIndex switch
        {
            0 => "32x32",
            1 => "64x64",
            2 => "96x96",
            _ => "64x64"
        };

        public string LogoPosition => LogoPositionIndex switch
        {
            0 => "Left",
            1 => "Center",
            2 => "Right",
            _ => "Center"
        };

        #endregion

        #region Receipt Layout Properties

        public int PaperSizeIndex
        {
            get => _paperSizeIndex;
            set => SetProperty(ref _paperSizeIndex, value);
        }

        public string PaperSize => PaperSizeIndex switch
        {
            0 => "58mm",
            1 => "80mm",
            2 => "A4",
            _ => "80mm"
        };

        public int PaperWidthMm => PaperSizeIndex switch
        {
            0 => 58,
            1 => 80,
            2 => 210, // A4 width
            _ => 80
        };

        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        public double LineSpacing
        {
            get => _lineSpacing;
            set => SetProperty(ref _lineSpacing, value);
        }

        public double HorizontalMargin
        {
            get => _horizontalMargin;
            set => SetProperty(ref _horizontalMargin, value);
        }

        public double VerticalMargin
        {
            get => _verticalMargin;
            set => SetProperty(ref _verticalMargin, value);
        }

        #endregion

        #region Receipt Content Properties

        public bool ShowDateTime
        {
            get => _showDateTime;
            set => SetProperty(ref _showDateTime, value);
        }

        public bool ShowTableNumber
        {
            get => _showTableNumber;
            set => SetProperty(ref _showTableNumber, value);
        }

        public bool ShowServerName
        {
            get => _showServerName;
            set => SetProperty(ref _showServerName, value);
        }

        public bool ShowItemDetails
        {
            get => _showItemDetails;
            set => SetProperty(ref _showItemDetails, value);
        }

        public bool ShowSubtotal
        {
            get => _showSubtotal;
            set => SetProperty(ref _showSubtotal, value);
        }

        public bool ShowTax
        {
            get => _showTax;
            set => SetProperty(ref _showTax, value);
        }

        public bool ShowDiscount
        {
            get => _showDiscount;
            set => SetProperty(ref _showDiscount, value);
        }

        public bool ShowPaymentMethod
        {
            get => _showPaymentMethod;
            set => SetProperty(ref _showPaymentMethod, value);
        }

        public string FooterMessage
        {
            get => _footerMessage;
            set => SetProperty(ref _footerMessage, value);
        }

        public bool ShowPreviewBeforePrinting
        {
            get => _showPreviewBeforePrinting;
            set => SetProperty(ref _showPreviewBeforePrinting, value);
        }

        #endregion

        #region Tax Settings Properties

        public double TaxRate
        {
            get => _taxRate;
            set => SetProperty(ref _taxRate, value);
        }

        public string TaxLabel
        {
            get => _taxLabel;
            set => SetProperty(ref _taxLabel, value);
        }

        public bool TaxInclusive
        {
            get => _taxInclusive;
            set => SetProperty(ref _taxInclusive, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Load settings from configuration
        /// </summary>
        private async Task LoadSettingsAsync()
        {
            try
            {
                var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MagiDesk", "receipt-format.json");
                
                if (File.Exists(settingsPath))
                {
                    var json = await File.ReadAllTextAsync(settingsPath);
                    var settings = JsonSerializer.Deserialize<ReceiptFormatSettings>(json);
                    
                    if (settings != null)
                    {
                        ApplySettings(settings);
                        _logger.LogInformation("Receipt format settings loaded from {Path}", settingsPath);
                    }
                }
                else
                {
                    _logger.LogInformation("No existing receipt format settings found, using defaults");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load receipt format settings");
            }
        }

        /// <summary>
        /// Save settings to configuration and generate receipt template
        /// </summary>
        public async Task SaveSettingsAsync()
        {
            try
            {
                var settings = CreateSettingsObject();
                var settingsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MagiDesk");
                Directory.CreateDirectory(settingsDir);
                
                var settingsPath = Path.Combine(settingsDir, "receipt-format.json");
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                
                await File.WriteAllTextAsync(settingsPath, json);
                _logger.LogInformation("Receipt format settings saved to {Path}", settingsPath);

                try
                {
                    await SaveReceiptTemplateAsync(settingsDir, settings);
                    
                    // Trigger template capture from preview
                    RequestTemplateCapture();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save receipt format settings");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save receipt format settings");
                throw;
            }
        }

        /// <summary>
        /// Save receipt template - this will be called by the page to capture the actual preview
        /// </summary>
        private async Task SaveReceiptTemplateAsync(string settingsDir, object settings)
        {
            try
            {
                // Just create the directory - the actual template capture will be done by the page
                Directory.CreateDirectory(Path.Combine(settingsDir, "Templates"));
                _logger.LogInformation("Template directory created at {Path}", Path.Combine(settingsDir, "Templates"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create template directory");
                throw;
            }
        }

        /// <summary>
        /// Event to notify when template should be captured from preview
        /// </summary>
        public event EventHandler? CaptureTemplateRequested;

        /// <summary>
        /// Trigger template capture from the preview
        /// </summary>
        public void RequestTemplateCapture()
        {
            CaptureTemplateRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Generate HTML template that exactly matches the Receipt Designer Preview layout
        /// </summary>
        private string GenerateReceiptTemplateHtml()
        {
            var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { 
            font-family: 'Courier New', monospace; 
            font-size: {{FONT_SIZE}}px; 
            margin: {{VERTICAL_MARGIN}}mm {{HORIZONTAL_MARGIN}}mm; 
            line-height: {{LINE_SPACING}};
            text-align: center;
        }
        .business-name { font-weight: bold; font-size: 1.2em; margin-bottom: 2px; }
        .business-info { margin-bottom: 1px; }
        .separator { border-top: 1px solid #000; margin: 4px 0; }
        .receipt-type { font-weight: bold; margin: 4px 0; }
        .receipt-details { text-align: left; margin: 2px 0; }
        .items-table { width: 100%; border-collapse: collapse; margin: 4px 0; }
        .items-table th, .items-table td { text-align: left; padding: 1px 2px; }
        .items-table th { font-weight: bold; }
        .items-table .qty, .items-table .price, .items-table .total { text-align: right; }
        .totals { text-align: right; margin: 2px 0; }
        .total-line { display: flex; justify-content: space-between; }
        .final-total { font-weight: bold; }
        .footer { text-align: center; margin-top: 4px; }
        .logo { margin-bottom: 4px; }
    </style>
</head>
<body>
    {{#SHOW_LOGO}}
    <div class='logo'>
        <img src='{{LOGO_PATH}}' style='width: {{LOGO_SIZE}}px; height: {{LOGO_SIZE}}px;' />
    </div>
    {{/SHOW_LOGO}}
    
    <div class='business-name'>{{BUSINESS_NAME}}</div>
    
    {{#BUSINESS_ADDRESS}}
    <div class='business-info'>{{BUSINESS_ADDRESS}}</div>
    {{/BUSINESS_ADDRESS}}
    
    {{#BUSINESS_PHONE}}
    <div class='business-info'>Tel: {{BUSINESS_PHONE}}</div>
    {{/BUSINESS_PHONE}}
    
    {{#BUSINESS_EMAIL}}
    <div class='business-info'>Email: {{BUSINESS_EMAIL}}</div>
    {{/BUSINESS_EMAIL}}
    
    {{#BUSINESS_WEBSITE}}
    <div class='business-info'>Web: {{BUSINESS_WEBSITE}}</div>
    {{/BUSINESS_WEBSITE}}
    
    <div class='separator'></div>
    
    <div class='receipt-type'>{{RECEIPT_TYPE}}</div>
    
    {{#SHOW_DATETIME}}
    <div class='receipt-details'>Date: {{DATE_TIME}}</div>
    {{/SHOW_DATETIME}}
    
    {{#SHOW_TABLE_NUMBER}}
    <div class='receipt-details'>Table: {{TABLE_NUMBER}}</div>
    {{/SHOW_TABLE_NUMBER}}
    
    {{#SHOW_SERVER_NAME}}
    <div class='receipt-details'>Server: {{SERVER_NAME}}</div>
    {{/SHOW_SERVER_NAME}}
    
    <div class='receipt-details'>Bill #: {{BILL_ID}}</div>
    
    <div class='separator'></div>
    
    {{#SHOW_ITEM_DETAILS}}
    <table class='items-table'>
        <tr>
            <th>Item</th>
            <th class='qty'>Qty</th>
            <th class='price'>Price</th>
            <th class='total'>Total</th>
        </tr>
        {{#ITEMS}}
        <tr>
            <td>{{NAME}}</td>
            <td class='qty'>{{QUANTITY}}</td>
            <td class='price'>${{UNIT_PRICE}}</td>
            <td class='total'>${{LINE_TOTAL}}</td>
        </tr>
        {{/ITEMS}}
    </table>
    <div class='separator'></div>
    {{/SHOW_ITEM_DETAILS}}
    
    <div class='totals'>
        {{#SHOW_SUBTOTAL}}
        <div class='total-line'>
            <span>Subtotal:</span>
            <span>${{SUBTOTAL}}</span>
        </div>
        {{/SHOW_SUBTOTAL}}
        
        {{#SHOW_DISCOUNT}}
        <div class='total-line'>
            <span>Discount:</span>
            <span>-${{DISCOUNT_AMOUNT}}</span>
        </div>
        {{/SHOW_DISCOUNT}}
        
        {{#SHOW_TAX}}
        <div class='total-line'>
            <span>{{TAX_LABEL}} ({{TAX_RATE}}%):</span>
            <span>${{TAX_AMOUNT}}</span>
        </div>
        {{/SHOW_TAX}}
        
        <div class='separator'></div>
        <div class='total-line final-total'>
            <span>TOTAL:</span>
            <span>${{TOTAL_AMOUNT}}</span>
        </div>
    </div>
    
    {{#SHOW_PAYMENT_METHOD}}
    {{#IS_FINAL_RECEIPT}}
    <div class='separator'></div>
    <div class='receipt-details'>Payment: {{PAYMENT_METHOD}}</div>
    <div class='receipt-details'>Amount Paid: ${{AMOUNT_PAID}}</div>
    <div class='receipt-details'>Change: ${{CHANGE_AMOUNT}}</div>
    {{/IS_FINAL_RECEIPT}}
    {{/SHOW_PAYMENT_METHOD}}
    
    {{#FOOTER_MESSAGE}}
    <div class='separator'></div>
    <div class='footer'>{{FOOTER_MESSAGE}}</div>
    {{/FOOTER_MESSAGE}}
</body>
</html>";

            // Replace template variables with actual settings
            html = html.Replace("{{FONT_SIZE}}", FontSize.ToString());
            html = html.Replace("{{HORIZONTAL_MARGIN}}", HorizontalMargin.ToString());
            html = html.Replace("{{VERTICAL_MARGIN}}", VerticalMargin.ToString());
            html = html.Replace("{{LINE_SPACING}}", LineSpacing.ToString());
            html = html.Replace("{{BUSINESS_NAME}}", BusinessName ?? "MagiDesk POS");
            html = html.Replace("{{BUSINESS_ADDRESS}}", BusinessAddress ?? "");
            html = html.Replace("{{BUSINESS_PHONE}}", BusinessPhone ?? "");
            html = html.Replace("{{BUSINESS_EMAIL}}", BusinessEmail ?? "");
            html = html.Replace("{{BUSINESS_WEBSITE}}", BusinessWebsite ?? "");
            html = html.Replace("{{LOGO_PATH}}", LogoPath ?? "");
            html = html.Replace("{{LOGO_SIZE}}", GetLogoSizePixels().ToString());

            return html;
        }

        private int GetLogoSizePixels()
        {
            return LogoSizeIndex switch
            {
                0 => 32,  // Small
                1 => 64,  // Medium
                2 => 96,  // Large
                _ => 64
            };
        }

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            BusinessName = "MagiDesk POS";
            BusinessAddress = "123 Main Street, City, State 12345";
            BusinessPhone = "(555) 123-4567";
            BusinessEmail = "info@magidesk.com";
            BusinessWebsite = "www.magidesk.com";
            
            ShowLogo = true;
            LogoPath = "";
            LogoSizeIndex = 1;
            LogoPositionIndex = 1;
            
            PaperSizeIndex = 1;
            FontSize = 12;
            LineSpacing = 1.2;
            HorizontalMargin = 5;
            VerticalMargin = 5;
            
            ShowDateTime = true;
            ShowTableNumber = true;
            ShowServerName = true;
            ShowItemDetails = true;
            ShowSubtotal = true;
            ShowTax = true;
            ShowDiscount = true;
            ShowPaymentMethod = true;
            FooterMessage = "Thank you for your business!\nPlease come again!";
            
            TaxRate = 13.0;
            TaxLabel = "HST";
            TaxInclusive = false;
            
            _logger.LogInformation("Receipt format settings reset to defaults");
        }

        /// <summary>
        /// Apply settings from loaded configuration
        /// </summary>
        private void ApplySettings(ReceiptFormatSettings settings)
        {
            BusinessName = settings.BusinessName ?? _businessName;
            BusinessAddress = settings.BusinessAddress ?? _businessAddress;
            BusinessPhone = settings.BusinessPhone ?? _businessPhone;
            BusinessEmail = settings.BusinessEmail ?? _businessEmail;
            BusinessWebsite = settings.BusinessWebsite ?? _businessWebsite;
            
            ShowLogo = settings.ShowLogo;
            LogoPath = settings.LogoPath ?? "";
            LogoSizeIndex = settings.LogoSizeIndex;
            LogoPositionIndex = settings.LogoPositionIndex;
            
            PaperSizeIndex = settings.PaperSizeIndex;
            FontSize = settings.FontSize;
            LineSpacing = settings.LineSpacing;
            HorizontalMargin = settings.HorizontalMargin;
            VerticalMargin = settings.VerticalMargin;
            
            ShowDateTime = settings.ShowDateTime;
            ShowTableNumber = settings.ShowTableNumber;
            ShowServerName = settings.ShowServerName;
            ShowItemDetails = settings.ShowItemDetails;
            ShowSubtotal = settings.ShowSubtotal;
            ShowTax = settings.ShowTax;
            ShowDiscount = settings.ShowDiscount;
            ShowPaymentMethod = settings.ShowPaymentMethod;
            FooterMessage = settings.FooterMessage ?? _footerMessage;
            ShowPreviewBeforePrinting = settings.ShowPreviewBeforePrinting;
            
            TaxRate = settings.TaxRate;
            TaxLabel = settings.TaxLabel ?? _taxLabel;
            TaxInclusive = settings.TaxInclusive;
        }

        /// <summary>
        /// Create settings object for serialization
        /// </summary>
        private ReceiptFormatSettings CreateSettingsObject()
        {
            return new ReceiptFormatSettings
            {
                BusinessName = BusinessName,
                BusinessAddress = BusinessAddress,
                BusinessPhone = BusinessPhone,
                BusinessEmail = BusinessEmail,
                BusinessWebsite = BusinessWebsite,
                
                ShowLogo = ShowLogo,
                LogoPath = LogoPath,
                LogoSizeIndex = LogoSizeIndex,
                LogoPositionIndex = LogoPositionIndex,
                
                PaperSizeIndex = PaperSizeIndex,
                FontSize = FontSize,
                LineSpacing = LineSpacing,
                HorizontalMargin = HorizontalMargin,
                VerticalMargin = VerticalMargin,
                
                ShowDateTime = ShowDateTime,
                ShowTableNumber = ShowTableNumber,
                ShowServerName = ShowServerName,
                ShowItemDetails = ShowItemDetails,
                ShowSubtotal = ShowSubtotal,
                ShowTax = ShowTax,
                ShowDiscount = ShowDiscount,
                ShowPaymentMethod = ShowPaymentMethod,
                FooterMessage = FooterMessage,
                ShowPreviewBeforePrinting = ShowPreviewBeforePrinting,
                
                TaxRate = TaxRate,
                TaxLabel = TaxLabel,
                TaxInclusive = TaxInclusive
            };
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Settings class for JSON serialization
    /// </summary>
    public sealed class ReceiptFormatSettings
    {
        // Business Information
        public string? BusinessName { get; set; }
        public string? BusinessAddress { get; set; }
        public string? BusinessPhone { get; set; }
        public string? BusinessEmail { get; set; }
        public string? BusinessWebsite { get; set; }

        // Logo Settings
        public bool ShowLogo { get; set; }
        public string? LogoPath { get; set; }
        public int LogoSizeIndex { get; set; }
        public int LogoPositionIndex { get; set; }

        // Receipt Layout
        public int PaperSizeIndex { get; set; }
        public double FontSize { get; set; }
        public double LineSpacing { get; set; }
        public double HorizontalMargin { get; set; }
        public double VerticalMargin { get; set; }

        // Receipt Content
        public bool ShowDateTime { get; set; }
        public bool ShowTableNumber { get; set; }
        public bool ShowServerName { get; set; }
        public bool ShowItemDetails { get; set; }
        public bool ShowSubtotal { get; set; }
        public bool ShowTax { get; set; }
        public bool ShowDiscount { get; set; }
        public bool ShowPaymentMethod { get; set; }
        public string? FooterMessage { get; set; }
        public bool ShowPreviewBeforePrinting { get; set; }

        // Tax Settings
        public double TaxRate { get; set; }
        public string? TaxLabel { get; set; }
        public bool TaxInclusive { get; set; }
    }
}
