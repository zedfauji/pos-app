# MagiDesk POS System - MSIX Installer Build Script
# This script builds the MSIX package for distribution

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64"
)

Write-Host "Building MagiDesk POS System Installer..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Platform: $Platform" -ForegroundColor Yellow

# Check if required tools are available
$msbuildPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
}
if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $msbuildPath)) {
    Write-Error "MSBuild not found. Please install Visual Studio 2022 or Build Tools."
    exit 1
}

# Set working directory to script location
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Recurse -Force "bin"
}
if (Test-Path "obj") {
    Remove-Item -Recurse -Force "obj"
}

# Build the frontend project first
Write-Host "Building frontend project..." -ForegroundColor Yellow
& $msbuildPath "..\frontend\MagiDesk.Frontend.csproj" /p:Configuration=$Configuration /p:Platform=$Platform /p:RestorePackages=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Frontend build failed!"
    exit 1
}

# Build the packaging project
Write-Host "Building MSIX package..." -ForegroundColor Yellow
& $msbuildPath "MagiDesk.Package.wapproj" /p:Configuration=$Configuration /p:Platform=$Platform /p:UapAppxPackageBuildMode=StoreUpload /p:AppxBundle=Always

if ($LASTEXITCODE -ne 0) {
    Write-Error "Package build failed!"
    exit 1
}

# Find the generated MSIX file
$packagePath = "bin\$Configuration\MagiDesk.Package_1.0.0.0_$Platform.msix"
if (Test-Path $packagePath) {
    Write-Host "✅ MSIX package created successfully!" -ForegroundColor Green
    Write-Host "Package location: $packagePath" -ForegroundColor Cyan
    
    # Get file size
    $fileSize = (Get-Item $packagePath).Length / 1MB
    Write-Host "Package size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
} else {
    Write-Warning "MSIX package not found at expected location: $packagePath"
    
    # List all generated files
    Write-Host "Generated files:" -ForegroundColor Yellow
    Get-ChildItem -Recurse "bin\$Configuration" -Include "*.msix", "*.appx" | ForEach-Object {
        Write-Host "  $($_.FullName)" -ForegroundColor Gray
    }
}

Write-Host "Build process completed!" -ForegroundColor Green
Write-Host ""
Write-Host "To install the package:" -ForegroundColor Yellow
Write-Host "1. Enable Developer Mode in Windows Settings" -ForegroundColor White
Write-Host "2. Double-click the .msix file to install" -ForegroundColor White
Write-Host "3. Or use PowerShell: Add-AppxPackage -Path `"$packagePath`"" -ForegroundColor White
