#!/usr/bin/env pwsh

Write-Host "🔍 TABLES PAGE ANALYSIS TEST" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

# Build the test project first
Write-Host "🔨 Building test project..." -ForegroundColor Yellow
dotnet build MagiDesk.Tests/MagiDesk.Tests.csproj -c Debug --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green

# Run the tables page analysis test
Write-Host "🚀 Running Tables Page Analysis..." -ForegroundColor Yellow
Write-Host "This will analyze all functions available on the Tables page" -ForegroundColor Gray
Write-Host ""

dotnet test MagiDesk.Tests/MagiDesk.Tests.csproj --filter "TablesPageAnalysis" --logger "console;verbosity=detailed" --no-build

Write-Host ""
Write-Host "📊 Analysis complete!" -ForegroundColor Green




