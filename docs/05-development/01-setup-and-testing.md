# Setup and testing

## Purpose

How to build, run and test MyKad Reader. The team works on Macs. Most hotels use Windows.

## Setup

- .NET 10 SDK.
- macOS: Xcode command line tools (for the AOT linker and `lipo`).
- An ACS ACR39U reader and a MyKad for hardware tests.

```
dotnet build
dotnet test
dotnet run --project src/Estatell.MyKad.Bridge -- --foreground --verbose
```

Estatell on `*.test` or `localhost` is an allowed origin, so a local Estatell can call the local app.

## Machines

```
 Your Mac                    CI                        Release check
 ─────────                   ──                        ─────────────
 build + test osx            win-x64/arm64 + osx        1× x64 Windows 10 PC
 ACR39U on USB               smoke tests                with ACR39U
 Parallels Win11 ARM                                    (what hotels have)
  (tests win-arm64 only)
```

- A Windows 11 VM in Parallels on Apple Silicon is Windows on Arm. It tests `win-arm64`, not the x64 build most hotels run. Pass the reader into the VM over USB.
- Keep one real x64 Windows 10 PC for release checks.

## Tests

| Project                         | Covers                                                                          |
| ------------------------------- | ------------------------------------------------------------------------------- |
| `Estatell.MyKad.Tests`          | Each field, BCD dates and postcodes, state mapping, bad status words, parity with the Python bridge. Uses `FakeReader` |
| `Estatell.MyKad.Bridge.Tests`   | Each route, each `code`, Host and Origin checks, token checks, `/pair` rules, CORS and PNA headers. Uses `WebApplicationFactory` with `FakeReader` |

- No test needs a real reader.
- Fixtures replace all personal data. Never commit a real card's data or portrait.

## Recording fixtures

`--record <folder>` (debug builds only) saves the raw responses of one read. Replace the name, IC, address and portrait before committing.

## Hardware checks

Before each release, on the release-check machines. See [Checks on a clean computer](../03-packaging/01-installers.md#checks-on-a-clean-computer).
