# Launch MagiDesk Application and Run Real UI Automation Tests
# This script helps you launch the application and run tests that actually interact with the UI

param(
    [Parameter(Mandatory=$false)]
    [switch]$LaunchAppOnly,
    [Parameter(Mandatory=$false)]
    [switch]$TestOnly
)

$ErrorActionPreference = "Stop"

Write-Host "🎬 MagiDesk Real UI Automation Launcher" -ForegroundColor Magenta
Write-Host "=======================================" -ForegroundColor Magenta
Write-Host "👀 This will show you REAL UI interactions!" -ForegroundColor Cyan
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
            Write-Host "⏳ Please wait for the application to fully load..." -ForegroundColor Yellow
            Write-Host "🎯 You'll see the main window with navigation menu" -ForegroundColor Yellow
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

# Function to run real UI automation tests
function Run-RealUIAutomationTests {
    Write-Host "🧪 Running Real UI Automation Tests..." -ForegroundColor Cyan
    Write-Host "👀 These tests will actually interact with the UI!" -ForegroundColor Cyan
    Write-Host ""
    
    try {
        $testOutput = dotnet test MagiDesk.Tests --filter "TestCategory=RealUIAutomation" --configuration Release --verbosity normal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Real UI Automation tests completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Real UI Automation tests had some issues" -ForegroundColor Yellow
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
    $appProcess = Launch-MagiDeskApp
    
    if ($appProcess) {
        Write-Host ""
        Write-Host "🎉 Application launched! You can now interact with it manually." -ForegroundColor Green
        Write-Host "👀 Watch the application window - you should see the MagiDesk interface!" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "💡 To run real UI automation tests later, use:" -ForegroundColor Yellow
        Write-Host "   .\launch-and-test-real-ui.ps1 -TestOnly" -ForegroundColor White
        Write-Host ""
        Write-Host "🎯 The application will stay open for you to explore!" -ForegroundColor Green
    }
    
} elseif ($TestOnly) {
    Write-Host "🎯 Running Real UI Automation Tests Only" -ForegroundColor Cyan
    Write-Host "💡 Make sure the MagiDesk application is running and visible!" -ForegroundColor Yellow
    Write-Host "👀 The tests will actually click and interact with the UI!" -ForegroundColor Cyan
    Write-Host ""
    Run-RealUIAutomationTests
    
} else {
    Write-Host "🎯 Full Launch & Real UI Test Sequence" -ForegroundColor Cyan
    Write-Host "👀 You'll see REAL UI interactions happening!" -ForegroundColor Cyan
    Write-Host ""
    
    # Launch the application
    $appProcess = Launch-MagiDeskApp
    
    if ($appProcess) {
        Write-Host "⏳ Waiting for application to fully load..." -ForegroundColor Yellow
        Start-Sleep -Seconds 8
        
        Write-Host ""
        Write-Host "🎯 Application is ready! Now running REAL UI automation tests..." -ForegroundColor Cyan
        Write-Host "👀 Watch the application window - you'll see it actually navigating!" -ForegroundColor Cyan
        Write-Host "🔄 The tests will click on navigation items, buttons, and interact with the UI!" -ForegroundColor Cyan
        Write-Host ""
        
        # Run the real UI automation tests
        Run-RealUIAutomationTests
        
        Write-Host ""
        Write-Host "🎉 Complete! You've seen REAL UI interactions!" -ForegroundColor Green
        Write-Host "👀 The application should have navigated between pages!" -ForegroundColor Cyan
        Write-Host "📱 The MagiDesk application should still be running for you to explore." -ForegroundColor Green
        Write-Host ""
        Write-Host "🎯 What you should have seen:" -ForegroundColor Yellow
        Write-Host "  • Application window opening" -ForegroundColor White
        Write-Host "  • Navigation between Dashboard, Tables, Orders, Payments, Menu" -ForegroundColor White
        Write-Host "  • UI elements being clicked and interacted with" -ForegroundColor White
        Write-Host "  • Page transitions and content changes" -ForegroundColor White
        Write-Host "  • Real application behavior" -ForegroundColor White
    } else {
        Write-Host "❌ Could not launch application. Please build it first." -ForegroundColor Red
        Write-Host "💡 Run: dotnet build frontend/MagiDesk.Frontend.csproj -c Release" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎬 Thank you for using MagiDesk Real UI Testing!" -ForegroundColor Magenta
Write-Host "👀 You should have seen the application actually running and being tested!" -ForegroundColor Cyan
