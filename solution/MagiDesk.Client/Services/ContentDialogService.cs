using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Serilog;

namespace MagiDesk.Client.Services;

public class ContentDialogService : IDialogService
{
    private XamlRoot? GetXamlRoot()
    {
        return (App.Current as App)?.MainWindow?.Content?.XamlRoot;
    }

    // Static lock to prevent multiple dialogs from opening concurrently across the entire app
    private static readonly System.Threading.SemaphoreSlim _dialogLock = new(1, 1);

    private async Task<ContentDialogResult> ShowDialogSafeAsync(ContentDialog dialog)
    {
        Log.Information($"[DialogService] Requesting Lock for '{dialog.Title}'...");
        try
        {
            await _dialogLock.WaitAsync();
            Log.Information($"[DialogService] Lock Acquired. Showing Dialog '{dialog.Title}'...");
            var result = await dialog.ShowAsync();
            Log.Information($"[DialogService] Dialog Closed. Result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Log.Error($"[DialogService] CRITICAL ERROR showing dialog: {ex}");
            return ContentDialogResult.None; 
        }
        finally
        {
            _dialogLock.Release();
            Log.Information($"[DialogService] Lock Released.");
        }
    }

    public async Task ShowMessageAsync(string title, string message)
    {
        var root = GetXamlRoot();
        if (root == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = root
        };

        await ShowDialogSafeAsync(dialog);
    }

    public async Task<string?> RequestPinAsync()
    {
        var root = GetXamlRoot();
        if (root == null) return null;

        var inputTextBox = new PasswordBox { PlaceholderText = "Enter PIN" };
        var dialog = new ContentDialog
        {
            Title = "Authentication Required",
            Content = inputTextBox,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            XamlRoot = root
        };

        var result = await ShowDialogSafeAsync(dialog);
        if (result == ContentDialogResult.Primary)
        {
            return inputTextBox.Password;
        }
        return null;
    }

    public async Task<string?> RequestSelectionAsync(string title, IEnumerable<string> options)
    {
        var root = GetXamlRoot();
        if (root == null) return null;

        var listView = new ListView { ItemsSource = options, SelectionMode = ListViewSelectionMode.Single };
        var dialog = new ContentDialog
        {
            Title = title,
            Content = listView,
            PrimaryButtonText = "Select",
            CloseButtonText = "Cancel",
            XamlRoot = root
        };

        var result = await ShowDialogSafeAsync(dialog);
        if (result == ContentDialogResult.Primary && listView.SelectedItem is string selected)
        {
            return selected;
        }
        return null;
    }

    public async Task<bool> RequestConfirmationAsync(string title, string message)
    {
        var root = GetXamlRoot();
        if (root == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = root
        };

        var result = await ShowDialogSafeAsync(dialog);
        return result == ContentDialogResult.Primary;
    }

    public async Task<MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto?> ShowMenuItemDialogAsync(MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto? existingItem, bool isEdit = false)
    {
        var root = GetXamlRoot();
        if (root == null) return null;

        var dialog = new Views.Dialogs.MenuItemDialog(existingItem, isEdit)
        {
            XamlRoot = root
        };

        var result = await ShowDialogSafeAsync(dialog);
        if (result == ContentDialogResult.Primary)
        {
            return dialog.GetResult();
        }
        return null;
    }

    public async Task<MagiDesk.Shared.DTOs.Tables.StopSessionRequest?> ShowPaymentDialogAsync(double total)
    {
        var root = GetXamlRoot();
        if (root == null) return null;

        var dialog = new Views.Dialogs.PaymentDialog(total)
        {
            XamlRoot = root
        };

        var result = await ShowDialogSafeAsync(dialog);
        if (result == ContentDialogResult.Primary)
        {
            return dialog.GetRequest();
        }
        return null;
    }
}
