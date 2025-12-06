param(
    [Parameter(Mandatory=$false)][string]$ProjectId,
    [Parameter(Mandatory=$false)][string]$Region = "northamerica-south1",
    [Parameter(Mandatory=$false)][string]$ServiceName = "magidesk-order",
    [Parameter(Mandatory=$false)][string]$CloudSqlInstance
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[ERR ] $msg" -ForegroundColor Red }

if (-not (Get-Command gcloud.cmd -ErrorAction SilentlyContinue)) {
    Write-Err "gcloud CLI not found. Install Google Cloud SDK: https://cloud.google.com/sdk/docs/install"
    exit 1
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path  # solution/backend
$SvcRoot = Join-Path $ScriptDir "OrderApi"
$SolutionRoot = Split-Path $ScriptDir -Parent                 # solution/
$DockerfilePath = Join-Path $SvcRoot "Dockerfile"
$TempDockerfile = Join-Path $SolutionRoot "Dockerfile"        # copy to solution root

if (-not (Test-Path $DockerfilePath)) { Write-Err "Dockerfile not found at $DockerfilePath"; exit 1 }
if (-not $ProjectId -or [string]::IsNullOrWhiteSpace($ProjectId)) { Write-Err "ProjectId is required. Re-run with: -ProjectId <YOUR_PROJECT_ID>"; exit 1 }

if ([string]::IsNullOrWhiteSpace($ServiceName)) { $ServiceName = "magidesk-order" }
$ServiceName = ($ServiceName.ToLower() -replace "[^a-z0-9-]", "-")
Write-Info "Using service name: $ServiceName"

try {
    $acct = (& gcloud.cmd auth list --filter=status:ACTIVE --format="value(account)")
    if (-not $acct) { Write-Warn "No active gcloud account detected. If deploy fails, run 'gcloud auth login' and re-run." }
    elseif ($acct) { Write-Info "Active gcloud account: $acct" }
} catch { Write-Warn "Could not check gcloud auth. If deploy fails, run 'gcloud auth login'. Details: $_" }

Write-Info "Setting project to $ProjectId and region to $Region"
& gcloud.cmd config set project $ProjectId | Out-Null
& gcloud.cmd config set run/region $Region | Out-Null

$Image = "gcr.io/${ProjectId}/${ServiceName}:latest"
Write-Info "Resolved Image: $Image"
Write-Info "Build Context (solution root): $SolutionRoot"
Write-Info "Preparing Dockerfile in build context (solution root)..."
Copy-Item -Path $DockerfilePath -Destination $TempDockerfile -Force

Write-Info "Submitting build to Cloud Build..."
& gcloud.cmd builds submit --project $ProjectId --tag $Image "$SolutionRoot"
if ($LASTEXITCODE -ne 0) { Write-Err "Cloud Build failed."; Remove-Item -ErrorAction SilentlyContinue $TempDockerfile; exit 1 }

Write-Info "Deploying to Cloud Run service '$ServiceName' in region '$Region'..."
$envPairs = @(
    "ASPNETCORE_URLS=http://0.0.0.0:8080",
    "ASPNETCORE_ENVIRONMENT=Production",
    "UsersApi:BaseUrl=https://magidesk-users-904541739138.northamerica-south1.run.app"
)
$EnvVars = ($envPairs -join ",")
$deployArgs = @(
    'run','deploy',$ServiceName,
    '--image',$Image,
    '--region',$Region,
    '--platform','managed',
    '--allow-unauthenticated',
    '--port','8080',
    '--set-env-vars',$EnvVars
)
if ($CloudSqlInstance -and $CloudSqlInstance.Trim().Length -gt 0) {
    Write-Info "Attaching Cloud SQL instance $CloudSqlInstance"
    $deployArgs += @('--add-cloudsql-instances', $CloudSqlInstance)
}
& gcloud.cmd @deployArgs
if ($LASTEXITCODE -ne 0) { Write-Err "Cloud Run deployment failed."; Remove-Item -ErrorAction SilentlyContinue $TempDockerfile; exit 1 }

$Url = (& gcloud.cmd run services describe $ServiceName --region $Region --format="value(status.url)")
Write-Info "Deployed: $Url"
Remove-Item -ErrorAction SilentlyContinue $TempDockerfile
