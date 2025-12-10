# DigitalOcean App Platform Migration Script
# Migrates MagiDesk POS from Render to DigitalOcean ($71/mo savings)

param(
    [string]$DoApiToken = $env:DIGITALOCEAN_API_TOKEN,
    [string]$DumpFile = "dumps/postgres-dump-20251209-210039.sql",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     DIGITALOCEAN APP PLATFORM MIGRATION                  ║" -ForegroundColor Yellow
Write-Host "╚════════════════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

if (-not $DoApiToken) {
    Write-Host "❌ DIGITALOCEAN_API_TOKEN environment variable not set." -ForegroundColor Red
    Write-Host "   Get API token from: https://cloud.digitalocean.com/account/api/tokens" -ForegroundColor Yellow
    Write-Host "   Set it: `$env:DIGITALOCEAN_API_TOKEN = 'your-token'" -ForegroundColor Yellow
    exit 1
}

$baseUrl = "https://api.digitalocean.com/v2"
$headers = @{
    "Authorization" = "Bearer $DoApiToken"
    "Content-Type" = "application/json"
}

function Write-Step {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "`n▶ $Message" -ForegroundColor $Color
}

function Invoke-DoApi {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body = $null
    )
    
    $uri = "$baseUrl/$Endpoint"
    
    if ($DryRun) {
        Write-Host "   [DRY RUN] $Method $uri" -ForegroundColor Gray
        if ($Body) {
            Write-Host "   Body: $($Body | ConvertTo-Json -Compress)" -ForegroundColor Gray
        }
        return $null
    }
    
    $params = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
    }
    
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }
    
    try {
        return Invoke-RestMethod @params
    } catch {
        Write-Host "   ❌ Error: $_" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "   Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
        throw
    }
}

Write-Step "Step 1: Verify Database Dump File"
if (-not (Test-Path $DumpFile)) {
    Write-Host "   ⚠️  Dump file not found: $DumpFile" -ForegroundColor Yellow
    Write-Host "   Creating dump..." -ForegroundColor Cyan
    .\scripts\db\create-postgres-dump.ps1 -DbHost "34.51.82.201" -OutputFile $DumpFile
}

if (Test-Path $DumpFile) {
    $dumpSize = (Get-Item $DumpFile).Length / 1MB
    Write-Host "   ✓ Dump file found: $DumpFile ($([math]::Round($dumpSize, 2)) MB)" -ForegroundColor Green
}

Write-Step "Step 2: Import App Spec to DigitalOcean"

$appSpecPath = "infra/do-app-platform/do-app.yaml"
if (-not (Test-Path $appSpecPath)) {
    Write-Host "   ❌ App spec not found: $appSpecPath" -ForegroundColor Red
    exit 1
}

$appSpec = Get-Content $appSpecPath -Raw
Write-Host "   ✓ App spec loaded from: $appSpecPath" -ForegroundColor Green

if ($DryRun) {
    Write-Host "   [DRY RUN] Would create app from spec" -ForegroundColor Gray
} else {
    Write-Host "   Creating app from spec..." -ForegroundColor Cyan
    
    # Note: DigitalOcean App Platform API requires creating app first, then adding services
    # This is a simplified version - full implementation would:
    # 1. Create app
    # 2. Add database
    # 3. Add services
    # 4. Import database dump
    
    Write-Host "   ℹ️  Full migration requires Dashboard UI or doctl CLI" -ForegroundColor Yellow
    Write-Host "   See: infra/do-app-platform/MIGRATION_INSTRUCTIONS.md" -ForegroundColor Yellow
}

Write-Step "Step 3: Database Import Command"

$importCommand = @"
# Import database dump to DigitalOcean Managed Database
# Replace DATABASE_CONNECTION_STRING with actual connection string from DO dashboard

# Option 1: Using psql (recommended)
psql "DATABASE_CONNECTION_STRING" < $DumpFile

# Option 2: Using pg_restore (if dump was created in custom format)
pg_restore -d "DATABASE_CONNECTION_STRING" $DumpFile

# Option 3: Using doctl (DigitalOcean CLI)
# First install: https://docs.digitalocean.com/reference/doctl/how-to/install/
doctl databases connection pool $DumpFile --connection-pool-name=magidesk-pos
"@

Write-Host "   Import command:" -ForegroundColor Cyan
Write-Host $importCommand -ForegroundColor Gray

if (-not $DryRun) {
    $importCmdPath = "infra/do-app-platform/import-db.md"
    $importCommand | Out-File -FilePath $importCmdPath -Encoding UTF8
    Write-Host "   ✓ Saved to: $importCmdPath" -ForegroundColor Green
}

Write-Step "Step 4: Next Steps"

$instructions = @"
╔════════════════════════════════════════════════════════════╗
║         DIGITALOCEAN MIGRATION NEXT STEPS                  ║
╚════════════════════════════════════════════════════════════╝

1. CREATE APP PLATFORM APP:
   - Go to: https://cloud.digitalocean.com/apps
   - Click "Create App" → "GitHub"
   - Connect repository: zedfauji/pos-app
   - Select branch: feature/revamp-2025-enterprise-ui
   - Click "Edit App Spec" and paste contents of:
     infra/do-app-platform/do-app.yaml

2. CREATE MANAGED DATABASE:
   - Plan: db-s-dev-database ($15/mo)
   - Engine: PostgreSQL 17
   - Region: NYC1 (same as app)
   - Database name: magideskdb
   - Note the connection string (format: postgresql://user:pass@host:port/db)

3. IMPORT DATABASE DUMP:
   - Get connection string from DO dashboard
   - Run: psql "CONNECTION_STRING" < dumps/postgres-dump-20251209-210039.sql
   - Or use DO web console SQL editor

4. UPDATE ENVIRONMENT VARIABLES:
   - For each service, set ConnectionStrings__Postgres to database connection string
   - Use DO's variable reference: `{magidesk-postgres.DATABASE_URL}`

5. ENABLE PREVIEW ENVIRONMENTS:
   - In App Platform settings, enable "Preview Deployments"
   - Each branch will get its own preview environment automatically

6. DEPLOY & VERIFY:
   - Push to feature/revamp-2025-enterprise-ui triggers deployment
   - Check health endpoints:
     - https://tables-api-xxx.ondigitalocean.app/health
     - https://order-api-xxx.ondigitalocean.app/health

COST COMPARISON:
  - Render: $86.50/mo (9 services × $7 + DB $20)
  - DigitalOcean: $15/mo (DB only, apps free in dev)
  - SAVINGS: $71.50/month (83% reduction)

VERIFICATION:
  - All 9 APIs should be running
  - Database import successful (verify table count)
  - Health checks passing
  - Connection strings correct
"@

Write-Host $instructions -ForegroundColor Cyan

$instructionsPath = "infra/do-app-platform/MIGRATION_INSTRUCTIONS.md"
$instructions | Out-File -FilePath $instructionsPath -Encoding UTF8
Write-Host "`n✓ Full instructions saved to: $instructionsPath" -ForegroundColor Green

Write-Host "`n✅ Migration preparation complete!" -ForegroundColor Green
Write-Host "   Next: Follow instructions in $instructionsPath" -ForegroundColor Yellow

