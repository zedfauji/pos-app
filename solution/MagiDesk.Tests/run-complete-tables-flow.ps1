#!/usr/bin/env pwsh

Write-Host "🎬 COMPLETE TABLES FLOW DEMONSTRATION" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

Write-Host ""
Write-Host "👀 WATCH: This will show the ENTIRE Tables workflow!" -ForegroundColor Cyan
Write-Host "📋 Including: App Launch → Tables Navigation → Tables Actions → Page Navigation" -ForegroundColor Cyan

Write-Host ""
Write-Host "🔪 Killing ALL MagiDesk processes..." -ForegroundColor Yellow

# Kill ALL MagiDesk processes aggressively
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "MagiDesk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "🎯 Running COMPLETE Tables flow demonstration..." -ForegroundColor Cyan
Write-Host "⏰ Total timeout: 120 seconds (2 minutes)" -ForegroundColor Cyan
Write-Host "👀 WATCH: You'll see the complete Tables workflow!" -ForegroundColor Cyan

# Run the complete flow test with a total timeout of 120 seconds
$timeout = 120
$job = Start-Job -ScriptBlock {
    Set-Location "C:\Users\giris\Documents\Code\Order-Tracking-By-GPT\solution"
    dotnet test MagiDesk.Tests --filter "CompleteTablesFlowTest_ShowFullTablesWorkflow_ShouldDemonstrateEverything" --logger "console;verbosity=detailed" --no-build
}

Write-Host "⏰ Waiting maximum $timeout seconds for complete flow demonstration..."

if (Wait-Job $job -Timeout $timeout) {
    $result = Receive-Job $job
    Remove-Job $job
    
    Write-Host ""
    Write-Host "📋 COMPLETE FLOW DEMONSTRATION OUTPUT:" -ForegroundColor Yellow
    Write-Host $result
    
    if ($result -match "Test Run Failed") {
        Write-Host ""
        Write-Host "❌ Complete Tables flow demonstration failed!" -ForegroundColor Red
    } else {
        Write-Host ""
        Write-Host "✅ Complete Tables flow demonstration completed successfully!" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "⏰ COMPLETE FLOW DEMONSTRATION TIMED OUT after $timeout seconds!" -ForegroundColor Red
    Write-Host "🔪 Force killing test process..." -ForegroundColor Yellow
    
    Stop-Job $job -ErrorAction SilentlyContinue
    Remove-Job $job -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "🧹 Final cleanup..." -ForegroundColor Yellow

# Final cleanup
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "MagiDesk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -eq "dotnet" } | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "🎉 Complete Tables flow demonstration finished!" -ForegroundColor Green
Write-Host "👏 You have seen the entire Tables workflow!" -ForegroundColor Green

