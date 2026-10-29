# Build and release

## Purpose

Build both installers in CI from a git tag and publish them to GitHub Releases, where Estatell links to them.

## Build

Native AOT cannot build for another OS, so each OS builds on its own runner.

| Runner           | Steps                                                                                       |
| ---------------- | ------------------------------------------------------------------------------------------- |
| `windows-latest` | .NET 10 SDK → `dotnet publish -c Release -r win-x64 -p:PublishAot=true`, same for `win-arm64` → Inno Setup |
| `macos-latest`   | .NET 10 SDK → publish `osx-arm64` and `osx-x64` → `lipo -create` → `pkgbuild` → `productbuild` |

`win-x86` is added only if the M0 spike shows AOT supports it.

## Smoke tests

Each runner starts the built app and checks:

- `GET /status` returns `version` equal to the tag.
- A foreign `Origin` gets `403`.
- A wrong `Host` gets `403`.
- `/read` without a token gets `401`.

## Release

| Trigger                    | What happens                                                    |
| -------------------------- | --------------------------------------------------------------- |
| Pull request               | Build and smoke tests. Nothing is published                     |
| Tag `v*` (for example `v2.0.0`) | Build, smoke tests, then a GitHub Release with both installers |

- The version comes from the tag. `Directory.Build.props` reads it, so the app, the installers and `/status` agree.
- Estatell links never change:

```
https://github.com/<org>/estatell-mykad-reader/releases/latest/download/estatell-mykad-setup.exe
https://github.com/<org>/estatell-mykad-reader/releases/latest/download/estatell-mykad.pkg
```

- The repo must be public, or CI publishes to a separate public repo (`estatell-mykad-releases`). Hotel admins have no GitHub login.

## Versions

- Semantic versions. A change Estatell needs is a minor or major bump.
- When Estatell needs a new build, bump `REQUIRED_BRIDGE_VERSION` in `estatell/resources/js/hooks/use-mykad-reader.ts` in the same Estatell release.
- 2.0.0 is a major bump: `/read` needs a token.

## Licences

Inno Setup is free for commercial use. Since 6.5.0 its author asks commercial users to buy a licence, but it is not required. `pkgbuild`, `productbuild` and `lipo` ship with Xcode. .NET is MIT. Check the `PCSC` licence in the M0 spike.
