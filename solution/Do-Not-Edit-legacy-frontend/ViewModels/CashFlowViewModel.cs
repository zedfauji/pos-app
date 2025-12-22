using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using MagiDesk.Frontend.Models;

namespace MagiDesk.Frontend.ViewModels;

public class CashFlowViewModel
{
    private readonly ApiService _api;

    // Form fields
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal CashAmount { get; set; }
    public string? Notes { get; set; }

    public ObservableCollection<CashFlow> History { get; } = new();
    public ObservableCollection<CashFlowDisplay> HistoryDisplay { get; } = new();

    public CashFlowViewModel(ApiService api)
    {
        _api = api;
        // Default to today; page will constrain selection to current week
        Date = DateTime.Today;
    }

    public bool IsDateInCurrentWeek(DateTime date)
    {
        var today = DateTime.Today;
        var delta = (int)today.DayOfWeek;
        var monday = today.AddDays(-(delta == 0 ? 6 : delta - 1));
        var sunday = monday.AddDays(6);
        return date.Date >= monday && date.Date <= sunday;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        History.Clear();
        HistoryDisplay.Clear();
        var list = await _api.GetCashFlowHistoryAsync(ct);
        foreach (var item in list)
        {
            History.Add(item);
            HistoryDisplay.Add(new CashFlowDisplay
            {
                EmployeeName = item.EmployeeName,
                Date = item.Date.ToString("d"),
                CashAmount = string.Format(System.Globalization.CultureInfo.GetCultureInfo("en-US"), "{0:C}", item.CashAmount),
                Notes = item.Notes
            });
        }
    }

    public async Task<bool> SubmitAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(EmployeeName)) return false;
        if (!IsDateInCurrentWeek(Date)) return false;
        if (CashAmount == 0m) return false;

        var entry = new CashFlow
        {
            EmployeeName = EmployeeName.Trim(),
            Date = Date,
            CashAmount = CashAmount,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes
        };
        await _api.AddCashFlowAsync(entry, ct);
        await RefreshAsync(ct);
        // reset form
        CashAmount = 0m;
        Notes = string.Empty;
        return true;
    }
}
