# Render API-Based Migration Script (Windows Compatible)
# Uses Render REST API instead of CLI

param(
    [switch]$DryRun = $false,
    [string]$RenderApiKey = "",
    [string]$GitHubRepo = "",
    [string]$DumpFile = "dumps\postgres-dump-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql"
)

$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "`n[STEP] $msg" -ForegroundColor Cyan }
function Write-Info { param($msg) Write-Host "  [INFO] $msg" -ForegroundColor Gray }
function Write-Success { param($msg) Write-Host "  [✓] $msg" -ForegroundColor Green }
function Write-Warning { param($msg) Write-Host "  [⚠] $msg" -ForegroundColor Yellow }
function Write-Error-Custom { param($msg) Write-Host "  [✗] $msg" -ForegroundColor Red }
function Write-DryRun { param($msg) Write-Host "  [DRY-RUN] $msg" -ForegroundColor Magenta }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RENDER API MIGRATION SCRIPT" -ForegroundColor Cyan
Write-Host "  (Windows Compatible - No CLI Required)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Mode: $(if ($DryRun) { 'DRY-RUN' } else { 'EXECUTE' })" -ForegroundColor $(if ($DryRun) { 'Yellow' } else { 'Green' })
Write-Host ""

# Get API key
if (-not $DryRun) {
    if (-not $RenderApiKey) {
        $RenderApiKey = Read-Host "Enter your Render API key" -AsSecureString
        $RenderApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($RenderApiKey)
        )
        if (-not $RenderApiKey) {
            Write-Error-Custom "API key is required. Get it from: https://dashboard.render.com/account/api-keys"
            exit 1
        }
    }
}

$baseUrl = "https://api.render.com/v1"
$headers = @{
    "Authorization" = "Bearer $RenderApiKey"
    "Accept" = "application/json"
    "Content-Type" = "application/json"
}

$Services = @(
    @{ Name = "TablesApi"; Path = "src/Backend/TablesApi" },
    @{ Name = "OrderApi"; Path = "src/Backend/OrderApi" },
    @{ Name = "PaymentApi"; Path = "src/Backend/PaymentApi" },
    @{ Name = "MenuApi"; Path = "src/Backend/MenuApi" },
    @{ Name = "CustomerApi"; Path = "src/Backend/CustomerApi" },
    @{ Name = "DiscountApi"; Path = "src/Backend/DiscountApi" },
    @{ Name = "InventoryApi"; Path = "src/Backend/InventoryApi" },
    @{ Name = "SettingsApi"; Path = "src/Backend/SettingsApi" },
    @{ Name = "UsersApi"; Path = "src/Backend/UsersApi" }
)

# Step 1: Verify API Key
Write-Step "Step 1: Verifying Render API Key"

if (-not $DryRun) {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/owners" -Headers $headers -Method Get
        Write-Success "API key verified. Owner: $($response[0].name)"
    } catch {
        Write-Error-Custom "API key verification failed: $_"
        Write-Info "Get your API key from: https://dashboard.render.com/account/api-keys"
        exit 1
    }
} else {
    Write-DryRun "Would verify API key"
}

# Step 2: Create Services via Blueprint (Recommended)
Write-Step "Step 2: Deploying via Blueprint"

Write-Info "Recommended: Use Render Dashboard to deploy from render.yaml"
Write-Info "  1. Go to: https://dashboard.render.com"
Write-Info "  2. Click 'New' → 'Blueprint'"
Write-Info "  3. Connect your GitHub repo"
Write-Info "  4. Select render.yaml file"
Write-Info "  5. Click 'Apply'"
Write-Host ""
Write-Info "Alternative: Create services individually via API (see next steps)"

# Step 3: Create Services via API
Write-Step "Step 3: Creating Services via API (Alternative)"

if (-not $DryRun -and (Read-Host "Create services via API? (y/n) [n]") -eq 'y') {
    Write-Info "Note: Blueprint method is recommended. API method shown for reference."
    
    foreach ($service in $Services) {
        Write-Info "Creating $($service.Name)..."
        
        $serviceBody = @{
            type = "web_service"
            name = $service.Name
            env = "dotnet"
            plan = "starter"
            region = "oregon"
            repo = $GitHubRepo
            branch = "main"
            buildCommand = "dotnet publish $($service.Path)/$($service.Name).csproj -c Release -o ./publish"
            startCommand = "dotnet ./publish/$($service.Name).dll"
            envVars = @(
                @{
                    key = "ASPNETCORE_URLS"
                    value = "http://0.0.0.0:10000"
                },
                @{
                    key = "ASPNETCORE_ENVIRONMENT"
                    value = "Production"
                }
            )
        } | ConvertTo-Json -Depth 10
        
        try {
            $response = Invoke-RestMethod -Uri "$baseUrl/services" -Headers $headers -Method Post -Body $serviceBody
            Write-Success "$($service.Name) created: $($response.service.serviceDetails.url)"
        } catch {
            Write-Warning "Failed to create $($service.Name) via API: $_"
            Write-Info "Create manually via Dashboard or use Blueprint method"
        }
    }
} else {
    if ($DryRun) {
        Write-DryRun "Would create services via API"
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  NEXT STEPS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Since Render CLI has limited Windows support, use:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. DASHBOARD METHOD (Recommended):" -ForegroundColor Green
Write-Host "   - Go to https://dashboard.render.com" -ForegroundColor White
Write-Host "   - New → Blueprint → Connect GitHub" -ForegroundColor White
Write-Host "   - Select render.yaml → Apply" -ForegroundColor White
Write-Host ""
Write-Host "2. API METHOD:" -ForegroundColor Green
Write-Host "   - Get API key: https://dashboard.render.com/account/api-keys" -ForegroundColor White
Write-Host "   - Use this script with -RenderApiKey parameter" -ForegroundColor White
Write-Host ""
Write-Host "3. DATABASE IMPORT:" -ForegroundColor Green
Write-Host "   - After DB is created, import dump:" -ForegroundColor White
Write-Host "   - psql -h <db-host> -U <user> -d postgres -f $DumpFile" -ForegroundColor White
Write-Host ""

