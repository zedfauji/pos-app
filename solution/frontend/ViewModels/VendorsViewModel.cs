using System.Collections.ObjectModel;
using MagiDesk.Frontend.Services;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public class VendorsViewModel
{
    private readonly ApiService _api;
    public ObservableCollection<VendorDto> Vendors { get; } = new();

    public VendorsViewModel(ApiService api)
    {
        _api = api;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        Vendors.Clear();
        var list = await _api.GetVendorsAsync(ct);
        foreach (var v in list)
            Vendors.Add(v);
    }

    public async Task<VendorDto?> CreateAsync(VendorDto dto, CancellationToken ct = default)
        => await _api.CreateVendorAsync(dto, ct);

    public async Task<VendorDto?> UpdateAsync(VendorDto dto, CancellationToken ct = default)
        => await _api.UpdateVendorAsync(dto, ct);

    public async Task<bool> DeleteAsync(VendorDto dto, CancellationToken ct = default)
        => dto.Id is not null && await _api.DeleteVendorAsync(dto.Id!, ct);
}
