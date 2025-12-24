using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;

namespace MagiDesk.Client.Services;

/// <summary>
/// Implementation of INavigationService that wraps ShellViewModel navigation.
/// This service provides a clean abstraction for navigation with parameter support.
/// Uses Lazy<ShellViewModel> to break circular dependency during DI resolution.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Lazy<ViewModels.ShellViewModel> _shellViewModel;
    private readonly Stack<object?> _navigationStack = new();
    private object? _pendingParameter;

    public NavigationService(IServiceProvider serviceProvider, Lazy<ViewModels.ShellViewModel> shellViewModel)
    {
        _serviceProvider = serviceProvider;
        _shellViewModel = shellViewModel;
    }

    public bool CanGoBack => _navigationStack.Count > 0;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        NavigateTo<TViewModel>(null);
    }

    public void NavigateTo<TViewModel>(object? parameter) where TViewModel : ObservableObject
    {
        var vmType = typeof(TViewModel);
        Log.Information("NavigationService: Navigating to {ViewModelType} with parameter: {HasParameter}", 
            vmType.Name, parameter != null);

        // Store parameter for ViewModel initialization
        _pendingParameter = parameter;

        // Push current ViewModel to stack
        if (_shellViewModel.Value.CurrentViewModel != null)
        {
            _navigationStack.Push(_shellViewModel.Value.CurrentViewModel);
        }

        // Resolve and set new ViewModel
        var newViewModel = _serviceProvider.GetRequiredService<TViewModel>();
        
        // Try to initialize with parameter if the ViewModel supports it
        InitializeViewModel(newViewModel, parameter);

        _shellViewModel.Value.CurrentViewModel = newViewModel;

        // Clear pending parameter after use
        _pendingParameter = null;
    }

    public void GoBack()
    {
        if (!CanGoBack)
        {
            Log.Warning("NavigationService: Cannot go back - navigation stack is empty");
            return;
        }

        var previousViewModel = _navigationStack.Pop();
        if (previousViewModel != null)
        {
            Log.Information("NavigationService: Navigating back to previous ViewModel");
            _shellViewModel.Value.CurrentViewModel = (ObservableObject)previousViewModel;
        }
    }

    /// <summary>
    /// Gets the pending navigation parameter (used by Pages during navigation).
    /// </summary>
    public object? GetPendingParameter() => _pendingParameter;

    /// <summary>
    /// Attempts to initialize a ViewModel with a parameter if it supports initialization.
    /// </summary>
    private void InitializeViewModel(ObservableObject viewModel, object? parameter)
    {
        if (parameter == null)
            return;

        Log.Information("InitializeViewModel: ViewModel={ViewModelType}, Parameter={ParameterType}", 
            viewModel.GetType().Name, parameter.GetType().Name);

        // Try to find and invoke InitializeAsync method via reflection
        var initializeMethod = viewModel.GetType().GetMethod("InitializeAsync");
        if (initializeMethod != null)
        {
            try
            {
                var parameters = initializeMethod.GetParameters();
                Log.Information("InitializeViewModel: Found InitializeAsync with {ParamCount} parameters", parameters.Length);
                
                if (parameters.Length > 0)
                {
                    Log.Information("InitializeViewModel: First param type = {Type}", parameters[0].ParameterType.Name);
                }
                
                // Handle string parameter (e.g., TableWorkspaceViewModel.InitializeAsync(string tableLabel))
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string) && parameter is string stringParam)
                {
                    Log.Information("InitializeViewModel: Matched string parameter pattern");
                    var task = initializeMethod.Invoke(viewModel, new object[] { stringParam }) as System.Threading.Tasks.Task;
                    if (task != null)
                    {
                        _ = task.ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                Log.Error(t.Exception?.GetBaseException(), "Error initializing ViewModel {ViewModelType}", viewModel.GetType().Name);
                            }
                        });
                    }
                }
                // Handle PaymentWorkspaceNavParams (3 parameters: Guid, Guid, string)
                else if (parameters.Length == 3 && parameter is Views.PaymentWorkspaceNavParams navParams)
                {
                    Log.Information("InitializeViewModel: Matched PaymentWorkspaceNavParams pattern - SessionId={SessionId}, BillingId={BillingId}, TableLabel={TableLabel}", 
                        navParams.SessionId, navParams.BillingId, navParams.TableLabel);
                    
                    var task = initializeMethod.Invoke(viewModel, new object[] 
                    { 
                        navParams.SessionId, 
                        navParams.BillingId, 
                        navParams.TableLabel 
                    }) as System.Threading.Tasks.Task;
                    
                    if (task != null)
                    {
                        _ = task.ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                Log.Error(t.Exception?.GetBaseException(), "Error initializing ViewModel {ViewModelType}", viewModel.GetType().Name);
                            }
                        });
                    }
                    else
                    {
                        Log.Warning("InitializeViewModel: Invoke returned null task");
                    }
                }
                // Direct parameter type match
                else if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(parameter))
                {
                    Log.Information("InitializeViewModel: Matched direct parameter type");
                    var task = initializeMethod.Invoke(viewModel, new[] { parameter }) as System.Threading.Tasks.Task;
                    if (task != null)
                    {
                        _ = task.ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                Log.Error(t.Exception?.GetBaseException(), "Error initializing ViewModel {ViewModelType}", viewModel.GetType().Name);
                            }
                        });
                    }
                }
                else
                {
                    Log.Warning("InitializeViewModel: No matching pattern. Parameters.Length={Length}, parameter is PaymentWorkspaceNavParams={IsNavParams}", 
                        parameters.Length, parameter is Views.PaymentWorkspaceNavParams);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "InitializeViewModel: Exception during invocation for {ViewModelType}. InnerException={InnerEx}", 
                    viewModel.GetType().Name, ex.InnerException?.Message);
            }
        }
        else
        {
            Log.Warning("InitializeViewModel: No InitializeAsync method found on {ViewModelType}", viewModel.GetType().Name);
        }
    }
}

