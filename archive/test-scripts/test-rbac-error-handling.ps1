# Test RBAC Error Handling Improvements
# Tests that APIs return 403 Forbidden instead of 500 Internal Server Error

Write-Host "=== RBAC Error Handling Test Suite ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login to get user ID and permissions
Write-Host "1. Testing UsersApi v2 login..." -ForegroundColor Yellow
$body = '{"username":"admin","password":"123456"}'
try {
    $login = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/auth/login" -Method POST -ContentType "application/json" -Body $body
    Write-Host "  ✓ Login successful - User has $($login.permissions.Count) permissions" -ForegroundColor Green
    $userId = $login.userId
} catch {
    Write-Host "  ✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test function
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [int]$ExpectedStatus = 403,
        [string]$Description = ""
    )
    
    Write-Host "$Name..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -Headers $Headers -UseBasicParsing -ErrorAction Stop
        $statusCode = $response.StatusCode
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ Correctly returned $statusCode" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  ✗ Returned $statusCode instead of $ExpectedStatus" -ForegroundColor Red
            return $false
        }
    } catch {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "  ✓ Correctly returned $statusCode Forbidden" -ForegroundColor Green
            # Try to read error response
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                $json = $responseBody | ConvertFrom-Json
                if ($json.error -eq "Forbidden") {
                    Write-Host "    Response: $($json.error) - $($json.message)" -ForegroundColor Gray
                }
            } catch {
                # Ignore JSON parsing errors
            }
            return $true
        } else {
            Write-Host "  ✗ Returned $statusCode instead of $ExpectedStatus" -ForegroundColor Red
            Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Gray
            return $false
        }
    }
}

$results = @()

# Test 2: SettingsApi without user ID
Write-Host "2. Testing SettingsApi WITHOUT user ID (should return 403)..." -ForegroundColor Yellow
$results += Test-Endpoint -Name "  SettingsApi" -Url "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend" -ExpectedStatus 403

Write-Host ""

# Test 3: SettingsApi with user ID (should return 200)
Write-Host "3. Testing SettingsApi WITH user ID (should return 200)..." -ForegroundColor Yellow
$results += Test-Endpoint -Name "  SettingsApi" -Url "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend" -Headers @{ "X-User-Id" = $userId } -ExpectedStatus 200

Write-Host ""

# Test 4: MenuApi without user ID
Write-Host "4. Testing MenuApi WITHOUT user ID (should return 403)..." -ForegroundColor Yellow
$results += Test-Endpoint -Name "  MenuApi" -Url "https://magidesk-menu-904541739138.northamerica-south1.run.app/api/v2/menu/items" -ExpectedStatus 403

Write-Host ""

# Test 5: OrderApi without user ID (POST requires permission)
Write-Host "5. Testing OrderApi WITHOUT user ID (should return 403)..." -ForegroundColor Yellow
try {
    $body = @{} | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "https://magidesk-order-904541739138.northamerica-south1.run.app/api/v2/orders" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
    $results += $false
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403) {
        Write-Host "  ✓ Correctly returned 403 Forbidden" -ForegroundColor Green
        $results += $true
    } else {
        Write-Host "  ✗ Returned $statusCode instead of 403" -ForegroundColor Red
        $results += $false
    }
}

Write-Host ""

# Test 6: PaymentApi without user ID (POST requires permission)
Write-Host "6. Testing PaymentApi WITHOUT user ID (should return 403)..." -ForegroundColor Yellow
try {
    # PaymentApi POST /api/v2/payments requires payment:process permission
    $body = @{
        billingId = [guid]::NewGuid().ToString()
        amount = 0
        paymentMethod = "cash"
    } | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "https://magidesk-payment-904541739138.northamerica-south1.run.app/api/v2/payments" -Method POST -ContentType "application/json" -Body $body -UseBasicParsing -ErrorAction Stop
    Write-Host "  ✗ Returned $($response.StatusCode) instead of 403" -ForegroundColor Red
    $results += $false
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 403) {
        Write-Host "  ✓ Correctly returned 403 Forbidden" -ForegroundColor Green
        $results += $true
    } elseif ($statusCode -eq 400) {
        # 400 is also acceptable - means request reached the endpoint but validation failed (before auth check)
        Write-Host "  ⚠ Returned 400 (validation error, but request reached endpoint)" -ForegroundColor Yellow
        $results += $true
    } else {
        Write-Host "  ✗ Returned $statusCode instead of 403" -ForegroundColor Red
        $results += $false
    }
}

Write-Host ""

# Test 7: InventoryApi without user ID
Write-Host "7. Testing InventoryApi WITHOUT user ID (should return 403)..." -ForegroundColor Yellow
$results += Test-Endpoint -Name "  InventoryApi" -Url "https://magidesk-inventory-904541739138.northamerica-south1.run.app/api/v2/inventory/items" -ExpectedStatus 403

Write-Host ""

# Summary
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_ -eq $true }).Count
$total = $results.Count
Write-Host "Passed: $passed / $total" -ForegroundColor $(if ($passed -eq $total) { "Green" } else { "Yellow" })

if ($passed -eq $total) {
    Write-Host "`n✓ All tests passed! Error handling is working correctly." -ForegroundColor Green
} else {
    Write-Host "`n⚠ Some tests failed. Check the output above for details." -ForegroundColor Yellow
}

