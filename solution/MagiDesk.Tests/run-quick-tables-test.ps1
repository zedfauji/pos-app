#!/usr/bin/env pwsh

Write-Host "🚀 QUICK TABLES TEST - AGGRESSIVE TIMEOUTS" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

Write-Host ""
Write-Host "🔪 Killing ALL MagiDesk processes..." -ForegroundColor Yellow

# Kill ALL MagiDesk processes aggressively
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "MagiDesk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "🎯 Running QUICK Tables test with 60 second total timeout..." -ForegroundColor Cyan
Write-Host "👀 WATCH: Test will timeout aggressively if it hangs!" -ForegroundColor Cyan

# Run the test with a total timeout of 60 seconds
$timeout = 60
$job = Start-Job -ScriptBlock {
    Set-Location "C:\Users\giris\Documents\Code\Order-Tracking-By-GPT\solution"
    dotnet test MagiDesk.Tests --filter "QuickTablesTest_NavigateToTables_ShouldWork" --logger "console;verbosity=detailed" --no-build
}

Write-Host "⏰ Waiting maximum $timeout seconds for test to complete..."

if (Wait-Job $job -Timeout $timeout) {
    $result = Receive-Job $job
    Remove-Job $job
    
    Write-Host ""
    Write-Host "📋 TEST OUTPUT:" -ForegroundColor Yellow
    Write-Host $result
    
    if ($result -match "Test Run Failed") {
        Write-Host ""
        Write-Host "❌ Quick Tables test failed!" -ForegroundColor Red
    } else {
        Write-Host ""
        Write-Host "✅ Quick Tables test completed!" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "⏰ TEST TIMED OUT after $timeout seconds!" -ForegroundColor Red
    Write-Host "🔪 Force killing test process..." -ForegroundColor Yellow
    
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "🧹 Final aggressive cleanup..." -ForegroundColor Yellow

# Final aggressive cleanup
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "MagiDesk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🎉 Quick Tables test execution completed!" -ForegroundColor Green



