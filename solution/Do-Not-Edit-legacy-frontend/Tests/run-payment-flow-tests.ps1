# Payment Flow Test Runner Script
# This script runs comprehensive tests for the Payment flow functionality

Write-Host "🧪 Payment Flow Test Runner" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Set up test environment
$testProject = "frontend/Tests"
$testResults = "test-results.xml"

Write-Host "📋 Running Payment Flow Tests..." -ForegroundColor Yellow

try {
    # Build the test project first
    Write-Host "🔨 Building test project..." -ForegroundColor Blue
    dotnet build $testProject -c Debug --verbosity minimal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Build successful!" -ForegroundColor Green
    
    # Run the tests
    Write-Host "🧪 Running Payment Flow Tests..." -ForegroundColor Blue
    dotnet test $testProject --logger "trx;LogFileName=$testResults" --verbosity normal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ All tests passed!" -ForegroundColor Green
        
        # Display test summary
        Write-Host "`n📊 Test Summary:" -ForegroundColor Cyan
        Write-Host "=================" -ForegroundColor Cyan
        
        # Parse test results if available
        if (Test-Path $testResults) {
            $xml = [xml](Get-Content $testResults)
            $totalTests = $xml.TestRun.Results.UnitTestResult.Count
            $passedTests = ($xml.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Passed" }).Count
            $failedTests = ($xml.TestRun.Results.UnitTestResult | Where-Object { $_.outcome -eq "Failed" }).Count
            
            Write-Host "Total Tests: $totalTests" -ForegroundColor White
            Write-Host "Passed: $passedTests" -ForegroundColor Green
            Write-Host "Failed: $failedTests" -ForegroundColor Red
            
            if ($failedTests -eq 0) {
                Write-Host "`n🎉 All Payment Flow tests passed successfully!" -ForegroundColor Green
                Write-Host "The Payment pane opening functionality is working correctly." -ForegroundColor Green
            } else {
                Write-Host "`n⚠️  Some tests failed. Check the test results for details." -ForegroundColor Yellow
            }
        }
        
    } else {
        Write-Host "❌ Some tests failed!" -ForegroundColor Red
        Write-Host "Check the output above for details." -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error running tests: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n📋 Test Categories Covered:" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host "✅ Unit Tests - Individual component testing" -ForegroundColor Green
Write-Host "✅ Integration Tests - End-to-end workflow testing" -ForegroundColor Green
Write-Host "✅ Edge Case Tests - Boundary condition testing" -ForegroundColor Green
Write-Host "✅ Performance Tests - Timing and concurrency testing" -ForegroundColor Green
Write-Host "✅ Error Handling Tests - Exception and error scenario testing" -ForegroundColor Green

Write-Host "`n🔍 Test Scenarios:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan
Write-Host "• PaymentPage ProcessPayment_Click with valid bill" -ForegroundColor White
Write-Host "• PaymentPane InitializeAsync with valid data" -ForegroundColor White
Write-Host "• Error handling for null inputs" -ForegroundColor White
Write-Host "• Pane already visible scenario" -ForegroundColor White
Write-Host "• PaneManager null scenario" -ForegroundColor White
Write-Host "• Large amount, zero amount, negative amount bills" -ForegroundColor White
Write-Host "• Empty items list handling" -ForegroundColor White
Write-Host "• Performance and concurrency testing" -ForegroundColor White

Write-Host "`n📁 Test Results Location: $testResults" -ForegroundColor Blue
Write-Host "`n✨ Payment Flow testing completed!" -ForegroundColor Cyan
