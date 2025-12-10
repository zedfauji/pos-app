# Render Blueprint Reconnection Script
# Safely disconnect from main and reconnect to develop branch
# Prevents production disruption during cleanup

param(
    [string]$RenderApiKey = $env:RENDER_API_KEY,
    [string]$RenderOwnerId = $env:RENDER_OWNER_ID,
    [string]$ServiceName = "magidesk-pos",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "=== Render Blueprint Reconnection ===" -ForegroundColor Cyan
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

# Step 1: Get current blueprint/service information
Write-Host "📋 Step 1: Fetching current Render services..." -ForegroundColor Yellow

try {
    # Get all services for this owner
    $servicesResponse = Invoke-RestMethod -Uri "$baseUrl/services" -Method Get -Headers $headers
    
    $blueprintServices = @(
        "TablesApi", "OrderApi", "PaymentApi", "MenuApi", 
        "CustomerApi", "DiscountApi", "InventoryApi", 
        "SettingsApi", "UsersApi"
    )
    
    $services = $servicesResponse | Where-Object { $blueprintServices -contains $_.service.name }
    
    Write-Host "   Found $($services.Count) services in blueprint" -ForegroundColor Green
    
    # Step 2: Pause auto-deploys on all services
    Write-Host ""
    Write-Host "⏸️  Step 2: Pausing auto-deploys..." -ForegroundColor Yellow
    
    foreach ($service in $services) {
        $serviceId = $service.service.id
        $serviceName = $service.service.name
        
        if ($DryRun) {
            Write-Host "   [DRY RUN] Would pause auto-deploy for: $serviceName ($serviceId)" -ForegroundColor Gray
        } else {
            try {
                $pauseBody = @{
                    autoDeploy = "no"
                } | ConvertTo-Json
                
                Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method PATCH -Headers $headers -Body $pauseBody | Out-Null
                Write-Host "   ✓ Paused auto-deploy for: $serviceName" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️ Failed to pause $serviceName`: $_" -ForegroundColor Yellow
            }
        }
    }
    
    # Step 3: Disconnect from main branch
    Write-Host ""
    Write-Host "🔌 Step 3: Disconnecting from main branch..." -ForegroundColor Yellow
    
    foreach ($service in $services) {
        $serviceId = $service.service.id
        $serviceName = $service.service.name
        
        if ($DryRun) {
            Write-Host "   [DRY RUN] Would disconnect $serviceName from main" -ForegroundColor Gray
        } else {
            try {
                # Get current service config
                $currentService = Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method Get -Headers $headers
                
                if ($currentService.service.repo -and $currentService.service.branch -eq "main") {
                    # Disconnect repo (set to empty/null)
                    $disconnectBody = @{
                        repo = $null
                        branch = $null
                    } | ConvertTo-Json -Depth 10
                    
                    Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method PATCH -Headers $headers -Body $disconnectBody | Out-Null
                    Write-Host "   ✓ Disconnected $serviceName from main" -ForegroundColor Green
                } else {
                    Write-Host "   ℹ️  $serviceName not connected to main (already disconnected)" -ForegroundColor Gray
                }
            } catch {
                Write-Host "   ⚠️ Failed to disconnect $serviceName`: $_" -ForegroundColor Yellow
            }
        }
    }
    
    # Step 4: Connect to develop branch
    Write-Host ""
    Write-Host "🔗 Step 4: Connecting to develop branch..." -ForegroundColor Yellow
    
    $repoUrl = "https://github.com/zedfauji/pos-app"
    
    foreach ($service in $services) {
        $serviceId = $service.service.id
        $serviceName = $service.service.name
        
        if ($DryRun) {
            Write-Host "   [DRY RUN] Would connect $serviceName to develop branch" -ForegroundColor Gray
        } else {
            try {
                $connectBody = @{
                    repo = $repoUrl
                    branch = "develop"
                    autoDeploy = "yes"
                } | ConvertTo-Json
                
                Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method PATCH -Headers $headers -Body $connectBody | Out-Null
                Write-Host "   ✓ Connected $serviceName to develop branch" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️ Failed to connect $serviceName`: $_" -ForegroundColor Yellow
            }
        }
    }
    
    # Step 5: Verify connection
    Write-Host ""
    Write-Host "✅ Step 5: Verifying connection..." -ForegroundColor Yellow
    
    Start-Sleep -Seconds 3
    
    foreach ($service in $services) {
        $serviceId = $service.service.id
        $serviceName = $service.service.name
        
        try {
            $serviceInfo = Invoke-RestMethod -Uri "$baseUrl/services/$serviceId" -Method Get -Headers $headers
            
            if ($serviceInfo.service.branch -eq "develop") {
                Write-Host "   ✓ $serviceName is connected to develop" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  $serviceName branch: $($serviceInfo.service.branch)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "   ⚠️ Could not verify $serviceName`: $_" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "=== Reconnection Complete ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Verify services in Render dashboard" -ForegroundColor White
    Write-Host "  2. Test health endpoints:" -ForegroundColor White
    Write-Host "     curl https://tablesapi.onrender.com/health" -ForegroundColor Gray
    Write-Host "  3. Monitor deployments: https://dashboard.render.com" -ForegroundColor White
    
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

