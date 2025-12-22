using System.Threading.Tasks;
using MagiDesk.Client.Services.Dtos; // For OrderRequest
using MagiDesk.Shared.DTOs.Tables; // For BillResult

namespace MagiDesk.Client.Services;

public interface IPrinterService
{
    Task PrintReceiptAsync(BillResult bill);
    Task PrintTicketAsync(OrderRequest order, string tableLabel);
    Task PrintTextAsync(string text);
}
