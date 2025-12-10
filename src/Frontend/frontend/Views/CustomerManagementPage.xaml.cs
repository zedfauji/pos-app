using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using MagiDesk.Frontend.Services;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagiDesk.Frontend.Views;

public sealed partial class CustomerManagementPage : Page
{
    private readonly CustomerApiService _customerService;
    private readonly ILogger<CustomerManagementPage> _logger;
    private readonly ObservableCollection<CustomerDto> _customers = new();
    private readonly List<CustomerDto> _allCustomers = new();
    private bool _isLoading = false;

    public CustomerManagementPage()
    {
        this.InitializeComponent();
        
        // Get services from DI container
        var serviceProvider = ((App)Application.Current).ServiceProvider;
        _customerService = serviceProvider.GetRequiredService<CustomerApiService>();
        _logger = serviceProvider.GetRequiredService<ILogger<CustomerManagementPage>>();
        
        CustomersListView.ItemsSource = _customers;
        Loaded += CustomerManagementPage_Loaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _ = LoadCustomersAsync();
    }

    private async void CustomerManagementPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        if (_isLoading) return;

        try
        {
            _isLoading = true;
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Loading customers...";

            var searchRequest = new CustomerSearchRequest
            {
                Page = 1,
                PageSize = 100,
                SortBy = "FirstName",
                SortDescending = false
            };

            var response = await _customerService.SearchCustomersAsync(searchRequest);
            
            _allCustomers.Clear();
            _customers.Clear();

            if (response?.Customers != null)
            {
                _allCustomers.AddRange(response.Customers);
                foreach (var customer in response.Customers)
                {
                    _customers.Add(customer);
                }

                CustomerCountTextBlock.Text = $"{response.TotalCount} customers";
                StatusTextBlock.Text = "Ready";
            }
            else
            {
                CustomerCountTextBlock.Text = "0 customers";
                StatusTextBlock.Text = "Failed to load customers";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
            StatusTextBlock.Text = "Error loading customers";
            
            var dialog = new ContentDialog()
            {
                Title = "Error",
                Content = $"Failed to load customers: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        finally
        {
            _isLoading = false;
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private void CustomerSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var searchTerm = sender.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                // Show all customers
                _customers.Clear();
                foreach (var customer in _allCustomers)
                {
                    _customers.Add(customer);
                }
            }
            else
            {
                // Filter customers
                var filtered = _allCustomers.Where(c =>
                    c.FullName.ToLower().Contains(searchTerm) ||
                    (c.Phone?.ToLower().Contains(searchTerm) ?? false) ||
                    (c.Email?.ToLower().Contains(searchTerm) ?? false)
                ).ToList();

                _customers.Clear();
                foreach (var customer in filtered)
                {
                    _customers.Add(customer);
                }
            }

            CustomerCountTextBlock.Text = $"{_customers.Count} customers";
        }
    }

    private void CustomerSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is CustomerDto customer)
        {
            sender.Text = customer.FullName;
            CustomersListView.SelectedItem = customer;
            CustomersListView.ScrollIntoView(customer);
        }
    }

    private async void CustomerSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            await SearchCustomersAsync(args.QueryText);
        }
    }

    private async Task SearchCustomersAsync(string searchTerm)
    {
        try
        {
            StatusTextBlock.Text = "Searching...";
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;

            var searchRequest = new CustomerSearchRequest
            {
                SearchTerm = searchTerm,
                Page = 1,
                PageSize = 100,
                SortBy = "FirstName"
            };

            var response = await _customerService.SearchCustomersAsync(searchRequest);
            
            _customers.Clear();
            if (response?.Customers != null)
            {
                foreach (var customer in response.Customers)
                {
                    _customers.Add(customer);
                }
                CustomerCountTextBlock.Text = $"{response.TotalCount} customers found";
            }
            else
            {
                CustomerCountTextBlock.Text = "No customers found";
            }

            StatusTextBlock.Text = "Ready";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            StatusTextBlock.Text = "Search failed";
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        CustomerSearchBox.Text = "";
        await LoadCustomersAsync();
    }

    private async void AddCustomer_Click(object sender, RoutedEventArgs e)
    {
        await ShowAddCustomerDialogAsync();
    }

    private async Task ShowAddCustomerDialogAsync()
    {
        var dialog = new ContentDialog()
        {
            Title = "Add New Customer",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };
        
        var firstNameBox = new TextBox { PlaceholderText = "First Name" };
        var lastNameBox = new TextBox { PlaceholderText = "Last Name" };
        var phoneBox = new TextBox { PlaceholderText = "Phone Number" };
        var emailBox = new TextBox { PlaceholderText = "Email Address" };
        var notesBox = new TextBox { PlaceholderText = "Notes (optional)", AcceptsReturn = true, Height = 80 };

        stackPanel.Children.Add(new TextBlock { Text = "First Name*", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(firstNameBox);
        stackPanel.Children.Add(new TextBlock { Text = "Last Name*", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(lastNameBox);
        stackPanel.Children.Add(new TextBlock { Text = "Phone", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(phoneBox);
        stackPanel.Children.Add(new TextBlock { Text = "Email", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(emailBox);
        stackPanel.Children.Add(new TextBlock { Text = "Notes", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(notesBox);

        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(firstNameBox.Text) || string.IsNullOrWhiteSpace(lastNameBox.Text))
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Validation Error",
                    Content = "First Name and Last Name are required.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            await CreateCustomerAsync(new CreateCustomerRequest
            {
                FirstName = firstNameBox.Text.Trim(),
                LastName = lastNameBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(phoneBox.Text) ? null : phoneBox.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(emailBox.Text) ? null : emailBox.Text.Trim(),
                Notes = string.IsNullOrWhiteSpace(notesBox.Text) ? null : notesBox.Text.Trim()
            });
        }
    }

    private async Task CreateCustomerAsync(CreateCustomerRequest request)
    {
        try
        {
            StatusTextBlock.Text = "Creating customer...";
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;

            var customer = await _customerService.CreateCustomerAsync(request);
            if (customer != null)
            {
                _allCustomers.Add(customer);
                _customers.Add(customer);
                CustomerCountTextBlock.Text = $"{_customers.Count} customers";
                StatusTextBlock.Text = "Customer created successfully";

                var successDialog = new ContentDialog()
                {
                    Title = "Success",
                    Content = $"Customer '{customer.FullName}' has been created successfully.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            else
            {
                StatusTextBlock.Text = "Failed to create customer";
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = "Failed to create customer. Please try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            StatusTextBlock.Text = "Error creating customer";
            
            var errorDialog = new ContentDialog()
            {
                Title = "Error",
                Content = $"Failed to create customer: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private void CustomersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Handle selection change if needed
    }

    private void CustomersListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CustomerDto customer)
        {
            // Navigate to customer details or show details pane
            ViewCustomerDetails(customer);
        }
    }

    private async void ViewCustomerDetails(CustomerDto customer)
    {
        var dialog = new ContentDialog()
        {
            Title = $"Customer Details - {customer.FullName}",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };
        
        stackPanel.Children.Add(new TextBlock { Text = $"Name: {customer.FullName}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        if (!string.IsNullOrEmpty(customer.Phone))
            stackPanel.Children.Add(new TextBlock { Text = $"Phone: {customer.Phone}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        if (!string.IsNullOrEmpty(customer.Email))
            stackPanel.Children.Add(new TextBlock { Text = $"Email: {customer.Email}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        
        stackPanel.Children.Add(new TextBlock { Text = $"Membership: {customer.MembershipLevel?.Name ?? "None"}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        stackPanel.Children.Add(new TextBlock { Text = $"Loyalty Points: {customer.LoyaltyPoints}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        stackPanel.Children.Add(new TextBlock { Text = $"Wallet Balance: {customer.Wallet?.Balance:C}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        stackPanel.Children.Add(new TextBlock { Text = $"Total Spent: {customer.TotalSpent:C}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        stackPanel.Children.Add(new TextBlock { Text = $"Total Visits: {customer.TotalVisits}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });
        stackPanel.Children.Add(new TextBlock { Text = $"Member Since: {customer.CreatedAt:MMM dd, yyyy}", Style = (Style)Application.Current.Resources["BodyTextBlockStyle"] });

        dialog.Content = stackPanel;
        await dialog.ShowAsync();
    }

    private async void EditCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is CustomerDto customer)
        {
            await ShowEditCustomerDialogAsync(customer);
        }
    }

    private async Task ShowEditCustomerDialogAsync(CustomerDto customer)
    {
        var dialog = new ContentDialog()
        {
            Title = $"Edit Customer - {customer.FullName}",
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };
        
        var firstNameBox = new TextBox { Text = customer.FirstName, PlaceholderText = "First Name" };
        var lastNameBox = new TextBox { Text = customer.LastName, PlaceholderText = "Last Name" };
        var phoneBox = new TextBox { Text = customer.Phone ?? "", PlaceholderText = "Phone Number" };
        var emailBox = new TextBox { Text = customer.Email ?? "", PlaceholderText = "Email Address" };
        var notesBox = new TextBox { Text = customer.Notes ?? "", PlaceholderText = "Notes", AcceptsReturn = true, Height = 80 };

        stackPanel.Children.Add(new TextBlock { Text = "First Name*", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(firstNameBox);
        stackPanel.Children.Add(new TextBlock { Text = "Last Name*", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(lastNameBox);
        stackPanel.Children.Add(new TextBlock { Text = "Phone", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(phoneBox);
        stackPanel.Children.Add(new TextBlock { Text = "Email", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(emailBox);
        stackPanel.Children.Add(new TextBlock { Text = "Notes", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(notesBox);

        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(firstNameBox.Text) || string.IsNullOrWhiteSpace(lastNameBox.Text))
            {
                var errorDialog = new ContentDialog()
                {
                    Title = "Validation Error",
                    Content = "First Name and Last Name are required.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
                return;
            }

            await UpdateCustomerAsync(customer.CustomerId, new UpdateCustomerRequest
            {
                FirstName = firstNameBox.Text.Trim(),
                LastName = lastNameBox.Text.Trim(),
                Phone = string.IsNullOrWhiteSpace(phoneBox.Text) ? null : phoneBox.Text.Trim(),
                Email = string.IsNullOrWhiteSpace(emailBox.Text) ? null : emailBox.Text.Trim(),
                Notes = string.IsNullOrWhiteSpace(notesBox.Text) ? null : notesBox.Text.Trim()
            });
        }
    }

    private async Task UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request)
    {
        try
        {
            StatusTextBlock.Text = "Updating customer...";
            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;

            var updatedCustomer = await _customerService.UpdateCustomerAsync(customerId, request);
            if (updatedCustomer != null)
            {
                // Update the customer in our collections
                var index = _allCustomers.FindIndex(c => c.CustomerId == customerId);
                if (index >= 0)
                {
                    _allCustomers[index] = updatedCustomer;
                }

                var displayIndex = _customers.ToList().FindIndex(c => c.CustomerId == customerId);
                if (displayIndex >= 0)
                {
                    _customers[displayIndex] = updatedCustomer;
                }

                StatusTextBlock.Text = "Customer updated successfully";

                var successDialog = new ContentDialog()
                {
                    Title = "Success",
                    Content = $"Customer '{updatedCustomer.FullName}' has been updated successfully.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            else
            {
                StatusTextBlock.Text = "Failed to update customer";
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = "Failed to update customer. Please try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer");
            StatusTextBlock.Text = "Error updating customer";
            
            var errorDialog = new ContentDialog()
            {
                Title = "Error",
                Content = $"Failed to update customer: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }
    }

    private async void ManageWallet_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is CustomerDto customer)
        {
            await ShowWalletManagementDialogAsync(customer);
        }
    }

    private async Task ShowWalletManagementDialogAsync(CustomerDto customer)
    {
        var dialog = new ContentDialog()
        {
            Title = $"Wallet Management - {customer.FullName}",
            PrimaryButtonText = "Add Funds",
            SecondaryButtonText = "Deduct Funds",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };
        
        stackPanel.Children.Add(new TextBlock { Text = $"Current Balance: {customer.Wallet?.Balance:C}", Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"] });
        
        var amountBox = new NumberBox { PlaceholderText = "Amount", Minimum = 0.01, Maximum = 10000, SmallChange = 1, LargeChange = 10 };
        var descriptionBox = new TextBox { PlaceholderText = "Description (optional)" };

        stackPanel.Children.Add(new TextBlock { Text = "Amount", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(amountBox);
        stackPanel.Children.Add(new TextBlock { Text = "Description", Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        stackPanel.Children.Add(descriptionBox);

        dialog.Content = stackPanel;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && amountBox.Value > 0)
        {
            // Add funds
            await AddWalletFundsAsync(customer.CustomerId, new AddWalletFundsRequest
            {
                Amount = (decimal)amountBox.Value,
                Description = string.IsNullOrWhiteSpace(descriptionBox.Text) ? "Manual top-up" : descriptionBox.Text.Trim()
            });
        }
        else if (result == ContentDialogResult.Secondary && amountBox.Value > 0)
        {
            // Deduct funds
            await DeductWalletFundsAsync(customer.CustomerId, new DeductWalletFundsRequest
            {
                Amount = (decimal)amountBox.Value,
                Description = string.IsNullOrWhiteSpace(descriptionBox.Text) ? "Manual deduction" : descriptionBox.Text.Trim()
            });
        }
    }

    private async Task AddWalletFundsAsync(Guid customerId, AddWalletFundsRequest request)
    {
        try
        {
            StatusTextBlock.Text = "Adding wallet funds...";
            var success = await _customerService.AddFundsAsync(customerId, request);
            
            if (success)
            {
                StatusTextBlock.Text = "Funds added successfully";
                await LoadCustomersAsync(); // Refresh to show updated balance
                
                var successDialog = new ContentDialog()
                {
                    Title = "Success",
                    Content = $"Successfully added {request.Amount:C} to wallet.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            else
            {
                StatusTextBlock.Text = "Failed to add funds";
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = "Failed to add funds to wallet. Please try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding wallet funds");
            StatusTextBlock.Text = "Error adding funds";
        }
    }

    private async Task DeductWalletFundsAsync(Guid customerId, DeductWalletFundsRequest request)
    {
        try
        {
            StatusTextBlock.Text = "Deducting wallet funds...";
            var success = await _customerService.DeductFundsAsync(customerId, request);
            
            if (success)
            {
                StatusTextBlock.Text = "Funds deducted successfully";
                await LoadCustomersAsync(); // Refresh to show updated balance
                
                var successDialog = new ContentDialog()
                {
                    Title = "Success",
                    Content = $"Successfully deducted {request.Amount:C} from wallet.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            else
            {
                StatusTextBlock.Text = "Failed to deduct funds";
                var errorDialog = new ContentDialog()
                {
                    Title = "Error",
                    Content = "Failed to deduct funds from wallet. Insufficient balance or other error.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting wallet funds");
            StatusTextBlock.Text = "Error deducting funds";
        }
    }

    private async void ViewCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuFlyoutItem)sender).Tag is CustomerDto customer)
        {
            ViewCustomerDetails(customer);
        }
    }

    private async void ViewTransactions_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuFlyoutItem)sender).Tag is CustomerDto customer)
        {
            var dialog = new ContentDialog()
            {
                Title = $"Transaction History - {customer.FullName}",
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot
            };

            var textBlock = new TextBlock 
            { 
                Text = "Transaction history feature will be implemented in a future update.",
                Style = (Style)Application.Current.Resources["BodyTextBlockStyle"]
            };

            dialog.Content = textBlock;
            await dialog.ShowAsync();
        }
    }

    private async void DeactivateCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuFlyoutItem)sender).Tag is CustomerDto customer)
        {
            var confirmDialog = new ContentDialog()
            {
                Title = "Confirm Deactivation",
                Content = $"Are you sure you want to deactivate customer '{customer.FullName}'?",
                PrimaryButtonText = "Deactivate",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await UpdateCustomerAsync(customer.CustomerId, new UpdateCustomerRequest { IsActive = false });
            }
        }
    }

    private async void ActivateCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuFlyoutItem)sender).Tag is CustomerDto customer)
        {
            await UpdateCustomerAsync(customer.CustomerId, new UpdateCustomerRequest { IsActive = true });
        }
    }
}
