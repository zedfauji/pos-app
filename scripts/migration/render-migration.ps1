# Render Migration Script - GCP Cloud Run to Render
# Migrates MagiDesk POS (9 ASP.NET Core APIs + Postgres) to Render
# Author: DevOps Migration Specialist
# Version: 1.0

param(
    [switch]$DryRun = $false,
    [switch]$SkipRenderCli = $false,
    [string]$RenderAccountEmail = "",
    [string]$GitHubRepo = "",
    [string]$DumpFile = "dumps\postgres-dump-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql",
    [string]$EnvJsonPath = "scripts\migration\render-env.json"
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Step { param($msg) Write-Host "`n[STEP] $msg" -ForegroundColor Cyan }
function Write-Info { param($msg) Write-Host "  [INFO] $msg" -ForegroundColor Gray }
function Write-Success { param($msg) Write-Host "  [✓] $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "  [⚠] $msg" -ForegroundColor Yellow }
function Write-Error-Custom { param($msg) Write-Host "  [✗] $msg" -ForegroundColor Red }
function Write-DryRun { param($msg) Write-Host "  [DRY-RUN] $msg" -ForegroundColor Magenta }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RENDER MIGRATION SCRIPT" -ForegroundColor Cyan
Write-Host "  GCP Cloud Run → Render" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mode: $(if ($DryRun) { 'DRY-RUN' } else { 'EXECUTE' })" -ForegroundColor $(if ($DryRun) { 'Yellow' } else { 'Green' })
Write-Host ""

# Configuration
$Services = @(
    @{ Name = "TablesApi"; Port = 8080; Tier = "starter" },
    @{ Name = "OrderApi"; Port = 8081; Tier = "starter" },
    @{ Name = "PaymentApi"; Port = 8082; Tier = "starter" },
    @{ Name = "MenuApi"; Port = 8083; Tier = "starter" },
    @{ Name = "CustomerApi"; Port = 8084; Tier = "starter" },
    @{ Name = "DiscountApi"; Port = 8085; Tier = "starter" },
    @{ Name = "InventoryApi"; Port = 8086; Tier = "starter" },
    @{ Name = "SettingsApi"; Port = 8087; Tier = "starter" },
    @{ Name = "UsersApi"; Port = 8088; Tier = "starter" }
)

$DbName = "magidesk-pos"
$DbTier = "basic-1gb"
$Region = "oregon"

# Step 1: Verify Prerequisites
Write-Step "Step 1: Verifying Prerequisites"

if (-not $DryRun) {
    # Check if render-cli is installed
    $renderCliInstalled = $false
    $renderCliPath = $null
    
    # Try different command variations
    $cliCommands = @("render", "render-cli", "render.exe")
    foreach ($cmd in $cliCommands) {
        try {
            $cliPath = Get-Command $cmd -ErrorAction SilentlyContinue
            if ($cliPath) {
                $renderCliInstalled = $true
                $renderCliPath = $cliPath.Source
                Write-Success "Render CLI found: $renderCliPath"
                break
            }
        } catch {
            # Continue checking
        }
    }

    if (-not $renderCliInstalled -and -not $SkipRenderCli) {
        Write-Warning "Render CLI not found. Options:"
        Write-Info "  1. Install manually from: https://github.com/renderinc/cli/releases"
        Write-Info "  2. Use Render API instead (requires API key)"
        Write-Info "  3. Use Dashboard-based migration (manual steps)"
        Write-Host ""
        
        $choice = Read-Host "Choose option (1=Download CLI, 2=Use API, 3=Manual Dashboard) [1]"
        
        if ($choice -eq "" -or $choice -eq "1") {
            Write-Info "Downloading Render CLI for Windows..."
            $cliUrl = "https://github.com/renderinc/cli/releases/latest/download/render-windows-amd64.exe"
            $cliDir = "$env:LOCALAPPDATA\RenderCLI"
            $cliExe = "$cliDir\render.exe"
            
            if (-not (Test-Path $cliDir)) {
                New-Item -ItemType Directory -Path $cliDir -Force | Out-Null
            }
            
            try {
                Invoke-WebRequest -Uri $cliUrl -OutFile $cliExe -UseBasicParsing
                Write-Success "Downloaded Render CLI to $cliExe"
                Write-Info "Adding to PATH for this session..."
                $env:Path += ";$cliDir"
                
                # Verify installation
                $version = & $cliExe --version 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Render CLI installed: $version"
                    $renderCliInstalled = $true
                    $renderCliPath = $cliExe
                }
            } catch {
                Write-Error-Custom "Failed to download Render CLI: $_"
                Write-Info "Please download manually from: https://github.com/renderinc/cli/releases"
                Write-Info "Or use API/Dashboard methods (options 2 or 3)"
            }
        } elseif ($choice -eq "2") {
            Write-Info "Will use Render API method (requires API key)"
            $script:UseRenderAPI = $true
        } else {
            Write-Info "Will provide dashboard-based manual instructions"
            $script:UseDashboard = $true
        }
    }
    
    # If CLI still not available, show alternative options
    if (-not $renderCliInstalled -and -not $SkipRenderCli -and -not $script:UseRenderAPI -and -not $script:UseDashboard) {
        Write-Host ""
        Write-Warning "Render CLI not available. Using alternative methods."
        Write-Host ""
        Write-Host "OPTION 1: Dashboard Method (Recommended)" -ForegroundColor Green
        Write-Host "  See: scripts/migration/render-dashboard-guide.md" -ForegroundColor White
        Write-Host "  Or run: Get-Content scripts/migration/render-dashboard-guide.md" -ForegroundColor Gray
        Write-Host ""
        Write-Host "OPTION 2: API Method" -ForegroundColor Green
        Write-Host "  Run: .\scripts\migration\render-api-migration.ps1" -ForegroundColor White
        Write-Host ""
        Write-Host "OPTION 3: Continue with manual CLI installation" -ForegroundColor Green
        Write-Host "  Download from: https://github.com/renderinc/cli/releases" -ForegroundColor White
        Write-Host "  Place render.exe in PATH and rerun this script" -ForegroundColor White
        Write-Host ""
        
        $continue = Read-Host "Continue with Dashboard instructions? (y/n) [y]"
        if ($continue -eq "" -or $continue -eq "y") {
            Write-Host ""
            Write-Host "Opening dashboard guide..." -ForegroundColor Cyan
            Start-Process "notepad.exe" -ArgumentList "scripts/migration/render-dashboard-guide.md"
            Write-Host ""
            Write-Host "Key steps:" -ForegroundColor Yellow
            Write-Host "  1. Go to https://dashboard.render.com" -ForegroundColor White
            Write-Host "  2. New → Blueprint → Connect GitHub" -ForegroundColor White
            Write-Host "  3. Select render.yaml → Apply" -ForegroundColor White
            Write-Host "  4. Import database dump manually" -ForegroundColor White
            Write-Host "  5. Configure environment variables" -ForegroundColor White
            Write-Host ""
            exit 0
        } else {
            exit 0
        }
    }

    # Check for required files
    if (-not (Test-Path "render.yaml")) {
        Write-Error-Custom "render.yaml not found. Please generate it first."
        exit 1
    }

    if (-not (Test-Path $DumpFile)) {
        Write-Warning "Database dump file not found: $DumpFile"
        Write-Info "You'll need to import the database manually later."
    }

    # Check git
    try {
        $null = git --version 2>$null
        Write-Success "Git is installed"
    } catch {
        Write-Error-Custom "Git is required but not found"
        exit 1
    }

    # Check if GitHub repo is configured
    if (-not $GitHubRepo) {
        try {
            $remoteUrl = (git remote get-url origin 2>$null)
            if ($remoteUrl -match 'github.com[:/]([^/]+/[^/]+)') {
                $GitHubRepo = $matches[1] -replace '\.git$', ''
                Write-Success "Detected GitHub repo: $GitHubRepo"
            }
        } catch {
            Write-Warning "Could not auto-detect GitHub repo"
        }
    }
} else {
    Write-DryRun "Would check prerequisites (render-cli, git, render.yaml)"
}

# Step 2: Render CLI Authentication (Skip if using API/Dashboard)
Write-Step "Step 2: Render CLI Authentication"

if ($script:UseRenderAPI -or $script:UseDashboard) {
    Write-Info "Skipping CLI authentication (using API/Dashboard method)"
} elseif ($renderCliInstalled -and -not $DryRun) {
    if (-not $RenderAccountEmail) {
        $RenderAccountEmail = Read-Host "Enter your Render account email"
    }

    Write-Info "Authenticating with Render..."
    Write-Info "You will be prompted to authenticate in your browser"
    
    try {
        $cliCmd = if ($renderCliPath) { $renderCliPath } else { "render-cli" }
        & $cliCmd auth login
        Write-Success "Authentication successful"
        
        # Get account info
        $accountInfo = & $cliCmd whoami
        Write-Info "Logged in as: $accountInfo"
    } catch {
        Write-Error-Custom "Authentication failed. Please run: $cliCmd auth login"
        Write-Info "Or use Dashboard/API methods instead"
        exit 1
    }
} else {
    Write-DryRun "Would authenticate with Render CLI"
    Write-DryRun "Command: render-cli auth login"
}

# Step 3: Create Render Services from Blueprint
Write-Step "Step 3: Deploying Services from Blueprint"

if (-not $DryRun) {
    Write-Info "Applying render.yaml blueprint..."
    
    try {
        # First, ensure we have the right render.yaml format
        $blueprintResult = render-cli blueprints apply --file render.yaml --name "magidesk-pos-migration"
        Write-Success "Blueprint applied successfully"
        Write-Info "Services are being created..."
        
        # Wait a bit for services to initialize
        Start-Sleep -Seconds 5
        
        # Get service list
        Write-Info "Fetching deployed services..."
        $services = render-cli services list
        Write-Success "Services deployed:"
        $services | ForEach-Object { Write-Info "  - $_" }
    } catch {
        Write-Error-Custom "Blueprint application failed: $_"
        Write-Info "You may need to create services manually via Render dashboard"
    }
} else {
    Write-DryRun "Would apply render.yaml blueprint"
    Write-DryRun "Command: render-cli blueprints apply --file render.yaml"
    Write-DryRun ""
    Write-DryRun "Services that would be created:"
    foreach ($service in $Services) {
        Write-DryRun "  - Web Service: $($service.Name) (Tier: $($service.Tier), Port: $($service.Port))"
    }
    Write-DryRun "  - Database: $DbName (Tier: $DbTier, Region: $Region)"
}

# Step 4: Provision PostgreSQL Database
Write-Step "Step 4: Provisioning PostgreSQL Database"

if (-not $DryRun) {
    Write-Info "Creating PostgreSQL database..."
    
    try {
        # Check if database already exists
        $existingDb = render-cli databases list | Select-String $DbName
        if ($existingDb) {
            Write-Warning "Database $DbName already exists"
            $createDb = Read-Host "Use existing database? (y/n)"
            if ($createDb -ne 'y') {
                Write-Info "Skipping database creation"
            }
        } else {
            $dbResult = render-cli databases create `
                --name $DbName `
                --database-name postgres `
                --region $Region `
                --plan $DbTier `
                --pg-version 17
            
            Write-Success "Database created: $DbName"
            Write-Info "Connection string will be set as environment variable"
        }
        
        # Get database connection info
        $dbInfo = render-cli databases show $DbName --format json | ConvertFrom-Json
        $dbConnectionString = $dbInfo.connectionString
        
        Write-Success "Database connection string retrieved"
        Write-Info "Internal host: $($dbInfo.internalConnectionString)"
        
    } catch {
        Write-Error-Custom "Database creation failed: $_"
        Write-Info "You may need to create the database manually via Render dashboard"
    }
} else {
    Write-DryRun "Would create PostgreSQL database"
    Write-DryRun "Command: render-cli databases create --name $DbName --database-name postgres --region $Region --plan $DbTier --pg-version 17"
}

# Step 5: Import Database Dump
Write-Step "Step 5: Importing Database Dump"

if (-not $DryRun -and (Test-Path $DumpFile)) {
    Write-Info "Importing database dump: $DumpFile"
    
    try {
        # Get database connection string
        $dbInfo = render-cli databases show $DbName --format json | ConvertFrom-Json
        $connectionString = $dbInfo.connectionString
        
        Write-Info "Connecting to database and importing..."
        
        # Extract connection details
        if ($connectionString -match 'postgres://([^:]+):([^@]+)@([^:]+):(\d+)/(.+)') {
            $dbUser = $matches[1]
            $dbPass = $matches[2]
            $dbHost = $matches[3]
            $dbPort = $matches[4]
            $dbName = $matches[5]
            
            # Set PGPASSWORD and import
            $env:PGPASSWORD = $dbPass
            psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -f $DumpFile
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Database dump imported successfully"
            } else {
                Write-Warning "Database import may have had issues. Please verify."
            }
        }
    } catch {
        Write-Error-Custom "Database import failed: $_"
        Write-Info "You can import manually using: psql <connection-string> < $DumpFile"
    }
} else {
    if ($DryRun) {
        Write-DryRun "Would import database dump: $DumpFile"
        Write-DryRun "Command: psql <connection-string> < $DumpFile"
    } else {
        Write-Warning "Database dump file not found. Skipping import."
        Write-Info "Import manually after database creation."
    }
}

# Step 6: Configure Environment Variables and Secrets
Write-Step "Step 6: Configuring Environment Variables"

if (-not $DryRun) {
    Write-Info "Setting environment variables for all services..."
    
    # Load environment variables from JSON
    if (Test-Path $EnvJsonPath) {
        $envConfig = Get-Content $EnvJsonPath | ConvertFrom-Json
        
        foreach ($service in $Services) {
            Write-Info "Configuring $($service.Name)..."
            
            try {
                # Set common environment variables
                $commonVars = @{
                    "ASPNETCORE_URLS" = "http://0.0.0.0:$($service.Port)"
                    "ASPNETCORE_ENVIRONMENT" = "Production"
                    "ConnectionStrings__Postgres" = "`${{db.$DbName.DATABASE_URL}}"
                }
                
                foreach ($key in $commonVars.Keys) {
                    render-cli env-vars set `
                        --service $service.Name `
                        --key $key `
                        --value $commonVars[$key]
                }
                
                # Set service-specific variables if defined
                if ($envConfig.Services.PSObject.Properties.Name -contains $service.Name) {
                    $serviceConfig = $envConfig.Services.$($service.Name)
                    foreach ($key in $serviceConfig.PSObject.Properties.Name) {
                        render-cli env-vars set `
                            --service $service.Name `
                            --key $key `
                            --value $serviceConfig.$key
                    }
                }
                
                Write-Success "Environment variables set for $($service.Name)"
            } catch {
                Write-Warning "Failed to set env vars for $($service.Name): $_"
            }
        }
        
        # Set database connection as service reference
        Write-Info "Linking database to services..."
        foreach ($service in $Services) {
            try {
                render-cli services link-database `
                    --service $service.Name `
                    --database $DbName
                Write-Success "Database linked to $($service.Name)"
            } catch {
                Write-Warning "Database linking may need to be done via dashboard for $($service.Name)"
            }
        }
    } else {
        Write-Warning "Environment config file not found: $EnvJsonPath"
        Write-Info "Please set environment variables manually in Render dashboard"
    }
} else {
    Write-DryRun "Would set environment variables for all services"
    Write-DryRun "Would load configuration from: $EnvJsonPath"
    Write-DryRun ""
    Write-DryRun "Common variables that would be set:"
    Write-DryRun "  - ASPNETCORE_URLS=http://0.0.0.0:<port>"
    Write-DryRun "  - ASPNETCORE_ENVIRONMENT=Production"
    Write-DryRun "  - ConnectionStrings__Postgres=`${{db.$DbName.DATABASE_URL}}"
}

# Step 7: Deploy Services
Write-Step "Step 7: Triggering Service Deployments"

if (-not $DryRun) {
    Write-Info "Services should auto-deploy from GitHub. Checking deployment status..."
    
    foreach ($service in $Services) {
        try {
            $deployments = render-cli deployments list --service $service.Name
            Write-Info "$($service.Name): Checking latest deployment..."
            
            # Trigger manual deploy if needed
            $latestDeploy = render-cli deployments list --service $service.Name --limit 1
            if ($latestDeploy) {
                Write-Success "$($service.Name): Deployment in progress or completed"
            } else {
                Write-Info "$($service.Name): Triggering deployment..."
                render-cli deployments create --service $service.Name
            }
        } catch {
            Write-Warning "$($service.Name): Deployment status check failed"
        }
    }
} else {
    Write-DryRun "Would trigger deployments for all services"
    Write-DryRun "Services will auto-deploy from GitHub after configuration"
}

# Step 8: Verify Deployment
Write-Step "Step 8: Verifying Deployment"

if (-not $DryRun) {
    Write-Info "Checking service health..."
    
    foreach ($service in $Services) {
        try {
            $serviceInfo = render-cli services show $service.Name --format json | ConvertFrom-Json
            $serviceUrl = $serviceInfo.serviceDetails.url
            
            Write-Info "Checking $($service.Name) at $serviceUrl..."
            
            # Wait a bit for services to be ready
            Start-Sleep -Seconds 2
            
            try {
                $response = Invoke-WebRequest -Uri "$serviceUrl/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
                if ($response.StatusCode -eq 200) {
                    Write-Success "$($service.Name): Healthy ✓"
                } else {
                    Write-Warning "$($service.Name): Responded with status $($response.StatusCode)"
                }
            } catch {
                Write-Warning "$($service.Name): Health check endpoint not responding (may still be deploying)"
            }
        } catch {
            Write-Warning "$($service.Name): Could not verify deployment status"
        }
    }
} else {
    Write-DryRun "Would verify all services are healthy"
    Write-DryRun "Would check: <service-url>/health endpoint"
}

# Step 9: Cost Optimization Setup
Write-Step "Step 9: Setting Up Cost Optimization"

if (-not $DryRun) {
    Write-Info "Configuring auto-suspend and budgets..."
    
    # This would typically be done via Render dashboard or API
    Write-Info "Note: Auto-suspend and budgets must be configured via Render dashboard:"
    Write-Info "  1. Go to each service → Settings → Auto-suspend"
    Write-Info "  2. Enable auto-suspend with 15 minute idle timeout"
    Write-Info "  3. Go to Account → Billing → Set budget alerts at $50"
    
    # Run cost optimization script
    if (Test-Path "scripts\migration\render-cost-optimizer.ps1") {
        Write-Info "Running cost optimization script..."
        & "scripts\migration\render-cost-optimizer.ps1" -DryRun:$DryRun
    }
} else {
    Write-DryRun "Would configure cost optimization settings"
    Write-DryRun "  - Auto-suspend: 15 minutes idle timeout"
    Write-DryRun "  - Budget alert: $50/month"
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MIGRATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "DRY-RUN MODE: No changes were made" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To execute the migration, run:" -ForegroundColor White
    Write-Host "  .\scripts\migration\render-migration.ps1" -ForegroundColor Green
} else {
    Write-Success "Migration steps completed!"
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify all services are running in Render dashboard" -ForegroundColor White
    Write-Host "  2. Update frontend API endpoints to point to Render URLs" -ForegroundColor White
    Write-Host "  3. Test all API endpoints" -ForegroundColor White
    Write-Host "  4. Monitor costs in Render dashboard" -ForegroundColor White
    Write-Host "  5. Once verified, update DNS/load balancer to point to Render" -ForegroundColor White
    Write-Host ""
    Write-Host "Rollback script available:" -ForegroundColor Yellow
    Write-Host "  .\scripts\migration\render-rollback.ps1" -ForegroundColor Green
}

Write-Host ""

