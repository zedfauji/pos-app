param(
    [Parameter(Mandatory=$false)][string]$ProjectId,
    [Parameter(Mandatory=$false)][string]$Region = "northamerica-south1",
    [Parameter(Mandatory=$false)][string]$ServiceName = "magidesk-backend"
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[ERR ] $msg" -ForegroundColor Red }

# Validate gcloud availability (prefer gcloud.cmd to avoid PS policy issues)
if (-not (Get-Command gcloud.cmd -ErrorAction SilentlyContinue)) {
    Write-Err "gcloud CLI not found. Install Google Cloud SDK: https://cloud.google.com/sdk/docs/install"
    exit 1
}

# Resolve repo root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path  # solution/backend
$BackendRoot = Join-Path $ScriptDir "MagiDesk.Backend"
$BackendContext = $ScriptDir                           # solution/backend
$SolutionRoot = Split-Path $BackendContext -Parent     # solution/
$DockerfilePath = Join-Path $BackendRoot "Dockerfile"
$TempDockerfile = Join-Path $SolutionRoot "Dockerfile" # copy to solution root
$ServiceAccountPath = Join-Path (Join-Path $BackendContext "config") "serviceAccount.json"

if (-not (Test-Path $DockerfilePath)) {
    Write-Err "Dockerfile not found at $DockerfilePath"
    exit 1
}

if (-not (Test-Path $ServiceAccountPath)) {
    Write-Warn "Service account JSON not found at $ServiceAccountPath. The image build will fail to COPY the credentials."
}

# Get project id if missing
if (-not $ProjectId -or [string]::IsNullOrWhiteSpace($ProjectId)) {
    Write-Err "ProjectId is required. Re-run with: -ProjectId <YOUR_PROJECT_ID>"
    exit 1
}

# Validate & sanitize service name
if ([string]::IsNullOrWhiteSpace($ServiceName)) { $ServiceName = "magidesk-backend" }
$ServiceName = ($ServiceName.ToLower() -replace "[^a-z0-9-]", "-")
if ([string]::IsNullOrWhiteSpace($ServiceName)) { $ServiceName = "magidesk-backend" }
Write-Info "Using service name: $ServiceName"

# Quick auth check
try {
    $acct = (& gcloud.cmd auth list --filter=status:ACTIVE --format="value(account)")
    if (-not $acct) { Write-Warn "No active gcloud account detected. If deploy fails, run 'gcloud auth login' and re-run." }
    elseif ($acct) { Write-Info "Active gcloud account: $acct" }
} catch { Write-Warn "Could not check gcloud auth. If deploy fails, run 'gcloud auth login'. Details: $_" }

Write-Info "Setting project to $ProjectId and region to $Region"
& gcloud.cmd config set project $ProjectId | Out-Null
& gcloud.cmd config set run/region $Region | Out-Null

# Build container with Cloud Build
$Image = "gcr.io/${ProjectId}/${ServiceName}:latest"
Write-Info "Resolved Image: $Image"
Write-Info "Build Context (solution root): $SolutionRoot"
Write-Info "Preparing Dockerfile in build context (solution root)..."
Copy-Item -Path $DockerfilePath -Destination $TempDockerfile -Force
Write-Info "Submitting build to Cloud Build..."
& gcloud.cmd builds submit --project $ProjectId --tag $Image "$SolutionRoot"

if ($LASTEXITCODE -ne 0) { Write-Err "Cloud Build failed."; Remove-Item -ErrorAction SilentlyContinue $TempDockerfile; exit 1 }

# Deploy to Cloud Run
Write-Info "Deploying to Cloud Run service '$ServiceName' in region '$Region'..."
 $ResendKey = "re_Cd4wzdzB_7vCexwABGJQqNPjqvF9zKvqS"
 $EnvVars = "ASPNETCORE_URLS=http://0.0.0.0:8080,ASPNETCORE_ENVIRONMENT=Production,RESEND_API_KEY=$ResendKey"
& gcloud.cmd run deploy $ServiceName `
    --image $Image `
    --region $Region `
    --platform managed `
    --allow-unauthenticated `
    --port 8080 `
    --set-env-vars $EnvVars

if ($LASTEXITCODE -ne 0) { Write-Err "Cloud Run deployment failed."; Remove-Item -ErrorAction SilentlyContinue $TempDockerfile; exit 1 }

# Fetch service URL
$Url = (& gcloud.cmd run services describe $ServiceName --region $Region --format="value(status.url)")
Write-Info "Deployed: $Url"
Remove-Item -ErrorAction SilentlyContinue $TempDockerfile
