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

- `Shell_NotifyIcon` with a message-only window, on its own thread with a message loop. Menu with `TrackPopupMenu`.
- Calls use `LibraryImport`, so they work with Native AOT.
- Icons are embedded `.ico` files, with light and dark versions for the taskbar theme.
- The binary is `WinExe`, so no console window opens at login. `--foreground` calls `AllocConsole` to print the log.

## macOS

- `NSStatusItem` through the Objective-C runtime (`objc_msgSend` with `LibraryImport`). No Xamarin or MAUI.
- `NSApplication` runs on the main thread with the `Accessory` activation policy (no Dock icon). The server runs on a background thread.
- The icon is a template image, so macOS colours it for light and dark menu bars.
- Needs an app bundle with `LSUIElement`. See [macOS](../03-packaging/01-installers.md#macos).

## More than one user

The Run key and the LaunchAgent start a copy for each logged-in user. Only one copy can hold port 9292. See [Single instance](02-security.md#single-instance).

- A second copy for the **same** user exits with code 0. A per-user lock (a `Local\` mutex on Windows, a lock file in the user's `~/Library` on macOS) finds it.
- A copy for **another** user finds the port taken. It keeps the tray, shows "MyKad Reader is open for another user" and tries the port again every 30 seconds.

## Tests and CI

- The tray sits behind an `ITray` interface. `--no-tray` and the bridge tests use a no-op tray.
- The CI smoke tests run with `--no-tray`. A tray needs a desktop session.
- Check the tray by hand in the [clean computer checks](../03-packaging/01-installers.md#checks-on-a-clean-computer).
