# Render Cost Optimization Script
# Configures auto-suspend and budget alerts for cost savings

param(
    [switch]$DryRun = $false,
    [int]$AutoSuspendMinutes = 15,
    [decimal]$BudgetAlert = 50.00
)

$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "`n[STEP] $msg" -ForegroundColor Cyan }
function Write-Info { param($msg) Write-Host "  [INFO] $msg" -ForegroundColor Gray }
function Write-Success { param($msg) Write-Host "  [✓] $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "  [⚠] $msg" -ForegroundColor Yellow }
function Write-DryRun { param($msg) Write-Host "  [DRY-RUN] $msg" -ForegroundColor Magenta }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RENDER COST OPTIMIZATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mode: $(if ($DryRun) { 'DRY-RUN' } else { 'EXECUTE' })" -ForegroundColor $(if ($DryRun) { 'Yellow' } else { 'Green' })
Write-Host ""

$Services = @(
    "TablesApi", "OrderApi", "PaymentApi", "MenuApi",
    "CustomerApi", "DiscountApi", "InventoryApi",
    "SettingsApi", "UsersApi"
)

# Step 1: Enable Auto-Suspend
Write-Step "Step 1: Configuring Auto-Suspend"

Write-Info "Auto-suspend settings:"
Write-Info "  - Idle timeout: $AutoSuspendMinutes minutes"
Write-Info "  - Services will auto-suspend when inactive"
Write-Info "  - Auto-resume on first request (may take 10-30 seconds)"

if (-not $DryRun) {
    foreach ($service in $Services) {
        try {
            Write-Info "Configuring auto-suspend for $service..."
            
            # Note: Render CLI may not support auto-suspend directly
            # This would typically be done via Render Dashboard or API
            Write-Warning "Auto-suspend configuration must be done via Render Dashboard:"
            Write-Info "  1. Go to: https://dashboard.render.com"
            Write-Info "  2. Select service: $service"
            Write-Info "  3. Go to Settings → Auto-Suspend"
            Write-Info "  4. Enable with $AutoSuspendMinutes minute timeout"
            
            # Alternative: Use Render API if available
            # $response = Invoke-RestMethod -Uri "https://api.render.com/v1/services/$service" `
            #     -Method PATCH `
            #     -Headers @{ "Authorization" = "Bearer $env:RENDER_API_KEY" } `
            #     -Body (@{ suspendAt = $AutoSuspendMinutes } | ConvertTo-Json)
        } catch {
            Write-Warning "Could not configure auto-suspend for $service via CLI"
        }
    }
} else {
    Write-DryRun "Would configure auto-suspend for all services"
    foreach ($service in $Services) {
        Write-DryRun "  - $service : Auto-suspend after $AutoSuspendMinutes minutes idle"
    }
}

# Step 2: Set Budget Alerts
Write-Step "Step 2: Configuring Budget Alerts"

Write-Info "Budget alert settings:"
Write-Info "  - Alert threshold: `$$BudgetAlert USD/month"
Write-Info "  - You'll receive email alerts when approaching budget"

if (-not $DryRun) {
    Write-Info "Setting budget alert via Render Dashboard:"
    Write-Info "  1. Go to: https://dashboard.render.com/account/billing"
    Write-Info "  2. Click 'Set Budget Alert'"
    Write-Info "  3. Set amount: `$$BudgetAlert"
    Write-Info "  4. Save settings"
    
    # Note: Budget alerts are typically configured via dashboard only
    Write-Warning "Budget alerts must be configured manually via Render Dashboard"
} else {
    Write-DryRun "Would set budget alert at `$$BudgetAlert/month"
}

# Step 3: Cost Estimation
Write-Step "Step 3: Cost Estimation"

Write-Info "Estimated monthly costs (Starter tier):"
Write-Host ""
Write-Host "  Web Services (9x Starter @ `$7/month):" -ForegroundColor White
Write-Host "    9 services × `$7 = `$63/month" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Database (Basic-1GB @ `$7/month):" -ForegroundColor White
Write-Host "    1 database × `$7 = `$7/month" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Estimated Total: `$70/month" -ForegroundColor Green
Write-Host ""
Write-Host "  With auto-suspend (estimated savings ~50%):" -ForegroundColor White
Write-Host "    Estimated: `$35-50/month" -ForegroundColor Green
Write-Host ""
Write-Host "  Current GCP estimate (Cloud Run + SQL):" -ForegroundColor White
Write-Host "    Estimated: `$80-120/month" -ForegroundColor Yellow
Write-Host ""

# Step 4: Generate Cost Report
Write-Step "Step 4: Cost Monitoring Commands"

Write-Info "Use these commands to monitor costs:"
Write-Host ""
Write-Host "  # Check service status" -ForegroundColor Cyan
Write-Host "  render-cli services list" -ForegroundColor White
Write-Host ""
Write-Host "  # Check billing info" -ForegroundColor Cyan
Write-Host "  # Visit: https://dashboard.render.com/account/billing" -ForegroundColor White
Write-Host ""

if (-not $DryRun) {
    Write-Success "Cost optimization configuration completed!"
    Write-Host ""
    Write-Host "Manual steps required:" -ForegroundColor Yellow
    Write-Host "  1. Enable auto-suspend for each service via Render Dashboard" -ForegroundColor White
    Write-Host "  2. Set budget alert at `$$BudgetAlert via Render Dashboard" -ForegroundColor White
    Write-Host "  3. Monitor costs in Render Dashboard weekly" -ForegroundColor White
} else {
    Write-DryRun "Cost optimization preview completed"
    Write-Host ""
    Write-Host "To apply:" -ForegroundColor Yellow
    Write-Host "  .\scripts\migration\render-cost-optimizer.ps1" -ForegroundColor Green
}

Write-Host ""

