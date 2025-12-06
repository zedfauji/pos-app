# Test Health Endpoints
Write-Host "`n=== Testing Health Endpoints ===" -ForegroundColor Cyan

$apis = @(
    @{ Name = "UsersApi"; Url = "https://magidesk-users-904541739138.northamerica-south1.run.app/health" },
    @{ Name = "MenuApi"; Url = "https://magidesk-menu-904541739138.northamerica-south1.run.app/health" },
    @{ Name = "OrderApi"; Url = "https://magidesk-order-904541739138.northamerica-south1.run.app/health" },
    @{ Name = "PaymentApi"; Url = "https://magidesk-payment-904541739138.northamerica-south1.run.app/health" },
    @{ Name = "InventoryApi"; Url = "https://magidesk-inventory-904541739138.northamerica-south1.run.app/health" },
    @{ Name = "SettingsApi"; Url = "https://magidesk-settings-904541739138.northamerica-south1.run.app/api/v2/settings/frontend/defaults" },
    @{ Name = "CustomerApi"; Url = "https://magidesk-customer-904541739138.northamerica-south1.run.app/api/customers/ping" },
    @{ Name = "DiscountApi"; Url = "https://magidesk-discount-904541739138.northamerica-south1.run.app/api/discounts/ping" }
)

foreach ($api in $apis) {
    Write-Host "`nTesting $($api.Name)..." -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri $api.Url -Method GET -UseBasicParsing -ErrorAction Stop
        Write-Host "  ✓ Status: $($response.StatusCode)" -ForegroundColor Green
    } catch {
        $statusCode = [int]$_.Exception.Response.StatusCode.value__
        Write-Host "  Status: $statusCode" -ForegroundColor $(if ($statusCode -eq 200) { "Green" } else { "Yellow" })
    }
}

Write-Host "`n=== Health Check Complete ===" -ForegroundColor Cyan

