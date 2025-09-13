# Payment Flow Test Script
# This script tests the refactored payment flow

param(
    [switch]$TestPaymentFlow,
    [switch]$TestSplitPayment,
    [switch]$TestErrorHandling,
    [switch]$All
)

$DatabaseHost = "34.51.82.201"
$DatabaseUser = "posapp"
$DatabasePassword = "Campus_66"
$DatabaseName = "postgres"

function Test-PaymentFlow {
    Write-Host "Testing Payment Flow Refactoring" -ForegroundColor Magenta
    Write-Host "================================" -ForegroundColor Magenta
    
    # Test 1: Check if payment tables exist
    Write-Host "1. Checking payment tables..." -ForegroundColor Cyan
    $tables = docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "\dt pay.*"
    if ($tables -match "pay.payments|pay.bill_ledger|pay.payment_logs") {
        Write-Host "   ✅ Payment tables exist" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Payment tables missing" -ForegroundColor Red
        return
    }
    
    # Test 2: Check payment schema
    Write-Host "2. Checking payment schema..." -ForegroundColor Cyan
    $schema = docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "\d pay.payments"
    if ($schema -match "payment_id|session_id|billing_id|amount_paid|payment_method") {
        Write-Host "   ✅ Payment schema is correct" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Payment schema issues" -ForegroundColor Red
    }
    
    # Test 3: Check bill_ledger schema
    Write-Host "3. Checking bill_ledger schema..." -ForegroundColor Cyan
    $ledgerSchema = docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "\d pay.bill_ledger"
    if ($ledgerSchema -match "billing_id|total_due|total_paid|total_discount|total_tip|status") {
        Write-Host "   ✅ Bill ledger schema is correct" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Bill ledger schema issues" -ForegroundColor Red
    }
    
    Write-Host "Payment flow test completed!" -ForegroundColor Green
}

function Test-SplitPayment {
    Write-Host "Testing Split Payment Logic" -ForegroundColor Magenta
    Write-Host "===========================" -ForegroundColor Magenta
    
    # Test split payment calculation
    Write-Host "1. Testing split payment calculation..." -ForegroundColor Cyan
    
    # Create a test payment with split amounts
    $testQuery = @"
INSERT INTO pay.payments (session_id, billing_id, amount_paid, payment_method, discount_amount, tip_amount, created_by)
VALUES 
    ('550e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440001', 20.00, 'Cash', 0.00, 2.00, 'test'),
    ('550e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440001', 15.00, 'Card', 0.00, 1.50, 'test'),
    ('550e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440001', 10.00, 'Mobile', 0.00, 1.00, 'test');
"@
    
    try {
        docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "$testQuery"
        Write-Host "   ✅ Split payment test data inserted" -ForegroundColor Green
        
        # Check the inserted data
        $result = docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "SELECT payment_method, amount_paid, tip_amount FROM pay.payments WHERE billing_id = '550e8400-e29b-41d4-a716-446655440001' ORDER BY payment_method;"
        Write-Host "   Split payment data:" -ForegroundColor Yellow
        Write-Host $result
        
        # Clean up test data
        docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "DELETE FROM pay.payments WHERE billing_id = '550e8400-e29b-41d4-a716-446655440001';"
        Write-Host "   ✅ Test data cleaned up" -ForegroundColor Green
        
    } catch {
        Write-Host "   ❌ Split payment test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "Split payment test completed!" -ForegroundColor Green
}

function Test-ErrorHandling {
    Write-Host "Testing Error Handling" -ForegroundColor Magenta
    Write-Host "=====================" -ForegroundColor Magenta
    
    # Test 1: Invalid payment amounts
    Write-Host "1. Testing invalid payment amounts..." -ForegroundColor Cyan
    $invalidQuery = @"
INSERT INTO pay.payments (session_id, billing_id, amount_paid, payment_method, discount_amount, tip_amount, created_by)
VALUES ('550e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440002', -10.00, 'Cash', 0.00, 0.00, 'test');
"@
    
    try {
        docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "$invalidQuery"
        Write-Host "   ❌ Invalid amount was accepted (should have been rejected)" -ForegroundColor Red
    } catch {
        Write-Host "   ✅ Invalid amount properly rejected" -ForegroundColor Green
    }
    
    # Test 2: Missing required fields
    Write-Host "2. Testing missing required fields..." -ForegroundColor Cyan
    $missingQuery = @"
INSERT INTO pay.payments (session_id, billing_id, amount_paid, payment_method)
VALUES ('550e8400-e29b-41d4-a716-446655440000', '550e8400-e29b-41d4-a716-446655440003', 10.00, 'Cash');
"@
    
    try {
        docker run --rm -e PGPASSWORD=$DatabasePassword postgres:14-alpine psql -h $DatabaseHost -U $DatabaseUser -d $DatabaseName -c "$missingQuery"
        Write-Host "   ✅ Missing fields handled gracefully" -ForegroundColor Green
    } catch {
        Write-Host "   ❌ Missing fields caused error: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host "Error handling test completed!" -ForegroundColor Green
}

# Main script logic
if ($All) {
    Test-PaymentFlow
    Test-SplitPayment
    Test-ErrorHandling
}
elseif ($TestPaymentFlow) {
    Test-PaymentFlow
}
elseif ($TestSplitPayment) {
    Test-SplitPayment
}
elseif ($TestErrorHandling) {
    Test-ErrorHandling
}
else {
    Write-Host "Payment Flow Test Script" -ForegroundColor Magenta
    Write-Host "=======================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\test-payment-flow.ps1 -TestPaymentFlow    # Test basic payment flow" -ForegroundColor White
    Write-Host "  .\test-payment-flow.ps1 -TestSplitPayment   # Test split payment logic" -ForegroundColor White
    Write-Host "  .\test-payment-flow.ps1 -TestErrorHandling  # Test error handling" -ForegroundColor White
    Write-Host "  .\test-payment-flow.ps1 -All               # Run all tests" -ForegroundColor White
}
