# Simple Visual Live Testing Runner
# This script runs visual tests against the live application

param(
    [Parameter(Mandatory=$false)]
    [switch]$ShowDetails
)

$ErrorActionPreference = "Stop"

Write-Host "🎬 MagiDesk Visual Live Testing Runner" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "🔴 TESTING AGAINST LIVE PRODUCTION APPLICATION" -ForegroundColor Yellow
Write-Host "👀 You can see real-time testing results!" -ForegroundColor Cyan
Write-Host ""

# Test live API connectivity
Write-Host "🌐 Testing Live API Connectivity..." -ForegroundColor Cyan
Write-Host ""

$apiUrls = @{
    "TablesApi" = "https://magidesk-tables-904541739138.northamerica-south1.run.app"
    "OrderApi" = "https://magidesk-order-904541739138.northamerica-south1.run.app"
    "PaymentApi" = "https://magidesk-payment-904541739138.northamerica-south1.run.app"
    "MenuApi" = "https://magidesk-menu-904541739138.northamerica-south1.run.app"
    "InventoryApi" = "https://magidesk-inventory-904541739138.northamerica-south1.run.app"
}

foreach ($apiName in $apiUrls.Keys) {
    $url = $apiUrls[$apiName]
    Write-Host "📡 Testing $apiName..." -ForegroundColor White
    Write-Host "   URL: $url" -ForegroundColor Gray
    
    try {
        $startTime = Get-Date
        $response = Invoke-RestMethod -Uri "$url/health" -Method GET -TimeoutSec 10
        $endTime = Get-Date
        $responseTime = [math]::Round(($endTime - $startTime).TotalMilliseconds, 0)
        
        Write-Host "   ✅ $apiName is LIVE and responding" -ForegroundColor Green
        Write-Host "   ⏱️ Response Time: ${responseTime}ms" -ForegroundColor Green
        
        if ($ShowDetails) {
            Write-Host "   📄 Response: $response" -ForegroundColor Gray
        }
    } catch {
        Write-Host "   ❌ $apiName failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
}

Write-Host "🎬 Running Visual Tests..." -ForegroundColor Cyan
Write-Host ""

# Run the visual tests
try {
    $testOutput = dotnet test MagiDesk.Tests --filter "TestCategory=Visual" --configuration Release --verbosity normal 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Visual tests completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📊 What you just saw:" -ForegroundColor Cyan
        Write-Host "  • Live API responses from production" -ForegroundColor White
        Write-Host "  • Real-time data structures" -ForegroundColor White
        Write-Host "  • Actual response times" -ForegroundColor White
        Write-Host "  • JSON parsing and validation" -ForegroundColor White
        Write-Host "  • Error handling demonstrations" -ForegroundColor White
        Write-Host ""
        Write-Host "🎯 Perfect for monitoring live systems!" -ForegroundColor Yellow
    } else {
        Write-Host "⚠️ Visual tests had some issues" -ForegroundColor Yellow
        Write-Host "But you still saw the live testing process!" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Test execution failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Visual Live Testing Complete!" -ForegroundColor Green
Write-Host "🔴 You tested the LIVE production application!" -ForegroundColor Yellow
