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
src/Estatell.MyKad.Bridge/     the local server, PublishAot, binary mykad-reader
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
| `Estatell.MyKad.Bridge.Tests`   | Each route, each `code`, Host and Origin checks, `/read` with no `Origin`, the Estatell seen time, CORS and PNA headers. Uses `WebApplicationFactory` with `FakeReader` |

- No test needs a real reader, except the hardware tests. They are skipped unless `MYKAD_HARDWARE=1` is set, with a reader and a MyKad plugged in:

```
MYKAD_HARDWARE=1 dotnet test --project tests/Estatell.MyKad.Tests
```
- Fixtures replace all personal data. Never commit a real card's data or portrait.

## Recording fixtures

`--record <folder>` (debug builds only, planned with the bridge) saves the raw responses of one read. Replace the name, IC, address and portrait before committing.

### Recording the parity fixture

1. With the reader and a MyKad plugged in, save the raw JPN files to a folder **outside** the repo. The test refuses a folder inside it.

   ```
   MYKAD_HARDWARE=1 MYKAD_RECORD=~/mykad-raw dotnet test --project tests/Estatell.MyKad.Tests --filter-method "*RecordsTheJpnFiles"
   ```

2. Clean the files and run the Python bridge's decoder on them. The bridge left Estatell after `670c6a01`, so pass that commit's `tools/mykad-bridge` with `--bridge <folder>`. The script does not need pyscard.

   ```
   python3 tools/parity/make_fixture.py ~/mykad-raw
   ```

   It replaces each personal field with made-up values of the same shape, keeps the padding, and turns any other unknown byte into `X`. It stops without saving if a word of the card's text is still there.
3. Check the hex dumps (`xxd tests/Estatell.MyKad.Tests/fixtures/parity/jpn1-1.bin`) and `python.json`, then delete `~/mykad-raw`.

## Hardware checks

Before each release, on the release-check machines. See [Checks on a clean computer](../03-packaging/01-installers.md#checks-on-a-clean-computer).
