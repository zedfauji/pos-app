# MagiDesk Frontend Standalone Installer
# This script creates a portable installer for the MagiDesk frontend application

param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "MagiDesk-Frontend-Installer"
)

Write-Host "Creating MagiDesk Frontend Standalone Installer..." -ForegroundColor Green

# Set paths
$PublishPath = "solution/frontend/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish"
$InstallerPath = $OutputPath

# Check if publish directory exists
if (-not (Test-Path $PublishPath)) {
    Write-Error "Publish directory not found: $PublishPath"
    Write-Host "Please run: dotnet publish frontend/MagiDesk.Frontend.csproj -c Release -r win-x64 --self-contained true" -ForegroundColor Yellow
    exit 1
}

# Create installer directory
if (Test-Path $InstallerPath) {
    Remove-Item -Recurse -Force $InstallerPath
}
New-Item -ItemType Directory -Path $InstallerPath | Out-Null

Write-Host "Copying application files..." -ForegroundColor Yellow

# Copy all published files
Copy-Item -Path "$PublishPath/*" -Destination $InstallerPath -Recurse -Force

# Create installer script
$InstallerScript = @"
@echo off
echo MagiDesk POS System - Frontend Installer
echo =====================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as Administrator - Installing system-wide...
    set "INSTALL_DIR=%ProgramFiles%\MagiDesk"
) else (
    echo Running as User - Installing to user directory...
    set "INSTALL_DIR=%LOCALAPPDATA%\MagiDesk"
)

echo.
echo Installing MagiDesk to: %INSTALL_DIR%
echo.

REM Create installation directory
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

REM Copy application files
echo Copying application files...
xcopy /E /I /Y . "%INSTALL_DIR%" >nul

REM Create desktop shortcut
echo Creating desktop shortcut...
set "DESKTOP=%USERPROFILE%\Desktop"
echo Set oWS = WScript.CreateObject("WScript.Shell") > CreateShortcut.vbs
echo sLinkFile = "%DESKTOP%\MagiDesk POS.lnk" >> CreateShortcut.vbs
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> CreateShortcut.vbs
echo oLink.TargetPath = "%INSTALL_DIR%\MagiDesk.Frontend.exe" >> CreateShortcut.vbs
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> CreateShortcut.vbs
echo oLink.Description = "MagiDesk POS System" >> CreateShortcut.vbs
echo oLink.Save >> CreateShortcut.vbs
cscript CreateShortcut.vbs >nul 2>&1
del CreateShortcut.vbs

REM Create Start Menu shortcut
echo Creating Start Menu shortcut...
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
if not exist "%START_MENU%" mkdir "%START_MENU%"
echo Set oWS = WScript.CreateObject("WScript.Shell") > CreateStartMenuShortcut.vbs
echo sLinkFile = "%START_MENU%\MagiDesk POS.lnk" >> CreateStartMenuShortcut.vbs
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> CreateStartMenuShortcut.vbs
echo oLink.TargetPath = "%INSTALL_DIR%\MagiDesk.Frontend.exe" >> CreateStartMenuShortcut.vbs
echo oLink.WorkingDirectory = "%INSTALL_DIR%" >> CreateStartMenuShortcut.vbs
echo oLink.Description = "MagiDesk POS System" >> CreateStartMenuShortcut.vbs
echo oLink.Save >> CreateStartMenuShortcut.vbs
cscript CreateStartMenuShortcut.vbs >nul 2>&1
del CreateStartMenuShortcut.vbs

echo.
echo =====================================
echo Installation completed successfully!
echo.
echo MagiDesk POS System has been installed to:
echo %INSTALL_DIR%
echo.
echo Desktop and Start Menu shortcuts have been created.
echo.
echo You can now run MagiDesk from:
echo - Desktop shortcut
echo - Start Menu
echo - Or directly: %INSTALL_DIR%\MagiDesk.Frontend.exe
echo.
echo Note: Make sure your backend services are running and accessible.
echo.
pause
"@

# Save installer script
$InstallerScript | Out-File -FilePath "$InstallerPath/Install.bat" -Encoding ASCII

# Create README
$ReadmeContent = @"
# MagiDesk POS System - Frontend Installer

## Installation Instructions

1. **Run Install.bat** as Administrator for system-wide installation, or as regular user for user-only installation.

2. **Prerequisites**: 
   - Windows 10/11
   - .NET 8.0 Runtime (if not self-contained)
   - Backend services must be running and accessible

## What's Included

- ✅ **Complete Settings Management**: Comprehensive settings for all aspects of the POS system
- ✅ **Business/Receipt Settings**: Paper width, tax rates, business info, receipt formatting
- ✅ **API Connections**: All backend API endpoints configuration
- ✅ **Notification Settings**: Toast notifications, sound alerts
- ✅ **Locale Settings**: Language and regional preferences
- ✅ **Table Management**: Session timers, auto-stop settings
- ✅ **Print Settings**: Receipt printer configuration

## Backend Requirements

This frontend application requires the following backend services to be running:

- **Settings API**: For managing all application settings
- **Menu API**: For menu management
- **Order API**: For order processing
- **Payment API**: For payment processing
- **Tables API**: For table management
- **Inventory API**: For inventory management
- **Users API**: For user management

## Configuration

After installation, configure the API endpoints in Settings:
1. Open MagiDesk POS
2. Go to Settings page
3. Configure API Connections section
4. Set the correct URLs for your backend services

## Features

### Enhanced Settings System
- **General Settings**: Theme, locale, notifications
- **API Connections**: All backend service URLs
- **Business Settings**: Company info, tax rates, receipt formatting
- **Table Settings**: Session management, timers
- **Print Settings**: Receipt printer configuration

### MVVM Architecture
- Clean separation of concerns
- Proper data binding
- Async/await patterns
- Error handling and validation

## Support

For support or issues, please contact your system administrator.

---
**Version**: 1.0.0
**Build Date**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Target**: Windows 10/11 x64
"@

$ReadmeContent | Out-File -FilePath "$InstallerPath/README.txt" -Encoding UTF8

# Create uninstaller
$UninstallerScript = @"
@echo off
echo MagiDesk POS System - Uninstaller
echo =================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as Administrator - Uninstalling system-wide...
    set "INSTALL_DIR=%ProgramFiles%\MagiDesk"
) else (
    echo Running as User - Uninstalling from user directory...
    set "INSTALL_DIR=%LOCALAPPDATA%\MagiDesk"
)

echo.
echo Removing MagiDesk from: %INSTALL_DIR%
echo.

REM Remove application files
if exist "%INSTALL_DIR%" (
    echo Removing application files...
    rmdir /S /Q "%INSTALL_DIR%"
)

REM Remove desktop shortcut
echo Removing desktop shortcut...
set "DESKTOP=%USERPROFILE%\Desktop"
if exist "%DESKTOP%\MagiDesk POS.lnk" del "%DESKTOP%\MagiDesk POS.lnk"

REM Remove Start Menu shortcut
echo Removing Start Menu shortcut...
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
if exist "%START_MENU%\MagiDesk POS.lnk" del "%START_MENU%\MagiDesk POS.lnk"

echo.
echo =================================
echo Uninstallation completed!
echo.
echo MagiDesk POS System has been removed from your system.
echo.
pause
"@

$UninstallerScript | Out-File -FilePath "$InstallerPath/Uninstall.bat" -Encoding ASCII

# Get file size
$TotalSize = (Get-ChildItem -Path $InstallerPath -Recurse | Measure-Object -Property Length -Sum).Sum
$SizeInMB = [math]::Round($TotalSize / 1MB, 2)

Write-Host "✅ Frontend installer created successfully!" -ForegroundColor Green
Write-Host "📁 Installer location: $InstallerPath" -ForegroundColor Cyan
Write-Host "📦 Total size: $SizeInMB MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Installation Instructions:" -ForegroundColor Yellow
Write-Host "1. Copy the '$InstallerPath' folder to the target system" -ForegroundColor White
Write-Host "2. Run 'Install.bat' as Administrator for system-wide install" -ForegroundColor White
Write-Host "3. Or run 'Install.bat' as regular user for user-only install" -ForegroundColor White
Write-Host "4. Configure backend API URLs in Settings after installation" -ForegroundColor White
Write-Host ""
Write-Host "🎯 The installer includes:" -ForegroundColor Yellow
Write-Host "   ✅ Complete MagiDesk frontend application" -ForegroundColor Green
Write-Host "   ✅ Enhanced settings management system" -ForegroundColor Green
Write-Host "   ✅ All dependencies and runtime files" -ForegroundColor Green
Write-Host "   ✅ Desktop and Start Menu shortcuts" -ForegroundColor Green
Write-Host "   ✅ Uninstaller utility" -ForegroundColor Green
