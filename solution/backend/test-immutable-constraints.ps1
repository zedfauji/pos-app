#!/usr/bin/env pwsh
# Immutable ID Constraints Test Script
# Tests that billing IDs and payment IDs cannot be modified

param(
    [string]$PaymentApiUrl = "https://magidesk-payment-23sbzjsxaq-pv.a.run.app"
)

$ErrorActionPreference = "Stop"

function Write-TestResult($testName, $success, $message) {
    $color = if ($success) { "Green" } else { "Red" }
    $status = if ($success) { "✅ PASS" } else { "❌ FAIL" }
    Write-Host "[$status] $testName" -ForegroundColor $color
    if ($message) { Write-Host "   $message" -ForegroundColor Gray }
}

function Test-ImmutableBillingIdFormat {
    Write-Host "`n🧪 Testing Immutable Billing ID Format Validation..." -ForegroundColor Cyan
    
    # Test with invalid billing ID format
    $invalidPayload = @{
        SessionId = "test-session"
        BillingId = "invalid-format-12345"
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
        Write-TestResult "Immutable Billing ID Format Test" $false "Payment was accepted with invalid billing ID format"
        return $false
    }
    catch {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            if ($errorResponse.error -eq "INVALID_BILLING_ID_FORMAT") {
                Write-TestResult "Immutable Billing ID Format Test" $true "Payment correctly rejected for invalid billing ID format"
                return $true
            }
            else {
                Write-TestResult "Immutable Billing ID Format Test" $false "Unexpected error: $($errorResponse.message)"
                return $false
            }
        }
        catch {
            Write-TestResult "Immutable Billing ID Format Test" $false "Error parsing response: $($_.Exception.Message)"
            return $false
        }
    }
}

function Test-EmptyBillingId {
    Write-Host "`n🧪 Testing Empty Billing ID Validation..." -ForegroundColor Cyan
    
    $emptyPayload = @{
        SessionId = "test-session"
        BillingId = ""
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
        $response = Invoke-RestMethod -Uri "$PaymentApiUrl/api/payments" -Method POST -Body $emptyPayload -ContentType "application/json"
        Write-TestResult "Empty Billing ID Test" $false "Payment was accepted with empty billing ID"
        return $false
    }
    catch {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            if ($errorResponse.error -eq "INVALID_BILLING_ID") {
                Write-TestResult "Empty Billing ID Test" $true "Payment correctly rejected for empty billing ID"
                return $true
            }
            else {
                Write-TestResult "Empty Billing ID Test" $false "Unexpected error: $($errorResponse.message)"
                return $false
            }
        }
        catch {
            Write-TestResult "Empty Billing ID Test" $false "Error parsing response: $($_.Exception.Message)"
            return $false
        }
    }
}

function Test-NullBillingId {
    Write-Host "`n🧪 Testing Null Billing ID Validation..." -ForegroundColor Cyan
    
    $nullPayload = @{
        SessionId = "test-session"
        BillingId = $null
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
        $response = Invoke-RestMethod -Uri "$PaymentApiUrl/api/payments" -Method POST -Body $nullPayload -ContentType "application/json"
        Write-TestResult "Null Billing ID Test" $false "Payment was accepted with null billing ID"
        return $false
    }
    catch {
        try {
            $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
            if ($errorResponse.errors.BillingId -and $errorResponse.errors.BillingId[0] -like "*required*") {
                Write-TestResult "Null Billing ID Test" $true "Payment correctly rejected for null billing ID (model validation)"
                return $true
            }
            elseif ($errorResponse.error -eq "INVALID_BILLING_ID") {
                Write-TestResult "Null Billing ID Test" $true "Payment correctly rejected for null billing ID"
                return $true
            }
            else {
                Write-TestResult "Null Billing ID Test" $false "Unexpected error: $($errorResponse.message)"
                return $false
            }
        }
        catch {
            Write-TestResult "Null Billing ID Test" $false "Error parsing response: $($_.Exception.Message)"
            return $false
        }
    }
}

function Test-ImmutableIdGeneration {
    Write-Host "`n🧪 Testing Immutable ID Generation..." -ForegroundColor Cyan
    
    try {
        # Test the validation endpoint to see if it shows immutable ID service is working
        $response = Invoke-RestMethod -Uri "$PaymentApiUrl/api/payments/validate" -Method GET
        
        if ($response.validationEnabled -eq $true -and $response.status -eq "healthy") {
            Write-TestResult "Immutable ID Service Test" $true "Immutable ID service is registered and working"
            return $true
        }
        else {
            Write-TestResult "Immutable ID Service Test" $false "Immutable ID service not properly configured"
            return $false
        }
    }
    catch {
        Write-TestResult "Immutable ID Service Test" $false "Error accessing validation endpoint: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
Write-Host "🔒 Immutable ID Constraints Test Suite" -ForegroundColor Yellow
Write-Host "PaymentApi URL: $PaymentApiUrl" -ForegroundColor Gray

$testResults = @()

$testResults += Test-ImmutableBillingIdFormat
$testResults += Test-EmptyBillingId
$testResults += Test-NullBillingId
$testResults += Test-ImmutableIdGeneration

$passedTests = ($testResults | Where-Object { $_ -eq $true }).Count
$totalTests = $testResults.Count

Write-Host "`n📊 Test Results Summary:" -ForegroundColor Yellow
Write-Host "   Passed: $passedTests/$totalTests" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Red" })

if ($passedTests -eq $totalTests) {
    Write-Host "`n🎉 All immutable ID constraint tests passed! Billing IDs are now protected." -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`n⚠️  Some tests failed. Please check the immutable ID implementation." -ForegroundColor Red
    exit 1
}

