# App changes

## Purpose

What changes in the Estatell repo for MyKad Reader 2.0.0. These are Estatell commits. They ship in one Estatell release, at the same time as 2.0.0.

No migration, model, route, permission or settings page. Only the frontend and the docs change.

## Frontend

| File                                                   | Change                                                                                  |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------- |
| `resources/js/hooks/use-mykad-reader.ts`               | Handle `code` from `/read`, falling back to `error` for 1.x. `REQUIRED_BRIDGE_VERSION = '2.0.0'` |
| `resources/js/components/pms/mykad-scanner.tsx`        | Status chip only. See [Check-in chip](01-check-in-chip.md)                               |
| `bridgeDownload()`                                     | GitHub Releases links. The "other OS" case shows "not supported"                         |

## Removed

- `tools/mykad-bridge/` (Python bridge and PyInstaller packaging).
- `.github/workflows/mykad-bridge.yml`.
- `public/downloads/mykad-bridge.zip`, `run_bridge.bat`, `run_bridge.sh`, and the 1.x installers in `public/downloads/`.

## Docs

- `docs/11-guests/05-mykad-reader.md`: link to this repo's docs, remove the Python and manual fallback sections, change "macOS 11+" to "macOS 15+".
- `docs/checklist.md`: add the Estatell items from this repo's [checklist](../checklist.md).
