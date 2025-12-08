# Launch MagiDesk Application and Run UI Tests
# This script helps you launch the application manually and then run UI automation tests

param(
    [Parameter(Mandatory=$false)]
    [switch]$LaunchAppOnly,
    [Parameter(Mandatory=$false)]
    [switch]$TestOnly
)

$ErrorActionPreference = "Stop"

Write-Host "🎬 MagiDesk Application Launcher & UI Tester" -ForegroundColor Magenta
Write-Host "=============================================" -ForegroundColor Magenta
Write-Host ""

# Function to find and launch the application
function Launch-MagiDeskApp {
    Write-Host "🔍 Looking for MagiDesk Application..." -ForegroundColor Cyan
    
    $possiblePaths = @(
        "frontend\bin\Release\net8.0-windows10.0.19041.0\MagiDesk.Frontend.exe",
        "frontend\bin\Debug\net8.0-windows10.0.19041.0\MagiDesk.Frontend.exe",
        "frontend\MagiDesk.Frontend.exe"
    )
    
    $appPath = $null
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $appPath = $path
            break
        }
    }
    
    if ($appPath) {
        Write-Host "✅ Found application at: $appPath" -ForegroundColor Green
        Write-Host "🚀 Launching MagiDesk Application..." -ForegroundColor Yellow
        Write-Host ""
        
        try {
            $process = Start-Process -FilePath $appPath -PassThru
            Write-Host "✅ Application launched successfully!" -ForegroundColor Green
            Write-Host "📱 Process ID: $($process.Id)" -ForegroundColor Green
            Write-Host "👀 You should see the MagiDesk application window opening!" -ForegroundColor Cyan
            Write-Host ""
            
            return $process
        } catch {
            Write-Host "❌ Failed to launch application: $($_.Exception.Message)" -ForegroundColor Red
            return $null
        }
    } else {
        Write-Host "❌ Application executable not found" -ForegroundColor Red
        Write-Host "🔍 Searched paths:" -ForegroundColor Yellow
        foreach ($path in $possiblePaths) {
            Write-Host "   - $path" -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "💡 Please build the frontend project first:" -ForegroundColor Yellow
        Write-Host "   dotnet build frontend/MagiDesk.Frontend.csproj -c Release" -ForegroundColor White
        return $null
    }
}

# Function to run UI automation tests
function Run-UIAutomationTests {
    Write-Host "🧪 Running UI Automation Tests..." -ForegroundColor Cyan
    Write-Host ""
    
    try {
        $testOutput = dotnet test MagiDesk.Tests --filter "TestCategory=UIAutomation" --configuration Release --verbosity normal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ UI Automation tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "⚠️ UI Automation tests had some issues" -ForegroundColor Yellow
        }
        
        Write-Host ""
        Write-Host "📊 Test Results:" -ForegroundColor Cyan
        Write-Host $testOutput -ForegroundColor White
        
    } catch {
        Write-Host "❌ Error running tests: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
if ($LaunchAppOnly) {
    Write-Host "🎯 Launching Application Only" -ForegroundColor Cyan
    Launch-MagiDeskApp
    Write-Host ""
    Write-Host "🎉 Application launched! You can now interact with it manually." -ForegroundColor Green
    Write-Host "💡 To run UI tests later, use: .\launch-app-and-test.ps1 -TestOnly" -ForegroundColor Yellow
    
} elseif ($TestOnly) {
    Write-Host "🎯 Running UI Tests Only" -ForegroundColor Cyan
    Write-Host "💡 Make sure the MagiDesk application is running!" -ForegroundColor Yellow
    Write-Host ""
    Run-UIAutomationTests
    
} else {
    Write-Host "🎯 Full Launch & Test Sequence" -ForegroundColor Cyan
    Write-Host ""
    
    # Launch the application
    $appProcess = Launch-MagiDeskApp
    
    if ($appProcess) {
        Write-Host "⏳ Waiting for application to fully load..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5
        
        Write-Host ""
        Write-Host "🎯 Application is ready! Now running UI automation tests..." -ForegroundColor Cyan
        Write-Host "👀 Watch the application window as tests interact with it!" -ForegroundColor Cyan
        Write-Host ""
        
        # Run the UI automation tests
        Run-UIAutomationTests
        
        Write-Host ""
        Write-Host "🎉 Complete! You've seen the application running and UI tests in action!" -ForegroundColor Green
        Write-Host "📱 The MagiDesk application should still be running for you to explore." -ForegroundColor Cyan
    } else {
        Write-Host "❌ Could not launch application. Please build it first." -ForegroundColor Red
        Write-Host "💡 Run: dotnet build frontend/MagiDesk.Frontend.csproj -c Release" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎬 Thank you for using MagiDesk UI Testing!" -ForegroundColor Magenta




