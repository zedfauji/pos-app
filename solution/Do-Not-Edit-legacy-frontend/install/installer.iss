; Inno Setup installer script for MagiDesk Frontend
; Pass in /DSourceDir="<frontend publish path>", and optionally /DAppVersion and /DOutputDir

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
ExtraDiskSpaceRequired=500000000
#if SetupIcon != ""
SetupIconFile={#SetupIcon}
#endif

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; Optional redistributables copied to temp for install
#if RedistDir != ""
#if FileExists(AddBackslash(RedistDir) + "WindowsAppRuntimeInstall.exe")
Source: "{#RedistDir}\WindowsAppRuntimeInstall.exe"; DestDir: "{tmp}"; Flags: ignoreversion
#endif
#if FileExists(AddBackslash(RedistDir) + "Microsoft.WebView2.Bootstrapper.exe")
Source: "{#RedistDir}\Microsoft.WebView2.Bootstrapper.exe"; DestDir: "{tmp}"; Flags: ignoreversion
#endif
#endif

[Icons]
Name: "{autoprograms}\MagiDesk"; Filename: "{app}\MagiDesk.Frontend.exe"; WorkingDir: "{app}"; 
#if AppIcon != ""
    IconFilename: "{#AppIcon}"
#endif
Name: "{autodesktop}\MagiDesk"; Filename: "{app}\MagiDesk.Frontend.exe"; WorkingDir: "{app}"; Tasks: desktopicon; 
#if AppIcon != ""
    IconFilename: "{#AppIcon}"
#endif

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; Flags: unchecked

[Run]
; Install redistributables silently BEFORE launching the app (with error handling)
#if RedistDir != ""
#if FileExists(AddBackslash(RedistDir) + "WindowsAppRuntimeInstall.exe")
Filename: "{tmp}\WindowsAppRuntimeInstall.exe"; Parameters: "/quiet /norestart"; Flags: runhidden skipifdoesntexist waituntilterminated
#endif
#if FileExists(AddBackslash(RedistDir) + "Microsoft.WebView2.Bootstrapper.exe")
Filename: "{tmp}\Microsoft.WebView2.Bootstrapper.exe"; Parameters: "/silent /install"; Flags: runhidden skipifdoesntexist waituntilterminated
#endif
#endif
; Launch MagiDesk after prerequisites are installed
Filename: "{app}\MagiDesk.Frontend.exe"; Description: "Launch MagiDesk"; Flags: nowait postinstall skipifsilent

[UninstallRun]

[Code]

function RedistProvided: Boolean;
begin
  Result := '{#RedistDir}' <> '';
end;

function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
  ErrorMessage: String;
begin
  Result := True;
  
  // Get Windows version
  GetWindowsVersionEx(Version);
  
  // Check if Windows 10 or later
  if Version.Major < 10 then
  begin
    ErrorMessage := 'MagiDesk requires Windows 10 or later.' + #13#10 + #13#10 +
                    'Your system is running Windows ' + IntToStr(Version.Major) + '.' + IntToStr(Version.Minor) + '.' + #13#10 +
                    'Please upgrade to Windows 10 version 1809 (October 2018 Update) or later.';
    MsgBox(ErrorMessage, mbError, MB_OK);
    Result := False;
    Exit;
  end;
  
  // Check if Windows 10 version 1809 (Build 17763) or later
  if (Version.Major = 10) and (Version.Build < 17763) then
  begin
    ErrorMessage := 'MagiDesk requires Windows 10 version 1809 (October 2018 Update) or later.' + #13#10 + #13#10 +
                    'Your system is running Windows 10 Build ' + IntToStr(Version.Build) + '.' + #13#10 +
                    'Minimum required: Windows 10 Build 17763 (version 1809).' + #13#10 + #13#10 +
                    'Please update Windows to the latest version.';
    MsgBox(ErrorMessage, mbError, MB_OK);
    Result := False;
    Exit;
  end;
  
  // Check if 64-bit architecture
  if not IsWin64 then
  begin
    ErrorMessage := 'MagiDesk requires a 64-bit version of Windows.' + #13#10 + #13#10 +
                    'Your system appears to be running 32-bit Windows.' + #13#10 +
                    'Please install the 64-bit version of Windows 10.';
    MsgBox(ErrorMessage, mbError, MB_OK);
    Result := False;
    Exit;
  end;
end;
