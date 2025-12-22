using Refit;
using MagiDesk.Shared.DTOs.Shifts;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace MagiDesk.Client.Services;

public interface IShiftApi
{
    [Get("/shifts/validate")]
    Task<ShiftValidationDto> ValidateAsync(CancellationToken ct = default);

    [Get("/shifts/current")]
    Task<ShiftDto> GetCurrentShiftAsync(CancellationToken ct = default);

    [Post("/shifts/open")]
    Task<ShiftDto> OpenShiftAsync([Body] OpenShiftRequest request, CancellationToken ct = default);

    [Post("/shifts/{id}/close")]
    Task<ShiftDto> CloseShiftAsync(Guid id, [Body] CloseShiftRequest request, CancellationToken ct = default);

    [Get("/shifts/history")]
    Task<List<ShiftDto>> GetHistoryAsync([Query] int limit = 20, [Query] int offset = 0, CancellationToken ct = default);
}
