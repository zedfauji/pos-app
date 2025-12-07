# Simple UsersApi Test
Write-Host "`n=== Testing UsersApi ===" -ForegroundColor Cyan

# Test v1 login
Write-Host "`n1. Testing v1 login..." -ForegroundColor Yellow
$body = @{
    username = "admin"
    password = "123456"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v1/auth/login" -Method POST -ContentType "application/json" -Body $body
    Write-Host "  ✓ v1 Login successful!" -ForegroundColor Green
    Write-Host "  User ID: $($response.UserId)" -ForegroundColor Cyan
    Write-Host "  Permissions: $($response.Permissions.Count) permissions" -ForegroundColor Cyan
    $testUserId = $response.UserId
} catch {
    Write-Host "  ✗ v1 Login failed: $($_.Exception.Message)" -ForegroundColor Red
    $testUserId = "test-user-id"
}

# Test v2 login
Write-Host "`n2. Testing v2 login..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/auth/login" -Method POST -ContentType "application/json" -Body $body
    Write-Host "  ✓ v2 Login successful!" -ForegroundColor Green
    Write-Host "  User ID: $($response.UserId)" -ForegroundColor Cyan
    Write-Host "  Permissions: $($response.Permissions.Count) permissions" -ForegroundColor Cyan
    if ($response.Permissions.Count -gt 0) {
        Write-Host "  Sample: $($response.Permissions[0..4] -join ', ')" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  ✗ v2 Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status Code: $statusCode" -ForegroundColor Yellow
    }
}

# Test v2 RBAC endpoint
Write-Host "`n3. Testing v2 RBAC endpoint..." -ForegroundColor Yellow
if ($testUserId -ne "test-user-id") {
    try {
        $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/api/v2/rbac/users/$testUserId/permissions" -Method GET
        Write-Host "  ✓ Get permissions successful!" -ForegroundColor Green
        Write-Host "  Permissions: $($response.Count) permissions" -ForegroundColor Cyan
    } catch {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status Code: $statusCode" -ForegroundColor Yellow
        if ($statusCode -eq 404) {
            Write-Host "  ⚠ Endpoint not found - route may be incorrect" -ForegroundColor Yellow
        }
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan

