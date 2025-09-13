#!/usr/bin/env pwsh

Write-Host "🎬 REAL TABLES WORKFLOW DEMONSTRATION" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green

Write-Host ""
Write-Host "👀 WATCH: This will show ACTUAL Tables operations!" -ForegroundColor Cyan
Write-Host "📋 Including: Start Tables → Add Items → Stop Tables → Edit → Delete" -ForegroundColor Cyan
Write-Host "🍽️ Real workflow: Table Management, Order Processing, Service Control" -ForegroundColor Cyan

Write-Host ""
Write-Host "🔪 Killing ALL MagiDesk processes..." -ForegroundColor Yellow

# Kill ALL MagiDesk processes aggressively
Get-Process -Name "MagiDesk.Frontend" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Get-Process -Name "MagiDesk" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "🎯 Running REAL Tables workflow demonstration..." -ForegroundColor Cyan
Write-Host "⏰ Total timeout: 180 seconds (3 minutes)" -ForegroundColor Cyan
Write-Host "👀 WATCH: You'll see the COMPLETE Tables workflow!" -ForegroundColor Cyan

# Run the real Tables workflow test with a total timeout of 180 seconds
$timeout = 180
$job = Start-Job -ScriptBlock {
    Set-Location "C:\Users\giris\Documents\Code\Order-Tracking-By-GPT\solution"
    dotnet test MagiDesk.Tests --filter "RealTablesWorkflowTest_ShowActualTablesOperations_ShouldDemonstrateRealWorkflow" --logger "console;verbosity=detailed" --no-build
}

Write-Host "⏰ Waiting maximum $timeout seconds for real Tables workflow demonstration..."

if (Wait-Job $job -Timeout $timeout) {
    $result = Receive-Job $job
    Remove-Job $job
    
    Write-Host ""
    Write-Host "📋 REAL TABLES WORKFLOW OUTPUT:" -ForegroundColor Yellow
    Write-Host $result
    
    if ($result -match "Test Run Failed") {
        Write-Host ""
        Write-Host "❌ Real Tables workflow demonstration failed!" -ForegroundColor Red
    } else {
        Write-Host ""
        Write-Host "✅ Real Tables workflow demonstration completed successfully!" -ForegroundColor Green
    }
} else {
    Write-Host ""
    Write-Host "⏰ REAL TABLES WORKFLOW TIMED OUT after $timeout seconds!" -ForegroundColor Red
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
Write-Host "🎉 Real Tables workflow demonstration finished!" -ForegroundColor Green
Write-Host "👏 You have seen the ACTUAL Tables workflow!" -ForegroundColor Green
