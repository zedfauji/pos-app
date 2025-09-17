#!/usr/bin/env pwsh

Write-Host "🚀 RUNNING UI AUTOMATION TEST WITH FORCED TIMEOUTS" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""

# Kill any existing MagiDesk processes first
Write-Host "🔪 Killing any existing MagiDesk processes..." -ForegroundColor Yellow
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "   Killing process: $($_.Id)" -ForegroundColor Red
    try {
        $_.Kill()
        $_.WaitForExit(2000)
    } catch {
        Write-Host "   Error killing process: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎯 Running test with FORCED timeouts (max 30 seconds total)..." -ForegroundColor Cyan
Write-Host "👀 WATCH: Test will either complete quickly or timeout aggressively!" -ForegroundColor Cyan
Write-Host ""

# Run the test with timeout
$testProcess = Start-Process -FilePath "dotnet" -ArgumentList "test", "MagiDesk.Tests", "--filter", "RealUI_DiscoverAndInteractWithRealElements", "--logger", "console;verbosity=detailed", "--no-build" -PassThru -NoNewWindow

# Wait for test to complete with overall timeout
if ($testProcess.WaitForExit(30000)) {  # 30 seconds max
    Write-Host ""
    Write-Host "✅ Test completed within timeout period" -ForegroundColor Green
    Write-Host "Exit Code: $($testProcess.ExitCode)" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "⏰ FORCED TIMEOUT: Test took longer than 30 seconds - KILLING" -ForegroundColor Red
    $testProcess.Kill()
    $testProcess.WaitForExit(2000)
}

# Cleanup any remaining processes
Write-Host ""
Write-Host "🧹 Final cleanup..." -ForegroundColor Yellow
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "   Killing remaining process: $($_.Id)" -ForegroundColor Red
    try {
        $_.Kill()
        $_.WaitForExit(2000)
    } catch {
        Write-Host "   Error killing process: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "🎉 Test execution completed!" -ForegroundColor Green



