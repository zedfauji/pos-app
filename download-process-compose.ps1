# Download Process Compose for Windows
# This script downloads Process Compose and places it in the project root

$ErrorActionPreference = "Stop"

Write-Host "Downloading Process Compose..." -ForegroundColor Cyan

# Get latest release info
$latestReleaseUrl = "https://api.github.com/repos/F1bonacc1/process-compose/releases/latest"
$releaseInfo = Invoke-RestMethod -Uri $latestReleaseUrl

# Find Windows AMD64 binary
$windowsAsset = $releaseInfo.assets | Where-Object { $_.name -like "*windows*amd64*" -or $_.name -like "*windows*amd64*" }

if (-not $windowsAsset) {
    # Try alternative naming
    $windowsAsset = $releaseInfo.assets | Where-Object { $_.name -like "*windows*" -and $_.name -like "*amd64*" }
}

if (-not $windowsAsset) {
    Write-Host "ERROR: Could not find Windows binary in release assets." -ForegroundColor Red
    Write-Host "Please download manually from: https://github.com/F1bonacc1/process-compose/releases" -ForegroundColor Yellow
    exit 1
}

$downloadUrl = $windowsAsset.browser_download_url
$outputFile = "process-compose.exe"

Write-Host "Found release: $($releaseInfo.tag_name)" -ForegroundColor Green
Write-Host "Downloading from: $downloadUrl" -ForegroundColor Gray

try {
    # Download the file
    Invoke-WebRequest -Uri $downloadUrl -OutFile $outputFile -UseBasicParsing
    
    Write-Host "`n✅ Download complete!" -ForegroundColor Green
    Write-Host "Process Compose saved as: $outputFile" -ForegroundColor Green
    Write-Host "`nYou can now run: process-compose up" -ForegroundColor Cyan
}
catch {
    Write-Host "`n❌ Download failed: $_" -ForegroundColor Red
    Write-Host "Please download manually from: https://github.com/F1bonacc1/process-compose/releases" -ForegroundColor Yellow
    exit 1
}

