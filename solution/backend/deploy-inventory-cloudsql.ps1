param(
  [Parameter(Mandatory=$true)][string]$InstanceConnectionName,
  [Parameter(Mandatory=$true)][string]$Database,
  [Parameter(Mandatory=$true)][string]$Username,
  [Parameter(Mandatory=$true)][string]$Password
)

$ErrorActionPreference = 'Stop'

Write-Host "Applying inventory schema to CloudSQL instance $InstanceConnectionName ..."

# Requires Cloud SDK and psql client available on runner/container.
# Uses unix socket path style for Cloud Run / Cloud Build; adjust for your environment if using TCP.

$env:PGPASSWORD = $Password

$schemaPath = (Join-Path $PSScriptRoot 'InventoryApi/Database/schema.sql')

try {
  # Try unix socket (works on Cloud Build/Run or when socket mounted)
  & psql "host=/cloudsql/$InstanceConnectionName dbname=$Database user=$Username sslmode=disable" -v ON_ERROR_STOP=1 -f $schemaPath
  if ($LASTEXITCODE -eq 0) { Write-Host "Schema applied successfully via unix socket."; return }
} catch { }

Write-Host "Unix socket apply failed or unavailable. Falling back to gcloud sql tunnel..."

# Use gcloud sql connect to open a TCP tunnel and pass file to psql
# Note: gcloud will invoke psql; we pass -f schema via extra args
& gcloud.cmd sql connect $InstanceConnectionName --user=$Username --quiet -- -v ON_ERROR_STOP=1 -f $schemaPath
if ($LASTEXITCODE -ne 0) { throw "Schema apply failed with code $LASTEXITCODE" }

Write-Host "Schema applied successfully."

