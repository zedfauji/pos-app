using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs.Tables;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace MagiDesk.Frontend.Views;

public sealed partial class BillingPage : Page
{
    private readonly TableRepository _repo = new TableRepository();
    public ObservableCollection<BillItem> Bills { get; } = new();

    public BillingPage()
    {
        this.InitializeComponent();
        this.DataContext = this;
        Loaded += BillingPage_Loaded;
    }

    private Microsoft.UI.Xaml.XamlRoot GetXamlRoot()
    {
        return (App.MainWindow?.Content as FrameworkElement)?.XamlRoot ?? this.XamlRoot;
    }

    private void BillsList_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        // Ensure the row under the pointer is selected so context menu actions have SelectedItem
        if (e.OriginalSource is FrameworkElement fe && fe.DataContext is BillItem bi)
        {
            BillsList.SelectedItem = bi;
        }
    }

    private async void BillingPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        // WinUI 3 DatePicker defaults to 1601-01-01 when not set; treat as null
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;
        var fd = FromDate?.Date;
        var td = ToDate?.Date;
        if (fd.HasValue && fd.Value.Year > 1900) from = fd.Value;
        if (td.HasValue && td.Value.Year > 1900) to = td.Value;
        var table = string.IsNullOrWhiteSpace(TableFilter.Text) ? null : TableFilter.Text;
        var server = string.IsNullOrWhiteSpace(ServerFilter.Text) ? null : ServerFilter.Text;
        var list = await _repo.GetBillsAsync(from, to, table, server);
        Bills.Clear();
        foreach (var b in list)
        {
            Bills.Add(new BillItem(b));
        }
        if (CountText != null) CountText.Text = $"Count: {Bills.Count}";
    }

    private async void ApplyBtn_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async void FromDate_DateChanged(object sender, DatePickerValueChangedEventArgs args)
    {
        await LoadAsync();
    }

    private async void ToDate_DateChanged(object sender, DatePickerValueChangedEventArgs args)
    {
        await LoadAsync();
    }

    private (int paperMm, decimal taxPercent) ReadPrinterConfig()
    {
        try
        {
            var userCfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk", "appsettings.user.json");
            var cfg = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(userCfgPath, optional: true, reloadOnChange: true)
                .Build();
            var paperStr = cfg["Printer:PaperWidthMm"] ?? "58";
            var taxStr = cfg["Printer:TaxPercent"] ?? "0";
            int paper = 58; int.TryParse(paperStr, out paper);
            decimal tax = 0m; decimal.TryParse(taxStr, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out tax);
            return (paper, tax);
        }
        catch { return (58, 0m); }
    }

    private async void Reprint_Click(object sender, RoutedEventArgs e)
    {
        // Context menu path uses SelectedItem
        if (BillsList.SelectedItem is not BillItem bi)
        {
            // Fallback to sender.DataContext if available
            if ((sender as FrameworkElement)?.DataContext is BillItem bi2) bi = bi2; else return;
        }
        var full = await _repo.GetBillByIdAsync(bi.BillId);
        if (full == null)
        {
            // fallback to basic
            full = new MagiDesk.Shared.DTOs.Tables.BillResult
            {
                BillId = bi.BillId,
                TableLabel = bi.TableLabel,
                ServerName = bi.ServerName,
                StartTime = bi.StartTime,
                EndTime = bi.EndTime,
                TotalTimeMinutes = bi.TotalTimeMinutes,
                Items = new System.Collections.Generic.List<MagiDesk.Shared.DTOs.Tables.ItemLine>()
            };
        }
        var (paper, tax) = ReadPrinterConfig();
        var view = Services.ReceiptFormatter.BuildReceiptView(full, paper, tax);
        await Services.PrintService.PrintVisualAsync(view);
    }

    private async void ViewReceipt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Row button path uses sender.DataContext; context menu path may need SelectedItem
            BillItem? bi = (sender as FrameworkElement)?.DataContext as BillItem;
            if (bi == null)
            {
                bi = BillsList.SelectedItem as BillItem;
                if (bi == null) return;
            }
            Log.Info($"Preview receipt requested for bill {bi.BillId}");
            var full = await _repo.GetBillByIdAsync(bi.BillId) ?? new MagiDesk.Shared.DTOs.Tables.BillResult
            {
                BillId = bi.BillId,
                TableLabel = bi.TableLabel,
                ServerName = bi.ServerName,
                StartTime = bi.StartTime,
                EndTime = bi.EndTime,
                TotalTimeMinutes = bi.TotalTimeMinutes,
                Items = new System.Collections.Generic.List<MagiDesk.Shared.DTOs.Tables.ItemLine>()
            };
            var (paper, tax) = ReadPrinterConfig();
            var preview = Services.ReceiptFormatter.BuildReceiptView(full, paper, tax);
            await new ContentDialog
            {
                Title = "Receipt Preview",
                Content = preview,
                CloseButtonText = "Close",
                XamlRoot = GetXamlRoot()
            }.ShowAsync();
        }
        catch (Exception ex)
        {
            Log.Error("ViewReceipt preview failed", ex);
            await new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "Close", XamlRoot = this.XamlRoot }.ShowAsync();
        }
    }

    private async void ReprintButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BillItem? bi = (sender as FrameworkElement)?.DataContext as BillItem;
            if (bi == null)
            {
                bi = BillsList.SelectedItem as BillItem;
                if (bi == null) return;
            }
            Log.Info($"Row print requested for bill {bi.BillId}");
            var full = await _repo.GetBillByIdAsync(bi.BillId) ?? new MagiDesk.Shared.DTOs.Tables.BillResult
            {
                BillId = bi.BillId,
                TableLabel = bi.TableLabel,
                ServerName = bi.ServerName,
                StartTime = bi.StartTime,
                EndTime = bi.EndTime,
                TotalTimeMinutes = bi.TotalTimeMinutes,
                Items = new System.Collections.Generic.List<MagiDesk.Shared.DTOs.Tables.ItemLine>()
            };
            var (paper, tax) = ReadPrinterConfig();
            var preview = Services.ReceiptFormatter.BuildReceiptView(full, paper, tax);
            var dlg = new ContentDialog
            {
                Title = "Receipt Preview",
                Content = preview,
                PrimaryButtonText = "Print",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = GetXamlRoot()
            };
            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await Services.PrintService.PrintVisualAsync(preview);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Row print preview failed", ex);
            await new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "Close", XamlRoot = this.XamlRoot }.ShowAsync();
        }
    }
}

public class BillItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public BillItem(BillResult b)
    {
        BillId = b.BillId;
        TableLabel = b.TableLabel;
        ServerName = b.ServerName;
        TotalTimeMinutes = b.TotalTimeMinutes;
        StartTime = b.StartTime;
        EndTime = b.EndTime;
    }

    public Guid BillId { get; }
    public string ShortBillId => BillId.ToString()[..8];
    public string TableLabel { get; }
    public string ServerName { get; }
    public int TotalTimeMinutes { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public string StartLocal => StartTime.ToLocalTime().ToString("g");
    public string EndLocal => EndTime.ToLocalTime().ToString("g");

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
