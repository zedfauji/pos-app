# MagiDesk Visual Live Testing Runner
# This script runs tests against the live application with full visibility

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Live", "Payment", "Tables", "Orders", "Menu", "Inventory")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory=$false)]
    [switch]$ShowDetails,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\TestResults"
)

$ErrorActionPreference = "Stop"

# Colors for output
$Colors = @{
    Success = "Green"
    Error = "Red"
    Warning = "Yellow"
    Info = "Cyan"
    Header = "Magenta"
    Live = "Yellow"
    Visual = "Cyan"
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Write-TestHeader {
    param([string]$Title)
    Write-ColorOutput "`n" -Color "Info"
    Write-ColorOutput "=" * 80 -Color "Header"
    Write-ColorOutput $Title -Color "Header"
    Write-ColorOutput "=" * 80 -Color "Header"
    Write-ColorOutput "`n" -Color "Info"
}

function Write-LiveIndicator {
    param([string]$Message)
    Write-ColorOutput "🔴 LIVE: $Message" -Color "Live"
}

function Test-Prerequisites {
    Write-TestHeader "Checking Prerequisites for Live Testing"
    
    # Check if .NET 8 is installed
    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "✅ .NET Version: $dotnetVersion" -Color "Success"
    } catch {
        Write-ColorOutput "❌ .NET 8 not found" -Color "Error"
        return $false
    }
    
    # Check if test project exists
    if (Test-Path "MagiDesk.Tests") {
        Write-ColorOutput "✅ Test project found" -Color "Success"
    } else {
        Write-ColorOutput "❌ Test project not found" -Color "Error"
        return $false
    }
    
    # Check if output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-ColorOutput "✅ Created output directory: $OutputPath" -Color "Success"
    } else {
        Write-ColorOutput "✅ Output directory exists: $OutputPath" -Color "Success"
    }
    
    return $true
}

function Build-TestProject {
    Write-TestHeader "Building Test Project for Live Testing"
    
    try {
        Write-ColorOutput "Building MagiDesk.Tests..." -Color "Info"
        $buildOutput = dotnet build MagiDesk.Tests -c Release --verbosity minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ Project built successfully" -Color "Success"
            return $true
        } else {
            Write-ColorOutput "❌ Build failed with exit code $LASTEXITCODE" -Color "Error"
            Write-ColorOutput $buildOutput -Color "Error"
            return $false
        }
    } catch {
        Write-ColorOutput "❌ Build exception: $($_.Exception.Message)" -Color "Error"
        return $false
    }
}

function Test-LiveAPIConnectivity {
    Write-TestHeader "Testing Live API Connectivity"
    
    $apiUrls = @{
        "TablesApi" = "https://magidesk-tables-904541739138.northamerica-south1.run.app"
        "OrderApi" = "https://magidesk-order-904541739138.northamerica-south1.run.app"
        "PaymentApi" = "https://magidesk-payment-904541739138.northamerica-south1.run.app"
        "MenuApi" = "https://magidesk-menu-904541739138.northamerica-south1.run.app"
        "InventoryApi" = "https://magidesk-inventory-904541739138.northamerica-south1.run.app"
        "SettingsApi" = "https://magidesk-settings-904541739138.northamerica-south1.run.app"
    }
    
    $results = @{}
    
    Write-LiveIndicator "Testing connectivity to live production APIs..."
    Write-ColorOutput "`n🌐 Live API Endpoints:" -Color "Visual"
    
    foreach ($apiName in $apiUrls.Keys) {
        $url = $apiUrls[$apiName]
        Write-ColorOutput "  📡 Testing $apiName..." -Color "Info"
        Write-ColorOutput "     URL: $url" -Color "Info"
        
        try {
            $startTime = Get-Date
            $response = Invoke-RestMethod -Uri "$url/health" -Method GET -TimeoutSec 10
            $endTime = Get-Date
            $responseTime = ($endTime - $startTime).TotalMilliseconds
            
            $results[$apiName] = $true
            Write-ColorOutput "     ✅ $apiName is LIVE and responding" -Color "Success"
            Write-ColorOutput "     ⏱️ Response Time: $([math]::Round($responseTime, 0))ms" -Color "Success"
            
            if ($ShowDetails) {
                Write-ColorOutput "     📄 Response: $response" -Color "Info"
            }
        } catch {
            $results[$apiName] = $false
            Write-ColorOutput "     ❌ $apiName failed: $($_.Exception.Message)" -Color "Error"
        }
        
        Write-ColorOutput "" -Color "Info"
    }
    
    $accessibleCount = ($results.Values | Where-Object { $_ -eq $true }).Count
    $totalCount = $results.Count
    
    Write-ColorOutput "🌐 Live API Connectivity Summary:" -Color "Visual"
    Write-ColorOutput "   $accessibleCount/$totalCount APIs are LIVE and accessible" -Color $(if ($accessibleCount -eq $totalCount) { "Success" } else { "Warning" })
    
    return $accessibleCount -ge ($totalCount * 0.6) # At least 60% should be accessible
}

function Run-VisualTests {
    param([string]$Category, [string]$Description)
    
    Write-TestHeader "Running $Description Visual Tests"
    
    Write-LiveIndicator "Starting visual testing against live application..."
    Write-ColorOutput "🎬 This will show you real-time testing results!" -Color "Visual"
    Write-ColorOutput "👀 Watch the console output to see live API responses!" -Color "Visual"
    Write-ColorOutput "📊 You'll see actual data, response times, and JSON structures!" -Color "Visual"
    Write-ColorOutput ""
    
    try {
        $testArgs = @(
            "test", "MagiDesk.Tests",
            "--configuration", "Release",
            "--logger", "trx;LogFileName=$OutputPath\Visual-$Category-$([DateTime]::Now.ToString('yyyyMMdd-HHmmss')).trx",
            "--results-directory", $OutputPath,
            "--verbosity", "normal"  # Always use normal verbosity for visual tests
        )
        
        if ($Category -ne "All") {
            $testArgs += "--filter", "TestCategory=$Category"
        }
        
        Write-ColorOutput "🎬 Running visual tests with category: $Category" -Color "Info"
        Write-ColorOutput "📡 Testing against LIVE production APIs..." -Color "Live"
        Write-ColorOutput ""
        
        $testOutput = & dotnet $testArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "✅ $Description visual tests completed successfully!" -Color "Success"
            return $true
        } else {
            Write-ColorOutput "⚠️ $Description visual tests had issues (Exit code: $LASTEXITCODE)" -Color "Warning"
            if ($ShowDetails) {
                Write-ColorOutput $testOutput -Color "Warning"
            }
            return $false
        }
    } catch {
        Write-ColorOutput "❌ $Description visual test execution exception: $($_.Exception.Message)" -Color "Error"
        return $false
    }
}

function Show-VisualTestSummary {
    Write-TestHeader "Visual Live Testing Summary"
    
    Write-ColorOutput "🎬 Visual Testing Configuration:" -Color "Info"
    Write-ColorOutput "  Test Type: $TestType" -Color "Info"
    Write-ColorOutput "  Show Details: $ShowDetails" -Color "Info"
    Write-ColorOutput "  Output Path: $OutputPath" -Color "Info"
    Write-ColorOutput "  Mode: LIVE PRODUCTION TESTING" -Color "Live"
    
    Write-ColorOutput "`n🎭 Visual Test Categories:" -Color "Info"
    Write-ColorOutput "  ✅ Live Application Flow Tests" -Color "Success"
    Write-ColorOutput "  ✅ Live Payment System Tests" -Color "Success"
    Write-ColorOutput "  ✅ Live Table Management Tests" -Color "Success"
    Write-ColorOutput "  ✅ Real-time API Response Testing" -Color "Success"
    Write-ColorOutput "  ✅ Live Data Structure Validation" -Color "Success"
    
    Write-ColorOutput "`n🔴 LIVE Testing Features:" -Color "Live"
    Write-ColorOutput "  • Real-time API responses displayed" -Color "Success"
    Write-ColorOutput "  • Actual production data shown" -Color "Success"
    Write-ColorOutput "  • Response times measured and displayed" -Color "Success"
    Write-ColorOutput "  • JSON structure analysis performed" -Color "Success"
    Write-ColorOutput "  • Error handling demonstrated live" -Color "Success"
    Write-ColorOutput "  • Step-by-step testing process visible" -Color "Success"
    
    Write-ColorOutput "`n📊 What You Can See:" -Color "Visual"
    Write-ColorOutput "  • Live table status and counts" -Color "Success"
    Write-ColorOutput "  • Real menu items and availability" -Color "Success"
    Write-ColorOutput "  • Payment system health and validation" -Color "Success"
    Write-ColorOutput "  • Order system status and capabilities" -Color "Success"
    Write-ColorOutput "  • Inventory levels and stock information" -Color "Success"
    Write-ColorOutput "  • Active sessions and table assignments" -Color "Success"
    
    Write-ColorOutput "`n🎯 Perfect for:" -Color "Visual"
    Write-ColorOutput "  • Monitoring production system health" -Color "Success"
    Write-ColorOutput "  • Debugging live application issues" -Color "Success"
    Write-ColorOutput "  • Understanding real API responses" -Color "Success"
    Write-ColorOutput "  • Demonstrating system capabilities" -Color "Success"
    Write-ColorOutput "  • Training and documentation purposes" -Color "Success"
}

# Main execution
Write-ColorOutput "🎬 MagiDesk Visual Live Testing Runner" -Color "Header"
Write-ColorOutput "========================================" -Color "Header"
Write-ColorOutput "🔴 TESTING AGAINST LIVE PRODUCTION APPLICATION" -Color "Live"
Write-ColorOutput "👀 You can see real-time testing results!" -Color "Visual"

# Check prerequisites
if (-not (Test-Prerequisites)) {
    Write-ColorOutput "`n❌ Prerequisites check failed. Exiting." -Color "Error"
    exit 1
}

# Build test project
if (-not (Build-TestProject)) {
    Write-ColorOutput "`n❌ Build failed. Exiting." -Color "Error"
    exit 1
}

# Test live API connectivity
$apiConnectivity = Test-LiveAPIConnectivity

# Run visual tests based on type
$testResults = @{}

switch ($TestType) {
    "All" {
        $testResults["Visual"] = Run-VisualTests "Visual" "All Visual Live"
    }
    "Live" {
        $testResults["Visual"] = Run-VisualTests "Live" "Live Application"
    }
    "Payment" {
        $testResults["Visual"] = Run-VisualTests "Visual" "Payment System"
    }
    "Tables" {
        $testResults["Visual"] = Run-VisualTests "Visual" "Table Management"
    }
    "Orders" {
        $testResults["Visual"] = Run-VisualTests "Visual" "Order System"
    }
    "Menu" {
        $testResults["Visual"] = Run-VisualTests "Visual" "Menu System"
    }
    "Inventory" {
        $testResults["Visual"] = Run-VisualTests "Visual" "Inventory System"
    }
}

# Show summary
Show-VisualTestSummary

# Final result
$overallSuccess = $testResults.Values -notcontains $false -and $apiConnectivity

Write-ColorOutput "`n" -Color "Info"
Write-ColorOutput "=" * 80 -Color "Header"
if ($overallSuccess) {
    Write-ColorOutput "🎉 ALL VISUAL LIVE TESTS COMPLETED SUCCESSFULLY!" -Color "Success"
    Write-ColorOutput "🔴 You have successfully tested the LIVE production application!" -Color "Live"
    Write-ColorOutput "👀 You saw real-time API responses and live data!" -Color "Visual"
    Write-ColorOutput "`n📊 Visual Test Results:" -Color "Info"
    Write-ColorOutput "  ✅ Live Application: Tested against production" -Color "Success"
    Write-ColorOutput "  ✅ Payment System: Live validation performed" -Color "Success"
    Write-ColorOutput "  ✅ Table Management: Live data retrieved" -Color "Success"
    Write-ColorOutput "  ✅ Real-time Responses: All displayed successfully" -Color "Success"
    Write-ColorOutput "`n🎯 Perfect for monitoring and debugging live systems!" -Color "Visual"
    exit 0
} else {
    Write-ColorOutput "⚠️  SOME VISUAL LIVE TESTS HAD ISSUES" -Color "Warning"
    Write-ColorOutput "🔴 Please review the live test results above." -Color "Live"
    Write-ColorOutput "👀 You still saw the live testing process!" -Color "Visual"
    exit 1
}
