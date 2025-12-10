using MagiDesk.Shared.DTOs;

namespace InventoryApi.Repositories;

public interface IVendorRepository
{
    Task<IReadOnlyList<VendorDto>> ListAsync(CancellationToken ct);
    Task<VendorDto?> GetAsync(string id, CancellationToken ct);
    Task<VendorDto> CreateAsync(VendorDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(string id, VendorDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);
}

