# Build and release

## Purpose

Build both installers in CI from a git tag and publish them to GitHub Releases, where Estatell links to them.

## Build

Native AOT cannot build for another OS, so each OS builds on its own runner.

| Runner           | Steps                                                                                       |
| ---------------- | ------------------------------------------------------------------------------------------- |
| `windows-latest` | .NET 10 SDK → `dotnet publish src/Estatell.MyKad.Bridge -c Release -r win-x64`, same for `win-arm64` → Inno Setup |
| `macos-latest`   | .NET 10 SDK → publish `osx-arm64` and `osx-x64` → `lipo -create` → `pkgbuild` → `productbuild` |

`win-x86` publishes with AOT (M0 spike, 1.7 MB). Ship it only if a hotel still runs 32-bit Windows 10. One Inno Setup installer can carry x64, arm64 and x86.

## Smoke tests

`packaging/smoke-test.sh` starts the built app with `--no-tray` on `windows-latest` (x64), `windows-11-arm` (arm64), `macos-latest` (arm64) and `macos-15-intel` (x64). It checks:

- `GET /status` returns `version` equal to the tag.
- A foreign `Origin` gets `403`.
- A wrong `Host` gets `403`.
- `/read` with no `Origin` gets `403`.
- A dev `Origin` (`http://estatell.test`) gets `403` in the release build.

## Release

| Trigger                    | What happens                                                    |
| -------------------------- | --------------------------------------------------------------- |
| Pull request that changes the app or packaging | Build and smoke tests. The installers go to the workflow run. Nothing is published |
| Tag `v*` (for example `v2.0.0`) | Build, smoke tests, then a GitHub Release with both installers |

- The version comes from the tag and overrides `Directory.Build.props`, so the app, the installers and `/status` agree. A pull request uses the version in `Directory.Build.props`.
- Each release has six files, with the version in the name:

| File | Holds |
| ---- | ----- |
| `estatell-mykad-reader-setup-v2.0.0.exe` | Windows x64 and arm64 |
| `estatell-mykad-reader-setup-v2.0.0_x64.exe` | Windows x64 |
| `estatell-mykad-reader-setup-v2.0.0_arm64.exe` | Windows arm64 |
| `estatell-mykad-reader-v2.0.0.pkg` | Mac, Apple Silicon and Intel |
| `estatell-mykad-reader-v2.0.0_apple-silicon.pkg` | Mac, Apple Silicon |
| `estatell-mykad-reader-v2.0.0_intel.pkg` | Mac, Intel |

- GitHub shows the SHA-256 of each file on the release page.
- Estatell and the user docs link to the two universal files at an exact version, so staff never choose a chip. Update both with each release that hotels should download:

```
https://github.com/LearningTigers-Sdn-Bhd/estatell-mykad-reader/releases/download/v2.0.0/estatell-mykad-reader-setup-v2.0.0.exe
https://github.com/LearningTigers-Sdn-Bhd/estatell-mykad-reader/releases/download/v2.0.0/estatell-mykad-reader-v2.0.0.pkg
```

- The repo is public (`LearningTigers-Sdn-Bhd/estatell-mykad-reader`), so hotel admins download without a GitHub login, and Actions minutes are free.

## Versions

- Semantic versions. A change Estatell needs is a minor or major bump.
- When Estatell needs a new build, bump `REQUIRED_BRIDGE_VERSION` in `estatell/resources/js/hooks/use-mykad-reader.ts` in the same Estatell release.
- 2.0.0 is a major bump: a new app with a tray, an error `code`, no `message`, and `/read` needs an allowed `Origin`.

## Licences

Inno Setup is free for commercial use. Since 6.5.0 its author asks commercial users to buy a licence, but it is not required. `pkgbuild`, `productbuild` and `lipo` ship with Xcode. .NET is MIT. `PCSC` is BSD-2-Clause: include its copyright notice and licence text with the installers.
