# Installers

## Purpose

An admin opens one file once. MyKad Reader then starts at every login, for every user of the computer, with no Python, no terminal and no driver. Its icon shows in the tray or menu bar. See [Tray](../02-bridge/04-tray.md).

## Files

| OS      | File                        | Built with                         |
| ------- | --------------------------- | ---------------------------------- |
| Windows | `estatell-mykad-reader-setup-v{version}.exe`, also `_x64` and `_arm64` | Inno Setup 6, `packaging/windows/installer.iss` |
| macOS   | `estatell-mykad-reader-v{version}.pkg`, also `_apple-silicon` and `_intel` | `pkgbuild`, `packaging/macos/`     |

The setup file names stay the same as 1.x. Everything else uses the 2.0.0 names.

| Name | Value |
| ---- | ----- |
| App name | **Estatell MyKad Reader** |
| Publisher and copyright holder | Jesselton Pixel Sdn Bhd |
| Program file | `mykad-reader` (`.exe` on Windows) |

The source project stays `Estatell.MyKad.Bridge`, because users never see it. The short name **MyKad Reader** stays in the menu's **Restart MyKad Reader**, the check-in chip and the user docs.

## Windows

| Item         | Value                                                                                 |
| ------------ | ------------------------------------------------------------------------------------- |
| Install to   | `C:\Program Files\Estatell MyKad Reader\`                                |
| Shown as     | **Estatell MyKad Reader** in Settings → Apps, publisher Jesselton Pixel Sdn Bhd. The Apps list links to the set-up help page |
| Start        | `HKLM\Software\Microsoft\Windows\CurrentVersion\Run\EstatellMyKadReader`, every user at login. The binary is `WinExe`, so no console window opens |
| Tray         | Windows hides new tray icons under **^** at first. The user docs show how to find it   |
| Admin prompt | Once                                                                                  |
| Silent       | `/VERYSILENT /SUPPRESSMSGBOXES` for IT roll-outs. MyKad Reader then starts at the next login, because the installer may run as SYSTEM |
| Uninstall    | Settings → Apps → MyKad Reader                                                        |

GitHub Actions builds the three installers with `.github/workflows/release.yml`, because Native AOT cannot build Windows on a Mac. Download them from the workflow run. The file with no chip in its name holds both the x64 and the arm64 binary.

Why a Run key and not a Windows service: a service runs in session 0, where card readers are harder to reach and debug. The Run key runs in the user's session, where the reader works.

## macOS

| Item         | Value                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------- |
| Install to   | `/Library/Application Support/Estatell/MyKadReader/`                           |
| App bundle   | `Estatell MyKad Reader.app` in the install folder, binary in `Contents/MacOS/mykad-reader`. `Info.plist` sets `LSUIElement` (menu bar only, no Dock icon) |
| Start        | `/Library/LaunchAgents/com.estatell.mykad-reader.plist`, `RunAtLoad`, `KeepAlive` on crash, `LimitLoadToSessionType` `Aqua` (only in a desktop session, where the menu bar exists) |
| Admin prompt | Mac password once                                                                           |
| Binary       | Universal (arm64 and x64, joined with `lipo`)                                               |
| Uninstall    | **Uninstall Estatell MyKad Reader.app** in `/Applications/Estatell/`. Asks for the Mac password once  |

The 1.x `uninstall.sh` stays inside the install folder for support use. User docs never mention it.

Build with `packaging/macos/build.sh [version]`. The result is three packages in `artifacts/macos`. The one with no chip in its name is universal. The version defaults to the one in `Directory.Build.props`. An Apple Silicon Mac builds both architectures.

- The package identifier is `com.estatell.mykad-reader`. The pre-install script forgets the old `com.estatell.mykad-bridge` receipt.
- Both app bundles are not relocatable. Otherwise macOS moves an upgrade to wherever it finds a copy with the same bundle id.
- The bundles have an ad-hoc signature, because Apple Silicon runs only signed code. This is not a Developer ID signature, so the confirm step below still applies.
- `packaging/THIRD-PARTY-NOTICES.txt` holds the `PCSC` licence and installs into the install folder.

## Upgrade

- Opening a newer installer stops the running copy, replaces it and starts the new one.
- 2.0.0 replaces a 1.x Python bridge. Windows keeps the 1.x AppId, so it upgrades the same entry in Settings → Apps.
- The installers remove the old names: the `Estatell MyKad Bridge` folder and the `EstatellMyKadBridge` Run key on Windows, and the `MyKadBridge` folder, the `com.estatell.mykad-bridge` agent and the old uninstaller on macOS.
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
- [ ] The tray icon and menu show on Windows (light and dark taskbar) and in the Mac menu bar (light and dark).
- [ ] With check-in open in Estatell, the menu shows **Estatell: connected · app.estatell.com**, and **not seen** about 10 seconds after the tab closes.
- [ ] A card fills the guest form.
- [ ] With two users logged in, the second shows "MyKad Reader is open for another user".
- [ ] Installs over 1.1.0 (Python) and over the previous 2.x.
- [ ] Uninstall removes the app and the start entry.

2.0.0: the user reported all checks passed on 3 Nov 2026.
