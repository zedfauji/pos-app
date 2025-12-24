using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace MagiDesk.Client.Services;

/// <summary>
/// Service abstraction for navigation operations.
/// Eliminates ViewModel dependencies on ShellViewModel and enables proper parameter passing.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified ViewModel type without parameters.
    /// </summary>
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;

    /// <summary>
    /// Navigates to the specified ViewModel type with a parameter object.
    /// The parameter will be passed to the ViewModel's InitializeAsync method if it exists,
    /// or stored for the View/Page to access via navigation event args.
    /// </summary>
    void NavigateTo<TViewModel>(object? parameter) where TViewModel : ObservableObject;

    /// <summary>
    /// Gets whether navigation can go back.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigates back to the previous ViewModel.
    /// </summary>
    void GoBack();
}

