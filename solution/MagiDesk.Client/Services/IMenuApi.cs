using Refit;
using MagiDesk.Client.Services.Dtos;
using MagiDesk.Shared.DTOs.Menu;
using MagiDesk.Shared.DTOs.Common;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace MagiDesk.Client.Services;

public interface IMenuApi
{
    [Get("/api/menu/items")]
    Task<IApiResponse<PagedResult<MenuItemDto>>> ListItemsAsync([Query] MenuItemQueryDto query, CancellationToken ct = default);

    [Get("/api/menu/items/{id}")]
    Task<IApiResponse<MenuItemDto>> GetItemAsync(Guid id, CancellationToken ct = default);

    [Post("/api/menu/items")]
    Task<IApiResponse<MenuItemDto>> CreateMenuItemAsync([Body] CreateMenuItemDto dto, CancellationToken ct = default);

    [Put("/api/menu/items/{id}")]
    Task<IApiResponse<MenuItemDto>> UpdateMenuItemAsync(Guid id, [Body] UpdateMenuItemDto dto, CancellationToken ct = default);

    [Delete("/api/menu/items/{id}")]
    Task<IApiResponse<object>> DeleteMenuItemAsync(Guid id, CancellationToken ct = default);
}
