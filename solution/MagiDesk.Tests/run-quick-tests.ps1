# Quick Test Runner for Critical Features
# This script runs the most critical tests to validate core functionality

param(
    [switch]$Critical,
    [switch]$Crash,
    [switch]$Health,
    [switch]$All
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Quick Test Runner for MagiDesk" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Set default to run critical tests if no specific option provided
if (-not ($Critical -or $Crash -or $Health -or $All)) {
    $Critical = $true
}

# Build the test project
Write-Host "`n🔨 Building test project..." -ForegroundColor Yellow
dotnet build MagiDesk.Tests -c Release --verbosity minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green

# Run tests based on parameters
$testResults = @()

if ($Critical -or $All) {
    Write-Host "`n🧪 Running Critical Flow Tests..." -ForegroundColor Yellow
    dotnet test MagiDesk.Tests --filter "Category=Critical" --configuration Release --verbosity minimal
    $testResults += $LASTEXITCODE -eq 0
}

if ($Crash -or $All) {
    Write-Host "`n🛡️ Running Crash Prevention Tests..." -ForegroundColor Yellow
    dotnet test MagiDesk.Tests --filter "Category=CrashPrevention" --configuration Release --verbosity minimal
    $testResults += $LASTEXITCODE -eq 0
}

if ($Health -or $All) {
    Write-Host "`n🏥 Running Health Check Tests..." -ForegroundColor Yellow
    dotnet test MagiDesk.Tests --filter "Category=HealthCheck" --configuration Release --verbosity minimal
    $testResults += $LASTEXITCODE -eq 0
}

# Summary
$passedTests = ($testResults | Where-Object { $_ -eq $true }).Count
$totalTests = $testResults.Count

Write-Host "`n📊 Quick Test Summary:" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Passed: $passedTests/$totalTests" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Red" })

if ($passedTests -eq $totalTests) {
    Write-Host "`n🎉 All quick tests passed!" -ForegroundColor Green
    Write-Host "Core functionality is working correctly." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠️  Some quick tests failed." -ForegroundColor Red
    Write-Host "Run full test suite for detailed analysis." -ForegroundColor Yellow
    exit 1
}




