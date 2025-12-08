 #!/usr/bin/env pwsh
# PaymentApi Billing ID Validation Test Script
# Tests both valid and invalid billing ID scenarios

param(
    [string]$PaymentApiUrl = "https://magidesk-payment-23sbzjsxaq-pv.a.run.app",
    [string]$TablesApiUrl = "https://magidesk-tables-904541739138.northamerica-south1.run.app"
)

$ErrorActionPreference = "Stop"

function Write-TestResult($testName, $success, $message) {
    $color = if ($success) { "Green" } else { "Red" }
    $status = if ($success) { "✅ PASS" } else { "❌ FAIL" }
    Write-Host "[$status] $testName" -ForegroundColor $color
    if ($message) { Write-Host "   $message" -ForegroundColor Gray }
}

function Test-InvalidBillingId {
    Write-Host "`n🧪 Testing Invalid Billing ID Validation..." -ForegroundColor Cyan
    
    $invalidPayload = @{
        SessionId = "test-session"
        BillingId = "invalid-billing-id-12345"
        TotalDue = 100.00
        Lines = @(
            @{
                AmountPaid = 50.00
                PaymentMethod = "cash"
                DiscountAmount = 0.00
                DiscountReason = $null
                TipAmount = 0.00
                ExternalRef = $null
                Meta = $null
            }
        )
        ServerId = "test-server"
    } | ConvertTo-Json -Depth 3
    
    try {
        $response = Invoke-RestMethod -Uri "$PaymentApiUrl/api/payments" -Method POST -Body $invalidPayload -ContentType "application/json"
        Write-TestResult "Invalid Billing ID Test" $false "Payment was accepted when it should have been rejected"
        return $false
    }
    catch {
        $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
        if ($errorResponse.error -eq "VALIDATION_ERROR" -and $errorResponse.message -like "*Cannot verify billing ID*") {
            Write-TestResult "Invalid Billing ID Test" $true "Payment correctly rejected with validation error"
            return $true
        }
        elseif ($errorResponse.error -eq "INVALID_BILLING_ID_FORMAT") {
            Write-TestResult "Invalid Billing ID Test" $true "Payment correctly rejected with invalid billing ID format"
            return $true
        }
        else {
            Write-TestResult "Invalid Billing ID Test" $false "Unexpected error: $($errorResponse.message)"
            return $false
        }
    }
}

function Test-ValidBillingId {
    Write-Host "`n🧪 Testing Valid Billing ID Validation..." -ForegroundColor Cyan
    
    # First, get an active session with a valid billing ID
    try {
        $activeSessions = Invoke-RestMethod -Uri "$TablesApiUrl/sessions/active" -Method GET
        if ($activeSessions.Count -eq 0) {
            Write-TestResult "Valid Billing ID Test" $false "No active sessions available for testing"
            return $false
        }
        
        $validBillingId = $activeSessions[0].billingId
        Write-Host "   Using billing ID: $validBillingId" -ForegroundColor Gray
        
        $validPayload = @{
            SessionId = "test-session"
            BillingId = $validBillingId
            TotalDue = 100.00
            Lines = @(
                @{
                    AmountPaid = 50.00
                    PaymentMethod = "cash"
                    DiscountAmount = 0.00
                    DiscountReason = $null
                    TipAmount = 0.00
                    ExternalRef = $null
                    Meta = $null
                }
            )
            ServerId = "test-server"
        } | ConvertTo-Json -Depth 3
        
        $response = Invoke-RestMethod -Uri "$PaymentApiUrl/api/payments" -Method POST -Body $validPayload -ContentType "application/json"
        
        if ($response.billingId -eq $validBillingId -and ($response.status -eq "partial" -or $response.status -eq "paid")) {
            Write-TestResult "Valid Billing ID Test" $true "Payment correctly processed for valid billing ID (status: $($response.status))"
            return $true
        }
        else {
            Write-TestResult "Valid Billing ID Test" $false "Unexpected response format: $($response | ConvertTo-Json)"
            return $false
        }
    }
    catch {
        Write-TestResult "Valid Billing ID Test" $false "Error processing valid billing ID: $($_.Exception.Message)"
        return $false
    }
}

function Test-ValidationEndpoint {
    Write-Host "`n🧪 Testing Validation Endpoint..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$PaymentApiUrl/api/payments/validate" -Method GET
        
        if ($response.validationEnabled -eq $true -and $response.status -eq "healthy") {
            Write-TestResult "Validation Endpoint Test" $true "Validation endpoint working correctly"
            Write-Host "   TablesApi URL: $($response.tablesApiUrl)" -ForegroundColor Gray
            return $true
        }
        else {
            Write-TestResult "Validation Endpoint Test" $false "Validation endpoint returned unexpected data"
            return $false
        }
    }
    catch {
        Write-TestResult "Validation Endpoint Test" $false "Error accessing validation endpoint: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
Write-Host "🚀 PaymentApi Billing ID Validation Test Suite" -ForegroundColor Yellow
Write-Host "PaymentApi URL: $PaymentApiUrl" -ForegroundColor Gray
Write-Host "TablesApi URL: $TablesApiUrl" -ForegroundColor Gray

$testResults = @()

$testResults += Test-InvalidBillingId
$testResults += Test-ValidBillingId  
$testResults += Test-ValidationEndpoint

$passedTests = ($testResults | Where-Object { $_ -eq $true }).Count
$totalTests = $testResults.Count

Write-Host "`n📊 Test Results Summary:" -ForegroundColor Yellow
Write-Host "   Passed: $passedTests/$totalTests" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Red" })

if ($passedTests -eq $totalTests) {
    Write-Host "`n🎉 All tests passed! Billing ID validation is working correctly." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`n⚠️  Some tests failed. Please check the implementation." -ForegroundColor Red
    exit 1
}
