using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MagiDesk.Frontend.Services;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Linq;

namespace MagiDesk.Frontend.Views;

public sealed partial class RbacTestPage : Page
{
    private string? _currentUserId;
    private string[] _permissions = Array.Empty<string>();

    public RbacTestPage()
    {
        this.InitializeComponent();
        Loaded += RbacTestPage_Loaded;
    }

    private async void RbacTestPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadUserInfo();
    }

    private async Task LoadUserInfo()
    {
        try
        {
            // Ensure session is loaded
            if (SessionService.Current == null)
            {
                await SessionService.InitializeAsync();
            }
            var session = SessionService.Current;
            if (session != null)
            {
                _currentUserId = session.UserId;
                UsernameText.Text = session.Username ?? "N/A";
                RoleText.Text = session.Role ?? "N/A";
                UserIdText.Text = session.UserId ?? "N/A";

                // Try to get permissions from login response
                // For now, we'll need to fetch them separately
                await LoadPermissions();
            }
            else
            {
                UserIdText.Text = "Not logged in";
                UsernameText.Text = "N/A";
                RoleText.Text = "N/A";
            }
        }
        catch (Exception ex)
        {
            UserIdText.Text = $"Error: {ex.Message}";
        }
    }

    private async Task LoadPermissions()
    {
        try
        {
            // First, try to get permissions from session (saved during login)
            var session = SessionService.Current;
            if (session != null && session.Permissions != null && session.Permissions.Length > 0)
            {
                _permissions = session.Permissions;
                PermissionsCountText.Text = $"{_permissions.Length} permissions (from session)";
                PermissionsList.ItemsSource = _permissions.OrderBy(p => p);
                return;
            }

            // Fallback: Fetch from API if not in session
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                PermissionsCountText.Text = "0 permissions (not logged in)";
                return;
            }

            try
            {
                var http = new HttpClient();
                http.BaseAddress = new Uri("https://magidesk-users-904541739138.northamerica-south1.run.app/");
                
                var response = await http.GetAsync($"api/v2/rbac/users/{_currentUserId}/permissions");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _permissions = JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
                    
                    PermissionsCountText.Text = $"{_permissions.Length} permissions (from API)";
                    PermissionsList.ItemsSource = _permissions.OrderBy(p => p);
                }
                else
                {
                    PermissionsCountText.Text = $"Failed to load permissions (HTTP {(int)response.StatusCode})";
                }
            }
            catch (Exception apiEx)
            {
                PermissionsCountText.Text = $"Error fetching permissions: {apiEx.Message}";
            }
        }
        catch (Exception ex)
        {
            PermissionsCountText.Text = $"Error: {ex.Message}";
        }
    }

    private async void TestButton_Click(object sender, RoutedEventArgs e)
    {
        if (EndpointCombo.SelectedItem is not ComboBoxItem selected)
        {
            ShowResult("Error", "Please select an endpoint", false);
            return;
        }

        var tag = selected.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(tag))
        {
            ShowResult("Error", "Invalid endpoint selection", false);
            return;
        }

        TestButton.IsEnabled = false;
        ResultBorder.Visibility = Visibility.Collapsed;

        try
        {
            var includeUserId = IncludeUserIdCheck.IsChecked == true;
            var result = await TestEndpointAsync(tag, includeUserId);
            ShowResult(result.Status, result.Details, result.Success);
        }
        catch (Exception ex)
        {
            ShowResult("Error", $"Exception: {ex.Message}\n\n{ex.StackTrace}", false);
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }

    private async Task<(bool Success, string Status, string Details)> TestEndpointAsync(string endpointTag, bool includeUserId)
    {
        var endpoints = new Dictionary<string, (string Url, string Method)>
        {
            { "settings", ("https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend", "GET") },
            { "menu", ("https://magidesk-menu-904541739138.northamerica-south1.run.app/api/v2/menu/items", "GET") },
            { "orders", ("https://magidesk-order-904541739138.northamerica-south1.run.app/api/v2/orders", "GET") },
            { "inventory", ("https://magidesk-inventory-904541739138.northamerica-south1.run.app/api/v2/inventory/items", "GET") },
            { "payment", ("https://magidesk-payment-904541739138.northamerica-south1.run.app/api/v2/payments", "POST") }
        };

        if (!endpoints.TryGetValue(endpointTag, out var endpoint))
        {
            return (false, "Error", "Unknown endpoint");
        }

        try
        {
            using var http = new HttpClient();
            var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), endpoint.Url);

            if (includeUserId && !string.IsNullOrWhiteSpace(_currentUserId))
            {
                request.Headers.Add("X-User-Id", _currentUserId);
            }

            if (endpoint.Method == "POST")
            {
                var body = new { billingId = Guid.NewGuid().ToString(), amount = 0, paymentMethod = "cash" };
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            }

            var response = await http.SendAsync(request);
            var statusCode = (int)response.StatusCode;
            var content = await response.Content.ReadAsStringAsync();

            var success = statusCode == 200 || statusCode == 201;
            var status = $"HTTP {statusCode} {response.StatusCode}";
            var details = $"URL: {endpoint.Url}\nMethod: {endpoint.Method}\n";
            details += includeUserId ? $"X-User-Id: {_currentUserId}\n" : "X-User-Id: (not included)\n";
            details += $"\nResponse:\n{content}";

            return (success, status, details);
        }
        catch (Exception ex)
        {
            return (false, "Exception", $"Error: {ex.Message}\n\n{ex.StackTrace}");
        }
    }

    private void ShowResult(string status, string details, bool success)
    {
        ResultStatusText.Text = status;
        ResultDetailsText.Text = details;
        ResultBorder.Visibility = Visibility.Visible;
        
        if (success)
        {
            ResultBorder.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        else if (status.Contains("403"))
        {
            ResultBorder.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Orange);
        }
        else
        {
            ResultBorder.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private void CheckPermissionButton_Click(object sender, RoutedEventArgs e)
    {
        var permission = PermissionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(permission))
        {
            PermissionResultText.Text = "Please enter a permission";
            return;
        }

        var hasPermission = _permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        PermissionResultText.Text = hasPermission 
            ? $"✓ User HAS permission: {permission}" 
            : $"✗ User does NOT have permission: {permission}";
        PermissionResultText.Foreground = hasPermission 
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green)
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
    }
}

