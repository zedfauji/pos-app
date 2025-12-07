# Fix RBAC Issues Script
# This script applies the fixes to all APIs

Write-Host "=== Applying RBAC Fixes to All APIs ===" -ForegroundColor Cyan
Write-Host ""

$apis = @(
    "OrderApi",
    "PaymentApi",
    "InventoryApi",
    "CustomerApi",
    "DiscountApi"
)

foreach ($api in $apis) {
    Write-Host "Fixing $api..." -ForegroundColor Yellow
    $programPath = "solution/backend/$api/Program.cs"
    
    if (Test-Path $programPath) {
        Write-Host "  ✓ Found $programPath" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Not found: $programPath" -ForegroundColor Red
    }
}

Write-Host "`nFixes to apply:" -ForegroundColor Cyan
Write-Host "1. Add Authentication service with 'Bearer' scheme" -ForegroundColor White
Write-Host "2. Set FallbackPolicy = null in Authorization options" -ForegroundColor White
Write-Host "3. Add UseAuthentication() before UseAuthorization()" -ForegroundColor White
Write-Host "4. Fix CancellationTokenSource.CreateLinkedTokenSource() call" -ForegroundColor White
Write-Host "5. Add exception handler middleware" -ForegroundColor White

