using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.Logging;
using MagiDesk.Extensions;
using MagiDesk.Frontend.ViewModels;
using MagiDesk.Frontend.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Collections.Generic;

namespace MagiDesk.Frontend.Views
{
    /// <summary>
    /// Receipt Format Designer Page for comprehensive receipt customization
    /// </summary>
    public sealed partial class ReceiptFormatDesignerPage : Page
    {
        private readonly ILogger<ReceiptFormatDesignerPage> _logger;
        private ReceiptFormatDesignerViewModel? _viewModel;

        public ReceiptFormatDesignerViewModel ViewModel => _viewModel ??= new ReceiptFormatDesignerViewModel();

        public ReceiptFormatDesignerPage()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel;
            
            this.Loaded += ReceiptFormatDesignerPage_Loaded;
            ViewModel.CaptureTemplateRequested += OnCaptureTemplateRequested;
            
            // Use a fallback logger if service is not available
            try
            {
                _logger = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ILogger<ReceiptFormatDesignerPage>>(App.Services) 
                    ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ReceiptFormatDesignerPage>.Instance;
            }
            catch
            {
                _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ReceiptFormatDesignerPage>.Instance;
            }
            
            this.Loaded += ReceiptFormatDesignerPage_Loaded;
            
            // Subscribe to property changes for live preview updates
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Handle template capture request from ViewModel
        /// </summary>
        private async void OnCaptureTemplateRequested(object? sender, EventArgs e)
        {
            await SavePreviewAsTemplate();
        }

        /// <summary>
        /// Capture the current preview as a template image for receipt generation
        /// </summary>
        private async Task SavePreviewAsTemplate()
        {
            try
            {
                // Ensure preview is generated and visible
                if (ProFormaPreviewContainer.Child == null)
                {
                    await RefreshPreviewAsync();
                }

                if (ProFormaPreviewContainer.Child is FrameworkElement previewContent)
                {
                    // Save the preview as PNG template
                    var templateDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MagiDesk", "Templates");
                    Directory.CreateDirectory(templateDir);
                    
                    var templatePath = Path.Combine(templateDir, "receipt-template.png");
                    var pngBytes = await previewContent.AsPng();
                    await File.WriteAllBytesAsync(templatePath, pngBytes);
                    
                    _logger.LogInformation("Receipt template image saved to {Path}", templatePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save preview as template");
                throw;
            }
        }

        private async void ReceiptFormatDesignerPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshPreviewAsync();
                UpdateStatusMessage("Receipt format designer loaded successfully");
                _logger.LogInformation("ReceiptFormatDesignerPage loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ReceiptFormatDesignerPage");
                UpdateStatusMessage($"Error loading page: {ex.Message}");
            }
        }

        private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update preview when relevant properties change
            if (IsPreviewProperty(e.PropertyName))
            {
                await RefreshPreviewAsync();
            }
        }

        private bool IsPreviewProperty(string? propertyName)
        {
            var previewProperties = new HashSet<string>
            {
                nameof(ViewModel.BusinessName),
                nameof(ViewModel.BusinessAddress),
                nameof(ViewModel.BusinessPhone),
                nameof(ViewModel.BusinessEmail),
                nameof(ViewModel.BusinessWebsite),
                nameof(ViewModel.ShowLogo),
                nameof(ViewModel.LogoPath),
                nameof(ViewModel.LogoSizeIndex),
                nameof(ViewModel.LogoPositionIndex),
                nameof(ViewModel.FontSize),
                nameof(ViewModel.LineSpacing),
                nameof(ViewModel.ShowDateTime),
                nameof(ViewModel.ShowTableNumber),
                nameof(ViewModel.ShowServerName),
                nameof(ViewModel.ShowItemDetails),
                nameof(ViewModel.ShowSubtotal),
                nameof(ViewModel.ShowTax),
                nameof(ViewModel.ShowDiscount),
                nameof(ViewModel.ShowPaymentMethod),
                nameof(ViewModel.FooterMessage),
                nameof(ViewModel.TaxRate),
                nameof(ViewModel.TaxLabel)
            };

            return previewProperties.Contains(propertyName ?? "");
        }

        #region Event Handlers

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.SaveSettingsAsync();
                UpdateStatusMessage("Settings saved successfully");
                LastSavedTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
                _logger.LogInformation("Receipt format settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                UpdateStatusMessage($"Error saving settings: {ex.Message}");
            }
        }

        private async void ResetToDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ContentDialog()
                {
                    Title = "Reset to Default",
                    Content = "Are you sure you want to reset all settings to default values? This action cannot be undone.",
                    PrimaryButtonText = "Reset",
                    SecondaryButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    ViewModel.ResetToDefaults();
                    await RefreshPreviewAsync();
                    UpdateStatusMessage("Settings reset to default values");
                    _logger.LogInformation("Receipt format settings reset to defaults");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting settings");
                UpdateStatusMessage($"Error resetting settings: {ex.Message}");
            }
        }

        private async void BrowseLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show progress
                ShowLogoProgress("Selecting logo file...");
                
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".bmp");
                picker.FileTypeFilter.Add(".gif");

                // Initialize the picker with the window handle
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    // Update progress
                    ShowLogoProgress("Uploading logo...");
                    
                    // Copy logo to app data folder
                    var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MagiDesk", "Logos");
                    Directory.CreateDirectory(appDataFolder);
                    
                    var logoFileName = $"logo_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(file.Name)}";
                    var logoPath = Path.Combine(appDataFolder, logoFileName);
                    
                    using var sourceStream = await file.OpenStreamForReadAsync();
                    using var destStream = File.Create(logoPath);
                    await sourceStream.CopyToAsync(destStream);
                    
                    // Update progress
                    ShowLogoProgress("Loading logo preview...");
                    
                    ViewModel.LogoPath = logoPath;
                    UpdateStatusMessage($"Logo uploaded: {file.Name}");
                    _logger.LogInformation("Logo uploaded: {LogoPath}", logoPath);
                    
                    // Refresh preview after logo upload
                    await RefreshPreviewAsync();
                    
                    // Show success confirmation
                    ShowLogoSuccess($"Logo '{file.Name}' loaded successfully! Size: {GetLogoSize()}x{GetLogoSize()}px");
                }
                else
                {
                    HideLogoFeedback();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error browsing for logo");
                ShowLogoError($"Failed to upload logo: {ex.Message}");
                UpdateStatusMessage($"Error uploading logo: {ex.Message}");
            }
        }

        private async void RemoveLogo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.LogoPath = "";
                UpdateStatusMessage("Logo removed");
                _logger.LogInformation("Logo removed");
                
                // Hide all logo feedback panels
                HideLogoFeedback();
                
                // Refresh preview after logo removal
                await RefreshPreviewAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing logo");
                UpdateStatusMessage($"Error removing logo: {ex.Message}");
            }
        }

        private void ShowLogoProgress(string message)
        {
            LogoStatusPanel.Visibility = Visibility.Visible;
            LogoConfirmationPanel.Visibility = Visibility.Collapsed;
            LogoErrorPanel.Visibility = Visibility.Collapsed;
            LogoStatusText.Text = message;
        }

        private void ShowLogoSuccess(string message)
        {
            LogoStatusPanel.Visibility = Visibility.Collapsed;
            LogoConfirmationPanel.Visibility = Visibility.Visible;
            LogoErrorPanel.Visibility = Visibility.Collapsed;
            LogoConfirmationText.Text = message;
            
            // Auto-hide success message after 5 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                LogoConfirmationPanel.Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }

        private void ShowLogoError(string message)
        {
            LogoStatusPanel.Visibility = Visibility.Collapsed;
            LogoConfirmationPanel.Visibility = Visibility.Collapsed;
            LogoErrorPanel.Visibility = Visibility.Visible;
            LogoErrorText.Text = message;
            
            // Auto-hide error message after 8 seconds
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                LogoErrorPanel.Visibility = Visibility.Collapsed;
            };
            timer.Start();
        }

        private void HideLogoFeedback()
        {
            LogoStatusPanel.Visibility = Visibility.Collapsed;
            LogoConfirmationPanel.Visibility = Visibility.Collapsed;
            LogoErrorPanel.Visibility = Visibility.Collapsed;
        }

        private async void PreviewType_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton toggleButton)
                {
                    // Ensure only one preview type is selected
                    if (toggleButton == ProFormaToggle && toggleButton.IsChecked == true)
                    {
                        FinalToggle.IsChecked = false;
                    }
                    else if (toggleButton == FinalToggle && toggleButton.IsChecked == true)
                    {
                        ProFormaToggle.IsChecked = false;
                    }
                    
                    // If neither is checked, default to Pro-forma
                    if (ProFormaToggle.IsChecked != true && FinalToggle.IsChecked != true)
                    {
                        ProFormaToggle.IsChecked = true;
                    }
                    
                    await RefreshPreviewAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing preview type");
                UpdateStatusMessage($"Error changing preview: {ex.Message}");
            }
        }

        private async void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshPreviewAsync();
                UpdateStatusMessage("Preview refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing preview");
                UpdateStatusMessage($"Error refreshing preview: {ex.Message}");
            }
        }

        private async void TestPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ContentDialog()
                {
                    Title = "Test Print",
                    Content = "Test printing functionality will be implemented with the receipt service integration.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await dialog.ShowAsync();
                UpdateStatusMessage("Test print feature coming soon");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error with test print");
                UpdateStatusMessage($"Error with test print: {ex.Message}");
            }
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ContentDialog()
                {
                    Title = "Export PDF",
                    Content = "PDF export functionality will be implemented with the receipt service integration.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                
                await dialog.ShowAsync();
                UpdateStatusMessage("PDF export feature coming soon");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error with PDF export");
                UpdateStatusMessage($"Error with PDF export: {ex.Message}");
            }
        }

        #endregion

        #region Preview Generation

        private async Task RefreshPreviewAsync()
        {
            try
            {
                var isProForma = ProFormaToggle.IsChecked == true;
                
                if (isProForma)
                {
                    await GenerateProFormaPreviewAsync();
                }
                else
                {
                    await GenerateFinalPreviewAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing preview");
                UpdateStatusMessage($"Error refreshing preview: {ex.Message}");
            }
        }

        private async Task GenerateProFormaPreviewAsync()
        {
            try
            {
                var previewContent = await GenerateReceiptPreviewContentAsync(true);
                ProFormaPreviewContainer.Child = previewContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pro-forma preview");
                ProFormaPreviewContainer.Child = CreateErrorPreview("Error generating pro-forma preview");
            }
        }

        private async Task GenerateFinalPreviewAsync()
        {
            try
            {
                var previewContent = await GenerateReceiptPreviewContentAsync(false);
                FinalPreviewContainer.Child = previewContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating final preview");
                FinalPreviewContainer.Child = CreateErrorPreview("Error generating final preview");
            }
        }

        private async Task<FrameworkElement> GenerateReceiptPreviewContentAsync(bool isProForma)
        {
            await Task.Yield(); // Ensure async operation
            
            var stackPanel = new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Logo
            _logger.LogInformation($"Logo check - ShowLogo: {ViewModel.ShowLogo}, LogoPath: '{ViewModel.LogoPath}', File exists: {File.Exists(ViewModel.LogoPath ?? "")}");
            
            if (ViewModel.ShowLogo && !string.IsNullOrEmpty(ViewModel.LogoPath))
            {
                try
                {
                    // Verify file exists
                    if (!File.Exists(ViewModel.LogoPath))
                    {
                        _logger.LogWarning($"Logo file does not exist at path: {ViewModel.LogoPath}");
                        var logoPlaceholder = CreatePreviewText("[LOGO FILE NOT FOUND]", true, true);
                        logoPlaceholder.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                        stackPanel.Children.Add(logoPlaceholder);
                        return stackPanel;
                    }

                    var logoSize = GetLogoSize();
                    _logger.LogInformation($"Creating logo image - Size: {logoSize}x{logoSize}, Alignment: {GetLogoAlignment()}");
                    
                    var logoImage = new Image
                    {
                        Width = logoSize,
                        Height = logoSize,
                        HorizontalAlignment = GetLogoAlignment(),
                        Margin = new Thickness(0, 2, 0, 2),
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    
                    var bitmap = new BitmapImage();
                    bitmap.UriSource = new Uri(ViewModel.LogoPath, UriKind.Absolute);
                    logoImage.Source = bitmap;
                    
                    stackPanel.Children.Add(logoImage);
                    _logger.LogInformation($"Logo image added to preview container. Children count: {stackPanel.Children.Count}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error loading logo for preview from path: {ViewModel.LogoPath}");
                    
                    // Show error in UI
                    ShowLogoError($"Failed to load logo in preview: {ex.Message}");
                    
                    // Add a placeholder text to show logo should be here
                    var logoPlaceholder = CreatePreviewText("[LOGO - FAILED TO LOAD]", true, true);
                    logoPlaceholder.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
                    stackPanel.Children.Add(logoPlaceholder);
                }
            }
            else
            {
                _logger.LogInformation("Logo not shown - either ShowLogo is false or LogoPath is empty");
                if (ViewModel.ShowLogo)
                {
                    var logoPlaceholder = CreatePreviewText("[NO LOGO SELECTED]", true, true);
                    logoPlaceholder.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                    stackPanel.Children.Add(logoPlaceholder);
                }
            }

            // Business Name
            if (!string.IsNullOrEmpty(ViewModel.BusinessName))
            {
                stackPanel.Children.Add(CreatePreviewText(ViewModel.BusinessName, true, true));
            }
            else
            {
                stackPanel.Children.Add(CreatePreviewText("Your Business Name", true, true));
            }
            
            // Business Address
            if (!string.IsNullOrEmpty(ViewModel.BusinessAddress))
            {
                stackPanel.Children.Add(CreatePreviewText(ViewModel.BusinessAddress, false, true));
            }
            else
            {
                stackPanel.Children.Add(CreatePreviewText("123 Main Street, City, State 12345", false, true));
            }
            
            // Business Contact Info
            if (!string.IsNullOrEmpty(ViewModel.BusinessPhone))
            {
                stackPanel.Children.Add(CreatePreviewText($"Tel: {ViewModel.BusinessPhone}", false, true));
            }
            else
            {
                stackPanel.Children.Add(CreatePreviewText("Tel: (555) 123-4567", false, true));
            }
            
            if (!string.IsNullOrEmpty(ViewModel.BusinessEmail))
            {
                stackPanel.Children.Add(CreatePreviewText($"Email: {ViewModel.BusinessEmail}", false, true));
            }
            else
            {
                stackPanel.Children.Add(CreatePreviewText("Email: info@yourstore.com", false, true));
            }
            
            if (!string.IsNullOrEmpty(ViewModel.BusinessWebsite))
            {
                stackPanel.Children.Add(CreatePreviewText($"Web: {ViewModel.BusinessWebsite}", false, true));
            }

            // Separator
            stackPanel.Children.Add(CreateSeparatorLine());

            // Receipt Type Header
            var receiptType = isProForma ? "PRE-BILL RECEIPT" : "FINAL RECEIPT";
            stackPanel.Children.Add(CreatePreviewText(receiptType, true, true));

            // Date/Time
            if (ViewModel.ShowDateTime)
            {
                stackPanel.Children.Add(CreatePreviewText($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}", false, false));
            }

            // Table Number
            if (ViewModel.ShowTableNumber)
            {
                stackPanel.Children.Add(CreatePreviewText("Table: 5", false, false));
            }

            // Server Name
            if (ViewModel.ShowServerName)
            {
                stackPanel.Children.Add(CreatePreviewText("Server: John Doe", false, false));
            }

            // Bill ID
            stackPanel.Children.Add(CreatePreviewText("Bill #: 12345", false, false));

            // Separator
            stackPanel.Children.Add(CreateSeparatorLine());

            // Items
            if (ViewModel.ShowItemDetails)
            {
                stackPanel.Children.Add(CreateItemHeader());
                stackPanel.Children.Add(CreateItemLine("Burger Deluxe", 2, 15.99m));
                stackPanel.Children.Add(CreateItemLine("French Fries", 2, 4.99m));
                stackPanel.Children.Add(CreateItemLine("Soft Drink", 2, 2.99m));
                stackPanel.Children.Add(CreateSeparatorLine());
            }

            // Totals
            if (ViewModel.ShowSubtotal)
            {
                stackPanel.Children.Add(CreateTotalLine("Subtotal:", 47.94m));
            }

            if (ViewModel.ShowDiscount)
            {
                stackPanel.Children.Add(CreateTotalLine("Discount:", -4.79m));
            }

            if (ViewModel.ShowTax)
            {
                var taxAmount = 43.15m * (decimal)(ViewModel.TaxRate / 100);
                stackPanel.Children.Add(CreateTotalLine($"{ViewModel.TaxLabel} ({ViewModel.TaxRate:F1}%):", taxAmount));
            }

            // Final Total
            stackPanel.Children.Add(CreateSeparatorLine());
            stackPanel.Children.Add(CreateTotalLine("TOTAL:", 48.76m, true));

            // Payment Method (Final Receipt only)
            if (!isProForma && ViewModel.ShowPaymentMethod)
            {
                stackPanel.Children.Add(CreateSeparatorLine());
                stackPanel.Children.Add(CreatePreviewText("Payment: Cash", false, false));
                stackPanel.Children.Add(CreatePreviewText("Amount Paid: $50.00", false, false));
                stackPanel.Children.Add(CreatePreviewText("Change: $1.24", false, false));
            }

            // Footer
            if (!string.IsNullOrEmpty(ViewModel.FooterMessage))
            {
                stackPanel.Children.Add(CreateSeparatorLine());
                var footerLines = ViewModel.FooterMessage.Split('\n');
                foreach (var line in footerLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        stackPanel.Children.Add(CreatePreviewText(line.Trim(), false, true));
                    }
                }
            }

            return stackPanel;
        }

        private TextBlock CreatePreviewText(string text, bool isBold = false, bool isCenter = false)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = ViewModel.FontSize,
                FontWeight = isBold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                HorizontalAlignment = isCenter ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 1, 0, 1),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black)
            };
        }

        private Border CreateSeparatorLine()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        private Grid CreateItemHeader()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var itemText = CreatePreviewText("Item", true);
            var qtyText = CreatePreviewText("Qty", true);
            var priceText = CreatePreviewText("Price", true);
            var totalText = CreatePreviewText("Total", true);

            Grid.SetColumn(itemText, 0);
            Grid.SetColumn(qtyText, 1);
            Grid.SetColumn(priceText, 2);
            Grid.SetColumn(totalText, 3);

            grid.Children.Add(itemText);
            grid.Children.Add(qtyText);
            grid.Children.Add(priceText);
            grid.Children.Add(totalText);

            return grid;
        }

        private Grid CreateItemLine(string itemName, int quantity, decimal unitPrice)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var itemText = CreatePreviewText(itemName);
            var qtyText = CreatePreviewText(quantity.ToString());
            var priceText = CreatePreviewText($"${unitPrice:F2}");
            var totalText = CreatePreviewText($"${quantity * unitPrice:F2}");

            Grid.SetColumn(itemText, 0);
            Grid.SetColumn(qtyText, 1);
            Grid.SetColumn(priceText, 2);
            Grid.SetColumn(totalText, 3);

            grid.Children.Add(itemText);
            grid.Children.Add(qtyText);
            grid.Children.Add(priceText);
            grid.Children.Add(totalText);

            return grid;
        }

        private Grid CreateTotalLine(string label, decimal amount, bool isBold = false)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelText = CreatePreviewText(label, isBold);
            var amountText = CreatePreviewText($"${amount:F2}", isBold);
            amountText.HorizontalAlignment = HorizontalAlignment.Right;

            Grid.SetColumn(labelText, 0);
            Grid.SetColumn(amountText, 1);

            grid.Children.Add(labelText);
            grid.Children.Add(amountText);

            return grid;
        }

        private FrameworkElement CreateErrorPreview(string errorMessage)
        {
            return new TextBlock
            {
                Text = errorMessage,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontStyle = Windows.UI.Text.FontStyle.Italic
            };
        }

        private double GetLogoSize()
        {
            return ViewModel.LogoSizeIndex switch
            {
                0 => 32,
                1 => 64,
                2 => 96,
                _ => 64
            };
        }

        private HorizontalAlignment GetLogoAlignment()
        {
            return ViewModel.LogoPositionIndex switch
            {
                0 => HorizontalAlignment.Left,
                1 => HorizontalAlignment.Center,
                2 => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Center
            };
        }

        #endregion

        #region Helper Methods

        private void UpdateStatusMessage(string message)
        {
            StatusTextBlock.Text = message;
            _logger.LogInformation("Status: {Message}", message);
        }

        #endregion
    }
}
