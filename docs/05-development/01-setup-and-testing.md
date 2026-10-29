# Setup and testing

## Purpose

How to build, run and test MyKad Reader. The team works on Macs. Most hotels use Windows.

## Setup

- .NET 10 SDK, 10.0.400 or a later patch. `global.json` pins it.
- macOS: Xcode command line tools (for the AOT linker and `lipo`).
- An ACS ACR39U reader and a MyKad for hardware tests.

```
dotnet build
dotnet test
dotnet run --project src/Estatell.MyKad.Bridge -- --foreground --verbose
dotnet publish src/Estatell.MyKad.Bridge -c Release -r osx-arm64
```

## Layout

```
EstatellMyKadReader.slnx
global.json                    SDK pin, Microsoft Testing Platform runner
Directory.Build.props          net10.0, version, nullable, warnings as errors
Directory.Packages.props       all NuGet versions
src/Estatell.MyKad/            card reading, IsAotCompatible
src/Estatell.MyKad.Bridge/     the local server, PublishAot, binary mykad-bridge
tests/Estatell.MyKad.Tests/
tests/Estatell.MyKad.Bridge.Tests/
```

- Add a package version to `Directory.Packages.props`, not to the `.csproj`.
- `PublishAot` is set in the bridge project only. Test projects cannot publish with AOT.
- Any AOT or trim warning fails the build.
- Test projects use xUnit v3 (`xunit.v3.mtp-v2`) and run as executables.

Estatell on `*.test` or `localhost` is an allowed origin, so a local Estatell can call the local app.

## Machines

```
 Your Mac                    CI (GitHub Actions)       Real Windows PC
 ─────────                   ───────────────────       ───────────────
 build + test osx            win-x64/arm64/x86 + osx   1× x64 Windows 10 or 11
 ACR39U on USB               AOT warnings, smoke tests with ACR39U
                             .exe as a download        (what hotels have)
```

- There is no Windows VM. Windows builds come from CI. Card reads on Windows are checked on a real x64 PC.
- To try a build on a Windows PC, download the `.exe` from the workflow run. It is one file and needs nothing installed. The reader uses the built-in Windows driver.
- Keep one real x64 Windows PC for release checks.

## Tests

| Project                         | Covers                                                                          |
| ------------------------------- | ------------------------------------------------------------------------------- |
| `Estatell.MyKad.Tests`          | Each field, BCD dates and postcodes, state mapping, bad status words, parity with the Python bridge. Uses `FakeReader` |
| `Estatell.MyKad.Bridge.Tests`   | Each route, each `code`, Host and Origin checks, token checks, `/pair` rules, CORS and PNA headers. Uses `WebApplicationFactory` with `FakeReader` |

- No test needs a real reader, except the hardware tests. They are skipped unless `MYKAD_HARDWARE=1` is set, with a reader and a MyKad plugged in:

```
MYKAD_HARDWARE=1 dotnet test --project tests/Estatell.MyKad.Tests
```
- Fixtures replace all personal data. Never commit a real card's data or portrait.

## Recording fixtures

`--record <folder>` (debug builds only) saves the raw responses of one read. Replace the name, IC, address and portrait before committing.

## Hardware checks

Before each release, on the release-check machines. See [Checks on a clean computer](../03-packaging/01-installers.md#checks-on-a-clean-computer).
