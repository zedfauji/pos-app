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

Write-Host "Publishing frontend to: $publishDir"
& dotnet publish $csproj -c $Configuration -f $Framework -r $Runtime --self-contained false -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false -p:PublishTrimmed=false -p:WindowsAppSDKSelfContained=false -p:Platform=x64 /nologo | Write-Host

if (!(Test-Path $publishDir)) { throw "Publish output not found: $publishDir" }

# Create redist directory and download Windows App Runtime
$redistDir = Join-Path $PSScriptRoot 'redist'
if (!(Test-Path $redistDir)) {
    New-Item -Path $redistDir -ItemType Directory -Force | Out-Null
}

# Download Windows App Runtime 1.7 (more stable than 1.8)
$runtimeUrl = "https://aka.ms/windowsappruntime/1.7/desktop/x64/msix"
$runtimeFile = Join-Path $redistDir "Microsoft.WindowsAppRuntime.1.7_x64.msix"

if (!(Test-Path $runtimeFile)) {
    Write-Host "Downloading Windows App Runtime 1.7..."
    try {
        Invoke-WebRequest -Uri $runtimeUrl -OutFile $runtimeFile -UseBasicParsing
        Write-Host "Windows App Runtime downloaded successfully"
    } catch {
        Write-Warning "Failed to download Windows App Runtime: $_"
        Write-Host "Continuing without runtime prerequisite..."
        $runtimeFile = $null
    }
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
if (-not $ISCC) { throw "Could not find ISCC.exe. Please install Inno Setup 6 and/or add ISCC.exe to PATH." }

# Output dir = Desktop\MagiDesk Setup
$setupDir = Join-Path ([Environment]::GetFolderPath('Desktop')) 'MagiDesk Setup'
$iss = Join-Path $PSScriptRoot 'installer-with-runtime.iss'
if (!(Test-Path $iss)) { throw "installer-with-runtime.iss not found at $iss" }

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

$outputFilename = "MagiDeskSetup-v$versionNumber-WithRuntime"
$outputPath = Join-Path $setupDir "$outputFilename.exe"

Write-Host "Compiling installer via ISCC..."
Write-Host "Output: $outputPath"

# Pass redist directory to ISCC if runtime file exists
$redistParam = if ($runtimeFile) { "/DRedistDir=$redistDir" } else { "/DRedistDir=" }

& "$ISCC" "/DSourceDir=$publishDir" $redistParam "/DAppVersion=$Version" "/DOutputDir=$setupDir" "/DOutputBaseFilename=$outputFilename" "$iss" | Write-Host

Write-Host "Installer built to: $outputPath"
