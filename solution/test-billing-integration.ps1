# Test BillingService integration to debug order items issue
Write-Host "Testing BillingService integration..." -ForegroundColor Green

# Known billing ID with order items from database
$testBillingId = "d6b51d97-b72a-49db-a9ee-ea7bfc7fc519"
$orderApiUrl = "https://magidesk-order-904541739138.northamerica-south1.run.app"

Write-Host "Testing OrderApi endpoint directly..." -ForegroundColor Yellow
$url = "$orderApiUrl/api/orders/by-billing/$testBillingId/items"
Write-Host "URL: $url"

try {
    $response = Invoke-RestMethod -Uri $url -Method Get
    Write-Host "OrderApi Response: $($response.Count) items found" -ForegroundColor Green
    if ($response.Count -gt 0) {
        Write-Host "Sample item: ID=$($response[0].id), MenuItemId=$($response[0].menuItemId), Price=$($response[0].basePrice)" -ForegroundColor Green
    }
}
catch {
    Write-Host "OrderApi Error: $_" -ForegroundColor Red
}

Write-Host "`nTesting CORRECTED TablesAPI for billing data..." -ForegroundColor Yellow
$tablesApiUrl = "https://magidesk-tables-904541739138.northamerica-south1.run.app"
$billsUrl = "$tablesApiUrl/bills?from=2025-09-15T00:00:00Z&to=2025-09-17T23:59:59Z"

Write-Host "Fixed URL: $billsUrl" -ForegroundColor Cyan

try {
    $bills = Invoke-RestMethod -Uri $billsUrl -Method Get
    Write-Host "Bills count: $($bills.Count)" -ForegroundColor Green
    
    if ($bills.Count -gt 0) {
        Write-Host "Sample bill structure:" -ForegroundColor Yellow
        $sampleBill = $bills[0]
        Write-Host "  BillId: $($sampleBill.billId)"
        Write-Host "  BillingId: $($sampleBill.billingId)"
        Write-Host "  TableLabel: $($sampleBill.tableLabel)"
        Write-Host "  TotalAmount: $($sampleBill.totalAmount)"
        Write-Host "  StartTime: $($sampleBill.startTime)"
    }
    
    $testBill = $bills | Where-Object { $_.billingId -eq $testBillingId }
    if ($testBill) {
        Write-Host "`nFound test bill in TablesAPI response:" -ForegroundColor Green
        Write-Host "  BillingId: $($testBill.billingId)"
        Write-Host "  TableLabel: $($testBill.tableLabel)"
        Write-Host "  TotalAmount: $($testBill.totalAmount)"
        Write-Host "  Current Items count: $($testBill.items.Count)"
    }
    else {
        Write-Host "`nTest billing ID not found in TablesAPI response" -ForegroundColor Red
        Write-Host "Available billing IDs in response:" -ForegroundColor Yellow
        $bills | ForEach-Object { Write-Host "  $($_.billingId)" }
    }
}
catch {
    Write-Host "TablesAPI Error: $_" -ForegroundColor Red
}
