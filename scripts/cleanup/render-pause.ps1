# Render Pause Script - Safely disconnect Render from main branch
# Prevents auto-deploys from main while keeping services running

param(
    [string]$RenderApiKey = $env:RENDER_API_KEY,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "=== Render Pause & Disconnect from Main ===" -ForegroundColor Cyan
Write-Host ""

if (-not $RenderApiKey) {
    Write-Host "❌ RENDER_API_KEY environment variable not set." -ForegroundColor Red
    Write-Host "   Set it via: `$env:RENDER_API_KEY = 'your-api-key'" -ForegroundColor Yellow
    Write-Host "   Get API key from: https://dashboard.render.com/account/api-keys" -ForegroundColor Yellow
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $RenderApiKey"
    "Accept" = "application/json"
    "Content-Type" = "application/json"
}

$baseUrl = "https://api.render.com/v1"

# Services in blueprint
$services = @(
    "TablesApi", "OrderApi", "PaymentApi", "MenuApi",
    "CustomerApi", "DiscountApi", "InventoryApi",
    "SettingsApi", "UsersApi"
)

Write-Host "📋 Step 1: Fetching Render services..." -ForegroundColor Yellow

try {
    $servicesResponse = Invoke-RestMethod -Uri "$baseUrl/services" -Method Get -Headers $headers
    $serviceMap = @{}
    
    foreach ($service in $services) {
        $found = $servicesResponse | Where-Object { $_.service.name -eq $service } | Select-Object -First 1
        if ($found) {
            $serviceMap[$service] = $found.service.id
            Write-Host "   Found: $service ($($found.service.id))" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  Not found: $service" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "⏸️  Step 2: Pausing auto-deploys..." -ForegroundColor Yellow
    
    foreach ($serviceName in $serviceMap.Keys) {
        $serviceId = $serviceMap[$serviceName]
        
        if ($DryRun) {
            Write-Host "   [DRY RUN] Would pause auto-deploy for: $serviceName" -ForegroundColor Gray
        } else {
            try {
                $pauseBody = @{ autoDeploy = "no" } | ConvertTo-Json
                Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method PATCH -Headers $headers -Body $pauseBody | Out-Null
                Write-Host "   ✓ Paused: $serviceName" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️ Failed to pause $serviceName`: $_" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Host ""
    Write-Host "🔌 Step 3: Disconnecting from main branch..." -ForegroundColor Yellow
    
    foreach ($serviceName in $serviceMap.Keys) {
        $serviceId = $serviceMap[$serviceName]
        
        if ($DryRun) {
            Write-Host "   [DRY RUN] Would disconnect $serviceName from main" -ForegroundColor Gray
        } else {
            try {
                $currentService = Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method Get -Headers $headers
                
                if ($currentService.service.branch -eq "main") {
                    $disconnectBody = @{
                        repo = $null
                        branch = $null
                    } | ConvertTo-Json -Depth 10
                    
                    Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method PATCH -Headers $headers -Body $disconnectBody | Out-Null
                    Write-Host "   ✓ Disconnected: $serviceName" -ForegroundColor Green
                } else {
                    Write-Host "   ℹ️  $serviceName already disconnected (branch: $($currentService.service.branch))" -ForegroundColor Gray
                }
            } catch {
                Write-Host "   ⚠️ Failed to disconnect $serviceName`: $_" -ForegroundColor Yellow
            }
        }
    }
    
    Write-Host ""
    Write-Host "✅ Render Services Paused & Disconnected" -ForegroundColor Green
    Write-Host ""
    Write-Host "Services are still running but will not auto-deploy from main." -ForegroundColor Cyan
    Write-Host "Next: Migrate to DigitalOcean for cost savings (`$71/mo saved)." -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Verification:" -ForegroundColor Yellow
    Write-Host "  Visit: https://dashboard.render.com" -ForegroundColor White
    Write-Host "  Check each service → Settings → Branch should be empty/null" -ForegroundColor White
    
} catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  - Verify RENDER_API_KEY is correct" -ForegroundColor White
    Write-Host "  - Check Render API documentation: https://render.com/docs/api" -ForegroundColor White
    Write-Host "  - Use -DryRun flag to preview changes" -ForegroundColor White
    exit 1
}

