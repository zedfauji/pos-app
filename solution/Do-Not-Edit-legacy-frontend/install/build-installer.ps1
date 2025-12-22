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
$syncRoot = (Resolve-Path "$PSScriptRoot\..\..\sync\SyncWorker").Path
$syncProj = Join-Path $syncRoot 'MagiDesk.SyncWorker.csproj'
$syncPublish = Join-Path $syncRoot "bin\$Configuration\net8.0\win-x64\publish"
${redistDir} = Join-Path $PSScriptRoot 'redist'

Write-Host "Publishing frontend to: $publishDir"
& dotnet publish $csproj -c $Configuration -f $Framework -r $Runtime --self-contained true -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false /nologo | Write-Host

if (!(Test-Path $publishDir)) { throw "Publish output not found: $publishDir" }

Write-Host "Publishing SyncWorker to: $syncPublish"
& dotnet publish $syncProj -c $Configuration -r win-x64 --self-contained true /nologo | Write-Host
if (!(Test-Path $syncPublish)) { throw "SyncWorker publish output not found: $syncPublish" }

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

# Output dir = Desktop
$desktop = [Environment]::GetFolderPath('Desktop')
$iss = Join-Path $PSScriptRoot 'installer.iss'
if (!(Test-Path $iss)) { throw "installer.iss not found at $iss" }

Write-Host "Compiling installer via ISCC..."
& "$ISCC" "/DSourceDir=$publishDir" "/DServiceDir=$syncPublish" "/DRedistDir=$redistDir" "/DAppVersion=$Version" "/DOutputDir=$desktop" "$iss" | Write-Host

Write-Host "Installer built to Desktop (MagiDeskSetup.exe)."
