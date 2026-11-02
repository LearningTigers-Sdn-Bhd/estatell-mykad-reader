; Builds estatell-mykad-setup.exe with Inno Setup 6.
; Publish win-x64 and win-arm64 into artifacts\windows first, then:
;   iscc /DAppVersion=2.0.0 packaging\windows\installer.iss
; Installs per machine and starts MyKad Reader at every user's login (HKLM Run key).
; AppId, the folder and the Run key stay the same as 1.x, so this upgrades 1.x in place.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif

[Setup]
AppId={{6F0B3C2E-6E8A-4C5B-9A51-2E7C0D4B8F13}
AppName=MyKad Reader
AppVersion={#AppVersion}
AppPublisher=Estatell
AppPublisherURL=https://app.estatell.com
DefaultDirName={autopf}\Estatell MyKad Bridge
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible or arm64
ArchitecturesInstallIn64BitMode=x64compatible or arm64
OutputDir=..\..\artifacts\windows
OutputBaseFilename=estatell-mykad-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=MyKad Reader
UninstallDisplayIcon={app}\mykad-bridge.exe
CloseApplications=no

[Files]
Source: "..\..\artifacts\windows\win-x64\mykad-bridge.exe"; DestDir: "{app}"; Check: not IsArm64; Flags: ignoreversion
Source: "..\..\artifacts\windows\win-arm64\mykad-bridge.exe"; DestDir: "{app}"; Check: IsArm64; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
; The 1.x Python files.
Type: filesandordirs; Name: "{app}\_internal"
Type: files; Name: "{app}\estatell-mykad-bridge.exe"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "EstatellMyKadBridge"; ValueData: """{app}\mykad-bridge.exe"""; Flags: uninsdeletevalue

[Run]
; Start now for the person installing, so no restart is needed. Not in a silent roll-out: the
; installer may run as SYSTEM there, and that copy would hold the port with no tray. It starts at the next login.
Filename: "{app}\mykad-bridge.exe"; Flags: nowait runasoriginaluser skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM mykad-bridge.exe"; Flags: runhidden; RunOnceId: "StopBridge"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  { Stop a running copy, 1.x or 2.x, for every user, so its files can be replaced. }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM estatell-mykad-bridge.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM mykad-bridge.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
