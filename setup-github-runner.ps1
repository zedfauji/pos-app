# GitHub Actions Self-Hosted Runner Setup Script for Windows x64
# This script downloads, verifies, and configures a GitHub Actions runner

param(
    [Parameter(Mandatory=$true)]
    [string]$RunnerName,
    
    [Parameter(Mandatory=$true)]
    [string]$GitHubToken,
    
    [Parameter(Mandatory=$false)]
    [string]$GitHubRepo = "zedfauji/pos-app",
    
    [Parameter(Mandatory=$false)]
    [string]$RunnerVersion = "2.329.0",
    
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "C:\actions-runner"
)

$ErrorActionPreference = "Stop"

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg) { Write-Host "[ERR ] $msg" -ForegroundColor Red }
function Write-Success($msg) { Write-Host "[OK  ] $msg" -ForegroundColor Green }

Write-Info "=== GitHub Actions Self-Hosted Runner Setup ==="
Write-Info "Runner Name: $RunnerName"
Write-Info "Repository: $GitHubRepo"
Write-Info "Version: $RunnerVersion"
Write-Info "Install Path: $InstallPath"

# Step 1: Create installation directory
Write-Info "`nStep 1: Creating installation directory..."
if (Test-Path $InstallPath) {
    Write-Warn "Directory already exists: $InstallPath"
    $overwrite = Read-Host "Do you want to remove it and start fresh? (y/N)"
    if ($overwrite -eq 'y' -or $overwrite -eq 'Y') {
        Remove-Item -Path $InstallPath -Recurse -Force
        Write-Success "Removed existing directory"
    } else {
        Write-Err "Installation cancelled"
        exit 1
    }
}
New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
Write-Success "Created directory: $InstallPath"

# Step 2: Download runner
Write-Info "`nStep 2: Downloading runner..."
$zipFile = "actions-runner-win-x64-$RunnerVersion.zip"
$zipPath = Join-Path $env:TEMP $zipFile
$downloadUrl = "https://github.com/actions/runner/releases/download/v$RunnerVersion/$zipFile"

Write-Info "Download URL: $downloadUrl"
Write-Info "Downloading to: $zipPath"

try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -UseBasicParsing
    Write-Success "Downloaded runner zip file"
} catch {
    Write-Err "Failed to download runner: $_"
    exit 1
}

# Step 3: Verify checksum
Write-Info "`nStep 3: Verifying checksum..."
$expectedHash = "f60be5ddf373c52fd735388c3478536afd12bfd36d1d0777c6b855b758e70f25"
$actualHash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToUpper()

if ($actualHash -ne $expectedHash.ToUpper()) {
    Write-Err "Checksum verification failed!"
    Write-Err "Expected: $expectedHash"
    Write-Err "Actual:   $actualHash"
    Remove-Item -Path $zipPath -Force
    exit 1
}
Write-Success "Checksum verified successfully"

# Step 4: Extract runner
Write-Info "`nStep 4: Extracting runner..."
try {
    Expand-Archive -Path $zipPath -DestinationPath $InstallPath -Force
    Write-Success "Extracted runner files"
} catch {
    Write-Err "Failed to extract runner: $_"
    exit 1
} finally {
    # Clean up zip file
    Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
}

# Step 5: Configure runner
Write-Info "`nStep 5: Configuring runner..."
Set-Location $InstallPath

try {
    # Run config command
    $configArgs = @(
        "configure",
        "--url", "https://github.com/$GitHubRepo",
        "--token", $GitHubToken,
        "--name", $RunnerName,
        "--work", "_work",
        "--replace"
    )
    
    Write-Info "Running: .\config.cmd $($configArgs -join ' ')"
    & .\config.cmd @configArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Runner configuration failed with exit code: $LASTEXITCODE"
        exit 1
    }
    
    Write-Success "Runner configured successfully"
} catch {
    Write-Err "Failed to configure runner: $_"
    exit 1
}

# Step 6: Install as Windows Service (optional)
Write-Info "`nStep 6: Installing as Windows Service..."
$installService = Read-Host "Do you want to install the runner as a Windows Service? (Y/n)"
if ($installService -ne 'n' -and $installService -ne 'N') {
    try {
        & .\svc.cmd install
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Runner installed as Windows Service"
            
            $startService = Read-Host "Do you want to start the service now? (Y/n)"
            if ($startService -ne 'n' -and $startService -ne 'N') {
                & .\svc.cmd start
                Write-Success "Runner service started"
            }
        } else {
            Write-Warn "Service installation may have failed. Check output above."
        }
    } catch {
        Write-Warn "Failed to install service: $_"
        Write-Info "You can install it manually later by running: .\svc.cmd install"
    }
} else {
    Write-Info "Skipping service installation. Run manually with: .\svc.cmd install"
}

# Step 7: Summary
Write-Info "`n=== Setup Complete ==="
Write-Success "Runner installed at: $InstallPath"
Write-Info "Runner name: $RunnerName"
Write-Info "Repository: $GitHubRepo"
Write-Info "`nTo start the runner manually, run:"
Write-Info "  cd $InstallPath"
Write-Info "  .\run.cmd"
Write-Info "`nTo install as service later:"
Write-Info "  cd $InstallPath"
Write-Info "  .\svc.cmd install"
Write-Info "  .\svc.cmd start"
Write-Info "`nTo remove the runner:"
Write-Info "  cd $InstallPath"
Write-Info "  .\config.cmd remove --token <TOKEN>"

Set-Location $PSScriptRoot
