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
