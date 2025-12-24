# OrderApi Smoke Tests
$BaseUrl = "http://localhost:5256/api"

function Assert-Equal {
    param($Expected, $Actual, $Message)
    if ($Expected -eq $Actual) {
        Write-Host "[PASS] $Message" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $Message. Expected: '$Expected', Actual: '$Actual'" -ForegroundColor Red
    }
}

function Test-CreateOrder {
    Write-Host "`n--- Testing Create Order (Legacy Parity) ---" -ForegroundColor Cyan
    
    # Use UUID for TableID to bypass legacy string mismatch if needed, 
    # but our recent fix allows strings to default to Empty GUID.
    # Let's test with a valid UUID first to confirm core logic.
    $TableId = "00000000-0000-0000-0000-000000000001" 
    $MenuItemId = "10000000-0000-0000-0000-000000000001" # Burger $12.99

    $body = @{
        SessionId = [Guid]::NewGuid().ToString()
        TableId = $TableId
        ServerId = "TEST-SERVER"
        Items = @(
            @{
                MenuItemId = $MenuItemId
                Quantity = 1
                Modifiers = @()
            }
        )
        DiscountTotal = 0
    } | ConvertTo-Json -Depth 5

    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/orders" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
        
        # assertions
        Assert-Equal -Expected "open" -Actual $response.status -Message "Order Status should be 'open'"
        Assert-Equal -Expected 12.99 -Actual $response.subtotal -Message "Subtotal should be 12.99"
        Assert-Equal -Expected 1.04 -Actual $response.taxTotal -Message "Tax should be 1.04 (8%)"
        Assert-Equal -Expected 14.03 -Actual $response.total -Message "Total should be 14.03"
        
        if ($response.billingId) {
            Write-Host "[PASS] Billing ID was auto-generated: $($response.billingId)" -ForegroundColor Green
        } else {
             Write-Host "[FAIL] Billing ID is missing!" -ForegroundColor Red
        }

    } catch {
        Write-Host "[ERROR] Request Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
             $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
             Write-Host "Response Body: $($reader.ReadToEnd())" -ForegroundColor Yellow
        }
    }
}

# Run Tests
Test-CreateOrder
