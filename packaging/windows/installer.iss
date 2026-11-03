; Builds estatell-mykad-reader-setup-v{version}{_x64 or _arm64}.exe with Inno Setup 6.
; Publish win-x64 and win-arm64 into artifacts\windows first, then, with no Arch for the file that holds both:
;   iscc /DAppVersion=2.0.0 [/DArch=x64|arm64] packaging\windows\installer.iss
; Installs per machine and starts Estatell MyKad Reader at every user's login (HKLM Run key).
; AppId stays the same as 1.x, so this upgrades 1.x. The old folder and Run key are removed below.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef Arch
  #define Arch ""
#endif
#if Arch == "x64"
  #define Allowed "x64compatible"
  #define Suffix "_x64"
#elif Arch == "arm64"
  #define Allowed "arm64"
  #define Suffix "_arm64"
#else
  #define Allowed "x64compatible or arm64"
  #define Suffix ""
#endif

[Setup]
AppId={{6F0B3C2E-6E8A-4C5B-9A51-2E7C0D4B8F13}
AppName=Estatell MyKad Reader
AppVersion={#AppVersion}
AppPublisher=Jesselton Pixel Sdn Bhd
AppPublisherURL=https://app.estatell.com
AppSupportURL=https://docs.estatell.com/en/docs/guests/identity-documents/set-up-mykad-reader
AppCopyright=Copyright © Jesselton Pixel Sdn Bhd
VersionInfoCompany=Jesselton Pixel Sdn Bhd
VersionInfoDescription=Estatell MyKad Reader Setup
VersionInfoProductName=Estatell MyKad Reader
SetupIconFile=..\..\src\Estatell.MyKad.Bridge\Tray\Icons\app.ico
DefaultDirName={autopf}\Estatell MyKad Reader
; An upgrade moves out of the old 1.x folder.
UsePreviousAppDir=no
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableWelcomePage=yes
DisableReadyPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed={#Allowed}
ArchitecturesInstallIn64BitMode={#Allowed}
OutputDir=..\..\artifacts\windows
OutputBaseFilename=estatell-mykad-reader-setup-v{#AppVersion}{#Suffix}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName=Estatell MyKad Reader
UninstallDisplayIcon={app}\mykad-reader.exe
CloseApplications=no

[Files]
#if Arch == "x64"
Source: "..\..\artifacts\windows\win-x64\mykad-reader.exe"; DestDir: "{app}"; Flags: ignoreversion
#elif Arch == "arm64"
Source: "..\..\artifacts\windows\win-arm64\mykad-reader.exe"; DestDir: "{app}"; Flags: ignoreversion
#else
Source: "..\..\artifacts\windows\win-x64\mykad-reader.exe"; DestDir: "{app}"; Check: not IsArm64; Flags: ignoreversion
Source: "..\..\artifacts\windows\win-arm64\mykad-reader.exe"; DestDir: "{app}"; Check: IsArm64; Flags: ignoreversion
#endif
Source: "..\THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
; The folder of older versions, with the 1.x Python files.
Type: filesandordirs; Name: "{autopf}\Estatell MyKad Bridge"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "EstatellMyKadBridge"; Flags: deletevalue
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "EstatellMyKadReader"; ValueData: """{app}\mykad-reader.exe"""; Flags: uninsdeletevalue

[Run]
; Start now for the person installing, so no restart is needed. Not in a silent roll-out: the
; installer may run as SYSTEM there, and that copy would hold the port with no tray. It starts at the next login.
Filename: "{app}\mykad-reader.exe"; Flags: nowait runasoriginaluser skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM mykad-reader.exe"; Flags: runhidden; RunOnceId: "StopReader"

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  { Stop a running copy, 1.x or 2.x, for every user, so its files can be replaced. }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM estatell-mykad-bridge.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM mykad-bridge.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM mykad-reader.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
