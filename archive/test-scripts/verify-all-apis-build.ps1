# Verify All APIs Build Successfully
Write-Host "=== Final Build Verification ===" -ForegroundColor Cyan

$apis = @("OrderApi", "PaymentApi", "InventoryApi", "CustomerApi", "DiscountApi", "MenuApi", "SettingsApi")
$allPassed = $true

foreach ($api in $apis) {
    Write-Host ""
    Write-Host "${api}:" -ForegroundColor Yellow
    Push-Location "solution/backend/$api"
    
    $output = dotnet build -c Release -nologo 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Build succeeded" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Build failed" -ForegroundColor Red
        $output | Select-String -Pattern "error" | Select-Object -First 3 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        $allPassed = $false
    }
    
    Pop-Location
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
if ($allPassed) {
    Write-Host "✓ All APIs build successfully!" -ForegroundColor Green
} else {
    Write-Host "✗ Some APIs failed to build" -ForegroundColor Red
}

