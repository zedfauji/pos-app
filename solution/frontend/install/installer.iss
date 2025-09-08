; Inno Setup installer script for MagiDesk Frontend + Sync Windows Service
; Pass in /DSourceDir="<frontend publish path>", /DServiceDir="<service publish path>", and optionally /DAppVersion and /DOutputDir

#ifndef SourceDir
  #error SourceDir not defined. Pass /DSourceDir="<publish path>" to ISCC.
#endif
#ifndef ServiceDir
  #define ServiceDir ""
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
; Sync Worker (if provided)
Source: "{#ServiceDir}\*"; DestDir: "{app}\SyncWorker"; Flags: recursesubdirs createallsubdirs ignoreversion; Check: ServiceProvided
; Default config to ProgramData
Source: "{#ServiceDir}\appsettings.json"; DestDir: "{commonappdata}\MagiDesk\sync"; Flags: ignoreversion; Check: ServiceProvided
; Optional redistributables copied to temp for install
#if FileExists(AddBackslash(RedistDir) + "WindowsAppRuntimeInstall.exe")
Source: "{#RedistDir}\WindowsAppRuntimeInstall.exe"; DestDir: "{tmp}"; Flags: ignoreversion
#endif
#if FileExists(AddBackslash(RedistDir) + "Microsoft.WebView2.Bootstrapper.exe")
Source: "{#RedistDir}\Microsoft.WebView2.Bootstrapper.exe"; DestDir: "{tmp}"; Flags: ignoreversion
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
Filename: "{app}\MagiDesk.Frontend.exe"; Description: "Launch MagiDesk"; Flags: nowait postinstall skipifsilent
; Create and start Windows Service
Filename: "cmd.exe"; Parameters: "/c sc create RestockMate binPath= ""{app}\SyncWorker\MagiDesk.SyncWorker.exe"" start= auto DisplayName= ""RestockMate"""; Flags: runhidden; Check: ServiceProvided
Filename: "cmd.exe"; Parameters: "/c sc start RestockMate"; Flags: runhidden; Check: ServiceProvided
; Install redistributables silently if provided
#if FileExists(AddBackslash(RedistDir) + "WindowsAppRuntimeInstall.exe")
Filename: "{tmp}\WindowsAppRuntimeInstall.exe"; Parameters: "/quiet /norestart"; Flags: runhidden
#endif
#if FileExists(AddBackslash(RedistDir) + "Microsoft.WebView2.Bootstrapper.exe")
Filename: "{tmp}\Microsoft.WebView2.Bootstrapper.exe"; Parameters: "/silent /install"; Flags: runhidden
#endif

[UninstallRun]
Filename: "cmd.exe"; Parameters: "/c sc stop RestockMate"; Flags: runhidden; Check: ServiceProvided
Filename: "cmd.exe"; Parameters: "/c sc delete RestockMate"; Flags: runhidden; Check: ServiceProvided

[Code]
function ServiceProvided: Boolean;
begin
  Result := '{#ServiceDir}' <> '';
end;

function RedistProvided: Boolean;
begin
  Result := '{#RedistDir}' <> '';
end;
