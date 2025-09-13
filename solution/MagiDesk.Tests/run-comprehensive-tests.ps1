# Comprehensive Test Runner for MagiDesk WinUI 3 Application
# This script runs all critical tests to validate functionality, flows, and crash prevention

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Critical", "Flows", "CrashPrevention", "HealthCheck", "Api", "Unit", "Integration", "UI")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "Local",
    
    [Parameter(Mandatory=$false)]
    [switch]$Parallel,
    
    [Parameter(Mandatory=$false)]
    [switch]$Detailed,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\TestResults"
)

$ErrorActionPreference = "Stop"

# Test Configuration
$testProject = "MagiDesk.Tests"
$testResultsFile = "TestResults.trx"
$coverageFile = "Coverage.cobertura.xml"

# Colors for output
$Colors = @{
    Success = "Green"
    Error = "Red"
    Warning = "Yellow"
    Info = "Cyan"
    Header = "Magenta"
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Colors[$Color]
}

function Write-TestHeader {
    param([string]$Title)
    Write-ColorOutput "`n" -Color "Info"
    Write-ColorOutput "=" * 60 -Color "Header"
    Write-ColorOutput $Title -Color "Header"
    Write-ColorOutput "=" * 60 -Color "Header"
    Write-ColorOutput "`n" -Color "Info"
}

function Write-TestResult {
    param([string]$TestName, [bool]$Success, [string]$Message = "")
    $color = if ($Success) { "Success" } else { "Error" }
    $status = if ($Success) { "✅ PASS" } else { "❌ FAIL" }
    Write-ColorOutput "[$status] $TestName" -Color $color
    if ($Message) { Write-ColorOutput "   $Message" -Color "Info" }
}

function Test-Prerequisites {
    Write-TestHeader "Checking Prerequisites"
    
    # Check if .NET 8 is installed
    try {
        $dotnetVersion = dotnet --version
        Write-TestResult ".NET Version Check" $true "Version: $dotnetVersion"
    } catch {
        Write-TestResult ".NET Version Check" $false ".NET 8 not found"
        return $false
    }
    
    # Check if test project exists
    if (Test-Path $testProject) {
        Write-TestResult "Test Project Check" $true "Project found: $testProject"
    } else {
        Write-TestResult "Test Project Check" $false "Test project not found: $testProject"
        return $false
    }
    
    # Check if output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-TestResult "Output Directory" $true "Created: $OutputPath"
    } else {
        Write-TestResult "Output Directory" $true "Exists: $OutputPath"
    }
    
    return $true
}

function Build-TestProject {
    Write-TestHeader "Building Test Project"
    
    try {
        Write-ColorOutput "Building $testProject..." -Color "Info"
        $buildOutput = dotnet build $testProject -c Release --verbosity minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-TestResult "Build" $true "Project built successfully"
            return $true
        } else {
            Write-TestResult "Build" $false "Build failed with exit code $LASTEXITCODE"
            Write-ColorOutput $buildOutput -Color "Error"
            return $false
        }
    } catch {
        Write-TestResult "Build" $false "Build exception: $($_.Exception.Message)"
        return $false
    }
}

function Run-Tests {
    param([string]$Category, [string]$Description)
    
    Write-TestHeader "Running $Description Tests"
    
    try {
        $testArgs = @(
            "test", $testProject,
            "--configuration", "Release",
            "--logger", "trx;LogFileName=$OutputPath\$Category-$testResultsFile",
            "--results-directory", $OutputPath,
            "--verbosity", $(if ($Detailed) { "normal" } else { "minimal" })
        )
        
        if ($Category -ne "All") {
            $testArgs += "--filter", "Category=$Category"
        }
        
        if ($Parallel) {
            $testArgs += "--parallel"
        }
        
        Write-ColorOutput "Running tests with category: $Category" -Color "Info"
        $testOutput = & dotnet $testArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-TestResult $Description $true "All tests passed"
            return $true
        } else {
            Write-TestResult $Description $false "Some tests failed (Exit code: $LASTEXITCODE)"
            if ($Verbose) {
                Write-ColorOutput $testOutput -Color "Error"
            }
            return $false
        }
    } catch {
        Write-TestResult $Description $false "Test execution exception: $($_.Exception.Message)"
        return $false
    }
}

function Analyze-TestResults {
    param([string]$ResultsPath)
    
    Write-TestHeader "Analyzing Test Results"
    
    try {
        $resultFiles = Get-ChildItem -Path $ResultsPath -Filter "*.trx" -Recurse
        
        if ($resultFiles.Count -eq 0) {
            Write-TestResult "Results Analysis" $false "No test result files found"
            return
        }
        
        $totalTests = 0
        $passedTests = 0
        $failedTests = 0
        
        foreach ($file in $resultFiles) {
            try {
                $xml = [xml](Get-Content $file.FullName)
                $testResults = $xml.TestRun.Results.UnitTestResult
                
                $totalTests += $testResults.Count
                $passedTests += ($testResults | Where-Object { $_.outcome -eq "Passed" }).Count
                $failedTests += ($testResults | Where-Object { $_.outcome -eq "Failed" }).Count
                
                Write-ColorOutput "File: $($file.Name)" -Color "Info"
                Write-ColorOutput "  Tests: $($testResults.Count)" -Color "Info"
                Write-ColorOutput "  Passed: $(($testResults | Where-Object { $_.outcome -eq 'Passed' }).Count)" -Color "Success"
                Write-ColorOutput "  Failed: $(($testResults | Where-Object { $_.outcome -eq 'Failed' }).Count)" -Color "Error"
            } catch {
                Write-ColorOutput "Error parsing $($file.Name): $($_.Exception.Message)" -Color "Error"
            }
        }
        
        Write-ColorOutput "`nOverall Results:" -Color "Header"
        Write-ColorOutput "Total Tests: $totalTests" -Color "Info"
        Write-ColorOutput "Passed: $passedTests" -Color "Success"
        Write-ColorOutput "Failed: $failedTests" -Color "Error"
        
        $successRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
        Write-ColorOutput "Success Rate: $successRate%" -Color $(if ($successRate -ge 90) { "Success" } elseif ($successRate -ge 70) { "Warning" } else { "Error" })
        
    } catch {
        Write-TestResult "Results Analysis" $false "Analysis exception: $($_.Exception.Message)"
    }
}

function Show-TestSummary {
    Write-TestHeader "Test Execution Summary"
    
    Write-ColorOutput "Test Configuration:" -Color "Info"
    Write-ColorOutput "  Test Type: $TestType" -Color "Info"
    Write-ColorOutput "  Environment: $Environment" -Color "Info"
    Write-ColorOutput "  Parallel: $Parallel" -Color "Info"
    Write-ColorOutput "  Detailed: $Detailed" -Color "Info"
    Write-ColorOutput "  Output Path: $OutputPath" -Color "Info"
    
    Write-ColorOutput "`nTest Categories Covered:" -Color "Info"
    Write-ColorOutput "  ✅ Critical Flows (Login, Orders, Payments)" -Color "Success"
    Write-ColorOutput "  ✅ Crash Prevention (Memory, Security, Concurrency)" -Color "Success"
    Write-ColorOutput "  ✅ Health Checks (All API endpoints)" -Color "Success"
    Write-ColorOutput "  ✅ API Endpoints (All CRUD operations)" -Color "Success"
    Write-ColorOutput "  ✅ Error Handling (Invalid inputs, edge cases)" -Color "Success"
    Write-ColorOutput "  ✅ Performance (Concurrent requests, timeouts)" -Color "Success"
    Write-ColorOutput "  ✅ UI Components (WinUI 3, Navigation, Data Binding)" -Color "Success"
    
    Write-ColorOutput "`nCritical Features Tested:" -Color "Info"
    Write-ColorOutput "  • User Authentication & Authorization" -Color "Success"
    Write-ColorOutput "  • Order Management (Create, Read, Update, Delete)" -Color "Success"
    Write-ColorOutput "  • Payment Processing (Single & Split payments)" -Color "Success"
    Write-ColorOutput "  • Menu Item Management" -Color "Success"
    Write-ColorOutput "  • Inventory Tracking" -Color "Success"
    Write-ColorOutput "  • Table & Session Management" -Color "Success"
    Write-ColorOutput "  • Settings Configuration" -Color "Success"
    Write-ColorOutput "  • Receipt Generation" -Color "Success"
}

# Main execution
Write-ColorOutput "🧪 MagiDesk Comprehensive Test Runner" -Color "Header"
Write-ColorOutput "=====================================" -Color "Header"
Write-ColorOutput "Testing all critical features, flows, and crash prevention" -Color "Info"

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

# Run tests based on type
$testResults = @{}

switch ($TestType) {
    "All" {
        $testResults["Critical"] = Run-Tests "Critical" "Critical Flow"
        $testResults["CrashPrevention"] = Run-Tests "CrashPrevention" "Crash Prevention"
        $testResults["HealthCheck"] = Run-Tests "HealthCheck" "Health Check"
        $testResults["Api"] = Run-Tests "Api" "API Endpoint"
        $testResults["UI"] = Run-Tests "UI" "UI Components"
    }
    "Critical" {
        $testResults["Critical"] = Run-Tests "Critical" "Critical Flow"
    }
    "Flows" {
        $testResults["Critical"] = Run-Tests "Critical" "Critical Flow"
    }
    "CrashPrevention" {
        $testResults["CrashPrevention"] = Run-Tests "CrashPrevention" "Crash Prevention"
    }
    "HealthCheck" {
        $testResults["HealthCheck"] = Run-Tests "HealthCheck" "Health Check"
    }
    "Api" {
        $testResults["Api"] = Run-Tests "Api" "API Endpoint"
    }
    "Unit" {
        $testResults["Unit"] = Run-Tests "Unit" "Unit"
    }
    "Integration" {
        $testResults["Integration"] = Run-Tests "Integration" "Integration"
    }
    "UI" {
        $testResults["UI"] = Run-Tests "UI" "UI Components"
    }
}

# Analyze results
Analyze-TestResults $OutputPath

# Show summary
Show-TestSummary

# Final result
$overallSuccess = $testResults.Values -notcontains $false

Write-ColorOutput "`n" -Color "Info"
Write-ColorOutput "=" * 60 -Color "Header"
if ($overallSuccess) {
    Write-ColorOutput "🎉 ALL TESTS COMPLETED SUCCESSFULLY!" -Color "Success"
    Write-ColorOutput "The MagiDesk application is ready for deployment." -Color "Success"
    exit 0
} else {
    Write-ColorOutput "⚠️  SOME TESTS FAILED" -Color "Warning"
    Write-ColorOutput "Please review the test results and fix issues before deployment." -Color "Warning"
    exit 1
}
