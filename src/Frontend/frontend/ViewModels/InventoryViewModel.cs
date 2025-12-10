using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;
using MagiDesk.Frontend.Models;

namespace MagiDesk.Frontend.ViewModels;

public class InventoryViewModel
{
    private readonly ApiService _api;

    public ObservableCollection<InventoryItem> Inventory { get; } = new();
    public ObservableCollection<JobStatusDto> JobHistory { get; } = new();
    public ObservableCollection<InventoryDisplay> InventoryDisplay { get; } = new();
    public ObservableCollection<InventoryDisplay> InventoryDisplayFiltered { get; } = new();

    public string? SearchText { get; set; }

    public InventoryViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Inventory.Clear();
        InventoryDisplay.Clear();
        InventoryDisplayFiltered.Clear();
        var list = await _api.GetInventoryAsync(ct);
        foreach (var item in list)
        {
            Inventory.Add(item);
            var disp = new InventoryDisplay
            {
                Id = item.Id,
                clave = item.clave,
                productname = item.productname,
                entradas = item.entradas.ToString("N2"),
                salidas = item.salidas.ToString("N2"),
                saldo = item.saldo.ToString("N2"),
                migrated_at = item.migrated_at?.ToLocalTime().ToString("g") ?? string.Empty
            };
            InventoryDisplay.Add(disp);
        }
        ApplyFilter();
    }

    public async Task RefreshJobsAsync(CancellationToken ct = default)
    {
        JobHistory.Clear();
        var list = await _api.GetRecentJobsAsync(10, ct);
        foreach (var j in list)
            JobHistory.Add(j);
    }

    public async Task<string?> LaunchSyncAsync(Func<Task> onTick, CancellationToken ct = default)
    {
        var (success, jobId) = await _api.LaunchSyncProductNamesAsync(ct);
        if (!success || string.IsNullOrWhiteSpace(jobId)) return null;
        // Poll every 1s until complete/failed
        while (true)
        {
            var job = await _api.GetJobAsync(jobId, ct);
            if (job == null) break;
            if (job.Status == "completed" || job.Status == "failed")
            {
                await RefreshJobsAsync(ct);
                if (job.Status == "completed")
                {
                    await LoadAsync(ct);
                }
                break;
            }
            if (onTick != null) await onTick();
            await Task.Delay(1000, ct);
        }
        return jobId;
    }

    public void ApplyFilter()
    {
        InventoryDisplayFiltered.Clear();
        var q = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
        IEnumerable<InventoryDisplay> src = InventoryDisplay;
        if (!string.IsNullOrEmpty(q))
        {
            src = src.Where(x => (x.clave ?? string.Empty).ToLowerInvariant().Contains(q)
                               || (x.productname ?? string.Empty).ToLowerInvariant().Contains(q));
        }
        foreach (var d in src)
            InventoryDisplayFiltered.Add(d);
    }

    public async Task<bool> HasRunningSyncAsync(CancellationToken ct = default)
    {
        var recent = await _api.GetRecentJobsAsync(5, ct);
        return recent.Any(j => j.Type == "syncProductNames" && j.Status == "running");
    }
}
