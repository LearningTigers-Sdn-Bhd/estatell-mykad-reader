# Build checklist

The build plan as a list of commits, in build order. Any agent (Claude Code, Codex or another) can pick the next open item.

## How to use

1. Take the **first open item** in **Now**, then **Next**, then **Later**, whose "Needs" items are all done. Do not skip ahead unless the user says so.
2. Read the linked doc. The doc is the spec. If the doc and the code disagree, stop and ask.
3. Propose an overview first. Write code only after the user approves.
4. One item is one commit. Use the commit subject as written: `type(scope): summary`, 72 characters or fewer, no trailing period.
5. In the same commit, tick the box (`- [x]`), add the short hash to the Done table on the next commit, and update the linked doc when behavior or a decision changes.
6. Items marked **estatell** or **estatell-docs** are commits in that repo. Add them to that repo's `docs/checklist.md` when work starts there, and tick them here when they land.
7. The user can reorder, split or remove items. Edit this file when that happens.

Legend: **Needs** lists items that must be done first. Scopes: `card`, `bridge`, `tray`, `packaging`, `ci`, `docs`.

Plan change, 1 Nov 2026: pairing, the token and the Estatell settings page are dropped. The tray stays and shows the reader, the card and whether an Estatell page is talking to MyKad Reader. See [Why no token](02-bridge/02-security.md#why-no-token).

## Now

### M0: spike and card logic

AOT and PC/SC must work before anything else is built. Docs: [PC/SC access](01-card/02-pcsc.md), [Reading a MyKad](01-card/01-reading-mykad.md).

- [x] `docs: plan MyKad Reader 2.0.0` — this docs folder, the README and the diagrams. Start the git repo with it.
- [x] `chore: add the .NET 10 solution and projects` — `EstatellMyKadReader.slnx`, `Estatell.MyKad` (`IsAotCompatible`), `Estatell.MyKad.Bridge` (`PublishAot`, binary `mykad-bridge`), two xUnit v3 test projects, `Directory.Build.props` (version, nullable, warnings as errors), `Directory.Packages.props`, `.editorconfig`, `global.json` pinning the .NET 10 SDK and the Microsoft Testing Platform runner.
- [ ] `spike(card): read a MyKad with PCSC in an AOT build` — a throw-away console app that selects JPN and reads JPN1-1 with the `PCSC` NuGet, published with AOT for `osx-arm64` and `win-x64`. Record: AOT and trim warnings, whether it reads on both, the binary size, the `PCSC` licence, and whether `win-x86` publishes. Write the result in [PC/SC access](01-card/02-pcsc.md#library) and pick the library. Code stays in `spikes/PcscAot/`, outside the solution. Needs: the solution. **Mac done, Windows AOT builds done (all three architectures), `PCSC` chosen. Open: a read on a real x64 Windows PC.**
- [x] `feat(card): add the reader interface and PC/SC reader` — `IReader`, `IReaderSession`, `ReaderStatus`, `PcscReader` on `PCSC`, `FakeReader`, context reopen on service stop, exclusive T=0 connect, one read at a time, hardware tests behind `MYKAD_HARDWARE=1`. Needs: the spike. Docs: [PC/SC access](01-card/02-pcsc.md).
- [x] `feat(card): read and decode the JPN files` — the APDU sequence, JPN1-1, JPN1-4 and JPN1-2, each field, BCD date and postcode, title case, line 3 joined to line 2, `CardReadException`, buffers cleared. Unit tests per field with fixtures. Needs: the reader interface. Docs: [Read sequence](01-card/01-reading-mykad.md#read-sequence), [Fields](01-card/01-reading-mykad.md#fields).
- [x] `feat(card): map the state to the LHDN code` — port `STATE_CODES` and the postcode fallback from the Python bridge, `state` as name and `state_code` as code. Tests for each state, each postcode range and no match. Needs: decode the JPN files. Docs: [Rules](01-card/01-reading-mykad.md#rules).
- [x] `test(card): match the Python bridge output` — record the raw JPN files of a real card once, replace the personal data, run the Python bridge's decoder on them and save its output as a parity fixture, and a test that decodes the same files and compares field by field. Needs: map the state. Docs: [Tests](01-card/01-reading-mykad.md#tests).

## Next

### M1: the bridge and the tray

Docs: [HTTP API](02-bridge/01-http-api.md), [Security](02-bridge/02-security.md), [Tray](02-bridge/04-tray.md).

- [ ] `spike(tray): show a tray icon from an AOT build` — a throw-away app with a Kestrel server on a background thread and a tray icon with a two-item menu: `Shell_NotifyIcon` on `win-x64`, `NSStatusItem` through `objc_msgSend` on `osx-arm64`, inside a minimal `.app` with `LSUIElement`. Record: AOT and trim warnings, binary size, whether the menu works on both, light and dark icons. Write the result in [Tray](02-bridge/04-tray.md). Code stays in `spikes/TrayAot/`, outside the solution. Needs: the solution. Docs: [Tray](02-bridge/04-tray.md). **Mac done. Open: the Windows builds and a hand check of the menu on a PC.**
- [x] `feat(bridge): serve status on 127.0.0.1:9292` — `CreateSlimBuilder`, `GET /status` (`/`, `/health`) with the 1.x field names plus `device_id`, `paired`, `property_name`, the source-generated JSON context, `--foreground`, `--verbose`, `--version`. Needs: the reader interface. Docs: [GET /status](02-bridge/01-http-api.md#get-status).
- [x] `feat(bridge): refuse foreign hosts and origins` — the Host check, the origin allowlist with `ESTATELL_BRIDGE_ORIGINS` and the dev pattern, CORS and PNA preflight, `403` with `code: forbidden`. Tests for each allowed and refused case. Needs: serve status. Docs: [Checks](02-bridge/02-security.md#checks).
- [x] `refactor(bridge): remove the pairing fields from status` — drop `device_id`, `paired` and `property_name` from `/status`, delete `IConnectionState` and `MemoryConnectionState`, drop `Authorization` from the preflight headers. Update the tests. Needs: refuse foreign hosts. Docs: [GET /status](02-bridge/01-http-api.md#get-status).
- [ ] `fix(bridge): allow dev origins in debug builds only` — `localhost`, `127.0.0.1` and `*.test` only when built in Debug; a release build allows them only through `ESTATELL_BRIDGE_ORIGINS`. Tests for both builds' rules. Needs: refuse foreign hosts. Docs: [Allowed origins](02-bridge/02-security.md#allowed-origins).
- [ ] `feat(bridge): log to a rotating file without card data` — the 1.x paths, 1 MB plus 2 files, a test that a read writes no card field to the log. Needs: serve status. Docs: [Logs](02-bridge/02-security.md#logs).
- [ ] `feat(bridge): run as a single instance` — `SO_EXCLUSIVEADDRUSE` on Windows, the per-user lock, exit 0 for a second copy of the same user, wait for the port for another user. Needs: serve status. Docs: [Single instance](02-bridge/02-security.md#single-instance), [More than one user](02-bridge/04-tray.md#more-than-one-user).
- [ ] `feat(bridge): track when an Estatell page was last seen` — store the time and host of the last request with an allowed `Origin`, "connected" for 10 seconds after it. Tests with a fake clock: allowed, refused and no-`Origin` requests. Needs: remove the pairing fields. Docs: [Menu](02-bridge/04-tray.md#menu).
- [ ] `feat(tray): show the status in the tray and menu bar` — `ITray` with the Windows and macOS versions from the spike and a no-op one, the status, reader, card and Estatell lines, the icons, **Help**, **Restart MyKad Reader**, `WinExe` with `AllocConsole` for `--foreground`, `--no-tray`. Needs: the tray spike, run as a single instance, track when an Estatell page was last seen. Docs: [Tray](02-bridge/04-tray.md).
- [ ] `feat(bridge): read the card` — `GET`/`POST /read`, `403` without an allowed `Origin`, the error `code` table, the 10-second read timeout and `409 busy`. Needs: remove the pairing fields, read and decode the JPN files. Docs: [GET /read](02-bridge/01-http-api.md#get-read).

### M2: Estatell side

Docs: [App changes](04-estatell/02-app-changes.md), [Check-in chip](04-estatell/01-check-in-chip.md).

- [ ] **estatell** `docs(guests): plan MyKad Reader 2.0.0` — rewrite `docs/11-guests/05-mykad-reader.md` and add the items below to the Estatell checklist.
- [ ] **estatell** `feat(guests): show MyKad Reader 2.0.0 status at check-in` — `useMykadReader` handles `code` (falls back to `error` for 1.x), `REQUIRED_BRIDGE_VERSION = '2.0.0'`; `MykadScanner` becomes the status chip with links to the user docs; `bridgeDownload()` points to GitHub Releases. No migration, route or permission. Needs: the Estatell docs item, read the card (this repo). Docs: [Check-in chip](04-estatell/01-check-in-chip.md).

### M3: ship

Docs: [Installers](03-packaging/01-installers.md), [Build and release](03-packaging/02-build-and-release.md), [User docs](04-estatell/03-user-docs.md).

- [ ] `feat(packaging): build the Windows installer` — `installer.iss`: install path, **MyKad Reader** display name, Run key, silent switches, stop and replace an older copy, remove 1.x Python files. Needs: read the card, show the status in the tray. Docs: [Windows](03-packaging/01-installers.md#windows).
- [ ] `feat(packaging): build the macOS package` — universal binary with `lipo`, `MyKad Reader.app` with `LSUIElement`, `pkgbuild` and `productbuild`, the LaunchAgent with `LimitLoadToSessionType` `Aqua`, pre- and post-install scripts, **Uninstall MyKad Reader.app**. Needs: read the card, show the status in the tray. Docs: [macOS](03-packaging/01-installers.md#macos).
- [ ] `ci: build, smoke test and release on tags` — `release.yml` with the Windows and macOS jobs, the smoke tests, the version from the tag, GitHub Release on `v*` in this public repo. Needs: both installers. Docs: [Build and release](03-packaging/02-build-and-release.md).
- [ ] **estatell-docs** `docs(guests): add MyKad Reader set-up and problems pages` — `set-up-mykad-reader` with the download links, rewritten `mykad-reader`, `mykad-reader-problems`, `meta.json`, the confirm prompt and tray screenshots, the S8 exception, a link from onboarding. Needs: show the status in the tray. Docs: [User docs](04-estatell/03-user-docs.md).
- [ ] `chore: release 2.0.0` — run the [clean computer checks](03-packaging/01-installers.md#checks-on-a-clean-computer) on an x64 Windows 10 PC and a Mac on macOS 15, then tag `v2.0.0`. Needs: CI, the Estatell items, the user docs.
- [ ] **estatell** `chore(guests): remove the Python MyKad bridge` — delete `tools/mykad-bridge/`, `mykad-bridge.yml` and the files in `public/downloads/`; remove the Python sections from the Estatell docs. Needs: release 2.0.0.

## Later

### M4: polish

- [ ] `feat(bridge): push card events instead of polling` — `GET /events` (server-sent events) for card in and out. Keep `/status` polling for older Estatell pages. Estatell switches in its own item.
- [ ] `feat(bridge): read without the portrait on request` — `?photo=false` skips JPN1-2 for a faster fill.
- [ ] `feat(packaging): update in the background` — check GitHub Releases once a day and install a newer minor or patch version quietly.
- [ ] `chore(packaging): sign the installers` — when there is a budget. Windows code signing certificate and Apple Developer ID with notarization. Remove the confirm steps from the user docs.

- [ ] `feat(pairing): connect a computer to one hotel` — only if a hotel needs to know which computers have a reader or to cut one off. The dropped design is in git history (`e23270e`, `docs/02-bridge/03-connecting.md`).

## Done

### Commits

| Commit | Subject |
| ------ | ------- |
| `e23270e` | `docs: plan MyKad Reader 2.0.0` |
| `dfd8313` | `chore: add the .NET 10 solution and projects` |
| `023eecc` | `spike(card): read a MyKad with PCSC in an AOT build` |
| `5588059` | `docs(card): record the Windows spike builds` |
| `da6ca25` | `feat(card): add the reader interface and PC/SC reader` |
| `280b457` | `feat(card): read and decode the JPN files` |
| `939ae25` | `feat(card): map the state to the LHDN code` |
| `54eabfe` | `test(card): match the Python bridge output` |
| `8cb5829` | `docs(tray): plan the native tray and the connect code` |
| `2e522d8` | `spike(tray): show a tray icon from an AOT build` |
| `5cc2724` | `feat(bridge): serve status on 127.0.0.1:9292` |
| `7b1ae92` | `feat(bridge): refuse foreign hosts and origins` |
