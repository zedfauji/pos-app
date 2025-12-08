# Detailed API Testing Script
# Tests each API individually to identify issues

$ErrorActionPreference = "Continue"

Write-Host "`n=== Detailed API Testing ===" -ForegroundColor Cyan

# Test UsersApi v1 login first
Write-Host "`n1. Testing UsersApi v1 login..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "123456"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -ErrorAction Stop
    Write-Host "  ✓ v1 Login successful!" -ForegroundColor Green
    Write-Host "  User ID: $($response.UserId)" -ForegroundColor Cyan
    Write-Host "  Permissions: $($response.Permissions.Count) permissions" -ForegroundColor Cyan
    $testUserId = $response.UserId
} catch {
    Write-Host "  ✗ v1 Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status Code: $statusCode" -ForegroundColor Yellow
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
    $testUserId = "test-user-id"
}

# Test UsersApi v2 login
Write-Host "`n2. Testing UsersApi v2 login..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "123456"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -ErrorAction Stop
    Write-Host "  ✓ v2 Login successful!" -ForegroundColor Green
    Write-Host "  User ID: $($response.UserId)" -ForegroundColor Cyan
    Write-Host "  Permissions: $($response.Permissions.Count) permissions" -ForegroundColor Cyan
    if ($response.Permissions.Count -gt 0) {
        Write-Host "  Sample permissions: $($response.Permissions[0..4] -join ', ')" -ForegroundColor Cyan
    }
    $testUserId = $response.UserId
} catch {
    Write-Host "  ✗ v2 Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status Code: $statusCode" -ForegroundColor Yellow
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    }
}

# Test UsersApi v2 RBAC endpoint (get user permissions)
Write-Host "`n3. Testing UsersApi v2 RBAC endpoint (get user permissions)..." -ForegroundColor Yellow
if ($testUserId -ne "test-user-id") {
    try {
        $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/rbac/users/$testUserId/permissions" -Method GET -ErrorAction Stop
        Write-Host "  ✓ Get permissions successful!" -ForegroundColor Green
        Write-Host "  Permissions: $($response.Count) permissions" -ForegroundColor Cyan
    } catch {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status Code: $statusCode" -ForegroundColor Yellow
        if ($statusCode -eq 401 -or $statusCode -eq 403) {
            Write-Host "  ⚠ Authorization required (expected for RBAC endpoint)" -ForegroundColor Yellow
        } else {
            Write-Host "  ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Test SettingsApi v2 endpoint without user ID
Write-Host "`n4. Testing SettingsApi v2 endpoint WITHOUT user ID..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ FAILED: Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
    Write-Host "  RBAC is NOT enforcing permissions!" -ForegroundColor Red
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } elseif ($statusCode -eq 500) {
        Write-Host "  ✗ ERROR: 500 Internal Server Error" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

# Test MenuApi v2 endpoint without user ID
Write-Host "`n5. Testing MenuApi v2 endpoint WITHOUT user ID..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://magidesk-menu-904541739138.northamerica-south1.run.app/api/v2/menu/items" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ FAILED: Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } elseif ($statusCode -eq 500) {
        Write-Host "  ✗ ERROR: 500 Internal Server Error" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

# Test OrderApi v2 endpoint (POST for creating order)
Write-Host "`n6. Testing OrderApi v2 endpoint (POST)..." -ForegroundColor Yellow
try {
    $orderBody = @{
        tableId = "test-table"
        items = @()
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "https://magidesk-order-904541739138.northamerica-south1.run.app/api/v2/orders" -Method POST -ContentType "application/json" -Body $orderBody -UseBasicParsing -ErrorAction Stop
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Yellow
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } elseif ($statusCode -eq 400) {
        Write-Host "  ⚠ Bad Request (expected - invalid order data)" -ForegroundColor Yellow
    } elseif ($statusCode -eq 500) {
        Write-Host "  ✗ ERROR: 500 Internal Server Error" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

# Test InventoryApi v2 endpoint
Write-Host "`n7. Testing InventoryApi v2 endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://magidesk-inventory-904541739138.northamerica-south1.run.app/api/v2/inventory/items" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ FAILED: Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } elseif ($statusCode -eq 500) {
        Write-Host "  ✗ ERROR: 500 Internal Server Error" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "  Response: $responseBody" -ForegroundColor Yellow
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan

