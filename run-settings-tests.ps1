# Test Runner Script for MagiDesk Settings API (PowerShell)
# This script runs all unit and integration tests for the settings functionality

Write-Host "Starting MagiDesk Settings API Test Suite..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan

# Set test environment variables
$env:ASPNETCORE_ENVIRONMENT = "Test"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=magidesk_test;Username=test_user;Password=test_password;SSL Mode=Disable;"

# Function to run tests and capture results
function Run-Tests {
    param(
        [string]$ProjectPath,
        [string]$TestName
    )
    
    Write-Host "Running $TestName..." -ForegroundColor Yellow
    
    $result = dotnet test $ProjectPath --verbosity normal --logger "console;verbosity=detailed" --collect:"XPlat Code Coverage"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ $TestName passed" -ForegroundColor Green
        return $true
    } else {
        Write-Host "✗ $TestName failed" -ForegroundColor Red
        return $false
    }
}

# Track test results
$totalTests = 0
$passedTests = 0
$failedTests = 0

# Run backend service tests
if (Test-Path "solution/backend/SettingsApi.Tests") {
    $totalTests++
    if (Run-Tests "solution/backend/SettingsApi.Tests/SettingsApi.Tests.csproj" "Backend Service Tests") {
        $passedTests++
    } else {
        $failedTests++
    }
} else {
    Write-Host "Backend test project not found" -ForegroundColor Red
    $failedTests++
}

# Run frontend ViewModel tests
if (Test-Path "solution/frontend/Tests") {
    $totalTests++
    if (Run-Tests "solution/frontend/Tests/Tests.csproj" "Frontend ViewModel Tests") {
        $passedTests++
    } else {
        $failedTests++
    }
} else {
    Write-Host "Frontend test project not found" -ForegroundColor Red
    $failedTests++
}

# Generate test report
Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Test Results Summary" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Total Test Suites: $totalTests"
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red

if ($failedTests -eq 0) {
    Write-Host "All tests passed! 🎉" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests failed. Please check the output above." -ForegroundColor Red
    exit 1
}

