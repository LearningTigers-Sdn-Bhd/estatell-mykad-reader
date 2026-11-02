# Tray

## Purpose

Show staff that MyKad Reader is running, that the card reader is plugged in, and that an Estatell page is talking to it. The icon sits in the Windows tray and the macOS menu bar. MyKad Reader has no window.

## Menu

```
 MyKad Reader 2.0.0
 ● Ready
 ───────────────────────────
 Card reader: ACS ACR39U
 Card: none
 Estatell: connected · app.estatell.com
 ───────────────────────────
 Help
 Restart MyKad Reader
```

| Line         | Values                                                                 | From                         |
| ------------ | ---------------------------------------------------------------------- | ---------------------------- |
| Status       | See the table below                                                    | All of the lines below       |
| Card reader  | The reader name, or **not found**                                      | `reader_name` in `/status`   |
| Card         | **inserted** or **none**                                               | `card_present` in `/status`  |
| Estatell     | **connected · {host}** or **not seen**                                 | The last request with an allowed `Origin` |

| Status line                                   | Shown when                                   | Icon      |
| --------------------------------------------- | -------------------------------------------- | --------- |
| ● Ready                                       | Reader plugged in                            | Normal    |
| ○ Plug in the card reader                     | No reader                                    | Grey      |
| ○ MyKad Reader is open for another user       | Another user on this computer runs it        | Grey      |

- **Estatell: connected** means a request with an allowed `Origin` came in during the last 10 seconds. The check-in page polls `/status` every 2.5 seconds, so the line turns on within a few seconds of opening it, and turns to **not seen** 10 seconds after the page closes. `{host}` is the host of that `Origin`, for example `app.estatell.com` or `demo.estatell.com`.
- **not seen** is not an error. It only means no Estatell page with the MyKad box is open on this computer now. It does not change the icon.
- The menu refreshes each time it opens (`menuWillOpen:` on macOS, before `TrackPopupMenu` on Windows). The icon refreshes when the reader is plugged in or out.
- **Help** opens the `set-up-mykad-reader` user docs page.
- **Restart MyKad Reader** stops the server and starts it again.
- There is no **Quit**. Quitting would stop check-in without a warning.
- The menu never shows card data.
- Words follow [Words on screen](../04-estatell/01-check-in-chip.md#words-on-screen).

## Windows

- `Shell_NotifyIcon` with a hidden top-level window, on its own thread with a message loop. Not a message-only window: those get no broadcasts, so they miss `TaskbarCreated` (add the icon again after Explorer restarts) and `WM_SETTINGCHANGE` (taskbar theme changed).
- Menu with `TrackPopupMenu`. Call `SetForegroundWindow` first, or the menu stays open when the user clicks elsewhere.
- Calls use `LibraryImport`, so they work with Native AOT.
- Icons are embedded `.ico` files, with light and dark versions for the taskbar theme.
- The binary is `WinExe`, so no console window opens at login. `--foreground` calls `AllocConsole` to print the log.

## macOS

- `NSStatusItem` through the Objective-C runtime (`objc_msgSend` with `LibraryImport`). No Xamarin or MAUI.
- `NSApplication` runs on the main thread with the `Accessory` activation policy (no Dock icon). The server runs on a background thread.
- The icon is a template image, so macOS colours it for light and dark menu bars. It turns grey (`appearsDisabled`) when the status is not **Ready**. A timer reads `Status` every 2 seconds to follow the reader, because there is no plug callback.
- `Stop` from another thread runs `stop:` on the main thread and posts an empty event, so the run loop wakes and `Run` returns.
- Menu actions go to a small Objective-C class made at run time (`objc_allocateClassPair`, `class_addMethod`) whose methods are `UnmanagedCallersOnly` functions. The same class is the menu delegate, so `menuWillOpen:` refreshes the status line.
- Needs an app bundle with `LSUIElement`. See [macOS](../03-packaging/01-installers.md#macos).

## Signals

The tray owns the main thread, so nothing waits on the ASP.NET host. Its default console lifetime catches SIGTERM and Ctrl+C, asks the host to stop and swallows the signal: the process keeps running. Replace it with an `IHostLifetime` that does nothing, and handle SIGTERM and SIGINT with `PosixSignalRegistration`: stop the server, then exit. `launchctl`, the installers and logout send SIGTERM.

## Code

| Piece | What |
| --- | --- |
| `ITray` | `Run` blocks on the main thread until `Stop`. `Stop` is safe from any thread, and a stop before the run is not lost. No `Dispose`, because a restart disposes the app that created the tray. `Run` removes the icon when it ends |
| `TrayActions` | `Help`, `Restart` and `Status`. None waits for the server, so a tray can call them on its UI thread |
| `TrayStatus` | The state and the lines of the menu. Refresh the menu and the icon from `Status` |
| `TrayChooser` | Picks the tray. Each OS tray adds its branch here |
| `BridgeServer` | Runs beside the tray. Waits for the port, stops, and rebuilds the app for a restart, because a stopped `WebApplication` cannot start again |

Program order: build the app and take the lock, register the signals, start the server on a background task, then run the tray on the main thread. When `Run` returns, stop the server and exit.

**Help** opens `https://docs.estatell.com/en/docs/guests/identity-documents/set-up-mykad-reader`. The page does not exist until the user docs item lands.

## More than one user

The Run key and the LaunchAgent start a copy for each logged-in user. Only one copy can hold port 9292. See [Single instance](02-security.md#single-instance).

- A second copy for the **same** user exits with code 0. A per-user lock (a `Local\` mutex on Windows, a lock file in the user's `~/Library` on macOS) finds it.
- A copy for **another** user finds the port taken. It keeps the tray, shows "MyKad Reader is open for another user" and tries the port again every 30 seconds.

## Spike

The M1 spike (`spikes/TrayAot/`, run on 30 Oct 2026). One binary: Kestrel on a background thread, a tray icon with **Open status**, **Restart server** and **Quit spike**.

| Check                                | osx-arm64                                                  | win-x64 · win-arm64 · win-x86 |
| ------------------------------------ | ---------------------------------------------------------- | ----------------------------- |
| AOT publish                          | Yes. 9.3 MB, nearly all of it Kestrel                      | Yes, all three (run 41537649352) |
| AOT and trim warnings                | None                                                       | None                          |
| Icon shows                           | Status item visible, image set, 34 × 22 pt button          | Pending                       |
| Menu actions reach .NET              | Yes, sent through AppKit (`--self-test`)                   | Pending: a hand check on a PC |
| Restart server in the same process   | Yes. Same process ID, `start` goes from 1 to 2             | Pending                       |
| Light and dark icons                 | Template image                                             | `.ico` per taskbar theme, swapped on `WM_SETTINGCHANGE`. Pending |
| No Dock icon or console window       | Yes, `LSUIElement` in a minimal `.app`                     | `WinExe`. Pending             |

- The Windows spike ran on a real PC on 3 Nov 2026. The rows still marked Pending, the browser and the antivirus result are not confirmed yet. One browser blocked the download as dangerous until it was kept by hand.
- `--self-test` sends the **Restart server** action and `menuWillOpen:` through AppKit, because the build machine allows no scripted clicks. Clicking the icon by hand is still a check to do.
- The SIGTERM problem in [Signals](#signals) came from this spike.
- Icons come from `spikes/TrayAot/icons/card.svg` with `make-icons.sh`. They are placeholders until there is a real logo.

## Tests and CI

- The tray sits behind an `ITray` interface. `--no-tray` and the bridge tests use a no-op tray.
- The CI smoke tests run with `--no-tray`. A tray needs a desktop session.
- Check the tray by hand in the [clean computer checks](../03-packaging/01-installers.md#checks-on-a-clean-computer).
