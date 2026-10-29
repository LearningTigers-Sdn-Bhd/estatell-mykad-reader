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

Legend: **Needs** lists items that must be done first. Scopes: `card`, `bridge`, `pairing`, `packaging`, `ci`, `docs`.

## Now

### M0: spike and card logic

AOT and PC/SC must work before anything else is built. Docs: [PC/SC access](01-card/02-pcsc.md), [Reading a MyKad](01-card/01-reading-mykad.md).

- [x] `docs: plan MyKad Reader 2.0.0` — this docs folder, the README and the diagrams. Start the git repo with it.
- [x] `chore: add the .NET 10 solution and projects` — `EstatellMyKadReader.slnx`, `Estatell.MyKad` (`IsAotCompatible`), `Estatell.MyKad.Bridge` (`PublishAot`, binary `mykad-bridge`), two xUnit v3 test projects, `Directory.Build.props` (version, nullable, warnings as errors), `Directory.Packages.props`, `.editorconfig`, `global.json` pinning the .NET 10 SDK and the Microsoft Testing Platform runner.
- [ ] `spike(card): read a MyKad with PCSC in an AOT build` — a throw-away console app that selects JPN and reads JPN1-1 with the `PCSC` NuGet, published with AOT for `osx-arm64` and `win-x64`. Record: AOT and trim warnings, whether it reads on both, the binary size, the `PCSC` licence, and whether `win-x86` publishes. Write the result in [PC/SC access](01-card/02-pcsc.md#library) and pick the library. Code stays in `spikes/PcscAot/`, outside the solution. Needs: the solution. **Mac done, Windows AOT builds done (all three architectures), `PCSC` chosen. Open: a read on a real x64 Windows PC.**
- [ ] `feat(card): add the reader interface and PC/SC reader` — `IReader`, `ReaderStatus`, `PcscReader` with the library the spike chose (or own `[LibraryImport]` bindings), `FakeReader`, context reopen on service stop, exclusive connect, one read at a time. Needs: the spike. Docs: [PC/SC access](01-card/02-pcsc.md).
- [ ] `feat(card): read and decode the JPN files` — the APDU sequence, JPN1-1, JPN1-4 and JPN1-2, each field, BCD date and postcode, title case, line 3 joined to line 2, `CardReadException`, buffers cleared. Unit tests per field with fixtures. Needs: the reader interface. Docs: [Read sequence](01-card/01-reading-mykad.md#read-sequence), [Fields](01-card/01-reading-mykad.md#fields).
- [ ] `feat(card): map the state to the LHDN code` — port `STATE_CODES` and the postcode fallback from the Python bridge, `state` as name and `state_code` as code. Tests for each state, each postcode range and no match. Needs: decode the JPN files. Docs: [Rules](01-card/01-reading-mykad.md#rules).
- [ ] `test(card): match the Python bridge output` — read the same test card with both, save both outputs (personal data replaced) as a parity fixture, and a test that compares them field by field. Needs: map the state. Docs: [Tests](01-card/01-reading-mykad.md#tests).

## Next

### M1: the bridge and connecting

Docs: [HTTP API](02-bridge/01-http-api.md), [Security](02-bridge/02-security.md), [Connecting](02-bridge/03-connecting.md).

- [ ] `feat(bridge): serve status on 127.0.0.1:9292` — `CreateSlimBuilder`, `GET /status` (`/`, `/health`) with the 1.x field names plus `device_id`, `paired`, `property_name`, the source-generated JSON context, `--foreground`, `--verbose`, `--version`. Needs: the reader interface. Docs: [GET /status](02-bridge/01-http-api.md#get-status).
- [ ] `feat(bridge): refuse foreign hosts and origins` — the Host check, the origin allowlist with `ESTATELL_BRIDGE_ORIGINS` and the dev pattern, CORS and PNA preflight, `403` with `code: forbidden`. Tests for each allowed and refused case. Needs: serve status. Docs: [Checks](02-bridge/02-security.md#checks).
- [ ] `feat(bridge): log to a rotating file without card data` — the 1.x paths, 1 MB plus 2 files, a test that a read writes no card field to the log. Needs: serve status. Docs: [Logs](02-bridge/02-security.md#logs).
- [ ] `feat(bridge): run as a single instance` — `SO_EXCLUSIVEADDRUSE` on Windows, exit 0 when the port is taken. Needs: serve status. Docs: [Single instance](02-bridge/02-security.md#single-instance).
- [ ] `feat(pairing): connect and disconnect this computer` — `device_id` on first start, the shared pairing file (DPAPI on Windows, `0660` on macOS), `POST /pair`, `POST /unpair`, `--reset-pairing`, constant-time compare. Tests for each `/pair` rule. Needs: refuse foreign hosts. Docs: [Connecting](02-bridge/03-connecting.md), [Token](02-bridge/02-security.md#token).
- [ ] `feat(bridge): read the card with a token` — `GET`/`POST /read`, `401` without the token, the error `code` table, the 10-second read timeout and `409 busy`. Needs: connect and disconnect, read and decode the JPN files. Docs: [GET /read](02-bridge/01-http-api.md#get-read).

### M2: Estatell side

Docs: [App changes](04-estatell/02-app-changes.md), [Settings page](04-estatell/01-settings-page.md).

- [ ] **estatell** `docs(guests): plan MyKad Reader 2.0.0 and connecting` — rewrite `docs/11-guests/05-mykad-reader.md` and add the items below to the Estatell checklist.
- [ ] **estatell** `feat(guests): add MyKad reader devices and tokens` — `mykad_devices` migration, model, factory, `mykad-reader.manage` with the role seeder, `devices.store`, `devices.update`, `devices.destroy`, `mykad.token`, activity log entries, Pest tests (other property, revoked, last seen throttle). Needs: the Estatell docs item.
- [ ] **estatell** `feat(guests): add the MyKad Reader settings page` — the steps, the other states, the connected computers table, the try-it read, the settings navigation item, `wayfinder:generate`. Needs: devices and tokens, connect and disconnect (this repo). Docs: [Settings page](04-estatell/01-settings-page.md).
- [ ] **estatell** `feat(guests): read MyKad with a device token` — `useMykadReader` gets the token, sends the header, handles `401` and `code`, reads the 2.0.0 names only, `REQUIRED_BRIDGE_VERSION = '2.0.0'`; `MykadScanner` becomes the status chip; `bridgeDownload()` points to GitHub Releases. Needs: the settings page, read the card with a token (this repo). Docs: [Check-in chip](04-estatell/01-settings-page.md#check-in-chip).

### M3: ship

Docs: [Installers](03-packaging/01-installers.md), [Build and release](03-packaging/02-build-and-release.md), [User docs](04-estatell/03-user-docs.md).

- [ ] `feat(packaging): build the Windows installer` — `installer.iss`: install path, **MyKad Reader** display name, Run key, data folder with Users modify, silent switches, stop and replace an older copy, remove 1.x Python files. Needs: read the card with a token. Docs: [Windows](03-packaging/01-installers.md#windows).
- [ ] `feat(packaging): build the macOS package` — universal binary with `lipo`, `pkgbuild` and `productbuild`, the LaunchAgent, pre- and post-install scripts, data folder, **Uninstall MyKad Reader.app**. Needs: read the card with a token. Docs: [macOS](03-packaging/01-installers.md#macos).
- [ ] `ci: build, smoke test and release on tags` — `release.yml` with the Windows and macOS jobs, the smoke tests, the version from the tag, GitHub Release on `v*` in this public repo. Needs: both installers. Docs: [Build and release](03-packaging/02-build-and-release.md).
- [ ] **estatell-docs** `docs(guests): add MyKad Reader set-up and problems pages` — `set-up-mykad-reader`, rewritten `mykad-reader`, `mykad-reader-problems`, `meta.json`, the confirm prompt screenshots, the S8 exception, a link from onboarding. Needs: the settings page. Docs: [User docs](04-estatell/03-user-docs.md).
- [ ] `chore: release 2.0.0` — run the [clean computer checks](03-packaging/01-installers.md#checks-on-a-clean-computer) on an x64 Windows 10 PC and a Mac on macOS 15, then tag `v2.0.0`. Needs: CI, the Estatell items, the user docs.
- [ ] **estatell** `chore(guests): remove the Python MyKad bridge` — delete `tools/mykad-bridge/`, `mykad-bridge.yml` and the files in `public/downloads/`; remove the Python sections from the Estatell docs. Needs: release 2.0.0.

## Later

### M4: polish

- [ ] `feat(bridge): push card events instead of polling` — `GET /events` (server-sent events) for card in and out. Keep `/status` polling for older Estatell pages. Estatell switches in its own item.
- [ ] `feat(bridge): read without the portrait on request` — `?photo=false` skips JPN1-2 for a faster fill.
- [ ] `feat(packaging): update in the background` — check GitHub Releases once a day and install a newer minor or patch version quietly.
- [ ] `chore(packaging): sign the installers` — when there is a budget. Windows code signing certificate and Apple Developer ID with notarization. Remove the confirm steps from the user docs.

### Open questions

- [ ] One computer connected to two hotels? See [Connecting](02-bridge/03-connecting.md#open-questions).
- [ ] Disconnect computers not seen for 90 days?

## Done

### Commits

| Commit | Subject |
| ------ | ------- |
| `e23270e` | `docs: plan MyKad Reader 2.0.0` |
| `dfd8313` | `chore: add the .NET 10 solution and projects` |
