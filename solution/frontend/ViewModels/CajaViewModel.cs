using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MagiDesk.Frontend.Services;

namespace MagiDesk.Frontend.ViewModels;

public sealed class CajaViewModel : INotifyPropertyChanged
{
    private readonly CajaService? _cajaService;
    private CajaService.CajaSessionDto? _activeSession;
    private bool _isLoading = false;
    private bool _hasError = false;
    private string? _errorMessage;

    public CajaViewModel()
    {
        _cajaService = App.CajaService;
        
        if (_cajaService == null)
        {
            HasError = true;
            ErrorMessage = "Caja service is not available. Please restart the application.";
        }

        RefreshCommand = new Services.RelayCommand(async _ => await LoadActiveSessionAsync());
        OpenCajaCommand = new Services.RelayCommand(_ => { /* Dialog will be shown from code-behind */ });
        CloseCajaCommand = new Services.RelayCommand(_ => { /* Dialog will be shown from code-behind */ });
        LoadHistoryCommand = new Services.RelayCommand(async _ => await LoadHistoryAsync());
    }

    public CajaService.CajaSessionDto? ActiveSession
    {
        get => _activeSession;
        set
        {
            _activeSession = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCajaOpen));
            OnPropertyChanged(nameof(CajaStatusText));
            OnPropertyChanged(nameof(OpenedByText));
            OnPropertyChanged(nameof(OpenedAtText));
        }
    }

    public bool IsCajaOpen => ActiveSession != null && ActiveSession.Status == "open";

    public string CajaStatusText => IsCajaOpen 
        ? $"Caja Abierta desde {ActiveSession?.OpenedAt:HH:mm}" 
        : "Caja Cerrada";

    public string OpenedByText => ActiveSession != null 
        ? $"Abierta por: {ActiveSession.OpenedByUserId}" 
        : "";

    public string OpenedAtText => ActiveSession != null 
        ? $"Abierta: {ActiveSession.OpenedAt:g}" 
        : "";

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<CajaSessionSummaryViewModel> SessionHistory { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand OpenCajaCommand { get; }
    public ICommand CloseCajaCommand { get; }
    public ICommand LoadHistoryCommand { get; }

    public event EventHandler<CajaService.CajaSessionDto>? CajaOpened;
    public event EventHandler<CajaService.CajaSessionDto>? CajaClosed;
    public event EventHandler<string>? ErrorOccurred;

    public async Task LoadActiveSessionAsync()
    {
        if (_cajaService == null) return;

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            var session = await _cajaService.GetActiveSessionAsync();
            ActiveSession = session;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error al cargar la sesión de caja: {ex.Message}";
            ErrorOccurred?.Invoke(this, ErrorMessage);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> OpenCajaAsync(decimal openingAmount, string? notes = null)
    {
        if (_cajaService == null)
        {
            ErrorMessage = "Caja service no disponible";
            return false;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            var request = new CajaService.OpenCajaRequestDto(openingAmount, notes);
            var session = await _cajaService.OpenCajaAsync(request);

            if (session == null)
            {
                HasError = true;
                ErrorMessage = "No se pudo abrir la caja. Verifique los permisos y que no haya otra caja abierta.";
                return false;
            }

            ActiveSession = session;
            CajaOpened?.Invoke(this, session);
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error al abrir la caja: {ex.Message}";
            ErrorOccurred?.Invoke(this, ErrorMessage);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> CloseCajaAsync(decimal closingAmount, string? notes = null, bool managerOverride = false)
    {
        if (_cajaService == null)
        {
            ErrorMessage = "Caja service no disponible";
            return false;
        }

        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = null;

            var request = new CajaService.CloseCajaRequestDto(closingAmount, notes, managerOverride);
            var session = await _cajaService.CloseCajaAsync(request);

            if (session == null)
            {
                HasError = true;
                ErrorMessage = "No se pudo cerrar la caja. Verifique que no haya mesas abiertas o pagos pendientes.";
                return false;
            }

            ActiveSession = null;
            CajaClosed?.Invoke(this, session);
            return true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error al cerrar la caja: {ex.Message}";
            ErrorOccurred?.Invoke(this, ErrorMessage);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadHistoryAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        if (_cajaService == null) return;

        try
        {
            IsLoading = true;
            var result = await _cajaService.GetHistoryAsync(startDate, endDate);

            if (result != null)
            {
                SessionHistory.Clear();
                foreach (var session in result.Items)
                {
                    SessionHistory.Add(new CajaSessionSummaryViewModel(session));
                }
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error al cargar el historial: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
