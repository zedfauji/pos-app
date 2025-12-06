# Final RBAC API Testing Script
# Tests all modified APIs

$ErrorActionPreference = "Continue"

# API Base URLs
$apis = @{
    UsersApi = "https://magidesk-users-904541739138.northamerica-south1.run.app"
    MenuApi = "https://magidesk-menu-904541739138.northamerica-south1.run.app"
    OrderApi = "https://magidesk-order-904541739138.northamerica-south1.run.app"
    PaymentApi = "https://magidesk-payment-904541739138.northamerica-south1.run.app"
    InventoryApi = "https://magidesk-inventory-904541739138.northamerica-south1.run.app"
    SettingsApi = "https://magidesk-settings-904541739138.northamerica-south1.run.app"
    CustomerApi = "https://magidesk-customer-904541739138.northamerica-south1.run.app"
    DiscountApi = "https://magidesk-discount-904541739138.northamerica-south1.run.app"
}

function Test-Endpoint {
    param($apiName, $baseUrl, $endpoint, $method = "GET", $headers = @{}, $body = $null)
    
    try {
        $uri = "$baseUrl$endpoint"
        $params = @{
            Uri = $uri
            Method = $method
            Headers = $headers
            TimeoutSec = 15
            UseBasicParsing = $true
            ErrorAction = "Stop"
        }
        
        if ($body) {
            $params["Body"] = ($body | ConvertTo-Json -Depth 10)
            $params["ContentType"] = "application/json"
        }
        
        $response = Invoke-WebRequest @params
        $statusCode = $response.StatusCode
        
        Write-Host "[PASS] $apiName - $endpoint (HTTP $statusCode)" -ForegroundColor Green
        
        if ($statusCode -eq 200 -and $response.Content) {
            try {
                $json = $response.Content | ConvertFrom-Json
                if ($json.Permissions) {
                    Write-Host "  Permissions: $($json.Permissions.Count) permissions returned" -ForegroundColor Gray
                }
            } catch {
                # Not JSON, ignore
            }
        }
        
        return @{ Success = $true; StatusCode = $statusCode }
    } catch {
        $statusCode = 0
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode.value__
        }
        
        $statusText = if ($statusCode -eq 403) { "EXPECTED (RBAC working)" } 
                     elseif ($statusCode -eq 401) { "EXPECTED (Auth required)" }
                     elseif ($statusCode -eq 404) { "NOT FOUND" }
                     elseif ($statusCode -eq 405) { "METHOD NOT ALLOWED" }
                     else { "ERROR" }
        
        $color = if ($statusCode -eq 403 -or $statusCode -eq 401) { "Yellow" } else { "Red" }
        Write-Host "[$statusText] $apiName - $endpoint (HTTP $statusCode)" -ForegroundColor $color
        
        return @{ Success = $false; StatusCode = $statusCode }
    }
}

Write-Host "`n=== RBAC API Comprehensive Testing ===" -ForegroundColor Cyan
Write-Host "Testing all modified APIs for RBAC compliance`n" -ForegroundColor Cyan

# Test UsersApi
Write-Host "`n--- UsersApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "UsersApi" $apis.UsersApi "/health"
Test-Endpoint "UsersApi" $apis.UsersApi "/api/users/ping"  # v1 - should work
$loginResult = Test-Endpoint "UsersApi" $apis.UsersApi "/api/v2/auth/login" "POST" @{} @{ username = "admin"; password = "123456" }
Test-Endpoint "UsersApi" $apis.UsersApi "/api/v2/users/ping"  # v2 ping - no auth
Test-Endpoint "UsersApi" $apis.UsersApi "/api/v2/users" "GET" @{ "X-User-Id" = "test-user" }  # v2 - should return 403

# Test MenuApi
Write-Host "`n--- MenuApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "MenuApi" $apis.MenuApi "/health"
Test-Endpoint "MenuApi" $apis.MenuApi "/api/v2/menu/items" "GET"  # No user ID - should return 401/403
Test-Endpoint "MenuApi" $apis.MenuApi "/api/v2/menu/items" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

# Test OrderApi
Write-Host "`n--- OrderApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "OrderApi" $apis.OrderApi "/health"
Test-Endpoint "OrderApi" $apis.OrderApi "/api/v2/orders" "GET"  # No user ID - should return 401/403
Test-Endpoint "OrderApi" $apis.OrderApi "/api/v2/orders" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

# Test PaymentApi
Write-Host "`n--- PaymentApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "PaymentApi" $apis.PaymentApi "/health"
Test-Endpoint "PaymentApi" $apis.PaymentApi "/api/v2/payments" "GET"  # No user ID - should return 401/403
Test-Endpoint "PaymentApi" $apis.PaymentApi "/api/v2/payments" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

# Test InventoryApi
Write-Host "`n--- InventoryApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "InventoryApi" $apis.InventoryApi "/health"
Test-Endpoint "InventoryApi" $apis.InventoryApi "/api/v2/inventory/items" "GET"  # No user ID - should return 401/403
Test-Endpoint "InventoryApi" $apis.InventoryApi "/api/v2/inventory/items" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

# Test SettingsApi
Write-Host "`n--- SettingsApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "SettingsApi" $apis.SettingsApi "/health"
Test-Endpoint "SettingsApi" $apis.SettingsApi "/api/v2/settings" "GET"  # No user ID - should return 401/403
Test-Endpoint "SettingsApi" $apis.SettingsApi "/api/v2/settings" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

# Test CustomerApi
Write-Host "`n--- CustomerApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "CustomerApi" $apis.CustomerApi "/api/customers/ping"  # Check if ping exists
Test-Endpoint "CustomerApi" $apis.CustomerApi "/api/v2/customers" "GET"  # No user ID - should return 401/403
Test-Endpoint "CustomerApi" $apis.CustomerApi "/api/v2/customers" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

# Test DiscountApi
Write-Host "`n--- DiscountApi Tests ---" -ForegroundColor Yellow
Test-Endpoint "DiscountApi" $apis.DiscountApi "/api/discounts/ping"  # Check if ping exists
Test-Endpoint "DiscountApi" $apis.DiscountApi "/api/v2/discounts" "GET"  # No user ID - should return 401/403
Test-Endpoint "DiscountApi" $apis.DiscountApi "/api/v2/discounts" "GET" @{ "X-User-Id" = "test-user" }  # With user ID but no permission - should return 403

Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "`nExpected Results:" -ForegroundColor Yellow
Write-Host "  ✓ Health endpoints: 200 OK" -ForegroundColor Green
Write-Host "  ✓ v1 endpoints: 200 OK (backward compatible)" -ForegroundColor Green
Write-Host "  ✓ v2 login: 200 OK with permissions array" -ForegroundColor Green
Write-Host "  ✓ v2 endpoints (no X-User-Id): 401/403 (RBAC working)" -ForegroundColor Yellow
Write-Host "  ✓ v2 endpoints (with X-User-Id, no permission): 403 (RBAC working)" -ForegroundColor Yellow
Write-Host "`nNote: 404 responses indicate v2 controllers may not be deployed yet" -ForegroundColor Gray

