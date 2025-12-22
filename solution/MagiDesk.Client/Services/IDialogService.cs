using System.Collections.Generic;
using System.Threading.Tasks;

namespace MagiDesk.Client.Services;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task<string?> RequestPinAsync();
    Task<string?> RequestSelectionAsync(string title, IEnumerable<string> options);
    Task<bool> RequestConfirmationAsync(string title, string message);
    Task<MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto?> ShowMenuItemDialogAsync(MagiDesk.Shared.DTOs.Menu.CreateMenuItemDto? existingItem, bool isEdit = false);
    Task<MagiDesk.Shared.DTOs.Tables.StopSessionRequest?> ShowPaymentDialogAsync(double total);
}
