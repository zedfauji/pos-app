# Render Rollback Script - Blue-Green Deployment
# Pauses Render services and resumes GCP Cloud Run

param(
    [switch]$DryRun = $false,
    [switch]$PauseRender = $false,
    [switch]$ResumeGCP = $false,
    [string]$GCPProject = "bola8pos",
    [string]$GCPRegion = "northamerica-south1"
)

$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "`n[STEP] $msg" -ForegroundColor Cyan }
function Write-Info { param($msg) Write-Host "  [INFO] $msg" -ForegroundColor Gray }
function Write-Success { param($msg) Write-Host "  [✓] $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "  [⚠] $msg" -ForegroundColor Yellow }
function Write-Error-Custom { param($msg) Write-Host "  [✗] $msg" -ForegroundColor Red }
function Write-DryRun { param($msg) Write-Host "  [DRY-RUN] $msg" -ForegroundColor Magenta }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RENDER ROLLBACK SCRIPT" -ForegroundColor Cyan
Write-Host "  Blue-Green Deployment Rollback" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mode: $(if ($DryRun) { 'DRY-RUN' } else { 'EXECUTE' })" -ForegroundColor $(if ($DryRun) { 'Yellow' } else { 'Green' })
Write-Host ""

$RenderServices = @(
    "TablesApi", "OrderApi", "PaymentApi", "MenuApi",
    "CustomerApi", "DiscountApi", "InventoryApi",
    "SettingsApi", "UsersApi"
)

$GCPServices = @(
    "magidesk-tables",
    "magidesk-order",
    "magidesk-payment",
    "magidesk-menu",
    "magidesk-customer",
    "magidesk-discount",
    "magidesk-inventory",
    "magidesk-settings",
    "magidesk-users"
)

# Step 1: Pause Render Services
Write-Step "Step 1: Pausing Render Services"

if ($PauseRender -or (-not $ResumeGCP)) {
    Write-Info "Pausing Render services to stop traffic..."
    
    if (-not $DryRun) {
        foreach ($service in $RenderServices) {
            try {
                Write-Info "Pausing $service..."
                
                # Suspend service (stops accepting requests)
                # Note: Render CLI may use different command - check documentation
                try {
                    render-cli services suspend $service
                    Write-Success "$service paused"
                } catch {
                    # Alternative: Use API or manual dashboard
                    Write-Warning "Could not pause $service via CLI. Pause manually in Render Dashboard"
                    Write-Info "  Go to: https://dashboard.render.com → $service → Suspend"
                }
            } catch {
                Write-Error-Custom "Failed to pause $service: $_"
            }
        }
        
        Write-Info "Waiting 30 seconds for traffic to drain..."
        Start-Sleep -Seconds 30
        Write-Success "Render services paused"
    } else {
        Write-DryRun "Would pause all Render services:"
        foreach ($service in $RenderServices) {
            Write-DryRun "  - render-cli services suspend $service"
        }
    }
} else {
    Write-Info "Skipping Render pause (--ResumeGCP only mode)"
}

# Step 2: Verify Render Services Paused
Write-Step "Step 2: Verifying Render Services Status"

if (-not $DryRun -and ($PauseRender -or (-not $ResumeGCP))) {
    Write-Info "Checking service status..."
    
    foreach ($service in $RenderServices) {
        try {
            $status = render-cli services show $service --format json | ConvertFrom-Json
            if ($status.suspendedAt) {
                Write-Success "$service : Suspended"
            } else {
                Write-Warning "$service : Still active (may need manual pause)"
            }
        } catch {
            Write-Warning "$service : Could not verify status"
        }
    }
} else {
    Write-DryRun "Would check Render service status"
}

# Step 3: Resume GCP Cloud Run Services
Write-Step "Step 3: Resuming GCP Cloud Run Services"

if ($ResumeGCP -or (-not $PauseRender)) {
    Write-Info "Resuming GCP Cloud Run services..."
    
    # Check if gcloud is available
    $gcloudAvailable = $false
    try {
        $null = gcloud --version 2>$null
        $gcloudAvailable = $true
    } catch {
        Write-Warning "gcloud CLI not found. GCP operations will need to be done manually."
    }
    
    if (-not $DryRun -and $gcloudAvailable) {
        foreach ($service in $GCPServices) {
            try {
                Write-Info "Resuming $service..."
                
                # Ensure service is at 100% traffic (may already be)
                gcloud run services update $service `
                    --project $GCPProject `
                    --region $GCPRegion `
                    --no-traffic-splits 2>&1 | Out-Null
                
                # Scale to minimum instances if needed
                gcloud run services update $service `
                    --project $GCPProject `
                    --region $GCPRegion `
                    --min-instances 1 2>&1 | Out-Null
                
                Write-Success "$service resumed on GCP"
            } catch {
                Write-Warning "Could not resume $service: $_"
                Write-Info "Resume manually: gcloud run services update $service --project $GCPProject --region $GCPRegion"
            }
        }
        
        Write-Success "GCP Cloud Run services resumed"
    } else {
        if ($DryRun) {
            Write-DryRun "Would resume GCP Cloud Run services:"
            foreach ($service in $GCPServices) {
                Write-DryRun "  - gcloud run services update $service --project $GCPProject --region $GCPRegion --min-instances 1"
            }
        } else {
            Write-Warning "Manual GCP resumption required"
            Write-Info "For each service, run:"
            foreach ($service in $GCPServices) {
                Write-Info "  gcloud run services update $service --project $GCPProject --region $GCPRegion --min-instances 1"
            }
        }
    }
} else {
    Write-Info "Skipping GCP resume (--PauseRender only mode)"
}

# Step 4: Update DNS/Load Balancer (if applicable)
Write-Step "Step 4: DNS/Load Balancer Configuration"

Write-Info "If using custom domains or load balancers:"
Write-Info "  1. Update DNS records to point back to GCP Cloud Run URLs"
Write-Info "  2. Update load balancer backends to GCP services"
Write-Info "  3. Verify DNS propagation (may take up to 48 hours)"

if (-not $DryRun) {
    Write-Warning "DNS/Load balancer updates must be done manually"
    Write-Info "GCP Cloud Run service URLs format:"
    Write-Info "  https://magidesk-<service>-<project-hash>.northamerica-south1.run.app"
} else {
    Write-DryRun "Would update DNS to point back to GCP Cloud Run"
}

# Step 5: Verification
Write-Step "Step 5: Rollback Verification"

if (-not $DryRun) {
    Write-Info "Verifying rollback..."
    
    Write-Info "1. Check GCP services are running:"
    if ($gcloudAvailable) {
        foreach ($service in $GCPServices) {
            try {
                $gcpStatus = gcloud run services describe $service `
                    --project $GCPProject `
                    --region $GCPRegion `
                    --format="value(status.url)" 2>$null
                
                if ($gcpStatus) {
                    Write-Success "$service : $gcpStatus"
                } else {
                    Write-Warning "$service : Could not get status"
                }
            } catch {
                Write-Warning "$service : Verification failed"
            }
        }
    }
    
    Write-Info "2. Test GCP endpoints (replace <service-url> with actual URL):"
    Write-Info "   curl https://<service-url>/health"
    Write-Info ""
    Write-Info "3. Monitor GCP Cloud Run metrics in GCP Console"
    Write-Info "   https://console.cloud.google.com/run?project=$GCPProject"
} else {
    Write-DryRun "Would verify GCP services are running and responding"
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ROLLBACK SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "DRY-RUN MODE: No changes were made" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To execute rollback:" -ForegroundColor White
    Write-Host "  # Pause Render only:" -ForegroundColor Cyan
    Write-Host "  .\scripts\migration\render-rollback.ps1 -PauseRender" -ForegroundColor Green
    Write-Host ""
    Write-Host "  # Resume GCP only:" -ForegroundColor Cyan
    Write-Host "  .\scripts\migration\render-rollback.ps1 -ResumeGCP" -ForegroundColor Green
    Write-Host ""
    Write-Host "  # Full rollback:" -ForegroundColor Cyan
    Write-Host "  .\scripts\migration\render-rollback.ps1 -PauseRender -ResumeGCP" -ForegroundColor Green
} else {
    Write-Success "Rollback process completed!"
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify all GCP services are responding" -ForegroundColor White
    Write-Host "  2. Update DNS/load balancer if using custom domains" -ForegroundColor White
    Write-Host "  3. Monitor GCP Cloud Run metrics" -ForegroundColor White
    Write-Host "  4. Update frontend to point back to GCP URLs" -ForegroundColor White
    Write-Host ""
    Write-Host "To resume Render later:" -ForegroundColor Yellow
    Write-Host "  render-cli services resume <service-name>" -ForegroundColor White
}

Write-Host ""

