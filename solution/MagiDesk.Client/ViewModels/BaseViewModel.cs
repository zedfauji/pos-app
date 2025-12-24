using CommunityToolkit.Mvvm.ComponentModel;

namespace MagiDesk.Client.ViewModels;

/// <summary>
/// Base ViewModel class providing common properties and functionality for all ViewModels.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Clears the error message.
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Sets an error message.
    /// </summary>
    /// <param name="message">The error message to set.</param>
    protected void SetError(string message)
    {
        ErrorMessage = message;
    }
}

