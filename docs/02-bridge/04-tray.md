# Tray

## Purpose

Show staff that MyKad Reader is running, and show the admin the connect code. The icon sits in the Windows tray and the macOS menu bar. MyKad Reader has no window.

## Menu

```
 MyKad Reader 2.0.0
 ● Ready · Hotel Seri Kota
 ───────────────────────────
 Connect code: 482-917            (only while not connected)
 ───────────────────────────
 Open MyKad Reader settings
 Help
 Restart MyKad Reader
```

| Status line                                   | Shown when                                   | Icon      |
| --------------------------------------------- | -------------------------------------------- | --------- |
| ● Ready · {hotel}                             | Connected, reader plugged in                 | Normal    |
| ○ Plug in the card reader                     | No reader                                    | Grey      |
| ○ Not connected to a hotel yet                | `paired: false`                              | Grey      |
| ○ MyKad Reader is open for another user       | Another user on this computer runs it        | Grey      |

- **Open MyKad Reader settings** opens **Settings → MyKad Reader** in Estatell in the default browser.
- **Help** opens the `set-up-mykad-reader` user docs page.
- **Restart MyKad Reader** stops the server and starts it again.
- There is no **Quit**. Quitting would stop check-in without a warning.
- The menu never shows card data, the token or `device_id`.
- Words follow [Words on screen](../04-estatell/01-settings-page.md#words-on-screen).

## Connect code

The code stops other programs on the computer from connecting it to a hotel first. See [Connecting](03-connecting.md).

- 6 digits, shown as `482-917`. Made at random when MyKad Reader starts while not connected, and again after each disconnect.
- Kept in memory only. Never written to disk, to the log or to `/status`.
- `/pair` needs it. It is compared in constant time.
- After 5 wrong codes, `/pair` is locked. The menu then shows **New connect code**, which makes a new code and unlocks `/pair`. A restart does the same.
- Cleared when the computer connects.

## Windows

- `Shell_NotifyIcon` with a hidden top-level window, on its own thread with a message loop. Not a message-only window: those get no broadcasts, so they miss `TaskbarCreated` (add the icon again after Explorer restarts) and `WM_SETTINGCHANGE` (taskbar theme changed).
- Menu with `TrackPopupMenu`. Call `SetForegroundWindow` first, or the menu stays open when the user clicks elsewhere.
- Calls use `LibraryImport`, so they work with Native AOT.
- Icons are embedded `.ico` files, with light and dark versions for the taskbar theme.
- The binary is `WinExe`, so no console window opens at login. `--foreground` calls `AllocConsole` to print the log.

## macOS

- `NSStatusItem` through the Objective-C runtime (`objc_msgSend` with `LibraryImport`). No Xamarin or MAUI.
- `NSApplication` runs on the main thread with the `Accessory` activation policy (no Dock icon). The server runs on a background thread.
- The icon is a template image, so macOS colours it for light and dark menu bars.
- Menu actions go to a small Objective-C class made at run time (`objc_allocateClassPair`, `class_addMethod`) whose methods are `UnmanagedCallersOnly` functions. The same class is the menu delegate, so `menuWillOpen:` refreshes the status line.
- Needs an app bundle with `LSUIElement`. See [macOS](../03-packaging/01-installers.md#macos).

## Signals

The tray owns the main thread, so nothing waits on the ASP.NET host. Its default console lifetime catches SIGTERM and Ctrl+C, asks the host to stop and swallows the signal: the process keeps running. Replace it with an `IHostLifetime` that does nothing, and handle SIGTERM and SIGINT with `PosixSignalRegistration`: stop the server, then exit. `launchctl`, the installers and logout send SIGTERM.

## More than one user

The Run key and the LaunchAgent start a copy for each logged-in user. Only one copy can hold port 9292. See [Single instance](02-security.md#single-instance).

- A second copy for the **same** user exits with code 0. A per-user lock (a `Local\` mutex on Windows, a lock file in the user's `~/Library` on macOS) finds it.
- A copy for **another** user finds the port taken. It keeps the tray, shows "MyKad Reader is open for another user" and tries the port again every 30 seconds.

## Spike

The M1 spike (`spikes/TrayAot/`, run on 30 Oct 2026). One binary: Kestrel on a background thread, a tray icon with **Open status**, **Restart server** and **Quit spike**.

| Check                                | osx-arm64                                                  | win-x64 · win-arm64 · win-x86 |
| ------------------------------------ | ---------------------------------------------------------- | ----------------------------- |
| AOT publish                          | Yes. 9.3 MB, nearly all of it Kestrel                      | Pending: `spike-tray-aot` workflow |
| AOT and trim warnings                | None                                                       | Pending                       |
| Icon shows                           | Status item visible, image set, 34 × 22 pt button          | Pending                       |
| Menu actions reach .NET              | Yes, sent through AppKit (`--self-test`)                   | Pending: a hand check on a PC |
| Restart server in the same process   | Yes. Same process ID, `start` goes from 1 to 2             | Pending                       |
| Light and dark icons                 | Template image                                             | `.ico` per taskbar theme, swapped on `WM_SETTINGCHANGE`. Pending |
| No Dock icon or console window       | Yes, `LSUIElement` in a minimal `.app`                     | `WinExe`. Pending             |

- `--self-test` sends the **Restart server** action and `menuWillOpen:` through AppKit, because the build machine allows no scripted clicks. Clicking the icon by hand is still a check to do.
- The SIGTERM problem in [Signals](#signals) came from this spike.
- Icons come from `spikes/TrayAot/icons/card.svg` with `make-icons.sh`. They are placeholders until there is a real logo.

## Tests and CI

- The tray sits behind an `ITray` interface. `--no-tray` and the bridge tests use a no-op tray.
- The CI smoke tests run with `--no-tray`. A tray needs a desktop session.
- Check the tray by hand in the [clean computer checks](../03-packaging/01-installers.md#checks-on-a-clean-computer).
