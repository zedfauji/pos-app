using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MagiDesk.Frontend.Services;
using MagiDesk.Frontend.Views;
using MagiDesk.Shared.DTOs;

namespace MagiDesk.Frontend.ViewModels;

public partial class VendorsViewModel : ObservableObject
{
    private readonly ApiClient _api;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<VendorDto> Vendors { get; } = new();

    public VendorsViewModel(ApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            Vendors.Clear();
            var list = await _api.GetVendorsAsync();
            if (list != null)
            {
                foreach (var v in list)
                    Vendors.Add(v);
            }
        }
        finally { IsBusy = false; }
    }

    public Task<VendorDto?> CreateAsync(VendorDto dto) => _api.CreateVendorAsync(dto);

    public async Task<VendorDto?> UpdateAsync(VendorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id)) return null;
        var ok = await _api.UpdateVendorAsync(dto.Id!, dto);
        return ok ? dto : null;
    }

    public async Task<bool> DeleteAsync(VendorDto vendor)
    {
        if (string.IsNullOrEmpty(vendor.Id)) return false;
        return await _api.DeleteVendorAsync(vendor.Id!);
    }
}
