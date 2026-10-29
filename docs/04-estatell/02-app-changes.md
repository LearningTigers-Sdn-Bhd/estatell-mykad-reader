# App changes

## Purpose

What changes in the Estatell repo for MyKad Reader 2.0.0. These are Estatell commits. They ship in one Estatell release, at the same time as 2.0.0.

## Data model

```
mykad_devices    id, property_id, device_id, name, os, bridge_version,
                 secret (encrypted), paired_by (user), paired_at,
                 last_seen_at, revoked_at, timestamps
```

- Unique `(property_id, device_id)` where `revoked_at` is null.
- `secret` uses Laravel's `encrypted` cast. Estatell must read it back to give the token out.
- `os` is `windows` or `macos`, with a `CHECK` constraint.

## Routes

Under `/p/{code}/settings/mykad-reader` (`settings.mykad-reader.*`).

| Route                | Permission             | What                                                              |
| -------------------- | ---------------------- | ----------------------------------------------------------------- |
| `index`              | `mykad-reader.manage`  | The settings page                                                 |
| `devices.store`      | `mykad-reader.manage`  | Make a device and a secret. Returns the secret once               |
| `devices.update`     | `mykad-reader.manage`  | Rename                                                            |
| `devices.destroy`    | `mykad-reader.manage`  | Disconnect: sets `revoked_at`                                     |

Under `/p/{code}/mykad` (`mykad.*`), for any user who can open a screen with the MyKad box:

| Route                | What                                                                                  |
| -------------------- | ------------------------------------------------------------------------------------- |
| `token`              | `GET` with `device_id`. Returns the token if the device is in this property and not revoked. Updates `last_seen_at` (at most every 5 minutes) |

## Permission

`mykad-reader.manage`, label **Manage MyKad reader**. Given to the Owner and Manager roles by the property seeder.

## Activity log

`mykad_reader.connected`, `mykad_reader.renamed`, `mykad_reader.disconnected` on the property. Never the secret.

## Frontend

| File                                                   | Change                                                                                  |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------- |
| `resources/js/hooks/use-mykad-reader.ts`               | Get the token for `device_id`, send `Authorization: Bearer`, handle `401` and `code`. Read the 2.0.0 field names only. `REQUIRED_BRIDGE_VERSION = '2.0.0'` |
| `resources/js/components/pms/mykad-scanner.tsx`        | Status chip only. See [Check-in chip](01-settings-page.md#check-in-chip)                 |
| `bridgeDownload()`                                     | GitHub Releases links. The "other OS" case shows "not supported"                         |
| New settings page                                      | See [Settings page](01-settings-page.md)                                                 |
| Settings navigation                                    | **MyKad Reader** item for `mykad-reader.manage`                                          |

## Removed

- `tools/mykad-bridge/` (Python bridge and PyInstaller packaging).
- `.github/workflows/mykad-bridge.yml`.
- `public/downloads/mykad-bridge.zip`, `run_bridge.bat`, `run_bridge.sh`, and the 1.x installers in `public/downloads/`.

## Docs

- `docs/11-guests/05-mykad-reader.md`: rewrite around connecting, link to this repo's docs, remove the Python and manual fallback sections, change "macOS 11+" to "macOS 15+".
- `docs/checklist.md`: add the Estatell items from this repo's [checklist](../checklist.md).
