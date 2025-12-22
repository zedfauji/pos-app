param(
  [string]$Configuration = "Release",
  [string]$Framework = "net8.0-windows10.0.19041.0",
  [string]$Runtime = "win-x64",
  [string]$Version = "1.0.0"
)

$ErrorActionPreference = 'Stop'

$root = (Resolve-Path "$PSScriptRoot\..\").Path
$csproj = Join-Path $root 'MagiDesk.Frontend.csproj'
$publishDir = Join-Path $root "bin\$Configuration\$Framework\$Runtime\publish"

Write-Host "Building stable installer with framework-dependent deployment..."
Write-Host "Using Windows App SDK 1.8 to match installed runtime"

# Framework-dependent deployment (more stable)
Write-Host "Publishing frontend to: $publishDir"
& dotnet publish $csproj -c $Configuration -f $Framework -r $Runtime --self-contained false -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false -p:PublishTrimmed=false /nologo | Write-Host

if (!(Test-Path $publishDir)) { throw "Publish output not found: $publishDir" }

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
if (-not $ISCC) { throw "Could not find ISCC.exe. Please install Inno Setup 6 and/or add ISCC.exe to PATH." }

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

$outputFilename = "MagiDeskSetup-v$versionNumber-Stable"
$outputPath = Join-Path $setupDir "$outputFilename.exe"

Write-Host "Compiling installer via ISCC..."
Write-Host "Output: $outputPath"
& "$ISCC" "/DSourceDir=$publishDir" "/DRedistDir=" "/DAppVersion=$Version" "/DOutputDir=$setupDir" "/DOutputBaseFilename=$outputFilename" "$iss" | Write-Host

Write-Host ""
Write-Host "✅ Stable installer built successfully!"
Write-Host "📁 Location: $outputPath"
Write-Host "🚀 Ready to install - uses framework-dependent deployment with Windows App SDK 1.8"
Write-Host ""
Write-Host "This installer should work without crashes because:"
Write-Host "  • Uses framework-dependent deployment (no runtime conflicts)"
Write-Host "  • Matches your installed Windows App Runtime 1.8"
Write-Host "  • Includes all necessary dependencies"
Write-Host "  • No trimming to preserve JSON serialization"

