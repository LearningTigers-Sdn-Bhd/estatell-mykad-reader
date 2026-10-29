# Installers

## Purpose

An admin opens one file once. MyKad Reader then starts at every login, for every user of the computer, with no Python, no terminal and no driver.

## Files

| OS      | File                        | Built with                         |
| ------- | --------------------------- | ---------------------------------- |
| Windows | `estatell-mykad-setup.exe`  | Inno Setup 6, `packaging/windows/installer.iss` |
| macOS   | `estatell-mykad.pkg`        | `pkgbuild`, `packaging/macos/`     |

File names stay the same as 1.x.

## Windows

| Item         | Value                                                                                 |
| ------------ | ------------------------------------------------------------------------------------- |
| Install to   | `C:\Program Files\Estatell MyKad Bridge\` (same as 1.x)                                |
| Shown as     | **MyKad Reader** in Settings → Apps, publisher Estatell                                |
| Start        | `HKLM\Software\Microsoft\Windows\CurrentVersion\Run\EstatellMyKadBridge`, hidden, every user at login |
| Data folder  | `%ProgramData%\Estatell\MyKadReader\`, Users group can modify                          |
| Admin prompt | Once                                                                                  |
| Silent       | `/VERYSILENT /SUPPRESSMSGBOXES` for IT roll-outs                                       |
| Uninstall    | Settings → Apps → MyKad Reader. Keeps the data folder, so a reinstall stays connected  |

Why a Run key and not a Windows service: a service runs in session 0, where card readers are harder to reach and debug. The Run key runs in the user's session, where the reader works.

## macOS

| Item         | Value                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------- |
| Install to   | `/Library/Application Support/Estatell/MyKadBridge/` (same as 1.x)                           |
| Start        | `/Library/LaunchAgents/com.estatell.mykad-bridge.plist`, `RunAtLoad`, `KeepAlive` on crash   |
| Data folder  | `/Library/Application Support/Estatell/MyKadReader/`, group `staff`, mode `0770`             |
| Admin prompt | Mac password once                                                                           |
| Binary       | Universal (arm64 and x64, joined with `lipo`)                                               |
| Uninstall    | **Uninstall MyKad Reader.app** in `/Applications/Estatell/`. Asks for the Mac password once  |

The 1.x `uninstall.sh` stays inside the install folder for support use. User docs never mention it.

## Upgrade

- Opening a newer installer stops the running copy, replaces it and starts the new one.
- 2.0.0 replaces a 1.x Python bridge in place: same paths, same Run key and same LaunchAgent label.
- The installer removes the 1.x Python files it finds (`_internal\`, the PyInstaller folder).

## First run without code signing

The installers are not signed. The OS asks the admin to confirm once.

| OS      | What the admin does                                                                   |
| ------- | ------------------------------------------------------------------------------------- |
| Windows | **More info → Run anyway**, then **Yes**. If security software asks, **Allow**        |
| macOS   | **System Settings → Privacy & Security → Open Anyway**, then open the file again      |

User docs explain this with screenshots and calm words. See [User docs](../04-estatell/03-user-docs.md). Screens and user docs never say "unsigned".

## Checks on a clean computer

Run before each release, on a real x64 Windows 10 PC and a Mac on macOS 15:

- [ ] Installs with the confirm steps above.
- [ ] Reader works with no driver installed.
- [ ] Starts at login for a second user account.
- [ ] Installs over 1.1.0 (Python) and over the previous 2.x.
- [ ] Stays connected after an upgrade.
- [ ] Uninstall removes the app and the start entry.
