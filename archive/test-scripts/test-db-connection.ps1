# Test Database Connection
Write-Host "`n=== Testing UsersApi Database Connection ===" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/debug/db" -Method GET -ErrorAction Stop
    Write-Host "Database Status:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Yellow
    }
}

