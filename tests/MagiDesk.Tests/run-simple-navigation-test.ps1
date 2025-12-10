#!/usr/bin/env pwsh

Write-Host "🚀 RUNNING SIMPLE TABLES NAVIGATION TEST" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "🔪 Killing any existing MagiDesk processes..." -ForegroundColor Yellow

# Kill any existing MagiDesk processes
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

Write-Host ""
Write-Host "🎯 Running simple Tables navigation test..." -ForegroundColor Cyan
Write-Host "👀 WATCH: Test will focus specifically on Tables navigation!" -ForegroundColor Cyan

# Run the simple navigation test
$testResult = dotnet test MagiDesk.Tests --filter "SimpleNavigationTest_NavigateToTables_ShouldWork" --logger "console;verbosity=detailed"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Simple navigation test completed successfully!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Simple navigation test failed!" -ForegroundColor Red
}

Write-Host ""
Write-Host "🧹 Final cleanup..." -ForegroundColor Yellow

# Final cleanup
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🎉 Simple navigation test execution completed!" -ForegroundColor Green




