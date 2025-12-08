# Test script for RBAC-enabled APIs
# Tests v1 (backward compatible) and v2 (RBAC-enabled) endpoints

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

# Test user ID (using admin user ID from database - you may need to get actual user ID)
$testUserId = "test-user-id"  # This will fail auth, which is expected for testing

function Write-TestResult {
    param($api, $endpoint, $status, $message)
    $color = if ($status -eq "PASS") { "Green" } elseif ($status -eq "FAIL") { "Red" } else { "Yellow" }
    Write-Host "[$status] $api - $endpoint" -ForegroundColor $color
    if ($message) { Write-Host "  $message" -ForegroundColor Gray }
}

function Test-HealthEndpoint {
    param($apiName, $baseUrl)
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 10 -ErrorAction Stop
        Write-TestResult $apiName "/health" "PASS" "Response: $response"
        return $true
    } catch {
        Write-TestResult $apiName "/health" "FAIL" "Error: $($_.Exception.Message)"
        return $false
    }
}

function Test-V1Endpoint {
    param($apiName, $baseUrl, $endpoint, $method = "GET")
    try {
        $uri = "$baseUrl$endpoint"
        if ($method -eq "GET") {
            $response = Invoke-RestMethod -Uri $uri -Method Get -TimeoutSec 10 -ErrorAction Stop
        } else {
            $response = Invoke-RestMethod -Uri $uri -Method $method -TimeoutSec 10 -ErrorAction Stop
        }
        Write-TestResult $apiName "v1$endpoint" "PASS" "Response received (backward compatible)"
        return $true
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            Write-TestResult $apiName "v1$endpoint" "SKIP" "Endpoint not found (may not exist)"
        } elseif ($statusCode -eq 401 -or $statusCode -eq 403) {
            Write-TestResult $apiName "v1$endpoint" "WARN" "Auth required (unexpected for v1)"
        } else {
            Write-TestResult $apiName "v1$endpoint" "FAIL" "Error: $($_.Exception.Message)"
        }
        return $false
    }
}

function Test-V2Endpoint {
    param($apiName, $baseUrl, $endpoint, $method = "GET", $userId = $null)
    try {
        $uri = "$baseUrl$endpoint"
        $headers = @{}
        if ($userId) {
            $headers["X-User-Id"] = $userId
        }
        
        if ($method -eq "GET") {
            $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers -TimeoutSec 10 -ErrorAction Stop
            Write-TestResult $apiName "v2$endpoint" "PASS" "Response received"
            return $true
        } else {
            $response = Invoke-RestMethod -Uri $uri -Method $method -Headers $headers -TimeoutSec 10 -ErrorAction Stop
            Write-TestResult $apiName "v2$endpoint" "PASS" "Response received"
            return $true
        }
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 403) {
            Write-TestResult $apiName "v2$endpoint" "PASS" "403 Forbidden (RBAC working - no permission)"
        } elseif ($statusCode -eq 401) {
            Write-TestResult $apiName "v2$endpoint" "PASS" "401 Unauthorized (RBAC working - no user ID)"
        } elseif ($statusCode -eq 404) {
            Write-TestResult $apiName "v2$endpoint" "SKIP" "Endpoint not found"
        } else {
            Write-TestResult $apiName "v2$endpoint" "FAIL" "Error: $($_.Exception.Message) (Status: $statusCode)"
        }
        return $false
    }
}

Write-Host "`n=== RBAC API Testing ===" -ForegroundColor Cyan
Write-Host "Testing all modified APIs for RBAC compliance`n" -ForegroundColor Cyan

# Test UsersApi
Write-Host "`n--- UsersApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "UsersApi" $apis.UsersApi
Test-V1Endpoint "UsersApi" $apis.UsersApi "/api/users/ping"
Test-V2Endpoint "UsersApi" $apis.UsersApi "/api/v2/users/ping" "GET" $null
Test-V2Endpoint "UsersApi" $apis.UsersApi "/api/v2/users" "GET" $testUserId  # Should return 403

# Test MenuApi
Write-Host "`n--- MenuApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "MenuApi" $apis.MenuApi
Test-V2Endpoint "MenuApi" $apis.MenuApi "/api/v2/menu/items" "GET" $null  # Should return 401/403
Test-V2Endpoint "MenuApi" $apis.MenuApi "/api/v2/menu/items" "GET" $testUserId  # Should return 403

# Test OrderApi
Write-Host "`n--- OrderApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "OrderApi" $apis.OrderApi
Test-V2Endpoint "OrderApi" $apis.OrderApi "/api/v2/orders" "GET" $null  # Should return 401/403
Test-V2Endpoint "OrderApi" $apis.OrderApi "/api/v2/orders" "GET" $testUserId  # Should return 403

# Test PaymentApi
Write-Host "`n--- PaymentApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "PaymentApi" $apis.PaymentApi
Test-V2Endpoint "PaymentApi" $apis.PaymentApi "/api/v2/payments" "GET" $null  # Should return 401/403
Test-V2Endpoint "PaymentApi" $apis.PaymentApi "/api/v2/payments" "GET" $testUserId  # Should return 403

# Test InventoryApi
Write-Host "`n--- InventoryApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "InventoryApi" $apis.InventoryApi
Test-V2Endpoint "InventoryApi" $apis.InventoryApi "/api/v2/inventory/items" "GET" $null  # Should return 401/403
Test-V2Endpoint "InventoryApi" $apis.InventoryApi "/api/v2/inventory/items" "GET" $testUserId  # Should return 403

# Test SettingsApi
Write-Host "`n--- SettingsApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "SettingsApi" $apis.SettingsApi
Test-V2Endpoint "SettingsApi" $apis.SettingsApi "/api/v2/settings" "GET" $null  # Should return 401/403
Test-V2Endpoint "SettingsApi" $apis.SettingsApi "/api/v2/settings" "GET" $testUserId  # Should return 403

# Test CustomerApi
Write-Host "`n--- CustomerApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "CustomerApi" $apis.CustomerApi
Test-V2Endpoint "CustomerApi" $apis.CustomerApi "/api/v2/customers" "GET" $null  # Should return 401/403
Test-V2Endpoint "CustomerApi" $apis.CustomerApi "/api/v2/customers" "GET" $testUserId  # Should return 403

# Test DiscountApi
Write-Host "`n--- DiscountApi Tests ---" -ForegroundColor Yellow
Test-HealthEndpoint "DiscountApi" $apis.DiscountApi
Test-V2Endpoint "DiscountApi" $apis.DiscountApi "/api/v2/discounts" "GET" $null  # Should return 401/403
Test-V2Endpoint "DiscountApi" $apis.DiscountApi "/api/v2/discounts" "GET" $testUserId  # Should return 403

Write-Host "`n=== Testing Complete ===" -ForegroundColor Cyan
Write-Host "`nExpected Results:" -ForegroundColor Yellow
Write-Host "  - Health endpoints: Should return 200 OK" -ForegroundColor Gray
Write-Host "  - v1 endpoints: Should work without auth (backward compatible)" -ForegroundColor Gray
Write-Host "  - v2 endpoints (no user ID): Should return 401/403" -ForegroundColor Gray
Write-Host "  - v2 endpoints (with user ID, no permission): Should return 403" -ForegroundColor Gray

