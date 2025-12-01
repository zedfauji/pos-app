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
