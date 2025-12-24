using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Client.Services;
using MagiDesk.Client.Views.Dialogs;
using MagiDesk.Shared.DTOs.Shifts;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MagiDesk.Client.ViewModels;

public partial class ShiftControllerViewModel : ObservableObject
{
    private readonly IShiftApi _shiftApi;
    private readonly IReportingApi _reportingApi;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isShiftOpen;
    [ObservableProperty] private ShiftDto currentShift;
    [ObservableProperty] private string shiftStatusText = "Checking...";
    [ObservableProperty] private string errorMessage;

    // Stats
    [ObservableProperty] private decimal startingCash;
    [ObservableProperty] private decimal currentSales; // Placeholder, would come from reports
    
    [ObservableProperty] private ObservableCollection<ShiftDto> shiftHistory = new();

    public ShiftControllerViewModel(IShiftApi shiftApi, IReportingApi reportingApi, IDialogService dialogService)
    {
        _shiftApi = shiftApi;
        _reportingApi = reportingApi;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            // 1. Check Current Shift
            var current = await _shiftApi.GetCurrentShiftAsync();
            if (current != null)
            {
                CurrentShift = current;
                IsShiftOpen = true;
                ShiftStatusText = $"Shift #{current.ShiftNumber} (OPEN)";
                StartingCash = current.StartingCash;
            }
            else
            {
                CurrentShift = null;
                IsShiftOpen = false;
                ShiftStatusText = "No Shift Open";
            }

            // 2. Load History
            var history = await _shiftApi.GetHistoryAsync(limit: 10);
            ShiftHistory.Clear();
            foreach (var h in history) ShiftHistory.Add(h);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load data: {ex.Message}";
            IsShiftOpen = false; // Safe default
            ShiftStatusText = "Error";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task OpenShiftAsync()
    {
        var vm = new OpenShiftViewModel();
        var dialog = new OpenShiftDialog { DataContext = vm, XamlRoot = App.Current.MainWindow.Content.XamlRoot };
        
        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            IsLoading = true;
            try
            {
                var request = new OpenShiftRequest
                {
                    StartingCash = (decimal)vm.StartingCash,
                    Note = vm.Note ?? "",
                    IdempotencyKey = Guid.NewGuid()
                };

                await _shiftApi.OpenShiftAsync(request);
                await _dialogService.ShowMessageAsync("Success", "Shift Opened Successfully.");
                await LoadAsync(); // Refresh
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync("Error", $"Failed to open shift: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public async Task CloseShiftAsync()
    {
        if (CurrentShift == null) return;

        if (CurrentShift == null) return;

        IsLoading = true;
        MagiDesk.Shared.DTOs.Reporting.ZReportDto? report = null;
        try
        {
            report = await _reportingApi.GetShiftReportAsync(CurrentShift.ShiftId);
        }
        catch (Exception ex)
        {
             // Log or warn, but allow closing anyway?
             // For now, let's just show a warning toast if possible, or proceed with null report
             ErrorMessage = "Warning: Failed to fetch shift report. " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }

        var vm = new CloseShiftViewModel(CurrentShift, report);
        var dialog = new CloseShiftDialog { DataContext = vm, XamlRoot = App.Current.MainWindow.Content.XamlRoot };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            IsLoading = true;
            try
            {
                 var request = new CloseShiftRequest
                {
                    DeclaredCash = (decimal)vm.DeclaredCash,
                    Note = vm.Note ?? "",
                    IdempotencyKey = Guid.NewGuid()
                };

                await _shiftApi.CloseShiftAsync(CurrentShift.ShiftId, request);
                await _dialogService.ShowMessageAsync("Success", "Shift Closed Successfully.");
                await LoadAsync(); // Refresh
            }
            catch (Exception ex)
            {
                 await _dialogService.ShowMessageAsync("Error", $"Failed to close shift: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}

