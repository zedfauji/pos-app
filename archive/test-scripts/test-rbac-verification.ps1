# RBAC Verification Test Script
# Tests actual RBAC enforcement

$ErrorActionPreference = "Continue"

Write-Host "`n=== RBAC Verification Tests ===" -ForegroundColor Cyan

# Test 1: UsersApi v2 login
Write-Host "`n1. Testing UsersApi v2 login..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = "admin"
        password = "123456"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -ErrorAction Stop
    Write-Host "  ✓ Login successful!" -ForegroundColor Green
    Write-Host "  User ID: $($loginResponse.UserId)" -ForegroundColor Cyan
    Write-Host "  Permissions: $($loginResponse.Permissions.Count) permissions" -ForegroundColor Cyan
    $testUserId = $loginResponse.UserId
    $testPermissions = $loginResponse.Permissions
} catch {
    Write-Host "  ✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    $testUserId = "test-user-id"
}

# Test 2: SettingsApi v2 endpoint without user ID (should return 403)
Write-Host "`n2. Testing SettingsApi v2 endpoint WITHOUT user ID..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ FAILED: Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
    Write-Host "  RBAC is NOT enforcing permissions!" -ForegroundColor Red
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } else {
        Write-Host "  ✗ FAILED: Returned $statusCode (expected 403)" -ForegroundColor Red
    }
}

# Test 3: SettingsApi v2 endpoint with user ID but no permission (should return 403)
Write-Host "`n3. Testing SettingsApi v2 endpoint WITH user ID but NO permission..." -ForegroundColor Yellow
try {
    $headers = @{ "X-User-Id" = $testUserId }
    $response = Invoke-WebRequest -Uri "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend" -Method GET -Headers $headers -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ FAILED: Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
    Write-Host "  User may have SETTINGS_VIEW permission" -ForegroundColor Yellow
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403) {
        Write-Host "  ✓ PASSED: Returned 403 (RBAC working!)" -ForegroundColor Green
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

# Test 4: MenuApi v2 endpoint without user ID
Write-Host "`n4. Testing MenuApi v2 endpoint WITHOUT user ID..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://magidesk-menu-904541739138.northamerica-south1.run.app/api/v2/menu/items" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ FAILED: Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } elseif ($statusCode -eq 500) {
        Write-Host "  ⚠ ERROR: 500 Internal Server Error (check logs)" -ForegroundColor Yellow
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

# Test 5: OrderApi v2 endpoint (check if GET is supported)
Write-Host "`n5. Testing OrderApi v2 endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://magidesk-order-904541739138.northamerica-south1.run.app/api/v2/orders" -Method GET -UseBasicParsing -ErrorAction Stop
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Yellow
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 405) {
        Write-Host "  ⚠ Method Not Allowed - GET may not be supported, try POST" -ForegroundColor Yellow
    } elseif ($statusCode -eq 403 -or $statusCode -eq 401) {
        Write-Host "  ✓ PASSED: Returned $statusCode (RBAC working!)" -ForegroundColor Green
    } else {
        Write-Host "  Status: $statusCode" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan

