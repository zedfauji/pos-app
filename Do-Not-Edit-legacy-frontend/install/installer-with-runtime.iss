; Inno Setup installer script for MagiDesk Frontend with Windows App Runtime
; Pass in /DSourceDir="<frontend publish path>", and optionally /DAppVersion, /DOutputDir, /DRedistDir

#ifndef SourceDir
  #error SourceDir not defined. Pass /DSourceDir="<publish path>" to ISCC.
#endif
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef OutputDir
  #define OutputDir "Output"
#endif
#ifndef SetupIcon
  #define SetupIcon ""
#endif
#ifndef AppIcon
  #define AppIcon ""
#endif
#ifndef RedistDir
  #define RedistDir ""
#endif

#define AppVer AppVersion
#define OutDir OutputDir

[Setup]
AppId={{6F3C0E41-2B2D-4E2F-9F1C-6A2C8BBA2D77}
AppName=MagiDesk
AppVersion={#AppVer}
DefaultDirName={autopf}\MagiDesk
DefaultGroupName=MagiDesk
OutputDir={#OutDir}
OutputBaseFilename=MagiDeskSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
ArchitecturesInstallIn64BitMode=x64
#if SetupIcon != ""
SetupIconFile={#SetupIcon}
#endif

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; Windows App Runtime MSIX package
#if RedistDir != ""
#if FileExists(AddBackslash(RedistDir) + "Microsoft.WindowsAppRuntime.1.7_x64.msix")
Source: "{#RedistDir}\Microsoft.WindowsAppRuntime.1.7_x64.msix"; DestDir: "{tmp}"; Flags: ignoreversion
#endif
#endif

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Icons]
Name: "{autoprograms}\MagiDesk"; Filename: "{app}\MagiDesk.Frontend.exe"
Name: "{autodesktop}\MagiDesk"; Filename: "{app}\MagiDesk.Frontend.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\MagiDesk"; Filename: "{app}\MagiDesk.Frontend.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\MagiDesk.Frontend.exe"; Description: "Launch MagiDesk"; Flags: nowait postinstall skipifsilent

[Code]
function RedistProvided: Boolean;
begin
  Result := '{#RedistDir}' <> '';
end;

function InstallWindowsAppRuntime: Boolean;
var
  ResultCode: Integer;
  RuntimeFile: String;
begin
  Result := True;
  
  if not RedistProvided then
  begin
    Log('No redistributables directory provided, skipping Windows App Runtime installation');
    Exit;
  end;
  
  RuntimeFile := ExpandConstant('{tmp}\Microsoft.WindowsAppRuntime.1.7_x64.msix');
  
  if not FileExists(RuntimeFile) then
  begin
    Log('Windows App Runtime MSIX file not found: ' + RuntimeFile);
    Exit;
  end;
  
  Log('Installing Windows App Runtime 1.7...');
  
  if not Exec('powershell.exe', '-Command "Add-AppxPackage -Path ''' + RuntimeFile + ''' -Force"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Log('Failed to install Windows App Runtime. Result code: ' + IntToStr(ResultCode));
    Result := False;
  end
  else
  begin
    Log('Windows App Runtime installation completed. Result code: ' + IntToStr(ResultCode));
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not InstallWindowsAppRuntime then
    begin
      Log('Warning: Windows App Runtime installation failed. The application may not work properly.');
    end;
  end;
end;
