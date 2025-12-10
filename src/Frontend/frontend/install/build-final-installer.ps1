param(
    [string]$Configuration = "Release",
    [string]$Framework = "net8.0-windows10.0.19041.0",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path "$PSScriptRoot\..\").Path
$csproj = Join-Path $root 'MagiDesk.Frontend.csproj'
$publishDir = Join-Path $root "publish"
$redistDir = Join-Path $PSScriptRoot "redist"

Write-Host "=== MagiDesk Final Installer Build ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Framework: $Framework" -ForegroundColor Yellow
Write-Host "Runtime: $Runtime" -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor Yellow

# Create redist directory if it doesn't exist
if (!(Test-Path $redistDir)) {
    New-Item -Path $redistDir -ItemType Directory -Force | Out-Null
    Write-Host "✓ Created redist directory: $redistDir" -ForegroundColor Green
}

# Download WebView2 Bootstrapper if not present
$webView2Bootstrapper = Join-Path $redistDir "Microsoft.WebView2.Bootstrapper.exe"
if (!(Test-Path $webView2Bootstrapper)) {
    Write-Host "Downloading WebView2 Bootstrapper..." -ForegroundColor Yellow
    $webView2Url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    try {
        Invoke-WebRequest -Uri $webView2Url -OutFile $webView2Bootstrapper -UseBasicParsing
        Write-Host "✓ Downloaded WebView2 Bootstrapper" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠ Warning: Failed to download WebView2 Bootstrapper: $_" -ForegroundColor Yellow
        Write-Host "  The installer will still work, but WebView2 components may not function." -ForegroundColor Yellow
        Write-Host "  Users can install WebView2 manually if needed." -ForegroundColor Yellow
    }
} else {
    Write-Host "✓ WebView2 Bootstrapper already exists" -ForegroundColor Green
}

# Verify publish directory exists and contains critical files
if (!(Test-Path $publishDir)) {
    throw "Publish directory not found: $publishDir. Run 'dotnet publish' first."
}

$criticalFiles = @(
    "MagiDesk.Frontend.exe",
    "Microsoft.UI.Xaml.dll",
    "Microsoft.WindowsAppRuntime.dll"
)

foreach ($file in $criticalFiles) {
    $filePath = Join-Path $publishDir $file
    if (!(Test-Path $filePath)) {
        throw "Critical file missing: $filePath"
    }
    Write-Host "✓ Found: $file" -ForegroundColor Green
}

# Locate Inno Setup Compiler (ISCC)
function Get-ISCCPath {
    $candidates = @(
        "$Env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$Env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "$Env:ProgramFiles(x86)\Inno Setup 5\ISCC.exe",
        "$Env:ProgramFiles\Inno Setup 5\ISCC.exe"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    
    # Try registry
    $regPaths = @(
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1',
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1'
    )
    foreach ($rp in $regPaths) {
        try {
            $loc = (Get-ItemProperty -Path $rp -ErrorAction Stop).InstallLocation
            if ($loc) {
                $tryPath = Join-Path $loc 'ISCC.exe'
                if (Test-Path $tryPath) { return $tryPath }
            }
        } catch {}
    }
    
    # Last chance: PATH
    $which = (Get-Command ISCC.exe -ErrorAction SilentlyContinue)
    if ($which) { return $which.Source }
    return $null
}

$ISCC = Get-ISCCPath
if (-not $ISCC) { 
    throw "Could not find ISCC.exe. Please install Inno Setup 6 and/or add ISCC.exe to PATH." 
}

Write-Host "✓ Found ISCC: $ISCC" -ForegroundColor Green

# Output dir = Desktop\MagiDesk Setup
$setupDir = Join-Path ([Environment]::GetFolderPath('Desktop')) 'MagiDesk Setup'
$iss = Join-Path $PSScriptRoot 'installer.iss'

if (!(Test-Path $iss)) { throw "installer.iss not found at $iss" }

# Create setup directory if it doesn't exist
if (!(Test-Path $setupDir)) {
    New-Item -Path $setupDir -ItemType Directory -Force | Out-Null
}

# Generate incremental version number
$existingFiles = Get-ChildItem -Path $setupDir -Filter "MagiDeskSetup*.exe" -ErrorAction SilentlyContinue
$versionNumber = 1
if ($existingFiles) {
    $maxVersion = 0
    foreach ($file in $existingFiles) {
        if ($file.Name -match "MagiDeskSetup-v(\d+)") {
            $fileVersion = [int]$matches[1]
            if ($fileVersion -gt $maxVersion) { $maxVersion = $fileVersion }
        }
    }
    $versionNumber = $maxVersion + 1
}

$outputFilename = "MagiDeskSetup-v$versionNumber-Final"
$outputPath = Join-Path $setupDir "$outputFilename.exe"

Write-Host "Compiling installer via ISCC..." -ForegroundColor Yellow
Write-Host "Output: $outputPath" -ForegroundColor Cyan

# Build installer with WebView2 bootstrapper if available
$isccParams = @(
    "/DSourceDir=$publishDir",
    "/DAppVersion=$Version",
    "/DOutputDir=$setupDir",
    "/DOutputBaseFilename=$outputFilename"
)

# Add redist directory if WebView2 bootstrapper exists
if (Test-Path $webView2Bootstrapper) {
    $isccParams += "/DRedistDir=$redistDir"
    Write-Host "✓ Including WebView2 Bootstrapper in installer" -ForegroundColor Green
} else {
    $isccParams += "/DRedistDir="
    Write-Host "⚠ WebView2 Bootstrapper not included (not found)" -ForegroundColor Yellow
}

$isccParams += $iss

& "$ISCC" $isccParams

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Installer built successfully: $outputPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Installation Instructions ===" -ForegroundColor Yellow
    Write-Host "1. Run the installer: $outputPath" -ForegroundColor White
    Write-Host "2. The installer will check system requirements (Windows 10 1809+)" -ForegroundColor White
    Write-Host "3. The installer will install MagiDesk to Program Files" -ForegroundColor White
    Write-Host "4. All Windows App Runtime DLLs are bundled (self-contained)" -ForegroundColor White
    Write-Host "5. WebView2 Runtime will be installed automatically if needed" -ForegroundColor White
    Write-Host "6. No additional runtime installation required" -ForegroundColor White
    Write-Host ""
    Write-Host "=== Test Instructions ===" -ForegroundColor Yellow
    Write-Host "1. Install on a clean machine (no .NET or WinUI 3 SDK)" -ForegroundColor White
    Write-Host "2. Launch MagiDesk from Start Menu or Desktop shortcut" -ForegroundColor White
    Write-Host "3. If it crashes, check Windows Event Log for details" -ForegroundColor White
} else {
    throw "ISCC compilation failed with exit code $LASTEXITCODE"
}
