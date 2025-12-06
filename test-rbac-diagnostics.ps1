# RBAC Diagnostics Script
# This script will help identify why APIs are returning 500 instead of 403

Write-Host "=== RBAC Error Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if UsersApi is reachable
Write-Host "1. Testing UsersApi connectivity..." -ForegroundColor Yellow
try {
    $body = '{"username":"admin","password":"123456"}'
    $login = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/auth/login" -Method POST -ContentType "application/json" -Body $body
    Write-Host "  ✓ UsersApi is reachable" -ForegroundColor Green
    Write-Host "  User ID: $($login.userId)" -ForegroundColor Gray
    Write-Host "  Permissions: $($login.permissions.Count)" -ForegroundColor Gray
    $userId = $login.userId
    
    # Test permissions endpoint
    Write-Host "`n2. Testing permissions endpoint..." -ForegroundColor Yellow
    try {
        $perms = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/rbac/users/$userId/permissions" -Method GET
        Write-Host "  ✓ Permissions endpoint works" -ForegroundColor Green
        Write-Host "  Returned $($perms.Count) permissions" -ForegroundColor Gray
    } catch {
        Write-Host "  ✗ Permissions endpoint failed" -ForegroundColor Red
        Write-Host "  Status: $([int]$_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  ✗ UsersApi is not reachable" -ForegroundColor Red
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test function to get detailed error information
function Test-EndpointDetailed {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{}
    )
    
    Write-Host "$Name..." -ForegroundColor Yellow
    Write-Host "  URL: $Url" -ForegroundColor Gray
    Write-Host "  Method: $Method" -ForegroundColor Gray
    if ($Headers.Count -gt 0) {
        Write-Host "  Headers: $($Headers.Keys -join ', ')" -ForegroundColor Gray
    }
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -Headers $Headers -UseBasicParsing -ErrorAction Stop
        Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "  Content Length: $($response.Content.Length)" -ForegroundColor Gray
        if ($response.Content.Length -lt 500) {
            Write-Host "  Response: $($response.Content)" -ForegroundColor Gray
        }
        return $response
    } catch {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status: $statusCode" -ForegroundColor $(if ($statusCode -eq 403) { "Green" } elseif ($statusCode -eq 500) { "Red" } else { "Yellow" })
        
        # Try to read error response
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            $reader.Close()
            $stream.Close()
            
            Write-Host "  Response Body:" -ForegroundColor Gray
            Write-Host "  $responseBody" -ForegroundColor Gray
            
            # Try to parse as JSON
            try {
                $json = $responseBody | ConvertFrom-Json
                Write-Host "  Parsed JSON:" -ForegroundColor Gray
                $json | ConvertTo-Json -Depth 3 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
            } catch {
                Write-Host "  (Not valid JSON)" -ForegroundColor Gray
            }
        } catch {
            Write-Host "  Could not read response body: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Write-Host "  Exception Type: $($_.Exception.GetType().Name)" -ForegroundColor Gray
        Write-Host "  Exception Message: $($_.Exception.Message)" -ForegroundColor Gray
        
        return $null
    }
    Write-Host ""
}

Write-Host "3. Testing SettingsApi WITHOUT user ID..." -ForegroundColor Yellow
Test-EndpointDetailed -Name "  SettingsApi" -Url "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend"

Write-Host ""

Write-Host "4. Testing SettingsApi WITH user ID..." -ForegroundColor Yellow
Test-EndpointDetailed -Name "  SettingsApi" -Url "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend" -Headers @{ "X-User-Id" = $userId }

Write-Host ""

Write-Host "5. Testing MenuApi WITHOUT user ID..." -ForegroundColor Yellow
Test-EndpointDetailed -Name "  MenuApi" -Url "https://magidesk-menu-904541739138.northamerica-south1.run.app/api/v2/menu/items"

Write-Host ""

Write-Host "6. Testing OrderApi WITHOUT user ID..." -ForegroundColor Yellow
Test-EndpointDetailed -Name "  OrderApi" -Url "https://magidesk-order-904541739138.northamerica-south1.run.app/api/v2/orders"

Write-Host ""

Write-Host "=== Diagnostic Summary ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Please share:" -ForegroundColor Yellow
Write-Host "1. The Status Code and Response Body from the tests above" -ForegroundColor White
Write-Host "2. Any Cloud Run logs you can see for these APIs" -ForegroundColor White
Write-Host "3. Whether the error happens immediately or after a delay" -ForegroundColor White
Write-Host ""
Write-Host "This will help identify:" -ForegroundColor Yellow
Write-Host "- If HttpRbacService is failing to connect to UsersApi" -ForegroundColor White
Write-Host "- If the exception handler middleware is not catching errors" -ForegroundColor White
Write-Host "- If there's a configuration issue with UsersApi:BaseUrl" -ForegroundColor White

